// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher {
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Models;
    using System.Collections.Generic;

    /// <summary>
    /// Encoder to encode data set writer messages
    /// </summary>
    public interface IMessageEncoder {

        /// <summary>
        /// Number of incoming subscription notifications that are
        /// too big to be processed based on the message size limits
        /// or other issues with the notification.
        /// </summary>
        uint NotificationsDroppedCount { get; }

        /// <summary>
        /// Number of successfully processed subscription notifications
        /// from OPC client
        /// </summary>
        uint NotificationsProcessedCount { get; }

        /// <summary>
        /// Number of successfully generated messages that are to be
        /// sent using the message sender
        /// </summary>
        uint MessagesProcessedCount { get; }

        /// <summary>
        /// Average subscription notifications packed into a message
        /// </summary>
        double AvgNotificationsPerMessage { get; }

        /// <summary>
        /// Average size of a message through the lifetime of the
        /// encoders.
        /// </summary>
        double AvgMessageSize { get; }

        /// <summary>
        /// The message split ration specifies into how many messages a
        /// subscription notification had to be split. Less is better
        /// for performance. If the number is large user should attempt
        /// to limit the number of notifications in a message using
        /// configuration.
        /// </summary>
        double MaxMessageSplitRatio { get; }

        /// <summary>
        /// Encodes the list of notifications into network messages to send
        /// </summary>
        /// <param name="notifications">Notifications to encode</param>
        /// <param name="maxMessageSize">Maximum size of messages</param>
        /// <param name="asBatch">Encode in batch mode</param>
        IEnumerable<NetworkMessageModel> Encode(
            IEnumerable<SubscriptionNotificationModel> notifications,
            int maxMessageSize, bool asBatch);
    }
}