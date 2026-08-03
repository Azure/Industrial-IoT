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
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public sealed class DiscoveryControllerTests
    {
        [Fact]
        public void ConstructorRejectsMissingDependencies()
        {
            var discover = new Mock<INetworkDiscovery>().Object;
            var servers = new Mock<IServerDiscovery>().Object;

            Assert.Equal("discover", Assert.Throws<ArgumentNullException>(
                () => new DiscoveryController(null!, servers)).ParamName);
            Assert.Equal("servers", Assert.Throws<ArgumentNullException>(
                () => new DiscoveryController(discover, null!)).ParamName);
        }

        [Fact]
        public async Task FindServerDelegatesAndReturnsServiceModelAsync()
        {
            var discover = new Mock<INetworkDiscovery>(MockBehavior.Strict);
            var servers = new Mock<IServerDiscovery>(MockBehavior.Strict);
            var request = new ServerEndpointQueryModel { Url = "opc.tcp://localhost:4840" };
            var expected = new ApplicationRegistrationModel
            {
                Application = new ApplicationInfoModel
                {
                    ApplicationId = "id",
                    ApplicationUri = "urn:test"
                }
            };
            var ct = new CancellationTokenSource().Token;
            servers.Setup(s => s.FindServerAsync(request, ct))
                .ReturnsAsync(expected)
                .Verifiable();

            var actual = await new DiscoveryController(discover.Object, servers.Object)
                .FindServerAsync(request, ct);

            Assert.Same(expected, actual);
            servers.Verify();
            discover.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task RegisterReturnsTrueAfterServiceCompletesAsync()
        {
            var discover = new Mock<INetworkDiscovery>(MockBehavior.Strict);
            var servers = new Mock<IServerDiscovery>(MockBehavior.Strict);
            var request = new ServerRegistrationRequestModel
            {
                DiscoveryUrl = "opc.tcp://localhost:4840"
            };
            discover.Setup(d => d.RegisterAsync(request, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var actual = await new DiscoveryController(discover.Object, servers.Object)
                .RegisterAsync(request);

            Assert.Equal(true, actual);
            discover.Verify();
            servers.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task DiscoverReturnsTrueAfterServiceCompletesAsync()
        {
            var discover = new Mock<INetworkDiscovery>(MockBehavior.Strict);
            var servers = new Mock<IServerDiscovery>(MockBehavior.Strict);
            var request = new DiscoveryRequestModel();
            discover.Setup(d => d.DiscoverAsync(request, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var actual = await new DiscoveryController(discover.Object, servers.Object)
                .DiscoverAsync(request);

            Assert.Equal(true, actual);
            discover.Verify();
            servers.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task CancelReturnsTrueAfterServiceCompletesAsync()
        {
            var discover = new Mock<INetworkDiscovery>(MockBehavior.Strict);
            var servers = new Mock<IServerDiscovery>(MockBehavior.Strict);
            var request = new DiscoveryCancelRequestModel();
            discover.Setup(d => d.CancelAsync(request, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var actual = await new DiscoveryController(discover.Object, servers.Object)
                .CancelAsync(request);

            Assert.Equal(true, actual);
            discover.Verify();
            servers.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(nameof(DiscoveryController.FindServerAsync))]
        [InlineData(nameof(DiscoveryController.RegisterAsync))]
        [InlineData(nameof(DiscoveryController.DiscoverAsync))]
        [InlineData(nameof(DiscoveryController.CancelAsync))]
        public async Task NullRequestsAreRejectedBeforeCallingServicesAsync(string method)
        {
            var discover = new Mock<INetworkDiscovery>(MockBehavior.Strict);
            var servers = new Mock<IServerDiscovery>(MockBehavior.Strict);
            var controller = new DiscoveryController(discover.Object, servers.Object);

            await Assert.ThrowsAsync<ArgumentNullException>(() => method switch
            {
                nameof(DiscoveryController.FindServerAsync) =>
                    controller.FindServerAsync(null!),
                nameof(DiscoveryController.RegisterAsync) =>
                    controller.RegisterAsync(null!),
                nameof(DiscoveryController.DiscoverAsync) =>
                    controller.DiscoverAsync(null!),
                _ => controller.CancelAsync(null!)
            });

            discover.VerifyNoOtherCalls();
            servers.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ServiceExceptionsPropagateToRouterFilterAsync()
        {
            var discover = new Mock<INetworkDiscovery>(MockBehavior.Strict);
            var servers = new Mock<IServerDiscovery>(MockBehavior.Strict);
            var request = new DiscoveryRequestModel();
            var expected = new InvalidOperationException("discovery failed");
            discover.Setup(d => d.DiscoverAsync(request, It.IsAny<CancellationToken>()))
                .ThrowsAsync(expected);

            var actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                new DiscoveryController(discover.Object, servers.Object)
                    .DiscoverAsync(request));

            Assert.Same(expected, actual);
        }
    }
}
