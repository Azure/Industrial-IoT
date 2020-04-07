// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault.Services {
    using Microsoft.Azure.IIoT.OpcUa.Vault.Models;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.Crypto;
    using Microsoft.Azure.IIoT.Crypto.Models;
    using Microsoft.Azure.IIoT.Exceptions;
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Certificate authority
    /// </summary>
    public sealed class CertificateAuthority : ICertificateAuthority {

        /// <summary>
        /// Create certificate authority services
        /// </summary>
        /// <param name="store"></param>
        /// <param name="revoker"></param>
        /// <param name="crls"></param>
        public CertificateAuthority(ICertificateStore store, ICertificateRevoker revoker,
            ICrlEndpoint crls) {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _crls = crls ?? throw new ArgumentNullException(nameof(crls));
            _revoker = revoker ?? throw new ArgumentNullException(nameof(revoker));
        }

        /// <inheritdoc/>
        public async Task<X509CrlChainModel> GetIssuerCrlChainAsync(
            string serialNumber, CancellationToken ct) {
            if (string.IsNullOrEmpty(serialNumber)) {
                throw new ArgumentNullException(nameof(serialNumber));
            }
            var crl = await _crls.GetCrlChainAsync(SerialNumber.Parse(serialNumber).Value, ct);
            if (!crl.Any()) {
                throw new ResourceNotFoundException(
                    "Crl chain for serial number not found");
            }
            return new X509CrlChainModel {
                Chain = crl
                    .Select(c => c.ToServiceModel())
                    .ToList()
            };
        }

        /// <inheritdoc/>
        public async Task<X509CertificateChainModel> GetIssuerCertificateChainAsync(
            string serialNumber, CancellationToken ct = default) {
            if (string.IsNullOrEmpty(serialNumber)) {
                throw new ArgumentNullException(nameof(serialNumber));
            }
            var issuerCertChain = await _store.ListCompleteCertificateChainAsync(
                SerialNumber.Parse(serialNumber).Value, ct);
            if (!issuerCertChain.Any()) {
                throw new ResourceNotFoundException(
                    "Certificate chain for serial number not found");
            }
            return new X509CertificateChainModel {
                Chain = issuerCertChain?
                    .Select(c => c.ToServiceModel())
                    .ToList()
            };
        }

        /// <inheritdoc/>
        public async Task RevokeCertificateAsync(X509CertificateModel certificate,
            CancellationToken ct) {
            var serialNumber = certificate.ToStackModel().SerialNumber;
            await _revoker.RevokeCertificateAsync(serialNumber, ct);
        }

        private readonly ICertificateRevoker _revoker;
        private readonly ICertificateStore _store;
        private readonly ICrlEndpoint _crls;
    }
}
