// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Extensions.DependencyInjection
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    /// <summary>
    /// Registers concrete services and their generated, explicitly declared
    /// forwarding interfaces. The generated table keeps the service graph static
    /// for trimming and Native AOT while preserving the shared-instance behavior
    /// of Autofac's <c>AsImplementedInterfaces</c>.
    /// </summary>
    public static class ServiceCollectionForwardingEx
    {
        /// <summary>
        /// Registers a generated forwarding table for one application assembly.
        /// </summary>
        /// <param name="addForwarders">Adds all implemented interface forwards.</param>
        /// <param name="addExplicit">Adds an explicitly requested interface set.</param>
        public static void RegisterGeneratedTable(
            Func<IServiceCollection, Type, ServiceLifetime, bool> addForwarders,
            Func<IServiceCollection, Type, ServiceLifetime, Type[], bool> addExplicit)
        {
            ArgumentNullException.ThrowIfNull(addForwarders);
            ArgumentNullException.ThrowIfNull(addExplicit);
            lock (s_tablesLock)
            {
                s_tables.Add(new ServiceForwardingTable(addForwarders, addExplicit));
            }
        }

        /// <summary>
        /// Registers a concrete type as singleton and forwards all interfaces
        /// declared by the generated table.
        /// </summary>
        /// <typeparam name="T">Concrete implementation.</typeparam>
        /// <param name="services">Service collection.</param>
        /// <returns>The service collection.</returns>
        public static IServiceCollection AddSingletonAsImplementedInterfaces<T>(
            this IServiceCollection services) where T : class
        {
            AddImplementation<T>(services, ServiceLifetime.Singleton);
            AddForwarders<T>(services, ServiceLifetime.Singleton);
            return services;
        }

        /// <summary>
        /// Registers a singleton factory and forwards all interfaces declared by
        /// the generated table to its shared instance.
        /// </summary>
        /// <typeparam name="T">Concrete implementation.</typeparam>
        /// <param name="services">Service collection.</param>
        /// <param name="factory">Concrete implementation factory.</param>
        /// <returns>The service collection.</returns>
        public static IServiceCollection AddSingletonAsImplementedInterfaces<T>(
            this IServiceCollection services, Func<IServiceProvider, T> factory)
            where T : class
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(factory);
            services.AddSingleton(factory);
            AddForwarders<T>(services, ServiceLifetime.Singleton);
            return services;
        }

        /// <summary>
        /// Registers a concrete type as transient and forwards all interfaces
        /// declared by the generated table.
        /// </summary>
        /// <typeparam name="T">Concrete implementation.</typeparam>
        /// <param name="services">Service collection.</param>
        /// <returns>The service collection.</returns>
        public static IServiceCollection AddTransientAsImplementedInterfaces<T>(
            this IServiceCollection services) where T : class
        {
            AddImplementation<T>(services, ServiceLifetime.Transient);
            AddForwarders<T>(services, ServiceLifetime.Transient);
            return services;
        }

        /// <summary>
        /// Registers a concrete type as scoped and forwards all interfaces declared
        /// by the generated table.
        /// </summary>
        /// <typeparam name="T">Concrete implementation.</typeparam>
        /// <param name="services">Service collection.</param>
        /// <returns>The service collection.</returns>
        public static IServiceCollection AddScopedAsImplementedInterfaces<T>(
            this IServiceCollection services) where T : class
        {
            AddImplementation<T>(services, ServiceLifetime.Scoped);
            AddForwarders<T>(services, ServiceLifetime.Scoped);
            return services;
        }

        /// <summary>
        /// Registers a concrete type and the explicitly listed service types.
        /// The generated table validates that this exact service map is declared.
        /// </summary>
        /// <typeparam name="T">Concrete implementation.</typeparam>
        /// <param name="services">Service collection.</param>
        /// <param name="lifetime">Registration lifetime.</param>
        /// <param name="serviceTypes">Explicit service interface types.</param>
        /// <returns>The service collection.</returns>
        public static IServiceCollection AddAs<T>(this IServiceCollection services,
            ServiceLifetime lifetime, params Type[] serviceTypes) where T : class
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(serviceTypes);
            foreach (var table in GetTables())
            {
                if (table.AddExplicit(services, typeof(T), lifetime, serviceTypes))
                {
                    return services;
                }
            }
            throw new InvalidOperationException(
                $"No generated explicit service forwarding table exists for {typeof(T)}.");
        }

        /// <summary>
        /// Adds a concrete service through the strongly typed DI helpers.
        /// </summary>
        /// <typeparam name="T">Concrete implementation.</typeparam>
        /// <param name="services">Service collection.</param>
        /// <param name="lifetime">Registration lifetime.</param>
        public static void AddImplementation<T>(IServiceCollection services,
            ServiceLifetime lifetime) where T : class
        {
            ArgumentNullException.ThrowIfNull(services);
            switch (lifetime)
            {
                case ServiceLifetime.Singleton:
                    services.AddSingleton<T>();
                    break;
                case ServiceLifetime.Scoped:
                    services.AddScoped<T>();
                    break;
                case ServiceLifetime.Transient:
                    services.AddTransient<T>();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(lifetime));
            }
        }

        /// <summary>
        /// Adds one strongly typed service forward to its concrete registration.
        /// </summary>
        /// <typeparam name="TImplementation">Concrete implementation.</typeparam>
        /// <typeparam name="TService">Forwarded service.</typeparam>
        /// <param name="services">Service collection.</param>
        /// <param name="lifetime">Registration lifetime.</param>
        public static void AddForward<TImplementation, TService>(
            IServiceCollection services, ServiceLifetime lifetime)
            where TImplementation : class, TService
            where TService : class
        {
            ArgumentNullException.ThrowIfNull(services);
            switch (lifetime)
            {
                case ServiceLifetime.Singleton:
                    services.AddSingleton<TService>(
                        static provider => provider.GetRequiredService<TImplementation>());
                    break;
                case ServiceLifetime.Scoped:
                    services.AddScoped<TService>(
                        static provider => provider.GetRequiredService<TImplementation>());
                    break;
                case ServiceLifetime.Transient:
                    services.AddTransient<TService>(
                        static provider => provider.GetRequiredService<TImplementation>());
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(lifetime));
            }
        }

        private static void AddForwarders<T>(IServiceCollection services,
            ServiceLifetime lifetime) where T : class
        {
            foreach (var table in GetTables())
            {
                if (table.AddForwarders(services, typeof(T), lifetime))
                {
                    return;
                }
            }
            throw new InvalidOperationException(
                $"No generated service forwarding table exists for {typeof(T)}.");
        }

        private static ServiceForwardingTable[] GetTables()
        {
            lock (s_tablesLock)
            {
                return s_tables.ToArray();
            }
        }

        private sealed class ServiceForwardingTable
        {
            public Func<IServiceCollection, Type, ServiceLifetime, bool> AddForwarders { get; }
            public Func<IServiceCollection, Type, ServiceLifetime, Type[], bool> AddExplicit { get; }

            public ServiceForwardingTable(
                Func<IServiceCollection, Type, ServiceLifetime, bool> addForwarders,
                Func<IServiceCollection, Type, ServiceLifetime, Type[], bool> addExplicit)
            {
                AddForwarders = addForwarders;
                AddExplicit = addExplicit;
            }
        }

        private static readonly Lock s_tablesLock = new();
        private static readonly List<ServiceForwardingTable> s_tables = [];
    }
}
