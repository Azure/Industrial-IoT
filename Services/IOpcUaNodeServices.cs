// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services {
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Models;
    using System.Threading.Tasks;

    /// <summary>
    /// Node services
    /// </summary>
    public interface IOpcUaNodeServices {

        /// <summary>
        /// Read node value
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<ValueReadResultModel> NodeValueReadAsync(ServerEndpointModel endpoint,
            ValueReadRequestModel request);

        /// <summary>
        /// Write node value
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<ValueWriteResultModel> NodeValueWriteAsync(ServerEndpointModel endpoint,
            ValueWriteRequestModel request);

        /// <summary>
        /// Get meta data for method call (input and output arguments)
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<MethodMetadataResultModel> NodeMethodGetMetadataAsync(
            ServerEndpointModel endpoint, MethodMetadataRequestModel request);

        /// <summary>
        /// Call method
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<MethodCallResultModel> NodeMethodCallAsync(ServerEndpointModel endpoint,
            MethodCallRequestModel request);
    }
}