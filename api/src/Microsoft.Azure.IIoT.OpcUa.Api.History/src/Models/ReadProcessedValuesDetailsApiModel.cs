// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.History.Models {
    using Microsoft.Azure.IIoT.OpcUa.Api.Core.Models;
    using System.Runtime.Serialization;
    using System;

    /// <summary>
    /// Read processed historic data
    /// </summary>
    [DataContract]
    public class ReadProcessedValuesDetailsApiModel {

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
        public double? ProcessingInterval { get; set; }

        /// <summary>
        /// The aggregate type node ids
        /// </summary>
        [DataMember(Name = "aggregateTypeId", Order = 3,
            EmitDefaultValue = false)]
        public string AggregateTypeId { get; set; }

        /// <summary>
        /// A configuration for the aggregate
        /// </summary>
        [DataMember(Name = "aggregateConfiguration", Order = 4,
            EmitDefaultValue = false)]
        public AggregateConfigurationApiModel AggregateConfiguration { get; set; }
    }
}
