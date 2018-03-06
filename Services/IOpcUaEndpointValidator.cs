// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.Services {
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.Models;
    using System.Threading.Tasks;

    /// <summary>
    /// Internal stack services
    /// </summary>
    public interface IOpcUaEndpointValidator {

        /// <summary>
        /// Validates and fills out remainder of the server
        /// endpoint.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        Task<ServerEndpointModel> ValidateAsync(
            EndpointModel endpoint);
    }
}