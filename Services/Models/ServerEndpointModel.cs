// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.Services.Models {

    /// <summary>
    /// Endpoint with server info
    /// </summary>
    public class ServerEndpointModel {

        /// <summary>
        /// Server info for the endpoint
        /// </summary>
        public ServerInfoModel Server { get; set; }

        /// <summary>
        /// Endoint validated
        /// </summary>
        public EndpointModel Endpoint { get; set; }
    }
}
