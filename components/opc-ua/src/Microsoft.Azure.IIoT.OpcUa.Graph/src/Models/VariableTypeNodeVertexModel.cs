// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Graph.Models {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.Serializers;
    using Gremlin.Net.CosmosDb.Structure;
    using System.Runtime.Serialization;
    using Opc.Ua;

    /// <summary>
    /// Variable type node vertex
    /// </summary>
    [Label(AddressSpaceElementNames.VariableType)]
    [DataContract]
    public class VariableTypeNodeVertexModel : BaseNodeVertexModel {

        /// <summary>
        /// Whether type is abstract, if type can be abstract.
        /// </summary>
        [DataMember(Name = "isAbstract",
            EmitDefaultValue = false)]
        public bool? IsAbstract { get; set; }

        /// <summary>
        /// Value rank of the variable data of a variable
        /// or variable type, otherwise null.
        /// </summary>
        [DataMember(Name = "valueRank",
            EmitDefaultValue = false)]
        public NodeValueRank? ValueRank { get; set; }

        /// <summary>
        /// Array dimensions of variable or variable type.
        /// </summary>
        [DataMember(Name = "arrayDimensions",
            EmitDefaultValue = false)]
        public uint[] ArrayDimensions { get; set; }

        /// <summary>
        /// Default value of the subtyped variable in case node is a
        /// variable type.
        /// </summary>
        [DataMember(Name = "value",
            EmitDefaultValue = false)]
        public VariantValue Value { get; set; }

        /// <summary>
        /// Built in type of the value.
        /// </summary>
        [DataMember(Name = "builtInType")]
        public BuiltInType BuiltInType { get; set; }

        /// <summary>
        /// If variable the datatype of the variable as link to
        /// data type node.
        /// </summary>
        public DataTypeEdgeModel DataType { get; set; }
    }
}
