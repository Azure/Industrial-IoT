// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto.KeyVault.Clients {
    using Microsoft.Azure.IIoT.Crypto.KeyVault.Models;
    using Microsoft.Azure.IIoT.Crypto.Models;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Azure.IIoT.Storage;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Azure.KeyVault;
    using Microsoft.Azure.KeyVault.Models;
    using Microsoft.Azure.KeyVault.WebKey;
    using Microsoft.Extensions.Diagnostics.HealthChecks;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// A KeyVault service client.
    /// </summary>
    public class KeyVaultServiceClient : IKeyValueStore, ICertificateIssuer, IKeyStore,
        IHealthCheck, IDisposable {

        /// <summary>
        /// Create key vault service client
        /// </summary>
        /// <param name="config">Keyvault configuration.</param>
        /// <param name="serializer"></param>
        /// <param name="certificates"></param>
        /// <param name="factory"></param>
        /// <param name="provider"></param>
        public KeyVaultServiceClient(ICertificateRepository certificates,
            ICertificateFactory factory, IKeyVaultConfig config, IJsonSerializer serializer,
            Auth.ITokenProvider provider) : this (certificates, factory, config, serializer,
                new KeyVaultClient(async (_, resource, scope) => {
                    var token = await provider.GetTokenForAsync(
                        resource, scope.YieldReturn());
                    return token.RawToken;
                })) {
        }

        /// <summary>
        /// Create key vault service client
        /// </summary>
        /// <param name="config">Keyvault configuration.</param>
        /// <param name="serializer"></param>
        /// <param name="certificates"></param>
        /// <param name="factory"></param>
        /// <param name="client"></param>
        public KeyVaultServiceClient(ICertificateRepository certificates,
            ICertificateFactory factory, IKeyVaultConfig config, IJsonSerializer serializer,
            IKeyVaultClient client) {

            if (config == null) {
                throw new ArgumentNullException(nameof(config));
            }

            _vaultBaseUrl = config.KeyVaultBaseUrl;
            _keyStoreIsHsm = config.KeyVaultIsHsm;
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _certificates = certificates ?? throw new ArgumentNullException(nameof(certificates));
            _keyVaultClient = client ?? throw new ArgumentNullException(nameof(client));
        }

        /// <inheritdoc/>
        public void Dispose() {
            _keyVaultClient.Dispose();
        }

        /// <inheritdoc/>
        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context, CancellationToken ct) {
            try {
                var secret = await _keyVaultClient.GetSecretsAsync(_vaultBaseUrl,
                    1, ct);

                // TODO: Check certificates in sync with keyvault

                return HealthCheckResult.Healthy();
            }
            catch (Exception ex) {
                return new HealthCheckResult(context.Registration.FailureStatus,
                    exception: ex);
            }
        }

        /// <inheritdoc/>
        public async Task<string> GetKeyValueAsync(
            string key, string contentType, CancellationToken ct) {
            if (string.IsNullOrEmpty(key)) {
                throw new ArgumentNullException(nameof(key));
            }
            try {
                var secret = await _keyVaultClient.GetSecretAsync(_vaultBaseUrl,
                    key, ct);
                if (contentType != null) {
                    if (secret.ContentType == null ||
                        !secret.ContentType.EqualsIgnoreCase(contentType)) {
                        throw new ResourceInvalidStateException("Content type mismatch");
                    }
                }
                return secret.Value;
            }
            catch (KeyVaultErrorException ex) {
                throw new ExternalDependencyException("Failed to get secret", ex);
            }
        }

        /// <inheritdoc/>
        public async Task SetKeyValueAsync(string key, string value, DateTime? notBefore,
            DateTime? notAfter, string contentType, CancellationToken ct) {
            if (string.IsNullOrEmpty(key)) {
                throw new ArgumentNullException(nameof(key));
            }
            if (string.IsNullOrEmpty(value)) {
                throw new ArgumentNullException(nameof(value));
            }
            try {
                var secretAttributes = new SecretAttributes {
                    Enabled = true,
                    NotBefore = notBefore,
                    Expires = notAfter
                };
                var secret = await _keyVaultClient.SetSecretAsync(_vaultBaseUrl,
                    key, value, null, contentType, secretAttributes, ct);
            }
            catch (KeyVaultErrorException ex) {
                throw new ExternalDependencyException("Failed to set secret", ex);
            }
        }

        /// <inheritdoc/>
        public async Task DeleteKeyValueAsync(string key, CancellationToken ct) {
            if (string.IsNullOrEmpty(key)) {
                throw new ArgumentNullException(nameof(key));
            }
            try {
                await _keyVaultClient.DeleteSecretAsync(_vaultBaseUrl, key, ct);
            }
            catch (KeyVaultErrorException ex) {
                throw new ExternalDependencyException("Failed to delete secret", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<Certificate> ImportCertificateAsync(string certificateName,
            Certificate certificate, Key privateKey, CancellationToken ct) {
            if (string.IsNullOrEmpty(certificateName)) {
                throw new ArgumentNullException(nameof(certificateName));
            }
            if (certificate == null) {
                throw new ArgumentNullException(nameof(certificate));
            }
            if (privateKey == null) {
                certificate = certificate.Clone();
                certificate.IssuerPolicies = null;
                await _certificates.AddCertificateAsync(certificateName, certificate, null, ct);
                return certificate;
            }
            try {
                var password = Guid.NewGuid().ToString();
                var pfxBase64 = certificate.ToPfx(privateKey, password).ToBase64String();
                // Import bundle
                var bundle = await _keyVaultClient.ImportCertificateAsync(_vaultBaseUrl,
                    certificateName, pfxBase64, password,
                    CreateCertificatePolicy(certificate, true, _keyStoreIsHsm),
                    CreateCertificateAttributes(certificate.NotBeforeUtc,
                        certificate.NotAfterUtc - certificate.NotBeforeUtc, certificate.NotAfterUtc),
                    null, ct);
                try {
                    var result = CertificateEx.Create(bundle.Cer,
                        KeyVaultKeyHandle.Create(bundle), certificate.IssuerPolicies);
                    await _certificates.AddCertificateAsync(
                        certificateName, result, bundle.CertificateIdentifier.Identifier, ct);
                    return result;
                }
                catch {
                    await Try.Async(() => _keyVaultClient.DeleteCertificateAsync(
                        _vaultBaseUrl, certificateName, ct));
                    throw;
                }
            }
            catch (KeyVaultErrorException ex) {
                throw new ExternalDependencyException("Failed to import certificate", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<Certificate> NewRootCertificateAsync(string certificateName,
            X500DistinguishedName subjectName, DateTime? notBefore, TimeSpan lifetime,
            CreateKeyParams keyParams, IssuerPolicies policies,
            Func<byte[], IEnumerable<X509Extension>> extensions,
            CancellationToken ct) {

            if (string.IsNullOrEmpty(certificateName)) {
                throw new ArgumentNullException(nameof(certificateName));
            }

            // Validate policies
            policies = policies.Validate(null, keyParams);
            string caTempCertIdentifier = null;
            try {
                // (1) Create key in key vault and get CSR.

                // policy self signed, new key, not exportable key
                var policySelfSignedNewKey = CreateCertificatePolicy(
                    subjectName.Name, keyParams, true, _keyStoreIsHsm, false, false);
                var tempAttributes = CreateCertificateAttributes(
                    DateTime.UtcNow.AddMinutes(-10), TimeSpan.FromMinutes(10),
                    DateTime.MaxValue);

                await CreateCertificateAsync(certificateName, policySelfSignedNewKey,
                    tempAttributes, null, ct);

                // We have the cert - get it and key identifier to do the signing
                var createdCertificateBundle = await _keyVaultClient.GetCertificateAsync(
                    _vaultBaseUrl, certificateName, ct);
                caTempCertIdentifier =
                    createdCertificateBundle.CertificateIdentifier.Identifier;

                // policy unknown issuer, reuse key - not exportable
                var policyUnknownReuse = CreateCertificatePolicy(
                    subjectName.Name, keyParams, false, _keyStoreIsHsm, true, false);
                var attributes = CreateCertificateAttributes(notBefore, lifetime,
                    DateTime.MaxValue);

                // create the CSR
                var createResult = await CreateCertificateAsync(certificateName,
                    policyUnknownReuse, attributes, null, ct);
                if (createResult.Csr == null) {
                    throw new CryptographicUnexpectedOperationException(
                        "Failed to read CSR from CreateCertificate.");
                }

                // decode the CSR and verify consistency
                var info = createResult.Csr.ToCertificationRequest();

                // (2) - Issue root X509 Certificate with the csr.

                var signedcert = await _factory.CreateCertificateAsync(this,
                    KeyVaultKeyHandle.Create(createdCertificateBundle), subjectName,
                    info.PublicKey,
                    attributes.NotBefore.Value, attributes.Expires.Value,
                    policies.SignatureType.Value, true, extensions, ct);

                // (3) - Complete certificate creation with merger of X509 Certificate.

                var mergeResult = await _keyVaultClient.MergeCertificateAsync(
                    _vaultBaseUrl, certificateName,
                    new X509Certificate2Collection(signedcert), null, null, ct);

                // (4) - Get merged certificate and key identifier

                var mergedCert = await _keyVaultClient.GetCertificateAsync(
                    mergeResult.CertificateIdentifier.Identifier, ct);
                var cert = CertificateEx.Create(mergedCert.Cer,
                    KeyVaultKeyHandle.Create(mergedCert), policies);
                await _certificates.AddCertificateAsync(certificateName, cert,
                    mergedCert.CertificateIdentifier.Identifier, ct);
                return cert;
            }
            catch (KeyVaultErrorException kex) {
                throw new ExternalDependencyException(
                    "Failed to create new Root CA certificate", kex);
            }
            finally {
                if (caTempCertIdentifier != null) {
                    // disable the temp cert for self signing operation
                    var attr = new CertificateAttributes {
                        Enabled = false
                    };
                    await Try.Async(() => _keyVaultClient.UpdateCertificateAsync(
                        caTempCertIdentifier, null, attr));
                }
            }
        }

        /// <inheritdoc/>
        public async Task DisableCertificateAsync(Certificate certificate,
            CancellationToken ct) {
            var id = await _certificates.DisableCertificateAsync(certificate, ct);
            try {
                var cert = await _keyVaultClient.GetCertificateAsync(id, ct);
                if (cert == null) {
                    return;
                }
                // Disable in key store - if not already disabled
                var enabled = cert.Attributes.Enabled ?? true;
                if (!enabled) {
                    return;
                }
                await _keyVaultClient.UpdateCertificateAsync(
                    id, null, new CertificateAttributes { Enabled = false });
            }
            catch (KeyVaultErrorException ex) {
                throw new ExternalDependencyException(
                    "Failed to disable certificate in key vault", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<Certificate> NewIssuerCertificateAsync(string rootCertificate,
            string certificateName, X500DistinguishedName subjectName,
            DateTime? notBefore, CreateKeyParams keyParams, IssuerPolicies policies,
            Func<byte[], IEnumerable<X509Extension>> extensions,
            CancellationToken ct) {
            try {
                // (0) Retrieve issuer certificate

                var caCertBundle = await _keyVaultClient.GetCertificateAsync(
                    _vaultBaseUrl, rootCertificate, ct);
                if (caCertBundle == null) {
                    throw new ResourceNotFoundException("Issuer cert not found.");
                }
                var caCert = await _certificates.FindCertificateAsync(
                    caCertBundle.CertificateIdentifier.Identifier);
                if (caCert?.IssuerPolicies == null) {
                    throw new ArgumentException("Certificate cannot issue.");
                }

                // Validate policies
                policies = policies.Validate(caCert.IssuerPolicies, keyParams);

                // (1) Create key in key vault and get CSR.

                // policy unknown issuer, new key, exportable key
                var policyUnknownNewExportable = CreateCertificatePolicy(
                    subjectName.Name, keyParams, false,
                    _keyStoreIsHsm, false, true);

                var attributes = CreateCertificateAttributes(notBefore,
                    caCert.IssuerPolicies.IssuedLifetime.Value, caCert.NotAfterUtc);
                var createResult = await CreateCertificateAsync(certificateName,
                    policyUnknownNewExportable, attributes, null, ct);

                if (createResult.Csr == null) {
                    throw new CryptographicUnexpectedOperationException(
                        "Failed to read CSR from CreateCertificate.");
                }
                // decode the CSR and verify consistency
                var info = createResult.Csr.ToCertificationRequest();

                try {
                    // (2) - Issue X509 Certificate with csr and root certificate.

                    var signedcert = await _factory.CreateCertificateAsync(this,
                        caCert, subjectName, info.PublicKey,
                        attributes.NotBefore.Value, attributes.Expires.Value,
                        caCert.IssuerPolicies.SignatureType.Value, true, extensions, ct);

                    // (3) - Complete certificate creation with merger of X509 Certificate.

                    var mergeResult = await _keyVaultClient.MergeCertificateAsync(
                        _vaultBaseUrl, certificateName,
                        new X509Certificate2Collection(signedcert), null, null, ct);

                    // (4) - Get merged certificate and key identifier

                    var mergedCert = await _keyVaultClient.GetCertificateAsync(
                        mergeResult.CertificateIdentifier.Identifier, ct);

                    var cert = CertificateEx.Create(mergedCert.Cer,
                        KeyVaultKeyHandle.Create(mergedCert), policies);
                    if (!cert.IsIssuer()) {
                        throw new ArgumentException("Certifcate created is not issuer.");
                    }
                    await _certificates.AddCertificateAsync(certificateName, cert,
                        mergedCert.CertificateIdentifier.Identifier, ct);
                    return cert;
                }
                catch {
                    await Try.Async(() => _keyVaultClient.DeleteCertificateAsync(
                        _vaultBaseUrl, certificateName, ct));
                    await Try.Async(() => _keyVaultClient.PurgeDeletedCertificateAsync(
                        _vaultBaseUrl, certificateName, ct));
                    throw;
                }
            }
            catch (KeyVaultErrorException ex) {
                throw new ExternalDependencyException(
                    "Failed to create new key pair certificate", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<Certificate> CreateSignedCertificateAsync(
            string rootCertificate, string certificateName, Key publicKey,
            X500DistinguishedName subjectName, DateTime? notBefore,
            Func<byte[], IEnumerable<X509Extension>> extensions,
            CancellationToken ct) {

            if (string.IsNullOrEmpty(certificateName)) {
                throw new ArgumentNullException(nameof(certificateName));
            }
            if (publicKey == null) {
                throw new ArgumentNullException(nameof(publicKey));
            }

            // (0) Retrieve issuer certificate
            var caCertBundle = await _keyVaultClient.GetCertificateAsync(
                _vaultBaseUrl, rootCertificate, ct);
            if (caCertBundle == null) {
                throw new ResourceNotFoundException("Issuer cert not found.");
            }
            var caCert = await _certificates.FindCertificateAsync(
                caCertBundle.CertificateIdentifier.Identifier);
            if (caCert?.IssuerPolicies == null) {
                throw new ArgumentException("Certificate cannot issue.");
            }

            // (1) create signed cert
            var attributes = CreateCertificateAttributes(notBefore,
                caCert.IssuerPolicies.IssuedLifetime.Value, caCert.NotAfterUtc);
            var signedcert = await _factory.CreateCertificateAsync(this,
                caCert, subjectName, publicKey.GetPublicKey(),
                attributes.NotBefore.Value, attributes.Expires.Value,
                caCert.IssuerPolicies.SignatureType.Value, false, extensions, ct);

            using (signedcert) {
                // (3) Import new signed certificate
                var result = signedcert.ToCertificate();
                if (result.IsIssuer()) {
                    throw new ArgumentException("Factory created issuer certifcate.");
                }
                await _certificates.AddCertificateAsync(certificateName, result, null, ct);
                return result;
            }
        }

        /// <inheritdoc/>
        public async Task<Certificate> CreateCertificateAndPrivateKeyAsync(string rootCertificate,
            string certificateName, X500DistinguishedName subjectName, DateTime? notBefore,
            CreateKeyParams keyParams, Func<byte[], IEnumerable<X509Extension>> extensions,
            CancellationToken ct) {
            try {
                // (0) Retrieve issuer certificate
                var caCertBundle = await _keyVaultClient.GetCertificateAsync(
                    _vaultBaseUrl, rootCertificate, ct);
                if (caCertBundle == null) {
                    throw new ResourceNotFoundException("Issuer cert not found.");
                }
                var caCert = await _certificates.FindCertificateAsync(
                    caCertBundle.CertificateIdentifier.Identifier);
                if (caCert?.IssuerPolicies == null) {
                    throw new ArgumentException("Certificate cannot issue.");
                }

                // (1) Create key in key vault and get CSR.

                // policy unknown issuer, new key, exportable key
                var policyUnknownNewExportable = CreateCertificatePolicy(
                    subjectName.Name, keyParams, false, _keyStoreIsHsm, false, true);

                var attributes = CreateCertificateAttributes(notBefore,
                    caCert.IssuerPolicies.IssuedLifetime.Value, caCert.NotAfterUtc);
                var createResult = await CreateCertificateAsync(certificateName,
                    policyUnknownNewExportable, attributes, null, ct);
                if (createResult.Csr == null) {
                    throw new CryptographicUnexpectedOperationException(
                        "Failed to read CSR from CreateCertificate.");
                }
                // decode the CSR and verify consistency
                var info = createResult.Csr.ToCertificationRequest();

                try {
                    // (2) - Issue X509 Certificate with csr and root certificate.

                    // create signed cert
                    var signedcert = await _factory.CreateCertificateAsync(this,
                        caCert, subjectName, info.PublicKey,
                        attributes.NotBefore.Value, attributes.Expires.Value,
                        caCert.IssuerPolicies.SignatureType.Value, false, extensions, ct);

                    // (3) - Complete certificate creation with merger of X509 Certificate.

                    var mergeResult = await _keyVaultClient.MergeCertificateAsync(
                        _vaultBaseUrl, certificateName,
                        new X509Certificate2Collection(signedcert), null, null, ct);

                    // (4) - Get merged certificate and key identifier

                    var mergedCert = await _keyVaultClient.GetCertificateAsync(
                        mergeResult.CertificateIdentifier.Identifier, ct);

                    var cert = CertificateEx.Create(mergedCert.Cer,
                        KeyVaultKeyHandle.Create(mergedCert));
                    System.Diagnostics.Debug.Assert(!cert.IsIssuer());
                    await _certificates.AddCertificateAsync(certificateName,
                        cert, mergedCert.CertificateIdentifier.Identifier, ct);
                    return cert;
                }
                catch {
                    await _keyVaultClient.DeleteCertificateAsync(
                        _vaultBaseUrl, certificateName, ct);
                    await Try.Async(() => _keyVaultClient.PurgeDeletedCertificateAsync(
                        _vaultBaseUrl, certificateName, ct));
                    throw;
                }
            }
            catch (KeyVaultErrorException ex) {
                throw new ExternalDependencyException(
                    "Failed to create new key pair certificate", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<KeyHandle> CreateKeyAsync(string name, CreateKeyParams keyParams,
            KeyStoreProperties store, CancellationToken ct) {
            if (string.IsNullOrEmpty(name)) {
                throw new ArgumentNullException(nameof(name));
            }
            if (keyParams == null) {
                throw new ArgumentNullException(nameof(keyParams));
            }
            try {
                if (!(store?.Exportable ?? false)) {
                    // Create key inside key vault
                    var result = await _keyVaultClient.CreateKeyAsync(_vaultBaseUrl, name,
                        new NewKeyParameters {
                            KeySize = (int?)keyParams.KeySize,
                            CurveName = keyParams.Curve?.ToJsonWebKeyCurveName(),
                            Attributes = new KeyAttributes {
                                Enabled = true,
                                NotBefore = DateTime.UtcNow
                            },
                            Kty = keyParams.Type.ToKty(_keyStoreIsHsm)
                        }, ct);

                    return KeyVaultKeyHandle.Create(result);
                }
                // Create key outside and import
                return await ImportKeyAsync(name, keyParams.CreateKey(),
                    new KeyStoreProperties { Exportable = true }, ct);
            }
            catch (KeyVaultErrorException kex) {
                throw new ExternalDependencyException("Failed to create key", kex);
            }
        }

        /// <inheritdoc/>
        public async Task<KeyHandle> ImportKeyAsync(string name, Key key,
            KeyStoreProperties store, CancellationToken ct) {
            if (string.IsNullOrEmpty(name)) {
                throw new ArgumentNullException(nameof(name));
            }
            if (key == null) {
                throw new ArgumentNullException(nameof(key));
            }
            try {
                // Import key
                var keyBundle = await _keyVaultClient.ImportKeyAsync(_vaultBaseUrl, name,
                    key.ToJsonWebKey(), null, new KeyAttributes {
                        Enabled = true,
                        NotBefore = DateTime.UtcNow
                    }, null, ct);

                if (store?.Exportable ?? false) {
                    // Store key as json web key secret so we can export it
                    var secretBundle = await _keyVaultClient.SetSecretAsync(_vaultBaseUrl,
                        name, _serializer.SerializeToString(key.ToJsonWebKey()),
                        null, ContentMimeType.Json,
                        new SecretAttributes {
                            Enabled = true,
                            NotBefore = DateTime.UtcNow
                        }, ct);
                    return KeyVaultKeyHandle.Create(keyBundle, secretBundle);
                }
                return KeyVaultKeyHandle.Create(keyBundle);
            }
            catch (KeyVaultErrorException kex) {
                throw new ExternalDependencyException("Failed to import key", kex);
            }
        }

        /// <inheritdoc/>
        public async Task<KeyHandle> GetKeyHandleAsync(string name, CancellationToken ct) {
            if (string.IsNullOrEmpty(name)) {
                throw new ArgumentNullException(nameof(name));
            }
            // Get key first
            var keyBundle = await Try.Async(() => _keyVaultClient.GetKeyAsync(name, ct));
            if (keyBundle == null) {
                // If no key - then try getting cert bundle instead
                var certBundle = await Try.Async(
                    () => _keyVaultClient.GetCertificateAsync(_vaultBaseUrl, name, ct));
                if (certBundle != null) {
                    return KeyVaultKeyHandle.Create(certBundle);
                }
                throw new ResourceNotFoundException("Key with name not found");
            }
            var secretBundle = await Try.Async(() => _keyVaultClient.GetSecretAsync(name, ct));
            return KeyVaultKeyHandle.Create(keyBundle, secretBundle);
        }

        /// <inheritdoc/>
        public async Task<Key> ExportKeyAsync(KeyHandle handle, CancellationToken ct) {
            var bundle = KeyVaultKeyHandle.GetBundle(handle);
            if (string.IsNullOrEmpty(bundle.SecretIdentifier)) {
                throw new InvalidOperationException("Non-exportable key.");
            }
            try {
                var secretBundle = await _keyVaultClient.GetSecretAsync(
                    bundle.SecretIdentifier, ct);

                // Check whether this is an imported key
                if (secretBundle.ContentType.EqualsIgnoreCase(ContentMimeType.Json)) {
                    // Decode json web key and convert to key
                    var key = _serializer.Deserialize<JsonWebKey>(secretBundle.Value);
                    return key.ToKey();
                }

                // Check whether this is a certificate backing secret
                if (secretBundle.ContentType.EqualsIgnoreCase(CertificateContentType.Pfx)) {
                    // Decode pfx from secret and get key
                    var pfx = Convert.FromBase64String(secretBundle.Value);
                    using (var cert = new X509Certificate2(pfx, (string)null,
                        X509KeyStorageFlags.Exportable)) {
                        return cert.PrivateKey.ToKey();
                    }
                }
                throw new ResourceNotFoundException(
                    $"Key handle points to invalid content {secretBundle.ContentType}");
            }
            catch (KeyVaultErrorException kex) {
                throw new ExternalDependencyException("Failed to export key", kex);
            }
        }

        /// <inheritdoc/>
        public async Task<Key> GetPublicKeyAsync(KeyHandle handle, CancellationToken ct) {
            var bundle = await _keyVaultClient.GetKeyAsync(_vaultBaseUrl,
                KeyVaultKeyHandle.GetBundle(handle).KeyIdentifier, ct);
            return bundle.Key.ToKey();
        }

        /// <inheritdoc/>
        public async Task DisableKeyAsync(KeyHandle handle, CancellationToken ct) {
            var bundle = KeyVaultKeyHandle.GetBundle(handle);
            try {
                if (!string.IsNullOrEmpty(bundle.KeyIdentifier)) {
                    await _keyVaultClient.UpdateKeyAsync(bundle.KeyIdentifier, null,
                        new KeyAttributes {
                            Enabled = false,
                            Expires = DateTime.UtcNow
                        }, null, ct);
                }
                if (!string.IsNullOrEmpty(bundle.SecretIdentifier)) {
                    // Delete private key secret also
                    await _keyVaultClient.UpdateSecretAsync(bundle.SecretIdentifier,
                        ContentMimeType.Json, new SecretAttributes {
                            Enabled = false,
                            Expires = DateTime.UtcNow
                        }, null, ct);

                }
            }
            catch (KeyVaultErrorException kex) {
                throw new ExternalDependencyException("Failed to create key", kex);
            }
        }

        /// <inheritdoc/>
        public async Task DeleteKeyAsync(KeyHandle handle, CancellationToken ct) {
            var bundle = KeyVaultKeyHandle.GetBundle(handle);
            try {
                if (!string.IsNullOrEmpty(bundle.KeyIdentifier)) {
                    await _keyVaultClient.DeleteKeyAsync(
                        _vaultBaseUrl, bundle.KeyIdentifier, ct);
                }
                if (!string.IsNullOrEmpty(bundle.SecretIdentifier)) {
                    await _keyVaultClient.DeleteSecretAsync(
                        _vaultBaseUrl, bundle.SecretIdentifier, ct);
                }
            }
            catch (KeyVaultErrorException kex) {
                throw new ExternalDependencyException("Failed to delete key", kex);
            }
        }

        /// <inheritdoc/>
        public async Task<byte[]> SignAsync(KeyHandle handle, byte[] digest,
            SignatureType algorithm, CancellationToken ct) {
            var signingKey = KeyVaultKeyHandle.GetBundle(handle).KeyIdentifier;
            try {
                var result = await _keyVaultClient.SignAsync(signingKey,
                    algorithm.ToJsonWebKeySignatureAlgorithm(), digest, ct);
                return result.Result;
            }
            catch (KeyVaultErrorException kex) {
                throw new ExternalDependencyException("Failed to Sign", kex);
            }
        }

        /// <inheritdoc/>
        public async Task<bool> VerifyAsync(KeyHandle handle, byte[] digest,
            SignatureType algorithm, byte[] signature, CancellationToken ct) {
            var signingKey = KeyVaultKeyHandle.GetBundle(handle).KeyIdentifier;
            try {
                return await _keyVaultClient.VerifyAsync(signingKey,
                    algorithm.ToJsonWebKeySignatureAlgorithm(), digest, signature, ct);
            }
            catch (KeyVaultErrorException kex) {
                throw new ExternalDependencyException("Failed to Sign", kex);
            }
        }

        /// <summary>
        /// Create certificate
        /// </summary>
        /// <param name="certificateName"></param>
        /// <param name="policy"></param>
        /// <param name="attributes"></param>
        /// <param name="tags"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task<CertificateOperation> CreateCertificateAsync(
            string certificateName, CertificatePolicy policy,
            CertificateAttributes attributes, IDictionary<string, string> tags,
            CancellationToken ct) {
            var operation = await _keyVaultClient.CreateCertificateAsync(
                _vaultBaseUrl, certificateName, policy, attributes, tags, ct);
            while (operation.Status.EqualsIgnoreCase("inprogress")) {
                await Task.Delay(1000, ct);
                ct.ThrowIfCancellationRequested();
                operation = await _keyVaultClient.GetCertificateOperationAsync(
                    _vaultBaseUrl, certificateName, ct);
            }
            if (!operation.Status.EqualsIgnoreCase("completed")) {
                throw new CryptographicUnexpectedOperationException(
                    $"Failed to create certificate - Status {operation.Status}");
            }
            return operation;
        }

        /// <summary>
        /// Create certificate attributes
        /// </summary>
        /// <param name="notBefore"></param>
        /// <param name="lifetime"></param>
        /// <param name="maxNotAfter"></param>
        /// <returns></returns>
        private static CertificateAttributes CreateCertificateAttributes(
            DateTime? notBefore, TimeSpan lifetime, DateTime maxNotAfter) {

            var now = DateTime.UtcNow;
            notBefore ??= now;
            var notAfter = notBefore.Value + lifetime;
            if (notAfter > maxNotAfter) {
                notAfter = maxNotAfter;
            }
            if (notAfter < notBefore.Value) {
                notBefore = notAfter; // Invalidate - could throw...
            }
            return new CertificateAttributes {
                Enabled = notAfter != notBefore,
                NotBefore = notBefore,
                Expires = notAfter
            };
        }

        /// <summary>
        /// Create certificate policy
        /// </summary>
        /// <param name="certificate"></param>
        /// <param name="selfSigned"></param>
        /// <param name="isHsm"></param>
        /// <returns></returns>
        private static CertificatePolicy CreateCertificatePolicy(
            Certificate certificate, bool selfSigned, bool isHsm) {
            var key = certificate.GetPublicKey();
            var createParams = new CreateKeyParams {
                Type = key.Type,
            };
            switch (key.Type) {
                case KeyType.RSA:
                    // createParams.KeySize = (uint)((RsaParams)key.Parameters).KeySize;
                    break;
                case KeyType.ECC:
                    // createParams.KeySize = (uint)((EccParams)key.Parameters).KeySize;
                    createParams.Curve = ((EccParams)key.Parameters).Curve;
                    break;
                default:
                    throw new ArgumentException("Signing algorithm not supported");
            }
            return CreateCertificatePolicy(certificate.Subject.Name, createParams,
                selfSigned, isHsm, false, false);
        }

        /// <summary>
        /// Create certificate policy
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="keyParams"></param>
        /// <param name="selfSigned"></param>
        /// <param name="reuseKey"></param>
        /// <param name="isHsm"></param>
        /// <param name="exportable"></param>
        /// <returns></returns>
        private static CertificatePolicy CreateCertificatePolicy(string subject,
            CreateKeyParams keyParams, bool selfSigned, bool isHsm, bool reuseKey,
            bool exportable) {
            ValidateKeyParameters(keyParams);
            var policy = new CertificatePolicy {
                IssuerParameters = new IssuerParameters {
                    Name = selfSigned ? "Self" : "Unknown"
                },
                KeyProperties = new KeyProperties {
                    Exportable = exportable,
                    KeySize = (int?)keyParams.KeySize,
                    Curve = keyParams.Curve?.ToJsonWebKeyCurveName(),
                    KeyType = keyParams.Type.ToKty(isHsm && !exportable),
                    ReuseKey = reuseKey
                },
                SecretProperties = new SecretProperties {
                    ContentType = CertificateContentType.Pfx
                },
                X509CertificateProperties = new X509CertificateProperties {
                    Subject = subject
                },
                LifetimeActions = new List<LifetimeAction>(),
            };
            return policy;
        }

        /// <summary>
        /// Validate key parameters
        /// </summary>
        /// <param name="keyParams"></param>
        private static void ValidateKeyParameters(CreateKeyParams keyParams) {
            if (keyParams == null) {
                throw new ArgumentNullException(nameof(keyParams));
            }
            switch (keyParams.Type) {
                case KeyType.AES:
                    throw new ArgumentException("Symmetric key not valid");
            }
        }

        private readonly string _vaultBaseUrl;
        private readonly bool _keyStoreIsHsm;
        private readonly IJsonSerializer _serializer;
        private readonly ICertificateFactory _factory;
        private readonly ICertificateRepository _certificates;
        private readonly IKeyVaultClient _keyVaultClient;
    }
}