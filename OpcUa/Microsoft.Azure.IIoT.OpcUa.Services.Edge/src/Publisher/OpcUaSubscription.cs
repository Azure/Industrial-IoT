// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Services.Edge.Publisher {
    using Microsoft.Azure.IIoT.OpcUa.Services.Edge.Models;
    using Microsoft.Azure.IIoT.OpcUa.Services.Models;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Opc.Ua;
    using Opc.Ua.Client;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Class to manage OPC subscriptions. We create a subscription
    /// for each different publishing interval.  This class is not
    /// thread safe and can only be accessed from a single thread.
    /// </summary>
    class OpcUaSubscription {

        /// <summary> Requested interval </summary>
        public int RequestedPublishingInterval { get; set; }

        /// <summary> Opc subscription </summary>
        public Subscription Subscription { get; set; }

        /// <summary> Monitored items in the subscription </summary>
        internal Dictionary<string, OpcUaMonitoredItem> Monitored { get; }

        /// <summary>
        /// Create subscription wrapper
        /// </summary>
        /// <param name="events"></param>
        /// <param name="logger"></param>
        public OpcUaSubscription(Action<PublishedValueModel> events, ILogger logger) {
            _event = events ??
                throw new ArgumentNullException(nameof(events));
            _logger = logger ??
                throw new ArgumentNullException(nameof(logger));
            Monitored = new Dictionary<string, OpcUaMonitoredItem>();
        }

        /// <summary>
        /// Start a subscription in the session.
        /// </summary>
        public void Start(Session session) {
            if (Subscription != null || session == null) {
                return;
            }
            Subscription = new Subscription {
                PublishingInterval = RequestedPublishingInterval
            };
            session.AddSubscription(Subscription);
            Subscription.Create();
            if (!Monitored.Any()) {
                return;
            }
            // Add existing monitored items
            var old = new Dictionary<string, OpcUaMonitoredItem>(Monitored);
            Monitored.Clear();
            foreach (var item in old) {
                Monitored.Add(item.Key, new OpcUaMonitoredItem(item.Value));
            }
            Subscription.AddItems(Monitored.Values.Select(s => s.Item));
            Subscription.SetPublishingMode(true);
            Subscription.ApplyChanges();
        }

        /// <summary>
        /// Stop subscription
        /// </summary>
        public void Stop(Session session) {
            if (Subscription != null) {
                if (session != null) {
                    try {
                        session.RemoveSubscription(Subscription);
                    }
                    catch (ServiceResultException) { }
                }
                else {
                    Subscription.Delete(true);
                }
                Subscription.Dispose();
                Subscription = null;
            }
        }

        /// <summary>
        /// Create new monitored item
        /// </summary>
        /// <param name="request"></param>
        public PublishResultModel Change(Session session, PublishRequestModel request) {
            if (request.Enabled ?? false) {
                // Add
                if (Monitored.ContainsKey(request.NodeId)) {
                    return new PublishResultModel { Diagnostics = "Already monitored" };
                }

                var nodeId = NodeId.Parse(request.NodeId);  // TODO: Update in context of session
                if (session != null) {
                    // Validate
                    var node = session.ReadNode(nodeId);
                    if (string.IsNullOrEmpty(request.DisplayName)) {
                        request.DisplayName = node.DisplayName.Text;
                    }
                }
                var item = new OpcUaMonitoredItem(MonitoredItem_Notification,
                    nodeId, (int)request.PublishingInterval, request.DisplayName);
                Monitored.Add(request.NodeId, item);
                if (Subscription != null) {
                    Subscription.AddItem(item.Item);
                    Subscription.SetPublishingMode(true);
                    Subscription.ApplyChanges();
                }
            }
            else {
                // Remove
                if (!Monitored.TryGetValue(request.NodeId, out var item)) {
                    return new PublishResultModel {
                        Diagnostics = request.Enabled == null ? null : "Not monitored"
                    };
                }
                Monitored.Remove(request.NodeId);
                if (Subscription != null) {
                    Subscription.RemoveItem(item.Item);
                    Subscription.SetPublishingMode(Monitored.Any());
                    Subscription.ApplyChanges();
                }
            }
            _logger.Debug("Request processed", () => request);
            return new PublishResultModel();
        }

        /// <summary>
        /// Create monitored item
        /// </summary>
        /// <param name="samplingInterval"></param>
        private MonitoredItem CreateMonitoredItem(NodeId nodeId,
            int samplingInterval, string displayName = null) {
            var item = new MonitoredItem {
                StartNodeId = nodeId,
                DisplayName = displayName ?? string.Empty,
                AttributeId = Attributes.Value,
                MonitoringMode = MonitoringMode.Reporting,
                SamplingInterval = samplingInterval,
                QueueSize = 0,
                DiscardOldest = true,
            };
            item.Notification += MonitoredItem_Notification;
            return item;
        }

        /// <summary>
        /// The notification that the data for a monitored item has changed
        /// on an server.
        /// </summary>
        private void MonitoredItem_Notification(MonitoredItem monitoredItem,
            MonitoredItemNotificationEventArgs args) {
            try {
                if (args == null ||
                    args.NotificationValue == null ||
                    monitoredItem == null ||
                    monitoredItem.Subscription == null ||
                    monitoredItem.Subscription.Session == null) {
                    return;
                }
                var notification = args.NotificationValue as MonitoredItemNotification;
                if (notification == null) {
                    return;
                }
                var value = notification.Value as DataValue;
                if (value == null) {
                    return;
                }
                _event(new PublishedValueModel {
                    NodeId = monitoredItem.StartNodeId.ToString(),
                    DisplayName = monitoredItem.DisplayName,
                    SourceTimestamp = value.SourceTimestamp,
                    SourcePicoseconds = value.SourcePicoseconds,
                    ServerTimestamp = value.ServerTimestamp,
                    ServerPicoseconds = value.ServerPicoseconds,
                    StatusCode = value.StatusCode.Code,
                    Status = StatusCode.LookupSymbolicId(value.StatusCode.Code)
                });
            }
            catch (Exception e) {
                _logger.Debug("Exception during receive", () => e);
            }
        }

        /// <summary>
        /// Class to manage opc ua monitored items.
        /// </summary>
        internal class OpcUaMonitoredItem {

            /// <summary>
            /// Number of successes
            /// </summary>
            public int SuccessCount { get; set; }

            /// <summary>
            /// Monitored item
            /// </summary>
            public MonitoredItem Item { get; }

            /// <summary>
            /// Create item
            /// </summary>
            /// <param name="nodeId"></param>
            /// <param name="samplingInterval"></param>
            /// <param name="displayName"></param>
            public OpcUaMonitoredItem(MonitoredItemNotificationEventHandler handler,
                NodeId nodeId, int samplingInterval, string displayName = null) {
                _handler = handler;
                Item = Create(handler, nodeId, samplingInterval,
                    displayName);
            }

            /// <summary>
            /// Clone item
            /// </summary>
            /// <param name="item"></param>
            public OpcUaMonitoredItem(OpcUaMonitoredItem item) {
                SuccessCount = item.SuccessCount;
                Item = Create(_handler, item.Item.StartNodeId, item.Item.SamplingInterval,
                    item.Item.DisplayName);
            }

            private MonitoredItem Create(MonitoredItemNotificationEventHandler handler,
                NodeId nodeId, int samplingInterval, string displayName) {
                var item = new MonitoredItem {
                    StartNodeId = nodeId,
                    DisplayName = displayName ?? string.Empty,
                    AttributeId = Attributes.Value,
                    MonitoringMode = MonitoringMode.Reporting,
                    SamplingInterval = samplingInterval,
                    QueueSize = 0,
                    DiscardOldest = true,
                };
                item.Notification += handler;
                return item;
            }

            private readonly MonitoredItemNotificationEventHandler _handler;
        }

        private readonly Action<PublishedValueModel> _event;
        private readonly ILogger _logger;
    }
}
