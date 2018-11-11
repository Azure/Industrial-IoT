// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.IIoT.Diagnostics;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Azure.KeyVault.WebKey;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;
using Opc.Ua;
using static Microsoft.Azure.IIoT.OpcUa.Services.Vault.KeyVault.KeyVaultCertFactory;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.KeyVault
{
    public struct CertificateKeyInfo
    {
        public X509Certificate2 Certificate { get; set; }
        public string KeyIdentifier { get; set; }
    }
    /// <summary>
    /// 
    /// </summary>
    public class KeyVaultServiceClient
    {
        const int MaxResults = 5;
        const string ContentTypeJson = "application/json";
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

        const string GroupSecret = "groups";

        /// <summary>
        /// 
        /// </summary>
        /// <param name="vaultBaseUrl">The Url of the Key Vault.</param>
        /// <param name="keyStoreHSM">The KeyVault is HSM backed.</param>
        /// <param name="logger">The logger.</param>
        public KeyVaultServiceClient(string vaultBaseUrl, bool keyStoreHSM, ILogger logger)
        {
            _vaultBaseUrl = vaultBaseUrl;
            _keyStoreHSM = keyStoreHSM;
            _logger = logger;
        }

        /// <summary>
        /// Set appID and app secret for keyVault authentication.
        /// </summary>
        /// <param name="appId"></param>
        /// <param name="appSecret"></param>
        public void SetAuthenticationClientCredential(
            string appId,
            string appSecret)
        {
            _assertionCert = null;
            _clientCredential = new ClientCredential(appId, appSecret);
            _keyVaultClient = new KeyVaultClient(
                new KeyVaultClient.AuthenticationCallback(GetAccessTokenAsync));
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
            _clientCredential = null;
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
        /// Service client credentials.
        /// </summary>
        public void SetServiceClientCredentials(ServiceClientCredentials credentials)
        {
            _keyVaultClient = new KeyVaultClient(credentials);
        }

        /// <summary>
        /// Private callback for keyvault authentication.
        /// </summary>
        private async Task<string> GetAccessTokenAsync(string authority, string resource, string scope)
        {
            var context = new AuthenticationContext(authority, TokenCache.DefaultShared);
            AuthenticationResult result;
            if (_clientCredential != null)
            {
                result = await context.AcquireTokenAsync(resource, _clientCredential);
            }
            else
            {
                result = await context.AcquireTokenAsync(resource, _assertionCert);
            }
            return result.AccessToken;
        }

        /// <summary>
        /// Read the OpcVault CertificateConfigurationGroups as Json.
        /// </summary>
        public async Task<string> GetCertificateConfigurationGroupsAsync(CancellationToken ct = default(CancellationToken))
        {
            SecretBundle secret = await _keyVaultClient.GetSecretAsync(_vaultBaseUrl, GroupSecret, ct).ConfigureAwait(false);
            return secret.Value;
        }

        /// <summary>
        /// Write the OpcVault CertificateConfigurationGroups as Json.
        /// </summary>
        public async Task<string> PutCertificateConfigurationGroupsAsync(string json, CancellationToken ct = default(CancellationToken))
        {
            SecretBundle secret = await _keyVaultClient.SetSecretAsync(_vaultBaseUrl, GroupSecret, json, null, ContentTypeJson, null, ct).ConfigureAwait(false);
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
        /// Read all certificate versions of a CA certificate group.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<IList<CertificateKeyInfo>> GetCertificateVersionsKeyInfoAsync(string id, CancellationToken ct = default(CancellationToken))
        {
            var result = new List<CertificateKeyInfo>();
            try
            {
                var certItems = await _keyVaultClient.GetCertificateVersionsAsync(_vaultBaseUrl, id, MaxResults, ct).ConfigureAwait(false);
                while (certItems != null)
                {
                    foreach (var certItem in certItems)
                    {
                        var certBundle = await _keyVaultClient.GetCertificateAsync(certItem.Id, ct).ConfigureAwait(false);
                        var cert = new X509Certificate2(certBundle.Cer);
                        var certKeyInfo = new CertificateKeyInfo
                        {
                            Certificate = new X509Certificate2(certBundle.Cer),
                            KeyIdentifier = certBundle.KeyIdentifier.Identifier
                        };
                        result.Add(certKeyInfo);
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
            return result;
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

            var result = await _keyVaultClient.SignAsync(signingKey, algorithm, digest, ct).ConfigureAwait(false);
            return result.Result;
        }

        /// <summary>
        /// Imports a new CA certificate in group id, tags it for trusted or issuer store.
        /// </summary>
        public async Task ImportCACertificate(string id, X509Certificate2Collection certificates, bool trusted, CancellationToken ct = default(CancellationToken))
        {
            X509Certificate2 certificate = certificates[0];
            var attributes = CreateCertificateAttributes(certificate);
            var policy = CreateCertificatePolicy(certificate, true);
            var tags = CreateCertificateTags(id, trusted);
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
        /// Creates a new Root CA certificate in group id, tags it for trusted or issuer store.
        /// </summary>
        public async Task<X509Certificate2> CreateCACertificateAsync(
            string id,
            string subject,
            DateTime notBefore,
            DateTime notAfter,
            int keySize,
            int hashSize,
            bool trusted,
            CancellationToken ct = default)
        {
            try
            {
                // delete pending operations
                await _keyVaultClient.DeleteCertificateOperationAsync(_vaultBaseUrl, id);
            }
            catch
            {
                // intentionally ignore errors
            }

            try
            {
                // policy self signed, new key
                var policySelfSignedNewKey = CreateCertificatePolicy(subject, keySize, true, false);
                var tempAttributes = CreateCertificateAttributes(DateTime.UtcNow.AddMinutes(-10), DateTime.UtcNow.AddMinutes(10));
                var createKey = await _keyVaultClient.CreateCertificateAsync(
                    _vaultBaseUrl,
                    id,
                    policySelfSignedNewKey,
                    tempAttributes,
                    null,
                    ct)
                    .ConfigureAwait(false);
                CertificateOperation operation;
                do
                {
                    await Task.Delay(1000);
                    operation = await _keyVaultClient.GetCertificateOperationAsync(_vaultBaseUrl, id, ct);
                } while (operation.Status == "inProgress" && !ct.IsCancellationRequested);
                if (operation.Status != "completed")
                {
                    throw new ServiceResultException(StatusCodes.BadUnexpectedError, "Failed to create new key pair.");
                }
                var createdCertificateBundle = await _keyVaultClient.GetCertificateAsync(_vaultBaseUrl, id).ConfigureAwait(false);
                var caCertKeyIdentifier = createdCertificateBundle.KeyIdentifier.Identifier;
                var caTempCertIdentifier = createdCertificateBundle.CertificateIdentifier.Identifier;

                // policy unknown issuer, reuse key
                var policyUnknownReuse = CreateCertificatePolicy(subject, keySize, false, true);
                var attributes = CreateCertificateAttributes(notBefore, notAfter);
                var tags = CreateCertificateTags(id, trusted);

                // create the CSR
                var createResult = await _keyVaultClient.CreateCertificateAsync(
                    _vaultBaseUrl,
                    id,
                    policyUnknownReuse,
                    attributes,
                    tags,
                    ct)
                    .ConfigureAwait(false);

                if (createResult.Csr == null)
                {
                    throw new ServiceResultException(StatusCodes.BadInvalidArgument, "Failed to read CSR from CreateCertificate.");
                }

                // decode the CSR and verify consistency
                var pkcs10CertificationRequest = new Org.BouncyCastle.Pkcs.Pkcs10CertificationRequest(createResult.Csr);
                var info = pkcs10CertificationRequest.GetCertificationRequestInfo();
                if (createResult.Csr == null ||
                    pkcs10CertificationRequest == null ||
                    !pkcs10CertificationRequest.Verify())
                {
                    throw new ServiceResultException(StatusCodes.BadInvalidArgument, "Invalid CSR.");
                }

                // create the self signed root CA cert
                var asn1Decoder = new ASN1Decoder(info.SubjectPublicKeyInfo.GetDerEncoded());
                var publicKey = asn1Decoder.GetRSAPublicKey();
                var signedcert = await KeyVaultCertFactory.CreateSignedCertificate(
                    null,
                    null,
                    subject,
                    null,
                    (ushort)keySize,
                    notBefore,
                    notAfter,
                    (ushort)hashSize,
                    null,
                    publicKey,
                    new KeyVaultSignatureGenerator(this, caCertKeyIdentifier, null),
                    true);

                // merge Root CA cert with 
                var mergeResult = await _keyVaultClient.MergeCertificateAsync(
                    _vaultBaseUrl,
                    id,
                    new X509Certificate2Collection(signedcert)
                    );

                // disable the temp cert for self signing operation
                var attr = new CertificateAttributes()
                {
                    Enabled = false
                };
                await _keyVaultClient.UpdateCertificateAsync(caTempCertIdentifier, null, attr);

                return signedcert;
            }
            catch
            {
                throw new ServiceResultException(StatusCodes.BadInternalError, "Failed to create new Root CA certificate");
            }
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
        public async Task<Models.KeyVaultTrustListModel> GetTrustListAsync(string id, int? maxResults, string nextPageLink, CancellationToken ct = default)
        {
            var trustList = new Models.KeyVaultTrustListModel(id);
            if (maxResults == null)
            {
                maxResults = MaxResults;
            }

            Rest.Azure.IPage<SecretItem> secretItems = null;
            if (nextPageLink != null)
            {
                if (nextPageLink.Contains("/secrets"))
                {
                    secretItems = await _keyVaultClient.GetSecretsNextAsync(nextPageLink, ct).ConfigureAwait(false);
                }
            }
            else
            {
                secretItems = await _keyVaultClient.GetSecretsAsync(_vaultBaseUrl, maxResults, ct).ConfigureAwait(false);
            }

            int results = 0;
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
                        results++;
                    }
                }

                if (secretItems.NextPageLink != null)
                {
                    if (results >= maxResults)
                    {
                        trustList.NextPageLink = secretItems.NextPageLink;
                        return trustList;
                    }
                    else
                    {
                        secretItems = await _keyVaultClient.GetSecretsNextAsync(secretItems.NextPageLink, ct).ConfigureAwait(false);
                    }
                }
                else
                {
                    secretItems = null;
                }
            }

            Rest.Azure.IPage<CertificateItem> certItems = null;
            if (nextPageLink != null)
            {
                certItems = await _keyVaultClient.GetCertificateVersionsNextAsync(nextPageLink, ct).ConfigureAwait(false);
            }
            else
            {
                certItems = await _keyVaultClient.GetCertificateVersionsAsync(_vaultBaseUrl, id, maxResults, ct).ConfigureAwait(false);
            }

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
                        results++;
                    }
                }
                if (certItems.NextPageLink != null)
                {
                    if (results >= maxResults)
                    {
                        trustList.NextPageLink = certItems.NextPageLink;
                        return trustList;
                    }
                    else
                    {
                        certItems = await _keyVaultClient.GetCertificateVersionsNextAsync(certItems.NextPageLink, ct).ConfigureAwait(false);
                    }
                }
                else
                {
                    certItems = null;
                }
            }

            return trustList;
        }

        /// <summary>
        /// Purge all CRL and Certificates groups. Use for unit test only!
        /// </summary>
        public async Task PurgeAsync(CancellationToken ct = default(CancellationToken))
        {
            var secretItems = await _keyVaultClient.GetSecretsAsync(_vaultBaseUrl, MaxResults, ct).ConfigureAwait(false);
            while (secretItems != null)
            {
                foreach (var secretItem in secretItems.Where(s => s.ContentType == ContentTypeCrl))
                {
                    try
                    {
                        var deletedSecretBundle = await _keyVaultClient.DeleteSecretAsync(_vaultBaseUrl, secretItem.Identifier.Name, ct);
                        await _keyVaultClient.PurgeDeletedSecretAsync(_vaultBaseUrl, secretItem.Identifier.Name, ct);
                    }
                    catch
                    {
                        // intentionally fall through, purge may fail
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

            var certItems = await _keyVaultClient.GetCertificatesAsync(_vaultBaseUrl, MaxResults, true, ct).ConfigureAwait(false);
            while (certItems != null)
            {
                foreach (var certItem in certItems)
                {
                    try
                    {
                        var deletedCertBundle = await _keyVaultClient.DeleteCertificateAsync(_vaultBaseUrl, certItem.Identifier.Name, ct);
                        await _keyVaultClient.PurgeDeletedCertificateAsync(_vaultBaseUrl, certItem.Identifier.Name, ct);
                    }
                    catch
                    {
                        // intentionally fall through, purge may fail
                    }

                }
                if (certItems.NextPageLink != null)
                {
                    certItems = await _keyVaultClient.GetCertificatesNextAsync(certItems.NextPageLink, ct).ConfigureAwait(false);
                }
                else
                {
                    certItems = null;
                }
            }
        }


        private Dictionary<string, string> CreateCertificateTags(string id, bool trusted)
        {
            Dictionary<string, string> tags = new Dictionary<string, string>
            {
                [id] = trusted ? TagTrustedList : TagIssuerList
            };
            return tags;
        }

        private CertificateAttributes CreateCertificateAttributes(
            X509Certificate2 certificate
            )
        {
            return CreateCertificateAttributes(certificate.NotBefore, certificate.NotAfter);
        }

        private CertificateAttributes CreateCertificateAttributes(
            DateTime notBefore,
            DateTime notAfter
            )
        {
            var attributes = new CertificateAttributes
            {
                Enabled = true,
                NotBefore = notBefore,
                Expires = notAfter
            };
            return attributes;
        }


        private CertificatePolicy CreateCertificatePolicy(
            X509Certificate2 certificate,
            bool selfSigned)
        {
            int keySize;
            using (RSA rsa = certificate.GetRSAPublicKey())
            {
                keySize = rsa.KeySize;
                return CreateCertificatePolicy(certificate.Subject, rsa.KeySize, selfSigned);
            }
        }

        private CertificatePolicy CreateCertificatePolicy(
            string subject,
            int keySize,
            bool selfSigned,
            bool reuseKey = false)
        {

            var policy = new CertificatePolicy
            {
                IssuerParameters = new IssuerParameters
                {
                    Name = selfSigned ? "Self" : "Unknown"
                },
                KeyProperties = new KeyProperties
                {
                    Exportable = false,
                    KeySize = keySize,
                    KeyType = _keyStoreHSM ? "RSA-HSM" : "RSA",
                    ReuseKey = reuseKey
                },
                SecretProperties = new SecretProperties
                {
                    ContentType = CertificateContentType.Pfx
                },
                X509CertificateProperties = new X509CertificateProperties
                {
                    Subject = subject
                }
            };
            return policy;
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
        private bool _keyStoreHSM;
        private IKeyVaultClient _keyVaultClient;
        private ILogger _logger;
        private ClientAssertionCertificate _assertionCert;
        private ClientCredential _clientCredential;
    }
}

