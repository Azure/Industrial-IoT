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

    public sealed class ConfigurationControllerUnitTests
    {
        [Fact]
        public async Task PublishStartDelegatesEnvelopePartsAsync()
        {
            var service = new Mock<IPublishedNodesServices>(MockBehavior.Strict);
            var controller = new ConfigurationController(service.Object);
            var request = Envelope(new PublishStartRequestModel
            {
                Item = new PublishedItemModel { NodeId = "ns=2;s=value" }
            });
            var expected = new PublishStartResponseModel();
            service.Setup(s => s.PublishStartAsync(request.Connection, request.Request!,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(expected)
                .Verifiable();

            var actual = await controller.PublishStartAsync(request);

            Assert.Same(expected, actual);
            service.Verify();
        }

        [Fact]
        public async Task PublishStopDelegatesEnvelopePartsAsync()
        {
            var service = new Mock<IPublishedNodesServices>(MockBehavior.Strict);
            var controller = new ConfigurationController(service.Object);
            var request = Envelope(new PublishStopRequestModel { NodeId = "ns=2;s=value" });
            var expected = new PublishStopResponseModel();
            service.Setup(s => s.PublishStopAsync(request.Connection, request.Request!,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(expected)
                .Verifiable();

            var actual = await controller.PublishStopAsync(request);

            Assert.Same(expected, actual);
            service.Verify();
        }

        [Fact]
        public async Task PublishBulkDelegatesEnvelopePartsAsync()
        {
            var service = new Mock<IPublishedNodesServices>(MockBehavior.Strict);
            var controller = new ConfigurationController(service.Object);
            var request = Envelope(new PublishBulkRequestModel
            {
                NodesToRemove = ["ns=2;s=old"]
            });
            var expected = new PublishBulkResponseModel();
            service.Setup(s => s.PublishBulkAsync(request.Connection, request.Request!,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(expected)
                .Verifiable();

            var actual = await controller.PublishBulkAsync(request);

            Assert.Same(expected, actual);
            service.Verify();
        }

        [Fact]
        public async Task PublishListDelegatesEnvelopePartsAsync()
        {
            var service = new Mock<IPublishedNodesServices>(MockBehavior.Strict);
            var controller = new ConfigurationController(service.Object);
            var request = Envelope(new PublishedItemListRequestModel
            {
                ContinuationToken = "token"
            });
            var expected = new PublishedItemListResponseModel();
            service.Setup(s => s.PublishListAsync(request.Connection, request.Request!,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(expected)
                .Verifiable();

            var actual = await controller.PublishListAsync(request);

            Assert.Same(expected, actual);
            service.Verify();
        }

        [Fact]
        public async Task PublishNodesReturnsEmptyResponseAfterServiceCompletesAsync()
        {
            var service = new Mock<IPublishedNodesServices>(MockBehavior.Strict);
            var controller = new ConfigurationController(service.Object);
            var entry = new PublishedNodesEntryModel
            {
                EndpointUrl = "opc.tcp://localhost:4840",
                OpcNodes = [new OpcNodeModel { Id = "ns=2;s=value" }]
            };
            service.Setup(s => s.PublishNodesAsync(entry, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var response = await controller.PublishNodesAsync(entry);

            Assert.NotNull(response);
            service.Verify();
        }

        [Fact]
        public async Task UnpublishAllNodesAllowsNullRequestAsync()
        {
            var service = new Mock<IPublishedNodesServices>(MockBehavior.Strict);
            var controller = new ConfigurationController(service.Object);
            service.Setup(s => s.UnpublishAllNodesAsync(null,
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var response = await controller.UnpublishAllNodesAsync(null);

            Assert.NotNull(response);
            service.Verify();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task GetConfiguredEndpointsMapsIncludeNodesFlagAsync(bool includeNodes)
        {
            var service = new Mock<IPublishedNodesServices>(MockBehavior.Strict);
            var controller = new ConfigurationController(service.Object);
            var endpoints = new List<PublishedNodesEntryModel>
            {
                new() { EndpointUrl = "opc.tcp://localhost:4840" }
            };
            service.Setup(s => s.GetConfiguredEndpointsAsync(includeNodes,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(endpoints)
                .Verifiable();

            var response = await controller.GetConfiguredEndpointsAsync(
                new GetConfiguredEndpointsRequestModel { IncludeNodes = includeNodes });

            Assert.Same(endpoints, response.Endpoints);
            service.Verify();
        }

        [Fact]
        public async Task SetConfiguredEndpointsConvertsNullEndpointListToEmptyListAsync()
        {
            var service = new Mock<IPublishedNodesServices>(MockBehavior.Strict);
            var controller = new ConfigurationController(service.Object);
            service.Setup(s => s.SetConfiguredEndpointsAsync(
                    It.Is<IReadOnlyList<PublishedNodesEntryModel>>(entries =>
                        entries.Count == 0),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            await controller.SetConfiguredEndpointsAsync(
                new SetConfiguredEndpointsRequestModel());

            service.Verify();
        }

        [Fact]
        public async Task GetConfiguredNodesOnEndpointWrapsNodesInResponseAsync()
        {
            var service = new Mock<IPublishedNodesServices>(MockBehavior.Strict);
            var controller = new ConfigurationController(service.Object);
            var entry = new PublishedNodesEntryModel
            {
                EndpointUrl = "opc.tcp://localhost:4840"
            };
            var nodes = new List<OpcNodeModel> { new() { Id = "ns=2;s=value" } };
            service.Setup(s => s.GetConfiguredNodesOnEndpointAsync(entry,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(nodes)
                .Verifiable();

            var response = await controller.GetConfiguredNodesOnEndpointAsync(entry);

            Assert.Same(nodes, response.OpcNodes);
            service.Verify();
        }

        [Fact]
        public async Task GetDiagnosticInfoReturnsServiceListAsync()
        {
            var service = new Mock<IPublishedNodesServices>(MockBehavior.Strict);
            var controller = new ConfigurationController(service.Object);
            var diagnostics = new List<PublishDiagnosticInfoModel>
            {
                new() { IngressValueChanges = 1 }
            };
            service.Setup(s => s.GetDiagnosticInfoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(diagnostics)
                .Verifiable();

            var actual = await controller.GetDiagnosticInfoAsync();

            Assert.Same(diagnostics, actual);
            service.Verify();
        }

        [Fact]
        public async Task ServiceExceptionsPropagateAsync()
        {
            var service = new Mock<IPublishedNodesServices>(MockBehavior.Strict);
            var controller = new ConfigurationController(service.Object);
            var entry = new PublishedNodesEntryModel
            {
                EndpointUrl = "opc.tcp://localhost:4840"
            };
            var expected = new InvalidOperationException("configuration failed");
            service.Setup(s => s.PublishNodesAsync(entry, It.IsAny<CancellationToken>()))
                .ThrowsAsync(expected);

            var actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                controller.PublishNodesAsync(entry));

            Assert.Same(expected, actual);
        }

        [Theory]
        [InlineData(nameof(ConfigurationController.PublishStartAsync), "request")]
        [InlineData(nameof(ConfigurationController.PublishStopAsync), "request")]
        [InlineData(nameof(ConfigurationController.PublishBulkAsync), "request")]
        [InlineData(nameof(ConfigurationController.PublishListAsync), "request")]
        [InlineData(nameof(ConfigurationController.PublishNodesAsync), "request")]
        [InlineData(nameof(ConfigurationController.AddOrUpdateEndpointsAsync), "request")]
        [InlineData(nameof(ConfigurationController.SetConfiguredEndpointsAsync), "request")]
        [InlineData(nameof(ConfigurationController.GetConfiguredNodesOnEndpointAsync),
            "request")]
        public async Task NullRequestsAreRejectedBeforeCallingServicesAsync(
            string method, string parameterName)
        {
            var service = new Mock<IPublishedNodesServices>(MockBehavior.Strict);
            var controller = new ConfigurationController(service.Object);

            var exception = await Assert.ThrowsAsync<ArgumentNullException>(() =>
                method switch
                {
                    nameof(ConfigurationController.PublishStartAsync) =>
                        controller.PublishStartAsync(null!),
                    nameof(ConfigurationController.PublishStopAsync) =>
                        controller.PublishStopAsync(null!),
                    nameof(ConfigurationController.PublishBulkAsync) =>
                        controller.PublishBulkAsync(null!),
                    nameof(ConfigurationController.PublishListAsync) =>
                        controller.PublishListAsync(null!),
                    nameof(ConfigurationController.PublishNodesAsync) =>
                        controller.PublishNodesAsync(null!),
                    nameof(ConfigurationController.AddOrUpdateEndpointsAsync) =>
                        controller.AddOrUpdateEndpointsAsync(null!),
                    nameof(ConfigurationController.SetConfiguredEndpointsAsync) =>
                        controller.SetConfiguredEndpointsAsync(null!),
                    _ => controller.GetConfiguredNodesOnEndpointAsync(null!)
                });

            Assert.Equal(parameterName, exception.ParamName);
            service.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(nameof(ConfigurationController.PublishStartAsync))]
        [InlineData(nameof(ConfigurationController.PublishStopAsync))]
        [InlineData(nameof(ConfigurationController.PublishBulkAsync))]
        [InlineData(nameof(ConfigurationController.PublishListAsync))]
        public async Task EnvelopeConnectionAndBodyAreRequiredAsync(string method)
        {
            var service = new Mock<IPublishedNodesServices>(MockBehavior.Strict);
            var controller = new ConfigurationController(service.Object);
            var missingConnection = new RequestEnvelope<PublishStartRequestModel>
            {
                Connection = null!,
                Request = new PublishStartRequestModel
                {
                    Item = new PublishedItemModel { NodeId = "ns=2;s=value" }
                }
            };

            var connectionException = await Assert.ThrowsAsync<ArgumentNullException>(() =>
                controller.PublishStartAsync(missingConnection));
            Assert.Equal("request.Connection", connectionException.ParamName);

            var bodyException = await Assert.ThrowsAsync<ArgumentNullException>(() =>
                method switch
                {
                    nameof(ConfigurationController.PublishStartAsync) =>
                        controller.PublishStartAsync(Envelope<PublishStartRequestModel>(null)),
                    nameof(ConfigurationController.PublishStopAsync) =>
                        controller.PublishStopAsync(Envelope<PublishStopRequestModel>(null)),
                    nameof(ConfigurationController.PublishBulkAsync) =>
                        controller.PublishBulkAsync(Envelope<PublishBulkRequestModel>(null)),
                    _ => controller.PublishListAsync(
                        Envelope<PublishedItemListRequestModel>(null))
                });
            Assert.Equal("request.Request", bodyException.ParamName);
            service.VerifyNoOtherCalls();
        }

        private static RequestEnvelope<T> Envelope<T>(T? request) where T : class
        {
            return new RequestEnvelope<T>
            {
                Connection = new ConnectionModel
                {
                    Endpoint = new EndpointModel { Url = "opc.tcp://localhost:4840" }
                },
                Request = request
            };
        }
    }
}
