// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Enum type schema
    /// </summary>
    [DataContract]
    public record class EnumDescriptionModel
    {
        /// <summary>
        /// Data type identifier
        /// </summary>
        [DataMember(Name = "dataTypeId", Order = 1)]
        public required string DataTypeId { get; set; }

        /// <summary>
        /// Name of the type
        /// </summary>
        [DataMember(Name = "name", Order = 2)]
        public required string Name { get; set; }

        /// <summary>
        /// Fields of the enum
        /// </summary>
        [DataMember(Name = "fields", Order = 3)]
        public required IReadOnlyList<EnumFieldDescriptionModel> Fields { get; set; }

        /// <summary>
        /// Underlying built in type of the enum.
        /// Default is integer.
        /// </summary>
        [DataMember(Name = "builtInType", Order = 4,
            EmitDefaultValue = false)]
        public byte? BuiltInType { get; set; }
    }
}
