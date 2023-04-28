// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// History Server capabilities
    /// </summary>
    [DataContract]
    public sealed record class HistoryServerCapabilitiesModel
    {
        /// <summary>
        /// Server supports historic data access
        /// </summary>
        [DataMember(Name = "supportsHistoricData", Order = 0,
            EmitDefaultValue = false)]
        public bool AccessHistoryDataCapability { get; set; }

        /// <summary>
        /// Server supports historic event access
        /// </summary>
        [DataMember(Name = "supportsHistoricEvents", Order = 1,
            EmitDefaultValue = false)]
        public bool AccessHistoryEventsCapability { get; set; }

        /// <summary>
        /// Maximum number of historic data values that will
        /// be returned in a single read.
        /// </summary>
        [DataMember(Name = "maxReturnDataValues", Order = 2,
            EmitDefaultValue = false)]
        public uint? MaxReturnDataValues { get; set; }

        /// <summary>
        /// Maximum number of events that will be returned
        /// in a single read.
        /// </summary>
        [DataMember(Name = "maxReturnEventValues", Order = 3,
            EmitDefaultValue = false)]
        public uint? MaxReturnEventValues { get; set; }

        /// <summary>
        /// Server supports inserting data
        /// </summary>
        [DataMember(Name = "insertDataCapability", Order = 4,
            EmitDefaultValue = false)]
        public bool? InsertDataCapability { get; set; }

        /// <summary>
        /// Server supports replacing historic data
        /// </summary>
        [DataMember(Name = "replaceDataCapability", Order = 5,
            EmitDefaultValue = false)]
        public bool? ReplaceDataCapability { get; set; }

        /// <summary>
        /// Server supports updating historic data
        /// </summary>
        [DataMember(Name = "updateDataCapability", Order = 6,
            EmitDefaultValue = false)]
        public bool? UpdateDataCapability { get; set; }

        /// <summary>
        /// Server supports deleting raw data
        /// </summary>
        [DataMember(Name = "deleteRawCapability", Order = 7,
            EmitDefaultValue = false)]
        public bool? DeleteRawCapability { get; set; }

        /// <summary>
        /// Server support deleting data at times
        /// </summary>
        [DataMember(Name = "deleteAtTimeCapability", Order = 8,
            EmitDefaultValue = false)]
        public bool? DeleteAtTimeCapability { get; set; }

        /// <summary>
        /// Server supports inserting events
        /// </summary>
        [DataMember(Name = "insertEventCapability", Order = 9,
            EmitDefaultValue = false)]
        public bool? InsertEventCapability { get; set; }

        /// <summary>
        /// Server supports replacing events
        /// </summary>
        [DataMember(Name = "replaceEventCapability", Order = 10,
            EmitDefaultValue = false)]
        public bool? ReplaceEventCapability { get; set; }

        /// <summary>
        /// Server supports updating events
        /// </summary>
        [DataMember(Name = "updateEventCapability", Order = 11,
            EmitDefaultValue = false)]
        public bool? UpdateEventCapability { get; set; }

        /// <summary>
        /// Server supports deleting events
        /// </summary>
        [DataMember(Name = "deleteEventCapability", Order = 12,
            EmitDefaultValue = false)]
        public bool? DeleteEventCapability { get; set; }

        /// <summary>
        /// Allows inserting annotations
        /// </summary>
        [DataMember(Name = "insertAnnotationCapability", Order = 13,
            EmitDefaultValue = false)]
        public bool? InsertAnnotationCapability { get; set; }

        /// <summary>
        /// Server supports ServerTimestamps in addition
        /// to SourceTimestamp
        /// </summary>
        [DataMember(Name = "serverTimestampSupported", Order = 14,
            EmitDefaultValue = false)]
        public bool? ServerTimestampSupported { get; set; }

        /// <summary>
        /// Supported aggregate functions
        /// </summary>
        [DataMember(Name = "aggregateFunctions", Order = 15,
            EmitDefaultValue = false)]
        public IReadOnlyDictionary<string, string>? AggregateFunctions { get; set; }
    }
}
