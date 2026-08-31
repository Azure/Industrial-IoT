// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Messaging.Clients.EventHubs
{
    using global::Azure.Core;
    using Azure.IIoT.OpcUa.Core.AzureSdk;
    using Azure.IIoT.OpcUa.Core.Exceptions;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;
    using Microsoft.Extensions.Options;
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public sealed class EventHubsClientFactoryTests
    {
        [Fact]
        public void NameIdentifiesEventHubTransport()
        {
            var factory = new EventHubsClientFactory(new TrackingScopeFactory(
                CreateServiceProvider(null)));

            Assert.Equal("EventHub", factory.Name);
        }

        [Fact]
        public void CreateEventClientDisposesScopeWhenConstructionFails()
        {
            var scopeFactory = new TrackingScopeFactory(CreateServiceProvider(null));
            var factory = new EventHubsClientFactory(scopeFactory);

            Assert.Throws<InvalidConfigurationException>(() =>
                factory.CreateEventClient("not a connection string", out _));
            Assert.NotNull(scopeFactory.LastScope);
            Assert.Equal(true, scopeFactory.LastScope.Disposed);
        }

        [Fact]
        public void CreateEventClientCopiesBaseOptionsAndDisposesScopeWithClient()
        {
            var scopeFactory = new TrackingScopeFactory(CreateServiceProvider(
                new EventHubsClientOptions
                {
                    MaxEventPayloadSizeInBytes = 1234
                }));
            var factory = new EventHubsClientFactory(scopeFactory);

            var registration = factory.CreateEventClient(
                "Endpoint=sb://example.servicebus.windows.net/;" +
                "SharedAccessKeyName=name;SharedAccessKey=ZmFrZWtleQ==;EntityPath=hub",
                out var client);

            Assert.Equal("EventHub", client.Name);
            Assert.Equal("sb://example.servicebus.windows.net/", client.Identity);
            Assert.Equal(1234, client.MaxEventPayloadSizeInBytes);
            Assert.Equal(false, scopeFactory.LastScope!.Disposed);

            registration.Dispose();

            Assert.Equal(true, scopeFactory.LastScope.Disposed);
        }

        private static IServiceProvider CreateServiceProvider(
            EventHubsClientOptions? options)
        {
            var services = new ServiceCollection();
            services.AddSingleton(Options.Create(options ?? new EventHubsClientOptions()));
            services.AddSingleton<ICredentialProvider, TestCredentialProvider>();
            services.AddSingleton<ILogger<EventHubsClient>>(
                NullLogger<EventHubsClient>.Instance);
            return services.BuildServiceProvider();
        }

        private sealed class TrackingScopeFactory : IServiceScopeFactory
        {
            public TrackingScope? LastScope { get; private set; }

            public TrackingScopeFactory(IServiceProvider serviceProvider)
            {
                _serviceProvider = serviceProvider;
            }

            public IServiceScope CreateScope()
            {
                LastScope = new TrackingScope(_serviceProvider);
                return LastScope;
            }

            private readonly IServiceProvider _serviceProvider;
        }

        private sealed class TrackingScope : IServiceScope
        {
            public IServiceProvider ServiceProvider { get; }
            public bool Disposed { get; private set; }

            public TrackingScope(IServiceProvider serviceProvider)
            {
                ServiceProvider = serviceProvider;
            }

            public void Dispose()
            {
                Disposed = true;
            }
        }

        private sealed class TestCredentialProvider : ICredentialProvider
        {
            public TokenCredential Credential { get; } = new TestTokenCredential();
        }

        private sealed class TestTokenCredential : TokenCredential
        {
            public override AccessToken GetToken(TokenRequestContext requestContext,
                CancellationToken cancellationToken)
            {
                return new AccessToken("token", DateTimeOffset.MaxValue);
            }

            public override ValueTask<AccessToken> GetTokenAsync(
                TokenRequestContext requestContext, CancellationToken cancellationToken)
            {
                return ValueTask.FromResult(GetToken(requestContext, cancellationToken));
            }
        }
    }
}
