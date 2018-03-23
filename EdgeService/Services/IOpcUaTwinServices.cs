// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.EdgeService {
    using Microsoft.Azure.IoTSolutions.OpcTwin.EdgeService.Models;
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.Models;
    using System.Threading.Tasks;

    /// <summary>
    /// Specialized services provided by opc ua twin-
    /// </summary>
    public interface IOpcUaTwinServices {

        /// <summary>
        /// Endpoint
        /// </summary>
        TwinEndpointModel Endpoint { get; }

        /// <summary>
        /// Called to update endpoint
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        Task SetEndpointAsync(TwinEndpointModel endpoint);

        /// <summary>
        /// Process publish request
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<PublishResultModel> NodePublishAsync(
            PublishRequestModel request);

        /// <summary>
        /// Start/Stop publishing
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="enable"></param>
        /// <returns></returns>
        Task NodePublishAsync(string nodeId, bool? enable);
    }
}