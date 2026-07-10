// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Runtime
{
    using Azure.IIoT.OpcUa.Core;
    using Azure.IIoT.OpcUa.Core.AzureSdk;
    using Azure.IIoT.OpcUa.Core.Messaging;
    using Azure.IIoT.OpcUa.Core.Messaging.Clients;
    using Azure.IIoT.OpcUa.Core.Messaging.Clients.Dapr;
    using Azure.IIoT.OpcUa.Core.Messaging.Clients.EventHubs;
    using Azure.IIoT.OpcUa.Core.Messaging.Clients.IoTEdge;
    using Azure.IIoT.OpcUa.Core.Messaging.Clients.Mqtt;
    using Azure.IIoT.OpcUa.Core.Rpc;
    using Azure.IIoT.OpcUa.Core.Rpc.Router;
    using Azure.IIoT.OpcUa.Core.Rpc.Servers;
    using Azure.IIoT.OpcUa.Core.Storage;
    using Azure.IIoT.OpcUa.Core.Storage.Services;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Diagnostics.ExceptionSummarization;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using System;

    /// <summary>
    /// <see cref="IServiceCollection"/> registrations for the in-repo
    /// <c>Azure.IIoT.OpcUa.Core</c> messaging and storage implementations that
    /// replace the corresponding Legacy.Extensions services.
    /// </summary>
    public static class CoreServiceCollectionEx
    {
        /// <summary>
        /// Add method router (in-repo <c>Azure.IIoT.OpcUa.Core</c> tunnel). The
        /// IIoT host uses a singleton method router with property injected
        /// controllers.
        /// </summary>
        /// <param name="services"></param>
        public static IServiceCollection AddMethodRouter(this IServiceCollection services)
        {
            services.AddOptions();
            services.AddSingleton<MethodRouter>(s =>
                new MethodRouter(s.GetServices<IRpcServer>(),
                    s.GetRequiredService<ILogger<MethodRouter>>(),
                    s.GetService<IExceptionSummarizer>(),
                    s.GetService<IOptions<RouterOptions>>(),
                    s.GetService<TimeProvider>())
                {
                    Controllers = s.GetServices<IMethodController>()
                });
            services.AddSingleton<IRpcHandler>(
                s => s.GetRequiredService<MethodRouter>());
            services.AddSingleton<IAwaitable<MethodRouter>>(
                s => s.GetRequiredService<MethodRouter>());
            services.AddSingleton<IAwaitable>(
                s => s.GetRequiredService<MethodRouter>());
            return services;
        }

        /// <summary>
        /// Add the in-memory <see cref="IKeyValueStore"/> fallback.
        /// </summary>
        /// <param name="services"></param>
        public static IServiceCollection AddMemoryKeyValueStore(
            this IServiceCollection services)
        {
            return services.AddSingletonAsImplementedInterfaces<MemoryKVStore>();
        }

        /// <summary>
        /// Add the null <see cref="IEventClient"/> fallback.
        /// </summary>
        /// <param name="services"></param>
        public static IServiceCollection AddNullEventClient(
            this IServiceCollection services)
        {
            return services.AddAs<NullEventClient>(ServiceLifetime.Transient,
                typeof(IEventClient));
        }

        /// <summary>
        /// Add default Azure credentials for owned Azure transports.
        /// </summary>
        /// <param name="services"></param>
        public static IServiceCollection AddDefaultAzureCredentials(
            this IServiceCollection services)
        {
            services.AddOptions();
            services.AddSingletonAsImplementedInterfaces<DefaultAzureCredentials>();
            services.AddSingleton<IPostConfigureOptions<CredentialOptions>, CredentialConfig>();
            return services;
        }

        /// <summary>
        /// Add the Event Hubs event transport.
        /// </summary>
        /// <param name="services"></param>
        public static IServiceCollection AddHubEventClient(
            this IServiceCollection services)
        {
            services.AddOptions();
            AddDefaultAzureCredentials(services);
            services.AddTransientAsImplementedInterfaces<EventHubsClient>();
            services.AddTransientAsImplementedInterfaces<EventHubsClientFactory>();
            services.AddSingleton<IPostConfigureOptions<EventHubsClientOptions>,
                EventHubsClientConfig>();
            return services;
        }

        /// <summary>
        /// Add the Dapr pub/sub event transport.
        /// </summary>
        /// <param name="services"></param>
        public static IServiceCollection AddDaprPubSubClient(
            this IServiceCollection services)
        {
            services.AddAs<DaprPubSubClient>(ServiceLifetime.Transient,
                typeof(IEventClient));
            services.AddOptions();
            services.AddSingleton<IPostConfigureOptions<DaprOptions>, DaprConfig>();
            return services;
        }

        /// <summary>
        /// Add the Dapr state store.
        /// </summary>
        /// <param name="services"></param>
        public static IServiceCollection AddDaprStateStoreClient(
            this IServiceCollection services)
        {
            services.AddAs<DaprStateStoreClient>(ServiceLifetime.Singleton,
                typeof(IKeyValueStore), typeof(IAwaitable),
                typeof(IAwaitable<IKeyValueStore>));
            services.AddOptions();
            services.AddSingleton<IPostConfigureOptions<DaprOptions>, DaprConfig>();
            return services;
        }

        /// <summary>
        /// Add the filesystem event transport.
        /// </summary>
        /// <param name="services"></param>
        public static IServiceCollection AddFileSystemEventClient(
            this IServiceCollection services)
        {
            services.AddOptions();
            services.AddAs<FileSystemEventClient>(ServiceLifetime.Singleton,
                typeof(IEventClient));
            services.AddSingletonAsImplementedInterfaces<FileSystemClientFactory>();
            return services;
        }

        /// <summary>
        /// Add the filesystem RPC server.
        /// </summary>
        /// <param name="services"></param>
        public static IServiceCollection AddFileSystemRpcServer(
            this IServiceCollection services)
        {
            services.AddOptions();
            return services.AddSingletonAsImplementedInterfaces<FileSystemRpcServer>();
        }

        /// <summary>
        /// Add the HTTP event transport.
        /// </summary>
        /// <param name="services"></param>
        public static IServiceCollection AddHttpEventClient(
            this IServiceCollection services)
        {
            services.AddOptions();
            services.AddHttpClient();
            return services.AddAs<HttpEventClient>(ServiceLifetime.Transient,
                typeof(IEventClient));
        }

        /// <summary>
        /// Add the mqtt transport (in-repo <c>Azure.IIoT.OpcUa.Core</c> client
        /// built on the <c>Mqtt.Client</c> library) implementing the event and
        /// rpc abstractions. Replaces the former Legacy.Extensions.Mqtt client.
        /// </summary>
        /// <param name="services"></param>
        public static IServiceCollection AddMqttClient(
            this IServiceCollection services)
        {
            services.AddOptions();
            services.AddSingletonAsImplementedInterfaces<MqttClientTransport>();
            services.AddSingleton<IPostConfigureOptions<MqttOptions>, MqttConfig>();
            return services;
        }

        /// <summary>
        /// Add the IoT Edge transport backed by IoTHubby/IoTHubby.Edge.
        /// </summary>
        /// <param name="services"></param>
        public static IServiceCollection AddIoTEdgeServices(
            this IServiceCollection services)
        {
            services.AddOptions();
            services.AddSingletonAsImplementedInterfaces<IoTEdgeIdentity>();
            services.AddSingleton<IoTEdgeModuleClient>();
            services.AddSingletonAsImplementedInterfaces<IoTEdgeTransport>();
            services.AddSingletonAsImplementedInterfaces<IoTEdgeTwinStore>();
            services.AddTransientAsImplementedInterfaces<IoTEdgeWorkloadApi>();
            return services;
        }
    }
}
