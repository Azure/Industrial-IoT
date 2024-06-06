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
    /// Simple attribute operand model
    /// </summary>
    [DataContract]
    public sealed record class SimpleAttributeOperandModel
    {
        /// <summary>
        /// Type definition node id if operand is
        /// simple or full attribute operand.
        /// </summary>
        [DataMember(Name = "typeDefinitionId", Order = 0)]
        public string? TypeDefinitionId { get; init; }

        /// <summary>
        /// Browse path of attribute operand
        /// </summary>
        [DataMember(Name = "browsePath", Order = 1,
            EmitDefaultValue = false)]
        public IReadOnlyList<string>? BrowsePath { get; init; }

        /// <summary>
        /// Attribute id
        /// </summary>
        [DataMember(Name = "attributeId", Order = 2,
            EmitDefaultValue = false)]
        public NodeAttribute? AttributeId { get; init; }

        /// <summary>
        /// Index range of attribute operand
        /// </summary>
        [DataMember(Name = "indexRange", Order = 3,
            EmitDefaultValue = false)]
        public string? IndexRange { get; init; }

        /// <summary>
        /// Field name in the data set (Publisher extension)
        /// </summary>
        [DataMember(Name = "displayName", Order = 4,
            EmitDefaultValue = false)]
        public string? DisplayName { get; set; }

        /// <summary>
        /// Optional data set class field id (Publisher extension)
        /// </summary>
        [DataMember(Name = "dataSetClassFieldId", Order = 5,
            EmitDefaultValue = false)]
        public Guid DataSetClassFieldId { get; set; }

        /// <summary>
        /// Metadata for the event
        /// </summary>
        [DataMember(Name = "metaData", Order = 6,
            EmitDefaultValue = false)]
        public PublishedMetaDataModel? MetaData { get; set; }

        /// <summary>
        /// Field index in the dataset.
        /// </summary>
        [DataMember(Name = "fieldIndex", Order = 7,
            EmitDefaultValue = false)]
        public int FieldIndex { get; init; }
    }
}
