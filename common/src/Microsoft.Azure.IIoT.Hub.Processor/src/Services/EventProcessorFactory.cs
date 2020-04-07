// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Processor.Services {
    using Microsoft.Azure.IIoT.Messaging;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Azure.EventHubs;
    using Microsoft.Azure.EventHubs.Processor;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Collections;
    using System.Diagnostics;

    /// <summary>
    /// Default event hub event processor factory.
    /// </summary>
    public sealed class EventProcessorFactory : IEventProcessorFactory {

        /// <summary>
        /// Create processor factory
        /// </summary>
        /// <param name="handler"></param>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        public EventProcessorFactory(IEventProcessingHandler handler,
            IEventProcessorConfig config, ILogger logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <inheritdoc/>
        public IEventProcessor CreateEventProcessor(PartitionContext context) {
            return new DefaultProcessor(this, _logger);
        }

        /// <summary>
        /// Processor implementation
        /// </summary>
        private class DefaultProcessor : IEventProcessor {

            /// <summary>
            /// Create processor
            /// </summary>
            /// <param name="factory"></param>
            /// <param name="logger"></param>
            public DefaultProcessor(EventProcessorFactory factory, ILogger logger) {
                _logger = logger ?? throw new ArgumentNullException(nameof(logger));
                _factory = factory ?? throw new ArgumentNullException(nameof(factory));
                _processorId = Guid.NewGuid().ToString();
                _interval = (long?)_factory._config.CheckpointInterval?.TotalMilliseconds
                    ?? long.MaxValue;
                _sw = Stopwatch.StartNew();
                logger.Information("EventProcessor {id} created", _processorId);
            }

            /// <inheritdoc/>
            public async Task ProcessEventsAsync(PartitionContext context,
                IEnumerable<EventData> messages) {
                if (messages == null || !messages.Any()) {
                    return;
                }
                foreach (var eventData in messages) {
                    var properties = new EventProperties(eventData.SystemProperties,
                        eventData.Properties);
                    if (eventData.Body.Array == null) {
                        _logger.Verbose("WARNING: Received empty message with properties {@properties}",
                            properties);
                        continue;
                    }
                    await _factory._handler.HandleAsync(eventData.Body.Array, properties,
                        () => CheckpointAsync(context, eventData));
                }

                // Checkpoint if needed
                if (_sw.ElapsedMilliseconds >= _interval) {
                    try {
                        _logger.Debug("Checkpointing partition {partitionId}...", context.PartitionId);
                        await context.CheckpointAsync();
                        _sw.Restart();
                    }
                    catch (Exception ex) {
                        _logger.Debug(ex, "Failed checkpointing partition {partitionId}...",
                            context.PartitionId);
                        if (_sw.ElapsedMilliseconds >= 2 * _interval) {
                            // Give up checkpointing after trying a couple more times
                            _sw.Restart();
                        }
                    }
                }
                await Try.Async(_factory._handler.OnBatchCompleteAsync);
            }

            /// <inheritdoc/>
            public Task OpenAsync(PartitionContext context) {
                _logger.Information("Partition for {id} opened", _processorId);
                return Task.CompletedTask;
            }

            /// <inheritdoc/>
            public Task ProcessErrorAsync(PartitionContext context, Exception error) {
                _logger.Warning(error, "Processor {id} error", _processorId);
                return Task.CompletedTask;
            }

            /// <inheritdoc/>
            public Task CloseAsync(PartitionContext context, CloseReason reason) {
                _logger.Information("Partition {id} closed ({reason})", _processorId, reason);
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
                    _logger.Debug("Checkpointing partition {partition} with {event}...",
                        eventData, context.PartitionId);
                    await context.CheckpointAsync(eventData).ConfigureAwait(false);
                }
                catch {
                    _logger.Debug("Failed to checkpoint event {event}", eventData);
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
                        return value == item.Value.ToString();
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
            private readonly EventProcessorFactory _factory;
            private readonly string _processorId;
            private readonly long? _interval;
            private readonly Stopwatch _sw;
        }

        private readonly ILogger _logger;
        private readonly IEventProcessingHandler _handler;
        private readonly IEventProcessorConfig _config;
    }
}
