// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault {
    using Microsoft.Azure.IIoT.OpcUa.Vault.Models;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Certificate authority services
    /// </summary>
    public interface ICertificateAuthority {

        /// <summary>
        /// Revoke a single certificate.
        /// </summary>
        /// <param name="certificate">The certificate to revoke
        /// </param>
        /// <param name="ct"></param>
        /// <returns>The new CRL version</returns>
        Task RevokeCertificateAsync(X509CertificateModel certificate,
            CancellationToken ct = default);

        /// <summary>
        /// Get the CRLs for all certificates in the Issuer CA chain.
        /// </summary>
        /// <param name="serialNumber">serial number</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<X509CrlChainModel> GetIssuerCrlChainAsync(
            string serialNumber, CancellationToken ct = default);

        /// <summary>
        /// Get all certificates in the chain of the Issuer CA.
        /// </summary>
        /// <param name="serialNumber">serial number of
        /// certificate</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<X509CertificateChainModel> GetIssuerCertificateChainAsync(
            string serialNumber, CancellationToken ct = default);
    }
}
