// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Twin.Models {
    using Newtonsoft.Json;

    /// <summary>
    /// Node path target
    /// </summary>
    public class NodePathTargetApiModel {

        /// <summary>
        /// Target node
        /// </summary>
        [JsonProperty(PropertyName = "target")]
        public NodeApiModel Target { get; set; }

        /// <summary>
        /// Remaining index in path
        /// </summary>
        [JsonProperty(PropertyName = "remainingPathIndex",
            NullValueHandling = NullValueHandling.Ignore)]
        public int? RemainingPathIndex { get; set; }
    }
}
