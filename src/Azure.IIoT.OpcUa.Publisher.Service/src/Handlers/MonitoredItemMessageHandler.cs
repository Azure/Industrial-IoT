// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.Handlers
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Encoders;
    using Azure.IIoT.OpcUa.Encoders.PubSub;
    using Furly;
    using Microsoft.Extensions.Logging;
    using Opc.Ua;
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Publisher message handling
    /// </summary>
    public sealed class MonitoredItemMessageHandler : IMessageHandler
    {
        /// <inheritdoc/>
        public string MessageSchema => MessageSchemaTypes.MonitoredItemMessageJson;

        /// <summary>
        /// Create handler
        /// </summary>
        /// <param name="encoder"></param>
        /// <param name="handlers"></param>
        /// <param name="logger"></param>
        public MonitoredItemMessageHandler(IVariantEncoderFactory encoder,
            IEnumerable<ISubscriberMessageProcessor> handlers,
            ILogger<MonitoredItemMessageHandler> logger)
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
                var pubSubMessage = PubSubMessage.Decode(payload, ContentMimeType.Json,
                    context, null, MessageSchema);
                if (pubSubMessage is not BaseNetworkMessage networkMessage)
                {
                    _logger.LogInformation("Received non network message.");
                    return;
                }

                foreach (MonitoredItemMessage message in networkMessage.Messages)
                {
                    var type = BuiltInType.Null;
                    var codec = _encoder.Create(context);
                    var extensionFields = message.ExtensionFields?.ToDictionary(k => k.DataSetFieldName, v => v.Value);
                    var sample = new MonitoredItemMessageModel
                    {
                        PublisherId = (extensionFields != null &&
                            extensionFields.TryGetValue("PublisherId", out var publisherId))
                                ? (string?)publisherId : message.ApplicationUri ?? message.EndpointUrl,
                        DataSetWriterId = (extensionFields != null &&
                            extensionFields.TryGetValue("DataSetWriterId", out var dataSetWriterId))
                                ? (string?)dataSetWriterId : message.EndpointUrl ?? message.ApplicationUri,
                        EndpointId = message.WriterGroupId,
                        NodeId = message.NodeId,
                        DisplayName = message.DisplayName,
                        Timestamp = message.Timestamp,
                        SequenceNumber = message.SequenceNumber,
                        Value = message.Value == null
                            ? null : codec.Encode(message.Value.WrappedValue, out type),
                        DataType = type == BuiltInType.Null
                            ? null : type.ToString(),
                        Status = (message.Value?.StatusCode.Code == StatusCodes.Good)
                            ? null : (message.Value?.StatusCode).AsString(),
                        SourceTimestamp = (message.Value?.SourceTimestamp == DateTime.MinValue)
                            ? null : message.Value?.SourceTimestamp,
                        SourcePicoseconds = (message.Value?.SourcePicoseconds == 0)
                            ? null : message.Value?.SourcePicoseconds,
                        ServerTimestamp = (message.Value?.ServerTimestamp == DateTime.MinValue)
                            ? null : message.Value?.ServerTimestamp,
                        ServerPicoseconds = (message.Value?.ServerPicoseconds == 0)
                            ? null : message.Value?.ServerPicoseconds
                    };
                    await Task.WhenAll(_handlers.Select(h => h.HandleSampleAsync(sample))).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Publishing messages failed - skip");
            }
        }

        private readonly IVariantEncoderFactory _encoder;
        private readonly ILogger _logger;
        private readonly List<ISubscriberMessageProcessor> _handlers;
    }
}
