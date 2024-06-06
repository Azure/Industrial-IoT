// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Metadata containing the field schema definitions for
    /// items produced by the dataset writer
    /// </summary>
    [DataContract]
    public record class PublishedFieldMetaDataModel
    {
        /// <summary>
        /// Name of the field
        /// </summary>
        [DataMember(Name = "name", Order = 0,
            EmitDefaultValue = false)]
        public required string Name { get; init; }

        /// <summary>
        /// Field id
        /// </summary>
        [DataMember(Name = "id", Order = 1,
            EmitDefaultValue = false)]
        public Guid Id { get; init; }

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
    }
}
