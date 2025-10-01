// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using Furly.Extensions.Serializers;
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;

    /// <summary>
    /// Extension fields
    /// </summary>
    [DataContract]
    public record ExtensionFieldModel
    {
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
        [SkipValidation]
        public required VariantValue Value { get; init; }

        /// <summary>
        /// Identifier of field in the dataset class.
        /// </summary>
        [DataMember(Name = "dataSetClassFieldId", Order = 3,
            EmitDefaultValue = false)]
        public Guid DataSetClassFieldId { get; init; }

        /// <summary>
        /// Description for the field as it should show up in
        /// the data set meta data.
        /// </summary>
        [DataMember(Name = "dataSetFieldDescription", Order = 4,
            EmitDefaultValue = false)]
        public string? DataSetFieldDescription { get; set; }
    }
}
