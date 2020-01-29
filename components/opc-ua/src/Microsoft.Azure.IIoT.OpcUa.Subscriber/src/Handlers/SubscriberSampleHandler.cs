// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Subscriber.Handlers {
    using Microsoft.Azure.IIoT.OpcUa.Subscriber.Models;
    using Microsoft.Azure.IIoT.Hub;
    using Newtonsoft.Json.Linq;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Publisher message handling
    /// </summary>
    public sealed class SubscriberCdmSampleHandler : IDeviceTelemetryHandler {

        /// <inheritdoc/>
        public string MessageSchema => Models.MessageSchemaTypes.LegacySubscriberSample;

        /// <summary>
        /// Create handler
        /// </summary>
        /// <param name="handlers"></param>
        /// <param name="logger"></param>
        public SubscriberCdmSampleHandler(IEnumerable<ISubscriberSampleProcessor> handlers, ILogger logger) {
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
                    var sample = message.ToSubscriberSampleModel();
                    if (sample == null) {
                        continue;
                    }
                    await Task.WhenAll(_handlers.Select(h => h.OnSubscriberSampleAsync(
                        sample)));
                }
                catch (Exception ex) {
                    _logger.Error(ex,
                        "Subscriber message {message} failed with exception - skip",
                            message);
                }
            }
        }

        /// <inheritdoc/>
        public Task OnBatchCompleteAsync() {
            return Task.CompletedTask;
        }

        private readonly ILogger _logger;
        private readonly List<ISubscriberSampleProcessor> _handlers;
    }
}
