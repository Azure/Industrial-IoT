// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto.Default {
    using Microsoft.Azure.IIoT.Crypto.Models;
    using Microsoft.Azure.IIoT.Utils;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Simple Certificate authority
    /// </summary>
    public class CertificateIssuer : ICertificateIssuer {

        /// <summary>
        /// Create factory
        /// </summary>
        /// <param name="store"></param>
        /// <param name="repo"></param>
        /// <param name="keys"></param>
        /// <param name="factory"></param>
        /// <param name="logger"></param>
        public CertificateIssuer(ICertificateStore store, ICertificateRepository repo,
            IKeyStore keys, ICertificateFactory factory, ILogger logger) {
            _keys = keys ?? throw new ArgumentNullException(nameof(keys));
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        /// <inheritdoc/>
        public async Task<Certificate> NewIssuerCertificateAsync(string rootCertificate,
            string certificateName, X500DistinguishedName subjectName, DateTime? notBefore,
            CreateKeyParams keyParams, IssuerPolicies policies,
            Func<byte[], IEnumerable<X509Extension>> extensions,
            CancellationToken ct) {

            if (string.IsNullOrEmpty(certificateName)) {
                throw new ArgumentNullException(nameof(certificateName));
            }

            // Get CA certificate
            var caCertificate = await _store.GetLatestCertificateAsync(rootCertificate, ct);
            if (caCertificate.IssuerPolicies == null) {
                throw new ArgumentException("root certificate is not an issuer");
            }

            // Validate policies
            policies = policies.Validate(caCertificate.IssuerPolicies, keyParams);

            // Create new key
            var keyHandle = await _keys.CreateKeyAsync(Guid.NewGuid().ToString(),
                keyParams, new KeyStoreProperties {
                    Exportable = false
                }, ct);
            try {
                // Get public key for the key
                var publicKey = await _keys.GetPublicKeyAsync(keyHandle, ct);

                var signedcert = await _factory.CreateCertificateAsync(_keys, caCertificate,
                    subjectName, publicKey,
                    GetNotAfter(notBefore, caCertificate.IssuerPolicies.IssuedLifetime.Value,
                        caCertificate.NotAfterUtc, out var notAfter),
                    notAfter,
                    caCertificate.IssuerPolicies.SignatureType.Value, true, extensions, ct);

                using (signedcert) {
                    // Import new issued certificate
                    var result = signedcert.ToCertificate(policies, keyHandle);
                    await _repo.AddCertificateAsync(certificateName, result, null, ct);
                    return result;
                }
            }
            catch (Exception ex) {
                _logger.Verbose(ex, "Failed to add certificate, delete key");
                await Try.Async(() => _keys.DeleteKeyAsync(keyHandle, ct));
                throw;
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

            // Create new signing key
            var keyHandle = await _keys.CreateKeyAsync(Guid.NewGuid().ToString(), keyParams,
                new KeyStoreProperties {
                    Exportable = false
                }, ct);
            try {
                // Get public key
                var publicKey = await _keys.GetPublicKeyAsync(keyHandle, ct);

                // Create certificate
                var certificate = await _factory.CreateCertificateAsync(_keys, keyHandle,
                    subjectName, publicKey,
                    GetNotAfter(notBefore, lifetime, DateTime.MaxValue, out var notAfter),
                    notAfter, policies.SignatureType.Value, true, extensions, ct);
                using (certificate) {
                    // Import certificate
                    var result = certificate.ToCertificate(policies, keyHandle);
                    await _repo.AddCertificateAsync(certificateName, result, null, ct);
                    return result;
                }
            }
            catch (Exception ex) {
                _logger.Verbose(ex, "Failed to add certificate, delete key");
                await Try.Async(() => _keys.DeleteKeyAsync(keyHandle, ct));
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<Certificate> CreateSignedCertificateAsync(string rootCertificate,
            string certificateName, Key publicKey, X500DistinguishedName subjectName,
            DateTime? notBefore, Func<byte[], IEnumerable<X509Extension>> extensions,
            CancellationToken ct) {

            if (string.IsNullOrEmpty(certificateName)) {
                throw new ArgumentNullException(nameof(certificateName));
            }
            if (publicKey == null) {
                throw new ArgumentNullException(nameof(publicKey));
            }

            // Get CA certificate
            var caCertificate = await _store.GetLatestCertificateAsync(rootCertificate, ct);
            if (caCertificate.IssuerPolicies == null) {
                throw new ArgumentException("Specified isseur certificate is not an issuer");
            }

            var signedcert = await _factory.CreateCertificateAsync(_keys, caCertificate,
                subjectName, publicKey,
                GetNotAfter(notBefore, caCertificate.IssuerPolicies.IssuedLifetime.Value,
                    caCertificate.NotAfterUtc, out var notAfter),
                notAfter,
                caCertificate.IssuerPolicies.SignatureType.Value, false, extensions, ct);

            // Import new issued certificate
            var result = signedcert.ToCertificate();
            await _repo.AddCertificateAsync(certificateName, result, null, ct);
            return result;
        }

        /// <inheritdoc/>
        public async Task<Certificate> CreateCertificateAndPrivateKeyAsync(string rootCertificate,
            string certificateName, X500DistinguishedName subjectName, DateTime? notBefore,
            CreateKeyParams keyParams, Func<byte[], IEnumerable<X509Extension>> extensions,
            CancellationToken ct) {

            if (string.IsNullOrEmpty(certificateName)) {
                throw new ArgumentNullException(nameof(certificateName));
            }

            // Get CA certificate
            var caCertificate = await _store.GetLatestCertificateAsync(rootCertificate, ct);
            if (caCertificate.IssuerPolicies == null) {
                throw new ArgumentException("Specified issuer certificate is not an issuer");
            }

            // Create new key
            var keyHandle = await _keys.CreateKeyAsync(Guid.NewGuid().ToString(),
                keyParams, new KeyStoreProperties {
                    Exportable = true
                }, ct);
            try {
                // Get public key for the key
                var publicKey = await _keys.GetPublicKeyAsync(keyHandle, ct);

                // create new signed cert
                var signedcert = await _factory.CreateCertificateAsync(_keys, caCertificate,
                    subjectName, publicKey,
                    GetNotAfter(notBefore, caCertificate.IssuerPolicies.IssuedLifetime.Value,
                        caCertificate.NotAfterUtc, out var notAfter),
                    notAfter,
                    caCertificate.IssuerPolicies.SignatureType.Value, false, extensions, ct);
                using (signedcert) {
                    // Import new issued certificate
                    var result = signedcert.ToCertificate(null, keyHandle);
                    await _repo.AddCertificateAsync(certificateName, result, null, ct);
                    return result;
                }
            }
            catch (Exception ex) {
                _logger.Verbose(ex, "Failed to add certificate, delete key");
                await Try.Async(() => _keys.DeleteKeyAsync(keyHandle, ct));
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<Certificate> ImportCertificateAsync(string certificateName,
            Certificate certificate, Key privateKey, CancellationToken ct) {

            if (certificate == null) {
                throw new ArgumentException(nameof(certificate));
            }
            certificate = certificate.Clone();
            certificate.KeyHandle = null;
            if (privateKey != null) {
                // Store key
                certificate.KeyHandle = await _keys.ImportKeyAsync(certificateName,
                    privateKey, new KeyStoreProperties { Exportable = false }, ct);
            }
            else {
                // Cannot issue certificates without private key
                certificate.IssuerPolicies = null;
            }
            try {
                // Import certificate and optionally key handle
                await _repo.AddCertificateAsync(certificateName, certificate, null, ct);
                return certificate;
            }
            catch (Exception ex) {
                if (certificate.KeyHandle != null) {
                    _logger.Error(ex, "Failed to add certificate, delete key");
                    await Try.Async(() => _keys.DeleteKeyAsync(certificate.KeyHandle, ct));
                }
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task DisableCertificateAsync(Certificate certificate,
            CancellationToken ct) {
            var id = await _repo.DisableCertificateAsync(certificate, ct);
            try {
                var cert = await _repo.FindCertificateAsync(id);
                if (cert == null) {
                    return;
                }
                await _keys.DisableKeyAsync(cert.KeyHandle, ct);
            }
            catch (Exception ex) {
                _logger.Error(ex, "Failed to disable key - but continue...");
            }
        }

        /// <summary>
        /// Create certificate attributes
        /// </summary>
        /// <param name="notBefore"></param>
        /// <param name="lifetime"></param>
        /// <param name="maxNotAfter"></param>
        /// <param name="notAfter"></param>
        /// <returns></returns>
        private static DateTime GetNotAfter(
            DateTime? notBefore, TimeSpan lifetime,
            DateTime maxNotAfter, out DateTime notAfter) {
            var now = DateTime.UtcNow;
            notBefore = notBefore ?? now;
            notAfter = notBefore.Value + lifetime;
            if (notAfter > maxNotAfter) {
                notAfter = maxNotAfter;
            }
            if (notAfter < notBefore.Value) {
                notBefore = notAfter; // Invalidate - could throw...
            }
            return notBefore.Value;
        }


        private readonly IKeyStore _keys;
        private readonly ICertificateStore _store;
        private readonly ICertificateRepository _repo;
        private readonly ILogger _logger;
        private readonly ICertificateFactory _factory;
    }
}
