// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto.Default {
    using Microsoft.Azure.IIoT.Crypto.BouncyCastle;
    using Microsoft.Azure.IIoT.Crypto.Models;
    using Org.BouncyCastle.Asn1.X509;
    using Org.BouncyCastle.Math;
    using Org.BouncyCastle.X509;
    using Org.BouncyCastle.X509.Extension;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Certificate revocation list factory
    /// </summary>
    public class CrlFactory : ICrlFactory {

        /// <summary>
        /// Create factory
        /// </summary>
        /// <param name="signer"></param>
        public CrlFactory(IDigestSigner signer) {
            _signer = signer ?? throw new ArgumentNullException(nameof(signer));
        }

        /// <inheritdoc/>
        public Task<Crl> CreateCrlAsync(Certificate issuer, SignatureType signature,
            IEnumerable<Certificate> revokedCertificates, DateTime? nextUpdate,
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
                if (issuer.KeyHandle == null) {
                    throw new ArgumentNullException(nameof(issuer.KeyHandle));
                }

                var bcCertCA = new X509CertificateParser().ReadCertificate(issuer.RawData);
                var thisUpdate = DateTime.UtcNow;
                var crlGen = new X509V2CrlGenerator();

                crlGen.SetIssuerDN(bcCertCA.SubjectDN);
                crlGen.SetThisUpdate(DateTime.UtcNow);
                crlGen.SetNextUpdate(nextUpdate ?? issuer.NotAfterUtc);

                if (revokedCertificates == null || !revokedCertificates.Any()) {
                    // add a dummy entry
                    crlGen.AddCrlEntry(BigInteger.One, thisUpdate, CrlReason.Unspecified);
                }
                else {
                    // add the revoked certs
                    foreach (var revokedCertificate in revokedCertificates) {
                        var revoked = revokedCertificate.Revoked?.Date ?? thisUpdate;
                        crlGen.AddCrlEntry(new BigInteger(1, revokedCertificate.SerialNumber),
                            revoked, CrlReason.PrivilegeWithdrawn);
                    }
                }
                crlGen.AddExtension(X509Extensions.AuthorityKeyIdentifier, false,
                    new AuthorityKeyIdentifierStructure(bcCertCA));

                // set new serial number
                var crlSerialNumber = BigInteger.ValueOf(DateTime.UtcNow.ToFileTimeUtc());
                crlGen.AddExtension(X509Extensions.CrlNumber, false,
                    new CrlNumber(crlSerialNumber));

                // generate updated CRL
                var signatureGenerator = _signer.CreateX509SignatureGenerator(
                    issuer.KeyHandle, signature);
                var signatureFactory = new SignatureFactory(signature, signatureGenerator);
                var updatedCrl = crlGen.Generate(signatureFactory);
                return Task.FromResult(CrlEx.ToCrl(updatedCrl.GetEncoded()));
            }
            catch (Exception ex) {
                return Task.FromException<Crl>(ex);
            }
        }

        private readonly IDigestSigner _signer;
    }
}
