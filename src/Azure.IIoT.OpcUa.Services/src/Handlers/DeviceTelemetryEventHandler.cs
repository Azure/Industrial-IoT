// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Services.Handlers {
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Messaging;
    using Microsoft.Azure.IIoT.Utils;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Default iot hub device event handler implementation
    /// </summary>
    public sealed class DeviceTelemetryEventHandler : IEventProcessingHandler {

        /// <summary>
        /// Create processor factory
        /// </summary>
        /// <param name="handlers"></param>
        public DeviceTelemetryEventHandler(IEnumerable<IDeviceTelemetryHandler> handlers) {
            if (handlers == null) {
                throw new ArgumentNullException(nameof(handlers));
            }
            _handlers = new ConcurrentDictionary<string, IDeviceTelemetryHandler>(
                handlers.Select(h => KeyValuePair.Create(h.MessageSchema.ToLowerInvariant(), h)));
        }

        /// <inheritdoc/>
        public async Task HandleAsync(byte[] eventData, IDictionary<string, string> properties,
            Func<Task> checkpoint) {
            if (!properties.TryGetValue(CommonProperties.DeviceId, out var deviceId) &&
                !properties.TryGetValue(SystemProperties.ConnectionDeviceId, out deviceId) &&
                !properties.TryGetValue(SystemProperties.DeviceId, out deviceId)) {
                // Not from a device
                return;
            }

            if (!properties.TryGetValue(CommonProperties.ModuleId, out var moduleId) &&
                !properties.TryGetValue(SystemProperties.ConnectionModuleId, out moduleId) &&
                !properties.TryGetValue(SystemProperties.ModuleId, out moduleId)) {
                // Not from a module
                moduleId = null;
            }

            if (properties.TryGetValue(CommonProperties.EventSchemaType, out var schemaType) ||
                properties.TryGetValue(SystemProperties.MessageSchema, out schemaType)) {

                //  TODO: when handling third party OPC UA PubSub Messages
                //  the schemaType might not exist
                if (_handlers.TryGetValue(schemaType.ToLowerInvariant(), out var handler)) {
                    await handler.HandleAsync(deviceId, moduleId?.ToString(), eventData,
                        properties, checkpoint);
                    _used.Enqueue(handler);
                }

                // Handled...
                return;
            }
        }

        /// <inheritdoc/>
        public async Task OnBatchCompleteAsync() {
            while (_used.TryDequeue(out var handler)) {
                await Try.Async(handler.OnBatchCompleteAsync);
            }
        }

        private readonly ConcurrentQueue<IDeviceTelemetryHandler> _used =
            new();
        private readonly ConcurrentDictionary<string, IDeviceTelemetryHandler> _handlers;
    }
}
