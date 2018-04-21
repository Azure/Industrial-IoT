// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcTwin.Services {
    using Microsoft.Azure.IIoT.OpcTwin.Services.Models;
    using System.Threading.Tasks;

    /// <summary>
    /// Node services
    /// </summary>
    public interface IOpcUaTwinNodeServices {

        /// <summary>
        /// Read node value
        /// </summary>
        /// <param name="twinId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<ValueReadResultModel> NodeValueReadAsync(string twinId,
            ValueReadRequestModel request);

        /// <summary>
        /// Write node value
        /// </summary>
        /// <param name="twinId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<ValueWriteResultModel> NodeValueWriteAsync(string twinId,
            ValueWriteRequestModel request);

        /// <summary>
        /// Get meta data for method call (input and output arguments)
        /// </summary>
        /// <param name="twinId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<MethodMetadataResultModel> NodeMethodGetMetadataAsync(
            string twinId, MethodMetadataRequestModel request);

        /// <summary>
        /// Call method
        /// </summary>
        /// <param name="twinId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<MethodCallResultModel> NodeMethodCallAsync(string twinId,
            MethodCallRequestModel request);
    }
}