// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Exceptions {
    using Microsoft.Azure.IIoT.Exceptions;

    /// <summary>
    /// Subscription not found
    /// </summary>
    internal class SubscriptionModelNotFoundException : ResourceNotFoundException {

        /// <inheritdoc/>
        public SubscriptionModelNotFoundException() {
        }

        /// <inheritdoc/>
        public SubscriptionModelNotFoundException(string subscriptionName) : base(
            $"Subscription configuration for subscription with name '{subscriptionName}' could not be found.") {
        }
    }
}