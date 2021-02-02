// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher {
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Publish services
    /// </summary>
    public interface IPublishServices<T> {

        /// <summary>
        /// Start publishing node values
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="ct"> Cancellation token </param>
        /// <returns></returns>
        Task<PublishStartResultModel> NodePublishStartAsync(T endpoint,
            PublishStartRequestModel request, CancellationToken ct = default);

        /// <summary>
        /// Stop publishing node values
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="ct"> Cancellation token </param>
        /// <returns></returns>
        Task<PublishStopResultModel> NodePublishStopAsync(T endpoint,
            PublishStopRequestModel request, CancellationToken ct = default);

        /// <summary>
        /// Configure nodes to publish and unpublish in bulk
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="ct"> Cancellation token </param>
        /// <returns></returns>
        Task<PublishBulkResultModel> NodePublishBulkAsync(T endpoint,
            PublishBulkRequestModel request, CancellationToken ct = default);

        /// <summary>
        /// Get all published nodes for endpoint.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="ct"> Cancellation token </param>
        /// <returns></returns>
        Task<PublishedItemListResultModel> NodePublishListAsync(T endpoint,
            PublishedItemListRequestModel request, CancellationToken ct = default);
    }
}
