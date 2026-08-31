// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Messaging.Clients.IoTEdge
{
    using Azure.IIoT.OpcUa.Core.IoTEdge;
    using global::IoTHubby;
    using Microsoft.Extensions.Options;
    using Moq;
    using System;
    using System.Threading.Tasks;
    using Xunit;

    public sealed class IoTEdgeModuleClientTests
    {
        [Fact]
        public void ConstructorRejectsNullDependenciesBeforeCreatingSdkClient()
        {
            Assert.Throws<ArgumentNullException>(() => new IoTEdgeModuleClient(
                null!, new TestIdentity(), []));
            Assert.Throws<ArgumentNullException>(() => new IoTEdgeModuleClient(
                Options.Create(new IoTEdgeClientOptions()), null!, []));
        }

        [Fact]
        public async Task ConstructorUsesFactoryAndConfiguresSdkOptionsAsync()
        {
            var sdk = new IoTEdgeTestModuleClient();
            var factory = new IoTEdgeTestModuleClientFactory(sdk);
            using var loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(
                builder => _ = builder);
            var options = Options.Create(new IoTEdgeClientOptions
            {
                Product = "product",
                KeepAlivePeriodSeconds = 42,
                DefaultMethodCallTimeout = TimeSpan.FromSeconds(7)
            });

            await using var client = new IoTEdgeModuleClient(options, new TestIdentity(), [],
                loggerFactory, factory);

            Assert.Same(options.Value, factory.Options);
            Assert.Equal("product", factory.ConfiguredOptions!.ProductInfo);
            Assert.Equal(TimeSpan.FromSeconds(42), factory.ConfiguredOptions.KeepAlive);
            Assert.Equal(TimeSpan.FromSeconds(7),
                factory.ConfiguredOptions.OperationTimeout);
            Assert.Same(loggerFactory, factory.ConfiguredOptions.LoggerFactory);
        }

        [Fact]
        public async Task EnsureConnectedConnectsOnlyWhenDisconnectedAsync()
        {
            var sdk = new IoTEdgeTestModuleClient();
            await using var client = CreateClient(sdk);

            await client.EnsureConnectedAsync(default);
            await client.EnsureConnectedAsync(default);

            Assert.Equal(1, sdk.ConnectCount);
        }

        [Fact]
        public async Task DisposeUnsubscribesDisposesAndReportsClosedAsync()
        {
            var sdk = new IoTEdgeTestModuleClient();
            var state = new Mock<IIoTEdgeClientState>(MockBehavior.Strict);
            state.Setup(s => s.OnOpened(1, "device", "module"));
            state.Setup(s => s.OnClosed(1, "device", "module", "Disposed"));
            var client = CreateClient(sdk, state.Object);

            sdk.RaiseStateChanged(IoTHubConnectionState.Connected);
            await client.DisposeAsync();
            sdk.RaiseStateChanged(IoTHubConnectionState.Disconnected, "ignored");

            Assert.Equal(1, sdk.DisposeCount);
            state.VerifyAll();
        }

        [Fact]
        public async Task StateChangesMapToRegisteredStateHandlersAsync()
        {
            var sdk = new IoTEdgeTestModuleClient();
            var state = new Mock<IIoTEdgeClientState>(MockBehavior.Strict);
            state.Setup(s => s.OnOpened(1, "device", "module"));
            state.Setup(s => s.OnConnected(2, "device", "module", "again"));
            state.Setup(s => s.OnDisconnected(3, "device", "module", "lost"));
            state.Setup(s => s.OnError(4, "device", "module", "retry"));
            state.Setup(s => s.OnClosed(5, "device", "module", "closed"));
            state.Setup(s => s.OnClosed(5, "device", "module", "Disposed"));
            var client = CreateClient(sdk, state.Object);

            sdk.RaiseStateChanged(IoTHubConnectionState.Connected);
            sdk.RaiseStateChanged(IoTHubConnectionState.Connected, "again");
            sdk.RaiseStateChanged(IoTHubConnectionState.Disconnected, "lost");
            sdk.RaiseStateChanged(IoTHubConnectionState.Reconnecting, "retry");
            sdk.RaiseStateChanged(IoTHubConnectionState.Disposed, "closed");
            await client.DisposeAsync();

            state.VerifyAll();
        }

        private static IoTEdgeModuleClient CreateClient(IoTEdgeTestModuleClient sdk,
            params IIoTEdgeClientState[] handlers)
        {
            return new IoTEdgeModuleClient(
                Options.Create(new IoTEdgeClientOptions()),
                new TestIdentity(),
                handlers,
                clientFactory: new IoTEdgeTestModuleClientFactory(sdk));
        }

        private sealed class TestIdentity : IIoTEdgeDeviceIdentity
        {
            public string? Hub => "hub";
            public string DeviceId => "device";
            public string? ModuleId => "module";
            public string? Gateway => null;
        }
    }
}
