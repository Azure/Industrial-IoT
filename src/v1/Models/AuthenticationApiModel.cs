// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Modules.Twin.v1.Models {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Newtonsoft.Json.Linq;

    public class AuthenticationApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public AuthenticationApiModel() { }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public AuthenticationApiModel(AuthenticationModel model) {
            User = model?.User;
            Token = model?.Token;
            TokenType = model?.TokenType;
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public AuthenticationModel ToServiceModel() {
            return new AuthenticationModel {
                User = User,
                Token = Token,
                TokenType = TokenType
            };
        }

        /// <summary>
        /// User name to use
        /// </summary>
        public string User { get; set; }

        /// <summary>
        /// Type of token
        /// </summary>
        public TokenType? TokenType { get; set; }

        /// <summary>
        /// User token to pass to server
        /// </summary>
        public JToken Token { get; set; }
    }
}
