// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Runtime {
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using System.Collections.Generic;

    /// <summary>
    /// Monitored item message trigger configuration
    /// </summary>
    public class MonitoredItemSampleTriggerConfig : IMonitoredItemSampleTriggerConfig {

        /// <inheritdoc/>
        public List<SubscriptionInfoModel> Subscriptions { get; set; }
    }
}