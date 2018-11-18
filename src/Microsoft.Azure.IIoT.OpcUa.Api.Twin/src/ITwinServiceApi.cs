// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Twin {
    using Microsoft.Azure.IIoT.OpcUa.Api.Twin.Models;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents OPC twin service api functions
    /// </summary>
    public interface ITwinServiceApi {

        /// <summary>
        /// Returns status of the service
        /// </summary>
        /// <returns></returns>
        Task<StatusResponseApiModel> GetServiceStatusAsync();

        /// <summary>
        /// Browse node on endpoint
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<BrowseResponseApiModel> NodeBrowseAsync(string endpointId,
            BrowseRequestApiModel request);

        /// <summary>
        /// Browse next references on endpoint
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<BrowseNextResponseApiModel> NodeBrowseNextAsync(string endpointId,
            BrowseNextRequestApiModel request);

        /// <summary>
        /// Browse by path on endpoint
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<BrowsePathResponseApiModel> NodeBrowsePathAsync(string endpointId,
            BrowsePathRequestApiModel request);

        /// <summary>
        /// Call method on endpoint
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<MethodCallResponseApiModel> NodeMethodCallAsync(string endpointId,
            MethodCallRequestApiModel request);

        /// <summary>
        /// Get meta data for method call on endpoint
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<MethodMetadataResponseApiModel> NodeMethodGetMetadataAsync(
            string endpointId, MethodMetadataRequestApiModel request);

        /// <summary>
        /// Publish or unpublish node on endpoint
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<PublishResponseApiModel> NodePublishAsync(string endpointId,
            PublishRequestApiModel request);

        /// <summary>
        /// Get list of published nodes on endpoint
        /// </summary>
        /// <param name="continuation"></param>
        /// <param name="endpointId"></param>
        /// <returns></returns>
        Task<PublishedNodeListApiModel> ListPublishedNodesAsync(
            string continuation, string endpointId);

        /// <summary>
        /// Read node value on endpoint
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<ValueReadResponseApiModel> NodeValueReadAsync(string endpointId,
            ValueReadRequestApiModel request);

        /// <summary>
        /// Write node value on endpoint
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<ValueWriteResponseApiModel> NodeValueWriteAsync(string endpointId,
            ValueWriteRequestApiModel request);
    }
}
