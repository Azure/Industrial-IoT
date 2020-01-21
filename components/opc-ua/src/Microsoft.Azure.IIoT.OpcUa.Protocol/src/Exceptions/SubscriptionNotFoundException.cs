// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Exceptions {
    using System;

    /// <summary>
    /// Subscription not found
    /// </summary>
    public class SubscriptionNotFoundException : Exception {

        /// <inheritdoc/>
        public SubscriptionNotFoundException(string subscriptionName) :
            base($"The subscription '{subscriptionName}' could not be found.") {
        }
    }
}