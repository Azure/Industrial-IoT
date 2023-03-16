// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher
{
    using Azure.IIoT.OpcUa.Publisher.Discovery;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Services;
    using Azure.IIoT.OpcUa.Publisher.Stack.Runtime;
    using Azure.IIoT.OpcUa.Publisher.Stack.Services;
    using Azure.IIoT.OpcUa.Publisher.Storage;
    using Azure.IIoT.OpcUa.Encoders;
    using Autofac;

    /// <summary>
    /// Container builder extensions
    /// </summary>
    public static class ContainerBuilderEx
    {
        /// <summary>
        /// Configure services
        /// </summary>
        /// <param name="builder"></param>
        public static void AddPublisherCore(this ContainerBuilder builder)
        {
            builder.RegisterType<PublisherIdentity>()
                .AsImplementedInterfaces();
            builder.RegisterType<PublisherConfig>()
                .AsImplementedInterfaces();

            builder.RegisterType<PublishedNodesProvider>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<PublishedNodesConverter>()
                .SingleInstance();
            builder.RegisterType<PublisherConfigurationService>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<PublisherHostService>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<PublisherDiagnosticCollector>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<RuntimeStateReporter>()
                .AsImplementedInterfaces().SingleInstance();

            builder.RegisterType<WriterGroupScopeFactory>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<NodeServices<ConnectionModel>>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<HistoryServices<ConnectionModel>>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<ServerDiscovery>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<NetworkDiscovery>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<ProgressPublisher>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();

            builder.RegisterType<OpcUaClientManager>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<ClientConfig>()
                .AsImplementedInterfaces();
            builder.RegisterType<SubscriptionConfig>()
                .AsImplementedInterfaces();
            builder.RegisterType<VariantEncoderFactory>()
                .AsImplementedInterfaces();
        }
    }
}
