// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcUaExplorer.EdgeService.v1.Models {
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Models;

    /// <summary>
    /// Endpoint model for webservice api
    /// </summary>
    public class ServerEndpointApiModel {
        /// <summary>
        /// Default constructor
        /// </summary>
        public ServerEndpointApiModel() {}

        /// <summary>
        /// Create endpoint api model from service model
        /// </summary>
        /// <param name="model"></param>
        public ServerEndpointApiModel(ServerEndpointModel model) {
            Url = model.Url;
            User = model.User;
            Token = model.Token;
            Type = model.Type;
            IsTrusted = model.IsTrusted;
            EdgeController = model.EdgeController;
        }

        /// <summary>
        /// Create endpoint api model from node model
        /// </summary>
        public ServerEndpointModel ToServiceModel() {
            return new ServerEndpointModel {
                Url = Url,
                User = User,
                Token = Token,
                Type = Type,
                IsTrusted = IsTrusted,
                EdgeController = EdgeController
            };
        }
        
        /// <summary>
        /// Endpoint
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// User name to use
        /// </summary>
        public string User { get; set; }

        /// <summary>
        /// User token to pass to server
        /// </summary>
        public object Token { get; set; }

        /// <summary>
        /// Type of token
        /// </summary>
        public TokenType Type { get; set; }

        /// <summary>
        /// Implict trust of the other side
        /// </summary>
        public bool? IsTrusted { get; set; }

        /// <summary>
        /// Edge service targeted
        /// </summary>
        public string EdgeController { get; set; }
    }
}
