// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Models {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using System;

    /// <summary>
    /// Data monitored item
    /// </summary>
    public class DataMonitoredItemModel : BaseMonitoredItemModel {
        /// <summary>
        /// Data change filter
        /// </summary>
        public DataChangeFilterModel DataChangeFilter { get; set; }

        /// <summary>
        /// Aggregate filter
        /// </summary>
        public AggregateFilterModel AggregateFilter { get; set; }

        /// <summary>
        /// heartbeat interval not present if zero
        /// </summary>
        public TimeSpan? HeartbeatInterval { get; set; }
    }
}
