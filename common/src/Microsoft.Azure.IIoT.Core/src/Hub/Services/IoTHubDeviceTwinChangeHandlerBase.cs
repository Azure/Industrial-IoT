// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Services {
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Hub.Models;
    using Microsoft.Azure.IIoT.Utils;
    using Newtonsoft.Json;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Text;
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
        /// <param name="handlers"></param>
        /// <param name="logger"></param>
        public IoTHubDeviceTwinChangeHandlerBase(
            IEnumerable<IIoTHubDeviceTwinEventHandler> handlers, ILogger logger) {
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

            var twin = Try.Op(() => JsonConvertEx.DeserializeObject<DeviceTwinModel>(
                Encoding.UTF8.GetString(payload)));
            if (twin == null) {
                return;
            }

            twin.ModuleId = moduleId;
            twin.Id = deviceId;
            var operation = GetOperation(opType);
            if (operation == null) {
                return;
            }

            DateTime.TryParse(ts, out var time);
            var ev = new DeviceTwinEvent {
                Twin = twin,
                Event = operation.Value,
                IsPatch = true,
                Handled = false,
                AuthorityId = null, // TODO
                Timestamp = time
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

        private readonly ILogger _logger;
        private readonly List<IIoTHubDeviceTwinEventHandler> _handlers;
    }
}
