// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Runtime
{
    using Furly;
    using Furly.Extensions.Messaging;
    using Furly.Extensions.Messaging.Clients;
    using Furly.Extensions.Rpc;
    using Furly.Extensions.Rpc.Servers;
    using Furly.Extensions.Serializers;
    using Furly.Extensions.Serializers.Json;
    using Furly.Extensions.Serializers.Newtonsoft;
    using Furly.Extensions.Storage;
    using Furly.Extensions.Storage.Services;
    using Microsoft.Extensions.DependencyInjection;
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

}
