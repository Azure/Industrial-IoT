// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack
{
    using Azure.IIoT.OpcUa.Publisher.Stack.Runtime;
    using Azure.IIoT.OpcUa.Publisher.Stack.Services;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Core;
    using Azure.IIoT.OpcUa.Core.Storage;
    using Azure.IIoT.OpcUa.Publisher.Parser;
    using Azure.IIoT.OpcUa.Encoders;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Options;
    using Opc.Ua;
    using Opc.Ua.Configuration;

    /// <summary>
    /// Service collection extensions
    /// </summary>
    public static class OpcUaStackServiceCollectionEx
    {
        /// <summary>
        /// Configure services
        /// </summary>
        /// <param name="services"></param>
        public static IServiceCollection AddOpcUaStack(this IServiceCollection services)
        {
            services.TryAddSingleton<IMetricsContext>(IMetricsContext.Empty);

            // OpcUaStack registers the stack logger from its constructor. It used to
            // be an Autofac IStartable/AutoActivate - it is now an IHostedService so
            // that the host eagerly instantiates it during startup.
            services.AddSingleton<OpcUaStack>();
            services.AddSingleton<IHostedService>(
                sp => sp.GetRequiredService<OpcUaStack>());

            // OpcUaStackKeySetLogger starts its work from the constructor. It used to
            // be AutoActivate - registering it as an IHostedService keeps it eagerly
            // instantiated.
            services.AddSingleton<OpcUaStackKeySetLogger>();
            services.AddSingleton<IHostedService>(
                sp => sp.GetRequiredService<OpcUaStackKeySetLogger>());

            services.AddSingleton<OpcUaApplication>();
            services.AddSingleton<IAwaitable<OpcUaApplication>>(
                static sp => sp.GetRequiredService<OpcUaApplication>());
            services.AddSingleton<IAwaitable>(
                static sp => sp.GetRequiredService<OpcUaApplication>());
            services.AddSingleton<IOpcUaConfiguration>(
                static sp => sp.GetRequiredService<OpcUaApplication>());
            services.AddSingleton<IOpcUaCertificates>(
                static sp => sp.GetRequiredService<OpcUaApplication>());
            services.AddSingleton<ICertificatePasswordProvider>(
                static sp => sp.GetRequiredService<OpcUaApplication>());
            services.AddSingleton<OpcUaClientManager>();
            services.AddSingleton<IOpcUaClientManager<ConnectionModel>>(
                static sp => sp.GetRequiredService<OpcUaClientManager>());
            services.AddSingleton<IEndpointDiscovery>(
                static sp => sp.GetRequiredService<OpcUaClientManager>());
            services.AddSingleton<ICertificateServices<EndpointModel>>(
                static sp => sp.GetRequiredService<OpcUaClientManager>());
            services.AddSingleton<IClientDiagnostics>(
                static sp => sp.GetRequiredService<OpcUaClientManager>());
            services.AddSingleton<IConnectionServices<ConnectionModel>>(
                static sp => sp.GetRequiredService<OpcUaClientManager>());

            services.AddTransient<OpcUaClientConfig>();
            services.AddTransient<IPostConfigureOptions<OpcUaClientOptions>>(
                static sp => sp.GetRequiredService<OpcUaClientConfig>());
            services.AddTransient<OpcUaSubscriptionConfig>();
            services.AddTransient<IPostConfigureOptions<OpcUaSubscriptionOptions>>(
                static sp => sp.GetRequiredService<OpcUaSubscriptionConfig>());
            services.AddTransient<ConsoleWriter>();
            services.AddTransient<IFileWriter>(
                static sp => sp.GetRequiredService<ConsoleWriter>());
            services.AddTransient<ZipFileWriter>();
            services.AddTransient<IFileWriter>(
                static sp => sp.GetRequiredService<ZipFileWriter>());

            services.AddTransient<FilterQueryParser>();
            services.AddTransient<IFilterParser>(
                static sp => sp.GetRequiredService<FilterQueryParser>());
            return services;
        }
    }
}
