// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Handlers {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Serilog;
    using Microsoft.Azure.IIoT.Hub;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Server discovery result handling
    /// </summary>
    public sealed class DiscoveryEventHandler : IDeviceEventHandler {

        /// <inheritdoc/>
        public string ContentType => ContentTypes.DiscoveryEvents;

        /// <summary>
        /// Create handler
        /// </summary>
        /// <param name="registry"></param>
        /// <param name="logger"></param>
        public DiscoveryEventHandler(IDiscoveryProcessor registry, ILogger logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        /// <inheritdoc/>
        public async Task HandleAsync(string deviceId, string moduleId,
            byte[] payload, Func<Task> checkpoint) {
            var json = Encoding.UTF8.GetString(payload);
            DiscoveryEventModel discovery;
            try {
                discovery = JsonConvertEx.DeserializeObject<DiscoveryEventModel>(json);
            }
            catch (Exception ex) {
                _logger.Error(ex, "Failed to convert discovery result {json}", json);
                return;
            }
            try {
                var supervisorId = SupervisorModelEx.CreateSupervisorId(
                    deviceId, moduleId?.ToString());

                await ProcessServerEndpointDiscoveryAsync(supervisorId,
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

                var removed = new List<KeyValuePair<DateTime, SupervisorDiscoveryResult>>();
                foreach (var backlog in _supervisorQueues.ToList()) {
                    foreach (var queue in backlog.Value
                        .Where(kv => kv.Key < old).ToList()) {
                        backlog.Value.Remove(queue.Key);
                        removed.Add(queue);
                    }
                    if (backlog.Value.Count == 0) {
                        _supervisorQueues.Remove(backlog.Key);
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
        /// <param name="supervisorId"></param>
        /// <param name="model"></param>
        /// <param name="checkpoint"></param>
        /// <returns></returns>
        private async Task ProcessServerEndpointDiscoveryAsync(
            string supervisorId, DiscoveryEventModel model, Func<Task> checkpoint) {
            try {
                await _queueLock.WaitAsync();

                if (!_supervisorQueues.TryGetValue(supervisorId, out var backlog)) {
                    backlog = new Dictionary<DateTime, SupervisorDiscoveryResult>();
                    _supervisorQueues.Add(supervisorId, backlog);
                }
                if (!backlog.TryGetValue(model.TimeStamp, out var queue)) {
                    queue = new SupervisorDiscoveryResult(checkpoint);
                    backlog.Add(model.TimeStamp, queue);
                }
                queue.Enqueue(model);
                if (queue.Completed) {
                    try {
                        // Process discoveries
                        await _registry.ProcessDiscoveryResultsAsync(supervisorId,
                            queue.Result, queue.Events);
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
                    if (!_supervisorQueues.Any(d => d.Value
                            .Any(x => x.Value.Created <= queue.Created))) {
                        await queue.Checkpoint();
                    }
                }
                if (backlog.Count == 0) {
                    _supervisorQueues.Remove(supervisorId);
                }
            }
            finally {
                _queueLock.Release();
            }
        }

        /// <summary>
        /// Keeps queue of discovery messages per device
        /// </summary>
        private class SupervisorDiscoveryResult {

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
            public SupervisorDiscoveryResult(Func<Task> checkpoint) {
                Checkpoint = checkpoint;
                _endpoints = new List<DiscoveryEventModel>();
                _maxIndex = 0;
            }

            /// <summary>
            /// Add another discovery from supervisor
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
            Dictionary<DateTime, SupervisorDiscoveryResult>> _supervisorQueues =
            new Dictionary<string,
                Dictionary<DateTime, SupervisorDiscoveryResult>>();
        private readonly SemaphoreSlim _queueLock = new SemaphoreSlim(1);

        private readonly ILogger _logger;
        private readonly IDiscoveryProcessor _registry;
    }
}
