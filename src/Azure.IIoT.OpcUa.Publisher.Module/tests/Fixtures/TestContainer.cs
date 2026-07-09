// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Fixtures
{
    using Microsoft.Extensions.DependencyInjection;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Small disposable wrapper around a Microsoft.Extensions.DependencyInjection
    /// service provider that exposes an Autofac-like <see cref="Resolve{T}"/> API to
    /// keep the test call sites terse after the Autofac to MEDI migration.
    /// </summary>
    public sealed class TestContainer : IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// Underlying service provider
        /// </summary>
        public IServiceProvider Services => _provider;

        /// <summary>
        /// Create container
        /// </summary>
        /// <param name="provider"></param>
        public TestContainer(ServiceProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        /// <summary>
        /// Resolve a required service
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Resolve<T>() where T : notnull
        {
            return _provider.GetRequiredService<T>();
        }

        /// <summary>
        /// Resolve an optional service
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T ResolveOptional<T>() where T : class
        {
            return _provider.GetService<T>();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _provider.Dispose();
        }

        /// <inheritdoc/>
        public ValueTask DisposeAsync()
        {
            return _provider.DisposeAsync();
        }

        private readonly ServiceProvider _provider;
    }
}
