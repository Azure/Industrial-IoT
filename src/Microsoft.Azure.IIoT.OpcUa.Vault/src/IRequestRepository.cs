// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault {
    using Microsoft.Azure.IIoT.OpcUa.Vault.Models;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Request repo
    /// </summary>
    public interface IRequestRepository {

        /// <summary>
        /// Add request
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<CertificateRequestModel> AddAsync(
            CertificateRequestModel request,
            CancellationToken ct = default);

        /// <summary>
        /// Get request
        /// </summary>
        /// <param name="requestId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<CertificateRequestModel> FindAsync(string requestId,
            CancellationToken ct = default);

        /// <summary>
        /// Update request
        /// </summary>
        /// <param name="requestId"></param>
        /// <param name="predicate"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<CertificateRequestModel> UpdateAsync(string requestId,
            Func<CertificateRequestModel, bool> predicate,
            CancellationToken ct = default);

        /// <summary>
        /// Delete request
        /// </summary>
        /// <param name="requestId"></param>
        /// <param name="predicate"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<CertificateRequestModel> DeleteAsync(string requestId,
            Func<CertificateRequestModel, bool> predicate,
            CancellationToken ct = default);

        /// <summary>
        /// Query requests
        /// </summary>
        /// <param name="query"></param>
        /// <param name="nextPageLink"></param>
        /// <param name="maxResults"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<CertificateRequestListModel> QueryAsync(
            CertificateRequestQueryRequestModel query,
            string nextPageLink = null, int? maxResults = null,
            CancellationToken ct = default);
    }
}