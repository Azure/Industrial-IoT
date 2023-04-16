// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack
{
    using System.Threading.Tasks;

    /// <summary>
    /// Subscription handle
    /// </summary>
    internal interface ISubscriptionHandle
    {
        /// <summary>
        /// Reapply current configuration
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        ValueTask ReapplyToSessionAsync(IOpcUaSession session);

        /// <summary>
        /// Called to signal the underlying session is disconnected and the
        /// subscription is offline, or when it is reconnected and the
        /// session is back online. This is the case during reconnect handler
        /// execution or when the subscription was disconnected.
        /// </summary>
        /// <param name="online"></param>
        void OnSubscriptionStateChanged(bool online);
    }
}
