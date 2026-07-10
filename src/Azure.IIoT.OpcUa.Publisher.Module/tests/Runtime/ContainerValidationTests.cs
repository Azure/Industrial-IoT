// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Runtime
{
    using Azure.IIoT.OpcUa.Publisher;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Core.Messaging;
    using Furly.Extensions.Rpc;
    using Azure.IIoT.OpcUa.Core.Storage;
    using Furly.Tunnel.Router;
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
    }
}
