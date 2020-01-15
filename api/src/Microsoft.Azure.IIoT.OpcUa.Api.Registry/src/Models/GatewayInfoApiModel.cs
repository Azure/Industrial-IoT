// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {
    using Newtonsoft.Json;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Gateway info model
    /// </summary>
    public class GatewayInfoApiModel {

        /// <summary>
        /// Gateway identity
        /// </summary>
        [JsonProperty(PropertyName = "gateway")]
        [Required]
        public GatewayApiModel Gateway { get; set; }

        /// <summary>
        /// Gateway modules
        /// </summary>
        [JsonProperty(PropertyName = "modules",
            NullValueHandling = NullValueHandling.Ignore)]
        public GatewayModulesApiModel Modules { get; set; }
    }
}
