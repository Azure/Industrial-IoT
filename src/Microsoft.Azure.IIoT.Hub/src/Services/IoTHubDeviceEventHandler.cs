// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Services {
    using Microsoft.Azure.IIoT.Hub;
    using Serilog;
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
        /// <param name="logger"></param>
        public IoTHubDeviceEventHandler(IEnumerable<IDeviceEventHandler> handlers,
            ILogger logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            if (handlers == null) {
                throw new ArgumentNullException(nameof(handlers));
            }
            _handlers = handlers.ToDictionary(h => h.ContentType, h => h);
        }

        /// <inheritdoc/>
        public async Task HandleAsync(byte[] eventData, IDictionary<string, string> properties,
            Func<Task> checkpoint) {
            if (!properties.TryGetValue(CommonProperties.kDeviceId, out var deviceId) &&
                !properties.TryGetValue(SystemProperties.ConnectionDeviceId, out deviceId)) {
                // Not our content to process
                return;
            }
            if (!properties.TryGetValue(CommonProperties.kContentType, out var contentType) &&
                !properties.TryGetValue(EventProperties.kContentType, out contentType) &&
                !properties.TryGetValue(SystemProperties.ContentType, out contentType)) {
                // Not our content to process
                return;
            }
            properties.TryGetValue(CommonProperties.kModuleId, out var moduleId);
            if (_handlers.TryGetValue(contentType.ToLowerInvariant(), out var handler)) {
                await handler.HandleAsync(deviceId, moduleId?.ToString(), eventData, checkpoint);
                _used.Add(handler);
            }
        }

        /// <inheritdoc/>
        public async Task OnBatchCompleteAsync() {
            foreach (var handler in _used) {
                await Try.Async(handler.OnBatchCompleteAsync);
            }
            _used.Clear();
        }

        private readonly HashSet<IDeviceEventHandler> _used =
            new HashSet<IDeviceEventHandler>();
        private readonly ILogger _logger;
        private readonly Dictionary<string, IDeviceEventHandler> _handlers;
    }
}
