// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto.Default {
    using Microsoft.Azure.IIoT.Crypto.Models;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Certificate revocation support
    /// </summary>
    public class CertificateRevoker : ICertificateRevoker {

        /// <summary>
        /// Create revoker
        /// </summary>
        /// <param name="store"></param>
        /// <param name="issuer"></param>
        /// <param name="crls"></param>
        public CertificateRevoker(ICertificateStore store, ICertificateIssuer issuer,
            ICrlRepository crls) {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _crls = crls ?? throw new ArgumentNullException(nameof(crls));
            _issuer = issuer ?? throw new ArgumentNullException(nameof(issuer));
        }

        /// <inheritdoc/>
        public async Task RevokeCertificateAsync(byte[] serialNumber, CancellationToken ct) {
            // Get certificate
            var certificate = await _store.GetCertificateAsync(serialNumber, ct);
            await RevokeCertificateAsync(certificate, ct);
        }

        /// <summary>
        /// Revoke certificate
        /// </summary>
        /// <param name="certificate"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task RevokeCertificateAsync(Certificate certificate,
            CancellationToken ct) {

            // Disable
            await _issuer.DisableCertificateAsync(certificate, ct);

            // TODO: Notify listeners

            // Invalidate crl for issuer
            if (!certificate.IsSelfIssued()) {
                await _crls.InvalidateAsync(certificate.IssuerSerialNumber, ct);
            }

            // TODO: do this as a reaction to the revocation event above
            if (!certificate.IsIssuer()) {
                return; // Done
            }

            // Get all not revoked issued certificates and recursively revoke those
            var issuedCerts = await _store.GetIssuedCertificatesAsync(
                certificate, null, false, true, ct);

            // Recursively revoke certificate
            foreach (var issued in issuedCerts) {
                await RevokeCertificateAsync(issued, ct);
            }
        }

        private readonly ICertificateStore _store;
        private readonly ICrlRepository _crls;
        private readonly ICertificateIssuer _issuer;
    }
}
