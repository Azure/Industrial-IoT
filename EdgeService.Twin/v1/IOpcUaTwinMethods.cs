// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.EdgeService.v1 {
    using Microsoft.Azure.IoTSolutions.OpcTwin.EdgeService.v1.Models;
    using System.Threading.Tasks;

    /// <summary>
    /// V1 twin services
    /// </summary>
    public interface IOpcUaTwinMethods{

        /// <summary>
        /// Publish or unpublish from node
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<PublishResponseApiModel> PublishAsync(
            PublishRequestApiModel request);

        /// <summary>
        /// Browse nodes on endpoint
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<BrowseResponseApiModel> BrowseAsync(
            BrowseRequestApiModel request);

        /// <summary>
        /// Read node value
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<ValueReadResponseApiModel> ValueReadAsync(
            ValueReadRequestApiModel request);

        /// <summary>
        /// Write node value
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<ValueWriteResponseApiModel> ValueWriteAsync(
            ValueWriteRequestApiModel request);

        /// <summary>
        /// Get meta data for method call (input and output arguments)
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<MethodMetadataResponseApiModel> MethodMetadataAsync(
            MethodMetadataRequestApiModel request);

        /// <summary>
        /// Call method
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<MethodCallResponseApiModel> MethodCallAsync(
            MethodCallRequestApiModel request);
    }
}