// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Structure field description
    /// </summary>
    [DataContract]
    public record class StructureFieldDescriptionModel
    {
        /// <summary>
        /// Name of the field
        /// </summary>
        [DataMember(Name = "name", Order = 1)]
        public required string Name { get; set; }

        /// <summary>
        /// Data type schema
        /// </summary>
        [DataMember(Name = "dataType", Order = 2)]
        public required string DataType { get; set; }

        /// <summary>
        /// Description of the field
        /// </summary>
        [DataMember(Name = "description", Order = 3,
            EmitDefaultValue = false)]
        public string? Description { get; set; }

        /// <summary>
        /// Optionality of the field
        /// </summary>
        [DataMember(Name = "isOptional", Order = 4,
            EmitDefaultValue = false)]
        public bool IsOptional { get; set; }

        /// <summary>
        /// Value rank of the type
        /// </summary>
        [DataMember(Name = "valueRank", Order = 6,
            EmitDefaultValue = false)]
        public int ValueRank { get; set; }

        /// <summary>
        /// Array dimensions if non scalar
        /// </summary>
        [DataMember(Name = "arrayDimensions", Order = 7,
            EmitDefaultValue = false)]
        public IReadOnlyList<uint>? ArrayDimensions { get; set; }

        /// <summary>
        /// Max string length
        /// </summary>
        [DataMember(Name = "maxStringLength", Order = 8,
            EmitDefaultValue = false)]
        public uint MaxStringLength { get; set; }
    }
}
