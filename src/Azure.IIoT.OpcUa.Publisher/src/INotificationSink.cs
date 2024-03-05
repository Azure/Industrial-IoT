// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher
{
    using Azure.IIoT.OpcUa.Publisher.Stack;

    /// <summary>
    /// Notification sink
    /// </summary>
    public interface INotificationSink
    {
        /// <summary>
        /// Counter reset
        /// </summary>
        void OnReset();

        /// <summary>
        /// Message received handler
        /// </summary>
        /// <param name="notification"></param>
        void OnNotify(IOpcUaSubscriptionNotification notification);
    }
}
