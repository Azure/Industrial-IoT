// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack {
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Azure.IIoT.OpcUa.Encoders;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Subscription manager
    /// </summary>
    public interface ISubscriptionManager {

        /// <summary>
        /// Get or create new subscription
        /// </summary>
        /// <param name="subscriptionModel"></param>
        /// <param name="codec"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        ValueTask<ISubscription> CreateSubscriptionAsync(
            SubscriptionModel subscriptionModel,
            IVariantEncoderFactory codec = null,
            CancellationToken ct = default);
    }
}