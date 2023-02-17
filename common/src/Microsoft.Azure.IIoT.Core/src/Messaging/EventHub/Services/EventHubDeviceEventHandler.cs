// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Core.Messaging.EventHub {
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Messaging;
    using Microsoft.Azure.IIoT.Utils;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Default Event Hub message handler implementation
    /// </summary>
    public sealed class EventHubDeviceEventHandler : IEventProcessingHandler {

        /// <summary>
        /// Create processor factory
        /// </summary>
        /// <param name="handlers"></param>
        public EventHubDeviceEventHandler(IEnumerable<IDeviceTelemetryHandler> handlers) {
            if (handlers == null) {
                throw new ArgumentNullException(nameof(handlers));
            }
            _handlers = new ConcurrentDictionary<string, IDeviceTelemetryHandler>(
                handlers.Select(h => KeyValuePair.Create(h.MessageSchema.ToLowerInvariant(), h)));
        }

        /// <inheritdoc/>
        public async Task HandleAsync(byte[] eventData, IDictionary<string, string> properties,
            Func<Task> checkpoint) {

            if (properties.TryGetValue(CommonProperties.EventSchemaType, out var schemaType)) {

                properties.TryGetValue(CommonProperties.DeviceId, out var deviceId);
                properties.TryGetValue(CommonProperties.ModuleId, out var moduleId);

                if (_handlers.TryGetValue(schemaType, out var handler)) {
                    _used.Enqueue(handler);
                    await handler.HandleAsync(deviceId, moduleId, eventData, properties, checkpoint);
                }
            }
        }

        /// <inheritdoc/>
        public async Task OnBatchCompleteAsync() {
            while (_used.TryDequeue(out var handler)) {
                await Try.Async(handler.OnBatchCompleteAsync);
            }
        }

        private readonly ConcurrentQueue<IDeviceTelemetryHandler> _used =
            new ConcurrentQueue<IDeviceTelemetryHandler>();
        private readonly ConcurrentDictionary<string, IDeviceTelemetryHandler> _handlers;
    }
}
