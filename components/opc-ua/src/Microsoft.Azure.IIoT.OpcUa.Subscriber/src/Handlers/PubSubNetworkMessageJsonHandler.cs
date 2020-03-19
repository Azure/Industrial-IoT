// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Subscriber.Handlers {
    using Microsoft.Azure.IIoT.OpcUa.Subscriber;
    using Microsoft.Azure.IIoT.OpcUa.Subscriber.Models;
    using Microsoft.Azure.IIoT.Hub;
    using Opc.Ua;
    using Opc.Ua.PubSub;
    using Opc.Ua.Encoders;
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
    public sealed class PubSubNetworkMessageJsonHandler : IDeviceTelemetryHandler {

        /// <inheritdoc/>
        public string MessageSchema => Core.MessageSchemaTypes.NetworkMessageJson;

        /// <summary>
        /// Create handler
        /// </summary>
        /// <param name="handlers"></param>
        /// <param name="logger"></param>
        public PubSubNetworkMessageJsonHandler(IEnumerable<ISubscriberMessageProcessor> handlers, ILogger logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _handlers = handlers?.ToList() ?? throw new ArgumentNullException(nameof(handlers));
        }

        /// <inheritdoc/>
        public async Task HandleAsync(string deviceId, string moduleId,
            byte[] payload, IDictionary<string, string> properties, Func<Task> checkpoint) {
            var json = Encoding.UTF8.GetString(payload);
            using (var stream = new MemoryStream(payload)) {
                var context = new ServiceMessageContext();
                try {
                    using (var decoder = new JsonDecoderEx(stream, context)) {
                        var networkMessage = decoder.ReadEncodeable(null, typeof(NetworkMessage)) as NetworkMessage;
                        foreach (var message in networkMessage.Messages) {
                            var dataset = new DataSetMessageModel {
                                PublisherId = networkMessage.PublisherId,
                                MessageId = networkMessage.MessageId,
                                DataSetClassId = networkMessage.DataSetClassId,
                                DataSetWriterId = message.DataSetWriterId,
                                SequenceNumber = message.SequenceNumber,
                                Status = StatusCode.LookupSymbolicId(message.Status.Code),
                                MetaDataVersion = $"{message.MetaDataVersion.MajorVersion}.{message.MetaDataVersion.MinorVersion}",
                                Timestamp = message.Timestamp,
                                Payload = new Dictionary<string, DataValueModel>()
                            };
                            foreach (var datapoint in message.Payload) {
                                dataset.Payload[datapoint.Key] = new DataValueModel() {
                                    Value = datapoint.Value?.Value,
                                    Status = StatusCode.LookupSymbolicId(datapoint.Value.StatusCode.Code),
                                    TypeId = (datapoint.Value?.WrappedValue.TypeInfo != null) ?
                                        TypeInfo.GetSystemType(
                                            datapoint.Value.WrappedValue.TypeInfo.BuiltInType,
                                            datapoint.Value.WrappedValue.TypeInfo.ValueRank) : null,
                                    Timestamp = datapoint.Value?.SourceTimestamp
                                };
                            }
                            await Task.WhenAll(_handlers.Select(h => h.HandleMessageAsync(dataset)));
                        }
                    }
                }
                catch (Exception ex) {
                    _logger.Error(ex, "Subscriber json network message handling failed - skip");
                }
            }
        }

        /// <inheritdoc/>
        public Task OnBatchCompleteAsync() {
            return Task.CompletedTask;
        }

        private readonly ILogger _logger;
        private readonly List<ISubscriberMessageProcessor> _handlers;
    }
}
