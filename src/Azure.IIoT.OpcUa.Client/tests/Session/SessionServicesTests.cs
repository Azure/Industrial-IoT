// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.Extensions.Logging;
    using Moq;
    using Xunit;

    public class SessionServicesTests : IDisposable
    {
        private readonly Mock<ITransportChannel> _mockChannel;
        private readonly Mock<ILogger<SessionServices>> _mockLogger;
        private readonly Mock<IObservability> _mockObservability;
        private readonly SessionServices _sessionServices;

        public SessionServicesTests()
        {
            _mockObservability = new Mock<IObservability>();
            _mockChannel = new Mock<ITransportChannel>();
            _mockLogger = new Mock<ILogger<SessionServices>>();

            _mockObservability.Setup(o => o.LoggerFactory.CreateLogger(It.IsAny<string>()))
                .Returns(_mockLogger.Object);

            _sessionServices = new TestSessionServices(_mockObservability.Object, _mockChannel.Object);
        }

        [Fact]
        public async Task ActivateSessionAsyncShouldSimplyCallBaseMethodWhenNoLimitsSetAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var clientSignature = new SignatureData();
            var clientSoftwareCertificates = new SignedSoftwareCertificateCollection();
            var localeIds = new StringCollection();
            var userIdentityToken = new ExtensionObject();
            var userTokenSignature = new SignatureData();
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is ActivateSessionRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ReturnsAsync(new ActivateSessionResponse())
                .Verifiable(Times.Once);

            // Act
            var response = await _sessionServices.ActivateSessionAsync(requestHeader,
                clientSignature, clientSoftwareCertificates, localeIds,
                userIdentityToken, userTokenSignature, ct);

            // Assert
            response.Should().NotBeNull();
            requestHeader.RequestHandle.Should().NotBe(0);
            requestHeader.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            _mockChannel.Verify();
        }

        [Fact]
        public async Task ActivateSessionAsyncShouldThrowExceptionWhenResponseContainsBadStatusCodeAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var clientSignature = new SignatureData();
            var clientSoftwareCertificates = new SignedSoftwareCertificateCollection();
            var localeIds = new StringCollection();
            var userIdentityToken = new ExtensionObject();
            var userTokenSignature = new SignatureData();
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is ActivateSessionRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ReturnsAsync(new ActivateSessionResponse
                {
                    ResponseHeader = new ResponseHeader
                    {
                        ServiceResult = StatusCodes.Bad
                    }
                })
                .Verifiable(Times.Once);

            // Act
            Func<Task> act = async () => await _sessionServices.ActivateSessionAsync(requestHeader,
                clientSignature, clientSoftwareCertificates, localeIds,
                userIdentityToken, userTokenSignature, ct);

            // Assert
            await act.Should().ThrowAsync<ServiceResultException>();
            _mockChannel.Verify();
        }

        [Fact]
        public async Task ActivateSessionAsyncShouldThrowExceptionWhenSendRequestAsyncThrowsAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var clientSignature = new SignatureData();
            var clientSoftwareCertificates = new SignedSoftwareCertificateCollection();
            var localeIds = new StringCollection();
            var userIdentityToken = new ExtensionObject();
            var userTokenSignature = new SignatureData();
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is ActivateSessionRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ThrowsAsync(new IOException("Test exception"))
                .Verifiable(Times.Once);

            // Act
            Func<Task> act = async () => await _sessionServices.ActivateSessionAsync(requestHeader,
                clientSignature, clientSoftwareCertificates, localeIds,
                userIdentityToken, userTokenSignature, ct);

            // Assert
            await act.Should().ThrowAsync<IOException>().WithMessage("Test exception");
            _mockChannel.Verify();
        }

        [Fact]
        public async Task AddNodesAsyncShouldBatchRequestsWhenExceedingOperationLimitsAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var nodesToAdd = new AddNodesItemCollection(
                Enumerable.Repeat(new AddNodesItem(), 15).ToList());
            var ct = CancellationToken.None;

            _sessionServices.OperationLimits.MaxNodesPerNodeManagement = 10;

            _mockChannel
                .SetupSequence(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is AddNodesRequest),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AddNodesResponse
                {
                    Results = new AddNodesResultCollection(
                    Enumerable.Repeat(new AddNodesResult(), 10).ToList())
                })
                .ReturnsAsync(new AddNodesResponse
                {
                    Results = new AddNodesResultCollection(
                    Enumerable.Repeat(new AddNodesResult(), 5).ToList())
                });

            // Act
            var response = await _sessionServices.AddNodesAsync(requestHeader, nodesToAdd, ct);

            // Assert
            response.Should().NotBeNull();
            response.Results.Count.Should().Be(15);
            _mockChannel.Verify(c => c.SendRequestAsync(It.IsAny<IServiceRequest>(),
                It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task AddNodesAsyncShouldHandleBatchingWhenSecondOperationFailsAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var nodesToAdd = new AddNodesItemCollection(Enumerable.Repeat(new AddNodesItem(), 15).ToList());
            var ct = CancellationToken.None;

            _sessionServices.OperationLimits.MaxNodesPerNodeManagement = 10;

            _mockChannel
                .SetupSequence(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is AddNodesRequest),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AddNodesResponse
                {
                    Results = new AddNodesResultCollection(Enumerable.Repeat(new AddNodesResult(), 10).ToList())
                })
                .ReturnsAsync(new AddNodesResponse
                {
                    Results = new AddNodesResultCollection(Enumerable.Repeat(new AddNodesResult(), 5).ToList()),
                    ResponseHeader = new ResponseHeader
                    {
                        ServiceResult = StatusCodes.Bad
                    }
                });

            // Act
            Func<Task> act = async () => await _sessionServices.AddNodesAsync(requestHeader, nodesToAdd, ct);

            // Assert
            await act.Should().ThrowAsync<ServiceResultException>();
            _mockChannel.Verify(c => c.SendRequestAsync(It.IsAny<IServiceRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task AddNodesAsyncShouldSimplyCallBaseMethodWhenNoLimitsSetAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var nodesToAdd = new AddNodesItemCollection();
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is AddNodesRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ReturnsAsync(new AddNodesResponse())
                .Verifiable(Times.Once);

            // Act
            var response = await _sessionServices.AddNodesAsync(requestHeader,
                nodesToAdd, ct);

            // Assert
            response.Should().NotBeNull();
            requestHeader.RequestHandle.Should().NotBe(0);
            requestHeader.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            _mockChannel.Verify();
        }

        [Fact]
        public async Task AddNodesAsyncShouldThrowExceptionWhenResponseContainsBadStatusCodeAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var nodesToAdd = new AddNodesItemCollection();
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is AddNodesRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ReturnsAsync(new AddNodesResponse
                {
                    ResponseHeader = new ResponseHeader
                    {
                        ServiceResult = StatusCodes.Bad
                    }
                })
                .Verifiable(Times.Once);

            // Act
            Func<Task> act = async () => await _sessionServices.AddNodesAsync(requestHeader,
                nodesToAdd, ct);

            // Assert
            await act.Should().ThrowAsync<ServiceResultException>();
            _mockChannel.Verify();
        }

        [Fact]
        public async Task AddNodesAsyncShouldThrowExceptionWhenSendRequestAsyncThrowsAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var nodesToAdd = new AddNodesItemCollection();
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is AddNodesRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ThrowsAsync(new IOException("Test exception"))
                .Verifiable(Times.Once);

            // Act
            Func<Task> act = async () => await _sessionServices.AddNodesAsync(requestHeader,
                nodesToAdd, ct);

            // Assert
            await act.Should().ThrowAsync<IOException>().WithMessage("Test exception");
            _mockChannel.Verify();
        }

        [Fact]
        public async Task AddNodesAsyncShouldValidateResponseAndHandleDiagnosticInfoAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var nodesToAdd = new AddNodesItemCollection();
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is AddNodesRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ReturnsAsync(new AddNodesResponse
                {
                    Results = new AddNodesResultCollection { new AddNodesResult { StatusCode = StatusCodes.Good } },
                    DiagnosticInfos = new DiagnosticInfoCollection { new DiagnosticInfo() }
                })
                .Verifiable(Times.Once);

            // Act
            var response = await _sessionServices.AddNodesAsync(requestHeader, nodesToAdd, ct);

            // Assert
            response.Should().NotBeNull();
            response.Results.Should().HaveCount(1);
            response.DiagnosticInfos.Should().HaveCount(1);
        }

        [Fact]
        public async Task AddReferencesAsyncShouldBatchRequestsWhenExceedingOperationLimitsAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var referencesToAdd = new AddReferencesItemCollection(
                Enumerable.Repeat(new AddReferencesItem(), 15).ToList());
            var ct = CancellationToken.None;

            _sessionServices.OperationLimits.MaxNodesPerNodeManagement = 10;

            _mockChannel
                .SetupSequence(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is AddReferencesRequest),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AddReferencesResponse
                {
                    Results = new StatusCodeCollection(
                    Enumerable.Repeat((StatusCode)StatusCodes.Good, 10).ToList())
                })
                .ReturnsAsync(new AddReferencesResponse
                {
                    Results = new StatusCodeCollection(
                    Enumerable.Repeat((StatusCode)StatusCodes.Good, 5).ToList())
                });

            // Act
            var response = await _sessionServices.AddReferencesAsync(requestHeader, referencesToAdd, ct);

            // Assert
            response.Should().NotBeNull();
            response.Results.Count.Should().Be(15);
            _mockChannel.Verify(c => c.SendRequestAsync(It.IsAny<IServiceRequest>(),
                It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task AddReferencesAsyncShouldHandleBatchingWhenSecondOperationFailsAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var referencesToAdd = new AddReferencesItemCollection(Enumerable.Repeat(new AddReferencesItem(), 15).ToList());
            var ct = CancellationToken.None;

            _sessionServices.OperationLimits.MaxNodesPerNodeManagement = 10;

            _mockChannel
                .SetupSequence(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is AddReferencesRequest),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AddReferencesResponse
                {
                    Results = new StatusCodeCollection(Enumerable.Repeat((StatusCode)StatusCodes.Good, 10).ToList())
                })
                .ReturnsAsync(new AddReferencesResponse
                {
                    Results = new StatusCodeCollection(Enumerable.Repeat((StatusCode)StatusCodes.Bad, 5).ToList()),
                    ResponseHeader = new ResponseHeader
                    {
                        ServiceResult = StatusCodes.Bad
                    }
                });

            // Act
            Func<Task> act = async () => await _sessionServices.AddReferencesAsync(requestHeader, referencesToAdd, ct);

            // Assert
            await act.Should().ThrowAsync<ServiceResultException>();
            _mockChannel.Verify(c => c.SendRequestAsync(It.IsAny<IServiceRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task AddReferencesAsyncShouldSimplyCallBaseMethodWhenNoLimitsSetAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var referencesToAdd = new AddReferencesItemCollection();
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is AddReferencesRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ReturnsAsync(new AddReferencesResponse())
                .Verifiable(Times.Once);

            // Act
            var response = await _sessionServices.AddReferencesAsync(requestHeader,
                referencesToAdd, ct);

            // Assert
            response.Should().NotBeNull();
            requestHeader.RequestHandle.Should().NotBe(0);
            requestHeader.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            _mockChannel.Verify();
        }

        [Fact]
        public async Task AddReferencesAsyncShouldThrowExceptionWhenResponseContainsBadStatusCodeAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var referencesToAdd = new AddReferencesItemCollection();
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is AddReferencesRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ReturnsAsync(new AddReferencesResponse
                {
                    ResponseHeader = new ResponseHeader
                    {
                        ServiceResult = StatusCodes.Bad
                    }
                })
                .Verifiable(Times.Once);

            // Act
            Func<Task> act = async () => await _sessionServices.AddReferencesAsync(requestHeader,
                referencesToAdd, ct);

            // Assert
            await act.Should().ThrowAsync<ServiceResultException>();
            _mockChannel.Verify();
        }

        [Fact]
        public async Task AddReferencesAsyncShouldThrowExceptionWhenSendRequestAsyncThrowsAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var referencesToAdd = new AddReferencesItemCollection();
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is AddReferencesRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ThrowsAsync(new IOException("Test exception"))
                .Verifiable(Times.Once);

            // Act
            Func<Task> act = async () => await _sessionServices.AddReferencesAsync(requestHeader,
                referencesToAdd, ct);

            // Assert
            await act.Should().ThrowAsync<IOException>().WithMessage("Test exception");
            _mockChannel.Verify();
        }

        [Fact]
        public async Task AddReferencesAsyncShouldValidateResponseAndHandleDiagnosticInfoAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var referencesToAdd = new AddReferencesItemCollection();
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is AddReferencesRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ReturnsAsync(new AddReferencesResponse
                {
                    Results = new StatusCodeCollection { StatusCodes.Good },
                    DiagnosticInfos = new DiagnosticInfoCollection { new DiagnosticInfo() }
                })
                .Verifiable(Times.Once);

            // Act
            var response = await _sessionServices.AddReferencesAsync(requestHeader, referencesToAdd, ct);

            // Assert
            response.Should().NotBeNull();
            response.Results.Should().HaveCount(1);
            response.DiagnosticInfos.Should().HaveCount(1);
        }

        [Fact]
        public async Task BrowseAsyncShouldBatchRequestsWhenExceedingOperationLimitsAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var view = new ViewDescription();
            var requestedMaxReferencesPerNode = 10u;
            var nodesToBrowse = new BrowseDescriptionCollection(
                Enumerable.Repeat(new BrowseDescription(), 15).ToList());
            var ct = CancellationToken.None;

            _sessionServices.OperationLimits.MaxNodesPerBrowse = 10;

            _mockChannel
                .SetupSequence(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is BrowseRequest),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BrowseResponse
                {
                    Results = new BrowseResultCollection(
                    Enumerable.Repeat(new BrowseResult(), 10).ToList())
                })
                .ReturnsAsync(new BrowseResponse
                {
                    Results = new BrowseResultCollection(
                    Enumerable.Repeat(new BrowseResult(), 5).ToList())
                });

            // Act
            var response = await _sessionServices.BrowseAsync(requestHeader, view,
                requestedMaxReferencesPerNode, nodesToBrowse, ct);

            // Assert
            response.Should().NotBeNull();
            response.Results.Count.Should().Be(15);
            _mockChannel.Verify(c => c.SendRequestAsync(It.IsAny<IServiceRequest>(),
                It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task BrowseAsyncShouldContainTraceContextInRequestHeaderAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var view = new ViewDescription();
            var nodesToBrowse = new BrowseDescriptionCollection(
                Enumerable.Repeat(new BrowseDescription(), 5).ToList());
            var ct = CancellationToken.None;

            _sessionServices.OperationLimits.MaxNodesPerBrowse = 10;

            ActivitySource.AddActivityListener(new ActivityListener
            {
                ShouldListenTo = activitySource => activitySource.Name == "TestActivitySource",
                Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData,
                SampleUsingParentId = (ref ActivityCreationOptions<string> options) => ActivitySamplingResult.AllData
            });
            var activitySource = new ActivitySource("TestActivitySource");
            activitySource.HasListeners().Should().BeTrue();
            _mockObservability.Setup(o => o.ActivitySource).Returns(activitySource);

            using var activity = activitySource.StartActivity("TestActivity");

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is BrowseRequest),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BrowseResponse
                {
                    Results = new BrowseResultCollection(
                        Enumerable.Repeat(new BrowseResult(), 5).ToList())
                });

            // Act
            var response = await _sessionServices.BrowseAsync(requestHeader, view, 0, nodesToBrowse, ct);

            // Assert
            response.Should().NotBeNull();
            requestHeader.AdditionalHeader.Should().NotBeNull();
            var additionalParameters = requestHeader.AdditionalHeader.Body as AdditionalParametersType;
            additionalParameters.Should().NotBeNull();
            additionalParameters.Parameters.Should().Contain(p => p.Key == "traceparent");
            _mockChannel.Verify(c => c.SendRequestAsync(It.IsAny<IServiceRequest>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task BrowseAsyncShouldContainTraceContextInRequestHeaderWhenBatchedAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var view = new ViewDescription();
            var nodesToBrowse = new BrowseDescriptionCollection(
                Enumerable.Repeat(new BrowseDescription(), 15).ToList());
            var ct = CancellationToken.None;

            _sessionServices.OperationLimits.MaxNodesPerBrowse = 10;

            ActivitySource.AddActivityListener(new ActivityListener
            {
                ShouldListenTo = activitySource => activitySource.Name == "TestActivitySource",
                Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData,
                SampleUsingParentId = (ref ActivityCreationOptions<string> options) => ActivitySamplingResult.AllData
            });
            var activitySource = new ActivitySource("TestActivitySource");
            activitySource.HasListeners().Should().BeTrue();
            _mockObservability.Setup(o => o.ActivitySource).Returns(activitySource);
            _mockObservability.Setup(o => o.ActivitySource).Returns(activitySource);

            using var activity = activitySource.StartActivity("TestActivity");

            _mockChannel
                .SetupSequence(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is BrowseRequest),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BrowseResponse
                {
                    Results = new BrowseResultCollection(
                        Enumerable.Repeat(new BrowseResult(), 10).ToList())
                })
                .ReturnsAsync(new BrowseResponse
                {
                    Results = new BrowseResultCollection(
                        Enumerable.Repeat(new BrowseResult(), 5).ToList())
                });

            // Act
            var response = await _sessionServices.BrowseAsync(requestHeader, view, 0, nodesToBrowse, ct);

            // Assert
            response.Should().NotBeNull();
            requestHeader.AdditionalHeader.Should().NotBeNull();
            var additionalParameters = requestHeader.AdditionalHeader.Body as AdditionalParametersType;
            additionalParameters.Should().NotBeNull();
            additionalParameters.Parameters.Should().Contain(p => p.Key == "traceparent");
            _mockChannel.Verify(c => c.SendRequestAsync(It.IsAny<IServiceRequest>(),
                It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task BrowseAsyncShouldHandleBatchingWhenSecondOperationFailsAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var view = new ViewDescription();
            var requestedMaxReferencesPerNode = 10u;
            var nodesToBrowse = new BrowseDescriptionCollection(Enumerable.Repeat(new BrowseDescription(), 15).ToList());
            var ct = CancellationToken.None;

            _sessionServices.OperationLimits.MaxNodesPerBrowse = 10;
            _sessionServices.TraceActivityUsingLogger = true;

            _mockChannel
                .SetupSequence(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is BrowseRequest),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BrowseResponse
                {
                    Results = new BrowseResultCollection(
                        Enumerable.Repeat(new BrowseResult(), 10).ToList())
                })
                .ReturnsAsync(new BrowseResponse
                {
                    Results = new BrowseResultCollection(
                        Enumerable.Repeat(new BrowseResult(), 5).ToList()),
                    ResponseHeader = new ResponseHeader
                    {
                        ServiceResult = StatusCodes.Bad
                    }
                });

            // Act
            Func<Task> act = async () => await _sessionServices.BrowseAsync(requestHeader,
                view, requestedMaxReferencesPerNode, nodesToBrowse, ct);

            // Assert
            await act.Should().ThrowAsync<ServiceResultException>();
            _mockChannel.Verify(c => c.SendRequestAsync(It.IsAny<IServiceRequest>(),
                It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task BrowseAsyncShouldHandleDiagnosticInfosCorrectlyAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var view = new ViewDescription();
            var nodesToBrowse = new BrowseDescriptionCollection(
                Enumerable.Repeat(new BrowseDescription(), 15).ToList());
            var ct = CancellationToken.None;

            _sessionServices.OperationLimits.MaxNodesPerBrowse = 10;

            var diagnosticInfo1 = new DiagnosticInfo
            {
                SymbolicId = 1,
                NamespaceUri = 2,
                Locale = 3,
                LocalizedText = 4
            };

            var diagnosticInfo2 = new DiagnosticInfo
            {
                SymbolicId = 5,
                NamespaceUri = 6,
                Locale = 7,
                LocalizedText = 8,
                InnerDiagnosticInfo = diagnosticInfo1
            };

            _mockChannel
                .SetupSequence(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is BrowseRequest),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BrowseResponse
                {
                    Results = new BrowseResultCollection(
                        Enumerable.Repeat(new BrowseResult(), 10).ToList()),
                    DiagnosticInfos = new DiagnosticInfoCollection(
                        Enumerable.Repeat(diagnosticInfo1, 10).ToList())
                })
                .ReturnsAsync(new BrowseResponse
                {
                    Results = new BrowseResultCollection(
                        Enumerable.Repeat(new BrowseResult(), 5).ToList()),
                    DiagnosticInfos = new DiagnosticInfoCollection(
                        Enumerable.Repeat(diagnosticInfo2, 5).ToList())
                });

            // Act
            var response = await _sessionServices.BrowseAsync(requestHeader,
                view, 0, nodesToBrowse, ct);

            // Assert
            response.Should().NotBeNull();
            response.Results.Count.Should().Be(15);
            response.DiagnosticInfos.Count.Should().Be(15);
            response.DiagnosticInfos[0].SymbolicId.Should().Be(1);
            response.DiagnosticInfos[10].SymbolicId.Should().Be(5);
            response.DiagnosticInfos[10].InnerDiagnosticInfo.Should().NotBeNull();
            response.DiagnosticInfos[10].InnerDiagnosticInfo.SymbolicId.Should().Be(1);
            _mockChannel.Verify(c => c.SendRequestAsync(It.IsAny<IServiceRequest>(),
                It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task BrowseAsyncShouldHandleEmptyDiagnosticInfosCorrectlyAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var view = new ViewDescription();
            var nodesToBrowse = new BrowseDescriptionCollection(
                Enumerable.Repeat(new BrowseDescription(), 15).ToList());
            var ct = CancellationToken.None;

            _sessionServices.OperationLimits.MaxNodesPerBrowse = 10;

            _mockChannel
                .SetupSequence(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is BrowseRequest),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BrowseResponse
                {
                    Results = new BrowseResultCollection(
                        Enumerable.Repeat(new BrowseResult(), 10).ToList()),
                    DiagnosticInfos = new DiagnosticInfoCollection()
                })
                .ReturnsAsync(new BrowseResponse
                {
                    Results = new BrowseResultCollection(
                        Enumerable.Repeat(new BrowseResult(), 5).ToList()),
                    DiagnosticInfos = new DiagnosticInfoCollection()
                });

            // Act
            var response = await _sessionServices.BrowseAsync(requestHeader,
                view, 0, nodesToBrowse, ct);

            // Assert
            response.Should().NotBeNull();
            response.Results.Count.Should().Be(15);
            response.DiagnosticInfos.Count.Should().Be(0);
            _mockChannel.Verify(c => c.SendRequestAsync(It.IsAny<IServiceRequest>(),
                It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task BrowseAsyncShouldHandleEmptyStringTablesInDiagnosticInfos()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var view = new ViewDescription();
            var nodesToBrowse = new BrowseDescriptionCollection(
                Enumerable.Repeat(new BrowseDescription(), 15).ToList());
            var ct = CancellationToken.None;

            _sessionServices.OperationLimits.MaxNodesPerBrowse = 10;

            var diagnosticInfo1 = new DiagnosticInfo
            {
                SymbolicId = 1,
                NamespaceUri = 2,
                Locale = 3,
                LocalizedText = 4
            };

            _mockChannel
                .SetupSequence(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is BrowseRequest),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BrowseResponse
                {
                    Results = new BrowseResultCollection(
                        Enumerable.Repeat(new BrowseResult(), 10).ToList()),
                    DiagnosticInfos = new DiagnosticInfoCollection(
                        Enumerable.Repeat(diagnosticInfo1, 10).ToList()),
                    ResponseHeader = new ResponseHeader
                    {
                        StringTable = new StringCollection()
                    }
                })
                .ReturnsAsync(new BrowseResponse
                {
                    Results = new BrowseResultCollection(
                        Enumerable.Repeat(new BrowseResult(), 5).ToList()),
                    DiagnosticInfos = new DiagnosticInfoCollection(
                        Enumerable.Repeat(diagnosticInfo1, 5).ToList()),
                    ResponseHeader = new ResponseHeader
                    {
                        StringTable = new StringCollection()
                    }
                });

            // Act
            var response = await _sessionServices.BrowseAsync(requestHeader, view, 0, nodesToBrowse, ct);

            // Assert
            response.Should().NotBeNull();
            response.Results.Count.Should().Be(15);
            response.DiagnosticInfos.Count.Should().Be(15);
            response.ResponseHeader.StringTable.Count.Should().Be(0);

            // Verify that the indexes in the diagnostic infos are correctly updated
            response.DiagnosticInfos[0].SymbolicId.Should().Be(1);
            response.DiagnosticInfos[0].NamespaceUri.Should().Be(2);
            response.DiagnosticInfos[0].Locale.Should().Be(3);
            response.DiagnosticInfos[0].LocalizedText.Should().Be(4);

            _mockChannel.Verify(c => c.SendRequestAsync(It.IsAny<IServiceRequest>(),
                It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task BrowseAsyncShouldHandleMixedDiagnosticInfosCorrectlyAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var view = new ViewDescription();
            var nodesToBrowse = new BrowseDescriptionCollection(
                Enumerable.Repeat(new BrowseDescription(), 15).ToList());
            var ct = CancellationToken.None;

            _sessionServices.OperationLimits.MaxNodesPerBrowse = 10;

            var diagnosticInfo1 = new DiagnosticInfo
            {
                SymbolicId = 1,
                NamespaceUri = 2,
                Locale = 3,
                LocalizedText = 4
            };

            _mockChannel
                .SetupSequence(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is BrowseRequest),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BrowseResponse
                {
                    Results = new BrowseResultCollection(
                        Enumerable.Repeat(new BrowseResult(), 10).ToList()),
                    DiagnosticInfos = new DiagnosticInfoCollection(
                        Enumerable.Repeat(diagnosticInfo1, 10).ToList())
                })
                .ReturnsAsync(new BrowseResponse
                {
                    Results = new BrowseResultCollection(
                        Enumerable.Repeat(new BrowseResult(), 5).ToList()),
                    DiagnosticInfos = new DiagnosticInfoCollection()
                });

            // Act
            var response = await _sessionServices.BrowseAsync(requestHeader, view, 0, nodesToBrowse, ct);

            // Assert
            response.Should().NotBeNull();
            response.Results.Count.Should().Be(15);
            response.DiagnosticInfos.Count.Should().Be(15);
            response.DiagnosticInfos[0].SymbolicId.Should().Be(1);
            _mockChannel.Verify(c => c.SendRequestAsync(It.IsAny<IServiceRequest>(),
                It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task BrowseAsyncShouldRecombineStringTablesInDiagnosticInfos1Async()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var view = new ViewDescription();
            var nodesToBrowse = new BrowseDescriptionCollection(
                Enumerable.Range(0, 15).Select(_ => new BrowseDescription()));
            var ct = CancellationToken.None;

            _sessionServices.OperationLimits.MaxNodesPerBrowse = 10;

            static DiagnosticInfo diagnosticInfo() => new()
            {
                SymbolicId = 1,
                NamespaceUri = 2,
                Locale = 3,
                LocalizedText = 4
            };

            _mockChannel
                .SetupSequence(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is BrowseRequest),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BrowseResponse
                {
                    Results = new BrowseResultCollection(
                        Enumerable.Range(0, 10).Select(_ => new BrowseResult())),
                    DiagnosticInfos = new DiagnosticInfoCollection(
                        Enumerable.Range(0, 10).Select(_ => diagnosticInfo())),
                    ResponseHeader = new ResponseHeader
                    {
                        StringTable = new StringCollection { "String1", "String2", "String3", "String4" }
                    }
                })
                .ReturnsAsync(new BrowseResponse
                {
                    Results = new BrowseResultCollection(
                        Enumerable.Range(0, 5).Select(_ => new BrowseResult())),
                    DiagnosticInfos = new DiagnosticInfoCollection(
                        Enumerable.Range(0, 5).Select(_ => diagnosticInfo())),
                    ResponseHeader = new ResponseHeader
                    {
                        StringTable = new StringCollection { "String5", "String6", "String7", "String8" }
                    }
                });

            // Act
            var response = await _sessionServices.BrowseAsync(requestHeader, view, 0, nodesToBrowse, ct);

            // Assert
            response.Should().NotBeNull();
            response.Results.Count.Should().Be(15);
            response.DiagnosticInfos.Count.Should().Be(15);
            response.ResponseHeader.StringTable.Count.Should().Be(8);
            response.ResponseHeader.StringTable.Should()
                .ContainInOrder("String1", "String2", "String3", "String4", "String5", "String6", "String7", "String8");

            // Verify that the indexes in the diagnostic infos are correctly updated
            response.DiagnosticInfos[0].SymbolicId.Should().Be(1);
            response.DiagnosticInfos[0].NamespaceUri.Should().Be(2);
            response.DiagnosticInfos[0].Locale.Should().Be(3);
            response.DiagnosticInfos[0].LocalizedText.Should().Be(4);

            response.DiagnosticInfos[10].SymbolicId.Should().Be(5);
            response.DiagnosticInfos[10].NamespaceUri.Should().Be(6);
            response.DiagnosticInfos[10].Locale.Should().Be(7);
            response.DiagnosticInfos[10].LocalizedText.Should().Be(8);

            _mockChannel.Verify(c => c.SendRequestAsync(It.IsAny<IServiceRequest>(),
                It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task BrowseAsyncShouldRecombineStringTablesInDiagnosticInfos2Async()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var view = new ViewDescription();
            var nodesToBrowse = new BrowseDescriptionCollection(
                Enumerable.Range(0, 15).Select(_ => new BrowseDescription()));
            var ct = CancellationToken.None;

            _sessionServices.OperationLimits.MaxNodesPerBrowse = 10;

            DiagnosticInfo diagnosticInfo1() => new()
            {
                SymbolicId = 1,
                NamespaceUri = 2,
                Locale = 3,
                LocalizedText = 4
            };

            DiagnosticInfo diagnosticInfo2() => new()
            {
                SymbolicId = 1,
                NamespaceUri = 2,
                Locale = 3,
                LocalizedText = 4
            };

            _mockChannel
                .SetupSequence(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is BrowseRequest),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BrowseResponse
                {
                    Results = new BrowseResultCollection(
                        Enumerable.Range(0, 10).Select(_ => new BrowseResult())),
                    DiagnosticInfos = new DiagnosticInfoCollection(
                        Enumerable.Range(0, 10).Select(_ => diagnosticInfo1())),
                    ResponseHeader = new ResponseHeader
                    {
                        StringTable = new StringCollection { "String1", "String2", "String3", "String4" }
                    }
                })
                .ReturnsAsync(new BrowseResponse
                {
                    Results = new BrowseResultCollection(
                        Enumerable.Range(0, 5).Select(_ => new BrowseResult())),
                    DiagnosticInfos = new DiagnosticInfoCollection(
                        Enumerable.Range(0, 5).Select(_ => diagnosticInfo2())),
                    ResponseHeader = new ResponseHeader
                    {
                        StringTable = new StringCollection { "String5", "String6", "String7", "String8" }
                    }
                });

            // Act
            var response = await _sessionServices.BrowseAsync(requestHeader, view, 0, nodesToBrowse, ct);

            // Assert
            response.Should().NotBeNull();
            response.Results.Count.Should().Be(15);
            response.DiagnosticInfos.Count.Should().Be(15);
            response.ResponseHeader.StringTable.Count.Should().Be(8);
            response.ResponseHeader.StringTable.Should()
                .ContainInOrder("String1", "String2", "String3", "String4", "String5", "String6", "String7", "String8");

            // Verify that the indexes in the diagnostic infos are correctly updated
            response.DiagnosticInfos[0].SymbolicId.Should().Be(1);
            response.DiagnosticInfos[0].NamespaceUri.Should().Be(2);
            response.DiagnosticInfos[0].Locale.Should().Be(3);
            response.DiagnosticInfos[0].LocalizedText.Should().Be(4);

            response.DiagnosticInfos[10].SymbolicId.Should().Be(5);
            response.DiagnosticInfos[10].NamespaceUri.Should().Be(6);
            response.DiagnosticInfos[10].Locale.Should().Be(7);
            response.DiagnosticInfos[10].LocalizedText.Should().Be(8);

            _mockChannel.Verify(c => c.SendRequestAsync(It.IsAny<IServiceRequest>(),
                It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task BrowseAsyncShouldRecombineStringTablesInDiagnosticInfos3Async()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var view = new ViewDescription();
            var nodesToBrowse = new BrowseDescriptionCollection(
                Enumerable.Range(0, 15).Select(_ => new BrowseDescription()));
            var ct = CancellationToken.None;

            _sessionServices.OperationLimits.MaxNodesPerBrowse = 10;

            DiagnosticInfo diagnosticInfo1() => new()
            {
                SymbolicId = 1,
                NamespaceUri = 2,
                Locale = 1,
                LocalizedText = 2
            };

            DiagnosticInfo diagnosticInfo2() => new()
            {
                SymbolicId = 1,
                NamespaceUri = 2,
                Locale = 3,
                LocalizedText = 4,
                InnerDiagnosticInfo = diagnosticInfo1()
            };

            _mockChannel
                .SetupSequence(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is BrowseRequest),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BrowseResponse
                {
                    Results = new BrowseResultCollection(
                        Enumerable.Range(0, 10).Select(_ => new BrowseResult())),
                    DiagnosticInfos = new DiagnosticInfoCollection(
                        Enumerable.Range(0, 10).Select(_ => diagnosticInfo1())),
                    ResponseHeader = new ResponseHeader
                    {
                        StringTable = new StringCollection { "String1", "String2" }
                    }
                })
                .ReturnsAsync(new BrowseResponse
                {
                    Results = new BrowseResultCollection(
                        Enumerable.Range(0, 5).Select(_ => new BrowseResult())),
                    DiagnosticInfos = new DiagnosticInfoCollection(
                        Enumerable.Range(0, 5).Select(_ => diagnosticInfo2())),
                    ResponseHeader = new ResponseHeader
                    {
                        StringTable = new StringCollection { "String1", "String2", "String3", "String4" }
                    }
                });

            // Act
            var response = await _sessionServices.BrowseAsync(requestHeader, view, 0, nodesToBrowse, ct);

            // Assert
            response.Should().NotBeNull();
            response.Results.Count.Should().Be(15);
            response.DiagnosticInfos.Count.Should().Be(15);
            response.ResponseHeader.StringTable.Count.Should().Be(6);
            response.ResponseHeader.StringTable.Should().ContainInOrder("String1", "String2", "String1", "String2", "String3", "String4");

            // Verify that the indexes in the diagnostic infos are correctly updated
            response.DiagnosticInfos[0].SymbolicId.Should().Be(1);
            response.DiagnosticInfos[0].NamespaceUri.Should().Be(2);
            response.DiagnosticInfos[0].Locale.Should().Be(1);
            response.DiagnosticInfos[0].LocalizedText.Should().Be(2);

            response.DiagnosticInfos[10].SymbolicId.Should().Be(3);
            response.DiagnosticInfos[10].NamespaceUri.Should().Be(4);
            response.DiagnosticInfos[10].Locale.Should().Be(5);
            response.DiagnosticInfos[10].LocalizedText.Should().Be(6);
            response.DiagnosticInfos[10].InnerDiagnosticInfo.Should().NotBeNull();
            response.DiagnosticInfos[10].InnerDiagnosticInfo.SymbolicId.Should().Be(3);
            response.DiagnosticInfos[10].InnerDiagnosticInfo.NamespaceUri.Should().Be(4);
            response.DiagnosticInfos[10].InnerDiagnosticInfo.Locale.Should().Be(3);
            response.DiagnosticInfos[10].InnerDiagnosticInfo.LocalizedText.Should().Be(4);

            _mockChannel.Verify(c => c.SendRequestAsync(It.IsAny<IServiceRequest>(),
                It.IsAny<CancellationToken>()), Times.Exactly(2));
        }
        [Fact]
        public async Task BrowseAsyncShouldSimplyCallBaseMethodWhenNoLimitsSetAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var view = new ViewDescription();
            var requestedMaxReferencesPerNode = 10u;
            var nodesToBrowse = new BrowseDescriptionCollection();
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is BrowseRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ReturnsAsync(new BrowseResponse())
                .Verifiable(Times.Once);

            // Act
            var response = await _sessionServices.BrowseAsync(requestHeader, view,
                requestedMaxReferencesPerNode, nodesToBrowse, ct);

            // Assert
            response.Should().NotBeNull();
            requestHeader.RequestHandle.Should().NotBe(0);
            requestHeader.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            _mockChannel.Verify();
        }

        [Fact]
        public async Task BrowseAsyncShouldThrowExceptionWhenResponseContainsBadStatusCodeAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var view = new ViewDescription();
            var requestedMaxReferencesPerNode = 10u;
            var nodesToBrowse = new BrowseDescriptionCollection();
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is BrowseRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ReturnsAsync(new BrowseResponse
                {
                    ResponseHeader = new ResponseHeader
                    {
                        ServiceResult = StatusCodes.Bad
                    }
                })
                .Verifiable(Times.Once);

            // Act
            Func<Task> act = async () => await _sessionServices.BrowseAsync(requestHeader, view,
                requestedMaxReferencesPerNode, nodesToBrowse, ct);

            // Assert
            await act.Should().ThrowAsync<ServiceResultException>();
            _mockChannel.Verify();
        }

        [Fact]
        public async Task BrowseAsyncShouldThrowExceptionWhenSendRequestAsyncThrowsAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var view = new ViewDescription();
            var requestedMaxReferencesPerNode = 10u;
            var nodesToBrowse = new BrowseDescriptionCollection();
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is BrowseRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ThrowsAsync(new IOException("Test exception"))
                .Verifiable(Times.Once);

            // Act
            Func<Task> act = async () => await _sessionServices.BrowseAsync(requestHeader, view,
                requestedMaxReferencesPerNode, nodesToBrowse, ct);

            // Assert
            await act.Should().ThrowAsync<IOException>().WithMessage("Test exception");
            _mockChannel.Verify();
        }

        [Fact]
        public async Task BrowseAsyncShouldValidateResponseAndHandleDiagnosticInfoAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var view = new ViewDescription();
            var requestedMaxReferencesPerNode = 10u;
            var nodesToBrowse = new BrowseDescriptionCollection();
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is BrowseRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ReturnsAsync(new BrowseResponse
                {
                    Results = new BrowseResultCollection { new BrowseResult { StatusCode = StatusCodes.Good } },
                    DiagnosticInfos = new DiagnosticInfoCollection { new DiagnosticInfo() }
                })
                .Verifiable(Times.Once);

            // Act
            var response = await _sessionServices.BrowseAsync(requestHeader, view, requestedMaxReferencesPerNode, nodesToBrowse, ct);

            // Assert
            response.Should().NotBeNull();
            response.Results.Should().HaveCount(1);
            response.DiagnosticInfos.Should().HaveCount(1);
        }

        [Fact]
        public async Task BrowseNextAsyncShouldBatchRequestsWhenExceedingOperationLimitsAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var releaseContinuationPoints = true;
            var continuationPoints = new ByteStringCollection(
                Enumerable.Repeat(new byte[0], 15).ToList());
            var ct = CancellationToken.None;

            _sessionServices.OperationLimits.MaxBrowseContinuationPoints = 10;

            _mockChannel
                .SetupSequence(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is BrowseNextRequest),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BrowseNextResponse
                {
                    Results = new BrowseResultCollection(
                        Enumerable.Repeat(new BrowseResult(), 10).ToList())
                })
                .ReturnsAsync(new BrowseNextResponse
                {
                    Results = new BrowseResultCollection(
                        Enumerable.Repeat(new BrowseResult(), 5).ToList())
                });

            // Act
            var response = await _sessionServices.BrowseNextAsync(requestHeader, releaseContinuationPoints, continuationPoints, ct);

            // Assert
            response.Should().NotBeNull();
            response.Results.Count.Should().Be(15);
            _mockChannel.Verify(c => c.SendRequestAsync(It.IsAny<IServiceRequest>(),
                It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task BrowseNextAsyncShouldHandleBatchingWhenSecondOperationFailsAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var releaseContinuationPoints = true;
            var continuationPoints = new ByteStringCollection(Enumerable.Repeat(new byte[0], 15).ToList());
            var ct = CancellationToken.None;

            _sessionServices.OperationLimits.MaxBrowseContinuationPoints = 10;

            _mockChannel
                .SetupSequence(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is BrowseNextRequest),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BrowseNextResponse
                {
                    Results = new BrowseResultCollection(Enumerable.Repeat(new BrowseResult(), 10).ToList())
                })
                .ReturnsAsync(new BrowseNextResponse
                {
                    Results = new BrowseResultCollection(Enumerable.Repeat(new BrowseResult(), 5).ToList()),
                    ResponseHeader = new ResponseHeader
                    {
                        ServiceResult = StatusCodes.Bad
                    }
                });

            // Act
            Func<Task> act = async () => await _sessionServices.BrowseNextAsync(requestHeader, releaseContinuationPoints, continuationPoints, ct);

            // Assert
            await act.Should().ThrowAsync<ServiceResultException>();
            _mockChannel.Verify(c => c.SendRequestAsync(It.IsAny<IServiceRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task BrowseNextAsyncShouldSimplyCallBaseMethodWhenNoLimitsSetAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var releaseContinuationPoints = true;
            var continuationPoints = new ByteStringCollection();
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is BrowseNextRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ReturnsAsync(new BrowseNextResponse())
                .Verifiable(Times.Once);

            // Act
            var response = await _sessionServices.BrowseNextAsync(requestHeader,
                releaseContinuationPoints, continuationPoints, ct);

            // Assert
            response.Should().NotBeNull();
            requestHeader.RequestHandle.Should().NotBe(0);
            requestHeader.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            _mockChannel.Verify();
        }

        [Fact]
        public async Task BrowseNextAsyncShouldThrowExceptionWhenResponseContainsBadStatusCodeAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var releaseContinuationPoints = true;
            var continuationPoints = new ByteStringCollection();
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is BrowseNextRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ReturnsAsync(new BrowseNextResponse
                {
                    ResponseHeader = new ResponseHeader
                    {
                        ServiceResult = StatusCodes.Bad
                    }
                })
                .Verifiable(Times.Once);

            // Act
            Func<Task> act = async () => await _sessionServices.BrowseNextAsync(requestHeader,
                releaseContinuationPoints, continuationPoints, ct);

            // Assert
            await act.Should().ThrowAsync<ServiceResultException>();
            _mockChannel.Verify();
        }

        [Fact]
        public async Task BrowseNextAsyncShouldThrowExceptionWhenSendRequestAsyncThrowsAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var releaseContinuationPoints = true;
            var continuationPoints = new ByteStringCollection();
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is BrowseNextRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ThrowsAsync(new IOException("Test exception"))
                .Verifiable(Times.Once);

            // Act
            Func<Task> act = async () => await _sessionServices.BrowseNextAsync(requestHeader,
                releaseContinuationPoints, continuationPoints, ct);

            // Assert
            await act.Should().ThrowAsync<IOException>().WithMessage("Test exception");
            _mockChannel.Verify();
        }

        [Fact]
        public async Task BrowseNextAsyncShouldValidateResponseAndHandleDiagnosticInfoAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var releaseContinuationPoints = true;
            var continuationPoints = new ByteStringCollection();
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is BrowseNextRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ReturnsAsync(new BrowseNextResponse
                {
                    Results = new BrowseResultCollection { new BrowseResult { StatusCode = StatusCodes.Good } },
                    DiagnosticInfos = new DiagnosticInfoCollection { new DiagnosticInfo() }
                })
                .Verifiable(Times.Once);

            // Act
            var response = await _sessionServices.BrowseNextAsync(requestHeader, releaseContinuationPoints, continuationPoints, ct);

            // Assert
            response.Should().NotBeNull();
            response.Results.Should().HaveCount(1);
            response.DiagnosticInfos.Should().HaveCount(1);
        }

        [Fact]
        public async Task CallAsyncShouldBatchRequestsWhenExceedingOperationLimitsAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var methodsToCall = new CallMethodRequestCollection(
                Enumerable.Repeat(new CallMethodRequest(), 15).ToList());
            var ct = CancellationToken.None;

            _sessionServices.OperationLimits.MaxNodesPerMethodCall = 10;

            _mockChannel
                .SetupSequence(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is CallRequest),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CallResponse
                {
                    Results = new CallMethodResultCollection(
                    Enumerable.Repeat(new CallMethodResult(), 10).ToList())
                })
                .ReturnsAsync(new CallResponse
                {
                    Results = new CallMethodResultCollection(
                    Enumerable.Repeat(new CallMethodResult(), 5).ToList())
                });

            // Act
            var response = await _sessionServices.CallAsync(requestHeader, methodsToCall, ct);

            // Assert
            response.Should().NotBeNull();
            response.Results.Count.Should().Be(15);
            _mockChannel.Verify(c => c.SendRequestAsync(It.IsAny<IServiceRequest>(),
                It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task CallAsyncShouldHandleBatchingWhenSecondOperationFailsAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var methodsToCall = new CallMethodRequestCollection(Enumerable.Repeat(new CallMethodRequest(), 15).ToList());
            var ct = CancellationToken.None;

            _sessionServices.OperationLimits.MaxNodesPerMethodCall = 10;

            _mockChannel
                .SetupSequence(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is CallRequest),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CallResponse
                {
                    Results = new CallMethodResultCollection(Enumerable.Repeat(new CallMethodResult(), 10).ToList())
                })
                .ReturnsAsync(new CallResponse
                {
                    Results = new CallMethodResultCollection(Enumerable.Repeat(new CallMethodResult(), 5).ToList()),
                    ResponseHeader = new ResponseHeader
                    {
                        ServiceResult = StatusCodes.Bad
                    }
                });

            // Act
            Func<Task> act = async () => await _sessionServices.CallAsync(requestHeader, methodsToCall, ct);

            // Assert
            await act.Should().ThrowAsync<ServiceResultException>();
            _mockChannel.Verify(c => c.SendRequestAsync(It.IsAny<IServiceRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task CallAsyncShouldSimplyCallBaseMethodWhenNoLimitsSetAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var methodsToCall = new CallMethodRequestCollection();
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is CallRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ReturnsAsync(new CallResponse())
                .Verifiable(Times.Once);

            // Act
            var response = await _sessionServices.CallAsync(requestHeader,
                methodsToCall, ct);

            // Assert
            response.Should().NotBeNull();
            requestHeader.RequestHandle.Should().NotBe(0);
            requestHeader.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            _mockChannel.Verify();
        }

        [Fact]
        public async Task CallAsyncShouldThrowExceptionWhenResponseContainsBadStatusCodeAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var methodsToCall = new CallMethodRequestCollection();
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is CallRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ReturnsAsync(new CallResponse
                {
                    ResponseHeader = new ResponseHeader
                    {
                        ServiceResult = StatusCodes.Bad
                    }
                })
                .Verifiable(Times.Once);

            // Act
            Func<Task> act = async () => await _sessionServices.CallAsync(requestHeader,
                methodsToCall, ct);

            // Assert
            await act.Should().ThrowAsync<ServiceResultException>();
            _mockChannel.Verify();
        }

        [Fact]
        public async Task CallAsyncShouldThrowExceptionWhenSendRequestAsyncThrowsAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var methodsToCall = new CallMethodRequestCollection();
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is CallRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ThrowsAsync(new IOException("Test exception"))
                .Verifiable(Times.Once);

            // Act
            Func<Task> act = async () => await _sessionServices.CallAsync(requestHeader,
                methodsToCall, ct);

            // Assert
            await act.Should().ThrowAsync<IOException>().WithMessage("Test exception");
            _mockChannel.Verify();
        }

        [Fact]
        public async Task CallAsyncShouldValidateResponseAndHandleDiagnosticInfoAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var methodsToCall = new CallMethodRequestCollection();
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is CallRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ReturnsAsync(new CallResponse
                {
                    Results = new CallMethodResultCollection { new CallMethodResult { StatusCode = StatusCodes.Good } },
                    DiagnosticInfos = new DiagnosticInfoCollection { new DiagnosticInfo() }
                })
                .Verifiable(Times.Once);

            // Act
            var response = await _sessionServices.CallAsync(requestHeader, methodsToCall, ct);

            // Assert
            response.Should().NotBeNull();
            response.Results.Should().HaveCount(1);
            response.DiagnosticInfos.Should().HaveCount(1);
        }

        [Fact]
        public async Task CancelAsyncShouldSimplyCallBaseMethodWhenNoLimitsSetAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var requestHandle = 1u;
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is CancelRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ReturnsAsync(new CancelResponse())
                .Verifiable(Times.Once);

            // Act
            var response = await _sessionServices.CancelAsync(requestHeader,
                requestHandle, ct);

            // Assert
            response.Should().NotBeNull();
            requestHeader.RequestHandle.Should().NotBe(0);
            requestHeader.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            _mockChannel.Verify();
        }

        [Fact]
        public async Task CancelAsyncShouldThrowExceptionWhenResponseContainsBadStatusCodeAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var requestHandle = 1u;
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is CancelRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ReturnsAsync(new CancelResponse
                {
                    ResponseHeader = new ResponseHeader
                    {
                        ServiceResult = StatusCodes.Bad
                    }
                })
                .Verifiable(Times.Once);

            // Act
            Func<Task> act = async () => await _sessionServices.CancelAsync(requestHeader,
                requestHandle, ct);

            // Assert
            await act.Should().ThrowAsync<ServiceResultException>();
            _mockChannel.Verify();
        }

        [Fact]
        public async Task CancelAsyncShouldThrowExceptionWhenSendRequestAsyncThrowsAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var requestHandle = 1u;
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is CancelRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ThrowsAsync(new IOException("Test exception"))
                .Verifiable(Times.Once);

            // Act
            Func<Task> act = async () => await _sessionServices.CancelAsync(requestHeader,
                requestHandle, ct);

            // Assert
            await act.Should().ThrowAsync<IOException>().WithMessage("Test exception");
            _mockChannel.Verify();
        }

        [Fact]
        public async Task CloseSessionAsyncShouldSimplyCallBaseMethodWhenNoLimitsSetAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var deleteSubscriptions = true;
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is CloseSessionRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ReturnsAsync(new CloseSessionResponse())
                .Verifiable(Times.Once);

            // Act
            var response = await _sessionServices.CloseSessionAsync(requestHeader,
                deleteSubscriptions, ct);

            // Assert
            response.Should().NotBeNull();
            requestHeader.RequestHandle.Should().NotBe(0);
            requestHeader.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            _mockChannel.Verify();
        }

        [Fact]
        public async Task CloseSessionAsyncShouldThrowExceptionWhenResponseContainsBadStatusCodeAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var deleteSubscriptions = true;
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is CloseSessionRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ReturnsAsync(new CloseSessionResponse
                {
                    ResponseHeader = new ResponseHeader
                    {
                        ServiceResult = StatusCodes.Bad
                    }
                })
                .Verifiable(Times.Once);

            // Act
            Func<Task> act = async () => await _sessionServices.CloseSessionAsync(requestHeader,
                deleteSubscriptions, ct);

            // Assert
            await act.Should().ThrowAsync<ServiceResultException>();
            _mockChannel.Verify();
        }

        [Fact]
        public async Task CloseSessionAsyncShouldThrowExceptionWhenSendRequestAsyncThrowsAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var deleteSubscriptions = true;
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is CloseSessionRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ThrowsAsync(new IOException("Test exception"))
                .Verifiable(Times.Once);

            // Act
            Func<Task> act = async () => await _sessionServices.CloseSessionAsync(requestHeader,
                deleteSubscriptions, ct);

            // Assert
            await act.Should().ThrowAsync<IOException>().WithMessage("Test exception");
            _mockChannel.Verify();
        }

        [Fact]
        public async Task CreateMonitoredItemsAsyncShouldBatchRequestsWhenExceedingOperationLimitsAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var subscriptionId = 1u;
            var timestampsToReturn = TimestampsToReturn.Both;
            var itemsToCreate = new MonitoredItemCreateRequestCollection(
                Enumerable.Repeat(new MonitoredItemCreateRequest(), 15).ToList());
            var ct = CancellationToken.None;

            _sessionServices.OperationLimits.MaxMonitoredItemsPerCall = 10;

            _mockChannel
                .SetupSequence(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is CreateMonitoredItemsRequest),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CreateMonitoredItemsResponse
                {
                    Results = new MonitoredItemCreateResultCollection(
                        Enumerable.Repeat(new MonitoredItemCreateResult(), 10).ToList())
                })
                .ReturnsAsync(new CreateMonitoredItemsResponse
                {
                    Results = new MonitoredItemCreateResultCollection(
                        Enumerable.Repeat(new MonitoredItemCreateResult(), 5).ToList())
                });

            // Act
            var response = await _sessionServices.CreateMonitoredItemsAsync(requestHeader,
                subscriptionId, timestampsToReturn, itemsToCreate, ct);

            // Assert
            response.Should().NotBeNull();
            response.Results.Count.Should().Be(15);
            _mockChannel.Verify(c => c.SendRequestAsync(It.IsAny<IServiceRequest>(),
                It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task CreateMonitoredItemsAsyncShouldHandleBatchingWhenSecondOperationFailsAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var subscriptionId = 1u;
            var timestampsToReturn = TimestampsToReturn.Both;
            var itemsToCreate = new MonitoredItemCreateRequestCollection(Enumerable.Repeat(new MonitoredItemCreateRequest(), 15).ToList());
            var ct = CancellationToken.None;

            _sessionServices.OperationLimits.MaxMonitoredItemsPerCall = 10;

            _mockChannel
                .SetupSequence(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is CreateMonitoredItemsRequest),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CreateMonitoredItemsResponse
                {
                    Results = new MonitoredItemCreateResultCollection(Enumerable.Repeat(new MonitoredItemCreateResult(), 10).ToList())
                })
                .ReturnsAsync(new CreateMonitoredItemsResponse
                {
                    Results = new MonitoredItemCreateResultCollection(Enumerable.Repeat(new MonitoredItemCreateResult(), 5).ToList()),
                    ResponseHeader = new ResponseHeader
                    {
                        ServiceResult = StatusCodes.Bad
                    }
                });

            // Act
            Func<Task> act = async () => await _sessionServices.CreateMonitoredItemsAsync(requestHeader, subscriptionId, timestampsToReturn, itemsToCreate, ct);

            // Assert
            await act.Should().ThrowAsync<ServiceResultException>();
            _mockChannel.Verify(c => c.SendRequestAsync(It.IsAny<IServiceRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task CreateMonitoredItemsAsyncShouldSimplyCallBaseMethodWhenNoLimitsSetAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var subscriptionId = 1u;
            var timestampsToReturn = TimestampsToReturn.Both;
            var itemsToCreate = new MonitoredItemCreateRequestCollection();
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is CreateMonitoredItemsRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ReturnsAsync(new CreateMonitoredItemsResponse())
                .Verifiable(Times.Once);

            // Act
            var response = await _sessionServices.CreateMonitoredItemsAsync(requestHeader,
                subscriptionId, timestampsToReturn, itemsToCreate, ct);

            // Assert
            response.Should().NotBeNull();
            requestHeader.RequestHandle.Should().NotBe(0);
            requestHeader.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            _mockChannel.Verify();
        }

        [Fact]
        public async Task CreateMonitoredItemsAsyncShouldThrowExceptionWhenResponseContainsBadStatusCodeAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var subscriptionId = 1u;
            var timestampsToReturn = TimestampsToReturn.Both;
            var itemsToCreate = new MonitoredItemCreateRequestCollection();
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is CreateMonitoredItemsRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ReturnsAsync(new CreateMonitoredItemsResponse
                {
                    ResponseHeader = new ResponseHeader
                    {
                        ServiceResult = StatusCodes.Bad
                    }
                })
                .Verifiable(Times.Once);

            // Act
            Func<Task> act = async () => await _sessionServices.CreateMonitoredItemsAsync(requestHeader,
                subscriptionId, timestampsToReturn, itemsToCreate, ct);

            // Assert
            await act.Should().ThrowAsync<ServiceResultException>();
            _mockChannel.Verify();
        }

        [Fact]
        public async Task CreateMonitoredItemsAsyncShouldThrowExceptionWhenSendRequestAsyncThrowsAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var subscriptionId = 1u;
            var timestampsToReturn = TimestampsToReturn.Both;
            var itemsToCreate = new MonitoredItemCreateRequestCollection();
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is CreateMonitoredItemsRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ThrowsAsync(new IOException("Test exception"))
                .Verifiable(Times.Once);

            // Act
            Func<Task> act = async () => await _sessionServices.CreateMonitoredItemsAsync(requestHeader,
                subscriptionId, timestampsToReturn, itemsToCreate, ct);

            // Assert
            await act.Should().ThrowAsync<IOException>().WithMessage("Test exception");
            _mockChannel.Verify();
        }

        [Fact]
        public async Task CreateMonitoredItemsAsyncShouldValidateResponseAndHandleDiagnosticInfoAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var subscriptionId = 1u;
            var timestampsToReturn = TimestampsToReturn.Both;
            var itemsToCreate = new MonitoredItemCreateRequestCollection();
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is CreateMonitoredItemsRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ReturnsAsync(new CreateMonitoredItemsResponse
                {
                    Results = new MonitoredItemCreateResultCollection { new MonitoredItemCreateResult { StatusCode = StatusCodes.Good } },
                    DiagnosticInfos = new DiagnosticInfoCollection { new DiagnosticInfo() }
                })
                .Verifiable(Times.Once);

            // Act
            var response = await _sessionServices.CreateMonitoredItemsAsync(requestHeader, subscriptionId, timestampsToReturn, itemsToCreate, ct);

            // Assert
            response.Should().NotBeNull();
            response.Results.Should().HaveCount(1);
            response.DiagnosticInfos.Should().HaveCount(1);
        }

        [Fact]
        public async Task CreateSubscriptionAsyncShouldSimplyCallBaseMethodWhenNoLimitsSetAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var requestedPublishingInterval = 1000.0;
            var requestedLifetimeCount = 10u;
            var requestedMaxKeepAliveCount = 5u;
            var maxNotificationsPerPublish = 100u;
            var publishingEnabled = true;
            var priority = (byte)1;
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is CreateSubscriptionRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ReturnsAsync(new CreateSubscriptionResponse())
                .Verifiable(Times.Once);

            // Act
            var response = await _sessionServices.CreateSubscriptionAsync(requestHeader,
                requestedPublishingInterval, requestedLifetimeCount, requestedMaxKeepAliveCount,
                maxNotificationsPerPublish, publishingEnabled, priority, ct);

            // Assert
            response.Should().NotBeNull();
            requestHeader.RequestHandle.Should().NotBe(0);
            requestHeader.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            _mockChannel.Verify();
        }

        [Fact]
        public async Task CreateSubscriptionAsyncShouldThrowExceptionWhenResponseContainsBadStatusCodeAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var requestedPublishingInterval = 1000.0;
            var requestedLifetimeCount = 10u;
            var requestedMaxKeepAliveCount = 5u;
            var maxNotificationsPerPublish = 100u;
            var publishingEnabled = true;
            var priority = (byte)1;
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is CreateSubscriptionRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ReturnsAsync(new CreateSubscriptionResponse
                {
                    ResponseHeader = new ResponseHeader
                    {
                        ServiceResult = StatusCodes.Bad
                    }
                })
                .Verifiable(Times.Once);

            // Act
            Func<Task> act = async () => await _sessionServices.CreateSubscriptionAsync(requestHeader,
                requestedPublishingInterval, requestedLifetimeCount, requestedMaxKeepAliveCount,
                maxNotificationsPerPublish, publishingEnabled, priority, ct);

            // Assert
            await act.Should().ThrowAsync<ServiceResultException>();
            _mockChannel.Verify();
        }

        [Fact]
        public async Task CreateSubscriptionAsyncShouldThrowExceptionWhenSendRequestAsyncThrowsAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var requestedPublishingInterval = 1000.0;
            var requestedLifetimeCount = 10u;
            var requestedMaxKeepAliveCount = 5u;
            var maxNotificationsPerPublish = 100u;
            var publishingEnabled = true;
            var priority = (byte)1;
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is CreateSubscriptionRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ThrowsAsync(new IOException("Test exception"))
                .Verifiable(Times.Once);

            // Act
            Func<Task> act = async () => await _sessionServices.CreateSubscriptionAsync(requestHeader,
                requestedPublishingInterval, requestedLifetimeCount, requestedMaxKeepAliveCount,
                maxNotificationsPerPublish, publishingEnabled, priority, ct);

            // Assert
            await act.Should().ThrowAsync<IOException>().WithMessage("Test exception");
            _mockChannel.Verify();
        }

        [Fact]
        public async Task DeleteMonitoredItemsAsyncShouldBatchRequestsWhenExceedingOperationLimitsAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var subscriptionId = 1u;
            var monitoredItemIds = new UInt32Collection(Enumerable.Repeat(1u, 15).ToList());
            var ct = CancellationToken.None;

            _sessionServices.OperationLimits.MaxMonitoredItemsPerCall = 10;

            _mockChannel
                .SetupSequence(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is DeleteMonitoredItemsRequest),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DeleteMonitoredItemsResponse
                {
                    Results = new StatusCodeCollection(
                    Enumerable.Repeat((StatusCode)StatusCodes.Good, 10).ToList())
                })
                .ReturnsAsync(new DeleteMonitoredItemsResponse
                {
                    Results = new StatusCodeCollection(
                    Enumerable.Repeat((StatusCode)StatusCodes.Good, 5).ToList())
                });

            // Act
            var response = await _sessionServices.DeleteMonitoredItemsAsync(requestHeader,
                subscriptionId, monitoredItemIds, ct);

            // Assert
            response.Should().NotBeNull();
            response.Results.Count.Should().Be(15);
            _mockChannel.Verify(c => c.SendRequestAsync(It.IsAny<IServiceRequest>(),
                It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task DeleteMonitoredItemsAsyncShouldHandleBatchingWhenSecondOperationFailsAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var subscriptionId = 1u;
            var monitoredItemIds = new UInt32Collection(Enumerable.Repeat(1u, 15).ToList());
            var ct = CancellationToken.None;

            _sessionServices.OperationLimits.MaxMonitoredItemsPerCall = 10;

            _mockChannel
                .SetupSequence(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is DeleteMonitoredItemsRequest),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DeleteMonitoredItemsResponse
                {
                    Results = new StatusCodeCollection(Enumerable.Repeat((StatusCode)StatusCodes.Good, 10).ToList())
                })
                .ReturnsAsync(new DeleteMonitoredItemsResponse
                {
                    Results = new StatusCodeCollection(Enumerable.Repeat((StatusCode)StatusCodes.Bad, 5).ToList()),
                    ResponseHeader = new ResponseHeader
                    {
                        ServiceResult = StatusCodes.Bad
                    }
                });

            // Act
            Func<Task> act = async () => await _sessionServices.DeleteMonitoredItemsAsync(requestHeader, subscriptionId, monitoredItemIds, ct);

            // Assert
            await act.Should().ThrowAsync<ServiceResultException>();
            _mockChannel.Verify(c => c.SendRequestAsync(It.IsAny<IServiceRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task DeleteMonitoredItemsAsyncShouldSimplyCallBaseMethodWhenNoLimitsSetAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var subscriptionId = 1u;
            var monitoredItemIds = new UInt32Collection();
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is DeleteMonitoredItemsRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ReturnsAsync(new DeleteMonitoredItemsResponse())
                .Verifiable(Times.Once);

            // Act
            var response = await _sessionServices.DeleteMonitoredItemsAsync(
                requestHeader, subscriptionId, monitoredItemIds, ct);

            // Assert
            response.Should().NotBeNull();
            requestHeader.RequestHandle.Should().NotBe(0);
            requestHeader.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            _mockChannel.Verify();
        }

        [Fact]
        public async Task DeleteMonitoredItemsAsyncShouldThrowExceptionWhenResponseContainsBadStatusCodeAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var subscriptionId = 1u;
            var monitoredItemIds = new UInt32Collection();
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is DeleteMonitoredItemsRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ReturnsAsync(new DeleteMonitoredItemsResponse
                {
                    ResponseHeader = new ResponseHeader
                    {
                        ServiceResult = StatusCodes.Bad
                    }
                })
                .Verifiable(Times.Once);

            // Act
            Func<Task> act = async () => await _sessionServices.DeleteMonitoredItemsAsync(requestHeader,
                subscriptionId, monitoredItemIds, ct);

            // Assert
            await act.Should().ThrowAsync<ServiceResultException>();
            _mockChannel.Verify();
        }

        [Fact]
        public async Task DeleteMonitoredItemsAsyncShouldThrowExceptionWhenSendRequestAsyncThrowsAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var subscriptionId = 1u;
            var monitoredItemIds = new UInt32Collection();
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is DeleteMonitoredItemsRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ThrowsAsync(new IOException("Test exception"))
                .Verifiable(Times.Once);

            // Act
            Func<Task> act = async () => await _sessionServices.DeleteMonitoredItemsAsync(requestHeader,
                subscriptionId, monitoredItemIds, ct);

            // Assert
            await act.Should().ThrowAsync<IOException>().WithMessage("Test exception");
            _mockChannel.Verify();
        }

        [Fact]
        public async Task DeleteNodesAsyncShouldBatchRequestsWhenExceedingOperationLimitsAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var nodesToDelete = new DeleteNodesItemCollection(
                Enumerable.Repeat(new DeleteNodesItem(), 15).ToList());
            var ct = CancellationToken.None;

            _sessionServices.OperationLimits.MaxNodesPerNodeManagement = 10;

            _mockChannel
                .SetupSequence(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is DeleteNodesRequest),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DeleteNodesResponse
                {
                    Results = new StatusCodeCollection(
                        Enumerable.Repeat((StatusCode)StatusCodes.Good, 10).ToList())
                })
                .ReturnsAsync(new DeleteNodesResponse
                {
                    Results = new StatusCodeCollection(
                        Enumerable.Repeat((StatusCode)StatusCodes.Good, 5).ToList())
                });

            // Act
            var response = await _sessionServices.DeleteNodesAsync(requestHeader, nodesToDelete, ct);

            // Assert
            response.Should().NotBeNull();
            response.Results.Count.Should().Be(15);
            _mockChannel.Verify(c => c.SendRequestAsync(It.IsAny<IServiceRequest>(),
                It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task DeleteNodesAsyncShouldHandleBatchingWhenSecondOperationFailsAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var nodesToDelete = new DeleteNodesItemCollection(Enumerable.Repeat(new DeleteNodesItem(), 15).ToList());
            var ct = CancellationToken.None;

            _sessionServices.OperationLimits.MaxNodesPerNodeManagement = 10;

            _mockChannel
                .SetupSequence(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is DeleteNodesRequest),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DeleteNodesResponse
                {
                    Results = new StatusCodeCollection(Enumerable.Repeat((StatusCode)StatusCodes.Good, 10).ToList())
                })
                .ReturnsAsync(new DeleteNodesResponse
                {
                    Results = new StatusCodeCollection(Enumerable.Repeat((StatusCode)StatusCodes.Bad, 5).ToList()),
                    ResponseHeader = new ResponseHeader
                    {
                        ServiceResult = StatusCodes.Bad
                    }
                });

            // Act
            Func<Task> act = async () => await _sessionServices.DeleteNodesAsync(requestHeader, nodesToDelete, ct);

            // Assert
            await act.Should().ThrowAsync<ServiceResultException>();
            _mockChannel.Verify(c => c.SendRequestAsync(It.IsAny<IServiceRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task DeleteNodesAsyncShouldSimplyCallBaseMethodWhenNoLimitsSetAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var nodesToDelete = new DeleteNodesItemCollection();
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is DeleteNodesRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ReturnsAsync(new DeleteNodesResponse())
                .Verifiable(Times.Once);

            // Act
            var response = await _sessionServices.DeleteNodesAsync(requestHeader,
                nodesToDelete, ct);

            // Assert
            response.Should().NotBeNull();
            requestHeader.RequestHandle.Should().NotBe(0);
            requestHeader.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            _mockChannel.Verify();
        }

        [Fact]
        public async Task DeleteNodesAsyncShouldThrowExceptionWhenResponseContainsBadStatusCodeAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var nodesToDelete = new DeleteNodesItemCollection();
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is DeleteNodesRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ReturnsAsync(new DeleteNodesResponse
                {
                    ResponseHeader = new ResponseHeader
                    {
                        ServiceResult = StatusCodes.Bad
                    }
                })
                .Verifiable(Times.Once);

            // Act
            Func<Task> act = async () => await _sessionServices.DeleteNodesAsync(requestHeader,
                nodesToDelete, ct);

            // Assert
            await act.Should().ThrowAsync<ServiceResultException>();
            _mockChannel.Verify();
        }

        [Fact]
        public async Task DeleteNodesAsyncShouldThrowExceptionWhenSendRequestAsyncThrowsAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var nodesToDelete = new DeleteNodesItemCollection();
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is DeleteNodesRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ThrowsAsync(new IOException("Test exception"))
                .Verifiable(Times.Once);

            // Act
            Func<Task> act = async () => await _sessionServices.DeleteNodesAsync(requestHeader,
                nodesToDelete, ct);

            // Assert
            await act.Should().ThrowAsync<IOException>().WithMessage("Test exception");
            _mockChannel.Verify();
        }

        [Fact]
        public async Task DeleteNodesAsyncShouldValidateResponseAndHandleDiagnosticInfoAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var nodesToDelete = new DeleteNodesItemCollection();
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is DeleteNodesRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ReturnsAsync(new DeleteNodesResponse
                {
                    Results = new StatusCodeCollection { StatusCodes.Good },
                    DiagnosticInfos = new DiagnosticInfoCollection { new DiagnosticInfo() }
                })
                .Verifiable(Times.Once);

            // Act
            var response = await _sessionServices.DeleteNodesAsync(requestHeader, nodesToDelete, ct);

            // Assert
            response.Should().NotBeNull();
            response.Results.Should().HaveCount(1);
            response.DiagnosticInfos.Should().HaveCount(1);
        }

        [Fact]
        public async Task DeleteReferencesAsyncShouldBatchRequestsWhenExceedingOperationLimitsAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var referencesToDelete = new DeleteReferencesItemCollection(
                Enumerable.Repeat(new DeleteReferencesItem(), 15).ToList());
            var ct = CancellationToken.None;

            _sessionServices.OperationLimits.MaxNodesPerNodeManagement = 10;

            _mockChannel
                .SetupSequence(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is DeleteReferencesRequest),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DeleteReferencesResponse
                {
                    Results = new StatusCodeCollection(
                        Enumerable.Repeat((StatusCode)StatusCodes.Good, 10).ToList())
                })
                .ReturnsAsync(new DeleteReferencesResponse
                {
                    Results = new StatusCodeCollection(
                        Enumerable.Repeat((StatusCode)StatusCodes.Good, 5).ToList())
                });

            // Act
            var response = await _sessionServices.DeleteReferencesAsync(requestHeader, referencesToDelete, ct);

            // Assert
            response.Should().NotBeNull();
            response.Results.Count.Should().Be(15);
            _mockChannel.Verify(c => c.SendRequestAsync(It.IsAny<IServiceRequest>(),
                It.IsAny<CancellationToken>()), Times.Exactly(2));
        }
        [Fact]
        public async Task DeleteReferencesAsyncShouldHandleBatchingWhenSecondOperationFailsAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var referencesToDelete = new DeleteReferencesItemCollection(Enumerable.Repeat(new DeleteReferencesItem(), 15).ToList());
            var ct = CancellationToken.None;

            _sessionServices.OperationLimits.MaxNodesPerNodeManagement = 10;

            _mockChannel
                .SetupSequence(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is DeleteReferencesRequest),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DeleteReferencesResponse
                {
                    Results = new StatusCodeCollection(Enumerable.Repeat((StatusCode)StatusCodes.Good, 10).ToList())
                })
                .ReturnsAsync(new DeleteReferencesResponse
                {
                    Results = new StatusCodeCollection(Enumerable.Repeat((StatusCode)StatusCodes.Bad, 5).ToList()),
                    ResponseHeader = new ResponseHeader
                    {
                        ServiceResult = StatusCodes.Bad
                    }
                });

            // Act
            Func<Task> act = async () => await _sessionServices.DeleteReferencesAsync(requestHeader, referencesToDelete, ct);

            // Assert
            await act.Should().ThrowAsync<ServiceResultException>();
            _mockChannel.Verify(c => c.SendRequestAsync(It.IsAny<IServiceRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task DeleteReferencesAsyncShouldSimplyCallBaseMethodWhenNoLimitsSetAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var referencesToDelete = new DeleteReferencesItemCollection();
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is DeleteReferencesRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ReturnsAsync(new DeleteReferencesResponse())
                .Verifiable(Times.Once);

            // Act
            var response = await _sessionServices.DeleteReferencesAsync(requestHeader,
                referencesToDelete, ct);

            // Assert
            response.Should().NotBeNull();
            requestHeader.RequestHandle.Should().NotBe(0);
            requestHeader.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            _mockChannel.Verify();
        }

        [Fact]
        public async Task DeleteReferencesAsyncShouldThrowExceptionWhenResponseContainsBadStatusCodeAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var referencesToDelete = new DeleteReferencesItemCollection();
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is DeleteReferencesRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ReturnsAsync(new DeleteReferencesResponse
                {
                    ResponseHeader = new ResponseHeader
                    {
                        ServiceResult = StatusCodes.Bad
                    }
                })
                .Verifiable(Times.Once);

            // Act
            Func<Task> act = async () => await _sessionServices.DeleteReferencesAsync(requestHeader,
                referencesToDelete, ct);

            // Assert
            await act.Should().ThrowAsync<ServiceResultException>();
            _mockChannel.Verify();
        }

        [Fact]
        public async Task DeleteReferencesAsyncShouldThrowExceptionWhenSendRequestAsyncThrowsAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var referencesToDelete = new DeleteReferencesItemCollection();
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is DeleteReferencesRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ThrowsAsync(new IOException("Test exception"))
                .Verifiable(Times.Once);

            // Act
            Func<Task> act = async () => await _sessionServices.DeleteReferencesAsync(requestHeader,
                referencesToDelete, ct);

            // Assert
            await act.Should().ThrowAsync<IOException>().WithMessage("Test exception");
            _mockChannel.Verify();
        }

        [Fact]
        public async Task DeleteReferencesAsyncShouldValidateResponseAndHandleDiagnosticInfoAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var referencesToDelete = new DeleteReferencesItemCollection();
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is DeleteReferencesRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ReturnsAsync(new DeleteReferencesResponse
                {
                    Results = new StatusCodeCollection { StatusCodes.Good },
                    DiagnosticInfos = new DiagnosticInfoCollection { new DiagnosticInfo() }
                })
                .Verifiable(Times.Once);

            // Act
            var response = await _sessionServices.DeleteReferencesAsync(requestHeader, referencesToDelete, ct);

            // Assert
            response.Should().NotBeNull();
            response.Results.Should().HaveCount(1);
            response.DiagnosticInfos.Should().HaveCount(1);
        }
        [Fact]
        public async Task DeleteSubscriptionsAsyncShouldSimplyCallBaseMethodWhenNoLimitsSetAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var subscriptionIds = new UInt32Collection();
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is DeleteSubscriptionsRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ReturnsAsync(new DeleteSubscriptionsResponse())
                .Verifiable(Times.Once);

            // Act
            var response = await _sessionServices.DeleteSubscriptionsAsync(
                requestHeader, subscriptionIds, ct);

            // Assert
            response.Should().NotBeNull();
            requestHeader.RequestHandle.Should().NotBe(0);
            requestHeader.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            _mockChannel.Verify();
        }

        [Fact]
        public async Task DeleteSubscriptionsAsyncShouldThrowExceptionWhenResponseContainsBadStatusCodeAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var subscriptionIds = new UInt32Collection();
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is DeleteSubscriptionsRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ReturnsAsync(new DeleteSubscriptionsResponse
                {
                    ResponseHeader = new ResponseHeader
                    {
                        ServiceResult = StatusCodes.Bad
                    }
                })
                .Verifiable(Times.Once);

            // Act
            Func<Task> act = async () => await _sessionServices.DeleteSubscriptionsAsync(requestHeader,
                subscriptionIds, ct);

            // Assert
            await act.Should().ThrowAsync<ServiceResultException>();
            _mockChannel.Verify();
        }

        [Fact]
        public async Task DeleteSubscriptionsAsyncShouldThrowExceptionWhenSendRequestAsyncThrowsAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var subscriptionIds = new UInt32Collection();
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is DeleteSubscriptionsRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ThrowsAsync(new IOException("Test exception"))
                .Verifiable(Times.Once);

            // Act
            Func<Task> act = async () => await _sessionServices.DeleteSubscriptionsAsync(requestHeader,
                subscriptionIds, ct);

            // Assert
            await act.Should().ThrowAsync<IOException>().WithMessage("Test exception");
            _mockChannel.Verify();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _sessionServices.Dispose();
        }

        [Fact]
        public async Task HistoryReadAsyncShouldBatchRequestsWhenExceedingOperationLimitsAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var historyReadDetails = new ExtensionObject();
            var timestampsToReturn = TimestampsToReturn.Both;
            var releaseContinuationPoints = true;
            var nodesToRead = new HistoryReadValueIdCollection(
                Enumerable.Repeat(new HistoryReadValueId(), 15).ToList());
            var ct = CancellationToken.None;

            _sessionServices.OperationLimits.MaxNodesPerHistoryReadData = 10;

            _mockChannel
                .SetupSequence(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is HistoryReadRequest),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new HistoryReadResponse
                {
                    Results = new HistoryReadResultCollection(
                    Enumerable.Repeat(new HistoryReadResult(), 10).ToList())
                })
                .ReturnsAsync(new HistoryReadResponse
                {
                    Results = new HistoryReadResultCollection(
                    Enumerable.Repeat(new HistoryReadResult(), 5).ToList())
                });

            // Act
            var response = await _sessionServices.HistoryReadAsync(requestHeader,
                historyReadDetails, timestampsToReturn, releaseContinuationPoints, nodesToRead, ct);

            // Assert
            response.Should().NotBeNull();
            response.Results.Count.Should().Be(15);
            _mockChannel.Verify(c => c.SendRequestAsync(It.IsAny<IServiceRequest>(),
                It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task HistoryReadAsyncShouldHandleBatchingWhenSecondOperationFailsAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var historyReadDetails = new ExtensionObject(new ReadEventDetails());
            var timestampsToReturn = TimestampsToReturn.Both;
            var releaseContinuationPoints = true;
            var nodesToRead = new HistoryReadValueIdCollection(Enumerable.Repeat(new HistoryReadValueId(), 15).ToList());
            var ct = CancellationToken.None;

            _sessionServices.OperationLimits.MaxNodesPerHistoryReadEvents = 10;

            _mockChannel
                .SetupSequence(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is HistoryReadRequest),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new HistoryReadResponse
                {
                    Results = new HistoryReadResultCollection(Enumerable.Repeat(new HistoryReadResult(), 10).ToList())
                })
                .ReturnsAsync(new HistoryReadResponse
                {
                    Results = new HistoryReadResultCollection(Enumerable.Repeat(new HistoryReadResult(), 5).ToList()),
                    ResponseHeader = new ResponseHeader
                    {
                        ServiceResult = StatusCodes.Bad
                    }
                });

            // Act
            Func<Task> act = async () => await _sessionServices.HistoryReadAsync(requestHeader, historyReadDetails, timestampsToReturn, releaseContinuationPoints, nodesToRead, ct);

            // Assert
            await act.Should().ThrowAsync<ServiceResultException>();
            _mockChannel.Verify(c => c.SendRequestAsync(It.IsAny<IServiceRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task HistoryReadAsyncShouldSimplyCallBaseMethodWhenNoLimitsSetAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var historyReadDetails = new ExtensionObject();
            var timestampsToReturn = TimestampsToReturn.Both;
            var releaseContinuationPoints = true;
            var nodesToRead = new HistoryReadValueIdCollection();
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is HistoryReadRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ReturnsAsync(new HistoryReadResponse())
                .Verifiable(Times.Once);

            // Act
            var response = await _sessionServices.HistoryReadAsync(requestHeader,
                historyReadDetails, timestampsToReturn, releaseContinuationPoints,
                nodesToRead, ct);

            // Assert
            response.Should().NotBeNull();
            requestHeader.RequestHandle.Should().NotBe(0);
            requestHeader.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            _mockChannel.Verify();
        }

        [Fact]
        public async Task HistoryReadAsyncShouldThrowExceptionWhenResponseContainsBadStatusCodeAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var historyReadDetails = new ExtensionObject();
            var timestampsToReturn = TimestampsToReturn.Both;
            var releaseContinuationPoints = true;
            var nodesToRead = new HistoryReadValueIdCollection();
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is HistoryReadRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ReturnsAsync(new HistoryReadResponse
                {
                    ResponseHeader = new ResponseHeader
                    {
                        ServiceResult = StatusCodes.Bad
                    }
                })
                .Verifiable(Times.Once);

            // Act
            Func<Task> act = async () => await _sessionServices.HistoryReadAsync(requestHeader,
                historyReadDetails, timestampsToReturn, releaseContinuationPoints,
                nodesToRead, ct);

            // Assert
            await act.Should().ThrowAsync<ServiceResultException>();
            _mockChannel.Verify();
        }

        [Fact]
        public async Task HistoryReadAsyncShouldThrowExceptionWhenSendRequestAsyncThrowsAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var historyReadDetails = new ExtensionObject();
            var timestampsToReturn = TimestampsToReturn.Both;
            var releaseContinuationPoints = true;
            var nodesToRead = new HistoryReadValueIdCollection();
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is HistoryReadRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ThrowsAsync(new IOException("Test exception"))
                .Verifiable(Times.Once);

            // Act
            Func<Task> act = async () => await _sessionServices.HistoryReadAsync(requestHeader,
                historyReadDetails, timestampsToReturn, releaseContinuationPoints,
                nodesToRead, ct);

            // Assert
            await act.Should().ThrowAsync<IOException>().WithMessage("Test exception");
            _mockChannel.Verify();
        }

        [Fact]
        public async Task HistoryReadAsyncShouldValidateResponseAndHandleDiagnosticInfoAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var historyReadDetails = new ExtensionObject();
            var timestampsToReturn = TimestampsToReturn.Both;
            var releaseContinuationPoints = true;
            var nodesToRead = new HistoryReadValueIdCollection();
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is HistoryReadRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ReturnsAsync(new HistoryReadResponse
                {
                    Results = new HistoryReadResultCollection { new HistoryReadResult { StatusCode = StatusCodes.Good } },
                    DiagnosticInfos = new DiagnosticInfoCollection { new DiagnosticInfo() }
                })
                .Verifiable(Times.Once);

            // Act
            var response = await _sessionServices.HistoryReadAsync(requestHeader, historyReadDetails, timestampsToReturn, releaseContinuationPoints, nodesToRead, ct);

            // Assert
            response.Should().NotBeNull();
            response.Results.Should().HaveCount(1);
            response.DiagnosticInfos.Should().HaveCount(1);
        }

        [Fact]
        public async Task HistoryUpdateAsyncShouldBatchRequestsWhenExceedingOperationLimitsAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var historyUpdateDetails = new ExtensionObjectCollection(Enumerable.Repeat(new ExtensionObject(), 15).ToList());
            var ct = CancellationToken.None;

            _sessionServices.OperationLimits.MaxNodesPerHistoryUpdateData = 10;

            _mockChannel
                .SetupSequence(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is HistoryUpdateRequest),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new HistoryUpdateResponse
                {
                    Results = new HistoryUpdateResultCollection(Enumerable.Repeat(new HistoryUpdateResult(), 10).ToList())
                })
                .ReturnsAsync(new HistoryUpdateResponse
                {
                    Results = new HistoryUpdateResultCollection(Enumerable.Repeat(new HistoryUpdateResult(), 5).ToList())
                });

            // Act
            var response = await _sessionServices.HistoryUpdateAsync(requestHeader, historyUpdateDetails, ct);

            // Assert
            response.Should().NotBeNull();
            response.Results.Count.Should().Be(15);
            _mockChannel.Verify(c => c.SendRequestAsync(It.IsAny<IServiceRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task HistoryUpdateAsyncShouldHandleBatchingWhenSecondOperationFailsAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var historyUpdateDetails = new ExtensionObjectCollection(
                Enumerable.Repeat(new ExtensionObject(new UpdateEventDetails()), 15).ToList());
            var ct = CancellationToken.None;

            _sessionServices.OperationLimits.MaxNodesPerHistoryUpdateEvents = 10;

            _mockChannel
                .SetupSequence(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is HistoryUpdateRequest),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new HistoryUpdateResponse
                {
                    Results = new HistoryUpdateResultCollection(Enumerable.Repeat(new HistoryUpdateResult(), 10).ToList())
                })
                .ReturnsAsync(new HistoryUpdateResponse
                {
                    Results = new HistoryUpdateResultCollection(Enumerable.Repeat(new HistoryUpdateResult(), 5).ToList()),
                    ResponseHeader = new ResponseHeader
                    {
                        ServiceResult = StatusCodes.Bad
                    }
                });

            // Act
            Func<Task> act = async () => await _sessionServices.HistoryUpdateAsync(requestHeader, historyUpdateDetails, ct);

            // Assert
            await act.Should().ThrowAsync<ServiceResultException>();
            _mockChannel.Verify(c => c.SendRequestAsync(It.IsAny<IServiceRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task HistoryUpdateAsyncShouldSimplyCallBaseMethodWhenNoLimitsSetAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var historyUpdateDetails = new ExtensionObjectCollection();
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is HistoryUpdateRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ReturnsAsync(new HistoryUpdateResponse())
                .Verifiable(Times.Once);

            // Act
            var response = await _sessionServices.HistoryUpdateAsync(requestHeader,
                historyUpdateDetails, ct);

            // Assert
            response.Should().NotBeNull();
            requestHeader.RequestHandle.Should().NotBe(0);
            requestHeader.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            _mockChannel.Verify();
        }

        [Fact]
        public async Task HistoryUpdateAsyncShouldThrowExceptionWhenResponseContainsBadStatusCodeAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var historyUpdateDetails = new ExtensionObjectCollection();
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is HistoryUpdateRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ReturnsAsync(new HistoryUpdateResponse
                {
                    ResponseHeader = new ResponseHeader
                    {
                        ServiceResult = StatusCodes.Bad
                    }
                })
                .Verifiable(Times.Once);

            // Act
            Func<Task> act = async () => await _sessionServices.HistoryUpdateAsync(requestHeader,
                historyUpdateDetails, ct);

            // Assert
            await act.Should().ThrowAsync<ServiceResultException>();
            _mockChannel.Verify();
        }

        [Fact]
        public async Task HistoryUpdateAsyncShouldThrowExceptionWhenSendRequestAsyncThrowsAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var historyUpdateDetails = new ExtensionObjectCollection();
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is HistoryUpdateRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ThrowsAsync(new IOException("Test exception"))
                .Verifiable(Times.Once);

            // Act
            Func<Task> act = async () => await _sessionServices.HistoryUpdateAsync(requestHeader,
                historyUpdateDetails, ct);

            // Assert
            await act.Should().ThrowAsync<IOException>().WithMessage("Test exception");
            _mockChannel.Verify();
        }

        [Fact]
        public async Task HistoryUpdateAsyncShouldValidateResponseAndHandleDiagnosticInfoAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var historyUpdateDetails = new ExtensionObjectCollection();
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is HistoryUpdateRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ReturnsAsync(new HistoryUpdateResponse
                {
                    Results = new HistoryUpdateResultCollection { new HistoryUpdateResult { StatusCode = StatusCodes.Good } },
                    DiagnosticInfos = new DiagnosticInfoCollection { new DiagnosticInfo() }
                })
                .Verifiable(Times.Once);

            // Act
            var response = await _sessionServices.HistoryUpdateAsync(requestHeader, historyUpdateDetails, ct);

            // Assert
            response.Should().NotBeNull();
            response.Results.Should().HaveCount(1);
            response.DiagnosticInfos.Should().HaveCount(1);
        }

        [Fact]
        public async Task ModifyMonitoredItemsAsyncShouldBatchRequestsWhenExceedingOperationLimitsAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var subscriptionId = 1u;
            var timestampsToReturn = TimestampsToReturn.Both;
            var itemsToModify = new MonitoredItemModifyRequestCollection(
                Enumerable.Repeat(new MonitoredItemModifyRequest(), 15).ToList());
            var ct = CancellationToken.None;

            _sessionServices.OperationLimits.MaxMonitoredItemsPerCall = 10;

            _mockChannel
                .SetupSequence(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is ModifyMonitoredItemsRequest),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ModifyMonitoredItemsResponse
                {
                    Results = new MonitoredItemModifyResultCollection(
                        Enumerable.Repeat(new MonitoredItemModifyResult(), 10).ToList())
                })
                .ReturnsAsync(new ModifyMonitoredItemsResponse
                {
                    Results = new MonitoredItemModifyResultCollection(
                        Enumerable.Repeat(new MonitoredItemModifyResult(), 5).ToList())
                });

            // Act
            var response = await _sessionServices.ModifyMonitoredItemsAsync(requestHeader, subscriptionId,
                timestampsToReturn, itemsToModify, ct);

            // Assert
            response.Should().NotBeNull();
            response.Results.Count.Should().Be(15);
            _mockChannel.Verify(c => c.SendRequestAsync(It.IsAny<IServiceRequest>(),
                It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task ModifyMonitoredItemsAsyncShouldHandleBatchingWhenSecondOperationFailsAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var subscriptionId = 1u;
            var timestampsToReturn = TimestampsToReturn.Both;
            var itemsToModify = new MonitoredItemModifyRequestCollection(Enumerable.Repeat(new MonitoredItemModifyRequest(), 15).ToList());
            var ct = CancellationToken.None;

            _sessionServices.OperationLimits.MaxMonitoredItemsPerCall = 10;

            _mockChannel
                .SetupSequence(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is ModifyMonitoredItemsRequest),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ModifyMonitoredItemsResponse
                {
                    Results = new MonitoredItemModifyResultCollection(Enumerable.Repeat(new MonitoredItemModifyResult(), 10).ToList())
                })
                .ReturnsAsync(new ModifyMonitoredItemsResponse
                {
                    Results = new MonitoredItemModifyResultCollection(Enumerable.Repeat(new MonitoredItemModifyResult(), 5).ToList()),
                    ResponseHeader = new ResponseHeader
                    {
                        ServiceResult = StatusCodes.Bad
                    }
                });

            // Act
            Func<Task> act = async () => await _sessionServices.ModifyMonitoredItemsAsync(requestHeader, subscriptionId, timestampsToReturn, itemsToModify, ct);

            // Assert
            await act.Should().ThrowAsync<ServiceResultException>();
            _mockChannel.Verify(c => c.SendRequestAsync(It.IsAny<IServiceRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task ModifyMonitoredItemsAsyncShouldSimplyCallBaseMethodWhenNoLimitsSetAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var subscriptionId = 1u;
            var timestampsToReturn = TimestampsToReturn.Both;
            var itemsToModify = new MonitoredItemModifyRequestCollection();
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is ModifyMonitoredItemsRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ReturnsAsync(new ModifyMonitoredItemsResponse())
                .Verifiable(Times.Once);

            // Act
            var response = await _sessionServices.ModifyMonitoredItemsAsync(requestHeader,
                subscriptionId, timestampsToReturn, itemsToModify, ct);

            // Assert
            response.Should().NotBeNull();
            requestHeader.RequestHandle.Should().NotBe(0);
            requestHeader.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            _mockChannel.Verify();
        }

        [Fact]
        public async Task ModifyMonitoredItemsAsyncShouldThrowExceptionWhenResponseContainsBadStatusCodeAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var subscriptionId = 1u;
            var timestampsToReturn = TimestampsToReturn.Both;
            var itemsToModify = new MonitoredItemModifyRequestCollection();
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is ModifyMonitoredItemsRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ReturnsAsync(new ModifyMonitoredItemsResponse
                {
                    ResponseHeader = new ResponseHeader
                    {
                        ServiceResult = StatusCodes.Bad
                    }
                })
                .Verifiable(Times.Once);

            // Act
            Func<Task> act = async () => await _sessionServices.ModifyMonitoredItemsAsync(requestHeader,
                subscriptionId, timestampsToReturn, itemsToModify, ct);

            // Assert
            await act.Should().ThrowAsync<ServiceResultException>();
            _mockChannel.Verify();
        }

        [Fact]
        public async Task ModifyMonitoredItemsAsyncShouldThrowExceptionWhenSendRequestAsyncThrowsAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var subscriptionId = 1u;
            var timestampsToReturn = TimestampsToReturn.Both;
            var itemsToModify = new MonitoredItemModifyRequestCollection();
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is ModifyMonitoredItemsRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ThrowsAsync(new IOException("Test exception"))
                .Verifiable(Times.Once);

            // Act
            Func<Task> act = async () => await _sessionServices.ModifyMonitoredItemsAsync(requestHeader,
                subscriptionId, timestampsToReturn, itemsToModify, ct);

            // Assert
            await act.Should().ThrowAsync<IOException>().WithMessage("Test exception");
            _mockChannel.Verify();
        }

        [Fact]
        public async Task ModifyMonitoredItemsAsyncShouldValidateResponseAndHandleDiagnosticInfoAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var subscriptionId = 1u;
            var timestampsToReturn = TimestampsToReturn.Both;
            var itemsToModify = new MonitoredItemModifyRequestCollection();
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is ModifyMonitoredItemsRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ReturnsAsync(new ModifyMonitoredItemsResponse
                {
                    Results = new MonitoredItemModifyResultCollection { new MonitoredItemModifyResult { StatusCode = StatusCodes.Good } },
                    DiagnosticInfos = new DiagnosticInfoCollection { new DiagnosticInfo() }
                })
                .Verifiable(Times.Once);

            // Act
            var response = await _sessionServices.ModifyMonitoredItemsAsync(requestHeader, subscriptionId, timestampsToReturn, itemsToModify, ct);

            // Assert
            response.Should().NotBeNull();
            response.Results.Should().HaveCount(1);
            response.DiagnosticInfos.Should().HaveCount(1);
        }

        [Fact]
        public async Task ModifySubscriptionAsyncShouldSimplyCallBaseMethodWhenNoLimitsSetAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var subscriptionId = 1u;
            var requestedPublishingInterval = 1000.0;
            var requestedLifetimeCount = 10u;
            var requestedMaxKeepAliveCount = 5u;
            var maxNotificationsPerPublish = 100u;
            var priority = (byte)1;
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is ModifySubscriptionRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ReturnsAsync(new ModifySubscriptionResponse())
                .Verifiable(Times.Once);

            // Act
            var response = await _sessionServices.ModifySubscriptionAsync(requestHeader,
                subscriptionId, requestedPublishingInterval, requestedLifetimeCount,
                requestedMaxKeepAliveCount, maxNotificationsPerPublish, priority, ct);

            // Assert
            response.Should().NotBeNull();
            requestHeader.RequestHandle.Should().NotBe(0);
            requestHeader.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            _mockChannel.Verify();
        }

        [Fact]
        public async Task ModifySubscriptionAsyncShouldThrowExceptionWhenResponseContainsBadStatusCodeAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var subscriptionId = 1u;
            var requestedPublishingInterval = 1000.0;
            var requestedLifetimeCount = 10u;
            var requestedMaxKeepAliveCount = 5u;
            var maxNotificationsPerPublish = 100u;
            var priority = (byte)1;
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is ModifySubscriptionRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ReturnsAsync(new ModifySubscriptionResponse
                {
                    ResponseHeader = new ResponseHeader
                    {
                        ServiceResult = StatusCodes.Bad
                    }
                })
                .Verifiable(Times.Once);

            // Act
            Func<Task> act = async () => await _sessionServices.ModifySubscriptionAsync(requestHeader,
                subscriptionId, requestedPublishingInterval, requestedLifetimeCount,
                requestedMaxKeepAliveCount, maxNotificationsPerPublish, priority, ct);

            // Assert
            await act.Should().ThrowAsync<ServiceResultException>();
            _mockChannel.Verify();
        }

        [Fact]
        public async Task ModifySubscriptionAsyncShouldThrowExceptionWhenSendRequestAsyncThrowsAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var subscriptionId = 1u;
            var requestedPublishingInterval = 1000.0;
            var requestedLifetimeCount = 10u;
            var requestedMaxKeepAliveCount = 5u;
            var maxNotificationsPerPublish = 100u;
            var priority = (byte)1;
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is ModifySubscriptionRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ThrowsAsync(new IOException("Test exception"))
                .Verifiable(Times.Once);

            // Act
            Func<Task> act = async () => await _sessionServices.ModifySubscriptionAsync(requestHeader,
                subscriptionId, requestedPublishingInterval, requestedLifetimeCount,
                requestedMaxKeepAliveCount, maxNotificationsPerPublish, priority, ct);

            // Assert
            await act.Should().ThrowAsync<IOException>().WithMessage("Test exception");
            _mockChannel.Verify();
        }

        [Fact]
        public async Task PublishAsyncShouldSimplyCallBaseMethodWhenNoLimitsSetAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var subscriptionAcknowledgements = new SubscriptionAcknowledgementCollection();
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is PublishRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ReturnsAsync(new PublishResponse())
                .Verifiable(Times.Once);

            // Act
            var response = await _sessionServices.PublishAsync(requestHeader,
                subscriptionAcknowledgements, ct);

            // Assert
            response.Should().NotBeNull();
            requestHeader.RequestHandle.Should().NotBe(0);
            requestHeader.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            _mockChannel.Verify();
        }

        [Fact]
        public async Task PublishAsyncShouldThrowExceptionWhenResponseContainsBadStatusCodeAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var subscriptionAcknowledgements = new SubscriptionAcknowledgementCollection();
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is PublishRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ReturnsAsync(new PublishResponse
                {
                    ResponseHeader = new ResponseHeader
                    {
                        ServiceResult = StatusCodes.Bad
                    }
                })
                .Verifiable(Times.Once);

            // Act
            Func<Task> act = async () => await _sessionServices.PublishAsync(requestHeader,
                subscriptionAcknowledgements, ct);

            // Assert
            await act.Should().ThrowAsync<ServiceResultException>();
            _mockChannel.Verify();
        }

        [Fact]
        public async Task PublishAsyncShouldThrowExceptionWhenSendRequestAsyncThrowsAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var subscriptionAcknowledgements = new SubscriptionAcknowledgementCollection();
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is PublishRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ThrowsAsync(new IOException("Test exception"))
                .Verifiable(Times.Once);

            // Act
            Func<Task> act = async () => await _sessionServices.PublishAsync(requestHeader,
                subscriptionAcknowledgements, ct);

            // Assert
            await act.Should().ThrowAsync<IOException>().WithMessage("Test exception");
            _mockChannel.Verify();
        }

        [Fact]
        public async Task QueryFirstAsyncShouldSimplyCallBaseMethodWhenNoLimitsSetAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var view = new ViewDescription();
            var nodeTypes = new NodeTypeDescriptionCollection();
            var filter = new ContentFilter();
            var maxDataSetsToReturn = 10u;
            var maxReferencesToReturn = 10u;
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is QueryFirstRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ReturnsAsync(new QueryFirstResponse())
                .Verifiable(Times.Once);

            // Act
            var response = await _sessionServices.QueryFirstAsync(requestHeader,
                view, nodeTypes, filter, maxDataSetsToReturn, maxReferencesToReturn, ct);

            // Assert
            response.Should().NotBeNull();
            requestHeader.RequestHandle.Should().NotBe(0);
            requestHeader.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            _mockChannel.Verify();
        }

        [Fact]
        public async Task QueryFirstAsyncShouldThrowExceptionWhenResponseContainsBadStatusCodeAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var view = new ViewDescription();
            var nodeTypes = new NodeTypeDescriptionCollection();
            var filter = new ContentFilter();
            var maxDataSetsToReturn = 10u;
            var maxReferencesToReturn = 10u;
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is QueryFirstRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ReturnsAsync(new QueryFirstResponse
                {
                    ResponseHeader = new ResponseHeader
                    {
                        ServiceResult = StatusCodes.Bad
                    }
                })
                .Verifiable(Times.Once);

            // Act
            Func<Task> act = async () => await _sessionServices.QueryFirstAsync(requestHeader,
                view, nodeTypes, filter, maxDataSetsToReturn, maxReferencesToReturn, ct);

            // Assert
            await act.Should().ThrowAsync<ServiceResultException>();
            _mockChannel.Verify();
        }

        [Fact]
        public async Task QueryFirstAsyncShouldThrowExceptionWhenSendRequestAsyncThrowsAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var view = new ViewDescription();
            var nodeTypes = new NodeTypeDescriptionCollection();
            var filter = new ContentFilter();
            var maxDataSetsToReturn = 10u;
            var maxReferencesToReturn = 10u;
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is QueryFirstRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ThrowsAsync(new IOException("Test exception"))
                .Verifiable(Times.Once);

            // Act
            Func<Task> act = async () => await _sessionServices.QueryFirstAsync(requestHeader,
                view, nodeTypes, filter, maxDataSetsToReturn, maxReferencesToReturn, ct);

            // Assert
            await act.Should().ThrowAsync<IOException>().WithMessage("Test exception");
            _mockChannel.Verify();
        }

        [Fact]
        public async Task QueryNextAsyncShouldSimplyCallBaseMethodWhenNoLimitsSetAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var releaseContinuationPoint = true;
            var continuationPoint = new byte[0];
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is QueryNextRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ReturnsAsync(new QueryNextResponse())
                .Verifiable(Times.Once);

            // Act
            var response = await _sessionServices.QueryNextAsync(requestHeader,
                releaseContinuationPoint, continuationPoint, ct);

            // Assert
            response.Should().NotBeNull();
            requestHeader.RequestHandle.Should().NotBe(0);
            requestHeader.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            _mockChannel.Verify();
        }

        [Fact]
        public async Task QueryNextAsyncShouldThrowExceptionWhenResponseContainsBadStatusCodeAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var releaseContinuationPoint = true;
            var continuationPoint = new byte[0];
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is QueryNextRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ReturnsAsync(new QueryNextResponse
                {
                    ResponseHeader = new ResponseHeader
                    {
                        ServiceResult = StatusCodes.Bad
                    }
                })
                .Verifiable(Times.Once);

            // Act
            Func<Task> act = async () => await _sessionServices.QueryNextAsync(requestHeader,
                releaseContinuationPoint, continuationPoint, ct);

            // Assert
            await act.Should().ThrowAsync<ServiceResultException>();
            _mockChannel.Verify();
        }

        [Fact]
        public async Task QueryNextAsyncShouldThrowExceptionWhenSendRequestAsyncThrowsAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var releaseContinuationPoint = true;
            var continuationPoint = new byte[0];
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is QueryNextRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ThrowsAsync(new IOException("Test exception"))
                .Verifiable(Times.Once);

            // Act
            Func<Task> act = async () => await _sessionServices.QueryNextAsync(requestHeader,
                releaseContinuationPoint, continuationPoint, ct);

            // Assert
            await act.Should().ThrowAsync<IOException>().WithMessage("Test exception");
            _mockChannel.Verify();
        }

        [Fact]
        public async Task ReadAsyncShouldBatchRequestsWhenExceedingOperationLimitsAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var maxAge = 1000.0;
            var timestampsToReturn = TimestampsToReturn.Both;
            var nodesToRead = new ReadValueIdCollection(
                Enumerable.Repeat(new ReadValueId(), 15).ToList());
            var ct = CancellationToken.None;

            _sessionServices.OperationLimits.MaxNodesPerRead = 10;

            _mockChannel
                .SetupSequence(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is ReadRequest),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ReadResponse
                {
                    Results = new DataValueCollection(
                    Enumerable.Repeat(new DataValue(), 10).ToList())
                })
                .ReturnsAsync(new ReadResponse
                {
                    Results = new DataValueCollection(
                    Enumerable.Repeat(new DataValue(), 5).ToList())
                });

            // Act
            var response = await _sessionServices.ReadAsync(requestHeader, maxAge,
                timestampsToReturn, nodesToRead, ct);

            // Assert
            response.Should().NotBeNull();
            response.Results.Count.Should().Be(15);
            _mockChannel.Verify(c => c.SendRequestAsync(It.IsAny<IServiceRequest>(),
                It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task ReadAsyncShouldHandleBatchingWhenSecondOperationFailsAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var maxAge = 1000.0;
            var timestampsToReturn = TimestampsToReturn.Both;
            var nodesToRead = new ReadValueIdCollection(Enumerable.Repeat(new ReadValueId(), 15).ToList());
            var ct = CancellationToken.None;

            _sessionServices.OperationLimits.MaxNodesPerRead = 10;

            _mockChannel
                .SetupSequence(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is ReadRequest),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ReadResponse
                {
                    Results = new DataValueCollection(Enumerable.Repeat(new DataValue(), 10).ToList())
                })
                .ReturnsAsync(new ReadResponse
                {
                    Results = new DataValueCollection(Enumerable.Repeat(new DataValue(), 5).ToList()),
                    ResponseHeader = new ResponseHeader
                    {
                        ServiceResult = StatusCodes.Bad
                    }
                });

            // Act
            Func<Task> act = async () => await _sessionServices.ReadAsync(requestHeader, maxAge, timestampsToReturn, nodesToRead, ct);

            // Assert
            await act.Should().ThrowAsync<ServiceResultException>();
            _mockChannel.Verify(c => c.SendRequestAsync(It.IsAny<IServiceRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task ReadAsyncShouldSimplyCallBaseMethodWhenNoLimitsSetAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var maxAge = 1000.0;
            var timestampsToReturn = TimestampsToReturn.Both;
            var nodesToRead = new ReadValueIdCollection();
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is ReadRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ReturnsAsync(new ReadResponse())
                .Verifiable(Times.Once);

            // Act
            var response = await _sessionServices.ReadAsync(requestHeader,
                maxAge, timestampsToReturn, nodesToRead, ct);

            // Assert
            response.Should().NotBeNull();
            requestHeader.RequestHandle.Should().NotBe(0);
            requestHeader.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            _mockChannel.Verify();
        }

        [Fact]
        public async Task ReadAsyncShouldThrowExceptionWhenResponseContainsBadStatusCodeAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var maxAge = 1000.0;
            var timestampsToReturn = TimestampsToReturn.Both;
            var nodesToRead = new ReadValueIdCollection();
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is ReadRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ReturnsAsync(new ReadResponse
                {
                    ResponseHeader = new ResponseHeader
                    {
                        ServiceResult = StatusCodes.Bad
                    }
                })
                .Verifiable(Times.Once);

            // Act
            Func<Task> act = async () => await _sessionServices.ReadAsync(requestHeader,
                maxAge, timestampsToReturn, nodesToRead, ct);

            // Assert
            await act.Should().ThrowAsync<ServiceResultException>();
            _mockChannel.Verify();
        }

        [Fact]
        public async Task ReadAsyncShouldThrowExceptionWhenSendRequestAsyncThrowsAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var maxAge = 1000.0;
            var timestampsToReturn = TimestampsToReturn.Both;
            var nodesToRead = new ReadValueIdCollection();
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is ReadRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ThrowsAsync(new IOException("Test exception"))
                .Verifiable(Times.Once);

            // Act
            Func<Task> act = async () => await _sessionServices.ReadAsync(requestHeader,
                maxAge, timestampsToReturn, nodesToRead, ct);

            // Assert
            await act.Should().ThrowAsync<IOException>().WithMessage("Test exception");
            _mockChannel.Verify();
        }

        [Fact]
        public async Task ReadAsyncShouldValidateResponseAndHandleDiagnosticInfoAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var maxAge = 1000.0;
            var timestampsToReturn = TimestampsToReturn.Both;
            var nodesToRead = new ReadValueIdCollection();
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is ReadRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ReturnsAsync(new ReadResponse
                {
                    Results = new DataValueCollection { new DataValue { StatusCode = StatusCodes.Good } },
                    DiagnosticInfos = new DiagnosticInfoCollection { new DiagnosticInfo() }
                })
                .Verifiable(Times.Once);

            // Act
            var response = await _sessionServices.ReadAsync(requestHeader, maxAge, timestampsToReturn, nodesToRead, ct);

            // Assert
            response.Should().NotBeNull();
            response.Results.Should().HaveCount(1);
            response.DiagnosticInfos.Should().HaveCount(1);
        }

        [Fact]
        public async Task RegisterNodesAsyncShouldBatchRequestsWhenExceedingOperationLimitsAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var nodesToRegister = new NodeIdCollection(Enumerable.Repeat(new NodeId(), 15).ToList());
            var ct = CancellationToken.None;

            _sessionServices.OperationLimits.MaxNodesPerRegisterNodes = 10;

            _mockChannel
                .SetupSequence(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is RegisterNodesRequest),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new RegisterNodesResponse
                {
                    RegisteredNodeIds = new NodeIdCollection(Enumerable.Repeat(new NodeId(), 10).ToList())
                })
                .ReturnsAsync(new RegisterNodesResponse
                {
                    RegisteredNodeIds = new NodeIdCollection(Enumerable.Repeat(new NodeId(), 5).ToList())
                });

            // Act
            var response = await _sessionServices.RegisterNodesAsync(requestHeader, nodesToRegister, ct);

            // Assert
            response.Should().NotBeNull();
            response.RegisteredNodeIds.Count.Should().Be(15);
            _mockChannel.Verify(c => c.SendRequestAsync(It.IsAny<IServiceRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task RegisterNodesAsyncShouldHandleBatchingWhenSecondOperationFailsAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var nodesToRegister = new NodeIdCollection(Enumerable.Repeat(new NodeId(), 15).ToList());
            var ct = CancellationToken.None;

            _sessionServices.OperationLimits.MaxNodesPerRegisterNodes = 10;

            _mockChannel
                .SetupSequence(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is RegisterNodesRequest),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new RegisterNodesResponse
                {
                    RegisteredNodeIds = new NodeIdCollection(Enumerable.Repeat(new NodeId(), 10).ToList())
                })
                .ReturnsAsync(new RegisterNodesResponse
                {
                    RegisteredNodeIds = new NodeIdCollection(Enumerable.Repeat(new NodeId(), 5).ToList()),
                    ResponseHeader = new ResponseHeader
                    {
                        ServiceResult = StatusCodes.Bad
                    }
                });

            // Act
            Func<Task> act = async () => await _sessionServices.RegisterNodesAsync(requestHeader, nodesToRegister, ct);

            // Assert
            await act.Should().ThrowAsync<ServiceResultException>();
            _mockChannel.Verify(c => c.SendRequestAsync(It.IsAny<IServiceRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task RegisterNodesAsyncShouldSimplyCallBaseMethodWhenNoLimitsSetAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var nodesToRegister = new NodeIdCollection();
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is RegisterNodesRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ReturnsAsync(new RegisterNodesResponse())
                .Verifiable(Times.Once);

            // Act
            var response = await _sessionServices.RegisterNodesAsync(
                requestHeader, nodesToRegister, ct);

            // Assert
            response.Should().NotBeNull();
            requestHeader.RequestHandle.Should().NotBe(0);
            requestHeader.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            _mockChannel.Verify();
        }

        [Fact]
        public async Task RegisterNodesAsyncShouldThrowExceptionWhenResponseContainsBadStatusCodeAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var nodesToRegister = new NodeIdCollection();
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is RegisterNodesRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ReturnsAsync(new RegisterNodesResponse
                {
                    ResponseHeader = new ResponseHeader
                    {
                        ServiceResult = StatusCodes.Bad
                    }
                })
                .Verifiable(Times.Once);

            // Act
            Func<Task> act = async () => await _sessionServices.RegisterNodesAsync(requestHeader,
                nodesToRegister, ct);

            // Assert
            await act.Should().ThrowAsync<ServiceResultException>();
            _mockChannel.Verify();
        }

        [Fact]
        public async Task RegisterNodesAsyncShouldThrowExceptionWhenSendRequestAsyncThrowsAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var nodesToRegister = new NodeIdCollection();
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is RegisterNodesRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ThrowsAsync(new IOException("Test exception"))
                .Verifiable(Times.Once);

            // Act
            Func<Task> act = async () => await _sessionServices.RegisterNodesAsync(requestHeader,
                nodesToRegister, ct);

            // Assert
            await act.Should().ThrowAsync<IOException>().WithMessage("Test exception");
            _mockChannel.Verify();
        }

        [Fact]
        public async Task RepublishAsyncShouldSimplyCallBaseMethodWhenNoLimitsSetAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var subscriptionId = 1u;
            var retransmitSequenceNumber = 1u;
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is RepublishRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ReturnsAsync(new RepublishResponse())
                .Verifiable(Times.Once);

            // Act
            var response = await _sessionServices.RepublishAsync(requestHeader,
                subscriptionId, retransmitSequenceNumber, ct);

            // Assert
            response.Should().NotBeNull();
            requestHeader.RequestHandle.Should().NotBe(0);
            requestHeader.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            _mockChannel.Verify();
        }

        [Fact]
        public async Task RepublishAsyncShouldThrowExceptionWhenResponseContainsBadStatusCodeAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var subscriptionId = 1u;
            var retransmitSequenceNumber = 1u;
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is RepublishRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ReturnsAsync(new RepublishResponse
                {
                    ResponseHeader = new ResponseHeader
                    {
                        ServiceResult = StatusCodes.Bad
                    }
                })
                .Verifiable(Times.Once);

            // Act
            Func<Task> act = async () => await _sessionServices.RepublishAsync(requestHeader,
                subscriptionId, retransmitSequenceNumber, ct);

            // Assert
            await act.Should().ThrowAsync<ServiceResultException>();
            _mockChannel.Verify();
        }

        [Fact]
        public async Task RepublishAsyncShouldThrowExceptionWhenSendRequestAsyncThrowsAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var subscriptionId = 1u;
            var retransmitSequenceNumber = 1u;
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is RepublishRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ThrowsAsync(new IOException("Test exception"))
                .Verifiable(Times.Once);

            // Act
            Func<Task> act = async () => await _sessionServices.RepublishAsync(requestHeader,
                subscriptionId, retransmitSequenceNumber, ct);

            // Assert
            await act.Should().ThrowAsync<IOException>().WithMessage("Test exception");
            _mockChannel.Verify();
        }

        [Fact]
        public async Task SetMonitoringModeAsyncShouldBatchRequestsWhenExceedingOperationLimitsAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var subscriptionId = 1u;
            var monitoringMode = MonitoringMode.Reporting;
            var monitoredItemIds = new UInt32Collection(Enumerable.Repeat(1u, 15).ToList());
            var ct = CancellationToken.None;

            _sessionServices.OperationLimits.MaxMonitoredItemsPerCall = 10;

            _mockChannel
                .SetupSequence(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is SetMonitoringModeRequest),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SetMonitoringModeResponse
                {
                    Results = new StatusCodeCollection(
                    Enumerable.Repeat((StatusCode)StatusCodes.Good, 10).ToList())
                })
                .ReturnsAsync(new SetMonitoringModeResponse
                {
                    Results = new StatusCodeCollection(
                    Enumerable.Repeat((StatusCode)StatusCodes.Good, 5).ToList())
                });

            // Act
            var response = await _sessionServices.SetMonitoringModeAsync(requestHeader, subscriptionId,
                monitoringMode, monitoredItemIds, ct);

            // Assert
            response.Should().NotBeNull();
            response.Results.Count.Should().Be(15);
            _mockChannel.Verify(c => c.SendRequestAsync(It.IsAny<IServiceRequest>(),
                It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task SetMonitoringModeAsyncShouldHandleBatchingWhenSecondOperationFailsAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var subscriptionId = 1u;
            var monitoringMode = MonitoringMode.Reporting;
            var monitoredItemIds = new UInt32Collection(Enumerable.Repeat(1u, 15).ToList());
            var ct = CancellationToken.None;

            _sessionServices.OperationLimits.MaxMonitoredItemsPerCall = 10;

            _mockChannel
                .SetupSequence(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is SetMonitoringModeRequest),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SetMonitoringModeResponse
                {
                    Results = new StatusCodeCollection(Enumerable.Repeat((StatusCode)StatusCodes.Good, 10).ToList())
                })
                .ReturnsAsync(new SetMonitoringModeResponse
                {
                    Results = new StatusCodeCollection(Enumerable.Repeat((StatusCode)StatusCodes.Bad, 5).ToList()),
                    ResponseHeader = new ResponseHeader
                    {
                        ServiceResult = StatusCodes.Bad
                    }
                });

            // Act
            Func<Task> act = async () => await _sessionServices.SetMonitoringModeAsync(requestHeader, subscriptionId, monitoringMode, monitoredItemIds, ct);

            // Assert
            await act.Should().ThrowAsync<ServiceResultException>();
            _mockChannel.Verify(c => c.SendRequestAsync(It.IsAny<IServiceRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task SetMonitoringModeAsyncShouldSimplyCallBaseMethodWhenNoLimitsSetAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var subscriptionId = 1u;
            var monitoringMode = MonitoringMode.Reporting;
            var monitoredItemIds = new UInt32Collection();
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is SetMonitoringModeRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ReturnsAsync(new SetMonitoringModeResponse())
                .Verifiable(Times.Once);

            // Act
            var response = await _sessionServices.SetMonitoringModeAsync(requestHeader,
                subscriptionId, monitoringMode, monitoredItemIds, ct);

            // Assert
            response.Should().NotBeNull();
            requestHeader.RequestHandle.Should().NotBe(0);
            requestHeader.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            _mockChannel.Verify();
        }

        [Fact]
        public async Task SetMonitoringModeAsyncShouldThrowExceptionWhenResponseContainsBadStatusCodeAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var subscriptionId = 1u;
            var monitoringMode = MonitoringMode.Reporting;
            var monitoredItemIds = new UInt32Collection();
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is SetMonitoringModeRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ReturnsAsync(new SetMonitoringModeResponse
                {
                    ResponseHeader = new ResponseHeader
                    {
                        ServiceResult = StatusCodes.Bad
                    }
                })
                .Verifiable(Times.Once);

            // Act
            Func<Task> act = async () => await _sessionServices.SetMonitoringModeAsync(requestHeader,
                subscriptionId, monitoringMode, monitoredItemIds, ct);

            // Assert
            await act.Should().ThrowAsync<ServiceResultException>();
            _mockChannel.Verify();
        }

        [Fact]
        public async Task SetMonitoringModeAsyncShouldThrowExceptionWhenSendRequestAsyncThrowsAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var subscriptionId = 1u;
            var monitoringMode = MonitoringMode.Reporting;
            var monitoredItemIds = new UInt32Collection();
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is SetMonitoringModeRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ThrowsAsync(new IOException("Test exception"))
                .Verifiable(Times.Once);

            // Act
            Func<Task> act = async () => await _sessionServices.SetMonitoringModeAsync(requestHeader,
                subscriptionId, monitoringMode, monitoredItemIds, ct);

            // Assert
            await act.Should().ThrowAsync<IOException>().WithMessage("Test exception");
            _mockChannel.Verify();
        }

        [Fact]
        public async Task SetMonitoringModeAsyncShouldValidateResponseAndHandleDiagnosticInfoAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var subscriptionId = 1u;
            var monitoringMode = MonitoringMode.Reporting;
            var monitoredItemIds = new UInt32Collection();
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is SetMonitoringModeRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ReturnsAsync(new SetMonitoringModeResponse
                {
                    Results = new StatusCodeCollection { StatusCodes.Good },
                    DiagnosticInfos = new DiagnosticInfoCollection { new DiagnosticInfo() }
                })
                .Verifiable(Times.Once);

            // Act
            var response = await _sessionServices.SetMonitoringModeAsync(requestHeader, subscriptionId, monitoringMode, monitoredItemIds, ct);

            // Assert
            response.Should().NotBeNull();
            response.Results.Should().HaveCount(1);
            response.DiagnosticInfos.Should().HaveCount(1);
        }

        [Fact]
        public async Task SetPublishingModeAsyncShouldSimplyCallBaseMethodWhenNoLimitsSetAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var publishingEnabled = true;
            var subscriptionIds = new UInt32Collection();
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is SetPublishingModeRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ReturnsAsync(new SetPublishingModeResponse())
                .Verifiable(Times.Once);

            // Act
            var response = await _sessionServices.SetPublishingModeAsync(requestHeader,
                publishingEnabled, subscriptionIds, ct);

            // Assert
            response.Should().NotBeNull();
            requestHeader.RequestHandle.Should().NotBe(0);
            requestHeader.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            _mockChannel.Verify();
        }

        [Fact]
        public async Task SetPublishingModeAsyncShouldThrowExceptionWhenResponseContainsBadStatusCodeAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var publishingEnabled = true;
            var subscriptionIds = new UInt32Collection();
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is SetPublishingModeRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ReturnsAsync(new SetPublishingModeResponse
                {
                    ResponseHeader = new ResponseHeader
                    {
                        ServiceResult = StatusCodes.Bad
                    }
                })
                .Verifiable(Times.Once);

            // Act
            Func<Task> act = async () => await _sessionServices.SetPublishingModeAsync(requestHeader,
                publishingEnabled, subscriptionIds, ct);

            // Assert
            await act.Should().ThrowAsync<ServiceResultException>();
            _mockChannel.Verify();
        }

        [Fact]
        public async Task SetPublishingModeAsyncShouldThrowExceptionWhenSendRequestAsyncThrowsAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var publishingEnabled = true;
            var subscriptionIds = new UInt32Collection();
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is SetPublishingModeRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ThrowsAsync(new IOException("Test exception"))
                .Verifiable(Times.Once);

            // Act
            Func<Task> act = async () => await _sessionServices.SetPublishingModeAsync(requestHeader,
                publishingEnabled, subscriptionIds, ct);

            // Assert
            await act.Should().ThrowAsync<IOException>().WithMessage("Test exception");
            _mockChannel.Verify();
        }

        [Fact]
        public async Task SetTriggeringAsyncShouldBatchRequestsWhenExceedingOperationLimitsAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var subscriptionId = 1u;
            var triggeringItemId = 1u;
            var linksToAdd = new UInt32Collection(Enumerable.Repeat(1u, 15).ToList());
            var linksToRemove = new UInt32Collection(Enumerable.Repeat(1u, 15).ToList());
            var ct = CancellationToken.None;

            _sessionServices.OperationLimits.MaxMonitoredItemsPerCall = 10;

            _mockChannel
                .SetupSequence(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is SetTriggeringRequest),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SetTriggeringResponse
                {
                    AddResults = new StatusCodeCollection(
                        Enumerable.Repeat((StatusCode)StatusCodes.Good, 10).ToList())
                })
                .ReturnsAsync(new SetTriggeringResponse
                {
                    AddResults = new StatusCodeCollection(
                        Enumerable.Repeat((StatusCode)StatusCodes.Good, 5).ToList()),
                    RemoveResults = new StatusCodeCollection(
                        Enumerable.Repeat((StatusCode)StatusCodes.Good, 5).ToList())
                })
                .ReturnsAsync(new SetTriggeringResponse
                {
                    RemoveResults = new StatusCodeCollection(
                        Enumerable.Repeat((StatusCode)StatusCodes.Good, 10).ToList())
                });

            // Act
            var response = await _sessionServices.SetTriggeringAsync(requestHeader,
                subscriptionId, triggeringItemId, linksToAdd, linksToRemove, ct);

            // Assert
            response.Should().NotBeNull();
            response.AddResults.Count.Should().Be(15);
            response.RemoveResults.Count.Should().Be(15);
            _mockChannel.Verify(c => c.SendRequestAsync(It.IsAny<IServiceRequest>(),
                It.IsAny<CancellationToken>()), Times.Exactly(3));
        }

        [Fact]
        public async Task SetTriggeringAsyncShouldHandleBatchingWhenSecondOperationFailsAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var subscriptionId = 1u;
            var triggeringItemId = 1u;
            var linksToAdd = new UInt32Collection(Enumerable.Repeat(1u, 15).ToList());
            var linksToRemove = new UInt32Collection(Enumerable.Repeat(1u, 15).ToList());
            var ct = CancellationToken.None;

            _sessionServices.OperationLimits.MaxMonitoredItemsPerCall = 10;

            _mockChannel
                .SetupSequence(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is SetTriggeringRequest),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SetTriggeringResponse
                {
                    AddResults = new StatusCodeCollection(
                        Enumerable.Repeat((StatusCode)StatusCodes.Good, 10).ToList())
                })
                .ReturnsAsync(new SetTriggeringResponse
                {
                    ResponseHeader = new ResponseHeader
                    {
                        ServiceResult = StatusCodes.Bad
                    }
                });

            // Act
            Func<Task> act = async () => await _sessionServices.SetTriggeringAsync(requestHeader,
                subscriptionId, triggeringItemId, linksToAdd, linksToRemove, ct);

            // Assert
            await act.Should().ThrowAsync<ServiceResultException>();
            _mockChannel.Verify(c => c.SendRequestAsync(It.IsAny<IServiceRequest>(),
                It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task SetTriggeringAsyncShouldSimplyCallBaseMethodWhenNoLimitsSetAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var subscriptionId = 1u;
            var triggeringItemId = 1u;
            var linksToAdd = new UInt32Collection();
            var linksToRemove = new UInt32Collection();
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is SetTriggeringRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ReturnsAsync(new SetTriggeringResponse())
                .Verifiable(Times.Once);

            // Act
            var response = await _sessionServices.SetTriggeringAsync(requestHeader,
                subscriptionId, triggeringItemId, linksToAdd, linksToRemove, ct);

            // Assert
            response.Should().NotBeNull();
            requestHeader.RequestHandle.Should().NotBe(0);
            requestHeader.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            _mockChannel.Verify();
        }

        [Fact]
        public async Task SetTriggeringAsyncShouldThrowExceptionWhenResponseContainsBadStatusCodeAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var subscriptionId = 1u;
            var triggeringItemId = 1u;
            var linksToAdd = new UInt32Collection();
            var linksToRemove = new UInt32Collection();
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is SetTriggeringRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ReturnsAsync(new SetTriggeringResponse
                {
                    ResponseHeader = new ResponseHeader
                    {
                        ServiceResult = StatusCodes.Bad
                    }
                })
                .Verifiable(Times.Once);

            // Act
            Func<Task> act = async () => await _sessionServices.SetTriggeringAsync(requestHeader,
                subscriptionId, triggeringItemId, linksToAdd, linksToRemove, ct);

            // Assert
            await act.Should().ThrowAsync<ServiceResultException>();
            _mockChannel.Verify();
        }

        [Fact]
        public async Task SetTriggeringAsyncShouldThrowExceptionWhenSendRequestAsyncThrowsAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var subscriptionId = 1u;
            var triggeringItemId = 1u;
            var linksToAdd = new UInt32Collection();
            var linksToRemove = new UInt32Collection();
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is SetTriggeringRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ThrowsAsync(new IOException("Test exception"))
                .Verifiable(Times.Once);

            // Act
            Func<Task> act = async () => await _sessionServices.SetTriggeringAsync(requestHeader,
                subscriptionId, triggeringItemId, linksToAdd, linksToRemove, ct);

            // Assert
            await act.Should().ThrowAsync<IOException>().WithMessage("Test exception");
            _mockChannel.Verify();
        }

        [Fact]
        public async Task SetTriggeringAsyncShouldValidateResponseAndHandleDiagnosticInfoAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var subscriptionId = 1u;
            var triggeringItemId = 1u;
            var linksToAdd = new UInt32Collection();
            var linksToRemove = new UInt32Collection();
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is SetTriggeringRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ReturnsAsync(new SetTriggeringResponse
                {
                    AddResults = new StatusCodeCollection { StatusCodes.Good },
                    AddDiagnosticInfos = new DiagnosticInfoCollection { new DiagnosticInfo() },
                    RemoveResults = new StatusCodeCollection { StatusCodes.Good },
                    RemoveDiagnosticInfos = new DiagnosticInfoCollection { new DiagnosticInfo() }
                })
                .Verifiable(Times.Once);

            // Act
            var response = await _sessionServices.SetTriggeringAsync(requestHeader, subscriptionId, triggeringItemId, linksToAdd, linksToRemove, ct);

            // Assert
            response.Should().NotBeNull();
            response.AddResults.Should().HaveCount(1);
            response.AddDiagnosticInfos.Should().HaveCount(1);
            response.RemoveResults.Should().HaveCount(1);
            response.RemoveDiagnosticInfos.Should().HaveCount(1);
        }

        [Fact]
        public async Task TransferSubscriptionsAsyncShouldSimplyCallBaseMethodWhenNoLimitsSetAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var subscriptionIds = new UInt32Collection();
            var sendInitialValues = true;
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is TransferSubscriptionsRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ReturnsAsync(new TransferSubscriptionsResponse())
                .Verifiable(Times.Once);

            // Act
            var response = await _sessionServices.TransferSubscriptionsAsync(
                requestHeader, subscriptionIds, sendInitialValues, ct);

            // Assert
            response.Should().NotBeNull();
            requestHeader.RequestHandle.Should().NotBe(0);
            requestHeader.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            _mockChannel.Verify();
        }

        [Fact]
        public async Task TransferSubscriptionsAsyncShouldThrowExceptionWhenResponseContainsBadStatusCodeAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var subscriptionIds = new UInt32Collection();
            var sendInitialValues = true;
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is TransferSubscriptionsRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ReturnsAsync(new TransferSubscriptionsResponse
                {
                    ResponseHeader = new ResponseHeader
                    {
                        ServiceResult = StatusCodes.Bad
                    }
                })
                .Verifiable(Times.Once);

            // Act
            Func<Task> act = async () => await _sessionServices.TransferSubscriptionsAsync(
                requestHeader, subscriptionIds, sendInitialValues, ct);

            // Assert
            await act.Should().ThrowAsync<ServiceResultException>();
            _mockChannel.Verify();
        }

        [Fact]
        public async Task TransferSubscriptionsAsyncShouldThrowExceptionWhenSendRequestAsyncThrowsAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var subscriptionIds = new UInt32Collection();
            var sendInitialValues = true;
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is TransferSubscriptionsRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ThrowsAsync(new IOException("Test exception"))
                .Verifiable(Times.Once);

            // Act
            Func<Task> act = async () => await _sessionServices.TransferSubscriptionsAsync(
                requestHeader, subscriptionIds, sendInitialValues, ct);

            // Assert
            await act.Should().ThrowAsync<IOException>().WithMessage("Test exception");
            _mockChannel.Verify();
        }

        [Fact]
        public async Task TransferSubscriptionsAsyncShouldValidateResponseAndHandleDiagnosticInfoAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var subscriptionIds = new UInt32Collection();
            var sendInitialValues = true;
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is TransferSubscriptionsRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ReturnsAsync(new TransferSubscriptionsResponse
                {
                    Results = new TransferResultCollection { new TransferResult { StatusCode = StatusCodes.Good } },
                    DiagnosticInfos = new DiagnosticInfoCollection { new DiagnosticInfo() }
                })
                .Verifiable(Times.Once);

            // Act
            var response = await _sessionServices.TransferSubscriptionsAsync(requestHeader, subscriptionIds, sendInitialValues, ct);

            // Assert
            response.Should().NotBeNull();
            response.Results.Should().HaveCount(1);
            response.DiagnosticInfos.Should().HaveCount(1);
        }

        [Fact]
        public async Task TranslateBrowsePathsToNodeIdsAsyncShouldBatchRequestsWhenExceedingOperationLimitsAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var browsePaths = new BrowsePathCollection(Enumerable.Repeat(new BrowsePath(), 15).ToList());
            var ct = CancellationToken.None;

            _sessionServices.OperationLimits.MaxNodesPerTranslateBrowsePathsToNodeIds = 10;

            _mockChannel
                .SetupSequence(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is TranslateBrowsePathsToNodeIdsRequest),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TranslateBrowsePathsToNodeIdsResponse
                {
                    Results = new BrowsePathResultCollection(Enumerable.Repeat(new BrowsePathResult(), 10).ToList())
                })
                .ReturnsAsync(new TranslateBrowsePathsToNodeIdsResponse
                {
                    Results = new BrowsePathResultCollection(Enumerable.Repeat(new BrowsePathResult(), 5).ToList())
                });

            // Act
            var response = await _sessionServices.TranslateBrowsePathsToNodeIdsAsync(requestHeader, browsePaths, ct);

            // Assert
            response.Should().NotBeNull();
            response.Results.Count.Should().Be(15);
            _mockChannel.Verify(c => c.SendRequestAsync(It.IsAny<IServiceRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task TranslateBrowsePathsToNodeIdsAsyncShouldHandleBatchingWhenSecondOperationFailsAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var browsePaths = new BrowsePathCollection(Enumerable.Repeat(new BrowsePath(), 15).ToList());
            var ct = CancellationToken.None;

            _sessionServices.OperationLimits.MaxNodesPerTranslateBrowsePathsToNodeIds = 10;

            _mockChannel
                .SetupSequence(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is TranslateBrowsePathsToNodeIdsRequest),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TranslateBrowsePathsToNodeIdsResponse
                {
                    Results = new BrowsePathResultCollection(Enumerable.Repeat(new BrowsePathResult(), 10).ToList())
                })
                .ReturnsAsync(new TranslateBrowsePathsToNodeIdsResponse
                {
                    Results = new BrowsePathResultCollection(Enumerable.Repeat(new BrowsePathResult(), 5).ToList()),
                    ResponseHeader = new ResponseHeader
                    {
                        ServiceResult = StatusCodes.Bad
                    }
                });

            // Act
            Func<Task> act = async () => await _sessionServices.TranslateBrowsePathsToNodeIdsAsync(requestHeader, browsePaths, ct);

            // Assert
            await act.Should().ThrowAsync<ServiceResultException>();
            _mockChannel.Verify(c => c.SendRequestAsync(It.IsAny<IServiceRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task TranslateBrowsePathsToNodeIdsAsyncShouldSimplyCallBaseMethodWhenNoLimitsSetAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var browsePaths = new BrowsePathCollection();
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is TranslateBrowsePathsToNodeIdsRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ReturnsAsync(new TranslateBrowsePathsToNodeIdsResponse())
                .Verifiable(Times.Once);

            // Act
            var response = await _sessionServices.TranslateBrowsePathsToNodeIdsAsync(
                requestHeader, browsePaths, ct);

            // Assert
            response.Should().NotBeNull();
        }

        [Fact]
        public async Task TranslateBrowsePathsToNodeIdsAsyncShouldValidateResponseAndHandleDiagnosticInfoAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var browsePaths = new BrowsePathCollection();
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is TranslateBrowsePathsToNodeIdsRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ReturnsAsync(new TranslateBrowsePathsToNodeIdsResponse
                {
                    Results = new BrowsePathResultCollection { new BrowsePathResult { StatusCode = StatusCodes.Good } },
                    DiagnosticInfos = new DiagnosticInfoCollection { new DiagnosticInfo() }
                })
                .Verifiable(Times.Once);

            // Act
            var response = await _sessionServices.TranslateBrowsePathsToNodeIdsAsync(requestHeader, browsePaths, ct);

            // Assert
            response.Should().NotBeNull();
            response.Results.Should().HaveCount(1);
            response.DiagnosticInfos.Should().HaveCount(1);
        }

        [Fact]
        public async Task UnregisterNodesAsyncShouldBatchRequestsWhenExceedingOperationLimitsAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var nodesToUnregister = new NodeIdCollection(Enumerable.Repeat(new NodeId(), 15).ToList());
            var ct = CancellationToken.None;

            _sessionServices.OperationLimits.MaxNodesPerRegisterNodes = 10;

            _mockChannel
                .SetupSequence(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is UnregisterNodesRequest),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UnregisterNodesResponse())
                .ReturnsAsync(new UnregisterNodesResponse());

            // Act
            var response = await _sessionServices.UnregisterNodesAsync(requestHeader, nodesToUnregister, ct);

            // Assert
            response.Should().NotBeNull();
            _mockChannel.Verify(c => c.SendRequestAsync(It.IsAny<IServiceRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task UnregisterNodesAsyncShouldHandleBatchingWhenSecondOperationFailsAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var nodesToUnregister = new NodeIdCollection(Enumerable.Repeat(new NodeId(), 15).ToList());
            var ct = CancellationToken.None;

            _sessionServices.OperationLimits.MaxNodesPerRegisterNodes = 10;

            _mockChannel
                .SetupSequence(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is UnregisterNodesRequest),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UnregisterNodesResponse())
                .ReturnsAsync(new UnregisterNodesResponse
                {
                    ResponseHeader = new ResponseHeader
                    {
                        ServiceResult = StatusCodes.Bad
                    }
                });

            // Act
            Func<Task> act = async () => await _sessionServices.UnregisterNodesAsync(requestHeader, nodesToUnregister, ct);

            // Assert
            await act.Should().ThrowAsync<ServiceResultException>();
            _mockChannel.Verify(c => c.SendRequestAsync(It.IsAny<IServiceRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task UnregisterNodesAsyncShouldSimplyCallBaseMethodWhenNoLimitsSetAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var nodesToUnregister = new NodeIdCollection();
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is UnregisterNodesRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ReturnsAsync(new UnregisterNodesResponse())
                .Verifiable(Times.Once);

            // Act
            var response = await _sessionServices.UnregisterNodesAsync(
                requestHeader, nodesToUnregister, ct);

            // Assert
            response.Should().NotBeNull();
            requestHeader.RequestHandle.Should().NotBe(0);
            requestHeader.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            _mockChannel.Verify();
        }

        [Fact]
        public async Task UnregisterNodesAsyncShouldThrowExceptionWhenResponseContainsBadStatusCodeAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var nodesToUnregister = new NodeIdCollection();
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is UnregisterNodesRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ReturnsAsync(new UnregisterNodesResponse
                {
                    ResponseHeader = new ResponseHeader
                    {
                        ServiceResult = StatusCodes.Bad
                    }
                })
                .Verifiable(Times.Once);

            // Act
            Func<Task> act = async () => await _sessionServices.UnregisterNodesAsync(requestHeader,
                nodesToUnregister, ct);

            // Assert
            await act.Should().ThrowAsync<ServiceResultException>();
            _mockChannel.Verify();
        }

        [Fact]
        public async Task UnregisterNodesAsyncShouldThrowExceptionWhenSendRequestAsyncThrowsAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var nodesToUnregister = new NodeIdCollection();
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is UnregisterNodesRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ThrowsAsync(new IOException("Test exception"))
                .Verifiable(Times.Once);

            // Act
            Func<Task> act = async () => await _sessionServices.UnregisterNodesAsync(requestHeader,
                nodesToUnregister, ct);

            // Assert
            await act.Should().ThrowAsync<IOException>().WithMessage("Test exception");
            _mockChannel.Verify();
        }

        [Fact]
        public async Task WriteAsyncShouldBatchRequestsWhenExceedingOperationLimitsAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var nodesToWrite = new WriteValueCollection(Enumerable.Repeat(new WriteValue(), 15).ToList());
            var ct = CancellationToken.None;

            _sessionServices.OperationLimits.MaxNodesPerWrite = 10;

            _mockChannel
                .SetupSequence(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is WriteRequest),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WriteResponse
                {
                    Results = new StatusCodeCollection(
                    Enumerable.Repeat((StatusCode)StatusCodes.Good, 10).ToList())
                })
                .ReturnsAsync(new WriteResponse
                {
                    Results = new StatusCodeCollection(
                    Enumerable.Repeat((StatusCode)StatusCodes.Good, 5).ToList())
                });

            // Act
            var response = await _sessionServices.WriteAsync(requestHeader, nodesToWrite, ct);

            // Assert
            response.Should().NotBeNull();
            response.Results.Count.Should().Be(15);
            _mockChannel.Verify(c => c.SendRequestAsync(It.IsAny<IServiceRequest>(),
                It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task WriteAsyncShouldHandleBatchingWhenSecondOperationFailsAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var nodesToWrite = new WriteValueCollection(Enumerable.Repeat(new WriteValue(), 15).ToList());
            var ct = CancellationToken.None;

            _sessionServices.OperationLimits.MaxNodesPerWrite = 10;

            _mockChannel
                .SetupSequence(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is WriteRequest),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WriteResponse
                {
                    Results = new StatusCodeCollection(Enumerable.Repeat((StatusCode)StatusCodes.Good, 10).ToList())
                })
                .ReturnsAsync(new WriteResponse
                {
                    Results = new StatusCodeCollection(Enumerable.Repeat((StatusCode)StatusCodes.Bad, 5).ToList()),
                    ResponseHeader = new ResponseHeader
                    {
                        ServiceResult = StatusCodes.Bad
                    }
                });

            // Act
            Func<Task> act = async () => await _sessionServices.WriteAsync(requestHeader, nodesToWrite, ct);

            // Assert
            await act.Should().ThrowAsync<ServiceResultException>();
            _mockChannel.Verify(c => c.SendRequestAsync(It.IsAny<IServiceRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task WriteAsyncShouldSimplyCallBaseMethodWhenNoLimitsSetAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var nodesToWrite = new WriteValueCollection();
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is WriteRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ReturnsAsync(new WriteResponse())
                .Verifiable(Times.Once);

            // Act
            var response = await _sessionServices.WriteAsync(requestHeader,
                nodesToWrite, ct);

            // Assert
            response.Should().NotBeNull();
            requestHeader.RequestHandle.Should().NotBe(0);
            requestHeader.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            _mockChannel.Verify();
        }

        [Fact]
        public async Task WriteAsyncShouldThrowExceptionWhenResponseContainsBadStatusCodeAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var nodesToWrite = new WriteValueCollection();
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is WriteRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ReturnsAsync(new WriteResponse
                {
                    ResponseHeader = new ResponseHeader
                    {
                        ServiceResult = StatusCodes.Bad
                    }
                })
                .Verifiable(Times.Once);

            // Act
            Func<Task> act = async () => await _sessionServices.WriteAsync(requestHeader,
                nodesToWrite, ct);

            // Assert
            await act.Should().ThrowAsync<ServiceResultException>();
            _mockChannel.Verify();
        }

        [Fact]
        public async Task WriteAsyncShouldThrowExceptionWhenSendRequestAsyncThrowsAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var nodesToWrite = new WriteValueCollection();
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is WriteRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ThrowsAsync(new IOException("Test exception"))
                .Verifiable(Times.Once);

            // Act
            Func<Task> act = async () => await _sessionServices.WriteAsync(requestHeader,
                nodesToWrite, ct);

            // Assert
            await act.Should().ThrowAsync<IOException>().WithMessage("Test exception");
            _mockChannel.Verify();
        }

        [Fact]
        public async Task WriteAsyncShouldValidateResponseAndHandleDiagnosticInfoAsync()
        {
            // Arrange
            var requestHeader = new RequestHeader();
            var nodesToWrite = new WriteValueCollection();
            var ct = CancellationToken.None;

            _mockChannel
                .Setup(c => c.SendRequestAsync(
                    It.Is<IServiceRequest>(r => r is WriteRequest),
                    It.Is<CancellationToken>(t => t == ct)))
                .ReturnsAsync(new WriteResponse
                {
                    Results = new StatusCodeCollection { StatusCodes.Good },
                    DiagnosticInfos = new DiagnosticInfoCollection { new DiagnosticInfo() }
                })
                .Verifiable(Times.Once);

            // Act
            var response = await _sessionServices.WriteAsync(requestHeader, nodesToWrite, ct);

            // Assert
            response.Should().NotBeNull();
            response.Results.Should().HaveCount(1);
            response.DiagnosticInfos.Should().HaveCount(1);
        }
        private class TestSessionServices : SessionServices
        {
            public TestSessionServices(IObservability observability, ITransportChannel channel)
                : base(observability, channel)
            {
                AttachChannel(channel);
            }
        }
    }
}
