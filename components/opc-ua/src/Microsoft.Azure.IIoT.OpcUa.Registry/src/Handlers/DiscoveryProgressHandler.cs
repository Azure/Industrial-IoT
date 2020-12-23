// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Handlers {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Serializers;
    using Serilog;
    using System;
    using System.Text;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Discovery message handling
    /// </summary>
    public sealed class DiscoveryProgressHandler : IDeviceTelemetryHandler {

        /// <inheritdoc/>
        public string MessageSchema => Models.MessageSchemaTypes.DiscoveryMessage;

        /// <summary>
        /// Create handler
        /// </summary>
        /// <param name="handlers"></param>
        /// <param name="serializer"></param>
        /// <param name="logger"></param>
        public DiscoveryProgressHandler(IEnumerable<IDiscoveryProgressProcessor> handlers,
            IJsonSerializer serializer, ILogger logger) {
            _serializer = serializer ??
                throw new ArgumentNullException(nameof(serializer));
            _logger = logger ??
                throw new ArgumentNullException(nameof(logger));
            _handlers = handlers?.ToList() ??
                throw new ArgumentNullException(nameof(handlers));
        }

        /// <inheritdoc/>
        public async Task HandleAsync(string deviceId, string moduleId,
            byte[] payload, IDictionary<string, string> properties, Func<Task> checkpoint) {
            DiscoveryProgressModel discovery;
            try {
                discovery = _serializer.Deserialize<DiscoveryProgressModel>(payload);
            }
            catch (Exception ex) {
                _logger.Error(ex, "Failed to convert discovery message {json}",
                    Encoding.UTF8.GetString(payload));
                return;
            }
            try {
                await Task.WhenAll(_handlers.Select(h => h.OnDiscoveryProgressAsync(discovery)));
            }
            catch (Exception ex) {
                _logger.Error(ex,
                    "Publishing discovery message failed with exception - skip");
            }
        }

        /// <inheritdoc/>
        public Task OnBatchCompleteAsync() {
            return Task.CompletedTask;
        }

        private readonly IJsonSerializer _serializer;
        private readonly ILogger _logger;
        private readonly List<IDiscoveryProgressProcessor> _handlers;
    }
}
