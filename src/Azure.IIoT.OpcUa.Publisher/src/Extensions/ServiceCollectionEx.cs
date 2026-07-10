// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher
{
    using Azure.IIoT.OpcUa.Publisher.Discovery;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Services;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Publisher.Storage;
    using Azure.IIoT.OpcUa.Core.Messaging;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using System;

    /// <summary>
    /// Service collection extensions
    /// </summary>
    public static class PublisherCoreServiceCollectionEx
    {
        /// <summary>
        /// Configure services
        /// </summary>
        /// <param name="services"></param>
        public static IServiceCollection AddPublisherCore(this IServiceCollection services)
        {
            services.AddOpcUaStack();

            services.AddTransientAsImplementedInterfaces<PublisherConfig>();

            services.AddSingletonAsImplementedInterfaces<PhysicalFileProviderFactory>();
            services.AddSingletonAsImplementedInterfaces<PublishedNodesProvider>();
            services.AddSingleton<PublishedNodesConverter>();
            services.AddSingletonAsImplementedInterfaces<PublishedNodesJsonServices>();
            services.AddSingletonAsImplementedInterfaces<PublisherService>();

            // The diagnostic collector used to be an Autofac IStartable (started
            // before the module hosted service). Register it (and its hosted
            // service facet) before the module so the host starts it first.
            services.AddSingletonAsImplementedInterfaces<PublisherDiagnosticCollector>();
            services.AddSingletonAsImplementedInterfaces<RuntimeStateReporter>();

            // The module hosted service
            services.AddSingletonAsImplementedInterfaces<PublisherModule>();

            services.AddSingletonAsImplementedInterfaces<WriterGroupScopeFactory>();

            // Per connection service facades (Autofac InstancePerLifetimeScope). They
            // are resolved from the root by the singleton method router as well as
            // per request by the MVC pipeline, therefore they are registered as
            // transient so they can be created from the root scope without violating
            // scope validation.
            services.AddTransientAsImplementedInterfaces<NodeServices<ConnectionModel>>();
            services.AddTransientAsImplementedInterfaces<ConfigurationServices>();
            services.AddTransientAsImplementedInterfaces<HistoryServices<ConnectionModel>>();
            services.AddTransientAsImplementedInterfaces<FileSystemServices<ConnectionModel>>();
            services.AddTransientAsImplementedInterfaces<ServerDiscovery>();
            services.AddTransientAsImplementedInterfaces<NetworkDiscovery>();
            services.AddTransientAsImplementedInterfaces<ProgressPublisher>();

            services.AddWriterGroupProcessing();
            return services;
        }

        /// <summary>
        /// Register the per writer group data flow engine. The Autofac
        /// implementation registered these inside the writer group child lifetime
        /// scope. On Microsoft.Extensions.DependencyInjection they are registered as
        /// scoped services whose scope specific dependencies (writer group model,
        /// diagnostics and metrics context) are supplied from the scoped
        /// <see cref="WriterGroupScopeContext"/>.
        /// </summary>
        /// <param name="services"></param>
        private static IServiceCollection AddWriterGroupProcessing(
            this IServiceCollection services)
        {
            services.AddScoped<WriterGroupScopeContext>();

            services.AddScoped<IMessageEncoder>(sp =>
            {
                var context = sp.GetRequiredService<WriterGroupScopeContext>();
                return new NetworkMessageEncoder(
                    sp.GetRequiredService<IOptions<PublisherOptions>>(),
                    context,
                    sp.GetRequiredService<ILogger<NetworkMessageEncoder>>(),
                    sp.GetService<TimeProvider>());
            });

            services.AddScoped<IMessageSink>(sp =>
            {
                var context = sp.GetRequiredService<WriterGroupScopeContext>();
                return new NetworkMessageSink(
                    context.WriterGroup,
                    sp.GetServices<IEventClient>(),
                    sp.GetServices<IEventClientFactory>(),
                    sp.GetRequiredService<IMessageEncoder>(),
                    sp.GetRequiredService<IOptions<PublisherOptions>>(),
                    sp.GetRequiredService<ILogger<NetworkMessageSink>>(),
                    context,
                    context,
                    sp.GetService<TimeProvider>());
            });

            services.AddScoped<IWriterGroupControl>(sp =>
            {
                var context = sp.GetRequiredService<WriterGroupScopeContext>();
                return new WriterGroupDataSource(
                    sp.GetRequiredService<Stack.IOpcUaClientManager<ConnectionModel>>(),
                    context.WriterGroup,
                    sp.GetRequiredService<IMessageSink>(),
                    sp.GetRequiredService<IOptions<PublisherOptions>>(),
                    context,
                    sp.GetRequiredService<ILoggerFactory>(),
                    sp.GetService<TimeProvider>());
            });
            return services;
        }
    }
}
