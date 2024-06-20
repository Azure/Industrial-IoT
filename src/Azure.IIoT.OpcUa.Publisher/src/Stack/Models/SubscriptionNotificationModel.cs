// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Models
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Encoders.PubSub;
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Subscription notification model
    /// </summary>
    public sealed record class SubscriptionNotificationModel :
        IOpcUaSubscriptionNotification
    {
        /// <inheritdoc/>
        public uint SequenceNumber { get; set; }

        /// <inheritdoc/>
        public MessageType MessageType { get; set; }

        /// <inheritdoc/>
        public PublishedDataSetMetaDataModel? MetaData { get; set; }

        /// <inheritdoc/>
        public string? SubscriptionName { get; set; }

        /// <inheritdoc/>
        public string? DataSetName { get; set; }

        /// <inheritdoc/>
        public ushort SubscriptionId { get; set; }

        /// <inheritdoc/>
        public string? EndpointUrl { get; set; }

        /// <inheritdoc/>
        public string? ApplicationUri { get; set; }

        /// <inheritdoc/>
        public DateTimeOffset? PublishTimestamp { get; set; }

        /// <inheritdoc/>
        public DateTimeOffset CreatedTimestamp { get; }

        /// <inheritdoc/>
        public uint? PublishSequenceNumber { get; set; }

        /// <inheritdoc/>
        public object? Context { get; set; }

        /// <inheritdoc/>
        public IServiceMessageContext ServiceMessageContext { get; set; }

        /// <inheritdoc/>
        public IList<MonitoredItemNotificationModel> Notifications { get; set; }
            = Array.Empty<MonitoredItemNotificationModel>();

        /// <summary>
        /// Create subscription notification
        /// </summary>
        /// <param name="createdTimestamp"></param>
        /// <param name="serviceMessageContext"></param>
        public SubscriptionNotificationModel(DateTimeOffset createdTimestamp,
            IServiceMessageContext serviceMessageContext)
        {
            CreatedTimestamp = createdTimestamp;
            ServiceMessageContext = serviceMessageContext;
        }

        /// <inheritdoc/>
        public bool TryUpgradeToKeyFrame()
        {
            // Not supported
            return false;
        }

        /// <inheritdoc/>
        public IEnumerable<IOpcUaSubscriptionNotification> Split(
            Func<MonitoredItemNotificationModel, object?> selector)
        {
            return this.YieldReturn();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            // Nothing to do
        }
    }
}
