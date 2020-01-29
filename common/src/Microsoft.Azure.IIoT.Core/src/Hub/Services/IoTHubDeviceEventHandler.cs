// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Services {
    using Microsoft.Azure.IIoT.Utils;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Default iot hub device event handler implementation
    /// </summary>
    public sealed class IoTHubDeviceEventHandler : IEventHandler {

        /// <summary>
        /// Create processor factory
        /// </summary>
        /// <param name="handlers"></param>
        /// <param name="unknown"></param>
        public IoTHubDeviceEventHandler(IEnumerable<IDeviceTelemetryHandler> handlers,
            IUnknownEventHandler unknown = null) {
            if (handlers == null) {
                throw new ArgumentNullException(nameof(handlers));
            }
            _handlers = handlers.ToDictionary(h => h.MessageSchema.ToLowerInvariant(), h => h);
            _unknown = unknown;
        }

        /// <inheritdoc/>
        public async Task HandleAsync(byte[] eventData, IDictionary<string, string> properties,
            Func<Task> checkpoint) {
            if (!properties.TryGetValue(CommonProperties.DeviceId, out var deviceId) &&
                !properties.TryGetValue(SystemProperties.ConnectionDeviceId, out deviceId)) {
                // Not from a device
                return;
            }

            if (properties.TryGetValue(CommonProperties.EventSchemaType, out var schemaType) ||
                properties.TryGetValue(SystemProperties.MessageSchema, out schemaType)) {

                properties.TryGetValue(CommonProperties.ModuleId, out var moduleId);
                if (_handlers.TryGetValue(schemaType.ToLowerInvariant(), out var handler)) {
                    await handler.HandleAsync(deviceId, moduleId?.ToString(), eventData,
                        properties, checkpoint);
                    _used.Add(handler);
                }

                // Handled...
                return;
            }

            if (_unknown != null) {
                // From a device, but does not have any event schema or message schema
                await _unknown.HandleAsync(eventData, properties);
                return;
            }
        }

        /// <inheritdoc/>
        public async Task OnBatchCompleteAsync() {
            foreach (var handler in _used.ToList()) {
                await Try.Async(handler.OnBatchCompleteAsync);
            }
            _used.Clear();
        }

        private readonly HashSet<IDeviceTelemetryHandler> _used =
            new HashSet<IDeviceTelemetryHandler>();
        private readonly Dictionary<string, IDeviceTelemetryHandler> _handlers;
        private readonly IUnknownEventHandler _unknown;
    }
}
