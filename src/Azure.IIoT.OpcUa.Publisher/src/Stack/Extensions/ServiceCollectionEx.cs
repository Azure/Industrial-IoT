// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack
{
    using Azure.IIoT.OpcUa.Publisher.Stack.Runtime;
    using Azure.IIoT.OpcUa.Publisher.Stack.Services;
    using Azure.IIoT.OpcUa.Publisher.Parser;
    using Azure.IIoT.OpcUa.Encoders;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Microsoft.Extensions.Hosting;

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

            services.AddSingletonAsImplementedInterfaces<OpcUaApplication>();
            services.AddSingletonAsImplementedInterfaces<OpcUaClientManager>();

            services.AddTransientAsImplementedInterfaces<OpcUaClientConfig>();
            services.AddTransientAsImplementedInterfaces<OpcUaSubscriptionConfig>();
            services.AddTransientAsImplementedInterfaces<ConsoleWriter>();
            services.AddTransientAsImplementedInterfaces<ZipFileWriter>();

            services.AddTransientAsImplementedInterfaces<FilterQueryParser>();
            return services;
        }
    }
}
