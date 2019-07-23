// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault {
    using Microsoft.Azure.IIoT.OpcUa.Vault.Models;
    using System.Threading;
    using System.Threading.Tasks;


    /// <summary>
    /// Certificate Request management
    /// </summary>
    public interface IRequestManagement {

        /// <summary>
        /// Manager approves the certificate request.
        /// </summary>
        /// <param name="requestId">The request Id</param>
        /// <param name="context"></param>
        /// <param name="ct"></param>
        Task ApproveRequestAsync(string requestId, VaultOperationContextModel context,
            CancellationToken ct = default);

        /// <summary>
        /// Mananger rejects a certificate request.
        /// </summary>
        /// <param name="requestId">The request Id</param>
        /// <param name="context"></param>
        /// <param name="ct"></param>
        Task RejectRequestAsync(string requestId, VaultOperationContextModel context,
            CancellationToken ct = default);

        /// <summary>
        /// Client accepts the results of the certificate request.
        /// </summary>
        /// <param name="requestId">The request Id</param>
        /// <param name="context"></param>
        /// <param name="ct"></param>
        Task AcceptRequestAsync(string requestId, VaultOperationContextModel context,
            CancellationToken ct = default);

        /// <summary>
        /// The accepted request is deleted from the database.
        /// </summary>
        /// <param name="requestId">The request Id</param>
        /// <param name="context"></param>
        /// <param name="ct"></param>
        Task DeleteRequestAsync(string requestId, VaultOperationContextModel context,
            CancellationToken ct = default);

        /// <summary>
        /// Read a certificate request.
        /// Returns only public information, e.g. signed certificate.
        /// </summary>
        /// <param name="requestId"></param>
        /// <param name="ct"></param>
        /// <returns>The request</returns>
        Task<CertificateRequestRecordModel> GetRequestAsync(
            string requestId, CancellationToken ct = default);

        /// <summary>
        /// Query the certificate request database.
        /// </summary>
        /// <param name="query">Optional: Filter info</param>
        /// <param name="maxResults">max number of results</param>
        /// <param name="ct"></param>
        /// <returns>List of certificate requests, next page link
        /// </returns>
        Task<CertificateRequestQueryResultModel> QueryRequestsAsync(
            CertificateRequestQueryRequestModel query, int? maxResults = null,
            CancellationToken ct = default);

        /// <summary>
        /// List the certificate requests.
        /// </summary>
        /// <param name="nextPageLink">The next page</param>
        /// <param name="maxResults">max number of results</param>
        /// <param name="ct"></param>
        /// <returns>List of certificate requests, next page link
        /// </returns>
        Task<CertificateRequestQueryResultModel> ListRequestsAsync(
            string nextPageLink = null, int? maxResults = null,
            CancellationToken ct = default);
    }
}
