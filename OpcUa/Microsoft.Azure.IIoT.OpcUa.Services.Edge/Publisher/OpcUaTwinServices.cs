// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Services.Edge.Publisher {
    using Microsoft.Azure.IIoT.OpcUa.Services.Edge.Models;
    using Microsoft.Azure.IIoT.OpcUa.Services.Protocol;
    using Microsoft.Azure.IIoT.OpcUa.Services.Models;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Edge;
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
    /// Maintains twin state for the lifetime of the twin
    /// </summary>
    public class OpcUaTwinServices : IOpcUaTwinServices, IDisposable {

        /// <summary>
        /// Current endpoint or null if not yet provisioned
        /// </summary>
        public EndpointModel Endpoint { get; set; }

        /// <summary>
        /// Create edge twin services
        /// </summary>
        /// <param name="client"></param>
        public OpcUaTwinServices(IOpcUaClient client, ITwinProperties twin,
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
            var command = new OpcUaTwinServiceCommand(request, OpcUaTwinServiceCommand.Change);
            _commandQueue.Add(command);
            return command.Tcs.Task;
        }

        /// <summary>
        /// Enable or disable publishing based on desired property
        /// </summary>
        /// <param name="nodeId"></param>
        /// <returns></returns>
        public Task NodePublishAsync(string nodeId, bool? enable) {
            var command = new OpcUaTwinServiceCommand(new PublishRequestModel {
                NodeId = nodeId,
                Enabled = enable
            }, OpcUaTwinServiceCommand.Set);
            _commandQueue.Add(command);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Clean up
        /// </summary>
        public void Dispose() {
            if (_runner != null) {
                _commandQueue.Add(new OpcUaTwinServiceCommand(OpcUaTwinServiceCommand.Exit));
                _runner.Wait();
                _runner = null;
            }
        }

        /// <summary>
        /// Refresh subscriptions
        /// </summary>
        private void PostRefresh() {
            _commandQueue.Add(new OpcUaTwinServiceCommand(OpcUaTwinServiceCommand.Refresh));
        }

        /// <summary>
        /// Publish subscriptions
        /// </summary>
        private void PostPublish(object state) {
            _commandQueue.Add(new OpcUaTwinServiceCommand(OpcUaTwinServiceCommand.Publish));
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

                OpcUaTwinServiceCommand item = null;
                if (item == null) {
                    item = _commandQueue.Take();
                    Debug.Assert(item != null);
                }

                // Process exit command
                if (item.Type == OpcUaTwinServiceCommand.Exit) {
                    _logger.Debug("Exiting twin command processor", () => { });
                    break;
                }

                if (!_subscriptions.Any()) {

                    // Stay idle if we do not have any subscriptions, but are asked to refresh...
                    if (item.Type == OpcUaTwinServiceCommand.Refresh) {
                        item = null;
                        continue;
                    }

                    // ... or to publish
                    if (item.Type == OpcUaTwinServiceCommand.Publish) {
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
                            /**/ if (item.Type == OpcUaTwinServiceCommand.Change) {
                                await ChangeSubscriptionsAsync(item, session, true);
                            }
                            else if (item.Type == OpcUaTwinServiceCommand.Set) {
                                await ChangeSubscriptionsAsync(item, session);
                            }
                            else if (item.Type == OpcUaTwinServiceCommand.Publish) {
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
        /// <param name="item"></param>
        private async Task ChangeSubscriptionsAsync(OpcUaTwinServiceCommand item,
            Session session, bool validate = false) {

            // Get subscription in question
            if (item.Request.PublishingInterval == null) {
                item.Request.PublishingInterval = 1000;
            }

            var start = false;
            var key = (int)item.Request.PublishingInterval;
            if (!_subscriptions.TryGetValue(key, out var subscription)) {

                subscription = new OpcUaSubscription(_events.Enqueue, _logger);
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
                    _commandQueue.Add(new OpcUaTwinServiceCommand(OpcUaTwinServiceCommand.Publish));
                }
            }
            catch (Exception ex) {
                _logger.Error("Failure publishing", () => ex);
            }
        }

        /// <summary>
        /// Command to process
        /// </summary>
        private class OpcUaTwinServiceCommand {

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
            public OpcUaTwinServiceCommand(string type) :
                this(null, type) {
            }

            /// <summary>
            /// Create publish command
            /// </summary>
            /// <param name="type"></param>
            public OpcUaTwinServiceCommand(PublishRequestModel request, string type) {
                Request = request;
                Type = type;
                Tcs = new TaskCompletionSource<PublishResultModel>();
            }
        }

        private readonly Dictionary<int, OpcUaSubscription> _subscriptions =
            new Dictionary<int, OpcUaSubscription>();
        private readonly ConcurrentQueue<PublishedValueModel> _events =
            new ConcurrentQueue<PublishedValueModel>();
        private readonly BlockingCollection<OpcUaTwinServiceCommand> _commandQueue =
            new BlockingCollection<OpcUaTwinServiceCommand>();
        private Task _runner;

        private readonly IOpcUaClient _client;
        private readonly ILogger _logger;
        private readonly ITwinProperties _twin;
        private readonly IEventEmitter _telemetry;
        private readonly Timer _timer;
        private const int kInterval = 3000;
    }
}
