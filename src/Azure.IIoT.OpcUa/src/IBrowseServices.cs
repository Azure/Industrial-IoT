// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa {
    using Azure.IIoT.OpcUa.Api.Models;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Browse services via handle
    /// </summary>
    public interface IBrowseServices<T> {

        /// <summary>
        /// Browse nodes on endpoint
        /// </summary>
        /// <param name="endpoint">Endpoint url of the server
        /// to talk to</param>
        /// <param name="request">Browse request</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<BrowseResponseModel> NodeBrowseFirstAsync(T endpoint,
            BrowseRequestModel request, CancellationToken ct = default);

        /// <summary>
        /// Browse remainder of references
        /// </summary>
        /// <param name="endpoint">Endpoint url of the server
        /// to talk to</param>
        /// <param name="request">Continuation token</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<BrowseNextResponseModel> NodeBrowseNextAsync(T endpoint,
            BrowseNextRequestModel request, CancellationToken ct = default);

        /// <summary>
        /// Browse by path
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<BrowsePathResponseModel> NodeBrowsePathAsync(T endpoint,
            BrowsePathRequestModel request, CancellationToken ct = default);
    }
}
