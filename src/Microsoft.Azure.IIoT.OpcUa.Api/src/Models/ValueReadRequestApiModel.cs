// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Models {
    using Newtonsoft.Json;

    /// <summary>
    /// Node value read request webservice api model
    /// </summary>
    public class ValueReadRequestApiModel {

        /// <summary>
        /// Node to read
        /// </summary>
        [JsonProperty(PropertyName = "nodeId")]
        public string NodeId { get; set; }

        /// <summary>
        /// Optional User elevation
        /// </summary>
        [JsonProperty(PropertyName = "elevation",
            NullValueHandling = NullValueHandling.Ignore)]
        public AuthenticationApiModel Elevation { get; set; }
    }
}
