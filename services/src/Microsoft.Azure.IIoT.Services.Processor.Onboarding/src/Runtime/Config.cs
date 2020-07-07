// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.Processor.Onboarding.Runtime {
    using Microsoft.Azure.IIoT.AspNetCore.Diagnostics;
    using Microsoft.Azure.IIoT.Hub.Client;
    using Microsoft.Azure.IIoT.Hub.Client.Runtime;
    using Microsoft.Azure.IIoT.Messaging.ServiceBus;
    using Microsoft.Azure.IIoT.Messaging.ServiceBus.Runtime;
    using Microsoft.Azure.IIoT.Messaging.EventHub;
    using Microsoft.Azure.IIoT.Storage.CosmosDb;
    using Microsoft.Azure.IIoT.Storage.CosmosDb.Runtime;
    using Microsoft.Azure.IIoT.Storage;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Hub.Processor;
    using Microsoft.Azure.IIoT.Hub.Processor.Runtime;
    using Microsoft.Extensions.Configuration;
    using System;

    /// <summary>
    /// Common web service configuration aggregation
    /// </summary>
    public class Config : DiagnosticsConfig, IEventProcessorHostConfig,
        IEventHubConsumerConfig, IServiceBusConfig, IIoTHubConfig,
        IEventProcessorConfig, ICosmosDbConfig, IItemContainerConfig,
        IMetricServerConfig {

        /// <inheritdoc/>
        public string ConsumerGroup => GetStringOrDefault(
            PcsVariable.PCS_IOTHUB_EVENTHUB_CONSUMER_GROUP_ONBOARDING,
                () => "onboarding");

        /// <inheritdoc/>
        public string IoTHubConnString => _hub.IoTHubConnString;

        /// <inheritdoc/>
        public string ServiceBusConnString => _sb.ServiceBusConnString;

        /// <inheritdoc/>
        public string DbConnectionString => _cosmos.DbConnectionString;
        /// <inheritdoc/>
        public int? ThroughputUnits => _cosmos.ThroughputUnits;
        /// <inheritdoc/>
        public string ContainerName => "iiot_opc";
        /// <inheritdoc/>
        public string DatabaseName => "iiot_opc";

        /// <inheritdoc/>
        public TimeSpan? CheckpointInterval => _ep.CheckpointInterval;
        /// <inheritdoc/>
        public TimeSpan? SkipEventsOlderThan => TimeSpan.FromMinutes(5);
        /// <inheritdoc/>
        public string EndpointSuffix => _ep.EndpointSuffix;
        /// <inheritdoc/>
        public string AccountName => _ep.AccountName;
        /// <inheritdoc/>
        public string AccountKey => _ep.AccountKey;
        /// <inheritdoc/>
        public int ReceiveBatchSize => _ep.ReceiveBatchSize;
        /// <inheritdoc/>
        public TimeSpan ReceiveTimeout => _ep.ReceiveTimeout;
        /// <inheritdoc/>
        public bool InitialReadFromEnd => _ep.InitialReadFromEnd;
        /// <inheritdoc/>
        public string LeaseContainerName => _ep.LeaseContainerName;

        /// <inheritdoc/>
        public bool UseWebsockets => _eh.UseWebsockets;
        /// <inheritdoc/>
        public string EventHubConnString => _eh.EventHubConnString;
        /// <inheritdoc/>
        public string EventHubPath => _eh.EventHubPath;

        /// <inheritdoc/>
        public int Port => 9501;

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public Config(IConfiguration configuration) :
            base(configuration) {

            _hub = new IoTHubConfig(configuration);
            _sb = new ServiceBusConfig(configuration);
            _ep = new EventProcessorConfig(configuration);
            _eh = new IoTHubEventConfig(configuration);
            _cosmos = new CosmosDbConfig(configuration);
        }

        private readonly EventProcessorConfig _ep;
        private readonly IoTHubEventConfig _eh;
        private readonly ServiceBusConfig _sb;
        private readonly CosmosDbConfig _cosmos;
        private readonly IoTHubConfig _hub;
    }
}
