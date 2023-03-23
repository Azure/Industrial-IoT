// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.Sdk
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents OPC twin service api functions
    /// </summary>
    public interface IPublisherServiceApi
    {
        /// <summary>
        /// Returns status of the service
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<string> GetServiceStatusAsync(CancellationToken ct = default);

        /// <summary>
        /// Start publishing node values
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<PublishStartResponseModel> NodePublishStartAsync(string endpointId,
            PublishStartRequestModel request, CancellationToken ct = default);

        /// <summary>
        /// Start publishing node values
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<PublishStopResponseModel> NodePublishStopAsync(string endpointId,
            PublishStopRequestModel request, CancellationToken ct = default);

        /// <summary>
        /// Add or remove published node from endpoint in bulk
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<PublishBulkResponseModel> NodePublishBulkAsync(string endpointId,
            PublishBulkRequestModel request, CancellationToken ct = default);

        /// <summary>
        /// Get all published nodes for endpoint.
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<PublishedItemListResponseModel> NodePublishListAsync(string endpointId,
            PublishedItemListRequestModel request, CancellationToken ct = default);
    }
}
