// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.History.Models {
    using System.Runtime.Serialization;
    using System;

    /// <summary>
    /// Read modified data
    /// </summary>
    [DataContract]
    public class ReadModifiedValuesDetailsApiModel {

        /// <summary>
        /// The start time to read from
        /// </summary>
        [DataMember(Name = "startTime",
            EmitDefaultValue = false)]
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// The end time to read to
        /// </summary>
        [DataMember(Name = "endTime",
            EmitDefaultValue = false)]
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// The number of values to read
        /// </summary>
        [DataMember(Name = "numValues",
            EmitDefaultValue = false)]
        public uint? NumValues { get; set; }
    }
}
