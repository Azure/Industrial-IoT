// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Twin.Models {
    using Newtonsoft.Json;

    /// <summary>
    /// Node publis request webservice api model
    /// </summary>
    public class PublishRequestApiModel {

        /// <summary>
        /// Node to publish or unpublish
        /// </summary>
        [JsonProperty(PropertyName = "nodeId")]
        public string NodeId { get; set; }

        /// <summary>
        /// Whether to enable or disable
        /// </summary>
        [JsonProperty(PropertyName = "enabled",
            NullValueHandling = NullValueHandling.Ignore)]
        public bool? Enabled { get; set; }
    }
}
