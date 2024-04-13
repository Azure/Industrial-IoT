﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack
{
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Azure.IIoT.OpcUa.Encoders;
    using Azure.IIoT.OpcUa.Encoders.PubSub;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Opc Ua subscription notification
    /// </summary>
    public interface IOpcUaSubscriptionNotification : IDisposable
    {
        /// <summary>
        /// Sequence number of the message
        /// </summary>
        uint SequenceNumber { get; }

        /// <summary>
        /// Codec
        /// </summary>
        IVariantEncoder Codec { get; }

        /// <summary>
        /// Notification
        /// </summary>
        IList<MonitoredItemNotificationModel> Notifications { get; }

        /// <summary>
        /// Subscription from which message originated
        /// </summary>
        string? SubscriptionName { get; }

        /// <summary>
        /// Endpoint url
        /// </summary>
        string? EndpointUrl { get; }

        /// <summary>
        /// Appplication url
        /// </summary>
        string? ApplicationUri { get; }

        /// <summary>
        /// Publishing time
        /// </summary>
        DateTime? PublishTimestamp { get; }

        /// <summary>
        /// Notification created time
        /// </summary>
        DateTime CreatedTimestamp { get; }

        /// <summary>
        /// Publishing sequence number
        /// </summary>
        uint? PublishSequenceNumber { get; }

        /// <summary>
        /// Message type
        /// </summary>
        MessageType MessageType { get; }

        /// <summary>
        /// Additional context information
        /// </summary>
        object? Context { get; set; }

        /// <summary>
        /// Try upgrde notification to key frame
        /// notification.
        /// </summary>
        /// <returns></returns>
        bool TryUpgradeToKeyFrame();

        /// <summary>
        /// Split into notifications per context
        /// </summary>
        /// <param name="selector"></param>
        /// <returns></returns>
        IEnumerable<IOpcUaSubscriptionNotification> Split(
            Func<MonitoredItemNotificationModel, object?> selector);

#if DEBUG
        /// <summary>
        /// Mark as processed
        /// </summary>
        public void MarkProcessed()
        {
        }

        /// <summary>
        /// Debug that we processed the item
        /// </summary>
        public void DebugAssertProcessed()
        {
        }
#endif
    }
}
