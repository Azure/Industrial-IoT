// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// History configuration
    /// </summary>
    [DataContract]
    public sealed record class HistoryConfigurationModel
    {
        /// <summary>
        /// specifies whether the historical data was
        /// collected in such a manner that it should
        /// be displayed as SlopedInterpolation (sloped
        /// line between points) or as SteppedInterpolation
        /// (vertically-connected horizontal lines
        /// between points) when raw data is examined.
        /// This Property also effects how some
        /// Aggregates are calculated
        /// </summary>
        [DataMember(Name = "stepped", Order = 0,
            EmitDefaultValue = false)]
        public bool? Stepped { get; set; }

        /// <summary>
        /// Human readable string that specifies how
        /// the value of this HistoricalDataNode is
        /// calculated
        /// </summary>
        [DataMember(Name = "definition", Order = 1,
            EmitDefaultValue = false)]
        public string? Definition { get; set; }

        /// <summary>
        /// Specifies the maximum interval between data
        /// points in the history repository
        /// regardless of their value change
        /// </summary>
        [DataMember(Name = "maxTimeInterval", Order = 2,
            EmitDefaultValue = false)]
        public TimeSpan? MaxTimeInterval { get; set; }

        /// <summary>
        /// Specifies the minimum interval between
        /// data points in the history repository
        /// regardless of their value change
        /// </summary>
        [DataMember(Name = "minTimeInterval", Order = 3,
            EmitDefaultValue = false)]
        public TimeSpan? MinTimeInterval { get; set; }

        /// <summary>
        /// Minimum amount that the data for the
        /// Node shall change in order for the change
        /// to be reported to the history database
        /// </summary>
        [DataMember(Name = "exceptionDeviation", Order = 4,
            EmitDefaultValue = false)]
        public double? ExceptionDeviation { get; set; }

        /// <summary>
        /// Specifies how the ExceptionDeviation is
        /// determined
        /// </summary>
        [DataMember(Name = "exceptionDeviationType", Order = 5,
            EmitDefaultValue = false)]
        public ExceptionDeviationType? ExceptionDeviationType { get; set; }

        /// <summary>
        /// The date before which there is no data in the
        /// archive either online or offline
        /// </summary>
        [DataMember(Name = "startOfArchive", Order = 6,
            EmitDefaultValue = false)]
        public DateTime? StartOfArchive { get; set; }

        /// <summary>
        /// The last date of the archive
        /// </summary>
        [DataMember(Name = "endOfArchive", Order = 7,
            EmitDefaultValue = false)]
        public DateTime? EndOfArchive { get; set; }

        /// <summary>
        /// Date of the earliest data in the online archive
        /// </summary>
        [DataMember(Name = "startOfOnlineArchive", Order = 8,
            EmitDefaultValue = false)]
        public DateTime? StartOfOnlineArchive { get; set; }

        /// <summary>
        /// Server supports ServerTimestamps in addition
        /// to SourceTimestamp
        /// </summary>
        [DataMember(Name = "serverTimestampSupported", Order = 9,
            EmitDefaultValue = false)]
        public bool? ServerTimestampSupported { get; set; }

        /// <summary>
        /// Aggregate configuration
        /// </summary>
        [DataMember(Name = "aggregateConfiguration", Order = 10,
            EmitDefaultValue = false)]
        public AggregateConfigurationModel? AggregateConfiguration { get; set; }

        /// <summary>
        /// Allowed aggregate functions
        /// </summary>
        [DataMember(Name = "aggregateFunctions", Order = 11,
            EmitDefaultValue = false)]
        public IReadOnlyDictionary<string, string>? AggregateFunctions { get; set; }
    }
}
