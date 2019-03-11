// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Graph.Models {
    using Gremlin.Net.CosmosDb.Structure;
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Opc.Ua;

    /// <summary>
    /// Variable type node vertex
    /// </summary>
    [Label(AddressSpaceElementNames.VariableType)]
    public class VariableTypeNodeVertexModel : NodeVertexModel {

        /// <summary>
        /// Whether type is abstract, if type can be abstract.
        /// </summary>
        [JsonProperty(PropertyName = "isAbstract")]
        public bool? IsAbstract { get; set; }

        /// <summary>
        /// Value rank of the variable data of a variable
        /// or variable type, otherwise null.
        /// </summary>
        [JsonProperty(PropertyName = "valueRank")]
        public NodeValueRank? ValueRank { get; set; }

        /// <summary>
        /// Array dimensions of variable or variable type.
        /// </summary>
        [JsonProperty(PropertyName = "arrayDimensions")]
        public uint[] ArrayDimensions { get; set; }

        /// <summary>
        /// Default value of the subtyped variable in case node is a
        /// variable type.
        /// </summary>
        [JsonProperty(PropertyName = "value")]
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
