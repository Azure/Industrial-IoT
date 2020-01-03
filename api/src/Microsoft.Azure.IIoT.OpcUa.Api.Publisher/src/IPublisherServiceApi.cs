// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher {
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents OPC twin service api functions
    /// </summary>
    public interface IPublisherServiceApi {

        /// <summary>
        /// Returns status of the service
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<StatusResponseApiModel> GetServiceStatusAsync(
            CancellationToken ct = default);

        /// <summary>
        /// Start publishing node values
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<PublishStartResponseApiModel> NodePublishStartAsync(string endpointId,
            PublishStartRequestApiModel request, CancellationToken ct = default);

        /// <summary>
        /// Start publishing node values
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<PublishStopResponseApiModel> NodePublishStopAsync(string endpointId,
            PublishStopRequestApiModel request, CancellationToken ct = default);

        /// <summary>
        /// Get all published nodes for endpoint.
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<PublishedItemListResponseApiModel> NodePublishListAsync(string endpointId,
            PublishedItemListRequestApiModel request, CancellationToken ct = default);

        /// <summary>
        /// Subscribe client to receive published samples
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="userId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task NodePublishSubscribeByEndpointAsync(string endpointId, string userId,
            CancellationToken ct = default);

        /// <summary>
        /// Unsubscribe client from receiving samples
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="userId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task NodePublishUnsubscribeByEndpointAsync(string endpointId, string userId,
            CancellationToken ct = default);
    }
}
