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
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<EndpointRegistry>()
                .AsImplementedInterfaces().SingleInstance();

            builder.RegisterType<ApplicationEventBroker>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<ApplicationRegistry>()
                .AsImplementedInterfaces().SingleInstance();

            builder.RegisterType<SupervisorEventBroker>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<SupervisorRegistry>()
                .AsImplementedInterfaces().SingleInstance();

            builder.RegisterType<PublisherEventBroker>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<PublisherRegistry>()
                .AsImplementedInterfaces().SingleInstance();

            builder.RegisterType<DiscovererEventBroker>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<DiscovererRegistry>()
                .AsImplementedInterfaces().SingleInstance();

            builder.RegisterType<GatewayEventBroker>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<GatewayRegistry>()
                .AsImplementedInterfaces().SingleInstance();

            base.Load(builder);
        }
    }
}
