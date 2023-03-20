// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service
{
    using Azure.IIoT.OpcUa.Publisher.Service.Services;
    using Autofac;

    /// <summary>
    /// Injected registry services
    /// </summary>
    public sealed class RegistryServices : Module
    {
        /// <summary>
        /// Load the module
        /// </summary>
        /// <param name="builder"></param>
        protected override void Load(ContainerBuilder builder)
        {
            // Services
            builder.RegisterType<ApplicationRegistry>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<EndpointManager>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<SupervisorRegistry>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<PublisherRegistry>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<DiscovererRegistry>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<GatewayRegistry>()
                .AsImplementedInterfaces().SingleInstance();

            base.Load(builder);
        }
    }
}
