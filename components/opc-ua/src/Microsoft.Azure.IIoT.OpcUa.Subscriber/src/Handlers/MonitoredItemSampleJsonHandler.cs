// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Subscriber.Handlers {
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.OpcUa.Subscriber;
    using Microsoft.Azure.IIoT.OpcUa.Subscriber.Models;
    using Opc.Ua;
    using Opc.Ua.Extensions;
    using Opc.Ua.PubSub;
    using Opc.Ua.Encoders;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Publisher message handling
    /// </summary>
    public sealed class MonitoredItemSampleJsonHandler : IDeviceTelemetryHandler {

        /// <inheritdoc/>
        public string MessageSchema => Core.MessageSchemaTypes.MonitoredItemMessageJson;

        /// <summary>
        /// Create handler
        /// </summary>
        /// <param name="encoder"></param>
        /// <param name="handlers"></param>
        /// <param name="logger"></param>
        public MonitoredItemSampleJsonHandler(IVariantEncoderFactory encoder,
            IEnumerable<ISubscriberMessageProcessor> handlers, ILogger logger) {
            _encoder = encoder ?? throw new ArgumentNullException(nameof(encoder));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _handlers = handlers?.ToList() ?? throw new ArgumentNullException(nameof(handlers));
        }

        /// <inheritdoc/>
        public async Task HandleAsync(string deviceId, string moduleId,
            byte[] payload, IDictionary<string, string> properties, Func<Task> checkpoint) {
            MonitoredItemMessage message;
            var context = new ServiceMessageContext();
            try {
                using (var stream = new MemoryStream(payload)) {
                    using (var decoder = new JsonDecoderEx(stream, context)) {
                        message = decoder.ReadEncodeable(null,
                            typeof(MonitoredItemMessage)) as MonitoredItemMessage;
                    }
                }
            }
            catch (Exception ex) {
                _logger.Error(ex, "Failed to decode message");
                return;
            }
            try {
                var type = BuiltInType.Null;
                var codec = _encoder.Create(context);
                var dataset = new DataSetMessageModel {
                    PublisherId = (message.ExtensionFields != null &&
                        message.ExtensionFields.TryGetValue("PublisherId", out var publisherId))
                            ? publisherId : message.ApplicationUri ?? message.EndpointUrl,
                    MessageId = null,
                    DataSetClassId = message.NodeId.AsString(null),
                    DataSetWriterId = (message.ExtensionFields != null &&
                        message.ExtensionFields.TryGetValue("DataSetWriterId", out var dataSetWriterId))
                            ? dataSetWriterId : message.EndpointUrl ?? message.ApplicationUri,
                    SequenceNumber = 0,
                    Status = StatusCode.LookupSymbolicId(message.Value.StatusCode.Code),
                    MetaDataVersion = "1.0",
                    Timestamp = message.Timestamp,
                    Payload = new Dictionary<string, DataValueModel>() {
                        [message.NodeId.AsString(context)] = new DataValueModel {
                            Value = message?.Value == null
                                ? null : codec.Encode(message.Value.WrappedValue, out type),
                            DataType = type == BuiltInType.Null
                                ? null : type.ToString(),
                            Status = (message?.Value?.StatusCode.Code == StatusCodes.Good)
                                ? null : StatusCode.LookupSymbolicId(message.Value.StatusCode.Code),
                            SourceTimestamp = (message?.Value?.SourceTimestamp == DateTime.MinValue)
                                ? null : message?.Value?.SourceTimestamp,
                            SourcePicoseconds = (message?.Value?.SourcePicoseconds == 0)
                                ? null : message?.Value?.SourcePicoseconds,
                            ServerTimestamp = (message?.Value?.ServerTimestamp == DateTime.MinValue)
                                ? null : message?.Value?.ServerTimestamp,
                            ServerPicoseconds = (message?.Value?.ServerPicoseconds == 0)
                                ? null : message?.Value?.ServerPicoseconds
                        }
                    }
                };
                await Task.WhenAll(_handlers.Select(h => h.HandleMessageAsync(dataset)));
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

        private readonly IVariantEncoderFactory _encoder;
        private readonly ILogger _logger;
        private readonly List<ISubscriberMessageProcessor> _handlers;
    }
}
