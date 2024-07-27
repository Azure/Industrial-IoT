// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack
{
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using System;

    /// <summary>
    /// Subscription manager
    /// </summary>
    public interface IOpcUaSubscriptionManager
    {
        /// <summary>
        /// Create new subscription with the subscription model
        /// The callback will have been called with the new subscription
        /// which then can be used to manage the subscription.
        /// </summary>
        /// <param name="subscription">The subscription template</param>
        /// <param name="callback">Callbacks from the subscription</param>
        /// <param name="metrics">Additional metrics information</param>
        /// <returns></returns>
        void RegisterSubscriptionCallbacks(SubscriptionModel subscription,
            ISubscriptionCallbacks callback, IMetricsContext metrics);
    }
}
