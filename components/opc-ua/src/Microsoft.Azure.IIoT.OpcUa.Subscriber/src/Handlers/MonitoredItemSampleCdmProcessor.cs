// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Subscriber.Handlers {
    using Microsoft.Azure.IIoT.OpcUa.Subscriber.Models;
    using Microsoft.Azure.IIoT.Cdm;
    using Microsoft.Azure.IIoT.Cdm.Models;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Cdm processor for monitored item messages
    /// </summary>
    public sealed class MonitoredItemSampleCdmProcessor : IMonitoredItemSampleProcessor,
        IDisposable {

        /// <summary>
        /// Create forwarder
        /// </summary>
        /// <param name="client"></param>
        public MonitoredItemSampleCdmProcessor(ICdmClient client) {
            if (client == null) {
                throw new ArgumentNullException(nameof(client));
            }
            client.OpenAsync().Wait();
            _client = client;
        }

        /// <inheritdoc/>
        public Task HandleSampleAsync(MonitoredItemSampleModel sample) {
            var cdmModel = new SubscriberCdmSampleModel() {
                SubscriptionId = sample.SubscriptionId,
                EndpointId = sample.EndpointId,
                DataSetId = sample.DataSetId,
                NodeId = sample.NodeId,
                Value = sample.Value.ToString(),
                // Set timestamp as source timestamp - todo make configurable
                Timestamp = sample.SourceTimestamp,
                ServerTimestamp = sample.ServerTimestamp,
                SourceTimestamp = sample.SourceTimestamp
            };
            return _client.ProcessAsync(cdmModel);
        }

        /// <inheritdoc/>
        public void Dispose() {
            _client.Dispose();
        }

        private readonly ICdmClient _client;
    }
}
