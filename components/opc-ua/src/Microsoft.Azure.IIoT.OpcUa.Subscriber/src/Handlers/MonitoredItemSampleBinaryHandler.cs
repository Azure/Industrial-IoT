// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Subscriber.Handlers {
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.OpcUa.Subscriber;
    using Microsoft.Azure.IIoT.OpcUa.Subscriber.Models;
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
        /// <param name="handlers"></param>
        /// <param name="logger"></param>
        public MonitoredItemSampleBinaryHandler(IEnumerable<ISubscriberMessageProcessor> handlers, ILogger logger) {
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
                var sample = new MonitoredItemSampleModel() {
                    NodeId = message.NodeId.AsString(null),
                    DisplayName = message.DisplayName,
                    Value = (message?.Value?.WrappedValue.Value != null) ?
                        message.Value.WrappedValue.Value : null,
                    Status = StatusCode.LookupSymbolicId(message.Value.StatusCode.Code),
                    TypeId = (message?.Value?.WrappedValue.TypeInfo != null) ?
                        TypeInfo.GetSystemType(
                            message.Value.WrappedValue.TypeInfo.BuiltInType,
                            message.Value.WrappedValue.TypeInfo.ValueRank) : null,
                    SourceTimestamp = message.Value.SourceTimestamp,
                    ServerTimestamp = message.Value.ServerTimestamp,
                    Timestamp = message.Timestamp,
                    PublisherId = (message.ExtensionFields != null &&
                        message.ExtensionFields.TryGetValue("PublisherId", out var publisherId))
                            ? publisherId : message.ApplicationUri ?? message.EndpointUrl,
                    DataSetWriterId = (message.ExtensionFields != null &&
                        message.ExtensionFields.TryGetValue("DataSetWriterId", out var dataSetWriterId))
                            ? dataSetWriterId : message.EndpointUrl ?? message.ApplicationUri,
                    EndpointId = (message.ExtensionFields != null &&
                        message.ExtensionFields.TryGetValue("EndpointId", out var endpointId))
                            ? endpointId : message.EndpointUrl ?? message.ApplicationUri
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
        private readonly List<ISubscriberMessageProcessor> _handlers;
    }
}
