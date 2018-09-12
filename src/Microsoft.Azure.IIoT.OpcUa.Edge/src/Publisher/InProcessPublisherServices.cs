// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher {
    using Microsoft.Azure.IIoT.OpcUa.Edge.Models;
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.OpcUa.Models;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Module.Framework;
    using Newtonsoft.Json;
    using Opc.Ua;
    using Opc.Ua.Client;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// A simple publisher implementation.
    /// </summary>
    public class InProcessPublisherServices : IPublisherServices, IDisposable {

        /// <summary>
        /// Current endpoint or null if not yet provisioned
        /// </summary>
        public EndpointModel Endpoint { get; set; }

        /// <summary>
        /// Create in process publisher service
        /// </summary>
        /// <param name="client"></param>
        /// <param name="twin"></param>
        /// <param name="telemetry"></param>
        /// <param name="logger"></param>
        public InProcessPublisherServices(IEndpointServices client, ITwinProperties twin,
            IEventEmitter telemetry, ILogger logger) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _twin = twin ?? throw new ArgumentNullException(nameof(twin));
            _telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
            _timer = new Timer(PostPublish, this, Timeout.Infinite, Timeout.Infinite);
            _runner = Task.Run(RunAsync);
        }

        /// <summary>
        /// Update endpoint information
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        public Task SetEndpointAsync(EndpointModel endpoint) {
            if (Endpoint != endpoint) {
                _logger.Info("Updating endpoint", () => new {
                    Old = Endpoint,
                    New = endpoint
                });
                Endpoint = endpoint;
                PostRefresh();
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Process publish request coming from method call
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public Task<PublishResultModel> NodePublishAsync(
            PublishRequestModel request) {
            var command = new TwinServiceCommand(request, TwinServiceCommand.Change);
            _commandQueue.Add(command);
            return command.Tcs.Task;
        }

        /// <summary>
        /// Enable or disable publishing based on desired property
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="enable"></param>
        /// <returns></returns>
        public Task NodePublishAsync(string nodeId, bool? enable) {
            var command = new TwinServiceCommand(new PublishRequestModel {
                NodeId = nodeId,
                Enabled = enable
            }, TwinServiceCommand.Set);
            _commandQueue.Add(command);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public void Dispose() {
            if (_runner != null) {
                _commandQueue.Add(new TwinServiceCommand(TwinServiceCommand.Exit));
                _runner.Wait();
                _runner = null;
            }
        }

        /// <summary>
        /// Refresh subscriptions
        /// </summary>
        private void PostRefresh() {
            _commandQueue.Add(new TwinServiceCommand(TwinServiceCommand.Refresh));
        }

        /// <summary>
        /// Publish subscriptions
        /// </summary>
        private void PostPublish(object state) {
            _commandQueue.Add(new TwinServiceCommand(TwinServiceCommand.Publish));
        }

        /// <summary>
        /// Run twin service
        /// </summary>
        /// <returns></returns>
        private async Task RunAsync() {
            while(true) {
                // Stop publishing
                _timer.Change(Timeout.Infinite, Timeout.Infinite);
                _logger.Debug("Publishing idle...", () => { });

                TwinServiceCommand item = null;
                if (item == null) {
                    item = _commandQueue.Take();
                    Debug.Assert(item != null);
                }

                // Process exit command
                if (item.Type == TwinServiceCommand.Exit) {
                    _logger.Debug("Exiting twin command processor", () => { });
                    break;
                }

                if (!_subscriptions.Any()) {

                    // Stay idle if we do not have any subscriptions, but are asked to refresh...
                    if (item.Type == TwinServiceCommand.Refresh) {
                        item = null;
                        continue;
                    }

                    // ... or to publish
                    if (item.Type == TwinServiceCommand.Publish) {
                        item = null;
                        await PublishSubscriptionsAsync();
                        continue;
                    }
                }

                // Start publishing
                _logger.Debug("Publishing processor starting...", () => { });
                _timer.Change(0, kInterval);

                try {
                    // Otherwise, start to manage subscriptions on a corresponding session.
                    await _client.ExecuteServiceAsync(Endpoint, async session => {
                        var keepAliveError = false;
                        session.KeepAlive += (s, e) => {
                            if (e != null && e.Status != null && ServiceResult.IsBad(e.Status) &&
                                !keepAliveError) {
                                // Refresh and reattach a new session...
                                e.CancelKeepAlive = keepAliveError = true;
                                PostRefresh();
                            }
                        };
                        await StartSubscriptionsAsync(session);
                        _logger.Debug("Subscriptions started.", () => { });
                        do {
                            //
                            // Using this session, process subscription changes until there
                            // are no more subscriptions in the twin, in which case we go
                            // back to idle waiting for commands...
                            //
                            if (item == null) {
                                item = _commandQueue.Take();
                                Debug.Assert(item != null);
                            }
                            /**/ if (item.Type == TwinServiceCommand.Change) {
                                await ChangeSubscriptionsAsync(item, session, true);
                            }
                            else if (item.Type == TwinServiceCommand.Set) {
                                await ChangeSubscriptionsAsync(item, session);
                            }
                            else if (item.Type == TwinServiceCommand.Publish) {
                                await PublishSubscriptionsAsync();
                            }
                            else {
                                // Refresh or exit process outside...
                                break;
                            }
                            item = null;
                        }
                        while (_subscriptions.Any());

                        await StopSubscriptionsAsync(session);
                        _logger.Debug("Subscriptions stopped.", () => { });
                        return keepAliveError;
                    },
                    ex => false); // Do not reconnect automatically

                    // Refresh, exit, or idle...
                }
                catch (Exception ex) {
                    await StopSubscriptionsAsync(null);
                    _logger.Debug("Publishing processor error...", () => ex);

                    if (item != null) {
                        item.Tcs.TrySetException(ex);
                        item = null;
                    }
                }
            }
        }

        /// <summary>
        /// Start existing subscriptions in this session
        /// </summary>
        /// <param name="session"></param>
        private async Task StartSubscriptionsAsync(Session session) {
            foreach (var sub in _subscriptions) {
                sub.Value.Start(session);
            }
            var state = _subscriptions.SelectMany(
                s => s.Value.Monitored.ToDictionary(kv => kv.Key,
                    kv => (dynamic)true));
            await _telemetry.SendAsync(state);
        }

        /// <summary>
        /// Stop subscriptions
        /// </summary>
        private async Task StopSubscriptionsAsync(Session session) {
            foreach (var sub in _subscriptions) {
                sub.Value.Stop(session);
            }
            var state = _subscriptions.SelectMany(
                s => s.Value.Monitored.ToDictionary(kv => kv.Key,
                    kv => (dynamic)false));
            await _telemetry.SendAsync(state);
        }

        /// <summary>
        /// Change subscriptions
        /// </summary>
        /// <param name="session"></param>
        /// <param name="validate"></param>
        /// <param name="item"></param>
        private async Task ChangeSubscriptionsAsync(TwinServiceCommand item,
            Session session, bool validate = false) {

            // Get subscription in question
            if (item.Request.PublishingInterval == null) {
                item.Request.PublishingInterval = 1000;
            }

            var start = false;
            var key = (int)item.Request.PublishingInterval;
            if (!_subscriptions.TryGetValue(key, out var subscription)) {

                subscription = new PublisherSubscription(_events.Enqueue, _logger);
                _subscriptions.Add(key, subscription);

                start = true;
            }

            // Change the subscription
            var result = subscription.Change(validate ? session : null, item.Request);
            if (!subscription.Monitored.Any()) {
                _subscriptions.Remove(key);
                if (!start) {
                    subscription.Stop(session);
                }
                start = false;
            }

            // Notify result - if we validate, we would have failed already.
            item.Tcs.TrySetResult(result);

            // This should throw if session is invalid, in this case refresh
            if (start) {
                subscription.Start(session);
            }
            await _telemetry.SendAsync(item.Request.NodeId, item.Request.Enabled);
        }

        /// <summary>
        /// Publish any received items
        /// </summary>
        /// <returns></returns>
        private async Task PublishSubscriptionsAsync() {
            var count = Math.Min(_events.Count, 10);
            if (count == 0) {
                _logger.Debug($"Nothing to publish...", () => { });
                return;
            }
            var messages = _events
                .Take(count)
                .Select(JsonConvertEx.SerializeObject)
                .Select(Encoding.UTF8.GetBytes);
            try {
                await _telemetry.SendAsync(messages, "application/x-publish-v1-json");
                for (var i = 0; i < count && _events.TryDequeue(out var tmp); i++) {}
                _logger.Debug($"Published {count} events ({_events.Count}).", () => { });
                if (_events.Count > 0) {
                    _commandQueue.Add(new TwinServiceCommand(TwinServiceCommand.Publish));
                }
            }
            catch (Exception ex) {
                _logger.Error("Failure publishing", () => ex);
            }
        }

        /// <summary>
        /// Command to process
        /// </summary>
        private class TwinServiceCommand {

            public const string Change = nameof(Change);
            public const string Set = nameof(Set);
            public const string Publish = nameof(Publish);
            public const string Refresh = nameof(Refresh);
            public const string Exit = nameof(Exit);

            /// <summary> Done when completed </summary>
            public TaskCompletionSource<PublishResultModel> Tcs { get; }

            /// <summary> Request </summary>
            public PublishRequestModel Request { get; }

            /// <summary> Command type </summary>
            public string Type { get; }

            /// <summary>
            /// Create publish command
            /// </summary>
            /// <param name="type"></param>
            public TwinServiceCommand(string type) :
                this(null, type) {
            }

            /// <summary>
            /// Create publish command
            /// </summary>
            /// <param name="request"></param>
            /// <param name="type"></param>
            public TwinServiceCommand(PublishRequestModel request, string type) {
                Request = request;
                Type = type;
                Tcs = new TaskCompletionSource<PublishResultModel>();
            }
        }

        /// <summary>
        /// Class to manage OPC subscriptions. We create a subscription
        /// for each different publishing interval.  This class is not
        /// thread safe and can only be accessed from a single thread.
        /// </summary>
        private class PublisherSubscription {

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
            public PublisherSubscription(Action<PublishedValueModel> events, ILogger logger) {
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
                Subscription = new Opc.Ua.Client.Subscription {
                    PublishingInterval = RequestedPublishingInterval
                };
                session.AddSubscription((Opc.Ua.Client.Subscription)Subscription);
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
                            session.RemoveSubscription((Opc.Ua.Client.Subscription)Subscription);
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
            /// <param name="session"></param>
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
            /// <param name="nodeId"></param>
            /// <param name="samplingInterval"></param>
            /// <param name="displayName"></param>
            /// <returns></returns>
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
                /// <param name="handler"></param>
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

        private readonly Dictionary<int, PublisherSubscription> _subscriptions =
            new Dictionary<int, PublisherSubscription>();
        private readonly ConcurrentQueue<PublishedValueModel> _events =
            new ConcurrentQueue<PublishedValueModel>();
        private readonly BlockingCollection<TwinServiceCommand> _commandQueue =
            new BlockingCollection<TwinServiceCommand>();
        private Task _runner;

        private readonly IEndpointServices _client;
        private readonly ILogger _logger;
        private readonly ITwinProperties _twin;
        private readonly IEventEmitter _telemetry;
        private readonly Timer _timer;
        private const int kInterval = 3000;
    }
}
