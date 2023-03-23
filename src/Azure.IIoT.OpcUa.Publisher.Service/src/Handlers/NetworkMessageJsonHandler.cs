// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.Handlers
{
    using Azure.IIoT.OpcUa.Publisher.Service.Subscriber;
    using Azure.IIoT.OpcUa.Encoders;
    using Azure.IIoT.OpcUa.Encoders.PubSub;
    using Furly;
    using Microsoft.Extensions.Logging;
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Publisher message handling
    /// </summary>
    public sealed class NetworkMessageJsonHandler : IMessageHandler
    {
        /// <inheritdoc/>
        public string MessageSchema => MessageSchemaTypes.NetworkMessageJson;

        /// <summary>
        /// Create handler
        /// </summary>
        /// <param name="encoder"></param>
        /// <param name="handlers"></param>
        /// <param name="logger"></param>
        public NetworkMessageJsonHandler(IVariantEncoderFactory encoder,
            IEnumerable<ISubscriberMessageProcessor> handlers,
            ILogger<NetworkMessageJsonHandler> logger)
        {
            _encoder = encoder ?? throw new ArgumentNullException(nameof(encoder));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _handlers = handlers?.ToList() ?? throw new ArgumentNullException(nameof(handlers));
        }

        /// <inheritdoc/>
        public async ValueTask HandleAsync(string deviceId, string moduleId, ReadOnlyMemory<byte> payload,
            IReadOnlyDictionary<string, string> properties, CancellationToken ct)
        {
            try
            {
                var context = new ServiceMessageContext();
                var pubSubMessage = PubSubMessage.Decode(payload, ContentMimeType.Json, context, null, MessageSchema);
                if (pubSubMessage is not BaseNetworkMessage networkMessage)
                {
                    _logger.LogInformation("Received non network message.");
                    return;
                }
                if (pubSubMessage is JsonNetworkMessage message)
                {
                    foreach (JsonDataSetMessage dataSetMessage in message.Messages)
                    {
                        var dataset = new DataSetMessageModel
                        {
                            PublisherId = message.PublisherId,
                            MessageId = message.MessageId(),
                            DataSetClassId = message.DataSetClassId.ToString(),
                            DataSetWriterId = dataSetMessage.DataSetWriterName,
                            SequenceNumber = dataSetMessage.SequenceNumber,
                            Status = dataSetMessage.Status == null ? null :
                                StatusCode.LookupSymbolicId(dataSetMessage.Status.Value.Code),
                            MetaDataVersion = $"{dataSetMessage.MetaDataVersion.MajorVersion}" +
                                $".{dataSetMessage.MetaDataVersion.MinorVersion}",
                            Timestamp = dataSetMessage.Timestamp,
                            Payload = new Dictionary<string, DataValueModel>()
                        };
                        foreach (var datapoint in dataSetMessage.Payload)
                        {
                            var codec = _encoder.Create(context);
                            var type = BuiltInType.Null;
                            dataset.Payload[datapoint.Key] = new DataValueModel
                            {
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
                        await Task.WhenAll(_handlers.Select(h => h.HandleMessageAsync(dataset))).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Subscriber json network message handling failed - skip");
            }
        }

        private readonly IVariantEncoderFactory _encoder;
        private readonly ILogger _logger;
        private readonly List<ISubscriberMessageProcessor> _handlers;
    }
}
