// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Runtime {
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Engine;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.Azure.IIoT.Agent.Framework;
    using Microsoft.Azure.IIoT.Module.Framework.Client;
    using Autofac;
    using System;

    /// <summary>
    /// Container builder for data set writer jobs
    /// </summary>
    public class WriterGroupJobContainerFactory : IProcessingEngineContainerFactory {

        /// <summary>
        /// Create job scope factory
        /// </summary>
        /// <param name="jobConfig"></param>
        /// <param name="clientConfig"></param>
        public WriterGroupJobContainerFactory(WriterGroupJobModel jobConfig,
            IModuleConfig clientConfig) {
            _clientConfig = clientConfig ?? throw new ArgumentNullException(nameof(clientConfig));
            _jobConfig = jobConfig ?? throw new ArgumentNullException(nameof(jobConfig));
        }

        /// <inheritdoc/>
        public Action<ContainerBuilder> GetJobContainerScope(string agentId, string jobId) {
            return builder => {
                // Register job configuration
                builder.RegisterInstance(_jobConfig.ToWriterGroupJobConfiguration(jobId))
                    .AsImplementedInterfaces();

                // Register processing engine - trigger, transform, sink
                builder.RegisterType<DataFlowProcessingEngine>()
                    .AsImplementedInterfaces().InstancePerLifetimeScope();
                builder.RegisterType<WriterGroupMessageTrigger>()
                    .AsImplementedInterfaces().InstancePerLifetimeScope();
                switch (_jobConfig.MessagingMode) {
                    case MessagingMode.Samples:
                        builder.RegisterType<MonitoredItemMessageJsonEncoder>()
                            .AsImplementedInterfaces().InstancePerLifetimeScope();
                        break;
                    case MessagingMode.SamplesBinary:
                        builder.RegisterType<MonitoredItemMessageBinaryEncoder>()
                            .AsImplementedInterfaces().InstancePerLifetimeScope();
                        break;
                    case MessagingMode.PubSub:
                        builder.RegisterType<NetworkMessageEncoder>()
                            .AsImplementedInterfaces().InstancePerLifetimeScope();
                        break;
                    case MessagingMode.PubSubBinary:
                        throw new NotImplementedException("PubSub binary encoding not implemented");
                    default:
                        builder.RegisterType<MonitoredItemMessageJsonEncoder>()
                            .AsImplementedInterfaces().InstancePerLifetimeScope();
                        break;
                }
                builder.RegisterType<IoTHubMessageSink>()
                    .AsImplementedInterfaces().InstancePerLifetimeScope();
                builder.RegisterInstance(_clientConfig.Clone(_jobConfig.ConnectionString))
                    .AsImplementedInterfaces();
            };
        }

        private readonly IModuleConfig _clientConfig;
        private readonly WriterGroupJobModel _jobConfig;
    }
}