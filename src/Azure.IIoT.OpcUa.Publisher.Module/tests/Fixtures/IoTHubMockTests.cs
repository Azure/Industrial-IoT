// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Fixtures
{
    using Azure.IIoT.OpcUa.Core.AzureSdk;
    using Azure.IIoT.OpcUa.Core.Exceptions;
    using Azure.IIoT.OpcUa.Core.Rpc;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using System;
    using System.Buffers;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public sealed class IoTHubMockTests
    {
        [Fact]
        public void RegistersAsRpcClient()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IoTHubMock>();
            services.AddSingleton<IIoTHubTwinServices>(
                static provider => provider.GetRequiredService<IoTHubMock>());
            services.AddSingleton<IIoTHubEventProcessor>(
                static provider => provider.GetRequiredService<IoTHubMock>());
            services.AddSingleton<IIoTHub>(
                static provider => provider.GetRequiredService<IoTHubMock>());
            services.AddSingleton<IRpcClient>(
                static provider => provider.GetRequiredService<IoTHubMock>());

            using var provider = services.BuildServiceProvider();
            var mock = provider.GetRequiredService<IoTHubMock>();
            var client = provider.GetRequiredService<IRpcClient>();

            Assert.Same(mock, client);
            Assert.Equal("IoTHub-Mock", client.Name);
            Assert.Equal(120 * 1024, client.MaxMethodPayloadSizeInBytes);
        }

        [Fact]
        public async Task DispatchesToConnectedHandlerAsync()
        {
            var mock = CreateMock();
            var connection = mock.Connect("device", "module");
            var handler = CreateHandler((_, payload, _, _) =>
                ValueTask.FromResult(payload));
            await using var registration = await connection.RpcServer.ConnectAsync(handler.Object);

            var response = await mock.CallAsync(HubResource.Format(null, "device", "module"),
                "method", new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes("request")),
                "application/json");

            Assert.Equal("request", Encoding.UTF8.GetString(response.ToArray()));
        }

        [Fact]
        public async Task TimesOutForNonCooperativeHandlerAsync()
        {
            var mock = CreateMock();
            var connection = mock.Connect("device", "module");
            var completion = new TaskCompletionSource<ReadOnlySequence<byte>>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var handler = CreateHandler((_, _, _, _) =>
                new ValueTask<ReadOnlySequence<byte>>(completion.Task));
            await using var registration = await connection.RpcServer.ConnectAsync(handler.Object);

            try
            {
                await Assert.ThrowsAsync<TimeoutException>(() => ((IRpcClient)mock).CallAsync(
                    HubResource.Format(null, "device", "module"), "method",
                    ReadOnlySequence<byte>.Empty, "application/json",
                    TimeSpan.FromMilliseconds(100)).AsTask());
            }
            finally
            {
                completion.TrySetResult(ReadOnlySequence<byte>.Empty);
            }
        }

        [Fact]
        public async Task ContinuesAfterHandlerDoesNotSupportMethodAsync()
        {
            var mock = CreateMock();
            var connection = mock.Connect("device", "module");
            var unsupported = CreateHandler((_, _, _, _) =>
                ValueTask.FromException<ReadOnlySequence<byte>>(new NotSupportedException()));
            var supported = CreateHandler((_, _, _, _) =>
                ValueTask.FromResult(new ReadOnlySequence<byte>(
                    Encoding.UTF8.GetBytes("response"))));
            await using var unsupportedRegistration =
                await connection.RpcServer.ConnectAsync(unsupported.Object);
            await using var supportedRegistration =
                await connection.RpcServer.ConnectAsync(supported.Object);

            var response = await mock.CallAsync(HubResource.Format(null, "device", "module"),
                "method", ReadOnlySequence<byte>.Empty, "application/json");

            Assert.Equal("response", Encoding.UTF8.GetString(response.ToArray()));
            unsupported.Verify(handler => handler.InvokeAsync("method",
                It.IsAny<ReadOnlySequence<byte>>(), "application/json",
                It.IsAny<CancellationToken>()), Times.Once);
            supported.Verify(handler => handler.InvokeAsync("method",
                It.IsAny<ReadOnlySequence<byte>>(), "application/json",
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task WrapsHandlerFailureAsync()
        {
            var mock = CreateMock();
            var connection = mock.Connect("device", "module");
            var handler = CreateHandler((_, _, _, _) =>
                ValueTask.FromException<ReadOnlySequence<byte>>(
                    new InvalidOperationException("boom")));
            await using var registration = await connection.RpcServer.ConnectAsync(handler.Object);

            var exception = await Assert.ThrowsAsync<MethodCallStatusException>(() =>
                mock.CallAsync(HubResource.Format(null, "device", "module"), "method",
                    ReadOnlySequence<byte>.Empty, "application/json").AsTask());

            Assert.Equal(500, exception.Status);
            Assert.Equal("boom", exception.Details.Detail);
        }

        [Fact]
        public async Task PreservesHandlerMethodCallStatusAsync()
        {
            var mock = CreateMock();
            var connection = mock.Connect("device", "module");
            var handler = CreateHandler((_, _, _, _) =>
                ValueTask.FromException<ReadOnlySequence<byte>>(
                    new MethodCallStatusException(404, "missing")));
            await using var registration = await connection.RpcServer.ConnectAsync(handler.Object);

            var exception = await Assert.ThrowsAsync<MethodCallStatusException>(() =>
                mock.CallAsync(HubResource.Format(null, "device", "module"), "method",
                    ReadOnlySequence<byte>.Empty, "application/json").AsTask());

            Assert.Equal(404, exception.Status);
            Assert.Equal("missing", exception.Details.Detail);
        }

        [Fact]
        public async Task ReportsNotSupportedWhenNoHandlerCanHandleMethodAsync()
        {
            var mock = CreateMock();
            var connection = mock.Connect("device", "module");
            var handler = CreateHandler((_, _, _, _) =>
                ValueTask.FromException<ReadOnlySequence<byte>>(new NotSupportedException()));
            await using var registration = await connection.RpcServer.ConnectAsync(handler.Object);

            var exception = await Assert.ThrowsAsync<MethodCallStatusException>(() =>
                mock.CallAsync(HubResource.Format(null, "device", "module"), "method",
                    ReadOnlySequence<byte>.Empty, "application/json").AsTask());

            Assert.Equal(500, exception.Status);
            Assert.Equal("Not supported", exception.Details.Detail);
        }

        [Fact]
        public async Task RejectsMalformedUnknownAndDisconnectedTargetsAsync()
        {
            var mock = CreateMock();

            await Assert.ThrowsAsync<ArgumentException>(() => mock.CallAsync("invalid",
                "method", ReadOnlySequence<byte>.Empty, "application/json").AsTask());
            await Assert.ThrowsAsync<ResourceNotFoundException>(() => mock.CallAsync(
                HubResource.Format(null, "unknown", "module"), "method",
                ReadOnlySequence<byte>.Empty, "application/json").AsTask());
            await Assert.ThrowsAsync<TimeoutException>(() => mock.CallAsync(
                HubResource.Format(null, "device", "module"), "method",
                ReadOnlySequence<byte>.Empty, "application/json").AsTask());
        }

        [Fact]
        public async Task TimesOutWhenCallingAfterConnectionIsClosedAsync()
        {
            var mock = CreateMock();
            var connection = mock.Connect("device", "module");
            connection.Close();

            await Assert.ThrowsAsync<TimeoutException>(() => ((IRpcClient)mock).CallAsync(
                HubResource.Format(null, "device", "module"), "method",
                ReadOnlySequence<byte>.Empty, "application/json").AsTask());
        }

        [Fact]
        public async Task RejectsDuplicateConnectionAndAllowsReconnectAfterCloseAsync()
        {
            var mock = CreateMock();
            var connection = mock.Connect("device", "module");

            Assert.Throws<InvalidOperationException>(() => mock.Connect("device", "module"));

            connection.Close();
            var reconnected = mock.Connect("device", "module");
            var handler = CreateHandler((_, _, _, _) =>
                ValueTask.FromResult(new ReadOnlySequence<byte>(
                    Encoding.UTF8.GetBytes("reconnected"))));
            await using var registration = await reconnected.RpcServer.ConnectAsync(handler.Object);
            var response = await mock.CallAsync(HubResource.Format(null, "device", "module"),
                "method", ReadOnlySequence<byte>.Empty, "application/json");

            Assert.NotSame(connection, reconnected);
            Assert.Equal("reconnected", Encoding.UTF8.GetString(response.ToArray()));
        }

        private static IoTHubMock CreateMock()
        {
            return IoTHubMock.Create(
            [
                new DeviceTwinModel
                {
                    Id = "device",
                    ModuleId = "module"
                }
            ]);
        }

        private static Mock<IRpcHandler> CreateHandler(Func<string, ReadOnlySequence<byte>,
            string, CancellationToken, ValueTask<ReadOnlySequence<byte>>> invoke)
        {
            var handler = new Mock<IRpcHandler>();
            handler.SetupGet(value => value.MountPoint).Returns("test");
            handler.Setup(value => value.InvokeAsync(It.IsAny<string>(),
                It.IsAny<ReadOnlySequence<byte>>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
                .Returns(invoke);
            return handler;
        }
    }
}
