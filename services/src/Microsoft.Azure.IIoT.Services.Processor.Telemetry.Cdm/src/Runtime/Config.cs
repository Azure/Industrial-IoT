// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.Processor.Telemetry.Cdm.Runtime {
    using Microsoft.Azure.IIoT.OpcUa.Cdm;
    using Microsoft.Azure.IIoT.OpcUa.Cdm.Runtime;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Hub.Processor;
    using Microsoft.Azure.IIoT.Hub.Processor.Runtime;
    using Microsoft.Azure.IIoT.Messaging.EventHub;
    using Microsoft.Azure.IIoT.Messaging.EventHub.Runtime;
    using Microsoft.Extensions.Configuration;
    using System;

    /// <summary>
    /// Cdm processor service configuration
    /// </summary>
    public class Config : DiagnosticsConfig, IEventProcessorHostConfig,
        IEventHubConsumerConfig, ICdmFolderConfig, IEventProcessorConfig {

        /// <inheritdoc/>
        public string ConsumerGroup => GetStringOrDefault(
            PcsVariable.PCS_EVENTHUB_CONSUMERGROUP_TELEMETRY_CDM,
                () => "telemetrycdm");

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
        public string EndpointSuffix => _ep.EndpointSuffix;
        /// <inheritdoc/>
        public string AccountName => _ep.AccountName;
        /// <inheritdoc/>
        public string AccountKey => _ep.AccountKey;
        /// <inheritdoc/>
        public TimeSpan? SkipEventsOlderThan => _ep.SkipEventsOlderThan;
        /// <inheritdoc/>
        public string LeaseContainerName => _ep.LeaseContainerName;
        /// <inheritdoc/>
        public bool InitialReadFromEnd => _ep.InitialReadFromEnd;
        /// <inheritdoc/>
        public TimeSpan? CheckpointInterval => _ep.CheckpointInterval;

        /// <inheritdoc/>
        public string StorageDrive => _cdm.StorageDrive;
        /// <inheritdoc/>
        public string StorageFolder => _cdm.StorageFolder;

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public Config(IConfiguration configuration) : base(configuration) {
            _ep = new EventProcessorConfig(configuration);
            _eh = new EventHubConsumerConfig(configuration);
            _cdm = new CdmFolderConfig(configuration);
        }

        private readonly EventProcessorConfig _ep;
        private readonly EventHubConsumerConfig _eh;
        private readonly CdmFolderConfig _cdm;
    }
}
