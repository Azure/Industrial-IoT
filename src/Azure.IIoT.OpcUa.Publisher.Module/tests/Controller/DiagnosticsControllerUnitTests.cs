// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Controller
{
    using Azure.IIoT.OpcUa.Publisher;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Module.Controllers;
    using Moq;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public sealed class DiagnosticsControllerUnitTests
    {
        [Fact]
        public void ConstructorThrowsWhenDiagnosticsIsNull()
        {
            var publisher = new Mock<IPublisher>(MockBehavior.Strict);

            var exception = Assert.Throws<ArgumentNullException>(
                () => new DiagnosticsController(null!, publisher.Object));

            Assert.Equal("diagnostics", exception.ParamName);
        }

        [Fact]
        public void ConstructorThrowsWhenPublisherIsNull()
        {
            var diagnostics = new Mock<IClientDiagnostics>(MockBehavior.Strict);

            var exception = Assert.Throws<ArgumentNullException>(
                () => new DiagnosticsController(diagnostics.Object, null!));

            Assert.Equal("publisher", exception.ParamName);
        }

        [Fact]
        public async Task ResetAllConnectionsDelegatesToDiagnosticsAsync()
        {
            using var cts = new CancellationTokenSource();
            var diagnostics = new Mock<IClientDiagnostics>(MockBehavior.Strict);
            diagnostics.Setup(d => d.ResetAllConnectionsAsync(cts.Token))
                .Returns(Task.CompletedTask).Verifiable();
            var controller = CreateController(diagnostics: diagnostics);

            await controller.ResetAllConnectionsAsync(cts.Token);

            diagnostics.Verify();
        }

        [Fact]
        public async Task ResetAllConnectionsPropagatesDiagnosticsFailureAsync()
        {
            var expected = new InvalidOperationException("reset failed");
            var diagnostics = new Mock<IClientDiagnostics>(MockBehavior.Strict);
            diagnostics.Setup(d => d.ResetAllConnectionsAsync(CancellationToken.None))
                .Returns(Task.FromException(expected));
            var controller = CreateController(diagnostics: diagnostics);

            var actual = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await controller.ResetAllConnectionsAsync());

            Assert.Same(expected, actual);
        }

        [Fact]
        public async Task GetActiveConnectionsReturnsDiagnosticsSnapshotAsync()
        {
            var expected = new List<ConnectionModel>
            {
                CreateConnection()
            };
            var diagnostics = new Mock<IClientDiagnostics>(MockBehavior.Strict);
            diagnostics.SetupGet(d => d.ActiveConnections).Returns(expected);
            var controller = CreateController(diagnostics: diagnostics);

            var actual = await controller.GetActiveConnectionsAsync();

            Assert.Same(expected, actual);
        }

        [Fact]
        public async Task GetActiveConnectionsThrowsWhenCancellationRequestedAsync()
        {
            var diagnostics = new Mock<IClientDiagnostics>(MockBehavior.Strict);
            var controller = CreateController(diagnostics: diagnostics);
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAsync<OperationCanceledException>(
                async () => await controller.GetActiveConnectionsAsync(cts.Token));
        }

        [Fact]
        public async Task GetWriterGroupStateDelegatesToPublisherAsync()
        {
            using var cts = new CancellationTokenSource();
            var expected = new WriterGroupStateDiagnosticModel
            {
                Id = "group",
                DataSetWriters = []
            };
            var publisher = new Mock<IPublisher>(MockBehavior.Strict);
            publisher.Setup(p => p.GetStateAsync("group", cts.Token))
                .Returns(ValueTask.FromResult(expected)).Verifiable();
            var controller = CreateController(publisher: publisher);

            var actual = await controller.GetWriterGroupStateAsync("group", cts.Token);

            Assert.Same(expected, actual);
            publisher.Verify();
        }

        [Fact]
        public async Task GetWriterGroupStateThrowsWhenPublisherFailsAsync()
        {
            var expected = new InvalidOperationException("state failed");
            var publisher = new Mock<IPublisher>(MockBehavior.Strict);
            publisher.Setup(p => p.GetStateAsync("group", CancellationToken.None))
                .Returns(ValueTask.FromException<WriterGroupStateDiagnosticModel>(expected));
            var controller = CreateController(publisher: publisher);

            var actual = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await controller.GetWriterGroupStateAsync("group"));

            Assert.Same(expected, actual);
        }

        [Fact]
        public async Task GetWriterGroupStateThrowsWhenWriterGroupIsNullAsync()
        {
            var controller = CreateController();

            var exception = await Assert.ThrowsAsync<ArgumentNullException>(
                async () => await controller.GetWriterGroupStateAsync(null!));

            Assert.Equal("dataSetWriterGroup", exception.ParamName);
        }

        [Fact]
        public async Task SendDataSetWriterKeyFramePassesWriterIdsAsync()
        {
            using var cts = new CancellationTokenSource();
            var publisher = new Mock<IPublisher>(MockBehavior.Strict);
            publisher.Setup(p => p.SendKeyFrameAsync("group", "writer", cts.Token))
                .Returns(ValueTask.CompletedTask).Verifiable();
            var controller = CreateController(publisher: publisher);

            await controller.SendDataSetWriterKeyFrameAsync("group", "writer", cts.Token);

            publisher.Verify();
        }

        [Fact]
        public async Task SendWriterGroupKeyFramePassesNullDataSetWriterIdAsync()
        {
            var publisher = new Mock<IPublisher>(MockBehavior.Strict);
            publisher.Setup(p => p.SendKeyFrameAsync("group", null, CancellationToken.None))
                .Returns(ValueTask.CompletedTask).Verifiable();
            var controller = CreateController(publisher: publisher);

            await controller.SendWriterGroupKeyFrameAsync("group");

            publisher.Verify();
        }

        [Fact]
        public async Task SendDataSetWriterKeyFrameThrowsWhenWriterIdIsNullAsync()
        {
            var controller = CreateController();

            var exception = await Assert.ThrowsAsync<ArgumentNullException>(
                async () => await controller.SendDataSetWriterKeyFrameAsync("group", null!));

            Assert.Equal("dataSetWriterId", exception.ParamName);
        }

        [Fact]
        public void GetConnectionDiagnosticsReturnsDiagnosticsStream()
        {
            var expected = GetConnectionDiagnostics();
            var diagnostics = new Mock<IClientDiagnostics>(MockBehavior.Strict);
            diagnostics.Setup(d => d.GetConnectionDiagnosticsAsync(CancellationToken.None))
                .Returns(expected);
            var controller = CreateController(diagnostics: diagnostics);

            var actual = controller.GetConnectionDiagnosticsAsync();

            Assert.Same(expected, actual);
        }

        [Fact]
        public async Task GetChannelDiagnosticsThrowsWhenCancellationRequestedAsync()
        {
            var diagnostics = new Mock<IClientDiagnostics>(MockBehavior.Strict);
            var controller = CreateController(diagnostics: diagnostics);
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAsync<OperationCanceledException>(
                async () => await controller.GetChannelDiagnosticsAsync(cts.Token));
        }

        private static DiagnosticsController CreateController(
            Mock<IClientDiagnostics>? diagnostics = null,
            Mock<IPublisher>? publisher = null)
        {
            diagnostics ??= new Mock<IClientDiagnostics>(MockBehavior.Strict);
            publisher ??= new Mock<IPublisher>(MockBehavior.Strict);
            return new DiagnosticsController(diagnostics.Object, publisher.Object);
        }

        private static ConnectionModel CreateConnection()
        {
            return new ConnectionModel
            {
                Endpoint = new EndpointModel
                {
                    Url = "opc.tcp://localhost:4840"
                }
            };
        }

        private static async IAsyncEnumerable<ConnectionDiagnosticsModel>
            GetConnectionDiagnostics()
        {
            await Task.Yield();
            yield return new ConnectionDiagnosticsModel
            {
                Connection = CreateConnection()
            };
        }
    }
}
