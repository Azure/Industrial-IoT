// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Subscriber.Handlers {
    using Microsoft.Azure.IIoT.OpcUa.Subscriber;
    using Microsoft.Azure.IIoT.OpcUa.Subscriber.Models;
    using Microsoft.Azure.IIoT.Hub;
    using Newtonsoft.Json.Linq;
    using Serilog;
    using System;
    using System.Text;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Publisher message handling
    /// </summary>
    public sealed class MonitoredItemSampleModelHandler : IDeviceTelemetryHandler {

        /// <inheritdoc/>
        public string MessageSchema => Core.MessageSchemaTypes.MonitoredItemMessageModelJson;

        /// <summary>
        /// Create handler
        /// </summary>
        /// <param name="handlers"></param>
        /// <param name="logger"></param>
        public MonitoredItemSampleModelHandler(IEnumerable<IMonitoredItemSampleProcessor> handlers, ILogger logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _handlers = handlers?.ToList() ?? throw new ArgumentNullException(nameof(handlers));
        }

        /// <inheritdoc/>
        public async Task HandleAsync(string deviceId, string moduleId,
            byte[] payload, IDictionary<string, string> properties, Func<Task> checkpoint) {
            var json = Encoding.UTF8.GetString(payload);
            IEnumerable<JToken> messages;
            try {
                var parsed = JToken.Parse(json);
                if (parsed.Type == JTokenType.Array) {
                    messages = parsed as JArray;
                }
                else {
                    messages = parsed.YieldReturn();
                }
            }
            catch (Exception ex) {
                _logger.Error(ex, "Failed to parse json {json}", json);
                return;
            }
            foreach (var message in messages) {
                try {
                    var sample = message.ToServiceModel();
                    if (sample == null) {
                        continue;
                    }
                    await Task.WhenAll(_handlers.Select(h => h.HandleSampleAsync(sample)));
                }
                catch (Exception ex) {
                    _logger.Error(ex,
                        "Publishing message {message} failed with exception - skip",
                            message);
                }
            }
        }

        /// <inheritdoc/>
        public Task OnBatchCompleteAsync() {
            return Task.CompletedTask;
        }

        private readonly ILogger _logger;
        private readonly List<IMonitoredItemSampleProcessor> _handlers;
    }
}
