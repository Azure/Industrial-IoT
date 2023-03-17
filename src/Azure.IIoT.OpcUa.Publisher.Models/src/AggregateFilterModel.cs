// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Aggregate Filter
    /// </summary>
    [DataContract]
    public sealed record class AggregateFilterModel
    {
        /// <summary>
        /// Start time
        /// </summary>
        [DataMember(Name = "startTime", Order = 0,
            EmitDefaultValue = false)]
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// Aggregate type
        /// </summary>
        [DataMember(Name = "aggregateTypeId", Order = 1,
            EmitDefaultValue = false)]
        public string? AggregateTypeId { get; set; }

        /// <summary>
        /// Processing Interval
        /// </summary>
        [DataMember(Name = "processingInterval", Order = 2,
            EmitDefaultValue = false)]
        public TimeSpan? ProcessingInterval { get; set; }

        /// <summary>
        /// Aggregate Configuration - use an empty configuration
        /// to clear the configuration and use the server defaults.
        /// Returns null if the default configuration in the
        /// server is being used.
        /// </summary>
        [DataMember(Name = "aggregateConfiguration", Order = 3,
            EmitDefaultValue = false)]
        public AggregateConfigurationModel? AggregateConfiguration { get; set; }
    }
}
