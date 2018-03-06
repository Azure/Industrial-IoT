// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.EdgeService.v1 {
    using Microsoft.Azure.IoTSolutions.OpcTwin.EdgeService.v1.Models;
    using System.Threading.Tasks;

    /// <summary>
    /// V1 supervisor services
    /// </summary>
    public interface IOpcUaSupervisorMethods {

        /// <summary>
        /// Validates and and returns validation response.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        Task<ServerEndpointApiModel> ValidateAsync(
            EndpointApiModel endpoint);

        /// <summary>
        /// Browse nodes on endpoint
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<BrowseResponseApiModel> BrowseAsync(
            EndpointApiModel endpoint, BrowseRequestApiModel request);

        /// <summary>
        /// Read node value
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<ValueReadResponseApiModel> ValueReadAsync(
            EndpointApiModel endpoint, ValueReadRequestApiModel request);

        /// <summary>
        /// Write node value
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<ValueWriteResponseApiModel> ValueWriteAsync(
            EndpointApiModel endpoint, ValueWriteRequestApiModel request);

        /// <summary>
        /// Get meta data for method call (input and output arguments)
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<MethodMetadataResponseApiModel> MethodMetadataAsync(
            EndpointApiModel endpoint, MethodMetadataRequestApiModel request);

        /// <summary>
        /// Call method
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<MethodCallResponseApiModel> MethodCallAsync(
            EndpointApiModel endpoint, MethodCallRequestApiModel request);
    }
}