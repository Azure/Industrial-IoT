// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Read modified data
    /// </summary>
    [DataContract]
    public sealed record class ReadModifiedValuesDetailsModel
    {
        /// <summary>
        /// The start time to read from
        /// </summary>
        [DataMember(Name = "startTime", Order = 0,
            EmitDefaultValue = false)]
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// The end time to read to
        /// </summary>
        [DataMember(Name = "endTime", Order = 1,
            EmitDefaultValue = false)]
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// The number of values to read
        /// </summary>
        [DataMember(Name = "numValues", Order = 2,
            EmitDefaultValue = false)]
        public uint? NumValues { get; set; }
    }
}
