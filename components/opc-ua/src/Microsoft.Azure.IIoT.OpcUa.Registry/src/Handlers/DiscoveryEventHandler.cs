// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Handlers {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Serializers;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Server discovery result handling
    /// </summary>
    public sealed class DiscoveryEventHandler : IDeviceTelemetryHandler {

        /// <inheritdoc/>
        public string MessageSchema => Models.MessageSchemaTypes.DiscoveryEvents;

        /// <summary>
        /// Create handler
        /// </summary>
        /// <param name="processors"></param>
        /// <param name="serializer"></param>
        /// <param name="logger"></param>
        public DiscoveryEventHandler(IEnumerable<IDiscoveryResultProcessor> processors,
            IJsonSerializer serializer, ILogger logger) {
            _serializer = serializer ??
                throw new ArgumentNullException(nameof(serializer));
            _logger = logger ??
                throw new ArgumentNullException(nameof(logger));
            _processors = processors?.ToList() ??
                throw new ArgumentNullException(nameof(processors));
        }

        /// <inheritdoc/>
        public async Task HandleAsync(string deviceId, string moduleId,
            byte[] payload, IDictionary<string, string> properties, Func<Task> checkpoint) {
            DiscoveryEventModel discovery;
            try {
                discovery = _serializer.Deserialize<DiscoveryEventModel>(payload);
            }
            catch (Exception ex) {
                _logger.Error(ex, "Failed to convert discovery result {json}",
                    Encoding.UTF8.GetString(payload));
                return;
            }
            try {
                var discovererId = DiscovererModelEx.CreateDiscovererId(
                    deviceId, moduleId?.ToString());

                await ProcessServerEndpointDiscoveryAsync(discovererId,
                    discovery, checkpoint);
            }
            catch (Exception ex) {
                _logger.Error(ex, "Handling discovery event failed with exception - skip");
            }
        }

        /// <inheritdoc/>
        public async Task OnBatchCompleteAsync() {
            try {
                await _queueLock.WaitAsync();
                var old = DateTime.UtcNow - TimeSpan.FromHours(1);

                var removed = new List<KeyValuePair<DateTime, DiscovererDiscoveryResult>>();
                foreach (var backlog in _discovererQueues.ToList()) {
                    foreach (var queue in backlog.Value
                        .Where(kv => kv.Key < old).ToList()) {
                        backlog.Value.Remove(queue.Key);
                        removed.Add(queue);
                    }
                    if (backlog.Value.Count == 0) {
                        _discovererQueues.Remove(backlog.Key);
                    }
                }
                //
                // Checkpoint the latest of the ones removed.
                //
                var newest = removed
                    .Select(x => x.Value)
                    .OrderByDescending(x => x.Created).FirstOrDefault();
                if (newest != null) {
                    try {
                        await newest.Checkpoint();
                    }
                    catch {
                        return;
                    }
                }

            }
            finally {
                _queueLock.Release();
            }
        }

        /// <summary>
        /// Process discovery model
        /// </summary>
        /// <param name="discovererId"></param>
        /// <param name="model"></param>
        /// <param name="checkpoint"></param>
        /// <returns></returns>
        private async Task ProcessServerEndpointDiscoveryAsync(
            string discovererId, DiscoveryEventModel model, Func<Task> checkpoint) {
            try {
                await _queueLock.WaitAsync();

                if (!_discovererQueues.TryGetValue(discovererId, out var backlog)) {
                    backlog = new Dictionary<DateTime, DiscovererDiscoveryResult>();
                    _discovererQueues.Add(discovererId, backlog);
                }
                if (!backlog.TryGetValue(model.TimeStamp, out var queue)) {
                    queue = new DiscovererDiscoveryResult(checkpoint);
                    backlog.Add(model.TimeStamp, queue);
                }
                queue.Enqueue(model);
                if (queue.Completed) {
                    try {
                        // Process discoveries
                        await Task.WhenAll(
                            _processors.Select(p => p.ProcessDiscoveryResultsAsync(
                                discovererId, queue.Result, queue.Events)));
                    }
                    catch (Exception ex) {
                        _logger.Error(ex,
                            "Failure during discovery processing in registry. Skip.");
                    }

                    backlog.Remove(model.TimeStamp);
                    //
                    // Check if there are any older queues still in the list.
                    // If not then checkpoint this queue.
                    //
                    if (!_discovererQueues.Any(d => d.Value
                            .Any(x => x.Value.Created <= queue.Created))) {
                        await queue.Checkpoint();
                    }
                }
                if (backlog.Count == 0) {
                    _discovererQueues.Remove(discovererId);
                }
            }
            finally {
                _queueLock.Release();
            }
        }

        /// <summary>
        /// Keeps queue of discovery messages per device
        /// </summary>
        private class DiscovererDiscoveryResult {

            /// <summary>
            /// Checkpointable event data
            /// </summary>
            public Func<Task> Checkpoint { get; }

            /// <summary>
            /// When queue was created
            /// </summary>
            public DateTime Created { get; } = DateTime.UtcNow;

            /// <summary>
            /// Whether the result is complete
            /// </summary>
            public bool Completed =>
                Result != null && _maxIndex == _endpoints.Count;

            /// <summary>
            /// Get events
            /// </summary>
            /// <returns></returns>
            public IEnumerable<DiscoveryEventModel> Events {
                get {
                    _endpoints.Sort((x, y) => x.Index - y.Index);
                    return _endpoints;
                }
            }

            /// <summary>
            /// Result of discovery
            /// </summary>
            public DiscoveryResultModel Result { get; private set; }

            /// <summary>
            /// Create queue
            /// </summary>
            public DiscovererDiscoveryResult(Func<Task> checkpoint) {
                Checkpoint = checkpoint;
                _endpoints = new List<DiscoveryEventModel>();
                _maxIndex = 0;
            }

            /// <summary>
            /// Add another discovery from discoverer
            /// </summary>
            /// <param name="model"></param>
            /// <returns></returns>
            public void Enqueue(DiscoveryEventModel model) {
                _maxIndex = Math.Max(model.Index, _maxIndex);
                if (model.Registration != null) {
                    _endpoints.Add(model);
                }
                else {
                    Result = model.Result ?? new DiscoveryResultModel();
                }
            }

            private readonly List<DiscoveryEventModel> _endpoints;
            private int _maxIndex;
        }

        private readonly Dictionary<string,
            Dictionary<DateTime, DiscovererDiscoveryResult>> _discovererQueues =
            new Dictionary<string,
                Dictionary<DateTime, DiscovererDiscoveryResult>>();
        private readonly SemaphoreSlim _queueLock = new SemaphoreSlim(1, 1);
        private readonly IJsonSerializer _serializer;
        private readonly ILogger _logger;
        private readonly List<IDiscoveryResultProcessor> _processors;
    }
}
