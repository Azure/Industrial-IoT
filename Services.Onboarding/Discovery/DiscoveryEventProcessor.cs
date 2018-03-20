// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.Services.Onboarding.EventHub {
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.Models;
    using Microsoft.Azure.IoTSolutions.Common.Diagnostics;
    using Microsoft.Azure.EventHubs;
    using Microsoft.Azure.EventHubs.Processor;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Threading;

    /// <summary>
    /// Processor implementation
    /// </summary>
    public class DiscoveryEventProcessor : IEventProcessor {

        /// <summary>
        /// Create processor
        /// </summary>
        /// <param name="registry"></param>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        public DiscoveryEventProcessor(IOpcUaRegistryMaintenance registry,
            IEventProcessorConfig config, ILogger logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _processorId = Guid.NewGuid().ToString();
            logger.Info("EventProcessor created", () => new { _processorId });
        }

        /// <summary>
        /// Handle events
        /// </summary>
        /// <param name="context"></param>
        /// <param name="messages"></param>
        /// <returns></returns>
        public async Task ProcessEventsAsync(PartitionContext context,
            IEnumerable<EventData> messages) {
            if (messages == null) {
                return;
            }

            foreach (var eventData in messages) {
                if (!eventData.Properties.TryGetValue("$$ContentType", out var contentType) ||
                    !contentType.ToString().Equals("application/x-discovery-v1-json",
                        StringComparison.InvariantCultureIgnoreCase)) {
                    // Not our content to process
                    continue;
                }
                if (!eventData.Properties.TryGetValue("iothub-connection-device-id", out var id)) {
                    // Not a device message
                    _logger.Error("Unexpected!  Missing device id property", () => eventData);
                    continue;
                }
                var json = Encoding.UTF8.GetString(eventData.Body.Array);
                DiscoveryEventModel discovery;
                try {
                    discovery = JsonConvertEx.DeserializeObject<DiscoveryEventModel>(json);
                }
                catch (Exception ex) {
                    _logger.Error("Failed to convert discovery json", () => new { json, ex });
                    continue;
                }
                try {
                    await ProcessServerEndpointDiscoveryAsync(context, id.ToString(), discovery,
                        eventData);
                }
                catch (AggregateException ex) {
                    _logger.Error($"Processing failed - skip", () => ex);
                }
            }

            await CleanupAsync(context);
        }

        /// <summary>
        /// Open
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public Task OpenAsync(PartitionContext context) {
            _logger.Info("Partition opened", () => new { _processorId, context });
            return Task.CompletedTask;
        }

        /// <summary>
        /// Handle processor error
        /// </summary>
        /// <param name="context"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        public Task ProcessErrorAsync(PartitionContext context, Exception error) {
            _logger.Warn("Processor error", () => new { _processorId, context, error });
            return Task.CompletedTask;
        }

        /// <summary>
        /// Close
        /// </summary>
        /// <param name="context"></param>
        /// <param name="reason"></param>
        /// <returns></returns>
        public Task CloseAsync(PartitionContext context, CloseReason reason) {
            _logger.Info("Partition closed", () => new { _processorId, context, reason });
            return Task.CompletedTask;
        }

        /// <summary>
        /// Process discovery model
        /// </summary>
        /// <param name="supervisorId"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        private async Task ProcessServerEndpointDiscoveryAsync(
            PartitionContext context, string supervisorId,
            DiscoveryEventModel model, EventData checkpoint) {
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
                    // Process discoveries
                    await _registry.ProcessSupervisorDiscoveryAsync(supervisorId,
                        queue.Endpoints);

                    backlog.Remove(model.TimeStamp);
                    //
                    // Check if there are any older queues still in the list.
                    // If not then checkpoint this queue.
                    //
                    if (!_supervisorQueues.Any(d => d.Value
                            .Any(x => x.Value.Created <= queue.Created))) {
                        await context.CheckpointAsync(queue.Checkpoint);
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
        /// Cleanup incomplete items
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private async Task CleanupAsync(PartitionContext context) {
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
                        await context.CheckpointAsync(newest.Checkpoint);
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
        /// Keeps queue of discovery messages per device
        /// </summary>
        private class SupervisorDiscoveryResult {

            /// <summary>
            /// Checkpointable event data
            /// </summary>
            public EventData Checkpoint { get; }

            /// <summary>
            /// When queue was created
            /// </summary>
            public DateTime Created { get; } = DateTime.UtcNow;

            /// <summary>
            /// Whether the result is complete
            /// </summary>
            public bool Completed =>
                _complete && _maxIndex == _endpoints.Count;

            /// <summary>
            /// Try get results
            /// </summary>
            /// <returns></returns>
            public IEnumerable<DiscoveryEventModel> Endpoints {
                get {
                    _endpoints.Sort((x, y) => x.Index - y.Index);
                    return _endpoints;
                }
            }

            /// <summary>
            /// Create queue
            /// </summary>
            public SupervisorDiscoveryResult(EventData checkpoint) {
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
                if (model.Endpoint != null) {
                    _endpoints.Add(model);
                }
                else {
                    _complete = true;
                }
            }

            private readonly List<DiscoveryEventModel> _endpoints;
            private bool _complete;
            private int _maxIndex;
        }

        private readonly Dictionary<string,
            Dictionary<DateTime, SupervisorDiscoveryResult>> _supervisorQueues =
            new Dictionary<string,
                Dictionary<DateTime, SupervisorDiscoveryResult>>();
        private readonly SemaphoreSlim _queueLock = new SemaphoreSlim(1);

        private readonly ILogger _logger;
        private readonly IEventProcessorConfig _config;
        private readonly IOpcUaRegistryMaintenance _registry;
        private readonly string _processorId;
    }
}