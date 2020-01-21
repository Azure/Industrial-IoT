// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.History.Models {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using System;

    /// <summary>
    /// Read processed historic data
    /// </summary>
    public class ReadProcessedValuesDetailsModel {

        /// <summary>
        /// Start time to read from.
        /// </summary>
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// End time to read until
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// Interval to process
        /// </summary>
        public double? ProcessingInterval { get; set; }

        /// <summary>
        /// The aggregate type node ids
        /// </summary>
        public string AggregateTypeId { get; set; }

        /// <summary>
        /// A configuration for the aggregate
        /// </summary>
        public AggregateConfigurationModel AggregateConfiguration { get; set; }
    }
}
