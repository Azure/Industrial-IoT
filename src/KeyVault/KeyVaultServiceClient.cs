// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


using Microsoft.Azure.IIoT.Diagnostics;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Azure.KeyVault.WebKey;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Opc.Ua;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.IIoT.OpcUa.Services.GdsVault.KeyVault
{

    public class KeyVaultServiceClient
    {
        const int MaxResults = 10;
        // see RFC 2585
        const string ContentTypeCert = "application/pkix-cert";
        const string ContentTypeCrl = "application/pkix-crl";
        // see CertificateContentType.Pfx and 
        const string ContentTypePfx = "application/x-pkcs12";
        // see CertificateContentType.Pem
        const string ContentTypePem = "application/x-pem-file";
        
        // trust list tags
        const string TagIssuerList = "Issuer";
        const string TagTrustedList = "Trusted";

        /// <summary>
        /// 
        /// </summary>
        /// <param name="vaultBaseUrl">The Url of the Key Vault.</param>
        public KeyVaultServiceClient(string vaultBaseUrl, ILogger logger)
        {
            _vaultBaseUrl = vaultBaseUrl;
            _logger = logger;
        }

        /// <summary>
        /// Set appID and client certificate for keyVault authentication.
        /// </summary>
        /// <param name="appId"></param>
        /// <param name="clientAssertionCertPfx"></param>
        public void SetAuthenticationAssertionCertificate(
            string appId,
            X509Certificate2 clientAssertionCertPfx)
        {
            _assertionCert = new ClientAssertionCertificate(appId, clientAssertionCertPfx);
            _keyVaultClient = new KeyVaultClient(
                new KeyVaultClient.AuthenticationCallback(GetAccessTokenAsync));
        }

        /// <summary>
        /// Authentication for MSI or dev user callback.
        /// </summary>
        public void SetAuthenticationTokenProvider()
        {
            AzureServiceTokenProvider azureServiceTokenProvider = new AzureServiceTokenProvider();
            _keyVaultClient = new KeyVaultClient(
                new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));
        }

        /// <summary>
        /// Private callback for keyvault authentication.
        /// </summary>
        private async Task<string> GetAccessTokenAsync(string authority, string resource, string scope)
        {
            var context = new AuthenticationContext(authority, TokenCache.DefaultShared);
            var result = await context.AcquireTokenAsync(resource, _assertionCert);
            return result.AccessToken;
        }

        /// <summary>
        /// Read the GdsVault CertificateConfigurationGroups as Json.
        /// </summary>
        public async Task<string> GetCertificateConfigurationGroupsAsync(CancellationToken ct = default(CancellationToken))
        {
            SecretBundle secret = await _keyVaultClient.GetSecretAsync(_vaultBaseUrl + "/secrets/groups", ct).ConfigureAwait(false);
            return secret.Value;
        }

        /// <summary>
        /// Get Certificate bundle from key Vault.
        /// </summary>
        /// <param name="name">Key Vault name</param>
        /// <param name="ct">CancellationToken</param>
        /// <returns></returns>
        internal async Task<CertificateBundle> GetCertificateAsync(string name, CancellationToken ct = default(CancellationToken))
        {
            return await _keyVaultClient.GetCertificateAsync(_vaultBaseUrl, name, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Read all certificate versions of a CA certificate group.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<X509Certificate2Collection> GetCertificateVersionsAsync(string id, CancellationToken ct = default(CancellationToken))
        {
            var certificates = new X509Certificate2Collection();
            try
            {
                var certItems = await _keyVaultClient.GetCertificateVersionsAsync(_vaultBaseUrl, id, MaxResults, ct).ConfigureAwait(false);
                while (certItems != null)
                {
                    foreach (var certItem in certItems)
                    {
                        var certBundle = await _keyVaultClient.GetCertificateAsync(certItem.Id, ct).ConfigureAwait(false);
                        var cert = new X509Certificate2(certBundle.Cer);
                        certificates.Add(cert);
                    }
                    if (certItems.NextPageLink != null)
                    {
                        certItems = await _keyVaultClient.GetCertificateVersionsNextAsync(certItems.NextPageLink, ct).ConfigureAwait(false);
                    }
                    else
                    {
                        certItems = null;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Error while loading the certificate versions for " + id + ".", () => new { ex });
            }
            return certificates;
        }

        /// <summary>
        /// Load the signing CA certificate for signing operations.
        /// </summary>
        internal Task<X509Certificate2> LoadSigningCertificateAsync(string signingCertificateKey, X509Certificate2 publicCert, CancellationToken ct = default(CancellationToken))
        {
#if LOADPRIVATEKEY
            var secret = await _keyVaultClient.GetSecretAsync(signingCertificateKey, ct);
            if (secret.ContentType == CertificateContentType.Pfx)
            {
                var certBlob = Convert.FromBase64String(secret.Value);
                return CertificateFactory.CreateCertificateFromPKCS12(certBlob, string.Empty);
            }
            else if (secret.ContentType == CertificateContentType.Pem)
            {
                Encoding encoder = Encoding.UTF8;
                var privateKey = encoder.GetBytes(secret.Value.ToCharArray());
                return CertificateFactory.CreateCertificateWithPEMPrivateKey(publicCert, privateKey, string.Empty);
            }
            throw new NotImplementedException("Unknown content type: " + secret.ContentType);
#else
            _logger.Error("Error in LoadSigningCertificateAsync " + signingCertificateKey + "." +
                "Loading the private key is not permitted.", () => new { signingCertificateKey });
            throw new NotSupportedException("Loading the private key from key Vault is not permitted.");
#endif
        }

        /// <summary>
        /// Sign a digest with the signing key.
        /// </summary>
        public async Task<byte[]> SignDigestAsync(
            string signingKey,
            byte[] digest,
            HashAlgorithmName hashAlgorithm,
            RSASignaturePadding padding,
            CancellationToken ct = default(CancellationToken))
        {
            string algorithm;

            if (padding == RSASignaturePadding.Pkcs1)
            {
                if (hashAlgorithm == HashAlgorithmName.SHA256)
                {
                    algorithm = JsonWebKeySignatureAlgorithm.RS256;
                }
                else if (hashAlgorithm == HashAlgorithmName.SHA384)
                {
                    algorithm = JsonWebKeySignatureAlgorithm.RS384;
                }
                else if (hashAlgorithm == HashAlgorithmName.SHA512)
                {
                    algorithm = JsonWebKeySignatureAlgorithm.RS512;
                }
                else
                {
                    _logger.Error("Error in SignDigestAsync " + signingKey + "." +
                        "Unsupported hash algorithm used.", () => new { signingKey });
                    throw new ArgumentOutOfRangeException(nameof(hashAlgorithm));
                }
            }
#if FUTURE
            else if (padding == RSASignaturePadding.Pss)
            {
                if (hashAlgorithm == HashAlgorithmName.SHA256)
                {
                    algorithm = JsonWebKeySignatureAlgorithm.PS256;
                }
                else if (hashAlgorithm == HashAlgorithmName.SHA384)
                {
                    algorithm = JsonWebKeySignatureAlgorithm.PS384;
                }
                else if (hashAlgorithm == HashAlgorithmName.SHA512)
                {
                    algorithm = JsonWebKeySignatureAlgorithm.PS512;
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(hashAlgorithm));
                }
            }
#endif
            else
            {
                _logger.Error("Error in SignDigestAsync " + padding + "." +
                    "Unsupported padding algorithm used.", () => new { padding });
                throw new ArgumentOutOfRangeException(nameof(padding));
            }

            var result = await _keyVaultClient.SignAsync(signingKey, algorithm, digest, ct);
            return result.Result;
        }

        /// <summary>
        /// Imports a new CA certificate in group id, tags it for trusted or issuer store.
        /// </summary>
        public async Task ImportCACertificate(string id, X509Certificate2Collection certificates, bool trusted, CancellationToken ct = default(CancellationToken))
        {
            X509Certificate2 certificate = certificates[0];
            CertificateAttributes attributes = new CertificateAttributes
            {
                Enabled = true,
                Expires = certificate.NotAfter,
                NotBefore = certificate.NotBefore,
            };

            var policy = new CertificatePolicy
            {
                IssuerParameters = new IssuerParameters
                {
                    Name = "Self"
                },
                KeyProperties = new KeyProperties
                {
                    Exportable = false,
                    KeySize = certificate.GetRSAPublicKey().KeySize,
                    KeyType = "RSA-HSM",
                    ReuseKey = false
                },
                SecretProperties = new SecretProperties
                {
                    ContentType = CertificateContentType.Pfx
                },
                X509CertificateProperties = new X509CertificateProperties
                {
                    Subject = certificate.Subject
                }
            };

            Dictionary<string, string> tags = new Dictionary<string, string>
            {
                [id] = trusted ? TagTrustedList : TagIssuerList
            };

            var result = await _keyVaultClient.ImportCertificateAsync(
                _vaultBaseUrl,
                id,
                certificates,
                policy,
                attributes,
                tags,
                ct)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Imports a new CA certificate in group id, tags it for trusted or issuer store.
        /// </summary>
        public async Task CreateCACertificateAsync(string id, X509Certificate2 certificate, bool trusted, CancellationToken ct = default(CancellationToken))
        {
            CertificateAttributes attributes = new CertificateAttributes
            {
                Enabled = true,
                Expires = certificate.NotAfter,
                NotBefore = certificate.NotBefore,
            };

            var policy = new CertificatePolicy
            {
                IssuerParameters = new IssuerParameters
                {
                    Name = "Self",
                },
                KeyProperties = new KeyProperties
                {
                    Exportable = false,
                    KeySize = certificate.GetRSAPublicKey().KeySize,
                    KeyType = "RSAHSM",
                    ReuseKey = false
                },
                SecretProperties = new SecretProperties
                {
                    ContentType = CertificateContentType.Pfx
                },
                X509CertificateProperties = new X509CertificateProperties
                {
                    Subject = certificate.Subject
                },
                Attributes = new CertificateAttributes()
            };

            Dictionary<string, string> tags = new Dictionary<string, string>
            {
                [id] = trusted ? TagTrustedList : TagIssuerList
            };

            var result2 = await _keyVaultClient.CreateCertificateAsync(
                _vaultBaseUrl,
                id,
                policy,
                attributes,
                tags,
                ct)
                .ConfigureAwait(false);

            var result = await _keyVaultClient.ImportCertificateAsync(
                _vaultBaseUrl,
                id,
                new X509Certificate2Collection(certificate),
                policy,
                attributes,
                tags,
                ct)
                .ConfigureAwait(false);
        }


        /// <summary>
        /// Imports a new CRL for group id.
        /// </summary>
        public async Task ImportCACrl(string id, X509Certificate2 certificate, Opc.Ua.X509CRL crl, CancellationToken ct = default(CancellationToken))
        {
            try
            {
                string secretIdentifier = CrlSecretName(id, certificate);
                SecretAttributes secretAttributes = new SecretAttributes()
                {
                    Enabled = true,
                    Expires = crl.NextUpdateTime,
                    NotBefore = crl.UpdateTime
                };

                // do not set tag for a CRL, the CA cert is already tagged.

                var result = await _keyVaultClient.SetSecretAsync(
                    _vaultBaseUrl,
                    secretIdentifier,
                    Convert.ToBase64String(crl.RawData),
                    null,
                    ContentTypeCrl,
                    secretAttributes,
                    ct)
                    .ConfigureAwait(false);
            }
            catch (Exception)
            {
                // TODO: add logging (is this a fatal error?)
            }
        }

        /// <summary>
        /// Load CRL for CA cert in group.
        /// </summary>
        public async Task<Opc.Ua.X509CRL> LoadCACrl(string id, X509Certificate2 certificate, CancellationToken ct = default(CancellationToken))
        {
            string secretIdentifier = CrlSecretName(id, certificate);
            var secret = await _keyVaultClient.GetSecretAsync(_vaultBaseUrl, secretIdentifier, ct).ConfigureAwait(false);
            if (secret.ContentType == ContentTypeCrl)
            {
                var crlBlob = Convert.FromBase64String(secret.Value);
                return new Opc.Ua.X509CRL(crlBlob);
            }
            return null;
        }

        /// <summary>
        /// Creates a trust list with all certs and crls in issuer and trusted list.
        /// i) First load all certs and crls tagged with id==Issuer or id==Trusted.
        /// ii) Then walk all CA cert versions and load all certs tagged with id==Issuer or id==Trusted. 
        ///     Crl is loaded too if CA cert is tagged.
        /// </summary>
        public async Task<Models.KeyVaultTrustListModel> GetTrustListAsync(string id, CancellationToken ct = default(CancellationToken))
        {
            var trustList = new Models.KeyVaultTrustListModel(id);
            var secretItems = await _keyVaultClient.GetSecretsAsync(_vaultBaseUrl, MaxResults, ct).ConfigureAwait(false);

            while (secretItems != null)
            {
                foreach (var secretItem in secretItems.Where(s => s.Tags != null))
                {
                    string tag = secretItem.Tags.FirstOrDefault(x => String.Equals(x.Key, id, StringComparison.OrdinalIgnoreCase)).Value;
                    bool issuer = tag == TagIssuerList;
                    bool trusted = tag == TagTrustedList;
                    bool certType = secretItem.ContentType == ContentTypeCert;
                    bool crlType = secretItem.ContentType == ContentTypeCrl;
                    if (issuer || trusted && (certType || crlType))
                    {
                        X509CRL crl = null;
                        X509Certificate2 cert = null;
                        if (certType)
                        {
                            var certCollection = issuer ? trustList.IssuerCertificates : trustList.TrustedCertificates;
                            cert = await LoadCertSecret(secretItem.Identifier.Name, ct).ConfigureAwait(false);
                            certCollection.Add(cert);
                        }
                        else
                        {
                            var crlCollection = issuer ? trustList.IssuerCrls : trustList.TrustedCrls;
                            crl = await LoadCrlSecret(secretItem.Identifier.Name, ct).ConfigureAwait(false);
                            crlCollection.Add(crl);
                        }
                    }
                }

                if (secretItems.NextPageLink != null)
                {
                    secretItems = await _keyVaultClient.GetSecretsNextAsync(secretItems.NextPageLink, ct).ConfigureAwait(false);
                }
                else
                {
                    secretItems = null;
                }
            }

            var certItems = await _keyVaultClient.GetCertificateVersionsAsync(_vaultBaseUrl, id, MaxResults, ct).ConfigureAwait(false);
            while (certItems != null)
            {
                foreach (var certItem in certItems.Where(c => c.Tags != null))
                {
                    string tag = certItem.Tags.FirstOrDefault(x => String.Equals(x.Key, id, StringComparison.OrdinalIgnoreCase)).Value;
                    bool issuer = tag == TagIssuerList;
                    bool trusted = tag == TagTrustedList;

                    if (issuer || trusted)
                    {
                        var certBundle = await _keyVaultClient.GetCertificateAsync(certItem.Id, ct).ConfigureAwait(false);
                        var cert = new X509Certificate2(certBundle.Cer);
                        var crl = await LoadCACrl(id, cert, ct);
                        if (issuer)
                        {
                            trustList.IssuerCertificates.Add(cert);
                            trustList.IssuerCrls.Add(crl);
                        }
                        else
                        {
                            trustList.TrustedCertificates.Add(cert);
                            trustList.TrustedCrls.Add(crl);
                        }
                    }
                }
                if (certItems.NextPageLink != null)
                {
                    certItems = await _keyVaultClient.GetCertificateVersionsNextAsync(certItems.NextPageLink, ct).ConfigureAwait(false);
                }
                else
                {
                    certItems = null;
                }
            }

            return trustList;
        }

        private string CrlSecretName(string name, X509Certificate2 certificate)
        {
            return name + "Crl" + certificate.Thumbprint;
        }

        private async Task<X509CRL> LoadCrlSecret(string secretIdentifier, CancellationToken ct = default(CancellationToken))
        {
            var secret = await _keyVaultClient.GetSecretAsync(_vaultBaseUrl, secretIdentifier, ct).ConfigureAwait(false); ;
            if (secret.ContentType == ContentTypeCrl)
            {
                var crlBlob = Convert.FromBase64String(secret.Value);
                return new Opc.Ua.X509CRL(crlBlob);
            }
            return null;
        }

        private async Task<X509Certificate2> LoadCertSecret(string secretIdentifier, CancellationToken ct = default(CancellationToken))
        {
            var secret = await _keyVaultClient.GetSecretAsync(_vaultBaseUrl, secretIdentifier, ct).ConfigureAwait(false); ;
            if (secret.ContentType == ContentTypeCrl)
            {
                var certBlob = Convert.FromBase64String(secret.Value);
                return new X509Certificate2(certBlob);
            }
            return null;
        }

        private string _vaultBaseUrl;
        private IKeyVaultClient _keyVaultClient;
        private ILogger _logger;
        private ClientAssertionCertificate _assertionCert;
    }
}

