// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.v2.Models {
    using Microsoft.Azure.IIoT.Auth.Models;
    using Newtonsoft.Json;
    using System;

    /// <summary>
    /// Identity token
    /// </summary>
    public class IdentityTokenApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public IdentityTokenApiModel() { }

        /// <summary>
        /// Create twin model
        /// </summary>
        /// <param name="model"></param>
        public IdentityTokenApiModel(IdentityTokenModel model) {
            Identity = model?.Identity;
            Key = model?.Key;
            Expires = model?.Expires ?? DateTime.MinValue;
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <returns></returns>
        public IdentityTokenModel ToServiceModel() {
            return new IdentityTokenModel {
                Identity = Identity,
                Key = Key,
                Expires = Expires
            };
        }

        /// <summary>
        /// Identity
        /// </summary>
        [JsonProperty(PropertyName = "identity")]
        public string Identity { get; set; }

        /// <summary>
        /// Key
        /// </summary>
        [JsonProperty(PropertyName = "key")]
        public string Key { get; set; }

        /// <summary>
        /// Expiration
        /// </summary>
        [JsonProperty(PropertyName = "expires")]
        public DateTime Expires { get; set; }
    }
}