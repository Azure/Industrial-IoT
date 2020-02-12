// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.Processor.Telemetry.Ux.Runtime {
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Hub.Processor;
    using Microsoft.Azure.IIoT.Hub.Processor.Runtime;
    using Microsoft.Azure.IIoT.Messaging.EventHub;
    using Microsoft.Azure.IIoT.Messaging.EventHub.Runtime;
    using Microsoft.Azure.IIoT.Messaging.SignalR;
    using Microsoft.Azure.IIoT.Messaging.SignalR.Runtime;
    using Microsoft.Extensions.Configuration;
    using System;

    /// <summary>
    /// Telemetry processor service configuration
    /// </summary>
    public class Config : DiagnosticsConfig, IEventProcessorConfig, 
        IEventHubConsumerConfig, ISignalRServiceConfig {

        private const string kEventHubConsumerGroupTelemetryUxKey = 
            "EventHubConsumerGroupTelemetryUx";

        /// <inheritdoc/>
        public string EventHubConnString => _eh.EventHubConnString;
        /// <inheritdoc/>
        public string EventHubPath => _eh.EventHubPath;
        /// <summary> Event hub consumer group telemetry ux</summary>
        public string ConsumerGroup => GetStringOrDefault(kEventHubConsumerGroupTelemetryUxKey,
            GetStringOrDefault(PcsVariable.PCS_EVENTHUB_CONSUMERGROUP_TELEMETRY_UX,
                "telemetryux"));
        /// <inheritdoc/>
        public bool UseWebsockets => _eh.UseWebsockets;
        /// <inheritdoc/>
        public int ReceiveBatchSize => _ep.ReceiveBatchSize;
        /// <inheritdoc/>
        public TimeSpan ReceiveTimeout => _ep.ReceiveTimeout;
        /// <inheritdoc/>
        public string BlobStorageConnString => _ep.BlobStorageConnString;
        /// <inheritdoc/>
        public string LeaseContainerName => _ep.LeaseContainerName;
        /// <inheritdoc/>
        public string SignalRHubName => _sr.SignalRHubName;
        /// <inheritdoc/>
        public string SignalRConnString => _sr.SignalRConnString;

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public Config(IConfiguration configuration) : base(configuration) {
            _ep = new EventProcessorConfig(configuration);
            _eh = new EventHubConsumerConfig(configuration);
            _sr = new SignalRServiceConfig(configuration);
        }

        private readonly EventProcessorConfig _ep;
        private readonly EventHubConsumerConfig _eh;
        private readonly SignalRServiceConfig _sr;
    }
}
