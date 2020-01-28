// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Subscriber.Handlers {
    using Microsoft.Azure.IIoT.OpcUa.Subscriber;
    using Microsoft.Azure.IIoT.OpcUa.Subscriber.Models;
    using Microsoft.Azure.IIoT.Hub;
    using Newtonsoft.Json.Linq;
    using Opc.Ua;
    using Opc.Ua.PubSub;
    using Serilog;
    using System;
    using System.IO;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Publisher message handling
    /// </summary>
    public sealed class PubSubNetworkMessageBinaryHandler : IDeviceTelemetryHandler {

        /// <inheritdoc/>
        public string MessageSchema => Core.MessageSchemaTypes.NetworkMessageUadp;

        /// <summary>
        /// Create handler
        /// </summary>
        /// <param name="handlers"></param>
        /// <param name="logger"></param>
        public PubSubNetworkMessageBinaryHandler(IEnumerable<IMonitoredItemSampleProcessor> handlers, ILogger logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _handlers = handlers?.ToList() ?? throw new ArgumentNullException(nameof(handlers));
        }

        /// <inheritdoc/>
        public async Task HandleAsync(string deviceId, string moduleId,
            byte[] payload, IDictionary<string, string> properties, Func<Task> checkpoint) {
            var json = Encoding.UTF8.GetString(payload);
            using (var stream = new MemoryStream(payload)) {
                var context = new ServiceMessageContext();
                using (var decoder = new BinaryDecoder(stream, context)) {
                    var networkMessage = decoder.ReadEncodeable(null, typeof(NetworkMessage)) as NetworkMessage;
                    foreach (var message in networkMessage.Messages) {
                        foreach (var datapoint in message.Payload) {
                            try {
                                var sample = new MonitoredItemSampleModel() {
                                    Value = new JObject {
                                        { "Body", datapoint.Value.WrappedValue.Value.ToString() },
                                        { "Type", datapoint.Value.WrappedValue.Value.GetType().ToString() }
                                    },
                                    Status = StatusCode.LookupSymbolicId(datapoint.Value.StatusCode.Code),
                                    TypeId = message.TypeId.ToString(),
                                    DataSetId = message.DataSetWriterId,
                                    Timestamp = DateTime.UtcNow,
                                    SubscriptionId = "network message",
                                    EndpointId = networkMessage.PublisherId,
                                    NodeId = datapoint.Key,
                                    SourcePicoseconds = datapoint.Value.SourcePicoseconds,
                                    ServerPicoseconds = datapoint.Value.ServerPicoseconds,
                                    SourceTimestamp = datapoint.Value.SourceTimestamp,
                                    ServerTimestamp = datapoint.Value.ServerTimestamp
                                };
                                if (sample == null) {
                                    continue;
                                }
                                await Task.WhenAll(_handlers.Select(h => h.HandleSampleAsync(sample)));
                            }
                            catch (Exception ex) {
                                _logger.Error(ex,
                                    "Subscriber message {message} failed with exception - skip",
                                        message);
                            }
                        }
                    }
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
