// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.Processor.Telemetry.Runtime {
    using Microsoft.Azure.IIoT.OpcUa.Api.Onboarding;
    using Microsoft.Azure.IIoT.OpcUa.Api.Runtime;
    using Microsoft.Azure.IIoT.Hub.Processor;
    using Microsoft.Azure.IIoT.Hub.Processor.Runtime;
    using Microsoft.Azure.IIoT.Hub.Client.Runtime;
    using Microsoft.Azure.IIoT.Messaging.EventHub;
    using Microsoft.Azure.IIoT.Messaging.SignalR;
    using Microsoft.Azure.IIoT.Messaging.SignalR.Runtime;
    using Microsoft.Azure.IIoT.Cdm;
    using Microsoft.Azure.IIoT.Cdm.Runtime;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Extensions.Configuration;
    using System;

    /// <summary>
    /// Telemetry processor service configuration
    /// </summary>
    public class Config : DiagnosticsConfig, IEventProcessorConfig, IEventHubConsumerConfig,
        IOnboardingConfig, ISignalRServiceConfig, ICdmClientConfig {

        /// <inheritdoc/>
        public string OpcUaOnboardingServiceUrl => _ia.OpcUaOnboardingServiceUrl;
        /// <inheritdoc/>
        public string OpcUaOnboardingServiceResourceId => _ia.OpcUaOnboardingServiceResourceId;
        /// <inheritdoc/>
        public string EventHubConnString => _eh.EventHubConnString;
        /// <inheritdoc/>
        public string EventHubPath => _eh.EventHubPath;
        /// <inheritdoc/>
        public string ConsumerGroup => _eh.ConsumerGroup;
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

        /// <inheritdoc/>
        public string ADLSg2HostName => _cdm.ADLSg2HostName;
        /// <inheritdoc/>
        public string ADLSg2BlobName => _cdm.ADLSg2BlobName;
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
        /// <inheritdoc/>
        public string Audience => _cdm.Audience;

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public Config(IConfiguration configuration) : base(configuration) {
            _ep = new EventProcessorConfig(configuration);
            _eh = new IoTHubEventConfig(configuration);
            _ia = new InternalApiConfig(configuration);
            _sr = new SignalRServiceConfig(configuration);
            _cdm = new CdmClientConfig(configuration);
        }

        private readonly EventProcessorConfig _ep;
        private readonly IoTHubEventConfig _eh;
        private readonly InternalApiConfig _ia;
        private readonly SignalRServiceConfig _sr;
        private readonly CdmClientConfig _cdm;
    }
}
