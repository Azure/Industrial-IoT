// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using Furly.Extensions.Serializers;
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Extension fields
    /// </summary>
    [DataContract]
    public record ExtensionFieldModel
    {
        /// <summary>
        /// Field index of this variable in the dataset.
        /// </summary>
        [DataMember(Name = "fieldIndex", Order = 0,
            EmitDefaultValue = false)]
        public int FieldIndex { get; init; }

        /// <summary>
        /// Field name or display name of the published variable
        /// </summary>
        [DataMember(Name = "dataSetFieldName", Order = 1,
            EmitDefaultValue = false)]
        public required string DataSetFieldName { get; init; }

        /// <summary>
        /// Name of the published dataset
        /// </summary>
        [DataMember(Name = "value", Order = 2,
            EmitDefaultValue = false)]
        public required VariantValue Value { get; init; }

        /// <summary>
        /// Identifier of field in the dataset class.
        /// </summary>
        [DataMember(Name = "dataSetClassFieldId", Order = 3,
            EmitDefaultValue = false)]
        public Guid DataSetClassFieldId { get; init; }

        /// <summary>
        /// Unique Identifier of variable in the dataset.
        /// </summary>
        [DataMember(Name = "id", Order = 4,
            EmitDefaultValue = false)]
        public string? Id { get; set; }

        /// <summary>
        /// Metadata for the field
        /// </summary>
        [DataMember(Name = "metaData", Order = 10,
            EmitDefaultValue = false)]
        public PublishedMetaDataModel? MetaData { get; set; }
    }
}
