// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Runtime
{
    using Azure.IIoT.OpcUa.Core;
    using Azure.IIoT.OpcUa.Core.AzureSdk;
    using Azure.IIoT.OpcUa.Core.Hosting;
    using Azure.IIoT.OpcUa.Core.IoTEdge;
    using Azure.IIoT.OpcUa.Core.IoTEdge.Services;
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
    using Azure.IIoT.OpcUa.Publisher.Module.Serialization;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Diagnostics.ExceptionSummarization;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Text.Json.Nodes;

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
            services.AddSingleton<IMethodRouterDescriptorProvider,
                Azure_IIoT_OpcUa_Publisher_ModuleMethodRouterDescriptors>();
            services.AddSingleton<IMethodRouterJsonTypeInfoProvider,
                MethodRouterJsonTypeInfoProvider>();
            services.AddSingleton<IMethodRouterJsonSerializer>(s =>
                new MethodRouterJsonSerializer(
                    s.GetServices<IMethodRouterJsonTypeInfoProvider>()));
            services.AddSingleton<MethodRouter>(s =>
            {
                var router = new MethodRouter(s.GetServices<IRpcServer>(),
                    s.GetRequiredService<ILogger<MethodRouter>>(),
                    s.GetRequiredService<IMethodRouterJsonSerializer>(),
                    s.GetService<IExceptionSummarizer>(),
                    s.GetService<IOptions<RouterOptions>>(),
                    s.GetService<TimeProvider>());
                foreach (var controller in s.GetServices<IMethodController>())
                {
                    foreach (var provider in s.GetServices<IMethodRouterDescriptorProvider>())
                    {
                        if (provider.TryRegister(router, controller,
                            router.JsonSerializer))
                        {
                            break;
                        }
                    }
                }
                return router;
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
            services.AddSingleton<MemoryKVStore>();
            services.AddSingleton<IKeyValueStore>(
                static provider => provider.GetRequiredService<MemoryKVStore>());
            return services;
        }

        /// <summary>
        /// Add the null <see cref="IEventClient"/> fallback.
        /// </summary>
        /// <param name="services"></param>
        public static IServiceCollection AddNullEventClient(
            this IServiceCollection services)
        {
            services.AddTransient<NullEventClient>();
            services.AddTransient<IEventClient>(
                static provider => provider.GetRequiredService<NullEventClient>());
            return services;
        }

        /// <summary>
        /// Add default Azure credentials for owned Azure transports.
        /// </summary>
        /// <param name="services"></param>
        public static IServiceCollection AddDefaultAzureCredentials(
            this IServiceCollection services)
        {
            services.AddOptions();
            services.AddSingleton<DefaultAzureCredentials>();
            services.AddSingleton<ICredentialProvider>(
                static provider => provider.GetRequiredService<DefaultAzureCredentials>());
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
            services.AddTransient<EventHubsClient>();
            services.AddTransient<IEventClient>(
                static provider => provider.GetRequiredService<EventHubsClient>());
            services.AddTransient<EventHubsClientFactory>();
            services.AddTransient<IEventClientFactory>(
                static provider => provider.GetRequiredService<EventHubsClientFactory>());
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
            services.AddTransient<DaprPubSubClient>();
            services.AddTransient<IEventClient>(
                static provider => provider.GetRequiredService<DaprPubSubClient>());
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
            services.AddSingleton<DaprStateStoreClient>();
            services.AddSingleton<IKeyValueStore>(
                static provider => provider.GetRequiredService<DaprStateStoreClient>());
            services.AddSingleton<IAwaitable>(
                static provider => provider.GetRequiredService<DaprStateStoreClient>());
            services.AddSingleton<IAwaitable<IKeyValueStore>>(
                static provider => provider.GetRequiredService<DaprStateStoreClient>());
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
            services.AddSingleton<FileSystemEventClient>();
            services.AddSingleton<IEventClient>(
                static provider => provider.GetRequiredService<FileSystemEventClient>());
            services.AddSingleton<FileSystemClientFactory>();
            services.AddSingleton<IEventClientFactory>(
                static provider => provider.GetRequiredService<FileSystemClientFactory>());
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
            services.AddSingleton<FileSystemRpcServer>();
            services.AddSingleton<IRpcServer>(
                static provider => provider.GetRequiredService<FileSystemRpcServer>());
            return services;
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
            services.AddTransient<HttpEventClient>();
            services.AddTransient<IEventClient>(
                static provider => provider.GetRequiredService<HttpEventClient>());
            return services;
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
            services.AddSingleton<MqttClientTransport>();
            services.AddSingleton<IEventClient>(
                static provider => provider.GetRequiredService<MqttClientTransport>());
            services.AddSingleton<IEventSubscriber>(
                static provider => provider.GetRequiredService<MqttClientTransport>());
            services.AddSingleton<IRpcClient>(
                static provider => provider.GetRequiredService<MqttClientTransport>());
            services.AddSingleton<IRpcServer>(
                static provider => provider.GetRequiredService<MqttClientTransport>());
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
            services.AddSingleton<IoTEdgeIdentity>();
            services.AddSingleton<IIoTEdgeDeviceIdentity>(
                static provider => provider.GetRequiredService<IoTEdgeIdentity>());
            services.AddSingleton<IoTEdgeModuleClient>();
            services.AddSingleton<IoTEdgeTransport>();
            services.AddSingleton<IEventClient>(
                static provider => provider.GetRequiredService<IoTEdgeTransport>());
            services.AddSingleton<IEventSubscriber>(
                static provider => provider.GetRequiredService<IoTEdgeTransport>());
            services.AddSingleton<IRpcServer>(
                static provider => provider.GetRequiredService<IoTEdgeTransport>());
            services.AddSingleton<IRpcClient>(
                static provider => provider.GetRequiredService<IoTEdgeTransport>());
            services.AddSingleton<IProcessIdentity>(
                static provider => provider.GetRequiredService<IoTEdgeTransport>());
            services.AddSingleton<IoTEdgeTwinStore>();
            services.AddSingleton<IKeyValueStore>(
                static provider => provider.GetRequiredService<IoTEdgeTwinStore>());
            services.AddSingleton<IAwaitable<IKeyValueStore>>(
                static provider => provider.GetRequiredService<IoTEdgeTwinStore>());
            services.AddSingleton<IAwaitable>(
                static provider => provider.GetRequiredService<IoTEdgeTwinStore>());
            services.AddSingleton<IDictionary<string, JsonNode?>>(
                static provider => provider.GetRequiredService<IoTEdgeTwinStore>());
            services.AddSingleton<ICollection<KeyValuePair<string, JsonNode?>>>(
                static provider => provider.GetRequiredService<IoTEdgeTwinStore>());
            services.AddSingleton<IEnumerable<KeyValuePair<string, JsonNode?>>>(
                static provider => provider.GetRequiredService<IoTEdgeTwinStore>());
            services.AddSingleton<IEnumerable>(
                static provider => provider.GetRequiredService<IoTEdgeTwinStore>());
            services.AddTransient<IoTEdgeWorkloadApi>();
            services.AddTransient<IIoTEdgeWorkloadApi>(
                static provider => provider.GetRequiredService<IoTEdgeWorkloadApi>());
            return services;
        }
    }
}
