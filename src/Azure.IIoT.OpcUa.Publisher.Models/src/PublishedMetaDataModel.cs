// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Metadata containing the schema definitions for
    /// an item produced by the dataset writer
    /// </summary>
    [DataContract]
    public record class PublishedMetaDataModel
    {
        /// <summary>
        /// Minor version
        /// </summary>
        [DataMember(Name = "minorVersion", Order = 1,
            EmitDefaultValue = false)]
        public uint MinorVersion { get; init; }

        /// <summary>
        /// Description
        /// </summary>
        [DataMember(Name = "description", Order = 2,
            EmitDefaultValue = false)]
        public string? Description { get; init; }

        /// <summary>
        /// Flags
        /// </summary>
        [DataMember(Name = "flags", Order = 3,
            EmitDefaultValue = false)]
        public ushort Flags { get; init; }

        /// <summary>
        /// Underlying built in type of the type
        /// </summary>
        [DataMember(Name = "builtInType", Order = 4,
            EmitDefaultValue = false)]
        public byte BuiltInType { get; init; }

        /// <summary>
        /// Data type
        /// </summary>
        [DataMember(Name = "dataType", Order = 5,
            EmitDefaultValue = false)]
        public string? DataType { get; init; }

        /// <summary>
        /// Value rank of the type
        /// </summary>
        [DataMember(Name = "valueRank", Order = 6,
            EmitDefaultValue = false)]
        public int ValueRank { get; init; }

        /// <summary>
        /// Array dimensions if non scalar
        /// </summary>
        [DataMember(Name = "arrayDimensions", Order = 7,
            EmitDefaultValue = false)]
        public IReadOnlyList<uint>? ArrayDimensions { get; init; }

        /// <summary>
        /// Max string length
        /// </summary>
        [DataMember(Name = "maxStringLength", Order = 8,
            EmitDefaultValue = false)]
        public uint MaxStringLength { get; init; }

        /// <summary>
        /// Properties of the field
        /// </summary>
        [DataMember(Name = "properties", Order = 10,
            EmitDefaultValue = false)]
        public IReadOnlyList<ExtensionFieldModel>? Properties { get; init; }

        /// <summary>
        /// Structure schema definitions
        /// </summary>
        [DataMember(Name = "StructureDataTypes", Order = 11,
            EmitDefaultValue = false)]
        public IReadOnlyList<StructureDescriptionModel>? StructureDataTypes { get; set; }

        /// <summary>
        /// Enum schema definitions
        /// </summary>
        [DataMember(Name = "enumDataTypes", Order = 12,
            EmitDefaultValue = false)]
        public IReadOnlyList<EnumDescriptionModel>? EnumDataTypes { get; set; }

        /// <summary>
        /// Simple type schema definitions
        /// </summary>
        [DataMember(Name = "simpleDataTypes", Order = 13,
            EmitDefaultValue = false)]
        public IReadOnlyList<SimpleTypeDescriptionModel>? SimpleDataTypes { get; set; }
    }
}
