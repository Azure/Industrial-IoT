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
    public sealed class MonitoredItemMessagePublisher : ISubscriberMessageProcessor,
        IDisposable {

        /// <summary>
        /// Create publisher
        /// </summary>
        /// <param name="callback"></param>
        public MonitoredItemMessagePublisher(ICallbackInvoker callback) {
            _callback = callback ?? throw new ArgumentNullException(nameof(callback));
        }

        /// <inheritdoc/>
        public async Task HandleSampleAsync(MonitoredItemSampleModel sample) {
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
                     new MonitoredItemMessageApiModel() {
                        Value = datapoint.Value.TypeId?.IsPrimitive == true ?
                            datapoint.Value.Value : datapoint.Value.Value?.ToString(),
                        TypeId = datapoint.Value.TypeId?.FullName,
                        Status = datapoint.Value.Status,
                        DataSetId = datapoint.Key,
                        Timestamp = DateTime.UtcNow,
                        SubscriptionId = message.DataSetWriterId,
                        EndpointId = message.PublisherId,
                        NodeId = datapoint.Key,
                        // TODO check how we can transport the Display name
                        DisplayName = datapoint.Key,
                        SourcePicoseconds = 0,
                        ServerPicoseconds = 0,
                        SourceTimestamp = datapoint.Value.Timestamp,
                        ServerTimestamp = datapoint.Value.Timestamp
                    }
                };
                if (!string.IsNullOrEmpty(message.PublisherId)) {
                    // Send to endpoint listeners
                    await _callback.MulticastAsync(message.PublisherId,
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
