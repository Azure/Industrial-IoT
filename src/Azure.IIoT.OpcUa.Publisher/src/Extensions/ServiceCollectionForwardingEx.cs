// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Extensions.DependencyInjection
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Helper extensions that replicate Autofac's <c>AsImplementedInterfaces</c>
    /// registration semantics on top of <see cref="IServiceCollection"/>. The
    /// concrete type is registered with the requested lifetime and every one of
    /// its implemented interfaces (except <see cref="IDisposable"/> and
    /// <see cref="IAsyncDisposable"/>) is forwarded to the same registration so
    /// that resolving any interface (or an <see cref="IEnumerable{T}"/> of it)
    /// returns the same instance for a given scope - exactly as Autofac did.
    /// </summary>
    public static class ServiceCollectionForwardingEx
    {
        /// <summary>
        /// Register concrete type as singleton and forward all implemented
        /// interfaces to it.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="services"></param>
        public static IServiceCollection AddSingletonAsImplementedInterfaces<T>(
            this IServiceCollection services) where T : class
        {
            return services.AddAsImplementedInterfaces(typeof(T), ServiceLifetime.Singleton);
        }

        /// <summary>
        /// Register the concrete type produced by the factory as a singleton and
        /// forward all implemented interfaces to the same instance.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="services"></param>
        /// <param name="factory"></param>
        public static IServiceCollection AddSingletonAsImplementedInterfaces<T>(
            this IServiceCollection services, Func<IServiceProvider, T> factory)
            where T : class
        {
            var implementationType = typeof(T);
            services.Add(new ServiceDescriptor(implementationType, factory,
                ServiceLifetime.Singleton));
            foreach (var interfaceType in implementationType.GetInterfaces())
            {
                if (interfaceType == typeof(IDisposable) ||
                    interfaceType == typeof(IAsyncDisposable))
                {
                    continue;
                }
                services.Add(new ServiceDescriptor(interfaceType,
                    sp => sp.GetRequiredService(implementationType),
                    ServiceLifetime.Singleton));
            }
            return services;
        }

        /// <summary>
        /// Register concrete type as transient and forward all implemented
        /// interfaces to it.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="services"></param>
        public static IServiceCollection AddTransientAsImplementedInterfaces<T>(
            this IServiceCollection services) where T : class
        {
            return services.AddAsImplementedInterfaces(typeof(T), ServiceLifetime.Transient);
        }

        /// <summary>
        /// Register concrete type as scoped and forward all implemented
        /// interfaces to it.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="services"></param>
        public static IServiceCollection AddScopedAsImplementedInterfaces<T>(
            this IServiceCollection services) where T : class
        {
            return services.AddAsImplementedInterfaces(typeof(T), ServiceLifetime.Scoped);
        }

        /// <summary>
        /// Register the concrete type with the given lifetime and forward the
        /// explicitly listed service types to it (mirrors Autofac's
        /// <c>.As&lt;X&gt;()</c>).
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="services"></param>
        /// <param name="lifetime"></param>
        /// <param name="serviceTypes"></param>
        public static IServiceCollection AddAs<T>(this IServiceCollection services,
            ServiceLifetime lifetime, params Type[] serviceTypes) where T : class
        {
            var implementationType = typeof(T);
            services.Add(new ServiceDescriptor(implementationType, implementationType,
                lifetime));
            foreach (var serviceType in serviceTypes)
            {
                services.Add(new ServiceDescriptor(serviceType,
                    sp => sp.GetRequiredService(implementationType), lifetime));
            }
            return services;
        }

        /// <summary>
        /// Register the concrete type and forward all implemented interfaces
        /// (except disposable markers) to it.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="implementationType"></param>
        /// <param name="lifetime"></param>
        public static IServiceCollection AddAsImplementedInterfaces(
            this IServiceCollection services, Type implementationType,
            ServiceLifetime lifetime)
        {
            services.Add(new ServiceDescriptor(implementationType, implementationType,
                lifetime));
            foreach (var interfaceType in implementationType.GetInterfaces())
            {
                if (interfaceType == typeof(IDisposable) ||
                    interfaceType == typeof(IAsyncDisposable))
                {
                    continue;
                }
                services.Add(new ServiceDescriptor(interfaceType,
                    sp => sp.GetRequiredService(implementationType), lifetime));
            }
            return services;
        }
    }
}
