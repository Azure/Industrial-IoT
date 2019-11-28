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

        public const string WEB_APP_CERT_NAME = "webAppCert";
        public const string AKS_CLUSTER_CERT_NAME = "aksClusterCert";

        private readonly KeyVaultClient _keyVaultClient;
        private readonly VaultInner _keyVault;

        public IIoTKeyVaultClient(
            AuthenticationCallback authenticationCallback,
            VaultInner keyVault
        ) {
            var kvAuthenticationCallback = new KeyVaultClient.AuthenticationCallback(
                async (authority, resource, scope) => {
                    return await authenticationCallback(authority, resource, scope);
                }
            );

            _keyVaultClient = new KeyVaultClient(kvAuthenticationCallback);
            _keyVault = keyVault;
        }

        private async Task WaitForCertificateCreationAsync(
            string certificateName,
            CancellationToken cancellationToken = default
        ) {
            var webAppCertificateOperation = await _keyVaultClient
                .GetCertificateOperationAsync(
                    _keyVault.Properties.VaultUri,
                    certificateName,
                    cancellationToken
                );

            while (webAppCertificateOperation.Status.ToLower().Equals("inprogress")) {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(1000, cancellationToken);

                webAppCertificateOperation = await _keyVaultClient
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

        public void Dispose() {
            if (null != _keyVaultClient) {
                _keyVaultClient.Dispose();
            }
        }

        public delegate Task<string> AuthenticationCallback(string authority, string resource, string scope);
    }
}
