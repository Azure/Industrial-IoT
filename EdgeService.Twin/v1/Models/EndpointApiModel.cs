// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.EdgeService.v1.Models {
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.Models;

    /// <summary>
    /// Endpoint model for edgeservice api
    /// </summary>
    public class EndpointApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public EndpointApiModel() {}

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public EndpointApiModel(EndpointModel model) {
            Url = model?.Url;
            User = model?.User;
            Token = model?.Token;
            TokenType = model?.TokenType;
            IsTrusted = model?.IsTrusted;
            SecurityMode = model?.SecurityMode;
            SecurityPolicy = model?.SecurityPolicy;
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public EndpointModel ToServiceModel() {
            return new EndpointModel {
                Url = Url,
                User = User,
                Token = Token,
                TokenType = TokenType,
                IsTrusted = IsTrusted,
                SecurityMode = SecurityMode,
                SecurityPolicy = SecurityPolicy,
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
        public TokenType? TokenType { get; set; }

        /// <summary>
        /// Endpoint security policy to use - null = Best.
        /// </summary>
        public string SecurityPolicy { get; set; }

        /// <summary>
        /// Security mode to use for communication - null = Best
        /// </summary>
        public SecurityMode? SecurityMode { get; set; }

        /// <summary>
        /// Enable (=true) or disable twin
        /// </summary>
        public bool? IsTrusted { get; set; }
    }
}
