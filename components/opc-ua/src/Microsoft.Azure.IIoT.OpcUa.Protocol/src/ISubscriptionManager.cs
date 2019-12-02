﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol {
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using System.Threading.Tasks;

    /// <summary>
    /// Subscription manager
    /// </summary>
    public interface ISubscriptionManager {

        /// <summary>
        /// Total subscriptions
        /// </summary>
        int TotalSubscriptionCount { get; }

        /// <summary>
        /// Get or create new subscription
        /// </summary>
        /// <param name="subscriptionModel"></param>
        /// <returns></returns>
        Task<ISubscription> GetOrCreateSubscriptionAsync(
            SubscriptionInfoModel subscriptionModel);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="subscription"></param>
        /// <returns></returns>
        Task DisposeSubscription(ISubscription subscription);
    }
}