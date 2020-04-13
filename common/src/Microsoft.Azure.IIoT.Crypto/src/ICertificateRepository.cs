// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto {
    using Microsoft.Azure.IIoT.Crypto.Models;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Certificate repository - this allows an issuer to add
    /// or remove certificates in the certificate database.
    /// </summary>
    public interface ICertificateRepository {

        /// <summary>
        /// Add certificate and returns id of the certificate in
        /// the repository.
        /// </summary>
        /// <param name="certificateName"></param>
        /// <param name="certificate"></param>
        /// <param name="id"></param>
        /// <param name="ct"></param>
        /// <returns>Id of the certificate</returns>
        Task AddCertificateAsync(string certificateName,
            Certificate certificate, string id = null,
            CancellationToken ct = default);

        /// <summary>
        /// Find certificate with the registered id in certificate
        /// repository.
        /// </summary>
        /// <param name="id">Id of certificate</param>
        /// <param name="ct">CancellationToken</param>
        /// <returns></returns>
        Task<Certificate> FindCertificateAsync(
            string id, CancellationToken ct = default);

        /// <summary>
        /// Disable certificate and returns the certificate
        /// identifier under which it was added.
        /// </summary>
        /// <param name="certificate"></param>
        /// <param name="ct"></param>
        /// <returns>Certificate identifier</returns>
        Task<string> DisableCertificateAsync(
            Certificate certificate, CancellationToken ct = default);
    }
}