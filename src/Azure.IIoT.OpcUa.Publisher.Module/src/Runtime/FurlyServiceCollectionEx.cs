// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Runtime
{
    using Furly;
    using Furly.Azure;
    using Furly.Azure.IoT.Edge;
    using Furly.Azure.IoT.Edge.Services;
    using Furly.Azure.Runtime;
    using Azure.IIoT.OpcUa.Core.Configuration;
    using Furly.Extensions.Messaging;
    using Furly.Extensions.Messaging.Clients;
    using Furly.Extensions.Rpc;
    using Furly.Extensions.Rpc.Servers;
    using Furly.Extensions.Serializers;
    using Furly.Extensions.Serializers.Json;
    using Furly.Extensions.Serializers.Newtonsoft;
    using Furly.Extensions.Storage;
    using Furly.Extensions.Storage.Services;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using System;

    /// <summary>
    /// IIoT owned <see cref="IServiceCollection"/> registrations that mirror the
    /// Autofac <c>ContainerBuilder</c> extensions shipped in the referenced Furly
    /// packages. The released Furly packages only expose Autofac extensions, so the
    /// registration logic (copied from the Furly source) lives here and calls the
    /// public Furly service types directly. Internal Furly post configure option
    /// classes are re-implemented below.
    /// </summary>
    public static class FurlyServiceCollectionEx
    {
        /// <summary>
        /// Add default json serializer (mirror of Furly.Extensions.Json).
        /// </summary>
        /// <param name="services"></param>
        public static IServiceCollection AddDefaultJsonSerializer(
            this IServiceCollection services)
        {
            return services
                .AddSingleton<DefaultJsonSerializer>()
                .AddSingleton<ISerializer>(
                    x => x.GetRequiredService<DefaultJsonSerializer>())
                .AddSingleton<IJsonSerializer>(
                    x => x.GetRequiredService<DefaultJsonSerializer>())
                .AddSingleton<IJsonSerializerSettingsProvider>(
                    x => x.GetRequiredService<DefaultJsonSerializer>());
        }

        /// <summary>
        /// Add newtonsoft json serializer (mirror of Furly.Extensions.Newtonsoft).
        /// </summary>
        /// <param name="services"></param>
        public static IServiceCollection AddNewtonsoftJsonSerializer(
            this IServiceCollection services)
        {
            return services
                .AddSingleton<NewtonsoftJsonSerializer>()
                .AddSingleton<ISerializer>(
                    x => x.GetRequiredService<NewtonsoftJsonSerializer>())
                .AddSingleton<IJsonSerializer>(
                    x => x.GetRequiredService<NewtonsoftJsonSerializer>())
                .AddSingleton<INewtonsoftSerializerSettingsProvider>(
                    x => x.GetRequiredService<NewtonsoftJsonSerializer>());
        }

        /// <summary>
        /// Add default azure credentials (mirror of Furly.Azure). The Autofac
        /// version registered the credential provider per lifetime scope; it is
        /// registered as a singleton here so it can be resolved from the root
        /// provider (it is stateless and shared across the app).
        /// </summary>
        /// <param name="services"></param>
        public static IServiceCollection AddDefaultAzureCredentials(
            this IServiceCollection services)
        {
            services.AddOptions();
            services.TryAddSingletonForwarded<ICredentialProvider, DefaultAzureCredentials>();
            services.AddSingleton<IPostConfigureOptions<CredentialOptions>, CredentialConfig>();
            return services;
        }

        /// <summary>
        /// Add file system event client (mirror of Furly.Extensions).
        /// </summary>
        /// <param name="services"></param>
        public static IServiceCollection AddFileSystemEventClient(
            this IServiceCollection services)
        {
            services.AddAs<FileSystemEventClient>(ServiceLifetime.Singleton,
                typeof(IEventClient));
            services.AddSingletonAsImplementedInterfaces<FileSystemClientFactory>();
            return services;
        }

        /// <summary>
        /// Add file system rpc server (mirror of Furly.Extensions).
        /// </summary>
        /// <param name="services"></param>
        public static IServiceCollection AddFileSystemRpcServer(
            this IServiceCollection services)
        {
            return services.AddSingletonAsImplementedInterfaces<FileSystemRpcServer>();
        }

        /// <summary>
        /// Add http event client (mirror of Furly.Extensions).
        /// </summary>
        /// <param name="services"></param>
        public static IServiceCollection AddHttpEventClient(this IServiceCollection services)
        {
            return services.AddAs<HttpEventClient>(ServiceLifetime.Transient,
                typeof(IEventClient));
        }

        /// <summary>
        /// Add IoT edge services (mirror of Furly.Azure.IoT.Edge).
        /// </summary>
        /// <param name="services"></param>
        public static IServiceCollection AddIoTEdgeServices(this IServiceCollection services)
        {
            services.AddSingleton<IPostConfigureOptions<IoTEdgeClientOptions>,
                IoTEdgeClientConfig>();

            services.AddSingletonAsImplementedInterfaces<IoTEdgeIdentity>();
            services.AddSingletonAsImplementedInterfaces<IoTEdgeHubSdkClient>();
            services.AddTransientAsImplementedInterfaces<IoTEdgeWorkloadApi>();

            services.AddSingletonAsImplementedInterfaces<IoTEdgeEventClient>();
            services.AddSingletonAsImplementedInterfaces<IoTEdgeClientFactory>();
            services.AddSingletonAsImplementedInterfaces<IoTEdgeTwinClient>();
            services.AddSingletonAsImplementedInterfaces<IoTEdgeRpcClient>();
            services.AddSingletonAsImplementedInterfaces<IoTEdgeRpcServer>();
            return services;
        }

        /// <summary>
        /// Register the implementation type as singleton and forward the service
        /// type to it only if the service type was not already registered (mirror of
        /// Autofac IfNotRegistered).
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <typeparam name="TImplementation"></typeparam>
        /// <param name="services"></param>
        private static IServiceCollection TryAddSingletonForwarded<TService, TImplementation>(
            this IServiceCollection services)
            where TService : class
            where TImplementation : class, TService
        {
            foreach (var descriptor in services)
            {
                if (descriptor.ServiceType == typeof(TService))
                {
                    return services;
                }
            }
            services.AddSingleton<TImplementation>();
            services.AddSingleton<TService>(
                sp => sp.GetRequiredService<TImplementation>());
            return services;
        }
    }

    /// <summary>
    /// IoT Edge client configuration (copied from internal
    /// Furly.Azure.IoT.Edge.Runtime.IoTEdgeClientConfig).
    /// </summary>
    internal sealed class IoTEdgeClientConfig : PostConfigureOptionBase<IoTEdgeClientOptions>
    {
        /// <inheritdoc/>
        public IoTEdgeClientConfig(IConfiguration configuration) :
            base(configuration)
        {
        }

        /// <inheritdoc/>
        public override void PostConfigure(string? name, IoTEdgeClientOptions options)
        {
            if (string.IsNullOrEmpty(options.EdgeHubConnectionString))
            {
                options.EdgeHubConnectionString =
                    GetStringOrDefault(nameof(options.EdgeHubConnectionString));
            }
            if (options.Transport == 0)
            {
                options.Transport = Enum.Parse<TransportOption>(
                    GetStringOrDefault(nameof(options.Transport),
                        nameof(TransportOption.MqttOverTcp)), true);
            }
        }
    }
}
