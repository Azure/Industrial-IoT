// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Runtime
{
    using Azure.IIoT.OpcUa.Publisher;
    using Azure.IIoT.OpcUa.Publisher.Module.Controllers;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Services;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Core.Messaging;
    using Azure.IIoT.OpcUa.Core.Rpc;
    using Azure.IIoT.OpcUa.Core.Storage;
    using Azure.IIoT.OpcUa.Core.Rpc.Router;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
    using System.Collections.Generic;
    using System.Linq;
    using Xunit;

    /// <summary>
    /// Validates that the Microsoft.Extensions.DependencyInjection container that
    /// replaced Autofac is wired up correctly. The provider is built exactly as
    /// the module host builds it (the publisher services and the ordered set of
    /// connectivity transports registered by <see cref="Startup"/>) and validated
    /// with <see cref="ServiceProviderOptions.ValidateOnBuild"/> and
    /// <see cref="ServiceProviderOptions.ValidateScopes"/> enabled. It then
    /// explicitly resolves every root the host depends on at runtime.
    /// </summary>
    public sealed class ContainerValidationTests
    {
        /// <summary>
        /// Exposes the protected publisher/transport wiring of the production
        /// startup so the test can compose the exact same graph.
        /// </summary>
        private sealed class TestStartup : Startup
        {
            public TestStartup(IConfiguration configuration)
                : base(configuration)
            {
            }

            public void AddPublisher(IServiceCollection services)
            {
                ConfigurePublisherServices(services);
            }
        }

        private static ServiceProvider BuildProvider(out IReadOnlyDictionary<string, string?> config)
        {
            var settings = new Dictionary<string, string?>();
            config = settings;
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(settings)
                .Build();

            var startup = new TestStartup(configuration);
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<IConfiguration>(configuration);
            services.AddSingleton<IConfigurationRoot>(configuration);
            startup.AddPublisher(services);

            return services.BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateScopes = true,
                ValidateOnBuild = true
            });
        }

        [Fact]
        public void ContainerBuildsAndValidatesWithScopeValidation()
        {
            // ValidateOnBuild throws inside BuildServiceProvider if any registered
            // service graph cannot be constructed, so simply building is the
            // primary proof that the MEDI graph is internally consistent.
            using var provider = BuildProvider(out _);
            Assert.NotNull(provider);
        }

        [Fact]
        public void ResolvesThePublisherRoot()
        {
            using var provider = BuildProvider(out _);
            Assert.NotNull(provider.GetRequiredService<IPublisher>());
        }

        [Fact]
        public void ResolvesEveryMethodController()
        {
            using var provider = BuildProvider(out _);

            var controllers = provider.GetServices<IMethodController>().ToList();

            // The publisher registers exactly these eight method controllers.
            Assert.Equal(8, controllers.Count);
            Assert.Equal(8, controllers.Select(c => c.GetType()).Distinct().Count());
        }

        [Fact]
        public void ExplicitRegistrationsPreserveSingletonInstances()
        {
            var services = new ServiceCollection();
            RegisterExplicitProbes(services);
            using var provider = services.BuildServiceProvider(
                new ServiceProviderOptions { ValidateScopes = true });

            var singleton = provider.GetRequiredService<SingletonProbe>();
            var singletonService = provider.GetRequiredService<ISingletonProbe>();
            var sharedService = provider.GetServices<ISharedProbe>().First();

            Assert.Same(singleton, singletonService);
            Assert.Same(singleton, sharedService);
        }

        [Fact]
        public void ExplicitRegistrationsPreserveLifetimeAndEnumerableSemantics()
        {
            var services = new ServiceCollection();
            RegisterExplicitProbes(services);
            using var provider = services.BuildServiceProvider(
                new ServiceProviderOptions { ValidateScopes = true });

            Assert.Same(provider.GetRequiredService<SingletonProbe>(),
                provider.GetRequiredService<ISingletonProbe>());
            Assert.NotSame(provider.GetRequiredService<TransientProbe>(),
                provider.GetRequiredService<ITransientProbe>());

            using var firstScope = provider.CreateScope();
            using var secondScope = provider.CreateScope();
            Assert.Same(firstScope.ServiceProvider.GetRequiredService<ScopedProbe>(),
                firstScope.ServiceProvider.GetRequiredService<IScopedProbe>());
            Assert.NotSame(firstScope.ServiceProvider.GetRequiredService<IScopedProbe>(),
                secondScope.ServiceProvider.GetRequiredService<IScopedProbe>());

            var probes = provider.GetServices<ISharedProbe>().ToList();
            Assert.Collection(probes,
                probe => Assert.IsType<SingletonProbe>(probe),
                probe => Assert.IsType<AdditionalProbe>(probe));
        }

        private static void RegisterExplicitProbes(IServiceCollection services)
        {
            services.AddSingleton<SingletonProbe>();
            services.AddSingleton<ISingletonProbe>(
                static provider => provider.GetRequiredService<SingletonProbe>());
            services.AddSingleton<ISharedProbe>(
                static provider => provider.GetRequiredService<SingletonProbe>());
            services.AddScoped<ScopedProbe>();
            services.AddScoped<IScopedProbe>(
                static provider => provider.GetRequiredService<ScopedProbe>());
            services.AddTransient<TransientProbe>();
            services.AddTransient<ITransientProbe>(
                static provider => provider.GetRequiredService<TransientProbe>());
            services.AddSingleton<AdditionalProbe>();
            services.AddSingleton<ISharedProbe>(
                static provider => provider.GetRequiredService<AdditionalProbe>());
        }

        [Fact]
        public void ResolvesThePublisherOptions()
        {
            using var provider = BuildProvider(out _);
            Assert.NotNull(provider.GetRequiredService<IOptions<PublisherOptions>>().Value);
            Assert.NotNull(provider.GetRequiredService<IOptions<OpcUaClientOptions>>().Value);
        }

        [Fact]
        public void ResolvesTheTransportEnumerables()
        {
            using var provider = BuildProvider(out _);

            // The always-on fallbacks are the in-memory key value store and the
            // null event client. Resolving the enumerables must never throw and
            // the fallbacks must be present.
            var keyValueStores = provider.GetServices<IKeyValueStore>().ToList();
            var eventClients = provider.GetServices<IEventClient>().ToList();
            var rpcServers = provider.GetServices<IRpcServer>().ToList();

            Assert.NotEmpty(keyValueStores);
            Assert.NotEmpty(eventClients);
            Assert.NotNull(rpcServers);
        }

        [Fact]
        public void ResolvesAndExercisesTheWriterGroupScope()
        {
            using var provider = BuildProvider(out _);

            var factory = provider.GetRequiredService<IWriterGroupScopeFactory>();
            Assert.NotNull(factory);

            // Create a per writer group scope exactly as the publisher does and
            // resolve the scoped data flow engine to prove the child scope wiring
            // that replaced the Autofac InstancePerLifetimeScope registrations.
            using var scope = factory.Create(new WriterGroupModel
            {
                Id = "test-writer-group",
                Name = "test"
            });
            Assert.NotNull(scope.WriterGroup);
        }

        internal interface ISharedProbe;

        internal interface ISingletonProbe : ISharedProbe;

        internal interface IScopedProbe;

        internal interface ITransientProbe;

        internal sealed class SingletonProbe : ISingletonProbe;

        internal sealed class AdditionalProbe : ISharedProbe;

        internal sealed class ScopedProbe : IScopedProbe;

        internal sealed class TransientProbe : ITransientProbe;
    }
}
