// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.EdgeService.v1.Models {
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.Models;

    /// <summary>
    /// Endpoint with server info
    /// </summary>
    public class ServerEndpointApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public ServerEndpointApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public ServerEndpointApiModel(ServerEndpointModel model) {
            Server = new ServerInfoApiModel(model?.Server);
            Endpoint = new EndpointApiModel(model?.Endpoint);
        }

        /// <summary>
        /// Server of the endpoint
        /// </summary>
        public ServerInfoApiModel Server { get; set; }

        /// <summary>
        /// Endoint validated
        /// </summary>
        public EndpointApiModel Endpoint { get; set; }
    }
}
