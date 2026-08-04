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

    public sealed class GeneralControllerUnitTests
    {
        [Fact]
        public void ConstructorThrowsWhenEndpointsIsNull()
        {
            var certificates = new Mock<ICertificateServices<EndpointModel>>();
            var nodes = new Mock<INodeServices<ConnectionModel>>();

            var exception = Assert.Throws<ArgumentNullException>(
                () => new GeneralController(null!, certificates.Object, nodes.Object));

            Assert.Equal("endpoints", exception.ParamName);
        }

        [Fact]
        public void ConstructorThrowsWhenCertificatesIsNull()
        {
            var endpoints = new Mock<IConnectionServices<ConnectionModel>>();
            var nodes = new Mock<INodeServices<ConnectionModel>>();

            var exception = Assert.Throws<ArgumentNullException>(
                () => new GeneralController(endpoints.Object, null!, nodes.Object));

            Assert.Equal("certificates", exception.ParamName);
        }

        [Fact]
        public void ConstructorThrowsWhenNodesIsNull()
        {
            var endpoints = new Mock<IConnectionServices<ConnectionModel>>();
            var certificates = new Mock<ICertificateServices<EndpointModel>>();

            var exception = Assert.Throws<ArgumentNullException>(
                () => new GeneralController(endpoints.Object, certificates.Object, null!));

            Assert.Equal("nodes", exception.ParamName);
        }

        [Fact]
        public async Task BrowseDelegatesConnectionRequestAndCancellationAsync()
        {
            var connection = CreateConnection();
            var request = new BrowseFirstRequestModel
            {
                NodeId = "i=84"
            };
            var expected = new BrowseFirstResponseModel
            {
                Node = null!,
                References = []
            };
            using var cts = new CancellationTokenSource();
            var nodes = new Mock<INodeServices<ConnectionModel>>(MockBehavior.Strict);
            nodes.Setup(n => n.BrowseFirstAsync(connection, request, cts.Token))
                .ReturnsAsync(expected).Verifiable();
            var controller = CreateController(nodes: nodes);

            var actual = await controller.BrowseAsync(new RequestEnvelope<BrowseFirstRequestModel>
            {
                Connection = connection,
                Request = request
            }, cts.Token);

            Assert.Same(expected, actual);
            nodes.Verify();
        }

        [Fact]
        public async Task BrowsePropagatesServiceFailureAsync()
        {
            var connection = CreateConnection();
            var request = new BrowseFirstRequestModel();
            var expected = new InvalidOperationException("browse failed");
            var nodes = new Mock<INodeServices<ConnectionModel>>(MockBehavior.Strict);
            nodes.Setup(n => n.BrowseFirstAsync(connection, request, CancellationToken.None))
                .Returns(Task.FromException<BrowseFirstResponseModel>(expected));
            var controller = CreateController(nodes: nodes);

            var actual = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await controller.BrowseAsync(
                    new RequestEnvelope<BrowseFirstRequestModel>
                    {
                        Connection = connection,
                        Request = request
                    }));

            Assert.Same(expected, actual);
        }

        [Fact]
        public async Task BrowseThrowsWhenRequestEnvelopeIsNullAsync()
        {
            var controller = CreateController();

            var exception = await Assert.ThrowsAsync<ArgumentNullException>(
                async () => await controller.BrowseAsync(null!));

            Assert.Equal("request", exception.ParamName);
        }

        [Fact]
        public async Task BrowseThrowsWhenConnectionIsNullAsync()
        {
            var controller = CreateController();

            var exception = await Assert.ThrowsAsync<ArgumentNullException>(
                async () => await controller.BrowseAsync(
                    new RequestEnvelope<BrowseFirstRequestModel>
                    {
                        Connection = null!,
                        Request = new BrowseFirstRequestModel()
                    }));

            Assert.Equal("request.Connection", exception.ParamName);
        }

        [Fact]
        public async Task BrowseThrowsWhenInnerRequestIsNullAsync()
        {
            var controller = CreateController();

            var exception = await Assert.ThrowsAsync<ArgumentNullException>(
                async () => await controller.BrowseAsync(
                    new RequestEnvelope<BrowseFirstRequestModel>
                    {
                        Connection = CreateConnection(),
                        Request = null
                    }));

            Assert.Equal("request.Request", exception.ParamName);
        }

        [Fact]
        public async Task GetServerCapabilitiesAllowsNullHeaderAsync()
        {
            var connection = CreateConnection();
            var expected = new ServerCapabilitiesModel
            {
                OperationLimits = null!
            };
            var nodes = new Mock<INodeServices<ConnectionModel>>(MockBehavior.Strict);
            nodes.Setup(n => n.GetServerCapabilitiesAsync(connection, null,
                    CancellationToken.None))
                .ReturnsAsync(expected).Verifiable();
            var controller = CreateController(nodes: nodes);

            var actual = await controller.GetServerCapabilitiesAsync(
                new RequestEnvelope<RequestHeaderModel?>
                {
                    Connection = connection,
                    Request = null
                });

            Assert.Same(expected, actual);
            nodes.Verify();
        }

        [Fact]
        public async Task GetEndpointCertificateDelegatesEndpointAsync()
        {
            var endpoint = new EndpointModel
            {
                Url = "opc.tcp://localhost:4840"
            };
            var expected = new X509CertificateChainModel();
            var certificates = new Mock<ICertificateServices<EndpointModel>>(
                MockBehavior.Strict);
            certificates.Setup(c => c.GetEndpointCertificateAsync(endpoint,
                    CancellationToken.None))
                .ReturnsAsync(expected).Verifiable();
            var controller = CreateController(certificates: certificates);

            var actual = await controller.GetEndpointCertificateAsync(endpoint);

            Assert.Same(expected, actual);
            certificates.Verify();
        }

        [Fact]
        public async Task TestConnectionPropagatesEndpointFailureAsync()
        {
            var connection = CreateConnection();
            var request = new TestConnectionRequestModel();
            var expected = new InvalidOperationException("connect failed");
            var endpoints = new Mock<IConnectionServices<ConnectionModel>>(
                MockBehavior.Strict);
            endpoints.Setup(e => e.TestConnectionAsync(connection, request,
                    CancellationToken.None))
                .Returns(Task.FromException<TestConnectionResponseModel>(expected));
            var controller = CreateController(endpoints: endpoints);

            var actual = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await controller.TestConnectionAsync(
                    new RequestEnvelope<TestConnectionRequestModel>
                    {
                        Connection = connection,
                        Request = request
                    }));

            Assert.Same(expected, actual);
        }

        private static GeneralController CreateController(
            Mock<IConnectionServices<ConnectionModel>>? endpoints = null,
            Mock<ICertificateServices<EndpointModel>>? certificates = null,
            Mock<INodeServices<ConnectionModel>>? nodes = null)
        {
            endpoints ??= new Mock<IConnectionServices<ConnectionModel>>(
                MockBehavior.Strict);
            certificates ??= new Mock<ICertificateServices<EndpointModel>>(
                MockBehavior.Strict);
            nodes ??= new Mock<INodeServices<ConnectionModel>>(MockBehavior.Strict);
            return new GeneralController(endpoints.Object, certificates.Object,
                nodes.Object);
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
    }
}
