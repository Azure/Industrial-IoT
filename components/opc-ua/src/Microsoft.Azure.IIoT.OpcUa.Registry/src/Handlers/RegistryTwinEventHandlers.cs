// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Handlers;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Default;
    using Microsoft.Azure.IIoT.Hub.Services;
    using Autofac;

    /// <summary>
    /// Injected registry event handlers
    /// </summary>
    public sealed class RegistryTwinEventHandlers : Module {

        /// <summary>
        /// Load the module
        /// </summary>
        /// <param name="builder"></param>
        protected override void Load(ContainerBuilder builder) {

            builder.RegisterType<IoTHubTwinChangeEventHandler>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<IoTHubDeviceLifecycleEventHandler>()
                .AsImplementedInterfaces().SingleInstance();

            builder.RegisterType<SupervisorTwinEventHandler>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<SupervisorEventBroker>()
                .AsImplementedInterfaces().SingleInstance();

            builder.RegisterType<PublisherTwinEventHandler>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<PublisherEventBroker>()
                .AsImplementedInterfaces().SingleInstance();

            builder.RegisterType<DiscovererTwinEventHandler>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<DiscovererEventBroker>()
                .AsImplementedInterfaces().SingleInstance();

            builder.RegisterType<GatewayTwinEventHandler>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<GatewayEventBroker>()
                .AsImplementedInterfaces().SingleInstance();

            builder.RegisterType<EndpointTwinEventHandler>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<EndpointEventBroker>()
                .AsImplementedInterfaces().SingleInstance();

            builder.RegisterType<ApplicationTwinEventHandler>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<ApplicationEventBroker>()
                .AsImplementedInterfaces().SingleInstance();

            base.Load(builder);
        }
    }
}
