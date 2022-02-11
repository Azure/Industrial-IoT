// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol {
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Models;
    using System.Threading.Tasks;

    /// <summary>
    /// Subscription manager
    /// </summary>
    public interface ISubscriptionManager {

        /// <summary>
        /// Get or create new subscription
        /// </summary>
        /// <param name="subscriptionModel"></param>
        /// <returns></returns>
        Task<ISubscription> GetOrCreateSubscriptionAsync(
            SubscriptionModel subscriptionModel);
    }
}