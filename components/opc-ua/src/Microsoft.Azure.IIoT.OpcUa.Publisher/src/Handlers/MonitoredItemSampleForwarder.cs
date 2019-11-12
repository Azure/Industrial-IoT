// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Handlers {
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Publisher;
    using Microsoft.Azure.IIoT.Messaging;
    using System;
    using System.Threading.Tasks;
    using System.Text;
    using Newtonsoft.Json;

    /// <summary>
    /// Forwards samples to another event hub
    /// </summary>
    public sealed class MonitoredItemSampleForwarder : IMonitoredItemMessageProcessor,
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
        public Task HandleMessageAsync(MonitoredItemMessageModel sample) {
            sample = sample.Clone();
            // Set timestamp as source timestamp
            // TODO: Make configurablew
            sample.Timestamp = sample.SourceTimestamp;
            return _client.SendAsync(Encoding.UTF8.GetBytes(
                JsonConvertEx.SerializeObject(sample)));
        }

        /// <inheritdoc/>
        public void Dispose() {
            _client.Dispose();
        }

        private readonly IEventQueueClient _client;
    }
}
