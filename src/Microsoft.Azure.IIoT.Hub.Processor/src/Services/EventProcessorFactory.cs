// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Processor.Services {
    using Microsoft.Azure.EventHubs;
    using Microsoft.Azure.EventHubs.Processor;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Utils;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Default iot hub event processor factory implementation
    /// </summary>
    public class EventProcessorFactory : IEventProcessorFactory {

        /// <summary>
        /// Create processor factory
        /// </summary>
        /// <param name="handlers"></param>
        /// <param name="logger"></param>
        public EventProcessorFactory(IEnumerable<IEventHandler> handlers,
            ILogger logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            if (handlers == null) {
                throw new ArgumentNullException(nameof(handlers));
            }
            _handlers = handlers.ToDictionary(h => h.ContentType, h => h);
        }

        /// <inheritdoc/>
        public IEventProcessor CreateEventProcessor(PartitionContext context) =>
            new DefaultProcessor(this, _logger);

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
                logger.Info("EventProcessor created", () => new { _processorId });
            }

            /// <inheritdoc/>
            public async Task ProcessEventsAsync(PartitionContext context,
                IEnumerable<EventData> messages) {
                if (messages == null || !messages.Any()) {
                    return;
                }

                var used = new HashSet<IEventHandler>();
                foreach (var eventData in messages) {
                    if (!eventData.Properties.TryGetValue(CommonProperties.kDeviceId,
                            out var deviceId) &&
                        !eventData.SystemProperties.TryGetValue(
                            SystemProperties.ConnectionDeviceId, out deviceId)) {
                        // Not our content to process
                        continue;
                    }

                    if (!eventData.Properties.TryGetValue(CommonProperties.kContentType,
                            out var contentType) &&
                        !eventData.Properties.TryGetValue(EventProperties.kContentType,
                            out contentType) &&
                        !eventData.SystemProperties.TryGetValue(
                            SystemProperties.ContentType, out contentType)) {
                        // Not our content to process
                        continue;
                    }

                    if (deviceId == null || contentType == null) {
                        // Not our content to process
                        continue;
                    }

                    eventData.Properties.TryGetValue(CommonProperties.kModuleId,
                        out var moduleId);
                    if (moduleId == null) {
                        // TODO:  Try get from system properties
                    }

                    if (_factory._handlers.TryGetValue(contentType.ToString().ToLowerInvariant(),
                        out var handler)) {
                        await handler.HandleAsync(deviceId.ToString(),
                            moduleId?.ToString(), eventData.Body.Array,
                                () => Try.Async(() => context.CheckpointAsync(eventData)));
                        used.Add(handler);
                    }
                }
                foreach (var handler in used) {
                    await Try.Async(handler.OnBatchCompleteAsync);
                }
            }

            /// <inheritdoc/>
            public Task OpenAsync(PartitionContext context) {
                _logger.Info("Partition opened", () => new { _processorId, context });
                return Task.CompletedTask;
            }

            /// <inheritdoc/>
            public Task ProcessErrorAsync(PartitionContext context, Exception error) {
                _logger.Warn("Processor error", () => new { _processorId, context, error });
                return Task.CompletedTask;
            }

            /// <inheritdoc/>
            public Task CloseAsync(PartitionContext context, CloseReason reason) {
                _logger.Info("Partition closed", () => new { _processorId, context, reason });
                return Task.CompletedTask;
            }

            private readonly ILogger _logger;
            private readonly EventProcessorFactory _factory;
            private readonly string _processorId;
        }

        private readonly ILogger _logger;
        private readonly Dictionary<string, IEventHandler> _handlers;
    }
}
