// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Structure type schema
    /// </summary>
    public record class StructureDescriptionModel
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
        /// Type of the structure. Default is structure.
        /// </summary>
        [DataMember(Name = "structureType", Order = 3,
            EmitDefaultValue = false)]
        public StructureType? StructureType { get; set; }

        /// <summary>
        /// Fields of the structure
        /// </summary>
        [DataMember(Name = "Fields", Order = 4)]
        public required IReadOnlyList<StructureFieldDescriptionModel> Fields { get; set; }

        /// <summary>
        /// Base data type
        /// </summary>
        [DataMember(Name = "baseDataType", Order = 5,
            EmitDefaultValue = false)]
        public string? BaseDataType { get; set; }

        /// <summary>
        /// Default encoding
        /// </summary>
        [DataMember(Name = "defaultEncodingId", Order = 6,
            EmitDefaultValue = false)]
        public string? DefaultEncodingId { get; set; }
    }
}
