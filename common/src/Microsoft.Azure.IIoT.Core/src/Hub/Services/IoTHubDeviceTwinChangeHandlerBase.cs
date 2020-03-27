// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Services {
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Hub.Models;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Azure.IIoT.Serializers;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Linq;

    /// <summary>
    /// Registry Twin change events
    /// </summary>
    public abstract class IoTHubDeviceTwinChangeHandlerBase : IDeviceTelemetryHandler {

        /// <inheritdoc/>
        public abstract string MessageSchema { get; }

        /// <summary>
        /// Create handler
        /// </summary>
        /// <param name="serializer"></param>
        /// <param name="handlers"></param>
        /// <param name="logger"></param>
        public IoTHubDeviceTwinChangeHandlerBase(IJsonSerializer serializer,
            IEnumerable<IIoTHubDeviceTwinEventHandler> handlers, ILogger logger) {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _handlers = handlers.ToList();
        }

        /// <inheritdoc/>
        public async Task HandleAsync(string deviceId, string moduleId,
            byte[] payload, IDictionary<string, string> properties,
            Func<Task> checkpoint) {

            if (_handlers.Count == 0) {
                return;
            }
            if (!properties.TryGetValue("opType", out var opType) ||
                !properties.TryGetValue("operationTimestamp", out var ts)) {
                return;
            }
            DateTime.TryParse(ts, out var timestamp);
            if (timestamp + TimeSpan.FromSeconds(10) < DateTime.UtcNow) {
                // Drop twin events that are too far in our past.
                _logger.Debug("Skipping {event} from {deviceId}({moduleId}) from {ts}.",
                    opType, deviceId, moduleId, timestamp);
                return;
            }

            var twin = Try.Op(() => _serializer.Deserialize<DeviceTwinModel>(payload));
            if (twin == null) {
                return;
            }
            twin.ModuleId = moduleId;
            twin.Id = deviceId;
            var operation = GetOperation(opType);
            if (operation == null) {
                return;
            }
            var ev = new DeviceTwinEvent {
                Twin = twin,
                Event = operation.Value,
                IsPatch = true,
                Handled = false,
                AuthorityId = null, // TODO
                Timestamp = timestamp
            };
            foreach (var handler in _handlers) {
                await handler.HandleDeviceTwinEventAsync(ev);
            }
        }

        /// <inheritdoc/>
        public Task OnBatchCompleteAsync() {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Get operation
        /// </summary>
        /// <param name="opType"></param>
        /// <returns></returns>
        protected abstract DeviceTwinEventType? GetOperation(string opType);

        private readonly IJsonSerializer _serializer;
        private readonly ILogger _logger;
        private readonly List<IIoTHubDeviceTwinEventHandler> _handlers;
    }
}
