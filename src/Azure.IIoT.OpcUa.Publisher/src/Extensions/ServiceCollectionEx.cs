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

            //
            // 3.0 publishes through the native PubSub runtime. The option and
            // its command line switch are still read so an existing
            // configuration keeps working, but the custom encoder they used to
            // select is gone, so there is nothing left to branch on here.
            //
            services.AddPubSubShadowHost();
            services.AddSingleton<NativePubSubEventClientSelector>();
            services.AddPubSubShadowEgressHost(
                static sp => sp.GetRequiredService<NativePubSubEventClientSelector>(),
                static (sp, options) =>
                {
                    var publisherOptions = sp
                        .GetRequiredService<IOptions<PublisherOptions>>().Value;
                    //
                    // PublishMessageSchema gates whether schema options are
                    // bound at all, so their presence is the signal that the
                    // user asked for schemas. When they did and the selected
                    // transport cannot carry one, the egress drops the schema
                    // with a warning rather than refusing to publish.
                    //
                    options.IncludeSchema = publisherOptions.SchemaOptions is not null;
                    //
                    // The egress queue is what stands between a broker that
                    // has stopped answering and unbounded growth, and its
                    // depth is the operator's tuning knob for that - a bigger
                    // queue rides out a longer outage, a smaller one applies
                    // backpressure to the subscription sooner. It was left at
                    // its built-in default, so the option said one thing and
                    // the publisher did another - and the built-in default was
                    // 64 where the writer path queued 4096.
                    //
                    // A non-positive value is treated as unset rather than as
                    // a queue of nothing, because the egress rejects a
                    // capacity of zero outright.
                    //
                    options.QueueCapacity =
                        publisherOptions.MaxNetworkMessageSendQueueSize is > 0 and var queue
                            ? queue
                            : PublisherConfig.MaxNetworkMessageSendQueueSizeDefault;
                });

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

            services.AddScoped<IMessageSink>(static sp => new PubSubNotificationSink(
                sp.GetRequiredService<IManagedPubSubNotificationBuffer>(),
                sp.GetRequiredService<ILogger<PubSubNotificationSink>>()));

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

            public PubSubShadowEventClientLease Select(WriterGroupModel writerGroup)
            {
                ArgumentNullException.ThrowIfNull(writerGroup);
                var eventClients = _services.GetServices<IEventClient>().Reverse().ToList();
                if (eventClients.Count == 0)
                {
                    throw new InvalidOperationException("No transports registered.");
                }
                var options = _services.GetRequiredService<IOptions<PublisherOptions>>();
                var selected = WriterGroupTransportOptions.SelectEventClient(
                    writerGroup, eventClients, options.Value);
                var key = (writerGroup.Id ?? string.Empty, selected.Name,
                    string.IsNullOrEmpty(writerGroup.TransportConfiguration)
                        ? null : writerGroup.TransportConfiguration);
                lock (_gate)
                {
                    ObjectDisposedException.ThrowIf(_disposed, this);
                    if (_selected.TryGetValue(key, out var existing))
                    {
                        return existing.Acquire();
                    }
                    var transport = new WriterGroupTransportOptions(writerGroup,
                        eventClients,
                        _services.GetServices<IEventClientFactory>()
                            .ToDictionary(f => f.Name, StringComparer.OrdinalIgnoreCase),
                        options,
                        _services.GetRequiredService<ILogger<WriterGroupTransportOptions>>());
                    var lease = new PubSubShadowEventClientLease(transport.EventClient,
                        transport, () =>
                        {
                            lock (_gate)
                            {
                                _selected.Remove(key);
                            }
                        });
                    // The cache does not own a reference. Its entry disappears
                    // after the last configuration/connection/tombstone releases it.
                    _selected[key] = lease;
                    return lease;
                }
            }

            public void Dispose()
            {
                lock (_gate)
                {
                    _disposed = true;
                    _selected.Clear();
                }
            }

            private readonly Lock _gate = new();
            private readonly Dictionary<(string Group, string Transport, string? Configuration),
                PubSubShadowEventClientLease> _selected = [];
            private readonly IServiceProvider _services;
            private bool _disposed;
        }
    }
}
