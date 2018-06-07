// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api {
    using Microsoft.Azure.IIoT.OpcUa.Api.Models;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents OPC twin service api functions
    /// </summary>
    public interface IOpcUaTwinApi {

        /// <summary>
        /// Returns status of the service
        /// </summary>
        /// <returns></returns>
        Task<StatusResponseApiModel> GetServiceStatusAsync();

        /// <summary>
        /// Browse node on twin
        /// </summary>
        /// <param name="twinId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<BrowseResponseApiModel> NodeBrowseAsync(string twinId,
            BrowseRequestApiModel request);

        /// <summary>
        /// Browse next references on twin
        /// </summary>
        /// <param name="twinId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<BrowseNextResponseApiModel> NodeBrowseNextAsync(string twinId,
            BrowseNextRequestApiModel request);

        /// <summary>
        /// Call method on twin
        /// </summary>
        /// <param name="twinId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<MethodCallResponseApiModel> NodeMethodCallAsync(string twinId,
            MethodCallRequestApiModel request);

        /// <summary>
        /// Get meta data for method call on twin
        /// </summary>
        /// <param name="twinId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<MethodMetadataResponseApiModel> NodeMethodGetMetadataAsync(
            string twinId, MethodMetadataRequestApiModel request);

        /// <summary>
        /// Publish or unpublish node on twin
        /// </summary>
        /// <param name="twinId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<PublishResponseApiModel> NodePublishAsync(string twinId,
            PublishRequestApiModel request);

        /// <summary>
        /// Get list of published nodes on twin
        /// </summary>
        /// <param name="continuation"></param>
        /// <param name="twinId"></param>
        /// <returns></returns>
        Task<PublishedNodeListApiModel> ListPublishedNodesAsync(
            string continuation, string twinId);

        /// <summary>
        /// Read node value on twin
        /// </summary>
        /// <param name="twinId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<ValueReadResponseApiModel> NodeValueReadAsync(string twinId,
            ValueReadRequestApiModel request);

        /// <summary>
        /// Write node value on twin
        /// </summary>
        /// <param name="twinId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<ValueWriteResponseApiModel> NodeValueWriteAsync(string twinId,
            ValueWriteRequestApiModel request);
    }
}