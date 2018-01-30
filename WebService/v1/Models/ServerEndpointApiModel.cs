// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcUaExplorer.WebService.v1.Models {
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;

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
            };
        }

        /// <summary>
        /// Endpoint
        /// </summary>
        [JsonProperty(PropertyName = "url")]
        [Required]
        public string Url { get; set; }

        /// <summary>
        /// User name to use
        /// </summary>
        [JsonProperty(PropertyName = "user",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public string User { get; set; }

        /// <summary>
        /// User token to pass to server
        /// </summary>
        [JsonProperty(PropertyName = "token",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public object Token { get; set; }

        /// <summary>
        /// Type of token
        /// </summary>
        [JsonProperty(PropertyName = "tokenType",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(TokenType.None)]
        public TokenType Type { get; set; }

        /// <summary>
        /// Implict trust of the other side
        /// </summary>
        [JsonProperty(PropertyName = "isTrusted",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(false)]
        public bool? IsTrusted { get; set; }
    }
}
