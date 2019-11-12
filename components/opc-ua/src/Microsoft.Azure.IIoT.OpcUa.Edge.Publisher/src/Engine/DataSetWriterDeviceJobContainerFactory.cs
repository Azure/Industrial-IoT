// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Engine {
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Triggering;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.Azure.IIoT.Agent.Framework;
    using Microsoft.Azure.IIoT.Module.Framework.Client;
    using Autofac;
    using System;

    /// <summary>
    /// Container builder for data set writer jobs
    /// </summary>
    public class DataSetWriterDeviceJobContainerFactory : IProcessingEngineContainerFactory {

        /// <summary>
        /// Create job scope factory
        /// </summary>
        /// <param name="jobConfig"></param>
        /// <param name="clientConfig"></param>
        public DataSetWriterDeviceJobContainerFactory(PubSubJobModel jobConfig,
            IModuleConfig clientConfig) {
            _clientConfig = clientConfig ?? throw new ArgumentNullException(nameof(clientConfig));
            _jobConfig = jobConfig ?? throw new ArgumentNullException(nameof(jobConfig));
        }

        /// <inheritdoc/>
        public Action<ContainerBuilder> GetScopedRegistrations() {
            return builder => {
                builder.RegisterType<PubSubMessageTrigger>().AsImplementedInterfaces()
                    .InstancePerLifetimeScope();
                builder.RegisterInstance(_jobConfig.Job.ToMessageTriggerConfig())
                    .AsImplementedInterfaces();
                builder.RegisterInstance(_jobConfig.Job.ToEncodingConfig())
                    .AsImplementedInterfaces();
                builder.RegisterInstance(_jobConfig.Job.ToEngineConfig())
                    .AsImplementedInterfaces();
                builder.RegisterInstance(_clientConfig.Clone(_jobConfig.ConnectionString))
                    .AsImplementedInterfaces();
            };
        }

        private readonly IModuleConfig _clientConfig;
        private readonly PubSubJobModel _jobConfig;
    }
}