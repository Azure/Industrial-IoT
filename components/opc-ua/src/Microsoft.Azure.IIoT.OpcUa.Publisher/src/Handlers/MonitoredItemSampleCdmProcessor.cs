// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Handlers {
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Publisher;
    using Microsoft.Azure.IIoT.Cdm;
    using Microsoft.Azure.IIoT.Cdm.Models;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Forwards samples to another event hub
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
            sample = sample.Clone();
            // Set timestamp as source timestamp
            // TODO: Make configurable
            sample.Timestamp = sample.SourceTimestamp;
            var cdmModel = new SubscriberCdmSampleModel() {
                SubscriptionId = sample.SubscriptionId,
                EndpointId = sample.EndpointId,
                DataSetId = sample.DataSetId,
                NodeId = sample.NodeId,
                Value = sample.Value.ToString(),
                Timestamp = sample.Timestamp,
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
