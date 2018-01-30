// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services {
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Models;
    using System.Threading.Tasks;

    /// <summary>
    /// Publish services
    /// </summary>
    public interface IOpcUaPublishServices {

        /// <summary>
        /// Publish or unpublish from node
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<PublishResultModel> NodePublishAsync(ServerEndpointModel endpoint,
            PublishRequestModel request);
    }
}