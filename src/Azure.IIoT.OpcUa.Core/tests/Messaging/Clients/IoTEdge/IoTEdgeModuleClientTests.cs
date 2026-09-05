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
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
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

        [Fact]
        public void ConstructorDisposesReturnedSdkWhenEventSubscriptionFails()
        {
            var failure = new InvalidOperationException("Event subscription failed.");
            var sdk = CreateDisposableSdk();
            sdk.SetupAdd(s => s.ConnectionStateChanged +=
                It.IsAny<EventHandler<IoTHubConnectionStateChangedEventArgs>>())
                .Throws(failure);

            Assert.Same(failure, Assert.Throws<InvalidOperationException>(() =>
                CreateClient(sdk.Object)));

            sdk.VerifyAdd(s => s.ConnectionStateChanged +=
                It.IsAny<EventHandler<IoTHubConnectionStateChangedEventArgs>>(), Times.Once);
            sdk.Verify(s => s.DisposeAsync(), Times.Once);
            sdk.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ConcurrentDisposalsShareOneTaskAndNotifyClosedOnlyAfterSdkFinishesAsync()
        {
            const int callerCount = 32;
            var gate = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var sdk = CreateDisposableSdk();
            EventHandler<IoTHubConnectionStateChangedEventArgs>? subscribedHandler = null;
            sdk.SetupAdd(s => s.ConnectionStateChanged +=
                It.IsAny<EventHandler<IoTHubConnectionStateChangedEventArgs>>())
                .Callback<EventHandler<IoTHubConnectionStateChangedEventArgs>>(handler =>
                    subscribedHandler = handler);
            sdk.Setup(s => s.DisposeAsync()).Returns(() => new ValueTask(gate.Task));
            var state = new Mock<IIoTEdgeClientState>(MockBehavior.Strict);
            state.Setup(s => s.OnClosed(0, "device", "module", "Disposed"));
            var client = CreateClient(sdk.Object, state.Object);
            var start = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var ready = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var readyCount = 0;
            var callers = Enumerable.Range(0, callerCount).Select(_ => Task.Run(async () =>
            {
                if (Interlocked.Increment(ref readyCount) == callerCount)
                {
                    ready.SetResult();
                }
                await start.Task;
                return client.DisposeAsync().AsTask();
            })).ToArray();

            await ready.Task;
            start.SetResult();
            var disposals = await Task.WhenAll(callers);
            try
            {
                Assert.All(disposals, disposal =>
                {
                    Assert.Same(disposals[0], disposal);
                    Assert.False(disposal.IsCompleted);
                });
                sdk.VerifyRemove(s => s.ConnectionStateChanged -= subscribedHandler,
                    Times.Once);
                sdk.Verify(s => s.DisposeAsync(), Times.Once);
                state.Verify(s => s.OnClosed(0, "device", "module", "Disposed"), Times.Never);

                gate.SetResult();
                await Task.WhenAll(disposals);
                Assert.Same(disposals[0], client.DisposeAsync().AsTask());
                await client.DisposeAsync();
                sdk.Verify(s => s.DisposeAsync(), Times.Once);
                sdk.VerifyAdd(s => s.ConnectionStateChanged +=
                    It.IsAny<EventHandler<IoTHubConnectionStateChangedEventArgs>>(), Times.Once);
                sdk.VerifyRemove(s => s.ConnectionStateChanged -= subscribedHandler,
                    Times.Once);
                sdk.VerifyNoOtherCalls();
                state.Verify(s => s.OnClosed(0, "device", "module", "Disposed"), Times.Once);
                state.VerifyNoOtherCalls();
            }
            finally
            {
                gate.TrySetResult();
                await client.DisposeAsync();
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task FailedOrCanceledDisposeAsyncCachesTheTaskAndNeverRetriesSdkAsync(
            bool cancelDisposal)
        {
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            var failure = new InvalidOperationException("SDK close failed.");
            var sdk = CreateDisposableSdk();
            sdk.Setup(s => s.DisposeAsync()).Returns(() => new ValueTask(cancelDisposal
                ? Task.FromCanceled(cancellation.Token) : Task.FromException(failure)));
            var state = new Mock<IIoTEdgeClientState>(MockBehavior.Strict);
            var client = CreateClient(sdk.Object, state.Object);

            var disposal = client.DisposeAsync().AsTask();
            AssertFailure(await Record.ExceptionAsync(() => disposal));
            var repeatedDisposal = client.DisposeAsync().AsTask();
            Assert.Same(disposal, repeatedDisposal);
            AssertFailure(await Record.ExceptionAsync(() => repeatedDisposal));
            AssertFailure(await Record.ExceptionAsync(async () => await client.DisposeAsync()));
            Assert.Equal(cancelDisposal, disposal.IsCanceled);
            Assert.Equal(!cancelDisposal, disposal.IsFaulted);
            sdk.Verify(s => s.DisposeAsync(), Times.Once);
            sdk.VerifyAdd(s => s.ConnectionStateChanged +=
                It.IsAny<EventHandler<IoTHubConnectionStateChangedEventArgs>>(), Times.Once);
            sdk.VerifyRemove(s => s.ConnectionStateChanged -=
                It.IsAny<EventHandler<IoTHubConnectionStateChangedEventArgs>>(), Times.Once);
            sdk.VerifyNoOtherCalls();
            state.VerifyNoOtherCalls();

            void AssertFailure(Exception? error)
            {
                if (cancelDisposal)
                {
                    var canceled = Assert.IsAssignableFrom<OperationCanceledException>(error);
                    Assert.Equal(cancellation.Token, canceled.CancellationToken);
                }
                else
                {
                    Assert.Same(failure, error);
                }
            }
        }

        [Fact]
        public async Task EventUnsubscribeFailureStillDisposesSdkExactlyOnceAsync()
        {
            var failure = new InvalidOperationException("Event unsubscription failed.");
            var order = new List<string>();
            var sdk = CreateDisposableSdk();
            sdk.SetupRemove(s => s.ConnectionStateChanged -=
                It.IsAny<EventHandler<IoTHubConnectionStateChangedEventArgs>>())
                .Callback(() => order.Add("unsubscribe"))
                .Throws(failure);
            sdk.Setup(s => s.DisposeAsync())
                .Callback(() => order.Add("sdk"))
                .Returns(ValueTask.CompletedTask);
            var state = new Mock<IIoTEdgeClientState>(MockBehavior.Strict);
            var client = CreateClient(sdk.Object, state.Object);

            var disposal = client.DisposeAsync().AsTask();
            Assert.Same(failure, await Record.ExceptionAsync(() => disposal));
            Assert.Equal(["unsubscribe", "sdk"], order);
            var repeatedDisposal = client.DisposeAsync().AsTask();
            Assert.Same(disposal, repeatedDisposal);
            Assert.Same(failure, await Record.ExceptionAsync(() => repeatedDisposal));
            sdk.Verify(s => s.DisposeAsync(), Times.Once);
            sdk.VerifyAdd(s => s.ConnectionStateChanged +=
                It.IsAny<EventHandler<IoTHubConnectionStateChangedEventArgs>>(), Times.Once);
            sdk.VerifyRemove(s => s.ConnectionStateChanged -=
                It.IsAny<EventHandler<IoTHubConnectionStateChangedEventArgs>>(), Times.Once);
            sdk.VerifyNoOtherCalls();
            state.VerifyNoOtherCalls();
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

        private static IoTEdgeModuleClient CreateClient(IIoTHubModuleClient sdk,
            params IIoTEdgeClientState[] handlers)
        {
            var factory = new Mock<IIoTHubModuleClientFactory>(MockBehavior.Strict);
            factory.Setup(f => f.Create(It.IsAny<IoTEdgeClientOptions>(),
                It.IsAny<Action<IoTHubClientOptions>>())).Returns(sdk);
            return new IoTEdgeModuleClient(
                Options.Create(new IoTEdgeClientOptions()),
                new TestIdentity(), handlers, clientFactory: factory.Object);
        }

        private static Mock<IIoTHubModuleClient> CreateDisposableSdk()
        {
            var sdk = new Mock<IIoTHubModuleClient>(MockBehavior.Strict);
            sdk.SetupAdd(s => s.ConnectionStateChanged +=
                It.IsAny<EventHandler<IoTHubConnectionStateChangedEventArgs>>());
            sdk.SetupRemove(s => s.ConnectionStateChanged -=
                It.IsAny<EventHandler<IoTHubConnectionStateChangedEventArgs>>());
            sdk.Setup(s => s.DisposeAsync()).Returns(ValueTask.CompletedTask);
            return sdk;
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
