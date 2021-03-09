// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Models {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;

    /// <summary>
    /// Data monitored item
    /// </summary>
    public class EventMonitoredItemModel : BaseMonitoredItemModel {
        /// <summary>
        /// Event filter
        /// </summary>
        public EventFilterModel EventFilter { get; set; }
    }
}
