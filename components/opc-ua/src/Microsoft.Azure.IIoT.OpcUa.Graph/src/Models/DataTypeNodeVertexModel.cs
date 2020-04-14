// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Graph.Models {
    using Microsoft.Azure.IIoT.Serializers;
    using Gremlin.Net.CosmosDb.Structure;
    using System.Runtime.Serialization;
    using Opc.Ua.Nodeset.Schema;

    /// <summary>
    /// Data type node vertex
    /// </summary>
    [Label(AddressSpaceElementNames.DataType)]
    [DataContract]
    public class DataTypeNodeVertexModel : BaseNodeVertexModel {

        /// <summary>
        /// Whether type is abstract, if type can
        /// be abstract.  Null if not type node.
        /// </summary>
        [DataMember(Name = "isAbstract",
            EmitDefaultValue = false)]
        public bool? IsAbstract { get; set; }

        /// <summary>
        /// Data type definition as extension object.
        /// </summary>
        [DataMember(Name = "dataTypeDefinition",
            EmitDefaultValue = false)]
        public VariantValue DataTypeDefinition { get; set; }

        /// <summary>
        /// Data type purpose
        /// </summary>
        [DataMember(Name = "purpose",
            EmitDefaultValue = false)]
        public DataTypePurpose? Purpose { get; set; }

        /// <summary>
        /// Where it is defining a variable or variable type value
        /// </summary>
        public DataTypeEdgeModel Defines { get; set; }
    }
}
