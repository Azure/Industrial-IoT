// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module
{
    using Azure.IIoT.OpcUa.Publisher.Module.Controller;
    using Azure.IIoT.OpcUa.Publisher.Module.Runtime;
    using Azure.IIoT.OpcUa.Publisher;
    using Furly.Tunnel.Router.Services;
    using Autofac;

    /// <summary>
    /// Container builder extensions
    /// </summary>
    public static class ContainerBuilderEx
    {
        /// <summary>
        /// Add all publisher dependencies minus connectivity components.
        /// </summary>
        /// <param name="builder"></param>
        public static void AddPublisherServices(this ContainerBuilder builder)
        {
            builder.AddNewtonsoftJsonSerializer();
            builder.AddDiagnostics();
            builder.AddPublisherCore();

            builder.RegisterType<PublisherCliOptions>()
                .AsImplementedInterfaces().AsSelf().SingleInstance();

            // Register controllers
            builder.RegisterType<MethodRouter>()
                .AsImplementedInterfaces().SingleInstance()
                .PropertiesAutowired(
                    PropertyWiringOptions.AllowCircularDependencies);

            builder.RegisterType<PublisherMethodsController>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<TwinMethodsController>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<HistoryMethodsController>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<DiscoveryMethodsController>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();
        }
    }
}
