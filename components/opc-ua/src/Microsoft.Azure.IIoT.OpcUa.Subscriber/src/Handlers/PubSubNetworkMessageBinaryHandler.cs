// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Subscriber.Handlers {
    using Microsoft.Azure.IIoT.OpcUa.Subscriber;
    using Microsoft.Azure.IIoT.OpcUa.Subscriber.Models;
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.Hub;
    using Opc.Ua;
    using Opc.Ua.PubSub;
    using Serilog;
    using System;
    using System.IO;
    using System.Collections.Generic;
    using System.Linq;
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
        /// <param name="encoder"></param>
        /// <param name="handlers"></param>
        /// <param name="logger"></param>
        public PubSubNetworkMessageBinaryHandler(IVariantEncoderFactory encoder,
            IEnumerable<ISubscriberMessageProcessor> handlers, ILogger logger) {
            _encoder = encoder ?? throw new ArgumentNullException(nameof(encoder));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _handlers = handlers?.ToList() ?? throw new ArgumentNullException(nameof(handlers));
        }

        /// <inheritdoc/>
        public async Task HandleAsync(string deviceId, string moduleId,
            byte[] payload, IDictionary<string, string> properties, Func<Task> checkpoint) {
            using (var stream = new MemoryStream(payload)) {
                var context = new ServiceMessageContext();
                try {
                    using (var decoder = new BinaryDecoder(stream, context)) {
                        var networkMessage = decoder.ReadEncodeable(null, typeof(NetworkMessage)) as NetworkMessage;
                        foreach (var dataSetMessage in networkMessage.Messages) {
                            var dataset = new DataSetMessageModel {
                                PublisherId = networkMessage.PublisherId,
                                MessageId = networkMessage.MessageId,
                                DataSetClassId = networkMessage.DataSetClassId,
                                DataSetWriterId = dataSetMessage.DataSetWriterId,
                                SequenceNumber = dataSetMessage.SequenceNumber,
                                Status = StatusCode.LookupSymbolicId(dataSetMessage.Status.Code),
                                MetaDataVersion = $"{dataSetMessage.MetaDataVersion.MajorVersion}"+
                                    $".{dataSetMessage.MetaDataVersion.MinorVersion}",
                                Timestamp = dataSetMessage.Timestamp,
                                Payload = new Dictionary<string, DataValueModel>()
                            };
                            foreach (var datapoint in dataSetMessage.Payload) {
                                var codec = _encoder.Create(context);
                                var type = BuiltInType.Null;
                                dataset.Payload[datapoint.Key] = new DataValueModel {
                                    Value = datapoint.Value == null
                                        ? null : codec.Encode(datapoint.Value.WrappedValue, out type),
                                    DataType = type == BuiltInType.Null
                                        ? null : type.ToString(),
                                    Status = (datapoint.Value?.StatusCode.Code == StatusCodes.Good)
                                        ? null : StatusCode.LookupSymbolicId(datapoint.Value.StatusCode.Code),
                                    SourceTimestamp = (datapoint.Value?.SourceTimestamp == DateTime.MinValue)
                                        ? null : datapoint.Value?.SourceTimestamp,
                                    SourcePicoseconds = (datapoint.Value?.SourcePicoseconds == 0)
                                        ? null : datapoint.Value?.SourcePicoseconds,
                                    ServerTimestamp = (datapoint.Value?.ServerTimestamp == DateTime.MinValue)
                                        ? null : datapoint.Value?.ServerTimestamp,
                                    ServerPicoseconds = (datapoint.Value?.ServerPicoseconds == 0)
                                        ? null : datapoint.Value?.ServerPicoseconds
                                };
                            }
                            await Task.WhenAll(_handlers.Select(h => h.HandleMessageAsync(dataset)));
                        }
                    }
                }
                catch (Exception ex) {
                    _logger.Error(ex, "Subscriber binary network message handling failed - skip");
                }
            }
        }

        /// <inheritdoc/>
        public Task OnBatchCompleteAsync() {
            return Task.CompletedTask;
        }

        private readonly IVariantEncoderFactory _encoder;
        private readonly ILogger _logger;
        private readonly List<ISubscriberMessageProcessor> _handlers;
    }
}
