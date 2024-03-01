// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Simple type schema
    /// </summary>
    [DataContract]
    public record class SimpleTypeDescriptionModel
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
        /// Base data type
        /// </summary>
        [DataMember(Name = "baseDataType", Order = 3,
            EmitDefaultValue = false)]
        public string? BaseDataType { get; set; }

        /// <summary>
        /// Underlying built in type of the enum.
        /// Default is integer.
        /// </summary>
        [DataMember(Name = "builtInType", Order = 4,
            EmitDefaultValue = false)]
        public byte? BuiltInType { get; set; }
    }
}
