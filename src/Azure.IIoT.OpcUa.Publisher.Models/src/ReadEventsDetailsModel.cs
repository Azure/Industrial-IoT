// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Read event data
    /// </summary>
    [DataContract]
    public sealed record class ReadEventsDetailsModel
    {
        /// <summary>
        /// Start time to read from
        /// </summary>
        [DataMember(Name = "startTime", Order = 0,
            EmitDefaultValue = false)]
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// End time to read to
        /// </summary>
        [DataMember(Name = "endTime", Order = 1,
            EmitDefaultValue = false)]
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// Number of events to read
        /// </summary>
        [DataMember(Name = "numEvents", Order = 2,
            EmitDefaultValue = false)]
        public uint? NumEvents { get; set; }

        /// <summary>
        /// The filter to use to select the event fields
        /// </summary>
        [DataMember(Name = "filter", Order = 3,
            EmitDefaultValue = false)]
        public EventFilterModel? Filter { get; set; }
    }
}
