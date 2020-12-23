// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Default;
    using Autofac;

    /// <summary>
    /// Injected event broker stubs
    /// </summary>
    public sealed class EventBrokerStubs : Module {

        /// <summary>
        /// Load the module
        /// </summary>
        /// <param name="builder"></param>
        protected override void Load(ContainerBuilder builder) {

            builder.RegisterType<EventBrokerStubT<IEndpointRegistryListener>>()
                .AsImplementedInterfaces();
            builder.RegisterType<EventBrokerStubT<IApplicationRegistryListener>>()
                .AsImplementedInterfaces();
            builder.RegisterType<EventBrokerStubT<IGatewayRegistryListener>>()
                .AsImplementedInterfaces();
            builder.RegisterType<EventBrokerStubT<IPublisherRegistryListener>>()
                .AsImplementedInterfaces();
            builder.RegisterType<EventBrokerStubT<ISupervisorRegistryListener>>()
                .AsImplementedInterfaces();
            builder.RegisterType<EventBrokerStubT<IDiscovererRegistryListener>>()
                .AsImplementedInterfaces();
            builder.RegisterType<EventBrokerStubT<IDiscoveryProgressProcessor>>()
                .AsImplementedInterfaces();

            base.Load(builder);
        }
    }
}
