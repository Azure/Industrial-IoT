// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services {
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Models;
    using System.Threading.Tasks;

    /// <summary>
    /// Browse services
    /// </summary>
    public interface IOpcUaBrowseServices {

        /// <summary>
        /// Browse nodes on endpoint
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<BrowseResultModel> NodeBrowseAsync(ServerEndpointModel endpoint,
            BrowseRequestModel request);
    }
}