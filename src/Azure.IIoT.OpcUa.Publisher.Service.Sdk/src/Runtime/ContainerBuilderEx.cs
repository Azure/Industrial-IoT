// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.Sdk.Runtime
{
    using Azure.IIoT.OpcUa.Publisher.Service.Sdk.Clients;
    using Azure.IIoT.OpcUa.Publisher.Service.Sdk.SignalR;
    using Autofac;
    using Microsoft.Extensions.DependencyInjection;
    using System;

    /// <summary>
    /// Configuration
    /// </summary>
    public static class ContainerBuilderEx
    {
        /// <summary>
        /// Add service sdk
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="configure"></param>
        /// <returns></returns>
        public static ContainerBuilder AddServiceSdk(this ContainerBuilder builder,
            Action<ServiceSdkOptions>? configure = null)
        {
            builder.ConfigureServiceSdk(configure);

            builder.AddMessagePackSerializer();
            builder.AddNewtonsoftJsonSerializer();

            // Register a default http client ...
            builder.ConfigureServices(services => services.AddHttpClient());

            // Register twin and registry services clients
            builder.RegisterType<TwinServiceClient>()
                .AsImplementedInterfaces();
            builder.RegisterType<RegistryServiceClient>()
                .AsImplementedInterfaces();
            builder.RegisterType<HistoryServiceClient>()
                .AsImplementedInterfaces();
            builder.RegisterType<PublisherServiceClient>()
                .AsImplementedInterfaces();

            // ... as well as signalR client (needed for api)
            builder.RegisterType<SignalRHubClient>()
                .AsImplementedInterfaces().SingleInstance();

            // ... with client event callbacks
            builder.RegisterType<RegistryServiceEvents>()
                .AsImplementedInterfaces();
            builder.RegisterType<PublisherServiceEvents>()
                .AsImplementedInterfaces();

            return builder;
        }
    }
}
