// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Diagnostics {

    /// <summary>
    /// Manages event source subscriptions
    /// </summary>
    public interface IEventSourceBroker {

        /// <summary>
        /// Register and unregister subscriber
        /// </summary>
        /// <param name="eventSource"></param>
        /// <param name="subscriber"></param>
        IEventSourceSubscription Subscribe(string eventSource,
            IEventSourceSubscriber subscriber);
    }
}
