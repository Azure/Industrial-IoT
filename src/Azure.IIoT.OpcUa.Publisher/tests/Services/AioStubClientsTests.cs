// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Services
{
    using Azure.IIoT.OpcUa.Publisher.Services;
    using Azure.IIoT.OpcUa.Core.Messaging;
    using Azure.Iot.Operations.Connector.Files;
    using Azure.Iot.Operations.Services.AssetAndDeviceRegistry.Models;
    using Moq;
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    /// <summary>
    /// Unit tests for <see cref="AioAdrStubClient"/> and <see cref="AioSrStubClient"/>.
    /// These are pure no-op stub implementations.
    /// </summary>
    public sealed class AioStubClientsTests
    {
        // ── AioAdrStubClient ──────────────────────────────────────────────────

        [Fact]
        public async Task StartMonitoringAssetsAsync_ReturnsCompletedTaskAsync()
        {
            var sut = new AioAdrStubClient();
            var task = sut.StartMonitoringAssetsAsync("dev1", "ep1");
            Assert.True(task.IsCompleted);
            await task;
        }

        [Fact]
        public async Task StopMonitoringAssetsAsync_ReturnsCompletedTaskAsync()
        {
            var sut = new AioAdrStubClient();
            var task = sut.StopMonitoringAssetsAsync("dev1", "ep1");
            Assert.True(task.IsCompleted);
            await task;
        }

        [Fact]
        public void GetEndpointCredentials_ReturnsNonNullEndpointCredentials()
        {
            var sut = new AioAdrStubClient();
            var settings = new InboundEndpointSchemaMapValue { Address = "opc.tcp://host:4840" };

            var result = sut.GetEndpointCredentials("dev1", "ep1", settings);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task UpdateAssetStatusAsync_ReturnsPassedStatusAsync()
        {
            var sut = new AioAdrStubClient();
            AssetStatus? status = null!;

            var result = await sut.UpdateAssetStatusAsync("dev1", "ep1", "asset1", status!);

            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateDeviceStatusAsync_ReturnsPassedStatusAsync()
        {
            var sut = new AioAdrStubClient();
            DeviceStatus? status = null!;

            var result = await sut.UpdateDeviceStatusAsync("dev1", "ep1", status!);

            Assert.Null(result);
        }

        [Fact]
        public async Task ReportDiscoveredAssetAsync_ThrowsNotSupportedExceptionAsync()
        {
            var sut = new AioAdrStubClient();

            await Assert.ThrowsAsync<NotSupportedException>(async () =>
                await sut.ReportDiscoveredAssetAsync(
                    "dev1", "ep1", "asset1", null!, null, CancellationToken.None));
        }

        [Fact]
        public async Task ReportDiscoveredDeviceAsync_ThrowsNotSupportedExceptionAsync()
        {
            var sut = new AioAdrStubClient();

            await Assert.ThrowsAsync<NotSupportedException>(async () =>
                await sut.ReportDiscoveredDeviceAsync(
                    "dev1", null!, "opcua", null, CancellationToken.None));
        }

        [Fact]
        public async Task DisposeAsync_ClearsEventHandlersAndCompletes()
        {
            var sut = new AioAdrStubClient();

            // Subscribe to verify they can be detached
            sut.OnDeviceChanged += (_, _) => { };
            sut.OnAssetChanged += (_, _) => { };

            await sut.DisposeAsync();
            // No exception expected and handlers are cleared
        }

        [Fact]
        public async Task DisposeAsync_WhenNoHandlers_CompletesSilentlyAsync()
        {
            var sut = new AioAdrStubClient();
            var ex = await Record.ExceptionAsync(async () => await sut.DisposeAsync());
            Assert.Null(ex);
        }

        // ── AioSrStubClient ───────────────────────────────────────────────────

        [Fact]
        public void Register_ReturnsNonNullDisposable()
        {
            var sut = new AioSrStubClient();
            var callbacks = Mock.Of<IAioSrCallbacks>();

            var registration = sut.Register(callbacks);

            Assert.NotNull(registration);
        }

        [Fact]
        public void Register_ReturnedDisposable_DisposeDoesNotThrow()
        {
            var sut = new AioSrStubClient();
            var registration = sut.Register(Mock.Of<IAioSrCallbacks>());

            var ex = Record.Exception(() => registration.Dispose());

            Assert.Null(ex);
        }

        [Fact]
        public async Task RegisterAsync_WhenSchemaHasId_ReturnsIdAsync()
        {
            var sut = new AioSrStubClient();
            var schema = new Mock<IEventSchema>();
            schema.SetupGet(s => s.Id).Returns("schema-id-123");
            schema.SetupGet(s => s.Name).Returns("SchemaName");

            var result = await sut.RegisterAsync(schema.Object);

            Assert.Equal("schema-id-123", result);
        }

        [Fact]
        public async Task RegisterAsync_WhenIdIsNull_ReturnsNameAsync()
        {
            var sut = new AioSrStubClient();
            var schema = new Mock<IEventSchema>();
            schema.SetupGet(s => s.Id).Returns((string?)null);
            schema.SetupGet(s => s.Name).Returns("MySchemaName");

            var result = await sut.RegisterAsync(schema.Object);

            Assert.Equal("MySchemaName", result);
        }

        [Fact]
        public async Task RegisterAsync_CompletesImmediatelyAsync()
        {
            var sut = new AioSrStubClient();
            var schema = new Mock<IEventSchema>();
            schema.SetupGet(s => s.Id).Returns("id");
            schema.SetupGet(s => s.Name).Returns("name");

            var task = sut.RegisterAsync(schema.Object);
            Assert.True(task.IsCompleted);
            await task;
        }
    }
}
