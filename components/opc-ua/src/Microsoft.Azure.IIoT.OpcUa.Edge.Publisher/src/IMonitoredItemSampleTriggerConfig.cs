// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher {
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using System.Collections.Generic;

    /// <summary>
    /// Message trigger configuration for monitored items
    /// </summary>
    public interface IMonitoredItemSampleTriggerConfig {

        /// <summary>
        /// Subscriptions to set up as part of the job
        /// </summary>
        List<SubscriptionInfoModel> Subscriptions { get; }
    }
}