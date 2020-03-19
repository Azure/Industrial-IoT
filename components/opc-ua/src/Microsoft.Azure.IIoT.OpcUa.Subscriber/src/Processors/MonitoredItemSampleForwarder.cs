// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Subscriber.Processors {
    using Microsoft.Azure.IIoT.Messaging;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.OpcUa.Core;
    using Microsoft.Azure.IIoT.OpcUa.Subscriber.Models;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Text;
    using Newtonsoft.Json;

    /// <summary>
    /// Forwards samples to another event hub
    /// </summary>
    public sealed class MonitoredItemSampleForwarder : ISubscriberMessageProcessor,
        IDisposable {

        /// <summary>
        /// Create forwarder
        /// </summary>
        /// <param name="queue"></param>
        public MonitoredItemSampleForwarder(IEventQueueService queue) {
            if (queue == null) {
                throw new ArgumentNullException(nameof(queue));
            }
            _client = queue.OpenAsync().Result;
        }

        /// <inheritdoc/>
        public Task HandleSampleAsync(MonitoredItemSampleModel sample) {
            // Set timestamp as source timestamp
            // TODO: Make configurable
            sample.Timestamp = sample.SourceTimestamp;

            var properties = new Dictionary<string, string>() {
                [CommonProperties.EventSchemaType] = 
                    MessageSchemaTypes.MonitoredItemMessageModelJson
            };
            return _client.SendAsync(Encoding.UTF8.GetBytes(
                JsonConvertEx.SerializeObject(sample)),properties);
        }

        /// <inheritdoc/>
        public Task HandleMessageAsync(DataSetMessageModel message) {
            var properties = new Dictionary<string, string>() {
                [CommonProperties.EventSchemaType] = 
                    MessageSchemaTypes.NetworkMessageModelJson
            };
            return _client.SendAsync(Encoding.UTF8.GetBytes(
                JsonConvertEx.SerializeObject(message)), properties);
        }

        /// <inheritdoc/>
        public void Dispose() {
            _client.Dispose();
        }

        private readonly IEventQueueClient _client;
    }
}
