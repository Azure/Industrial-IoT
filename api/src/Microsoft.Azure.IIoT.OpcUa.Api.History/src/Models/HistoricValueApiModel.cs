// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.History.Models {
    using Microsoft.Azure.IIoT.Serializers;
    using System.Runtime.Serialization;
    using System;

    /// <summary>
    /// Historic data
    /// </summary>
    [DataContract]
    public class HistoricValueApiModel {

        /// <summary>,
        /// The value of data value.
        /// </summary>
        [DataMember(Name = "value", Order = 0,
           EmitDefaultValue = false)]
        public VariantValue Value { get; set; }

        /// <summary>
        /// The status code associated with the value.
        /// </summary>
        [DataMember(Name = "statusCode", Order = 1,
            EmitDefaultValue = false)]
        public uint? StatusCode { get; set; }

        /// <summary>
        /// The source timestamp associated with the value.
        /// </summary>
        [DataMember(Name = "sourceTimestamp", Order = 2,
            EmitDefaultValue = false)]
        public DateTime? SourceTimestamp { get; set; }

        /// <summary>
        /// Additional resolution for the source timestamp.
        /// </summary>
        [DataMember(Name = "sourcePicoseconds", Order = 3,
            EmitDefaultValue = false)]
        public ushort? SourcePicoseconds { get; set; }

        /// <summary>
        /// The server timestamp associated with the value.
        /// </summary>
        [DataMember(Name = "serverTimestamp", Order = 4,
            EmitDefaultValue = false)]
        public DateTime? ServerTimestamp { get; set; }

        /// <summary>
        /// Additional resolution for the server timestamp.
        /// </summary>
        [DataMember(Name = "serverPicoseconds", Order = 5,
            EmitDefaultValue = false)]
        public ushort? ServerPicoseconds { get; set; }

        /// <summary>
        /// modification information when reading modifications.
        /// </summary>
        [DataMember(Name = "modificationInfo", Order = 6,
            EmitDefaultValue = false)]
        public ModificationInfoApiModel ModificationInfo { get; set; }
    }
}
