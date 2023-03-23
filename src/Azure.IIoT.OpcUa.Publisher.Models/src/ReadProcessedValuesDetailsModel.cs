// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Read processed historic data
    /// </summary>
    [DataContract]
    public sealed record class ReadProcessedValuesDetailsModel
    {
        /// <summary>
        /// Start time to read from.
        /// </summary>
        [DataMember(Name = "startTime", Order = 0,
            EmitDefaultValue = false)]
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// End time to read until
        /// </summary>
        [DataMember(Name = "endTime", Order = 1,
            EmitDefaultValue = false)]
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// Interval to process
        /// </summary>
        [DataMember(Name = "processingInterval", Order = 2,
            EmitDefaultValue = false)]
        public TimeSpan? ProcessingInterval { get; set; }

        /// <summary>
        /// The aggregate type to apply. Can be the name of
        /// the aggregate if available in the history server
        /// capabilities, or otherwise will be used as a node
        /// id referring to the aggregate.
        /// </summary>
        [DataMember(Name = "aggregateType", Order = 3,
            EmitDefaultValue = false)]
        public string? AggregateType { get; set; }

        /// <summary>
        /// Aggregate Configuration - use null or empty configuration
        /// to use the server defaults.
        /// </summary>
        [DataMember(Name = "aggregateConfiguration", Order = 4,
            EmitDefaultValue = false)]
        public AggregateConfigurationModel? AggregateConfiguration { get; set; }
    }
}
