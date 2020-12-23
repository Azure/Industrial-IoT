// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Core.Models {
    using System;

    /// <summary>
    /// Aggregate Filter
    /// </summary>
    public class AggregateFilterModel {

        /// <summary>
        /// Start time
        /// </summary>
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// Aggregate type
        /// </summary>
        public string AggregateTypeId { get; set; }

        /// <summary>
        /// Processing Interval
        /// </summary>
        public double? ProcessingInterval { get; set; }

        /// <summary>
        /// Aggregate Configuration
        /// </summary>
        public AggregateConfigurationModel AggregateConfiguration { get; set; }
    }
}