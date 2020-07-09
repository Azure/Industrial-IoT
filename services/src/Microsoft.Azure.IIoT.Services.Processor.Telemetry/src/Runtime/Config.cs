// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.Processor.Telemetry.Runtime {
    using Microsoft.Azure.IIoT.AspNetCore.Diagnostics;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Hub.Processor;
    using Microsoft.Azure.IIoT.Hub.Processor.Runtime;
    using Microsoft.Azure.IIoT.Hub.Client.Runtime;
    using Microsoft.Azure.IIoT.Messaging.EventHub;
    using Microsoft.Extensions.Configuration;
    using System;

    /// <summary>
    /// Telemetry processor service configuration
    /// </summary>
    public class Config : DiagnosticsConfig, IEventProcessorHostConfig,
        IEventHubConsumerConfig, IEventProcessorConfig, IMetricServerConfig {

        /// <inheritdoc/>
        public string ConsumerGroup => GetStringOrDefault(
            PcsVariable.PCS_IOTHUB_EVENTHUB_CONSUMER_GROUP_TELEMETRY,
                () => "telemetry");

        /// <inheritdoc/>
        public string EventHubConnString => _eh.EventHubConnString;
        /// <inheritdoc/>
        public string EventHubPath => _eh.EventHubPath;
        /// <inheritdoc/>
        public bool UseWebsockets => _eh.UseWebsockets;

        /// <inheritdoc/>
        public int ReceiveBatchSize => _ep.ReceiveBatchSize;
        /// <inheritdoc/>
        public TimeSpan ReceiveTimeout => _ep.ReceiveTimeout;
        /// <inheritdoc/>
        public string LeaseContainerName => _ep.LeaseContainerName;
        /// <inheritdoc/>
        public bool InitialReadFromEnd => _ep.InitialReadFromEnd;
        /// <inheritdoc/>
        public TimeSpan? CheckpointInterval => _ep.CheckpointInterval;
        /// <inheritdoc/>
        public TimeSpan? SkipEventsOlderThan => _ep.SkipEventsOlderThan;
        /// <inheritdoc/>
        public string EndpointSuffix => _ep.EndpointSuffix;
        /// <inheritdoc/>
        public string AccountName => _ep.AccountName;
        /// <inheritdoc/>
        public string AccountKey => _ep.AccountKey;

        /// <inheritdoc/>
        public int Port => 9502;

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public Config(IConfiguration configuration) : base(configuration) {
            _ep = new EventProcessorConfig(configuration);
            _eh = new IoTHubEventConfig(configuration);
        }

        private readonly EventProcessorConfig _ep;
        private readonly IoTHubEventConfig _eh;
    }
}
