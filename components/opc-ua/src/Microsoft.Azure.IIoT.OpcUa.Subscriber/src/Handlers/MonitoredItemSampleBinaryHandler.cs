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
    using Opc.Ua.Extensions;
    using Opc.Ua.PubSub;
    using Serilog;
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Publisher message handling
    /// </summary>
    public sealed class MonitoredItemSampleBinaryHandler : IDeviceTelemetryHandler {

        /// <inheritdoc/>
        public string MessageSchema => Core.MessageSchemaTypes.MonitoredItemMessageBinary;

        /// <summary>
        /// Create handler
        /// </summary>
        /// <param name="encoder"></param>
        /// <param name="handlers"></param>
        /// <param name="logger"></param>
        public MonitoredItemSampleBinaryHandler(IVariantEncoderFactory encoder,
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
                    using (var decoder = new BinaryDecoder(stream, context)) {
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
                var codec = _encoder.Create(context);
                var sample = new MonitoredItemMessageModel() {
                    Value = codec.Encode(message.Value),
                    Status = StatusCode.LookupSymbolicId(message.Value.StatusCode.Code),
                    TypeId = (message?.Value?.WrappedValue.TypeInfo != null) ?
                        TypeInfo.GetSystemType(
                            message.Value.WrappedValue.TypeInfo.BuiltInType,
                            message.Value.WrappedValue.TypeInfo.ValueRank)?.FullName : null,
                    DataSetId = !string.IsNullOrEmpty(message.DisplayName) ?
                        message.DisplayName : message.NodeId.AsString(null),
                    Timestamp = message.Timestamp,
                    EndpointId = (message.ExtensionFields != null &&
                        message.ExtensionFields.TryGetValue("EndpointId", out var endpointId))
                            ? endpointId : message.ApplicationUri ?? message.SubscriptionId,
                    SubscriptionId = message.SubscriptionId ?? message.ApplicationUri,
                    NodeId = message.NodeId.AsString(null),
                    DisplayName = message.DisplayName,
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

        private readonly IVariantEncoderFactory _encoder;
        private readonly ILogger _logger;
        private readonly List<ISubscriberMessageProcessor> _handlers;
    }
}
