// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Twin.Models {
    using Microsoft.Azure.IIoT.OpcUa.Api.Core.Models;
    using Microsoft.Azure.IIoT.Serializers;
    using System.Runtime.Serialization;
    using System;

    /// <summary>
    /// Value read response model
    /// </summary>
    [DataContract]
    public class ValueReadResponseApiModel {

        /// <summary>
        /// Value read
        /// </summary>
        [DataMember(Name = "value", Order = 0,
            EmitDefaultValue = false)]
        public VariantValue Value { get; set; }

        /// <summary>
        /// Built in data type of the value read.
        /// </summary>
        [DataMember(Name = "dataType", Order = 1,
            EmitDefaultValue = false)]
        public string DataType { get; set; }

        /// <summary>
        /// Pico seconds part of when value was read at source.
        /// </summary>
        [DataMember(Name = "sourcePicoseconds", Order = 2,
            EmitDefaultValue = false)]
        public ushort? SourcePicoseconds { get; set; }

        /// <summary>
        /// Timestamp of when value was read at source.
        /// </summary>
        [DataMember(Name = "sourceTimestamp", Order = 3,
            EmitDefaultValue = false)]
        public DateTime? SourceTimestamp { get; set; }

        /// <summary>
        /// Pico seconds part of when value was read at server.
        /// </summary>
        [DataMember(Name = "serverPicoseconds", Order = 4,
            EmitDefaultValue = false)]
        public ushort? ServerPicoseconds { get; set; }

        /// <summary>
        /// Timestamp of when value was read at server.
        /// </summary>
        [DataMember(Name = "serverTimestamp", Order = 5,
            EmitDefaultValue = false)]
        public DateTime? ServerTimestamp { get; set; }

        /// <summary>
        /// Service result in case of error
        /// </summary>
        [DataMember(Name = "errorInfo", Order = 6,
            EmitDefaultValue = false)]
        public ServiceResultApiModel ErrorInfo { get; set; }
    }
}
