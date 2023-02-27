// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Services.WebApi.Runtime
{
    using Microsoft.Azure.IIoT;
    using Microsoft.Azure.IIoT.AspNetCore.Auth;
    using Microsoft.Azure.IIoT.Hub.Client;
    using Microsoft.Azure.IIoT.Hub.Client.Runtime;
    using Microsoft.Azure.IIoT.Hub.Processor;
    using Microsoft.Azure.IIoT.Hub.Processor.Runtime;
    using Microsoft.Azure.IIoT.Messaging.EventHub;
    using Microsoft.Azure.IIoT.Messaging.EventHub.Runtime;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;
    using System;

    /// <summary>
    /// Common web service configuration aggregation
    /// </summary>
    public class Config : ConfigBase, IIoTHubConfig,
        IEventProcessorConfig, IEventHubConsumerConfig,
        IEventProcessorHostConfig, IRoleConfig
    {
        /// <inheritdoc/>
        public string EventHubConnString => _eh.EventHubConnString;
        /// <inheritdoc/>
        public string EventHubPath => _eh.EventHubPath;
        /// <inheritdoc/>
        public string ConsumerGroup => GetStringOrDefault(
            PcsVariable.PCS_EVENTHUB_CONSUMERGROUP_TELEMETRY_UX,
                () => "$default");

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
        public string LeaseContainerName => _ep.LeaseContainerName;
        /// <inheritdoc/>
        public bool InitialReadFromEnd => true;
        /// <inheritdoc/>
        public TimeSpan? SkipEventsOlderThan => TimeSpan.FromMinutes(5);
        /// <inheritdoc/>
        public TimeSpan? CheckpointInterval => TimeSpan.FromMinutes(1);

        /// <inheritdoc/>
        public string IoTHubConnString => _hub.IoTHubConnString;

        /// <inheritdoc/>
        public bool UseRoles => GetBoolOrDefault(PcsVariable.PCS_AUTH_ROLES);

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public Config(IConfiguration configuration) :
            base(configuration)
        {
            _ep = new EventProcessorConfig(configuration);
            _eh = new EventHubConsumerConfig(configuration);
            _hub = new IoTHubConfig(configuration);
        }

        private readonly EventProcessorConfig _ep;
        private readonly EventHubConsumerConfig _eh;
        private readonly IoTHubConfig _hub;
    }
}
