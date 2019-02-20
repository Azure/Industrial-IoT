// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Graph.Models {
    using Gremlin.Net.CosmosDb.Structure;
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Newtonsoft.Json;

    /// <summary>
    /// Variable node vertex - note that isabstract is always null
    /// </summary>
    [Label(AddressSpaceElementNames.Variable)]
    public class VariableNodeVertexModel : VariableTypeNodeVertexModel {

        /// <summary>
        /// Default access level for value in variable
        /// node or null if not a variable.
        /// </summary>
        [JsonProperty(PropertyName = "accessLevel")]
        public NodeAccessLevel? AccessLevel { get; set; }

        /// <summary>
        /// Whether the value of a variable is historizing.
        /// (default: false)
        /// </summary>
        [JsonProperty(PropertyName = "historizing")]
        public bool? Historizing { get; set; }

        /// <summary>
        /// Minimum sampling interval for the variable
        /// value, otherwise null if not a variable node.
        /// </summary>
        [JsonProperty(PropertyName = "minimumSamplingInterval")]
        public double? MinimumSamplingInterval { get; set; }
    }
}
