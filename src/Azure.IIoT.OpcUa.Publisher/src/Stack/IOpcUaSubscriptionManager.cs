// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack
{
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Subscription manager
    /// </summary>
    public interface IOpcUaSubscriptionManager
    {
        /// <summary>
        /// Get or create new subscription
        /// </summary>
        /// <param name="subscription"></param>
        /// <param name="callback"></param>
        /// <param name="metrics"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        ValueTask CreateSubscriptionAsync(SubscriptionModel subscription,
            ISubscriptionCallbacks callback, IMetricsContext metrics,
            CancellationToken ct = default);
    }
}
