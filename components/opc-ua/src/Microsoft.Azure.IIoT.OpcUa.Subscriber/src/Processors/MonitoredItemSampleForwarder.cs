// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Subscriber.Processors {
    using Microsoft.Azure.IIoT.OpcUa.Subscriber.Models;
    using Microsoft.Azure.IIoT.Messaging;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Serializers;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Forwards samples to another event hub
    /// </summary>
    public sealed class MonitoredItemSampleForwarder : ISubscriberMessageProcessor,
        IDisposable {

        /// <summary>
        /// Create forwarder
        /// </summary>
        /// <param name="queue"></param>
        /// <param name="serializer"></param>
        public MonitoredItemSampleForwarder(IEventQueueService queue,
            IJsonSerializer serializer) {
            _serializer = serializer ??
                throw new ArgumentNullException(nameof(serializer));
            if (queue == null) {
                throw new ArgumentNullException(nameof(queue));
            }
            _client = queue.OpenAsync().Result;
        }

        /// <inheritdoc/>
        public Task HandleSampleAsync(MonitoredItemMessageModel sample) {
            // Set timestamp as source timestamp
            var properties = new Dictionary<string, string>() {
                [CommonProperties.EventSchemaType] =
                    Core.MessageSchemaTypes.MonitoredItemMessageModelJson
            };
            return _client.SendAsync(_serializer.SerializeToBytes(sample).ToArray(),
                properties, sample.DataSetWriterId);
        }

        /// <inheritdoc/>
        public Task HandleMessageAsync(DataSetMessageModel message) {
            var properties = new Dictionary<string, string>() {
                [CommonProperties.EventSchemaType] =
                    Core.MessageSchemaTypes.NetworkMessageModelJson
            };
            return _client.SendAsync(_serializer.SerializeToBytes(message).ToArray(),
                properties, message.DataSetWriterId);
        }

        /// <inheritdoc/>
        public void Dispose() {
            _client.Dispose();
        }

        private readonly IEventQueueClient _client;
        private readonly IJsonSerializer _serializer;
    }
}
