// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Node metadata model
    /// </summary>
    [DataContract]
    public sealed record class NodeMetadataResponseModel
    {
        /// <summary>
        /// The node id of the node
        /// </summary>
        [DataMember(Name = "nodeId", Order = 0)]
        public string? NodeId { get; set; }

        /// <summary>
        /// The class of the node
        /// </summary>
        [DataMember(Name = "nodeClass", Order = 1)]
        public NodeClass NodeClass { get; set; }

        /// <summary>
        /// The display name of the node.
        /// </summary>
        [DataMember(Name = "displayName", Order = 2,
            EmitDefaultValue = false)]
        public string? DisplayName { get; set; }

        /// <summary>
        /// The description for the node.
        /// </summary>
        [DataMember(Name = "description", Order = 3,
            EmitDefaultValue = false)]
        public string? Description { get; set; }

        /// <summary>
        /// Variable meta data if the node is of class
        /// <see cref="NodeClass.VariableType"/> or
        /// <see cref="NodeClass.Variable"/> otherwise
        /// null.
        /// </summary>
        [DataMember(Name = "variableMetadata", Order = 4,
            EmitDefaultValue = false)]
        public VariableMetadataModel? VariableMetadata { get; set; }

        /// <summary>
        /// Data type meta data if the node is of class
        /// <see cref="NodeClass.DataType"/> otherwise
        /// null.
        /// </summary>
        [DataMember(Name = "dataTypeMetadata", Order = 5,
            EmitDefaultValue = false)]
        public DataTypeMetadataModel? DataTypeMetadata { get; set; }

        /// <summary>
        /// Method meta data if the node is of class
        /// <see cref="NodeClass.Method"/> otherwise
        /// null.
        /// </summary>
        [DataMember(Name = "nethodMetadata", Order = 6,
            EmitDefaultValue = false)]
        public MethodMetadataModel? MethodMetadata { get; set; }

        /// <summary>
        /// The returned type definition if the node is
        /// of class <see cref="NodeClass.Object"/> or
        /// <see cref="NodeClass.Variable"/>
        /// referenced a type definition node or if the node
        /// is a <see cref="NodeClass.VariableType"/>,
        /// <see cref="NodeClass.ObjectType"/>,
        /// or <see cref="NodeClass.ReferenceType"/>,
        /// </summary>
        [DataMember(Name = "typeDefinition", Order = 7,
            EmitDefaultValue = false)]
        public TypeDefinitionModel? TypeDefinition { get; set; }

        /// <summary>
        /// Service result in case of error
        /// </summary>
        [DataMember(Name = "errorInfo", Order = 8,
            EmitDefaultValue = false)]
        public ServiceResultModel? ErrorInfo { get; set; }
    }
}
