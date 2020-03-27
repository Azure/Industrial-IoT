// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Endpoint registry
    /// </summary>
    public interface IEndpointRegistry {

        /// <summary>
        /// Get all endpoints in paged form
        /// </summary>
        /// <param name="continuation"></param>
        /// <param name="onlyServerState">Whether only
        /// desired endpoint state should be returned.
        /// </param>
        /// <param name="pageSize"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<EndpointInfoListModel> ListEndpointsAsync(string continuation,
            bool onlyServerState = false, int? pageSize = null,
            CancellationToken ct = default);

        /// <summary>
        /// Find registration of the supplied endpoint.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="onlyServerState">Whether only
        /// desired endpoint state should be returned.
        /// </param>
        /// <param name="pageSize"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<EndpointInfoListModel> QueryEndpointsAsync(
            EndpointRegistrationQueryModel query,
            bool onlyServerState = false, int? pageSize = null,
            CancellationToken ct = default);

        /// <summary>
        /// Get endpoint registration by identifer.
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="onlyServerState">Whether only
        /// desired endpoint state should be returned.
        /// </param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<EndpointInfoModel> GetEndpointAsync(string endpointId,
            bool onlyServerState = false,
            CancellationToken ct = default);

        /// <summary>
        /// Returns the endpoint certificate
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<X509CertificateChainModel> GetEndpointCertificateAsync(
            string endpointId, CancellationToken ct = default);
    }
}
