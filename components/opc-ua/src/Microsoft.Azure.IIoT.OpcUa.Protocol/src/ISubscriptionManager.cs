// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol {
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Models;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Subscription manager
    /// </summary>
    public interface ISubscriptionManager {

        /// <summary>
        /// Subscription configuration
        /// </summary>
        ISubscriptionConfig Configuration { get; }

        /// <summary>
        /// Get or create new subscription
        /// </summary>
        /// <param name="subscriptionModel"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        ValueTask<ISubscription> CreateSubscriptionAsync(
            SubscriptionModel subscriptionModel, CancellationToken ct);
    }
}