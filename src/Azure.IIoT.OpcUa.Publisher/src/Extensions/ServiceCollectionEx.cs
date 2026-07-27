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
    using Azure.IIoT.OpcUa.Publisher.PubSub;
    using Azure.IIoT.OpcUa.Core;
    using Azure.IIoT.OpcUa.Core.IoTEdge;
    using Azure.IIoT.OpcUa.Core.Messaging;
    using Azure.IIoT.OpcUa.Core.Messaging.Clients;
    using Microsoft.Extensions.Configuration;
    using Azure.IIoT.OpcUa.Core.Storage;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;

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

            services.AddTransient<PublisherConfig>();
            services.AddTransient<IPostConfigureOptions<PublisherOptions>>(
                static sp => sp.GetRequiredService<PublisherConfig>());

            services.AddSingleton<PhysicalFileProviderFactory>();
            services.AddSingleton<IFileProviderFactory>(
                static sp => sp.GetRequiredService<PhysicalFileProviderFactory>());
            services.AddSingleton<PublishedNodesProvider>();
            services.AddSingleton<IStorageProvider>(
                static sp => sp.GetRequiredService<PublishedNodesProvider>());
            services.AddSingleton<PublishedNodesConverter>();
            services.AddSingleton<PublishedNodesJsonServices>();
            services.AddSingleton<IAwaitable<PublishedNodesJsonServices>>(
                static sp => sp.GetRequiredService<PublishedNodesJsonServices>());
            services.AddSingleton<IAwaitable>(
                static sp => sp.GetRequiredService<PublishedNodesJsonServices>());
            services.AddSingleton<IPublishedNodesServices>(
                static sp => sp.GetRequiredService<PublishedNodesJsonServices>());
            services.AddSingleton<IPublishServices<ConnectionModel>>(
                static sp => sp.GetRequiredService<PublishedNodesJsonServices>());
            services.AddSingleton<PublisherService>();
            services.AddSingleton<IPublisher>(
                static sp => sp.GetRequiredService<PublisherService>());
            services.AddSingleton<IMetricsContext>(
                static sp => sp.GetRequiredService<PublisherService>());

            // The diagnostic collector used to be an Autofac IStartable (started
            // before the module hosted service). Register it (and its hosted
            // service facet) before the module so the host starts it first.
            services.AddSingleton<PublisherDiagnosticCollector>();
            services.AddSingleton<IDiagnosticCollector>(
                static sp => sp.GetRequiredService<PublisherDiagnosticCollector>());
            services.AddSingleton<IHostedService>(
                static sp => sp.GetRequiredService<PublisherDiagnosticCollector>());
            services.AddSingleton<RuntimeStateReporter>();
            services.AddSingleton<IRuntimeStateReporter>(
                static sp => sp.GetRequiredService<RuntimeStateReporter>());
            services.AddSingleton<IApiKeyProvider>(
                static sp => sp.GetRequiredService<RuntimeStateReporter>());
            services.AddSingleton<ISslCertProvider>(
                static sp => sp.GetRequiredService<RuntimeStateReporter>());

            // The module hosted service
            services.AddSingleton<PublisherModule>();
            services.AddSingleton<IHostedService>(
                static sp => sp.GetRequiredService<PublisherModule>());
            services.AddSingleton<IIoTEdgeClientState>(
                static sp => sp.GetRequiredService<PublisherModule>());
            services.AddSingleton<IProcessControl>(
                static sp => sp.GetRequiredService<PublisherModule>());

            services.AddSingleton<WriterGroupScopeFactory>();
            services.AddSingleton<IWriterGroupScopeFactory>(
                static sp => sp.GetRequiredService<WriterGroupScopeFactory>());

            // Per connection service facades (Autofac InstancePerLifetimeScope). They
            // are resolved from the root by the singleton method router as well as
            // per request by the MVC pipeline, therefore they are registered as
            // transient so they can be created from the root scope without violating
            // scope validation.
            services.AddTransient<NodeServices<ConnectionModel>>();
            services.AddTransient<INodeServices<ConnectionModel>>(
                static sp => sp.GetRequiredService<NodeServices<ConnectionModel>>());
            services.AddTransient<INodeServicesInternal<ConnectionModel>>(
                static sp => sp.GetRequiredService<NodeServices<ConnectionModel>>());
            services.AddTransient<ConfigurationServices>();
            services.AddTransient<IConfigurationServices>(
                static sp => sp.GetRequiredService<ConfigurationServices>());
            services.AddTransient<IAssetConfiguration<System.IO.Stream>>(
                static sp => sp.GetRequiredService<ConfigurationServices>());
            services.AddTransient<IAssetConfiguration<byte[]>>(
                static sp => sp.GetRequiredService<ConfigurationServices>());
            services.AddTransient<HistoryServices<ConnectionModel>>();
            services.AddTransient<IHistoryServices<ConnectionModel>>(
                static sp => sp.GetRequiredService<HistoryServices<ConnectionModel>>());
            services.AddTransient<FileSystemServices<ConnectionModel>>();
            services.AddTransient<IFileSystemServices<ConnectionModel>>(
                static sp => sp.GetRequiredService<FileSystemServices<ConnectionModel>>());
            services.AddTransient<ServerDiscovery>();
            services.AddTransient<IServerDiscovery>(
                static sp => sp.GetRequiredService<ServerDiscovery>());
            services.AddTransient<NetworkDiscovery>();
            services.AddTransient<INetworkDiscovery>(
                static sp => sp.GetRequiredService<NetworkDiscovery>());
            services.AddTransient<IDiscoveryServices>(
                static sp => sp.GetRequiredService<NetworkDiscovery>());
            services.AddTransient<ProgressPublisher>();
            services.AddTransient<IDiscoveryProgress>(
                static sp => sp.GetRequiredService<ProgressPublisher>());

            var useNativePubSub = IsNativePubSubEnabled(services);
            if (useNativePubSub)
            {
                services.AddPubSubShadowHost();
                services.AddSingleton<NativePubSubEventClientSelector>();
                services.AddPubSubShadowEgressHost(
                    static sp => sp.GetRequiredService<NativePubSubEventClientSelector>(),
                    static (sp, options) =>
                    {
                        //
                        // PublishMessageSchema gates whether schema options are
                        // bound at all, so their presence is the signal that the
                        // user asked for schemas. When they did and the selected
                        // transport cannot carry one, the egress drops the schema
                        // with a warning rather than refusing to publish.
                        //
                        options.IncludeSchema = sp
                            .GetRequiredService<IOptions<PublisherOptions>>()
                            .Value.SchemaOptions is not null;
                    });
            }

            services.AddWriterGroupProcessing(useNativePubSub);
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
            this IServiceCollection services, bool useNativePubSub)
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

            if (useNativePubSub)
            {
                services.AddScoped<IMessageSink>(static sp => new PubSubNotificationSink(
                    sp.GetRequiredService<IManagedPubSubNotificationBuffer>(),
                    sp.GetRequiredService<ILogger<PubSubNotificationSink>>()));
            }
            else
            {
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
            }

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

        private static bool IsNativePubSubEnabled(IServiceCollection services)
        {
            foreach (var descriptor in services)
            {
                if (descriptor.ServiceType == typeof(IConfiguration) &&
                    descriptor.ImplementationInstance is IConfiguration configuration &&
                    IsTrue(configuration[PublisherConfig.UseNativePubSubKey]))
                {
                    return true;
                }
                if (descriptor.ServiceType == typeof(IConfigurationRoot) &&
                    descriptor.ImplementationInstance is IConfiguration configurationRoot &&
                    IsTrue(configurationRoot[PublisherConfig.UseNativePubSubKey]))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsTrue(string? value)
        {
            //
            // Must accept the same aliases as ConfigureOptionBase.GetBoolOrNull,
            // otherwise a value such as "1" would enable the option in
            // PublisherOptions while leaving the services unregistered, and the
            // flag would be silently ignored.
            //
            return value != null && value.ToUpperInvariant() switch
            {
                "TRUE" or "YES" or "Y" or "1" => true,
                _ => false
            };
        }

        /// <summary>
        /// Applies the writer path transport selection to the native PubSub
        /// egress, so a writer group that names its own transport publishes
        /// through that transport instead of an application-wide default.
        /// </summary>
        private sealed class NativePubSubEventClientSelector :
            IPubSubShadowEventClientSelector, IDisposable
        {
            public NativePubSubEventClientSelector(IServiceProvider services)
            {
                _services = services;
            }

            public IEventClient Select(WriterGroupModel writerGroup)
            {
                ArgumentNullException.ThrowIfNull(writerGroup);
                var eventClients = _services.GetServices<IEventClient>().Reverse().ToList();
                if (eventClients.Count == 0)
                {
                    throw new InvalidOperationException("No transports registered.");
                }
                //
                // A writer group specific transport configuration makes the
                // options own the client they build, so the selection is cached
                // per group and disposed with the selector rather than at the
                // end of this call.
                //
                lock (_gate)
                {
                    var key = writerGroup.Id ?? string.Empty;
                    if (_selected.TryGetValue(key, out var existing))
                    {
                        return existing.EventClient;
                    }
                    var transport = new NetworkMessageSink.TransportOptions(writerGroup,
                        eventClients,
                        _services.GetServices<IEventClientFactory>()
                            .ToDictionary(f => f.Name, StringComparer.OrdinalIgnoreCase),
                        _services.GetRequiredService<IOptions<PublisherOptions>>(),
                        _services.GetRequiredService<ILogger<NetworkMessageSink>>());
                    _selected[key] = transport;
                    return transport.EventClient;
                }
            }

            public void Dispose()
            {
                lock (_gate)
                {
                    foreach (var transport in _selected.Values)
                    {
                        transport.Dispose();
                    }
                    _selected.Clear();
                }
            }

            private readonly Lock _gate = new();
            private readonly Dictionary<string, NetworkMessageSink.TransportOptions> _selected =
                new(StringComparer.Ordinal);
            private readonly IServiceProvider _services;
        }
    }
}
