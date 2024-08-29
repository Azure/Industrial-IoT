// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher
{
    using Azure.IIoT.OpcUa.Publisher.Stack.Services;
    using Furly.Extensions.Messaging;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Encoder to encode data set writer messages
    /// </summary>
    public interface IMessageEncoder
    {
        /// <summary>
        /// Encodes the list of notifications into network messages to send
        /// </summary>
        /// <param name="factory">Factory to create empty messages</param>
        /// <param name="notifications">Notifications to encode</param>
        /// <param name="maxMessageSize">Maximum size of messages</param>
        /// <param name="asBatch">Encode in batch mode</param>
        IEnumerable<(IEvent Event, Action OnSent)> Encode(Func<IEvent> factory,
            IEnumerable<OpcUaSubscriptionNotification> notifications,
            int maxMessageSize, bool asBatch);
    }
}
