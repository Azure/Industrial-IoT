// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.Processor.Telemetry.Cdm.Runtime {
    using Microsoft.Azure.IIoT.Cdm;
    using Microsoft.Azure.IIoT.Cdm.Runtime;
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
    public class Config : DiagnosticsConfig, IEventProcessorConfig,
        IEventHubConsumerConfig, ICdmClientConfig, IEventProcessorHostConfig {

        /// <inheritdoc/>
        public string EventHubConnString => _eh.EventHubConnString;
        /// <inheritdoc/>
        public string EventHubPath => _eh.EventHubPath;
        /// <inheritdoc/>
        public string ConsumerGroup => GetStringOrDefault(
            PcsVariable.PCS_EVENTHUB_CONSUMERGROUP_TELEMETRY_CDM,
                () => "telemetrycdm");
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
        public bool InitialReadFromEnd => _ep.InitialReadFromEnd;
        /// <inheritdoc/>
        public TimeSpan? CheckpointInterval => _ep.CheckpointInterval;

        /// <inheritdoc/>
        public string ADLSg2HostName => _cdm.ADLSg2HostName;
        /// <inheritdoc/>
        public string ADLSg2ContainerName => _cdm.ADLSg2ContainerName;
        /// <inheritdoc/>
        public string RootFolder => _cdm.RootFolder;
        /// <inheritdoc/>
        public string TenantId => _cdm.TenantId;
        /// <inheritdoc/>
        public string Domain => _cdm.Domain;
        /// <inheritdoc/>
        public string AppId => _cdm.AppId;
        /// <inheritdoc/>
        public string AppSecret => _cdm.AppSecret;
        /// <inheritdoc/>
        public string InstanceUrl => _cdm.InstanceUrl;

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public Config(IConfiguration configuration) : base(configuration) {
            _ep = new EventProcessorConfig(configuration);
            _eh = new EventHubConsumerConfig(configuration);
            _cdm = new CdmClientConfig(configuration);
        }

        private readonly EventProcessorConfig _ep;
        private readonly EventHubConsumerConfig _eh;
        private readonly CdmClientConfig _cdm;
    }
}
