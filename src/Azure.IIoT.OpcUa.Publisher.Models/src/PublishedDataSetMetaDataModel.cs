// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Published metadata
    /// </summary>
    [DataContract]
    public sealed record class PublishedDataSetMetaDataModel
    {
        /// <summary>
        /// Minor version
        /// </summary>
        [DataMember(Name = "minorVersion", Order = 0,
            EmitDefaultValue = false)]
        public uint MinorVersion { get; init; }

        /// <summary>
        /// Provides context of the dataset meta data that is to
        /// be emitted. If set to null no dataset metadata is emitted.
        /// </summary>
        [DataMember(Name = "dataSetMetaData", Order = 1,
            EmitDefaultValue = false)]
        public required DataSetMetaDataModel DataSetMetaData { get; init; }

        /// <summary>
        /// Field metadata
        /// </summary>
        [DataMember(Name = "fields", Order = 2,
            EmitDefaultValue = false)]
        public required IReadOnlyList<PublishedFieldMetaDataModel> Fields { get; init; }

        /// <summary>
        /// Structure schema definitions
        /// </summary>
        [DataMember(Name = "StructureDataTypes", Order = 3,
            EmitDefaultValue = false)]
        public IReadOnlyList<StructureDescriptionModel>? StructureDataTypes { get; set; }

        /// <summary>
        /// Enum schema definitions
        /// </summary>
        [DataMember(Name = "enumDataTypes", Order = 4,
            EmitDefaultValue = false)]
        public IReadOnlyList<EnumDescriptionModel>? EnumDataTypes { get; set; }

        /// <summary>
        /// Simple type schema definitions
        /// </summary>
        [DataMember(Name = "simpleDataTypes", Order = 5,
            EmitDefaultValue = false)]
        public IReadOnlyList<SimpleTypeDescriptionModel>? SimpleDataTypes { get; set; }
    }
}
