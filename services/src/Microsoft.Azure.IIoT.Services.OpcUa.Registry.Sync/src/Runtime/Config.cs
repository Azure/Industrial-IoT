// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Registry.Sync.Runtime {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Runtime;
    using Microsoft.Azure.IIoT.OpcUa.Registry;
    using Microsoft.Azure.IIoT.Messaging.ServiceBus;
    using Microsoft.Azure.IIoT.Messaging.ServiceBus.Runtime;
    using Microsoft.Azure.IIoT.Hub.Client;
    using Microsoft.Azure.IIoT.Hub.Client.Runtime;
    using Microsoft.Azure.IIoT.Agent.Framework.Jobs;
    using Microsoft.Azure.IIoT.Agent.Framework.Jobs.Runtime;
    using Microsoft.Azure.IIoT.AspNetCore.Diagnostics;
    using Microsoft.Azure.IIoT.Auth;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Extensions.Configuration;
    using System;

    /// <summary>
    /// Alerting agent configuration
    /// </summary>
    public class Config : DiagnosticsConfig, IIoTHubConfig, IServiceBusConfig,
        IIdentityTokenUpdaterConfig, IActivationSyncConfig, IJobOrchestratorEndpointConfig,
        IMetricServerConfig {

        /// <inheritdoc/>
        public string IoTHubConnString => _hub.IoTHubConnString;
        /// <inheritdoc/>
        public string ServiceBusConnString => _sb.ServiceBusConnString;
        /// <inheritdoc/>
        public TimeSpan SyncInterval => _sync.SyncInterval;
        /// <inheritdoc/>
        public string JobOrchestratorUrl => _edge.JobOrchestratorUrl;
        /// <inheritdoc/>
        public TimeSpan JobOrchestratorUrlSyncInterval => _edge.JobOrchestratorUrlSyncInterval;
        /// <inheritdoc/>
        public int TokenLength => _id.TokenLength;
        /// <inheritdoc/>
        public TimeSpan TokenLifetime => _id.TokenLifetime;
        /// <inheritdoc/>
        public TimeSpan TokenStaleInterval => _id.TokenStaleInterval;
        /// <inheritdoc/>
        public TimeSpan UpdateInterval => _id.UpdateInterval;
        /// <inheritdoc/>
        public int Port => 9505;

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public Config(IConfiguration configuration) :
            base(configuration) {

            _sb = new ServiceBusConfig(configuration);
            _hub = new IoTHubConfig(configuration);
            _id = new IdentityTokenUpdaterConfig(configuration);
            _sync = new ActivationSyncConfig(configuration);
            _edge = new JobOrchestratorEndpointConfig(configuration);
        }

        private readonly IServiceBusConfig _sb;
        private readonly IIoTHubConfig _hub;
        private readonly IdentityTokenUpdaterConfig _id;
        private readonly JobOrchestratorEndpointConfig _edge;
        private readonly ActivationSyncConfig _sync;
    }
}
