// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto.Default {
    using Microsoft.Azure.IIoT.Crypto.Models;
    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Entity certificate factory
    /// </summary>
    public class CertificateFactory : ICertificateFactory {

        /// <summary>
        /// Configuration
        /// </summary>
        /// <param name="config"></param>
        public CertificateFactory(ICertificateFactoryConfig config = null) {
            _config = config;
        }

        /// <inheritdoc/>
        public Task<X509Certificate2> CreateCertificateAsync(IDigestSigner signer,
            Certificate issuer, X500DistinguishedName subjectName, Key pubKey,
            DateTime notBefore, DateTime notAfter, SignatureType signatureType,
            bool canIssue, Func<byte[], IEnumerable<X509Extension>> extensions,
            CancellationToken ct) {

            try {

                if (issuer == null) {
                    throw new ArgumentNullException(nameof(issuer));
                }
                if (issuer.RawData == null) {
                    throw new ArgumentNullException(nameof(issuer.RawData));
                }
                if (issuer.IssuerPolicies == null) {
                    throw new ArgumentNullException(nameof(issuer.IssuerPolicies));
                }
                if (pubKey == null) {
                    throw new ArgumentNullException(nameof(pubKey));
                }

                // new serial number
                var serialNumber = new SerialNumber();

                // Create certificate request
                var request = pubKey.CreateCertificateRequest(subjectName, signatureType);

                // Basic constraints
                request.CertificateExtensions.Add(
                    new X509BasicConstraintsExtension(canIssue, canIssue, 0, true));
                // Subject Key Identifier
                request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(
                    request.PublicKey, X509SubjectKeyIdentifierHashAlgorithm.Sha1, false));
                // Authority Key Identifier
                using (var issuerCert = issuer.ToX509Certificate2()) {
                    request.CertificateExtensions.Add(new X509AuthorityKeyIdentifierExtension(
                        issuerCert));
                }

                var keyUsages = X509KeyUsageFlags.None;
                if (extensions != null) {
                    foreach (var extension in extensions(serialNumber.Value)) {
                        if (extension == null ||
                            extension is X509BasicConstraintsExtension ||
                            extension is X509CrlDistributionPointsExtension ||
                            extension is X509SubjectKeyIdentifierExtension ||
                            extension is X509AuthorityKeyIdentifierExtension) {
                            continue;
                        }
                        if (extension is X509CrlDistributionPointsExtension &&
                            canIssue &&
                            _config.AuthorityCrlRootUrl != null) {
                            continue;
                        }
                        if (extension is X509KeyUsageExtension kux) {
                            keyUsages = kux.KeyUsages;
                            continue;
                        }
                        request.CertificateExtensions.Add(extension);
                    }
                }
                if (canIssue) {
                    request.CertificateExtensions.Add(new X509KeyUsageExtension(
                        X509KeyUsageFlags.DigitalSignature |
                        X509KeyUsageFlags.KeyCertSign |
                        X509KeyUsageFlags.CrlSign | keyUsages, true));
                    if (_config?.AuthorityCrlRootUrl != null) {
                        // add crl distribution point, if available
                        request.CertificateExtensions.Add(new X509CrlDistributionPointsExtension(
                            PatchUrl(_config.AuthorityCrlRootUrl, serialNumber.ToString())));
                    }
                }
                else {
                    // Key Usage
                    request.CertificateExtensions.Add(new X509KeyUsageExtension(
                        X509KeyUsageFlags.DigitalSignature |
                        X509KeyUsageFlags.DataEncipherment |
                        X509KeyUsageFlags.NonRepudiation |
                        X509KeyUsageFlags.KeyEncipherment | keyUsages, true));
                }

                if (_config?.AuthorityInfoRootUrl != null) {
                    // add information access point, if available for issuer authority
                    request.CertificateExtensions.Add(new X509AuthorityInformationAccessExtension(
                        PatchUrl(_config.AuthorityInfoRootUrl, issuer.GetSerialNumberAsString())));
                }

                // Adjust validity to issued certificate
                if (notAfter > issuer.NotAfterUtc) {
                    notAfter = issuer.NotAfterUtc;
                }
                if (notBefore < issuer.NotBeforeUtc) {
                    notBefore = issuer.NotBeforeUtc;
                }

                var signedCert = request.Create(signer, issuer, notBefore, notAfter, serialNumber.Value);
                return Task.FromResult(signedCert);
            }
            catch (Exception ex) {
                return Task.FromException<X509Certificate2>(ex);
            }
        }

        /// <inheritdoc/>
        Task<X509Certificate2> ICertificateFactory.CreateCertificateAsync(IDigestSigner signer,
            KeyHandle signingKey, X500DistinguishedName subject, Key pubKey,
            DateTime notBefore, DateTime notAfter, SignatureType signatureType,
            bool canIssue, Func<byte[], IEnumerable<X509Extension>> extensions, CancellationToken ct) {
            try {
                if (signingKey == null) {
                    throw new ArgumentNullException(nameof(signingKey));
                }
                if (pubKey == null) {
                    throw new ArgumentNullException(nameof(pubKey));
                }

                // new serial number
                var serialNumber = new SerialNumber();
                // Create certificate request
                var request = pubKey.CreateCertificateRequest(subject, signatureType);

                // Basic constraints
                request.CertificateExtensions.Add(new X509BasicConstraintsExtension(
                    canIssue, canIssue, 0, true));
                // Subject Key Identifier
                var ski = new X509SubjectKeyIdentifierExtension(request.PublicKey,
                    X509SubjectKeyIdentifierHashAlgorithm.Sha1, false);
                request.CertificateExtensions.Add(ski);
                // Authority key kdentifier
                request.CertificateExtensions.Add(new X509AuthorityKeyIdentifierExtension(
                    subject.Name, serialNumber, ski.SubjectKeyIdentifier));

                var keyUsages = X509KeyUsageFlags.None;
                if (extensions != null) {
                    foreach (var extension in extensions(serialNumber.Value)) {
                        if (extension == null ||
                            extension is X509BasicConstraintsExtension ||
                            extension is X509SubjectKeyIdentifierExtension ||
                            extension is X509AuthorityKeyIdentifierExtension) {
                            continue;
                        }
                        if (extension is X509CrlDistributionPointsExtension &&
                            canIssue &&
                            _config?.AuthorityCrlRootUrl != null) {
                            continue;
                        }
                        if (extension is X509KeyUsageExtension kux) {
                            keyUsages = kux.KeyUsages;
                            continue;
                        }
                        request.CertificateExtensions.Add(extension);
                    }
                }
                if (canIssue) {
                    request.CertificateExtensions.Add(new X509KeyUsageExtension(
                        X509KeyUsageFlags.DigitalSignature |
                        X509KeyUsageFlags.KeyCertSign |
                        X509KeyUsageFlags.CrlSign | keyUsages, true));

                    if (_config?.AuthorityCrlRootUrl != null) {
                        // add crl distribution point, if available
                        request.CertificateExtensions.Add(new X509CrlDistributionPointsExtension(
                            PatchUrl(_config.AuthorityCrlRootUrl, serialNumber.ToString())));
                    }
                }
                else {
                    // Key Usage
                    request.CertificateExtensions.Add(new X509KeyUsageExtension(
                        X509KeyUsageFlags.DigitalSignature |
                        X509KeyUsageFlags.DataEncipherment |
                        X509KeyUsageFlags.NonRepudiation |
                        X509KeyUsageFlags.KeyEncipherment | keyUsages, true));
                }

                var signedCert = request.Create(signer, subject, signingKey, signatureType,
                    notBefore, notAfter, serialNumber.Value);
                return Task.FromResult(signedCert);
            }
            catch (Exception ex) {
                return Task.FromException<X509Certificate2>(ex);
            }
        }

        /// <summary>
        /// Patch serial number in a Url. string version.
        /// </summary>
        /// <param name="extensionUrl"></param>
        /// <param name="serial"></param>
        /// <returns></returns>
        private static string PatchUrl(string extensionUrl, string serial) {
            if (extensionUrl == null) {
                throw new ArgumentNullException(nameof(extensionUrl));
            }
            if (serial == null) {
                throw new ArgumentNullException(nameof(serial));
            }
            if (extensionUrl.Contains("%serial%")) {
                return extensionUrl.Replace("%serial%", serial.ToLower());
            }
            return $"{extensionUrl.TrimEnd('/')}/{serial}";
        }

        private readonly ICertificateFactoryConfig _config;
    }
}
