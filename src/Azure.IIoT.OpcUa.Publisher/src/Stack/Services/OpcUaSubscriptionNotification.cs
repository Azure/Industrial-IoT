// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Services
{
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Azure.IIoT.OpcUa.Encoders.PubSub;
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    /// <summary>
    /// Opc Ua subscription notification
    /// </summary>
    public sealed record class OpcUaSubscriptionNotification : IDisposable
    {
        /// <inheritdoc/>
        public object? Context { get; set; }

        /// <inheritdoc/>
        public uint SequenceNumber { get; internal set; }

        /// <inheritdoc/>
        public MessageType MessageType { get; internal set; }

        /// <inheritdoc/>
        public string? EventTypeName { get; internal set; }

        /// <inheritdoc/>
        public string? EndpointUrl { get; internal set; }

        /// <inheritdoc/>
        public string? ApplicationUri { get; internal set; }

        /// <inheritdoc/>
        public DateTimeOffset? PublishTimestamp { get; internal set; }

        /// <inheritdoc/>
        public uint? PublishSequenceNumber { get; private set; }

        /// <inheritdoc/>
        public IServiceMessageContext ServiceMessageContext { get; private set; }

        /// <inheritdoc/>
        public IList<MonitoredItemNotificationModel> Notifications { get; private set; }

        /// <inheritdoc/>
        public DateTimeOffset CreatedTimestamp { get; }

        /// <summary>
        /// Create acknoledgeable notification
        /// </summary>
        /// <param name="outer"></param>
        /// <param name="messageContext"></param>
        /// <param name="notifications"></param>
        /// <param name="timeProvider"></param>
        /// <param name="advance"></param>
        /// <param name="sequenceNumber"></param>
        internal OpcUaSubscriptionNotification(OpcUaSubscription outer,
            IServiceMessageContext messageContext,
            IList<MonitoredItemNotificationModel> notifications,
            TimeProvider timeProvider, IDisposable? advance = null,
            uint? sequenceNumber = null)
        {
            _outer = outer;
            _advance = advance;

            PublishSequenceNumber = sequenceNumber;
            CreatedTimestamp = timeProvider.GetUtcNow();
            ServiceMessageContext = messageContext;

            Notifications = notifications;
        }

        internal OpcUaSubscriptionNotification(DateTimeOffset createdTimestamp,
            ServiceMessageContext? serviceMessageContext = null,
            IList<MonitoredItemNotificationModel>? notifications = null)
        {
            _outer = null;
            _advance = null;

            CreatedTimestamp = createdTimestamp;
            ServiceMessageContext = serviceMessageContext ?? new();
            Notifications = notifications ?? Array.Empty<MonitoredItemNotificationModel>();
        }

        /// <summary>
        /// Create an empty notification
        /// </summary>
        /// <param name="template"></param>
        /// <param name="notifications"></param>
        internal OpcUaSubscriptionNotification(OpcUaSubscriptionNotification template,
            IList<MonitoredItemNotificationModel>? notifications = null)
        {
            _outer = null;
            _advance = null;

            Notifications = notifications ?? Array.Empty<MonitoredItemNotificationModel>();
            CreatedTimestamp = template.CreatedTimestamp;
            ServiceMessageContext = template.ServiceMessageContext;
            ApplicationUri = template.ApplicationUri;
            EndpointUrl = template.EndpointUrl;
            EventTypeName = template.EventTypeName;
            MessageType = template.MessageType;
            SequenceNumber = template.SequenceNumber;
        }

        /// <inheritdoc/>
        public bool TryUpgradeToKeyFrame(ISubscriber owner)
        {
            if (_outer != null && _outer.TryGetNotifications(owner, out var allNotifications))
            {
                MessageType = MessageType.KeyFrame;

                Notifications.Clear();
                Notifications.AddRange(allNotifications);
                return true;
            }
            return false;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _advance?.Dispose();
        }

#if DEBUG
        /// <inheritdoc/>
        public void MarkProcessed()
        {
            _processed = true;
        }

        /// <inheritdoc/>
        public void DebugAssertProcessed()
        {
            Debug.Assert(_processed);
        }
        private bool _processed;
#endif

        /// <summary>
        /// Get diagnostics info from message
        /// </summary>
        /// <param name="modelChanges"></param>
        /// <param name="heartbeats"></param>
        /// <param name="overflow"></param>
        /// <returns></returns>
        internal int GetDiagnosticCounters(out int modelChanges, out int heartbeats,
            out int overflow)
        {
            modelChanges = 0;
            heartbeats = 0;
            overflow = 0;
            foreach (var n in Notifications)
            {
                if (n.Flags.HasFlag(MonitoredItemSourceFlags.ModelChanges))
                {
                    modelChanges++;
                }
                else if (n.Flags.HasFlag(MonitoredItemSourceFlags.Heartbeat))
                {
                    heartbeats++;
                }
                overflow += n.Overflow;
            }
            return Notifications.Count;
        }

        private readonly OpcUaSubscription? _outer;
        private readonly IDisposable? _advance;
    }
}
