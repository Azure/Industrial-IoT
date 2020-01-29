// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Subscriber.Handlers {
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.OpcUa.Subscriber;
    using Microsoft.Azure.IIoT.OpcUa.Subscriber.Models;
    using Newtonsoft.Json.Linq;
    using Opc.Ua;
    using Opc.Ua.Extensions;
    using Opc.Ua.PubSub;
    using Opc.Ua.Encoders;
    using Serilog;
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.Linq;


    /// <summary>
    /// Publisher message handling
    /// </summary>
    public sealed class MonitoredItemSampleJsonHandler : IDeviceTelemetryHandler {

        /// <inheritdoc/>
        public string MessageSchema => Core.MessageSchemaTypes.MonitoredItemMessageJson;

        /// <summary>
        /// Create handler
        /// </summary>
        /// <param name="handlers"></param>
        /// <param name="logger"></param>
        public MonitoredItemSampleJsonHandler(IEnumerable<IMonitoredItemSampleProcessor> handlers, ILogger logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _handlers = handlers?.ToList() ?? throw new ArgumentNullException(nameof(handlers));
        }

        /// <inheritdoc/>
        public async Task HandleAsync(string deviceId, string moduleId,
            byte[] payload, IDictionary<string, string> properties, Func<Task> checkpoint) {
            
            MonitoredItemMessage message;
            try {
                var context = new ServiceMessageContext();
                using (var stream = new MemoryStream(payload)) {
                    using (var decoder = new JsonDecoderEx(stream, context)) {
                        var result = decoder.ReadEncodeable(null, typeof(MonitoredItemMessage)) as MonitoredItemMessage;
                        message = result;
                    }
                }
            }
            catch (Exception ex) {
                _logger.Error(ex, "Failed to decode message");
                return;
            }
            try {
                var sample = new MonitoredItemSampleModel() {
                    Value = new JObject {
                            { "Body", message.Value.WrappedValue.Value.ToString() },
                            { "Type", message.Value.WrappedValue.Value.GetType().ToString() }
                        },
                    Status = StatusCode.LookupSymbolicId(message.Value.StatusCode.Code),
                    TypeId = message.TypeId.ToString(),
                    DataSetId = message.DisplayName,
                    Timestamp = DateTime.UtcNow,
                    SubscriptionId = message.SubscriptionId,
                    EndpointId = message?.ExtensionFields["EndpointId"],
                    NodeId = message.NodeId.AsString(null),
                    SourcePicoseconds = message.Value.SourcePicoseconds,
                    ServerPicoseconds = message.Value.ServerPicoseconds,
                    SourceTimestamp = message.Value.SourceTimestamp,
                    ServerTimestamp = message.Value.ServerTimestamp
                };
                await Task.WhenAll(_handlers.Select(h => h.HandleSampleAsync(sample)));
            }
            catch (Exception ex) {
                _logger.Error(ex,
                    "Publishing message {message} failed with exception - skip",
                        message);
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
