// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack;

using Opc.Ua;
using Opc.Ua.Security.Certificates;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// A flat certificate store option to use with a secret/volume mount
/// </summary>
public sealed class FlatCertificateStore : ICertificateStoreType
{
    /// <summary>
    /// Identifier for flat directory certificate store.
    /// </summary>
    public const string StoreTypeName = "FlatDirectory";

    /// <summary>
    /// Prefix for flat directory certificate store paths.
    /// </summary>
    public const string StoreTypePrefix = $"{StoreTypeName}:";

    /// <inheritdoc/>
    public ICertificateStore CreateStore() => new FlatDirectoryCertificateStore();

    /// <inheritdoc/>
    public bool SupportsStorePath(string storePath) => !string.IsNullOrEmpty(storePath) &&
            storePath.StartsWith(StoreTypePrefix,
                StringComparison.InvariantCultureIgnoreCase);

    /// <summary>
    /// Flat directory certificate store that does not have internal
    /// hierarchy with certs/crl/private subdirectories.
    /// </summary>
    internal sealed class FlatDirectoryCertificateStore : ICertificateStore
    {
        private const string CrtExtension = ".crt";
        private const string KeyExtension = ".key";

        private readonly DirectoryCertificateStore _innerStore;

        /// <summary>
        /// Create certificate store
        /// </summary>
        public FlatDirectoryCertificateStore()
        {
            _innerStore = new DirectoryCertificateStore(noSubDirs: true);
        }

        /// <inheritdoc/>
        public string StoreType => StoreTypeName;

        /// <inheritdoc/>
        public string StorePath => _innerStore.StorePath;

        /// <inheritdoc/>
        public bool SupportsLoadPrivateKey => _innerStore.SupportsLoadPrivateKey;

        /// <inheritdoc/>
        public bool SupportsCRLs => _innerStore.SupportsCRLs;

        public bool NoPrivateKeys => _innerStore.NoPrivateKeys;

        /// <inheritdoc/>
        public void Dispose() => _innerStore.Dispose();

        /// <inheritdoc/>
        public void Open(string location, bool noPrivateKeys = true)
        {
            ArgumentNullException.ThrowIfNullOrEmpty(location);
            if (!location.StartsWith(StoreTypePrefix, StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    $"Expected argument {nameof(location)} starting with {StoreTypePrefix}",
                    nameof(location));
            }
            _innerStore.Open(location.Substring(StoreTypePrefix.Length), noPrivateKeys);
        }

        /// <inheritdoc/>
        public void Close() => _innerStore.Close();

        /// <inheritdoc/>
        public Task AddAsync(X509Certificate2 certificate, string? password = null, CancellationToken ct = default)
            => _innerStore.AddAsync(certificate, password, ct);

        /// <inheritdoc/>
        public Task AddRejectedAsync(X509Certificate2Collection certificates, int maxCertificates, CancellationToken ct = default)
            => _innerStore.AddRejectedAsync(certificates, maxCertificates, ct);

        /// <inheritdoc/>
        public Task<bool> DeleteAsync(string thumbprint, CancellationToken ct = default)
            => _innerStore.DeleteAsync(thumbprint, ct);

        /// <inheritdoc/>
        public async Task<X509Certificate2Collection> EnumerateAsync(CancellationToken ct = default)
        {
            var certificatesCollection =
                await _innerStore.EnumerateAsync(ct).ConfigureAwait(false);
            if (!_innerStore.Directory.Exists)
            {
                return certificatesCollection;
            }

            foreach (var file in _innerStore.Directory.GetFiles('*' + CrtExtension))
            {
                try
                {
                    var certificates = new X509Certificate2Collection();
                    certificates.ImportFromPemFile(file.FullName);
                    certificatesCollection.AddRange(certificates);
                    foreach (var certificate in certificates)
                    {
                        Utils.LogInfo("Enumerate certificates - certificate added {thumbprint}",
                            certificate.Thumbprint);
                    }
                }
                catch (Exception e)
                {
                    Utils.LogError(e, "Could not load certificate from file: {fileName}",
                        file.FullName);
                }
            }

            return certificatesCollection;
        }

        /// <inheritdoc/>
        public Task AddCRLAsync(X509CRL crl, CancellationToken ct = default)
            => _innerStore.AddCRLAsync(crl, ct);

        /// <inheritdoc/>
        public Task<bool> DeleteCRLAsync(X509CRL crl, CancellationToken ct = default)
            => _innerStore.DeleteCRLAsync(crl, ct);

        /// <inheritdoc/>
        public Task<X509CRLCollection> EnumerateCRLsAsync(CancellationToken ct = default)
            => _innerStore.EnumerateCRLsAsync(ct);

        /// <inheritdoc/>
        public Task<X509CRLCollection> EnumerateCRLsAsync(X509Certificate2 issuer,
            bool validateUpdateTime = true, CancellationToken ct = default)
            => _innerStore.EnumerateCRLsAsync(issuer, validateUpdateTime, ct);

        /// <inheritdoc/>
        public async Task<X509Certificate2Collection> FindByThumbprintAsync(
            string thumbprint, CancellationToken ct = default)
        {
            var certificatesCollection =
                await _innerStore.FindByThumbprintAsync(thumbprint, ct).ConfigureAwait(false);

            if (!_innerStore.Directory.Exists)
            {
                return certificatesCollection;
            }

            foreach (var file in _innerStore.Directory.GetFiles('*' + CrtExtension))
            {
                try
                {
                    var certificates = new X509Certificate2Collection();
                    certificates.ImportFromPemFile(file.FullName);
                    foreach (var certificate in certificates)
                    {
                        if (string.Equals(certificate.Thumbprint, thumbprint,
                            StringComparison.OrdinalIgnoreCase))
                        {
                            Utils.LogInfo("Find by thumbprint: {thumbprint} - found", thumbprint);
                            certificatesCollection.Add(certificate);
                        }
                    }
                }
                catch (Exception e)
                {
                    Utils.LogError(e, "Could not load certificate from file: {fileName}",
                        file.FullName);
                }
            }

            return certificatesCollection;
        }

        /// <inheritdoc/>
        public Task<StatusCode> IsRevokedAsync(X509Certificate2 issuer, X509Certificate2 certificate,
            CancellationToken ct = default)
            => _innerStore.IsRevokedAsync(issuer, certificate, ct);

        /// <inheritdoc/>
        public Task<X509Certificate2> LoadPrivateKeyAsync(string thumbprint, string subjectName,
            string password, CancellationToken ct = default)
            => LoadPrivateKeyAsync(thumbprint, subjectName, applicationUri: null, certificateType: null, password, ct);

        /// <inheritdoc/>
        public async Task<X509Certificate2> LoadPrivateKeyAsync(string thumbprint, string subjectName,
            string? applicationUri, NodeId? certificateType, string password, CancellationToken ct = default)
        {
            if (!_innerStore.Directory.Exists)
            {
                return await _innerStore.LoadPrivateKeyAsync(thumbprint, subjectName, applicationUri,
                    certificateType, password, ct).ConfigureAwait(false);
            }

            foreach (var file in _innerStore.Directory.GetFiles('*' + CrtExtension))
            {
                try
                {
                    var keyFile = new FileInfo(file.FullName.Replace(CrtExtension, KeyExtension,
                        StringComparison.OrdinalIgnoreCase));
                    if (keyFile.Exists)
                    {
                        using var certificate = X509CertificateLoader.LoadCertificateFromFile(
                            file.FullName);
                        if (!MatchCertificate(certificate, thumbprint, subjectName,
                            applicationUri, certificateType))
                        {
                            continue;
                        }

                        var privateKeyCertificate = X509Certificate2.CreateFromPemFile(
                            file.FullName, keyFile.FullName);

                        Utils.LogInfo("Loading private key succeeded for {thumbprint} - {subjectName}",
                            thumbprint, subjectName);
                        return privateKeyCertificate;
                    }
                }
                catch (Exception e)
                {
                    Utils.LogError(e,
                        "Could not load private key for certificate file: {fileName}", file.FullName);
                }
            }

            return await _innerStore.LoadPrivateKeyAsync(thumbprint, subjectName, applicationUri,
                certificateType, password, ct).ConfigureAwait(false);
        }

        private static bool MatchCertificate(X509Certificate2 certificate, string thumbprint,
            string subjectName, string? applicationUri, NodeId? certificateType)
        {
            if (certificateType == null ||
                certificateType == ObjectTypeIds.RsaSha256ApplicationCertificateType ||
                certificateType == ObjectTypeIds.RsaMinApplicationCertificateType ||
                certificateType == ObjectTypeIds.ApplicationCertificateType)
            {
                if (!string.IsNullOrEmpty(thumbprint) &&
                    !string.Equals(certificate.Thumbprint, thumbprint, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                if (!string.IsNullOrEmpty(subjectName) &&
                    !X509Utils.CompareDistinguishedName(subjectName, certificate.Subject) &&
                    (
                        subjectName.Contains('=', StringComparison.OrdinalIgnoreCase) ||
                        !X509Utils.ParseDistinguishedName(certificate.Subject)
                            .Any(s => s.Equals("CN=" + subjectName, StringComparison.Ordinal))))
                {
                    return false;
                }

                // skip if not RSA certificate
                return X509Utils.GetRSAPublicKeySize(certificate) >= 0;
            }
            return false;
        }
    }
}
