// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Deployment.Infrastructure {

    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Azure.KeyVault;
    using Microsoft.Azure.KeyVault.Models;
    using Microsoft.Azure.Management.KeyVault.Fluent.Models;
    using Serilog;

    class IIoTKeyVaultClient : IDisposable {

        public const string AKS_CLUSTER_CERT_NAME = "aksClusterCert";
        public const string DATAPROTECTION_KEY_NAME = "dataprotection";

        private readonly KeyVaultClient _keyVaultClient;
        private readonly VaultInner _keyVault;

        /// <summary>
        /// Constructor of IIoT-specific KeyVault client.
        /// </summary>
        /// <param name="authenticationCallback"></param>
        /// <param name="keyVault"></param>
        public IIoTKeyVaultClient(
            AuthenticationCallback authenticationCallback,
            VaultInner keyVault
        ) {
            if (authenticationCallback is null) {
                throw new ArgumentNullException(nameof(authenticationCallback));
            }
            if (keyVault is null) {
                throw new ArgumentNullException(nameof(keyVault));
            }

            var kvAuthenticationCallback = new KeyVaultClient.AuthenticationCallback(
                async (authority, resource, scope) => {
                    return await authenticationCallback(authority, resource, scope);
                }
            );

            _keyVaultClient = new KeyVaultClient(kvAuthenticationCallback);
            _keyVault = keyVault;
        }

        /// <summary>
        /// Wait for certificate to be created.
        /// </summary>
        /// <param name="certificateName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task WaitForCertificateCreationAsync(
            string certificateName,
            CancellationToken cancellationToken = default
        ) {
            if (string.IsNullOrEmpty(certificateName)) {
                throw new ArgumentNullException(nameof(certificateName));
            }

            var certificateOperation = await _keyVaultClient
                .GetCertificateOperationAsync(
                    _keyVault.Properties.VaultUri,
                    certificateName,
                    cancellationToken
                );

            while (certificateOperation.Status.ToLower().Equals("inprogress")) {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(1000, cancellationToken);

                certificateOperation = await _keyVaultClient
                    .GetCertificateOperationAsync(
                        _keyVault.Properties.VaultUri,
                        certificateName,
                        cancellationToken
                    );
            }
        }

        public async Task<X509Certificate2> GetCertificateAsync(
            string certificateName,
            CancellationToken cancellationToken = default
        ) {
            if (string.IsNullOrEmpty(certificateName)) {
                throw new ArgumentNullException(nameof(certificateName));
            }

            // We first have to wait for certificate to be created.
            await WaitForCertificateCreationAsync(certificateName, cancellationToken);

            var certificateBundle = await _keyVaultClient
                .GetCertificateAsync(
                    _keyVault.Properties.VaultUri,
                    certificateName,
                    cancellationToken
                );

            var x509Certificate2 = new X509Certificate2(certificateBundle.Cer);

            return x509Certificate2;
        }

        public async Task<X509Certificate2> GetSecretAsync(
            string certificateName,
            CancellationToken cancellationToken = default
        ) {
            if (string.IsNullOrEmpty(certificateName)) {
                throw new ArgumentNullException(nameof(certificateName));
            }

            // We first have to wait for certificate to be created.
            await WaitForCertificateCreationAsync(certificateName, cancellationToken);

            var secretBundle = await _keyVaultClient
                .GetSecretAsync(
                    _keyVault.Properties.VaultUri,
                    certificateName,
                    cancellationToken
                );

            if (secretBundle.ContentType == CertificateContentType.Pfx) {
                var certPFX = Convert.FromBase64String(secretBundle.Value);

                // Note: X509KeyStorageFlags.Exportable flag is
                // necessary if we intend to export private key.
                var x509Certificate2 = new X509Certificate2(
                    certPFX,
                    (string) null,
                    X509KeyStorageFlags.Exportable
                );

                return x509Certificate2;
            } else {
                throw new NotImplementedException("Exporting PEM certificates is not supported.");
            }
        }

        public async Task<CertificateOperation> CreateCertificateAsync(
            string certificateName,
            string certificateCN,
            IDictionary<string, string> tags = null,
            CancellationToken cancellationToken = default
        ) {
            if (string.IsNullOrEmpty(certificateName)) {
                throw new ArgumentNullException(nameof(certificateName));
            }
            if (string.IsNullOrEmpty(certificateCN)) {
                throw new ArgumentNullException(nameof(certificateCN));
            }

            try {
                tags ??= new Dictionary<string, string>();

                Log.Information($"Adding certificate to Azure KeyVault: {certificateName} ...");

                // ToDo: Add support of PEM certificate creation: CertificateContentType.Pem
                var contentType = CertificateContentType.Pfx;

                var certificatePolicy = new CertificatePolicy {
                    KeyProperties = new KeyProperties {
                        Exportable = true,
                        KeyType = "RSA",
                        KeySize = 2048,
                        ReuseKey = false
                    },
                    SecretProperties = new SecretProperties {
                        ContentType = contentType
                    },
                    X509CertificateProperties = new X509CertificateProperties {
                        Subject = $"CN={certificateCN}",
                        SubjectAlternativeNames = new SubjectAlternativeNames {
                            DnsNames = new string[] { certificateCN }
                        }
                    },
                    IssuerParameters = new IssuerParameters {
                        Name = "Self"
                    }

                };

                certificatePolicy.Validate();

                var certificateAttributes = new CertificateAttributes();

                var certificateOperation = await _keyVaultClient
                    .CreateCertificateAsync(
                        _keyVault.Properties.VaultUri,
                        certificateName,
                        certificatePolicy,
                        certificateAttributes,
                        tags,
                        cancellationToken
                    );

                Log.Information($"Added certificate to Azure KeyVault: {certificateName}");

                return certificateOperation;
            }
            catch (Exception ex) {
                Log.Error(ex, $"Failed to add certificate to Azure KeyVault: {certificateName}");
                throw;
            }
        }

        /// <summary>
        /// Retrieves the public portion of a key plus its attributes.
        /// </summary>
        /// <param name="keyName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<KeyBundle> GetKeyAsync(
            string keyName,
            CancellationToken cancellationToken = default
        ) {
            if (string.IsNullOrEmpty(keyName)) {
                throw new ArgumentNullException(nameof(keyName));
            }

            var keyBundle = await _keyVaultClient
                .GetKeyAsync(
                    _keyVault.Properties.VaultUri,
                    keyName,
                    cancellationToken
                );

            return keyBundle;
        }

        /// <summary>
        /// Creates a new key, stores it, then returns key parameters and attributes.
        /// </summary>
        /// <param name="keyName"></param>
        /// <param name="keyParameters"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<KeyBundle> CreateKeyAsync(
            string keyName,
            NewKeyParameters keyParameters,
            CancellationToken cancellationToken = default
        ) {
            if (string.IsNullOrEmpty(keyName)) {
                throw new ArgumentNullException(nameof(keyName));
            }

            var keyBundle = await _keyVaultClient
                .CreateKeyAsync(
                    _keyVault.Properties.VaultUri,
                    keyName,
                    keyParameters,
                    cancellationToken
                );

            return keyBundle;
        }

        /// <summary>
        /// Creates a new key for dataprotection and returns key parameters and attributes.
        /// </summary>
        /// <param name="keyName"></param>
        /// <param name="tags"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<KeyBundle> CreateDataprotectionKeyAsync(
            string keyName,
            IDictionary<string, string> tags = null,
            CancellationToken cancellationToken = default
        ) {
            if (string.IsNullOrEmpty(keyName)) {
                throw new ArgumentNullException(nameof(keyName));
            }

            tags ??= new Dictionary<string, string>();
            var keyParameters = new NewKeyParameters {
                KeySize = 2048,
                Kty = KeyVault.WebKey.JsonWebKeyType.Rsa,
                KeyOps = new List<string> {
                    KeyVault.WebKey.JsonWebKeyOperation.Wrap,
                    KeyVault.WebKey.JsonWebKeyOperation.Unwrap
                },
                Tags = tags
            };

            var keyBundle = await CreateKeyAsync(
                keyName,
                keyParameters,
                cancellationToken
            );

            return keyBundle;
        }

        /// <summary>
        /// Create secret with "application/json" content type.
        /// </summary>
        /// <param name="secretName"></param>
        /// <param name="secretValue"></param>
        /// <param name="tags"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<SecretBundle> CreateSecretAsync(
            string secretName,
            string secretValue,
            IDictionary<string, string> tags = null,
            CancellationToken cancellationToken = default
        ) {
            if (string.IsNullOrEmpty(secretName)) {
                throw new ArgumentNullException(nameof(secretName));
            }
            if (string.IsNullOrEmpty(secretValue)) {
                throw new ArgumentNullException(nameof(secretValue));
            }

            tags ??= new Dictionary<string, string>();

            var secret = await _keyVaultClient
                .SetSecretAsync(
                    _keyVault.Properties.VaultUri,
                    secretName,
                    secretValue,
                    tags,
                    "application/json",
                    cancellationToken: cancellationToken
                );

            return secret;
        }

        public void Dispose() {
            if (null != _keyVaultClient) {
                _keyVaultClient.Dispose();
            }
        }

        public delegate Task<string> AuthenticationCallback(string authority, string resource, string scope);
    }
}
