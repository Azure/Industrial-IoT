// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Graph.Models {
    using Gremlin.Net.CosmosDb.Structure;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Opc.Ua;

    /// <summary>
    /// Variable type node vertex
    /// </summary>
    [Label(AddressSpaceElementNames.VariableType)]
    public class VariableTypeNodeVertexModel : BaseNodeVertexModel {

        /// <summary>
        /// Whether type is abstract, if type can be abstract.
        /// </summary>
        [JsonProperty(PropertyName = "isAbstract",
            NullValueHandling = NullValueHandling.Ignore)]
        public bool? IsAbstract { get; set; }

        /// <summary>
        /// Value rank of the variable data of a variable
        /// or variable type, otherwise null.
        /// </summary>
        [JsonProperty(PropertyName = "valueRank",
            NullValueHandling = NullValueHandling.Ignore)]
        public NodeValueRank? ValueRank { get; set; }

        /// <summary>
        /// Array dimensions of variable or variable type.
        /// </summary>
        [JsonProperty(PropertyName = "arrayDimensions",
            NullValueHandling = NullValueHandling.Ignore)]
        public uint[] ArrayDimensions { get; set; }

        /// <summary>
        /// Default value of the subtyped variable in case node is a
        /// variable type.
        /// </summary>
        [JsonProperty(PropertyName = "value",
            NullValueHandling = NullValueHandling.Ignore)]
        public JToken Value { get; set; }

        /// <summary>
        /// Built in type of the value.
        /// </summary>
        [JsonProperty(PropertyName = "builtInType")]
        public BuiltInType BuiltInType { get; set; }

        /// <summary>
        /// If variable the datatype of the variable as link to
        /// data type node.
        /// </summary>
        public DataTypeEdgeModel DataType { get; set; }
    }
}
