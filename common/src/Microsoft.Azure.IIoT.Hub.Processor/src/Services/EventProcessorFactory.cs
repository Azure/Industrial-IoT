// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Processor.Services {
    using Microsoft.Azure.IIoT.Messaging;
    using Microsoft.Azure.EventHubs;
    using Microsoft.Azure.EventHubs.Processor;
    using Microsoft.Extensions.Logging;
    using Autofac;
    using Furly.Extensions.Utils;
    using Prometheus;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Default event hub event processor factory.
    /// </summary>
    public sealed class EventProcessorFactory : IEventProcessorFactory {
        /// <summary>
        /// Create processor factory
        /// </summary>
        /// <param name="context"></param>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        public EventProcessorFactory(IComponentContext context,
            IEventProcessorConfig config, ILogger logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <inheritdoc/>
        public IEventProcessor CreateEventProcessor(PartitionContext context) {
            return new DefaultProcessor(this, context, _logger);
        }

        /// <summary>
        /// Processor implementation
        /// </summary>
        private class DefaultProcessor : IEventProcessor {
            /// <summary>
            /// Create processor
            /// </summary>
            /// <param name="outer"></param>
            /// <param name="partitionContext"></param>
            /// <param name="logger"></param>
            public DefaultProcessor(EventProcessorFactory outer, PartitionContext partitionContext,
                ILogger logger) {
                _outer = outer ?? throw new ArgumentNullException(nameof(outer));
                _partitionContext = partitionContext ?? throw new ArgumentNullException(nameof(partitionContext));
                _processorId = Guid.NewGuid().ToString();
                _logger = logger /* TODO: Use loggerfactory ?.ForContext("ProcessorId", _processorId) */
                    ?? throw new ArgumentNullException(nameof(logger));

                _handler = outer._context.Resolve<IEventProcessingHandler>();
                _interval = (long?)_outer._config.CheckpointInterval?.TotalMilliseconds
                    ?? long.MaxValue;

                _sw = Stopwatch.StartNew();
                _logger.LogInformation("EventProcessor {Id} for partition {PartitionId} created",
                    _processorId, _partitionContext.PartitionId);
                kEventProcessorDetails.WithLabels(_processorId, _partitionContext.PartitionId, "created").Inc();
            }

            /// <inheritdoc/>
            public async Task ProcessEventsAsync(PartitionContext context,
                IEnumerable<EventData> messages) {
                if (messages?.Any() != true) {
                    return;
                }
                foreach (var eventData in messages) {
                    if (_outer._config.SkipEventsOlderThan != null &&
                        eventData.SystemProperties.TryGetValue("x-opt-enqueued-time", out var enqueued) &&
                        (DateTime)enqueued + _outer._config.SkipEventsOlderThan < DateTime.UtcNow) {
                        continue;
                    }

                    var properties = new EventProperties(eventData.SystemProperties,
                        eventData.Properties);
                    if (eventData.Body.Array == null) {
                        _logger.LogTrace("WARNING: Received empty message with properties {@properties}",
                            properties);
                        continue;
                    }
                    await _handler.HandleAsync(eventData.Body.Array, properties,
                        () => CheckpointAsync(context, eventData)).ConfigureAwait(false);

                    if (context.CancellationToken.IsCancellationRequested) {
                        // Checkpoint to the last processed event.
                        await CheckpointAsync(context, eventData).ConfigureAwait(false);
                        context.CancellationToken.ThrowIfCancellationRequested();
                    }
                }

                // Checkpoint if needed
                if (_sw.ElapsedMilliseconds >= _interval) {
                    try {
                        _logger.LogDebug("Checkpointing EventProcessor {Id} for partition {PartitionId}...",
                            _processorId, context.PartitionId);
                        await context.CheckpointAsync().ConfigureAwait(false);
                        _sw.Restart();
                    }
                    catch (Exception ex) {
                        _logger.LogWarning(ex, "Failed checkpointing EventProcessor {Id} for partition {PartitionId}...",
                            _processorId, context.PartitionId);
                        kEventProcessorDetails.WithLabels(_processorId, context.PartitionId, "checkpoint_failed").Inc();
                        if (_sw.ElapsedMilliseconds >= 2 * _interval) {
                            // Give up checkpointing after trying a couple more times
                            _sw.Restart();
                        }
                    }
                }
                await Try.Async(_handler.OnBatchCompleteAsync).ConfigureAwait(false);
            }

            /// <inheritdoc/>
            public Task OpenAsync(PartitionContext context) {
                _logger.LogInformation("EventProcessor {Id} for partition {PartitionId} opened",
                    _processorId, context.PartitionId);
                kEventProcessorDetails.WithLabels(_processorId, context.PartitionId, "opened").Inc();
                return Task.CompletedTask;
            }

            /// <inheritdoc/>
            public Task ProcessErrorAsync(PartitionContext context, Exception error) {
                if (!(error is OperationCanceledException)) {
                    _logger.LogWarning(error, "EventProcessor {Id} for partition {PartitionId} error",
                        _processorId, context.PartitionId);
                    kEventProcessorDetails.WithLabels(_processorId, context.PartitionId, "error").Inc();
                }
                return Task.CompletedTask;
            }

            /// <inheritdoc/>
            public Task CloseAsync(PartitionContext context, CloseReason reason) {
                _logger.LogInformation("EventProcessor {Id} for partition {PartitionId} closed ({Reason})",
                    _processorId, context.PartitionId, reason);
                kEventProcessorDetails.WithLabels(_processorId, context.PartitionId, "closed").Inc();
                return Task.CompletedTask;
            }

            /// <summary>
            /// Wraps checkpointing
            /// </summary>
            /// <param name="context"></param>
            /// <param name="eventData"></param>
            /// <returns></returns>
            private async Task CheckpointAsync(PartitionContext context, EventData eventData) {
                try {
                    _logger.LogDebug("Checkpointing EventProcessor {Id} for partition {PartitionId} with event with " +
                        "{SequenceNumber} SequenceNumber and {Offset} Offset ...", _processorId, context.PartitionId,
                        eventData.SystemProperties.SequenceNumber, eventData.SystemProperties.Offset);
                    await context.CheckpointAsync(eventData).ConfigureAwait(false);
                }
                catch (Exception ex) {
                    _logger.LogWarning(ex, "Failed to checkpoint EventProcessor {Id} for partition {PartitionId} with " +
                        "event with {SequenceNumber} SequenceNumber and {Offset} Offset", _processorId,
                        context.PartitionId, eventData.SystemProperties.SequenceNumber, eventData.SystemProperties.Offset);
                    kEventProcessorDetails.WithLabels(_processorId, context.PartitionId, "checkpoint_failed").Inc();
                }
                finally {
                    _sw.Restart();
                }
            }

            /// <summary>
            /// Wraps the properties into a string dictionary
            /// </summary>
            private class EventProperties : IDictionary<string, string> {
                /// <summary>
                /// Create properties wrapper
                /// </summary>
                /// <param name="system"></param>
                /// <param name="user"></param>
                internal EventProperties(IDictionary<string, object> system,
                    IDictionary<string, object> user) {
                    _system = system ?? new Dictionary<string, object>();
                    _user = user ?? new Dictionary<string, object>();
                }

                /// <inheritdoc/>
                public ICollection<string> Keys => _user.Keys
                    .Concat(_system.Keys).ToList();

                /// <inheritdoc/>
                public ICollection<string> Values => _user.Values
                    .Select(v => v.ToString())
                    .Concat(_system.Values
                        .Select(v => v.ToString()))
                    .ToList();

                /// <inheritdoc/>
                public int Count =>
                    _system.Count + _user.Count;

                /// <inheritdoc/>
                public bool IsReadOnly => true;

                /// <inheritdoc/>
                public string this[string key] {
                    get {
                        if (!_user.TryGetValue(key, out var result)) {
                            result = _system[key];
                        }
                        return result.ToString();
                    }
                    set => _user[key] = value;
                }

                /// <inheritdoc/>
                public void Add(string key, string value) {
                    _user.Add(key, value);
                }

                /// <inheritdoc/>
                public bool ContainsKey(string key) {
                    return _user.ContainsKey(key) || _system.ContainsKey(key);
                }

                /// <inheritdoc/>
                public bool Remove(string key) {
                    return _user.Remove(key) || _system.Remove(key);
                }

                /// <inheritdoc/>
                public bool TryGetValue(string key, out string value) {
                    if (_user.TryGetValue(key, out var result) ||
                        _system.TryGetValue(key, out result)) {
                        value = result.ToString();
                        return true;
                    }
                    value = null;
                    return false;
                }

                /// <inheritdoc/>
                public void Add(KeyValuePair<string, string> item) {
                    _user.Add(new KeyValuePair<string, object>(item.Key, item.Value));
                }

                /// <inheritdoc/>
                public void Clear() {
                    _user.Clear();
                    _system.Clear();
                }

                /// <inheritdoc/>
                public bool Contains(KeyValuePair<string, string> item) {
                    if (TryGetValue(item.Key, out var value)) {
                        return value == item.Value;
                    }
                    return false;
                }

                /// <inheritdoc/>
                public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex) {
                    var index = arrayIndex;
                    foreach (var item in this) {
                        if (index >= array.Length) {
                            return;
                        }
                        array[index++] = item;
                    }
                }

                /// <inheritdoc/>
                public bool Remove(KeyValuePair<string, string> item) {
                    if (Contains(item)) {
                        return Remove(item.Key);
                    }
                    return false;
                }

                /// <inheritdoc/>
                public IEnumerator<KeyValuePair<string, string>> GetEnumerator() {
                    return _user
            .Select(v => new KeyValuePair<string, string>(v.Key, v.Value.ToString()))
            .Concat(_system
            .Select(v => new KeyValuePair<string, string>(v.Key, v.Value.ToString())))
            .GetEnumerator();
                }

                /// <inheritdoc/>
                IEnumerator IEnumerable.GetEnumerator() {
                    return _user.Concat(_system).GetEnumerator();
                }

                private readonly IDictionary<string, object> _system;
                private readonly IDictionary<string, object> _user;
            }

            private readonly ILogger _logger;
            private readonly IEventProcessingHandler _handler;
            private readonly EventProcessorFactory _outer;
            private readonly string _processorId;
            private readonly long? _interval;
            private readonly Stopwatch _sw;
            private readonly PartitionContext _partitionContext;
            private static readonly Gauge kEventProcessorDetails = Metrics
                .CreateGauge("iiot_event_processor_info", "details about event processor",
                    new GaugeConfiguration {
                        LabelNames = new[] { "id", "partition_id", "status" }
                    });
        }

        private readonly ILogger _logger;
        private readonly IComponentContext _context;
        private readonly IEventProcessorConfig _config;
    }
}
