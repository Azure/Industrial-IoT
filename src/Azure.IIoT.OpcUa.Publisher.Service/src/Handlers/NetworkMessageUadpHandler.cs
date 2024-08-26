// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.Handlers
{
    using Azure.IIoT.OpcUa.Publisher.Service.Subscriber;
    using Azure.IIoT.OpcUa.Encoders;
    using Azure.IIoT.OpcUa.Encoders.PubSub;
    using Microsoft.Extensions.Logging;
    using Opc.Ua;
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Publisher message handling
    /// </summary>
    public sealed class NetworkMessageUadpHandler : IMessageHandler
    {
        /// <inheritdoc/>
        public string MessageSchema => MessageSchemaTypes.NetworkMessageUadp;

        /// <summary>
        /// Create handler
        /// </summary>
        /// <param name="encoder"></param>
        /// <param name="handlers"></param>
        /// <param name="logger"></param>
        public NetworkMessageUadpHandler(IVariantEncoderFactory encoder,
            IEnumerable<ISubscriberMessageProcessor> handlers,
            ILogger<NetworkMessageUadpHandler> logger)
        {
            _encoder = encoder ?? throw new ArgumentNullException(nameof(encoder));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _handlers = handlers?.ToList() ?? throw new ArgumentNullException(nameof(handlers));
        }

        /// <inheritdoc/>
        public async ValueTask HandleAsync(string deviceId, string? moduleId, ReadOnlySequence<byte> payload,
            IReadOnlyDictionary<string, string?> properties, CancellationToken ct)
        {
            try
            {
                var context = new ServiceMessageContext();
                var pubSubMessage = PubSubMessage.Decode(payload, ContentType.Uadp, context, null, MessageSchema);
                if (pubSubMessage is not BaseNetworkMessage networkMessage)
                {
                    _logger.LogInformation("Received non network message.");
                    return;
                }
                if (pubSubMessage is UadpNetworkMessage message)
                {
                    foreach (UadpDataSetMessage dataSetMessage in message.Messages)
                    {
                        var dataset = new DataSetMessageModel
                        {
                            PublisherId = message.PublisherId,
                            MessageId = message.NetworkMessageNumber.ToString(CultureInfo.InvariantCulture),
                            DataSetClassId = message.DataSetClassId.ToString(),
                            DataSetWriterId = dataSetMessage.DataSetWriterId.ToString(CultureInfo.InvariantCulture),
                            SequenceNumber = dataSetMessage.SequenceNumber,
                            Status = dataSetMessage.Status.AsString(),
                            MetaDataVersion = $"{dataSetMessage.MetaDataVersion?.MajorVersion ?? 1}" +
                                $".{dataSetMessage.MetaDataVersion?.MinorVersion ?? 0}",
                            Timestamp = dataSetMessage.Timestamp,
                            Payload = new Dictionary<string, DataValueModel?>()
                        };
                        foreach (var datapoint in dataSetMessage.Payload)
                        {
                            var codec = _encoder.Create(context);
                            var type = BuiltInType.Null;
                            var dataValue = datapoint.Value;
                            dataset.Payload[datapoint.Key] = dataValue == null ? null : new DataValueModel
                            {
                                Value = codec.Encode(dataValue.WrappedValue, out type),
                                DataType = type == BuiltInType.Null
                                    ? null : type.ToString(),
                                Status = (dataValue.StatusCode.Code == StatusCodes.Good)
                                    ? null : dataValue.StatusCode.AsString(),
                                SourceTimestamp = (dataValue.SourceTimestamp == DateTime.MinValue)
                                    ? null : dataValue.SourceTimestamp,
                                SourcePicoseconds = (dataValue.SourcePicoseconds == 0)
                                    ? null : dataValue.SourcePicoseconds,
                                ServerTimestamp = (dataValue.ServerTimestamp == DateTime.MinValue)
                                    ? null : dataValue.ServerTimestamp,
                                ServerPicoseconds = (dataValue.ServerPicoseconds == 0)
                                    ? null : dataValue.ServerPicoseconds
                            };
                        }
                        await Task.WhenAll(_handlers.Select(h => h.HandleMessageAsync(dataset))).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Subscriber binary network message handling failed - skip");
            }
        }

        private readonly IVariantEncoderFactory _encoder;
        private readonly ILogger _logger;
        private readonly List<ISubscriberMessageProcessor> _handlers;
    }
}
