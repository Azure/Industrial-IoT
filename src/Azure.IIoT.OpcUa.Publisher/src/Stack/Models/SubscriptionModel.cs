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
        public required SubscriptionIdentifier Id { get; init; }

        /// <summary>
        /// Subscription configuration
        /// </summary>
        public required SubscriptionConfigurationModel Configuration { get; init; }

        /// <summary>
        /// Item templates for the subscription
        /// </summary>
        public required IReadOnlyList<BaseItemModel> MonitoredItems { get; init; }
    }
}
