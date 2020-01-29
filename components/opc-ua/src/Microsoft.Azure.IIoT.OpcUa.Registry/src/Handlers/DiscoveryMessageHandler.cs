// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Handlers {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.Hub;
    using Newtonsoft.Json;
    using Serilog;
    using System;
    using System.Text;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Discovery message handling
    /// </summary>
    public sealed class DiscoveryMessageHandler : IDeviceTelemetryHandler {

        /// <inheritdoc/>
        public string MessageSchema => Models.MessageSchemaTypes.DiscoveryMessage;

        /// <summary>
        /// Create handler
        /// </summary>
        /// <param name="handlers"></param>
        /// <param name="logger"></param>
        public DiscoveryMessageHandler(IEnumerable<IDiscoveryProgressProcessor> handlers, ILogger logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _handlers = handlers?.ToList() ?? throw new ArgumentNullException(nameof(handlers));
        }

        /// <inheritdoc/>
        public async Task HandleAsync(string deviceId, string moduleId,
            byte[] payload, IDictionary<string, string> properties, Func<Task> checkpoint) {
            var json = Encoding.UTF8.GetString(payload);
            DiscoveryProgressModel discovery;
            try {
                discovery = JsonConvertEx.DeserializeObject<DiscoveryProgressModel>(json);
            }
            catch (Exception ex) {
                _logger.Error(ex, "Failed to convert discovery message {json}", json);
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

        private readonly ILogger _logger;
        private readonly List<IDiscoveryProgressProcessor> _handlers;
    }
}
