// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Services
{
    using Autofac;
    using Azure.IIoT.OpcUa.Services.Handlers;

    /// <summary>
    /// Injected event handlers
    /// </summary>
    public sealed class EventHandlers : Module
    {
        /// <summary>
        /// Load the module
        /// </summary>
        /// <param name="builder"></param>
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<DeviceTelemetryEventHandler>()
                .AsImplementedInterfaces();

            builder.RegisterType<RegistryLifecycleHandler>()
                .AsImplementedInterfaces();

            builder.RegisterType<DiscoveryProgressHandler>()
                .AsImplementedInterfaces();
            builder.RegisterType<DiscoveryResultHandler>()
                .AsImplementedInterfaces();

            builder.RegisterType<MonitoredItemMessageHandler>()
                .AsImplementedInterfaces();
            builder.RegisterType<NetworkMessageJsonHandler>()
                .AsImplementedInterfaces();
            builder.RegisterType<NetworkMessageUadpHandler>()
                .AsImplementedInterfaces();

            base.Load(builder);
        }
    }
}
