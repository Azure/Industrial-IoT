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
        [DataMember(Name = "value",
            EmitDefaultValue = false)]
        public VariantValue Value { get; set; }

        /// <summary>
        /// Built in data type of the value read.
        /// </summary>
        [DataMember(Name = "dataType",
            EmitDefaultValue = false)]
        public string DataType { get; set; }

        /// <summary>
        /// Pico seconds part of when value was read at source.
        /// </summary>
        [DataMember(Name = "sourcePicoseconds",
            EmitDefaultValue = false)]
        public ushort? SourcePicoseconds { get; set; }

        /// <summary>
        /// Timestamp of when value was read at source.
        /// </summary>
        [DataMember(Name = "sourceTimestamp",
            EmitDefaultValue = false)]
        public DateTime? SourceTimestamp { get; set; }

        /// <summary>
        /// Pico seconds part of when value was read at server.
        /// </summary>
        [DataMember(Name = "serverPicoseconds",
            EmitDefaultValue = false)]
        public ushort? ServerPicoseconds { get; set; }

        /// <summary>
        /// Timestamp of when value was read at server.
        /// </summary>
        [DataMember(Name = "serverTimestamp",
            EmitDefaultValue = false)]
        public DateTime? ServerTimestamp { get; set; }

        /// <summary>
        /// Service result in case of error
        /// </summary>
        [DataMember(Name = "errorInfo",
            EmitDefaultValue = false)]
        public ServiceResultApiModel ErrorInfo { get; set; }
    }
}
