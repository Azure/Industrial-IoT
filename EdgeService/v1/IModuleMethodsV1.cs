// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcUaExplorer.EdgeService.v1 {
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.EdgeService.v1.Models;
    using System.Threading.Tasks;

    /// <summary>
    /// V1 edge services
    /// </summary>
    public interface IModuleMethodsV1 {

        /// <summary>
        /// Validates and fills out remainder of the server registration
        /// request.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<ServerRegistrationRequestApiModel> ValidateAsync(
            ServerRegistrationRequestApiModel request);

        /// <summary>
        /// Publish or unpublish from node
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<PublishResponseApiModel> NodePublishAsync(
            ServerEndpointApiModel endpoint, PublishRequestApiModel request);

        /// <summary>
        /// Browse nodes on endpoint
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<BrowseResponseApiModel> NodeBrowseAsync(
            ServerEndpointApiModel endpoint, BrowseRequestApiModel request);

        /// <summary>
        /// Read node value
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<ValueReadResponseApiModel> NodeValueReadAsync(
            ServerEndpointApiModel endpoint, ValueReadRequestApiModel request);

        /// <summary>
        /// Write node value
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<ValueWriteResponseApiModel> NodeValueWriteAsync(
            ServerEndpointApiModel endpoint, ValueWriteRequestApiModel request);

        /// <summary>
        /// Get meta data for method call (input and output arguments)
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<MethodMetadataResponseApiModel> NodeMethodGetMetadataAsync(
            ServerEndpointApiModel endpoint, MethodMetadataRequestApiModel request);

        /// <summary>
        /// Call method
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<MethodCallResponseApiModel> NodeMethodCallAsync(
            ServerEndpointApiModel endpoint, MethodCallRequestApiModel request);
    }
}