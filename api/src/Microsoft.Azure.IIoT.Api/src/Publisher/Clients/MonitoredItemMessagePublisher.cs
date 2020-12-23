// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Clients {
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Subscriber;
    using Microsoft.Azure.IIoT.OpcUa.Subscriber.Models;
    using Microsoft.Azure.IIoT.Messaging;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Monitored item sample message progress  publishing
    /// </summary>
    public sealed class MonitoredItemMessagePublisher<THub> : ISubscriberMessageProcessor,
        IDisposable {

        /// <summary>
        /// Create publisher
        /// </summary>
        /// <param name="callback"></param>
        public MonitoredItemMessagePublisher(ICallbackInvokerT<THub> callback) {
            _callback = callback ?? throw new ArgumentNullException(nameof(callback));
        }

        /// <inheritdoc/>
        public async Task HandleSampleAsync(MonitoredItemMessageModel sample) {
            var arguments = new object[] { sample.ToApiModel() };
            if (!string.IsNullOrEmpty(sample.EndpointId)) {
                // Send to endpoint listeners
                await _callback.MulticastAsync(sample.EndpointId,
                    EventTargets.PublisherSampleTarget, arguments);
            }
        }
        /// <inheritdoc/>
        public async Task HandleMessageAsync(DataSetMessageModel message) {
            foreach (var datapoint in message.Payload) {
                var arguments = new object[] {
                     new MonitoredItemMessageApiModel {
                        Timestamp = message.Timestamp,
                        DataSetWriterId = message.DataSetWriterId,
                        PublisherId = message.PublisherId,
                        NodeId = datapoint.Key,
                        DisplayName = datapoint.Key,
                        Value = datapoint.Value?.Value?.Copy(),
                        Status = datapoint.Value?.Status,
                        SourceTimestamp = datapoint.Value?.SourceTimestamp,
                        SourcePicoseconds = datapoint.Value?.SourcePicoseconds,
                        ServerTimestamp = datapoint.Value?.ServerTimestamp,
                        ServerPicoseconds = datapoint.Value?.ServerPicoseconds,
                        DataType = datapoint.Value?.DataType,
                        SequenceNumber = message.SequenceNumber,
                        EndpointId = null // TODO Remove
                    }
                };
                if (!string.IsNullOrEmpty(message.DataSetWriterId)) {
                    // Send to endpoint listeners
                    await _callback.MulticastAsync(message.DataSetWriterId,
                        EventTargets.PublisherSampleTarget, arguments);
                }
            }
        }

        /// <inheritdoc/>
        public void Dispose() {
            _callback.Dispose();
        }

        private readonly ICallbackInvoker _callback;
    }
}
