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
    /// Historic data
    /// </summary>
    [DataContract]
    public sealed record class HistoricValueModel
    {
        /// <summary>
        /// The value of data value.
        /// </summary>
        [DataMember(Name = "value", Order = 0)]
        [SkipValidation]
        public VariantValue? Value { get; set; }

        /// <summary>
        /// Built in data type of the updated values
        /// </summary>
        [DataMember(Name = "dataType", Order = 1,
            EmitDefaultValue = false)]
        public string? DataType { get; set; }

        /// <summary>
        /// The status code associated with the value.
        /// </summary>
        [DataMember(Name = "status", Order = 2,
            EmitDefaultValue = false)]
        public ServiceResultModel? Status { get; set; }

        /// <summary>
        /// The source timestamp associated with the value.
        /// </summary>
        [DataMember(Name = "sourceTimestamp", Order = 3,
            EmitDefaultValue = false)]
        public DateTime? SourceTimestamp { get; set; }

        /// <summary>
        /// Additional resolution for the source timestamp.
        /// </summary>
        [DataMember(Name = "sourcePicoseconds", Order = 4,
            EmitDefaultValue = false)]
        public ushort? SourcePicoseconds { get; set; }

        /// <summary>
        /// The server timestamp associated with the value.
        /// </summary>
        [DataMember(Name = "serverTimestamp", Order = 5,
            EmitDefaultValue = false)]
        public DateTime? ServerTimestamp { get; set; }

        /// <summary>
        /// Additional resolution for the server timestamp.
        /// </summary>
        [DataMember(Name = "serverPicoseconds", Order = 6,
            EmitDefaultValue = false)]
        public ushort? ServerPicoseconds { get; set; }

        /// <summary>
        /// Indicates the location of the data. (Default: raw)
        /// </summary>
        [DataMember(Name = "dataLocation", Order = 7,
            EmitDefaultValue = false)]
        public DataLocation? DataLocation { get; set; }

        /// <summary>
        /// modification information when reading modifications.
        /// </summary>
        [DataMember(Name = "modificationInfo", Order = 8,
            EmitDefaultValue = false)]
        public ModificationInfoModel? ModificationInfo { get; set; }

        /// <summary>
        /// History information if any (default: none)
        /// </summary>
        [DataMember(Name = "additionalData", Order = 9,
            EmitDefaultValue = false)]
        public AdditionalData? AdditionalData { get; set; }
    }
}
