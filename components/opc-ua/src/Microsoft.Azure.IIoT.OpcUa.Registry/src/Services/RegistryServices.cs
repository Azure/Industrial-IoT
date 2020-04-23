// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Services;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Default;
    using Autofac;

    /// <summary>
    /// Injected registry services
    /// </summary>
    public sealed class RegistryServices : Module {

        /// <summary>
        /// Load the module
        /// </summary>
        /// <param name="builder"></param>
        protected override void Load(ContainerBuilder builder) {

            // Services
            builder.RegisterType<EndpointEventBroker>()
                .AsImplementedInterfaces();
            builder.RegisterType<EndpointRegistry>()
                .AsImplementedInterfaces();

            builder.RegisterType<ApplicationEventBroker>()
                .AsImplementedInterfaces();
            builder.RegisterType<ApplicationRegistry>()
                .AsImplementedInterfaces();

            builder.RegisterType<SupervisorEventBroker>()
                .AsImplementedInterfaces();
            builder.RegisterType<SupervisorRegistry>()
                .AsImplementedInterfaces();

            builder.RegisterType<PublisherEventBroker>()
                .AsImplementedInterfaces();
            builder.RegisterType<PublisherRegistry>()
                .AsImplementedInterfaces();

            builder.RegisterType<DiscovererEventBroker>()
                .AsImplementedInterfaces();
            builder.RegisterType<DiscovererRegistry>()
                .AsImplementedInterfaces();

            builder.RegisterType<GatewayEventBroker>()
                .AsImplementedInterfaces();
            builder.RegisterType<GatewayRegistry>()
                .AsImplementedInterfaces();

            base.Load(builder);
        }
    }
}
