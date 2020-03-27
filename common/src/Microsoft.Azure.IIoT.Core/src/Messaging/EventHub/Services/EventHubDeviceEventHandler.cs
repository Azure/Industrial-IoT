// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Core.Messaging.EventHub {
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Messaging;
    using Microsoft.Azure.IIoT.Utils;
    using System;
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
        /// <param name="unknown"></param>
        public EventHubDeviceEventHandler(IEnumerable<IDeviceTelemetryHandler> handlers,
            IUnknownEventProcessor unknown = null) {
            if (handlers == null) {
                throw new ArgumentNullException(nameof(handlers));
            }
            _handlers = handlers.ToDictionary(h => h.MessageSchema.ToLowerInvariant(), h => h);
            _unknown = unknown;
        }

        /// <inheritdoc/>
        public async Task HandleAsync(byte[] eventData, IDictionary<string, string> properties,
            Func<Task> checkpoint) {

            var handled = false;
            if (properties.TryGetValue(CommonProperties.EventSchemaType, out var schemaType)) {

                properties.TryGetValue(CommonProperties.DeviceId, out var deviceId);
                properties.TryGetValue(CommonProperties.ModuleId, out var moduleId);

                if (_handlers.TryGetValue(schemaType, out var handler)) {
                    _used.Add(handler.MessageSchema);
                    await handler.HandleAsync(deviceId, moduleId, eventData, properties, checkpoint);
                    handled = true;
                }
            }

            if (!handled && _unknown != null) {
                // From a device, but does not have any event schema or message schema
                await _unknown.HandleAsync(eventData, properties);
                if (checkpoint != null) {
                    await Try.Async(() => checkpoint());
                }
            }
        }

        /// <inheritdoc/>
        public async Task OnBatchCompleteAsync() {
            foreach (var handler in _used) {
                await Try.Async(_handlers[handler]!.OnBatchCompleteAsync);
            }
            _used.Clear();
        }

        private readonly HashSet<string> _used =
            new HashSet<string>();
        private readonly Dictionary<string, IDeviceTelemetryHandler> _handlers;
        private readonly IUnknownEventProcessor _unknown;
    }
}
