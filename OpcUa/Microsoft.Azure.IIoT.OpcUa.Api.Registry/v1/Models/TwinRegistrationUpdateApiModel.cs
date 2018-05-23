// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.v1.Models {
    using Microsoft.Azure.IIoT.OpcUa.Services.Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Twin registration update request
    /// </summary>
    public class TwinRegistrationUpdateApiModel {
        /// <summary>
        /// Default constructor
        /// </summary>
        public TwinRegistrationUpdateApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public TwinRegistrationUpdateApiModel(TwinRegistrationUpdateModel model) {
            Id = model.Id;
            Duplicate = model.Duplicate;
            Activate = model.Activate;
            Token = model.Token;
            TokenType = model.TokenType;
            User = model.User;
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public TwinRegistrationUpdateModel ToServiceModel() {
            return new TwinRegistrationUpdateModel {
                Id = Id,
                Duplicate = Duplicate,
                Activate = Activate,
                Token = Token,
                TokenType = TokenType,
                User = User
            };
        }

        /// <summary>
        /// Identifier of the twin to patch
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        [Required]
        public string Id { get; set; }

        /// <summary>
        /// Whether to copy existing registration
        /// rather than replacing, null == false
        /// </summary>
        [JsonProperty(PropertyName = "duplicate",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public bool? Duplicate { get; set; }

        /// <summary>
        /// Activate (=true) or disable twin (=false), if
        /// null, unchanged.
        /// </summary>
        [JsonProperty(PropertyName = "activate",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public bool? Activate { get; set; }

        /// <summary>
        /// User name to use - if null, unchanged, empty
        /// string to delete
        /// </summary>
        [JsonProperty(PropertyName = "user",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public string User { get; set; }

        /// <summary>
        /// User token to pass to server - if null, unchanged, empty
        /// string to delete
        /// </summary>
        [JsonProperty(PropertyName = "token",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public JToken Token { get; set; }

        /// <summary>
        /// Type of token - if null, unchanged
        /// </summary>
        [JsonProperty(PropertyName = "tokenType",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public TokenType? TokenType { get; set; }
    }
}
