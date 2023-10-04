// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Certificate api
    /// </summary>
    public interface IOpcUaCertificates
    {
        /// <summary>
        /// Enumerate certificates
        /// </summary>
        /// <param name="store"></param>
        /// <param name="includePrivateKey"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<IReadOnlyList<X509CertificateModel>> ListCertificatesAsync(
            CertificateStoreName store, bool includePrivateKey = false,
            CancellationToken ct = default);

        /// <summary>
        /// Add certificate pfx to store
        /// </summary>
        /// <param name="store"></param>
        /// <param name="pfxBlob"></param>
        /// <param name="password"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task AddCertificateAsync(CertificateStoreName store, byte[] pfxBlob,
            string? password = null, CancellationToken ct = default);

        /// <summary>
        /// Add certificate to store
        /// </summary>
        /// <param name="store"></param>
        /// <param name="certificateChain"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task AddCertificateChainAsync(CertificateStoreName store,
            byte[] certificateChain, CancellationToken ct = default);

        /// <summary>
        /// Remove certificate from store
        /// </summary>
        /// <param name="store"></param>
        /// <param name="thumbprint"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task RemoveCertificateAsync(CertificateStoreName store,
            string thumbprint, CancellationToken ct = default);

        /// <summary>
        /// Approve a rejected certificate
        /// </summary>
        /// <param name="thumbprint"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task ApproveRejectedCertificateAsync(string thumbprint,
            CancellationToken ct = default);

        /// <summary>
        /// List certificate revocation lists
        /// </summary>
        /// <param name="store"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<IReadOnlyList<byte[]>> ListCertificateRevocationListsAsync(
            CertificateStoreName store, CancellationToken ct = default);
    }
}
