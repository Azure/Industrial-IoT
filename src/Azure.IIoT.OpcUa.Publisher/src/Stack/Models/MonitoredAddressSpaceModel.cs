// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Models
{
    using System;

    /// <summary>
    /// Monitor the address space
    /// </summary>
    public sealed record class MonitoredAddressSpaceModel : BaseMonitoredItemModel
    {
        /// <summary>
        /// Rebrowse period to use when monitoring
        /// </summary>
        public TimeSpan? RebrowsePeriod { get; set; }

        /// <summary>
        /// Root node to start browsing (optional)
        /// </summary>
        public string? RootNodeId { get; set; }
    }
}
