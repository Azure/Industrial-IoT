// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Processor.Handlers {
    using Microsoft.Azure.IIoT.Cdm;
    using Microsoft.Azure.IIoT.Processor;
    using Microsoft.Azure.IIoT.Processor.Models;
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
            return _client.ProcessAsync(sample);
        }

        /// <inheritdoc/>
        public void Dispose() {
            _client.Dispose();
        }

        private readonly ICdmClient _client;
    }
}
