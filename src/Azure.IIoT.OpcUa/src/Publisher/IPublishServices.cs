// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Publish services
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IPublishServices<T>
    {
        /// <summary>
        /// Start publishing values from a node
        /// </summary>
        /// <param name="endpoint">Server endpoint to talk to</param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<PublishStartResponseModel> PublishStartAsync(T endpoint,
            PublishStartRequestModel request, CancellationToken ct = default);

        /// <summary>
        /// Stop publishing values from a node
        /// </summary>
        /// <param name="endpoint">Server endpoint to talk to</param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<PublishStopResponseModel> PublishStopAsync(T endpoint,
            PublishStopRequestModel request, CancellationToken ct = default);

        /// <summary>
        /// Configure node values to publish and unpublish in bulk
        /// </summary>
        /// <param name="endpoint">Server endpoint to talk to</param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<PublishBulkResponseModel> PublishBulkAsync(T endpoint,
            PublishBulkRequestModel request, CancellationToken ct = default);

        /// <summary>
        /// Get all published nodes for a server endpoint.
        /// </summary>
        /// <param name="endpoint">Server endpoint to talk to</param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<PublishedItemListResponseModel> PublishListAsync(T endpoint,
            PublishedItemListRequestModel request, CancellationToken ct = default);
    }
}
