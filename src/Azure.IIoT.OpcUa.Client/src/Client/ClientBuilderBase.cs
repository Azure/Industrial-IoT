// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation, The OPC Foundation, Inc.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client
{
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Opc.Ua.Configuration;
    using System;
    using System.Diagnostics;
    using System.Diagnostics.Metrics;

    /// <summary>
    /// Builds clients that are then used to connect sessions to a server. Theses
    /// sessions can then be used to create subscriptions and monitored items or
    /// send service requests and receive responses.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="S"></typeparam>
    /// <typeparam name="C"></typeparam>
    /// <typeparam name="O"></typeparam>
    /// <typeparam name="OBuilder"></typeparam>
    /// <param name="services"></param>
    public abstract class ClientBuilderBase<T, S, C, O, OBuilder>(IServiceCollection? services = null) :
        IClientBuilder<T, S, C, O>, IApplicationConfigurationBuilder<T, S, C, O>,
        IApplicationNameBuilder<T, S, C, O>, IApplicationUriBuilder<T, S, C, O>,
        IProductBuilder<T, S, C, O>
        where T : PooledSessionOptions, new()
        where S : SessionOptions, new()
        where C : SessionCreateOptions, new()
        where O : ClientOptions, new()
        where OBuilder : IClientOptionsBuilder<O>, new()
    {
        /// <inheritdoc/>
        public IServiceCollection Services { get; } = services ?? new ServiceCollection();

        /// <inheritdoc/>
        public IApplicationNameBuilder<T, S, C, O> NewClientServer
        {
            get
            {
                _applicationType = ApplicationType.ClientAndServer;
                return this;
            }
        }

        /// <inheritdoc/>
        public IApplicationNameBuilder<T, S, C, O> NewClient
        {
            get
            {
                _applicationType = ApplicationType.Client;
                return this;
            }
        }

        /// <inheritdoc/>
        public IApplicationUriBuilder<T, S, C, O> WithName(string applicationName)
        {
            _application = new ApplicationInstance
            {
                ApplicationName = applicationName,
                ApplicationType = _applicationType
            };
            return this;
        }

        /// <inheritdoc/>
        public IProductBuilder<T, S, C, O> WithUri(string applicationUri)
        {
            _applicationUri = applicationUri;
            return this;
        }

        /// <inheritdoc/>
        public IApplicationConfigurationBuilder<T, S, C, O> WithProductUri(
            string productUri)
        {
            _builder = _application!.Build(_applicationUri, productUri);
            return this;
        }

        /// <inheritdoc/>
        public IApplicationConfigurationBuilder<T, S, C, O> WithConfiguration(
            Action<IApplicationConfigurationBuilderClientOptions> configure)
        {
            var builder = _builder!.AsClient();
            configure(builder);
            return this;
        }

        /// <inheritdoc/>
        public IApplicationConfigurationBuilder<T, S, C, O> WithSecuritySetting(
            Action<IApplicationConfigurationBuilderSecurity> configure)
        {
            var builder = _builder!.AsClient();
            configure(builder);
            return this;
        }

        /// <inheritdoc/>
        public IApplicationConfigurationBuilder<T, S, C, O> WithTransportQuota(
            Action<IApplicationConfigurationBuilderTransportQuotas> configure)
        {
            configure(_builder!);
            return this;
        }

        /// <inheritdoc/>
        public IApplicationConfigurationBuilder<T, S, C, O> WithOption(
            Action<IClientOptionsBuilder<O>> configure)
        {
            configure(_optionsBuilder);
            return this;
        }

        /// <inheritdoc/>
        public ISessionBuilder<T, S, C> Build()
        {
            // Resolve missing services from DI
            var provider = Services.BuildServiceProvider();

            var options = provider.GetService<IOptions<ClientOptions>>();
            var observability = provider.GetService<IObservability>();
            if (observability == null)
            {
                var loggerFactory = provider.GetService<ILoggerFactory>();
                if (loggerFactory == null)
                {
                    Services.AddLogging(builder => builder.AddConsole());
                    provider = Services.BuildServiceProvider();
                    loggerFactory = provider.GetRequiredService<ILoggerFactory>();
                }

                var meterFactory = provider.GetService<IMeterFactory>();
                var timeProvider = provider.GetService<TimeProvider>();
                var activitySource = provider.GetService<ActivitySource>();

                observability = new Observability(loggerFactory,
                     timeProvider ?? TimeProvider.System,
                     meterFactory ?? new Meters(), activitySource);
            }
            return Build(provider, ((IOptionsBuilder<O>)_optionsBuilder).Options,
                _application!.ApplicationConfiguration, observability);
        }

        /// <summary>
        /// Create
        /// </summary>
        /// <param name="provider"></param>
        /// <param name="options"></param>
        /// <param name="applicationConfiguration"></param>
        /// <param name="observability"></param>
        /// <returns></returns>
        protected abstract ISessionBuilder<T, S, C> Build(ServiceProvider provider, O options,
            ApplicationConfiguration applicationConfiguration, IObservability observability);

        /// <inheritdoc/>
        private sealed record class Observability(ILoggerFactory LoggerFactory,
             TimeProvider TimeProvider, IMeterFactory MeterFactory,
             ActivitySource? ActivitySource) : IObservability;

        /// <inheritdoc/>
        private sealed class Meters : IMeterFactory
        {
            /// <inheritdoc/>
            public Meter Create(MeterOptions options)
            {
                return new Meter(options);
            }

            /// <inheritdoc/>
            public void Dispose()
            {
            }
        }

        private ApplicationType _applicationType;
        private string? _applicationUri;
        private ApplicationInstance? _application;
        private IApplicationConfigurationBuilderTypes? _builder;
        private readonly OBuilder _optionsBuilder = new();
    }
}
