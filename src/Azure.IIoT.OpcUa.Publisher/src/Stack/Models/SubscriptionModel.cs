// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Models
{
    using System.Collections.Generic;

    /// <summary>
    /// An activated monitored item subscription on an endpoint
    /// </summary>
    public sealed record class SubscriptionModel
    {
        /// <summary>
        /// Id of the subscription
        /// </summary>
        public required SubscriptionIdentifier Id { get; set; }

        /// <summary>
        /// Subscription configuration
        /// </summary>
        public SubscriptionConfigurationModel? Configuration { get; set; }

        /// <summary>
        /// Monitored item templates for the subscription
        /// </summary>
#pragma warning disable CA2227 // Collection properties should be read only
        public IList<BaseMonitoredItemModel>? MonitoredItems { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only
    }
}
