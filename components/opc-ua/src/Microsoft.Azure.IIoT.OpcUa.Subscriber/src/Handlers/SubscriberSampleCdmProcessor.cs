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
    /// Forwards samples to another event hub
    /// </summary>
    public sealed class SubscriberSampleCdmProcessor : ISubscriberSampleProcessor,
        IDisposable {

        /// <summary>
        /// Create the Cdm Processor
        /// </summary>
        /// <param name="client"></param>
        public SubscriberSampleCdmProcessor(ICdmClient client) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            client.OpenAsync().Wait();
        }

        /// <inheritdoc/>
        public Task OnSubscriberSampleAsync(SubscriberSampleModel sample) {
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
