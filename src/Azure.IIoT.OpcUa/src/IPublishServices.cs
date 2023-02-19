// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa {
    using Azure.IIoT.OpcUa.Shared.Models;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Publish services
    /// </summary>
    public interface IPublishServices<T> {

        /// <summary>
        /// Start publishing values from a node
        /// </summary>
        /// <param name="id"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<PublishStartResponseModel> NodePublishStartAsync(T id,
            PublishStartRequestModel request, CancellationToken ct = default);

        /// <summary>
        /// Stop publishing values from a node
        /// </summary>
        /// <param name="id"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<PublishStopResponseModel> NodePublishStopAsync(T id,
            PublishStopRequestModel request, CancellationToken ct = default);

        /// <summary>
        /// Configure node values to publish and unpublish in bulk
        /// </summary>
        /// <param name="id"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<PublishBulkResponseModel> NodePublishBulkAsync(T id,
            PublishBulkRequestModel request, CancellationToken ct = default);

        /// <summary>
        /// Get all published nodes for a server endpoint.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<PublishedItemListResponseModel> NodePublishListAsync(T id,
            PublishedItemListRequestModel request, CancellationToken ct = default);
    }
}
