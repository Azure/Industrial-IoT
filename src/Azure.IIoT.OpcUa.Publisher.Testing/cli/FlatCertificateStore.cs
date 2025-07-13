// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Testing.Cli
{
    using Opc.Ua;
    using Opc.Ua.Security.Certificates;
    using System;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography.X509Certificates;
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
        /// Prefix for flat directory certificate store.
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
            public Task Add(X509Certificate2 certificate, string password = null)
                => _innerStore.Add(certificate, password);

            /// <inheritdoc/>
            public Task AddRejected(X509Certificate2Collection certificates, int maxCertificates)
                => _innerStore.AddRejected(certificates, maxCertificates);

            /// <inheritdoc/>
            public Task<bool> Delete(string thumbprint) => _innerStore.Delete(thumbprint);

            /// <inheritdoc/>
            public async Task<X509Certificate2Collection> Enumerate()
            {
                var certificatesCollection =
                    await _innerStore.Enumerate().ConfigureAwait(false);
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
            public Task AddCRL(X509CRL crl) => _innerStore.AddCRL(crl);

            /// <inheritdoc/>
            public Task<bool> DeleteCRL(X509CRL crl) => _innerStore.DeleteCRL(crl);

            /// <inheritdoc/>
            public Task<X509CRLCollection> EnumerateCRLs() => _innerStore.EnumerateCRLs();

            /// <inheritdoc/>
            public Task<X509CRLCollection> EnumerateCRLs(X509Certificate2 issuer,
                bool validateUpdateTime = true)
                => _innerStore.EnumerateCRLs(issuer, validateUpdateTime);

            /// <inheritdoc/>
            public async Task<X509Certificate2Collection> FindByThumbprint(string thumbprint)
            {
                var certificatesCollection =
                    await _innerStore.FindByThumbprint(thumbprint).ConfigureAwait(false);

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
            public Task<StatusCode> IsRevoked(X509Certificate2 issuer, X509Certificate2 certificate)
                => _innerStore.IsRevoked(issuer, certificate);

            /// <inheritdoc/>
            public Task<X509Certificate2> LoadPrivateKey(string thumbprint, string subjectName,
                string password) => LoadPrivateKey(thumbprint, subjectName, applicationUri: null,
                    certificateType: null, password);

            /// <inheritdoc/>
            public async Task<X509Certificate2> LoadPrivateKey(string thumbprint, string subjectName,
                string applicationUri, NodeId certificateType, string password)
            {
                if (!_innerStore.Directory.Exists)
                {
                    return await _innerStore.LoadPrivateKey(thumbprint, subjectName, applicationUri,
                        certificateType, password).ConfigureAwait(false);
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

                return await _innerStore.LoadPrivateKey(thumbprint, subjectName, applicationUri,
                    certificateType, password).ConfigureAwait(false);
            }

            private bool MatchCertificate(X509Certificate2 certificate, string thumbprint,
                string subjectName, string applicationUri, NodeId certificateType)
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
}
