// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto.Storage {
    using Microsoft.Azure.IIoT.Crypto.Models;
    using Microsoft.Azure.IIoT.Crypto.Storage.Models;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Storage;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Certificate store using an underlying database.
    /// </summary>
    public class CertificateDatabase : ICertificateRepository, ICertificateStore {

        /// <summary>
        /// Create database
        /// </summary>
        /// <param name="container"></param>
        /// <param name="keys"></param>
        public CertificateDatabase(IItemContainerFactory container,
            IKeyHandleSerializer keys) {
            _certificates = container.OpenAsync("certificates").Result.AsDocuments();
            _keys = keys ?? throw new ArgumentNullException(nameof(keys));
        }

        /// <inheritdoc/>
        public async Task AddCertificateAsync(string certificateName,
            Certificate certificate, string id, CancellationToken ct) {
            var document = certificate.ToDocument(certificateName, id, _keys);
            var newDoc = await _certificates.UpsertAsync(document, ct);
        }

        /// <inheritdoc/>
        public async Task<Certificate> FindCertificateAsync(string certificateId,
            CancellationToken ct) {

            // Select top 1 - there should only be 1
            var query = "SELECT TOP 1 * FROM Certificates c WHERE";
            query += $" c.{nameof(CertificateDocument.Type)} = '{nameof(Certificate)}'";

            // With matching id
            query += $" AND c.{nameof(CertificateDocument.CertificateId)} = @certificateId";
            var queryParameters = new Dictionary<string, object> {
                { "@certificateId", certificateId }
            };

            // Latest on top
            query += $" ORDER BY c.{nameof(CertificateDocument.Version)} DESC";
            var client = _certificates.OpenSqlClient();
            var result = client.Query<CertificateDocument>(query, queryParameters, 1);
            var documents = await result.ReadAsync(ct);
            return DocumentToCertificate(documents.SingleOrDefault()?.Value);
        }

        /// <inheritdoc/>
        public async Task<string> DisableCertificateAsync(Certificate certificate,
            CancellationToken ct) {
            if (certificate?.RawData == null) {
                throw new ArgumentNullException(nameof(certificate));
            }
            var now = DateTime.UtcNow;
            while (true) {
                var document = await _certificates.FindAsync<CertificateDocument>(
                    certificate.GetSerialNumberAsString(), ct);
                if (document == null) {
                    throw new ResourceNotFoundException("Certificate was not found");
                }
                if (document.Value?.DisabledSince != null) {
                    // Already disabled
                    return document.Value.CertificateId;
                }
                try {
                    var newDocument = document.Value.Clone();
                    newDocument.DisabledSince = now;
                    document = await _certificates.ReplaceAsync(document,
                        newDocument, ct);

                    // TODO: Notify disabled certificate
                    return document.Value.CertificateId;
                }
                catch (ResourceOutOfDateException) {
                    continue; // Replace failed due to etag out of date - retry
                }
            }
        }

        /// <inheritdoc/>
        public async Task<Certificate> GetCertificateAsync(byte[] serialNumber,
            CancellationToken ct) {
            var serial = new SerialNumber(serialNumber).ToString();
            var document = await _certificates.GetAsync<CertificateDocument>(serial, ct);
            return DocumentToCertificate(document?.Value);
        }

        /// <inheritdoc/>
        public async Task<Certificate> GetLatestCertificateAsync(string certificateName,
            CancellationToken ct) {

            // Select top 1
            var query = "SELECT TOP 1 * FROM Certificates c WHERE";
            query += $" c.{nameof(CertificateDocument.Type)} = '{nameof(Certificate)}'";

            // With matching name
            query += $" AND c.{nameof(CertificateDocument.CertificateName)} = @certificateName";
            var queryParameters = new Dictionary<string, object> {
                { "@certificateName", certificateName }
            };

            // Latest on top
            query += $" ORDER BY c.{nameof(CertificateDocument.Version)} DESC";

            var client = _certificates.OpenSqlClient();
            var result = client.Query<CertificateDocument>(query, queryParameters, 1);
            var documents = await result.ReadAsync(ct);
            return DocumentToCertificate(documents.SingleOrDefault()?.Value);
        }

        /// <inheritdoc/>
        public async Task<CertificateCollection> QueryCertificatesAsync(
            CertificateFilter filter, int? pageSize, CancellationToken ct) {

            var queryParameters = new Dictionary<string, object>();
            var query = "SELECT * FROM Certificates c WHERE";
            query += $" c.{nameof(CertificateDocument.Type)} = '{nameof(Certificate)}'";

            if (filter.NotBefore != null) {
                query += $" AND c.{nameof(CertificateDocument.NotBefore)} <= @NotBefore";
                queryParameters.Add("@NotBefore", filter.NotBefore.ToString());
            }
            if (filter.NotAfter != null) {
                query += $" AND c.{nameof(CertificateDocument.NotAfter)} >= @NotAfter";
                queryParameters.Add("@NotAfter", filter.NotAfter.ToString());
            }
            if (filter.IncludeDisabled && filter.ExcludeEnabled) {
                query += $" AND IS_DEFINED(c.{nameof(CertificateDocument.DisabledSince)})";
            }
            if (!filter.IncludeDisabled && !filter.ExcludeEnabled) {
                query += $" AND NOT IS_DEFINED(c.{nameof(CertificateDocument.DisabledSince)})";
            }
            if (filter.CertificateName != null) {
                query += $" AND c.{nameof(CertificateDocument.CertificateName)} = @Name";
                queryParameters.Add("@Name", filter.CertificateName);
            }
            if (filter.Subject != null) {
                query += $" AND (c.{nameof(CertificateDocument.Subject)} = @Subject";
                if (filter.IncludeAltNames) {
                    query += $" OR ARRAY_CONTAINS(" +
                        $"c.{nameof(CertificateDocument.SubjectAltNames)}, @Subject )";
                }
                query += " )";
                queryParameters.Add("@Subject", filter.Subject.Name);
            }
            if (filter.Thumbprint != null) {
                query += $" AND c.{nameof(CertificateDocument.Thumbprint)} = @Thumbprint";
                queryParameters.Add("@Thumbprint", filter.Thumbprint);
            }
            if (filter.KeyId != null) {
                query += $" AND c.{nameof(CertificateDocument.KeyId)} = @KeyId";
                queryParameters.Add("@KeyId", filter.KeyId);
            }
            if (filter.IsIssuer != null) {
                query += $" AND c.{nameof(CertificateDocument.IsIssuer)} = @IsIssuer";
                queryParameters.Add("@IsIssuer", filter.IsIssuer.Value);
            }
            if (filter.Issuer != null) {
                query += $" AND (c.{nameof(CertificateDocument.Issuer)} = @Issuer";
                if (filter.IncludeAltNames) {
                    query += $" OR ARRAY_CONTAINS(" +
                        $"c.{nameof(CertificateDocument.IssuerAltNames)}, @Issuer )";
                }
                query += " )";
                queryParameters.Add("@Issuer", filter.Issuer.Name);
            }
            if (filter.Issuer != null) {
                query += $" AND c.{nameof(CertificateDocument.IssuerSerialNumber)} = @Isn";
                queryParameters.Add("@Isn", new SerialNumber(filter.IssuerSerialNumber).ToString());
            }
            if (filter.IssuerKeyId != null) {
                query += $" AND c.{nameof(CertificateDocument.IssuerKeyId)} = @IssuerKeyId";
                queryParameters.Add("@IssuerKeyId", filter.IssuerKeyId);
            }

            query += $" ORDER BY c.{nameof(CertificateDocument.Version)} DESC";

            var client = _certificates.OpenSqlClient();
            var result = client.Query<CertificateDocument>(query, queryParameters,
                pageSize);
            var documents = await result.ReadAsync(ct);
            return new CertificateCollection {
                Certificates = documents
                    .Select(c => DocumentToCertificate(c.Value))
                    .ToList(),
                ContinuationToken = result.ContinuationToken
            };
        }

        /// <inheritdoc/>
        public async Task<CertificateCollection> ListCertificateChainAsync(
            Certificate certificate, CancellationToken ct) {
            if (certificate?.RawData == null) {
                throw new ArgumentNullException(nameof(certificate));
            }
            var chain = await ListChainAsync(certificate, ct);
            return new CertificateCollection {
                Certificates = chain.ToList()
            };
        }

        /// <inheritdoc/>
        public async Task<CertificateCollection> ListCertificatesAsync(
            string continuationToken, int? pageSize, CancellationToken ct) {
            var client = _certificates.OpenSqlClient();
            IResultFeed<IDocumentInfo<CertificateDocument>> result;
            if (!string.IsNullOrEmpty(continuationToken)) {
                result = client.Continue<CertificateDocument>(continuationToken, pageSize);
            }
            else {
                var query = "SELECT * FROM Certificates c WHERE";
                query += $" c.{nameof(CertificateDocument.Type)} = '{nameof(Certificate)}'";
                query += $" ORDER BY c.{nameof(CertificateDocument.Version)} DESC";
                result = client.Query<CertificateDocument>(query, null, pageSize);
            }
            var documents = await result.ReadAsync(ct);
            return new CertificateCollection {
                Certificates = documents
                    .Select(c => DocumentToCertificate(c.Value))
                    .ToList(),
                ContinuationToken = result.ContinuationToken
            };
        }

        /// <summary>
        /// Quick query by subject dn
        /// </summary>
        /// <param name="subjectName"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<Certificate> GetCertificateBySubjectAsync(X500DistinguishedName subjectName,
            CancellationToken ct) {

            // Select top 1
            var query = "SELECT TOP 1 * FROM Certificates c WHERE";
            query += $" c.{nameof(CertificateDocument.Type)} = '{nameof(Certificate)}'";

            // With matching name
            query += $" AND c.{nameof(CertificateDocument.Subject)} = @name";
            var queryParameters = new Dictionary<string, object> {
                { "@name", subjectName.Name }
            };

            // Latest on top
            query += $" ORDER BY c.{nameof(CertificateDocument.Version)} DESC";

            var client = _certificates.OpenSqlClient();
            var result = client.Query<CertificateDocument>(query, queryParameters, 1);
            var documents = await result.ReadAsync(ct);
            return DocumentToCertificate(documents.SingleOrDefault()?.Value);
        }

        /// <summary>
        /// Try list chain by serial number and if fails use subject names
        /// </summary>
        /// <param name="certificate"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task<IEnumerable<Certificate>> ListChainAsync(Certificate certificate,
            CancellationToken ct) {
            try {
                // Try find chain using serial and issuer serial
                return await GetChainBySerialAsync(certificate, ct);
            }
            catch (ResourceNotFoundException) {
                // Try traditional x500 name matching
                return await GetChainByNameAsync(certificate, ct);
            }
        }

        /// <summary>
        /// Get chain by serial number
        /// </summary>
        /// <param name="certificate"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task<IEnumerable<Certificate>> GetChainBySerialAsync(
            Certificate certificate, CancellationToken ct) {
            var chain = new List<Certificate> { certificate };
            // Compare subject and issuer serial number
            var issuer = certificate.GetIssuerSerialNumberAsString();
            if (string.IsNullOrEmpty(issuer)) {
                throw new ResourceNotFoundException("Issuer serial not found");
            }
            while (!certificate.IsSelfSigned()) {
                certificate = await GetCertificateAsync(certificate.IssuerSerialNumber, ct);
                if (certificate?.RawData == null) {
                    throw new ResourceNotFoundException("Incomplete chain");
                }
                chain.Add(certificate);
            }
            // Reverse to have root first
            chain.Reverse();
            return Validate(chain);
        }

        /// <summary>
        /// Get chain using subject names
        /// </summary>
        /// <param name="certificate"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task<IEnumerable<Certificate>> GetChainByNameAsync(
            Certificate certificate, CancellationToken ct) {
            var chain = new List<Certificate> { certificate };
            // Compare subject and issuer names
            while (!certificate.IsSelfSigned()) {
                certificate = await GetCertificateBySubjectAsync(certificate.Issuer, ct);
                if (certificate?.RawData == null) {
                    throw new ResourceNotFoundException("Incomplete chain");
                }
                chain.Add(certificate);
            }
            // Reverse to have root first
            chain.Reverse();
            return Validate(chain);
        }

        /// <summary>
        /// Validate the chain
        /// </summary>
        /// <param name="chain"></param>
        private IEnumerable<Certificate> Validate(List<Certificate> chain) {
            if (!chain.First().IsValidChain(chain, out var status)) {
                throw new CryptographicException(status.AsString("Chain invalid:"));
            }
            return chain;
        }

        /// <summary>
        /// Convert to certificate model
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        private Certificate DocumentToCertificate(CertificateDocument document) {
            if (document == null) {
                return null;
            }
            var keyHandle = _keys.DeserializeHandle(document.KeyHandle);
            return CertificateEx.Create(document.RawData,
                keyHandle,
                document.IsserPolicies,
                document.DisabledSince == null ? null : new RevocationInfo {
                    Date = document.DisabledSince,
                    // ...
                });
        }

        private readonly IDocuments _certificates;
        private readonly IKeyHandleSerializer _keys;
    }
}

