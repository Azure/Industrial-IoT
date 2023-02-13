// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Tests.Engine {
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Storage;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Tests.Utils;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher;
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.OpcUa.Publisher;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Config.Models;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Serializers;
    using Diagnostics;
    using FluentAssertions;
    using Models;
    using Moq;
    using Publisher.Engine;
    using Serializers.NewtonSoft;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Xunit;
    using System.IO;
    using System.Text;

    /// <summary>
    /// Tests the PublisherConfigService class
    /// </summary>
    public class PublisherConfigServicesTests : TempFileProviderBase {

        private readonly NewtonSoftJsonSerializer _newtonSoftJsonSerializer;
        private readonly NewtonSoftJsonSerializerRaw _newtonSoftJsonSerializerRaw;
        private readonly ILogger _logger;
        private readonly PublishedNodesJobConverter _publishedNodesJobConverter;
        private readonly Mock<IPublisherConfiguration> _configMock;
        private PublisherConfigurationService _configService;
        private readonly PublishedNodesProvider _publishedNodesProvider;
        private readonly Mock<IMessageSource> _triggerMock;
        private readonly IPublisherHost _publisher;

        /// <summary>
        /// Constructor that initializes common resources used by tests.
        /// </summary>
        public PublisherConfigServicesTests() {
            _newtonSoftJsonSerializer = new NewtonSoftJsonSerializer();
            _newtonSoftJsonSerializerRaw = new NewtonSoftJsonSerializerRaw();
            _logger = TraceLogger.Create();

            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();

            _publishedNodesJobConverter = new PublishedNodesJobConverter(_logger, _newtonSoftJsonSerializer,
                engineConfigMock.Object, clientConfignMock.Object);

            // Note that each test is responsible for setting content of _tempFile;
            Utils.CopyContent("Engine/empty_pn.json", _tempFile);

            _configMock = new Mock<IPublisherConfiguration>();
            _configMock.SetupAllProperties();
            _configMock.SetupGet(p => p.PublishedNodesFile).Returns(_tempFile);
            _configMock.SetupGet(p => p.PublishedNodesSchemaFile).Returns("Storage/publishednodesschema.json");
            _configMock.SetupGet(p => p.MaxNodesPerPublishedEndpoint).Returns(1000);
            _configMock.SetupGet(p => p.MessagingProfile).Returns(MessagingProfile.Get(
                OpcUa.Publisher.Models.MessagingMode.PubSub, OpcUa.Publisher.Models.MessageEncoding.Json));

            _publishedNodesProvider = new PublishedNodesProvider(_configMock.Object, _logger);
            _triggerMock = new Mock<IMessageSource>();
            var factoryMock = new Mock<IWriterGroupScopeFactory>();
            var writerGroup = new Mock<IWriterGroup>();
            writerGroup.SetupGet(l => l.Source).Returns(_triggerMock.Object);
            var lifetime = new Mock<IWriterGroupScope>();
            lifetime.SetupGet(l => l.WriterGroup).Returns(writerGroup.Object);
            factoryMock
                .Setup(factory => factory.Create(It.IsAny<IWriterGroupConfig>()))
                .Returns(lifetime.Object);
            _publisher = new PublisherHostService(factoryMock.Object, new Mock<IProcessIdentity>().Object, _logger);
        }

        /// <summary>
        /// This method should be called only after content of _tempFile is set.
        /// </summary>
        private void InitPublisherConfigService() {
            _configService = new PublisherConfigurationService(
                _publishedNodesJobConverter,
                _configMock.Object,
                _publisher,
                _logger,
                _publishedNodesProvider,
                _newtonSoftJsonSerializer
            );
            _configService.StartAsync().AsTask().GetAwaiter().GetResult();
        }

        [Theory]
        [InlineData("Engine/pn_2.5_legacy.json")]
        public async Task Legacy25PublishedNodesFile(string publishedNodesFile) {
            Utils.CopyContent(publishedNodesFile, _tempFile);
            InitPublisherConfigService();

            var endpoints = await _configService.GetConfiguredEndpointsAsync().ConfigureAwait(false);
            Assert.Equal(1, endpoints.Count);

            var endpoint = endpoints[0];
            Assert.Equal(endpoint.EndpointUrl, new Uri("opc.tcp://opcplc:50000"));
            Assert.Equal(endpoint.UseSecurity, false);
            Assert.Equal(endpoint.OpcAuthenticationMode, OpcAuthenticationMode.UsernamePassword);
            Assert.Equal(endpoint.OpcAuthenticationUsername, "username");
            Assert.Equal(endpoint.OpcAuthenticationPassword, null);

            endpoint.OpcAuthenticationPassword = "password";

            var nodes = await _configService.GetConfiguredNodesOnEndpointAsync(endpoint).ConfigureAwait(false);
            Assert.Equal(1, nodes.Count);
            Assert.Equal("nsu=http://microsoft.com/Opc/OpcPlc/;s=FastUInt1", nodes[0].Id);

            endpoint.OpcNodes = new List<OpcNodeModel> {
                new OpcNodeModel {
                    Id = "nsu=http://microsoft.com/Opc/OpcPlc/;s=FastUInt2",
                }
            };

            await _configService.PublishNodesAsync(endpoint).ConfigureAwait(false);

            endpoints = await _configService.GetConfiguredEndpointsAsync().ConfigureAwait(false);
            Assert.Equal(1, endpoints.Count);

            endpoint.OpcNodes = null;
            nodes = await _configService.GetConfiguredNodesOnEndpointAsync(endpoint).ConfigureAwait(false);
            Assert.Equal(2, nodes.Count);
            Assert.Equal("nsu=http://microsoft.com/Opc/OpcPlc/;s=FastUInt1", nodes[0].Id);
            Assert.Equal("nsu=http://microsoft.com/Opc/OpcPlc/;s=FastUInt2", nodes[1].Id);

            // Simulate restart.
            _configService.Dispose();
            _configService = null;
            InitPublisherConfigService();

            // We should get the same endpoint and nodes after restart.
            endpoints = await _configService.GetConfiguredEndpointsAsync().ConfigureAwait(false);
            Assert.Equal(1, endpoints.Count);

            endpoint = endpoints[0];
            Assert.Equal(endpoint.EndpointUrl, new Uri("opc.tcp://opcplc:50000"));
            Assert.Equal(endpoint.UseSecurity, false);
            Assert.Equal(endpoint.OpcAuthenticationMode, OpcAuthenticationMode.UsernamePassword);
            Assert.Equal(endpoint.OpcAuthenticationUsername, "username");
            Assert.Equal(endpoint.OpcAuthenticationPassword, null);

            endpoint.OpcAuthenticationPassword = "password";

            nodes = await _configService.GetConfiguredNodesOnEndpointAsync(endpoint).ConfigureAwait(false);
            Assert.Equal(2, nodes.Count);
            Assert.Equal("nsu=http://microsoft.com/Opc/OpcPlc/;s=FastUInt1", nodes[0].Id);
            Assert.Equal("nsu=http://microsoft.com/Opc/OpcPlc/;s=FastUInt2", nodes[1].Id);
        }

        [Theory]
        [InlineData("Engine/pn_2.5_legacy_error.json", false)]
        [InlineData("Engine/pn_2.5_legacy_error.json", true)]
        public async Task Legacy25PublishedNodesFileError(string publishedNodesFile, bool useSchemaValidation) {
            if (!useSchemaValidation) {
                _configMock.SetupGet(m => m.PublishedNodesSchemaFile).Returns((string)null);
            }

            Utils.CopyContent(publishedNodesFile, _tempFile);
            InitPublisherConfigService();

            // Transformation of published nodes entries should throw a serialization error since
            // Engine/pn_2.5_legacy_error.json contains both NodeId and OpcNodes.
            // So as a result, we should end up with zero endpoints.
            var endpoints = await _configService.GetConfiguredEndpointsAsync().ConfigureAwait(false);
            Assert.Equal(0, endpoints.Count);
        }

        [Theory]
        [InlineData("Engine/pn_assets.json")]
        [InlineData("Engine/pn_assets_with_optional_fields.json")]
        public void Test_PnJson_With_Multiple_Jobs_Expect_DifferentJobIds(string publishedNodesFile) {
            Utils.CopyContent(publishedNodesFile, _tempFile);
            InitPublisherConfigService();
            Assert.Equal(2, _publisher.WriterGroups.Count());
        }

        [Fact]
        public async Task Test_SerializableExceptionResponse() {
            InitPublisherConfigService();

            var exceptionResponse = $"{{\"Message\":\"Response 400 null request is provided\",\"Details\":{{}}}}";

            // Check null request.
            await FluentActions
                .Invoking(async () => await _configService
                    .PublishNodesAsync(null)
                    .ConfigureAwait(false))
                .Should()
                .ThrowAsync<MethodCallStatusException>()
                .WithMessage(exceptionResponse)
                .ConfigureAwait(false);

            // empty description
            var exceptionModel = new MethodCallStatusExceptionModel {
                Message = "Response 400 null request is provided",
                Details = "{}",
            };
            var serializeExceptionModel = _newtonSoftJsonSerializerRaw.SerializeToString(exceptionModel);
            serializeExceptionModel.Should().BeEquivalentTo(exceptionResponse);

            var numberOfEndpoints = 1;
            var opcNodes = Enumerable.Range(0, numberOfEndpoints)
                .Select(i => new OpcNodeModel {
                    Id = $"nsu=http://microsoft.com/Opc/OpcPlc/;s=FastUInt{i}",
                })
                .ToList();

            var endpoints = Enumerable.Range(0, numberOfEndpoints)
                .Select(i => GenerateEndpoint(i, opcNodes, false))
                .ToList();

            await _configService.PublishNodesAsync(endpoints[0]).ConfigureAwait(false);

            exceptionResponse = $"{{\"Message\":\"Response 404 Nodes not found\"," +
                $"\"Details\":{{\"DataSetWriterId\":\"DataSetWriterId0\"," +
                $"\"DataSetWriterGroup\":\"DataSetWriterGroup\"," +
                $"\"EndpointUrl\":\"opc.tcp://opcplc:50000\"," +
                $"\"UseSecurity\":false,\"OpcAuthenticationMode\":\"anonymous\"," +
                $"\"OpcNodes\":[{{\"Id\":\"nsu=http://microsoft.com/Opc/OpcPlc/;s=SlowUInt0\",\"OpcPublishingIntervalTimespan\":\"00:00:01\"}}]}}}}";

            var opcNodes1 = Enumerable.Range(0, numberOfEndpoints)
                .Select(i => new OpcNodeModel {
                    Id = $"nsu=http://microsoft.com/Opc/OpcPlc/;s=SlowUInt{i}",
                })
                .ToList();
            var endpointsToDelete = Enumerable.Range(0, numberOfEndpoints)
                .Select(i => GenerateEndpoint(i, opcNodes1, false))
                .ToList();

            // try to unpublish a not published nodes.
            await FluentActions
                .Invoking(async () => await _configService
                    .UnpublishNodesAsync(endpointsToDelete[0])
                    .ConfigureAwait(false))
                .Should()
                .ThrowAsync<MethodCallStatusException>()
                .WithMessage(exceptionResponse)
                .ConfigureAwait(false);

            // Details equal to a json string
            exceptionModel = new MethodCallStatusExceptionModel {
                Message = "Response 404 Nodes not found",
                Details = $"{{\"DataSetWriterId\":\"DataSetWriterId0\"," +
                    $"\"DataSetWriterGroup\":\"DataSetWriterGroup\"," +
                    $"\"EndpointUrl\":\"opc.tcp://opcplc:50000\"," +
                    $"\"UseSecurity\":false,\"OpcAuthenticationMode\":\"anonymous\"," +
                    $"\"OpcNodes\":[{{\"Id\":\"nsu=http://microsoft.com/Opc/OpcPlc/;s=SlowUInt0\",\"OpcPublishingIntervalTimespan\":\"00:00:01\"}}]}}",
            };
            serializeExceptionModel = _newtonSoftJsonSerializerRaw.SerializeToString(exceptionModel);
            serializeExceptionModel.Should().BeEquivalentTo(exceptionResponse);

            // test for null payload
            exceptionResponse = $"{{\"Message\":\"Response 400 \",\"Details\":null}}";
            FluentActions.Invoking(
                    () => throw new MethodCallStatusException(null, 400))
                    .Should()
                    .Throw<MethodCallStatusException>()
                    .WithMessage(exceptionResponse);

            exceptionModel = new MethodCallStatusExceptionModel {
                Message = "Response 400 "
            };
            serializeExceptionModel = _newtonSoftJsonSerializerRaw.SerializeToString(exceptionModel);
            serializeExceptionModel.Should().BeEquivalentTo(exceptionResponse);
        }

        [Fact]
        public async Task Test_PublishNodes_NullOrEmpty() {
            InitPublisherConfigService();

            // Check null request.
            await FluentActions
                .Invoking(async () => await _configService
                    .PublishNodesAsync(null)
                    .ConfigureAwait(false))
                .Should()
                .ThrowAsync<MethodCallStatusException>()
                .WithMessage($"{{\"Message\":\"Response 400 null request is provided\",\"Details\":{{}}}}")
                .ConfigureAwait(false);

            var request = new PublishedNodesEntryModel {
                EndpointUrl = new Uri("opc.tcp://opcplc:50000"),
            };

            // Check null OpcNodes in request.
            await FluentActions
                .Invoking(async () => await _configService
                    .PublishNodesAsync(request)
                    .ConfigureAwait(false))
                .Should()
                .ThrowAsync<MethodCallStatusException>()
                .WithMessage($"{{\"Message\":\"Response 400 null or empty OpcNodes is provided in request\",\"Details\":{{}}}}")
                .ConfigureAwait(false);

            request.OpcNodes = new List<OpcNodeModel>();

            // Check empty OpcNodes in request.
            await FluentActions
                .Invoking(async () => await _configService
                    .PublishNodesAsync(request)
                    .ConfigureAwait(false))
                .Should()
                .ThrowAsync<MethodCallStatusException>()
                .WithMessage($"{{\"Message\":\"Response 400 null or empty OpcNodes is provided in request\",\"Details\":{{}}}}")
                .ConfigureAwait(false);
        }

        [Fact]
        public async Task Test_UnpublishNodes_NullRequest() {
            InitPublisherConfigService();

            // Check null request.
            await FluentActions
                .Invoking(async () => await _configService
                    .UnpublishNodesAsync(null)
                    .ConfigureAwait(false))
                .Should()
                .ThrowAsync<MethodCallStatusException>()
                .WithMessage($"{{\"Message\":\"Response 400 null request is provided\",\"Details\":{{}}}}")
                .ConfigureAwait(false);
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public async Task Test_UnpublishNodes_NullOrEmptyOpcNodes(
            bool useEmptyOpcNodes,
            bool customEndpoint) {

            InitPublisherConfigService();

            var numberOfEndpoints = 3;
            var opcNodes = Enumerable.Range(0, numberOfEndpoints)
                .Select(i => new OpcNodeModel {
                    Id = $"nsu=http://microsoft.com/Opc/OpcPlc/;s=FastUInt{i}",
                })
                .ToList();

            var endpoints = Enumerable.Range(0, numberOfEndpoints)
                .Select(i => GenerateEndpoint(i, opcNodes, customEndpoint))
                .ToList();

            await _configService.PublishNodesAsync(endpoints[0]).ConfigureAwait(false);
            await _configService.PublishNodesAsync(endpoints[1]).ConfigureAwait(false);
            await _configService.PublishNodesAsync(endpoints[2]).ConfigureAwait(false);

            endpoints[1] = GenerateEndpoint(1, opcNodes, customEndpoint);
            endpoints[1].OpcNodes = useEmptyOpcNodes
                ? new List<OpcNodeModel>()
                : null;

            // Check null or empty OpcNodes in request.
            await FluentActions
                .Invoking(async () => await _configService
                    .UnpublishNodesAsync(endpoints[1])
                    .ConfigureAwait(false))
                .Should()
                .NotThrowAsync()
                .ConfigureAwait(false);

            var configuredEndpoints = await _configService
                .GetConfiguredEndpointsAsync().ConfigureAwait(false);

            Assert.Equal(2, configuredEndpoints.Count);

            Assert.True(endpoints[0].HasSameDataSet(configuredEndpoints[0]));
            Assert.True(endpoints[2].HasSameDataSet(configuredEndpoints[1]));
        }

        [Fact]
        public async Task Test_GetConfiguredNodesOnEndpoint_NullRequest() {
            InitPublisherConfigService();

            // Check call with null.
            await FluentActions
                .Invoking(async () => await _configService
                    .GetConfiguredNodesOnEndpointAsync(null)
                    .ConfigureAwait(false))
                .Should()
                .ThrowAsync<MethodCallStatusException>()
                .WithMessage($"{{\"Message\":\"Response 400 null request is provided\",\"Details\":{{}}}}")
                .ConfigureAwait(false);
        }

        [Fact]
        public async Task Test_AddOrUpdateEndpoints_NullRequest() {
            InitPublisherConfigService();

            // Check call with null.
            await FluentActions
                .Invoking(async () => await _configService
                    .AddOrUpdateEndpointsAsync(null)
                    .ConfigureAwait(false))
                .Should()
                .ThrowAsync<MethodCallStatusException>()
                .WithMessage($"{{\"Message\":\"Response 400 null request is provided\",\"Details\":{{}}}}")
                .ConfigureAwait(false);
        }

        [Fact]
        public async Task Test_AddOrUpdateEndpoints_MultipleEndpointEntries() {
            InitPublisherConfigService();

            var numberOfEndpoints = 3;
            var opcNodes = Enumerable.Range(0, numberOfEndpoints)
                .Select(i => new OpcNodeModel {
                    Id = $"nsu=http://microsoft.com/Opc/OpcPlc/;s=FastUInt{i}",
                })
                .ToList();

            var endpoints = Enumerable.Range(0, numberOfEndpoints)
                .Select(i => GenerateEndpoint(i, opcNodes, false))
                .ToList();

            // Make endpoint at index 0 and 2 the same.
            endpoints[2].DataSetWriterId = endpoints[0].DataSetWriterId;
            endpoints[2].DataSetWriterGroup = endpoints[0].DataSetWriterGroup;
            endpoints[2].DataSetPublishingInterval = endpoints[0].DataSetPublishingInterval;

            // The call should throw an exception.
            await FluentActions
                .Invoking(async () => await _configService
                    .AddOrUpdateEndpointsAsync(endpoints)
                    .ConfigureAwait(false))
                .Should()
                .ThrowAsync<MethodCallStatusException>()
                .WithMessage($"{{\"Message\":\"Response 400 Request contains two entries for the same endpoint at index 0 and 2\",\"Details\":{{}}}}")
                .ConfigureAwait(false);
        }

        [Fact]
        public async Task Test_AddOrUpdateEndpoints_MultipleEndpointEntries_Timesapn() {
            InitPublisherConfigService();

            var numberOfEndpoints = 3;
            var opcNodes = Enumerable.Range(0, numberOfEndpoints)
                .Select(i => new OpcNodeModel {
                    Id = $"nsu=http://microsoft.com/Opc/OpcPlc/;s=FastUInt{i}",
                })
                .ToList();

            var endpoints = Enumerable.Range(0, numberOfEndpoints)
                .Select(i => GenerateEndpoint(i, opcNodes, false))
                .ToList();

            endpoints.ForEach(e =>
                e.DataSetPublishingIntervalTimespan =
                TimeSpan.FromMilliseconds(e.DataSetPublishingInterval.GetValueOrDefault(1000)));

            // Make endpoint at index 0 and 2 the same.
            endpoints[2].DataSetWriterId = endpoints[0].DataSetWriterId;
            endpoints[2].DataSetWriterGroup = endpoints[0].DataSetWriterGroup;
            endpoints[2].DataSetPublishingIntervalTimespan = endpoints[0].DataSetPublishingIntervalTimespan;

            // The call should throw an exception.
            await FluentActions
                .Invoking(async () => await _configService
                    .AddOrUpdateEndpointsAsync(endpoints)
                    .ConfigureAwait(false))
                .Should()
                .ThrowAsync<MethodCallStatusException>()
                .WithMessage($"{{\"Message\":\"Response 400 Request contains two entries for the same endpoint at index 0 and 2\",\"Details\":{{}}}}")
                .ConfigureAwait(false);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Test_AddOrUpdateEndpoints_AddEndpoints(
            bool useDataSetSpecificEndpoints
        ) {
            _configMock.SetupGet(m => m.MaxNodesPerPublishedEndpoint).Returns(2);

            InitPublisherConfigService();

            Assert.Empty(_publisher.WriterGroups);

            var numberOfEndpoints = 3;

            var opcNodes = Enumerable.Range(0, numberOfEndpoints)
                .Select(i => new OpcNodeModel {
                    Id = $"nsu=http://microsoft.com/Opc/OpcPlc/;s=FastUInt{i}",
                })
                .ToList();

            var endpoints = Enumerable.Range(0, numberOfEndpoints)
                .Select(i => GenerateEndpoint(i, opcNodes, useDataSetSpecificEndpoints))
                .ToList();

            var tasks = new List<Task>();
            for (var i = 0; i < numberOfEndpoints; i++) {
                tasks.Add(_configService.AddOrUpdateEndpointsAsync(
                    new List<PublishedNodesEntryModel> { endpoints[i] }));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);

            for (var i = 0; i < numberOfEndpoints; i++) {
                var endpointNodes = await _configService
                    .GetConfiguredNodesOnEndpointAsync(endpoints[i])
                    .ConfigureAwait(false);

                AssertSameNodes(endpoints[i], endpointNodes);
            }

            Assert.Equal(1, _publisher.WriterGroups.Count());
        }

        [Theory]
        [InlineData("Engine/pn_events.json")]
        [InlineData("Engine/pn_pending_alarms.json")]
        [InlineData("Engine/empty_pn.json")]
        [InlineData("Engine/pn_assets.json")]
        [InlineData("Engine/pn_assets_with_optional_fields.json")]
        [InlineData("Engine/publishednodes.json")]
        [InlineData("Engine/publishednodeswithoptionalfields.json")]
        [InlineData("Engine/publishednodes_with_duplicates.json")]
        [InlineData("Controller/DmApiPayloadCollection.json")]
        [InlineData("Controller/DmApiPayloadTwoEndpoints.json")]
        public async Task Test_AddOrUpdateEndpoints_RemoveEndpoints(string publishedNodesFile) {
            Utils.CopyContent(publishedNodesFile, _tempFile);
            InitPublisherConfigService();

            string payload = Utils.GetFileContent(publishedNodesFile);
            var payloadRequests = _newtonSoftJsonSerializer.Deserialize<List<PublishedNodesEntryModel>>(payload);

            int index = 0;
            foreach (var request in payloadRequests) {
                request.OpcNodes = index % 2 == 0
                    ? null
                    : new List<OpcNodeModel>();
                ++index;

                var shouldThrow = !_publishedNodesJobConverter.ToPublishedNodes(0, default, _publisher.WriterGroups)
                    .Select(p => p.PropagatePublishingIntervalToNodes())
                    .Any(dataSet => dataSet.HasSameDataSet(request.PropagatePublishingIntervalToNodes()));
                if (shouldThrow) {
                    await FluentActions
                        .Invoking(async () => await _configService
                            .AddOrUpdateEndpointsAsync(new List<PublishedNodesEntryModel> { request })
                            .ConfigureAwait(false))
                        .Should()
                        .ThrowAsync<MethodCallStatusException>()
                        .WithMessage($"{{\"Message\":\"Response 404 Endpoint not found: {request.EndpointUrl}\",\"Details\":{{}}}}")
                        .ConfigureAwait(false);
                }
                else {
                    await FluentActions
                        .Invoking(async () => await _configService
                            .AddOrUpdateEndpointsAsync(new List<PublishedNodesEntryModel> { request })
                            .ConfigureAwait(false))
                        .Should()
                        .NotThrowAsync()
                        .ConfigureAwait(false);
                }
            }

            var configuredEndpoints = await _configService
                .GetConfiguredEndpointsAsync()
                .ConfigureAwait(false);
            Assert.Equal(0, configuredEndpoints.Count);
        }

        [Fact]
        public async Task Test_AddOrUpdateEndpoints_AddAndRemove() {
            _configMock.SetupGet(m => m.MaxNodesPerPublishedEndpoint).Returns(2);

            InitPublisherConfigService();

            Assert.Empty(_publisher.WriterGroups);

            var opcNodes = Enumerable.Range(0, 5)
                .Select(i => new OpcNodeModel {
                    Id = $"nsu=http://microsoft.com/Opc/OpcPlc/;s=FastUInt{i}",
                })
                .ToList();

            var endpoints = Enumerable.Range(0, 5)
                .Select(i => GenerateEndpoint(i, opcNodes))
                .ToList();

            var tasks = new List<Task>();
            for (var i = 0; i < 3; i++) {
                tasks.Add(_configService.PublishNodesAsync(endpoints[i]));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);

            for (var i = 0; i < 3; i++) {
                var endpointNodes = await _configService
                    .GetConfiguredNodesOnEndpointAsync(endpoints[i])
                    .ConfigureAwait(false);

                AssertSameNodes(endpoints[i], endpointNodes);
            }

            // Helper method.
            async Task AssertGetConfiguredNodesOnEndpointThrows(
                PublisherConfigurationService publisherConfigurationService,
                PublishedNodesEntryModel endpoint
            ) {
                await FluentActions
                    .Invoking(async () => await publisherConfigurationService
                        .GetConfiguredNodesOnEndpointAsync(endpoint)
                        .ConfigureAwait(false))
                    .Should()
                    .ThrowAsync<MethodCallStatusException>()
                    .WithMessage($"{{\"Message\":\"Response 404 Endpoint not found: {endpoint.EndpointUrl}\",\"Details\":{{}}}}")
                    .ConfigureAwait(false);
            }

            // Those calls should throw.
            for (var i = 3; i < 5; i++) {
                await AssertGetConfiguredNodesOnEndpointThrows(_configService, endpoints[i])
                    .ConfigureAwait(false);
            }

            var updateRequest = Enumerable.Range(0, 5)
                .Select(i => GenerateEndpoint(i, opcNodes))
                .ToList();
            updateRequest[0].OpcNodes = null; // Should result in endpoint deletion.
            updateRequest[1].OpcNodes = new List<OpcNodeModel>(); // Should result in endpoint deletion.
            updateRequest[2].OpcNodes = opcNodes.GetRange(0, 4).ToList();
            updateRequest[3].OpcNodes = null; // Should throw as endpoint does not exist.

            // Should throw as updateRequest[3] endpoint is not present in current configuratoin.
            await FluentActions
                .Invoking(async () => await _configService
                    .AddOrUpdateEndpointsAsync(updateRequest)
                    .ConfigureAwait(false))
                .Should()
                .ThrowAsync<MethodCallStatusException>()
                .WithMessage($"{{\"Message\":\"Response 404 Endpoint not found: {updateRequest[3].EndpointUrl}\",\"Details\":{{}}}}")
                .ConfigureAwait(false);

            updateRequest.RemoveAt(3);
            await _configService.AddOrUpdateEndpointsAsync(updateRequest).ConfigureAwait(false);

            // Check endpoint 0.
            await AssertGetConfiguredNodesOnEndpointThrows(_configService, endpoints[0])
                .ConfigureAwait(false);

            // Check endpoint 1.
            await AssertGetConfiguredNodesOnEndpointThrows(_configService, endpoints[1])
                .ConfigureAwait(false);

            // Check endpoint 2.
            var endpointNodes2 = await _configService
                .GetConfiguredNodesOnEndpointAsync(endpoints[2])
                .ConfigureAwait(false);

            AssertSameNodes(updateRequest[2], endpointNodes2);

            // Check endpoint 3.
            await AssertGetConfiguredNodesOnEndpointThrows(_configService, endpoints[3])
                .ConfigureAwait(false);

            // Check endpoint 4.
            var endpointNodes4 = await _configService
                .GetConfiguredNodesOnEndpointAsync(endpoints[4])
                .ConfigureAwait(false);

            AssertSameNodes(updateRequest[3], endpointNodes4);
        }

        [Theory]
        [InlineData("Engine/pn_opc_nodes_empty.json")]
        [InlineData("Engine/pn_opc_nodes_null.json")]
        [InlineData("Engine/pn_opc_nodes_empty_and_null.json")]
        public async Task Test_InitStandaloneJobOrchestratorFromEmptyOpcNodes(string publishedNodesFile) {
            Utils.CopyContent(publishedNodesFile, _tempFile);
            InitPublisherConfigService();

            // Engine/empty_opc_nodes.json contains entries with null or empty OpcNodes.
            // Those entries should not result in any endpoint entries in publisherConfigurationService.
            var configuredEndpoints = await _configService
                .GetConfiguredEndpointsAsync()
                .ConfigureAwait(false);
            Assert.Equal(0, configuredEndpoints.Count);

            // There should also not be any job entries.
            Assert.Empty(_publisher.WriterGroups);
        }

        [Theory]
        [InlineData("Engine/pn_assets_with_optional_fields.json")]
        public async Task OptionalFieldsPublishedNodesFile(string publishedNodesFile) {
            Utils.CopyContent(publishedNodesFile, _tempFile);
            InitPublisherConfigService();

            var endpoints = await _configService.GetConfiguredEndpointsAsync().ConfigureAwait(false);
            Assert.Equal(2, endpoints.Count);

            Assert.Equal(endpoints[0].DataSetWriterGroup, "Leaf0");
            Assert.Equal(endpoints[0].EndpointUrl, new Uri("opc.tcp://opcplc:50000"));
            Assert.Equal(endpoints[0].UseSecurity, false);
            Assert.Equal(endpoints[0].OpcAuthenticationMode, OpcAuthenticationMode.Anonymous);
            Assert.Equal(endpoints[0].DataSetWriterId, "Leaf0_10000_3085991c-b85c-4311-9bfb-a916da952234");
            Assert.Equal(endpoints[0].DataSetName, "Tag_Leaf0_10000_3085991c-b85c-4311-9bfb-a916da952234");

            Assert.Equal(endpoints[1].DataSetWriterGroup, "Leaf1");
            Assert.Equal(endpoints[1].EndpointUrl, new Uri("opc.tcp://opcplc:50000"));
            Assert.Equal(endpoints[1].UseSecurity, false);
            Assert.Equal(endpoints[1].OpcAuthenticationMode, OpcAuthenticationMode.UsernamePassword);
            Assert.Equal(endpoints[1].DataSetWriterId, "Leaf1_10000_2e4fc28f-ffa2-4532-9f22-378d47bbee5d");
            Assert.Equal(endpoints[1].DataSetName, "Tag_Leaf1_10000_2e4fc28f-ffa2-4532-9f22-378d47bbee5d");
            endpoints[0].OpcNodes = null;
            var nodes = await _configService.GetConfiguredNodesOnEndpointAsync(endpoints[0]).ConfigureAwait(false);
            Assert.Equal(1, nodes.Count);
            Assert.Equal("nsu=http://microsoft.com/Opc/OpcPlc/;s=StepUp", nodes[0].Id);

            endpoints[0].OpcNodes = new List<OpcNodeModel> {
                new OpcNodeModel {
                    Id = "nsu=http://microsoft.com/Opc/OpcPlc/;s=FastUInt2",
                }
            };

            await _configService.PublishNodesAsync(endpoints[0]).ConfigureAwait(false);

            endpoints = await _configService.GetConfiguredEndpointsAsync().ConfigureAwait(false);
            Assert.Equal(1, endpoints.Count);

            endpoints[0].OpcNodes = null;
            nodes = await _configService.GetConfiguredNodesOnEndpointAsync(endpoints[0]).ConfigureAwait(false);
            Assert.Equal(2, nodes.Count);
            Assert.Equal("nsu=http://microsoft.com/Opc/OpcPlc/;s=StepUp", nodes[0].Id);
            Assert.Equal("nsu=http://microsoft.com/Opc/OpcPlc/;s=FastUInt2", nodes[1].Id);

            // Simulate restart.
            _configService.Dispose();
            _configService = null;
            InitPublisherConfigService();

            // We should get the same endpoint and nodes after restart.
            endpoints = await _configService.GetConfiguredEndpointsAsync().ConfigureAwait(false);
            Assert.Equal(1, endpoints.Count);

            Assert.Equal(endpoints[0].DataSetWriterGroup, "Leaf0");
            Assert.Equal(endpoints[0].EndpointUrl, new Uri("opc.tcp://opcplc:50000"));
            Assert.Equal(endpoints[0].UseSecurity, false);
            Assert.Equal(endpoints[0].OpcAuthenticationMode, OpcAuthenticationMode.Anonymous);
            Assert.Equal(endpoints[0].DataSetWriterId, "Leaf0_10000_3085991c-b85c-4311-9bfb-a916da952234");
            Assert.Equal(endpoints[0].DataSetName, "Tag_Leaf0_10000_3085991c-b85c-4311-9bfb-a916da952234");

            nodes = await _configService.GetConfiguredNodesOnEndpointAsync(endpoints[0]).ConfigureAwait(false);
            Assert.Equal(2, nodes.Count);
            Assert.Equal("nsu=http://microsoft.com/Opc/OpcPlc/;s=StepUp", nodes[0].Id);
            Assert.Equal("nsu=http://microsoft.com/Opc/OpcPlc/;s=FastUInt2", nodes[1].Id);
        }

        [Theory]
        [InlineData("Engine/publishednodes.json")]
        [InlineData("Engine/publishednodeswithoptionalfields.json")]
        [InlineData("Engine/pn_assets.json")]
        [InlineData("Engine/pn_assets_with_optional_fields.json")]
        [InlineData("Engine/pn_events.json")]
        [InlineData("Engine/pn_pending_alarms.json")]
        public async Task PublishNodesOnEmptyConfiguration(string publishedNodesFile) {
            Utils.CopyContent("Engine/empty_pn.json", _tempFile);
            InitPublisherConfigService();

            string payload = Utils.GetFileContent(publishedNodesFile);
            var publishNodesRequest = _newtonSoftJsonSerializer.Deserialize<List<PublishedNodesEntryModel>>(payload);

            foreach (var request in publishNodesRequest) {
                await FluentActions
                    .Invoking(async () => await _configService.PublishNodesAsync(request).ConfigureAwait(false))
                    .Should()
                    .NotThrowAsync()
                    .ConfigureAwait(false);
            }

            _publisher.WriterGroups.Count()
                .Should()
                .Be(1);
        }

        [Theory]
        [InlineData("Engine/publishednodes.json", "Engine/publishednodeswithoptionalfields.json")]
        [InlineData("Engine/publishednodeswithoptionalfields.json", "Engine/publishednodes.json")]
        [InlineData("Engine/pn_assets.json", "Engine/pn_assets_with_optional_fields.json")]
        [InlineData("Engine/pn_assets_with_optional_fields.json", "Engine/pn_assets.json")]
        [InlineData("Engine/pn_events.json", "Engine/pn_pending_alarms.json")]
        public async Task PublishNodesOnExistingConfiguration(string existingConfig, string newConfig) {
            Utils.CopyContent(existingConfig, _tempFile);
            InitPublisherConfigService();

            string payload = Utils.GetFileContent(newConfig);
            var publishNodesRequest = _newtonSoftJsonSerializer.Deserialize<List<PublishedNodesEntryModel>>(payload);

            foreach (var request in publishNodesRequest) {
                await FluentActions
                    .Invoking(async () => await _configService.PublishNodesAsync(request).ConfigureAwait(false))
                    .Should()
                    .NotThrowAsync()
                    .ConfigureAwait(false);
            }

            _publisher.WriterGroups.Count()
                .Should()
                .Be(1);
        }

        [Theory]
        [InlineData("Engine/publishednodes.json", "Engine/pn_assets.json")]
        [InlineData("Engine/publishednodeswithoptionalfields.json", "Engine/pn_assets_with_optional_fields.json")]
        [InlineData("Engine/pn_assets.json", "Engine/publishednodes.json")]
        [InlineData("Engine/pn_assets_with_optional_fields.json", "Engine/publishednodeswithoptionalfields.json")]
        public async Task PublishNodesOnNewConfiguration(string existingConfig, string newConfig) {
            Utils.CopyContent(existingConfig, _tempFile);
            InitPublisherConfigService();

            string payload = Utils.GetFileContent(newConfig);
            var publishNodesRequest = _newtonSoftJsonSerializer.Deserialize<List<PublishedNodesEntryModel>>(payload);

            foreach (var request in publishNodesRequest) {
                await FluentActions
                    .Invoking(async () => await _configService.PublishNodesAsync(request).ConfigureAwait(false))
                    .Should()
                    .NotThrowAsync()
                    .ConfigureAwait(false);
            }

            _publisher.WriterGroups.Count()
                .Should()
                .Be(1);
        }

        [Theory]
        [InlineData("Engine/publishednodes.json")]
        [InlineData("Engine/publishednodeswithoptionalfields.json")]
        [InlineData("Engine/pn_assets.json")]
        [InlineData("Engine/pn_assets_with_optional_fields.json")]
        public async Task UnpublishNodesOnExistingConfiguration(string publishedNodesFile) {
            Utils.CopyContent(publishedNodesFile, _tempFile);
            InitPublisherConfigService();

            string payload = Utils.GetFileContent(publishedNodesFile);
            var unpublishNodesRequest = _newtonSoftJsonSerializer.Deserialize<List<PublishedNodesEntryModel>>(payload);

            foreach (var request in unpublishNodesRequest) {
                await FluentActions
                    .Invoking(async () => await _configService.UnpublishNodesAsync(request).ConfigureAwait(false))
                    .Should()
                    .NotThrowAsync()
                    .ConfigureAwait(false);
            }

            _publisher.WriterGroups.Count()
                .Should()
                .Be(0);
        }

        [Theory]
        [InlineData("Engine/publishednodes.json", "Engine/pn_assets.json")]
        [InlineData("Engine/publishednodeswithoptionalfields.json", "Engine/pn_assets_with_optional_fields.json")]
        [InlineData("Engine/pn_assets.json", "Engine/publishednodes.json")]
        [InlineData("Engine/pn_assets_with_optional_fields.json", "Engine/publishednodeswithoptionalfields.json")]
        public async Task UnpublishNodesOnNonExistingConfiguration(string existingConfig, string newConfig) {
            Utils.CopyContent(existingConfig, _tempFile);
            InitPublisherConfigService();

            string payload = Utils.GetFileContent(newConfig);
            var unpublishNodesRequest = _newtonSoftJsonSerializer.Deserialize<List<PublishedNodesEntryModel>>(payload);

            foreach (var request in unpublishNodesRequest) {
                await FluentActions
                    .Invoking(async () => await _configService.UnpublishNodesAsync(request).ConfigureAwait(false))
                    .Should()
                    .ThrowAsync<MethodCallStatusException>()
                    .WithMessage($"{{\"Message\":\"Response 404 Endpoint not found: {request.EndpointUrl}\",\"Details\":{{}}}}")
                    .ConfigureAwait(false);
            }

            _publisher.WriterGroups.Sum(g => g.WriterGroup.DataSetWriters.Count)
                .Should()
                .Be(2);
        }

        [Theory]
        [InlineData(2, 10)]
        // [InlineData(100, 1000)]
        public async Task PublishNodesStressTest(int numberOfEndpoints, int numberOfNodes) {
            using (var fileStream = new FileStream(_tempFile, FileMode.Open, FileAccess.Write)) {
                fileStream.Write(Encoding.UTF8.GetBytes("[]"));
            }
            InitPublisherConfigService();

            var payload = new List<PublishedNodesEntryModel>();
            for (int endpointIndex = 0; endpointIndex < numberOfEndpoints; ++endpointIndex) {
                var model = new PublishedNodesEntryModel {
                    EndpointUrl = new Uri($"opc.tcp://server{endpointIndex}:49580"),
                };

                model.OpcNodes = new List<OpcNodeModel>();
                for (var nodeIndex = 0; nodeIndex < numberOfNodes; ++nodeIndex) {
                    model.OpcNodes.Add(new OpcNodeModel {
                        Id = $"ns=2;s=Node-Server-{nodeIndex}",
                    });
                }

                payload.Add(model);
            }

            // Publish all nodes.
            foreach (var request in payload) {
                await FluentActions
                    .Invoking(async () => await _configService.PublishNodesAsync(request).ConfigureAwait(false))
                    .Should()
                    .NotThrowAsync()
                    .ConfigureAwait(false);
            }

            void CheckEndpointsAndNodes(
                int expectedNumberOfEndpoints,
                int expectedNumberOfNodesPerEndpoint
            ) {
                var writerGroups = _publisher.WriterGroups;
                writerGroups
                    .SelectMany(jobModel => jobModel.WriterGroup.DataSetWriters)
                    .Count(v => v.DataSet.DataSetSource.PublishedVariables.PublishedData.Count == expectedNumberOfNodesPerEndpoint)
                    .Should()
                    .Be(expectedNumberOfEndpoints);
            }

            // Check
            CheckEndpointsAndNodes(numberOfEndpoints, numberOfNodes);

            // Publish one more node for each endpoint.
            var payloadDiff = new List<PublishedNodesEntryModel>();
            for (int endpointIndex = 0; endpointIndex < numberOfEndpoints; ++endpointIndex) {
                var model = new PublishedNodesEntryModel {
                    EndpointUrl = new Uri($"opc.tcp://server{endpointIndex}:49580"),
                    OpcNodes = new List<OpcNodeModel> {
                        new OpcNodeModel {
                            Id = $"ns=2;s=Node-Server-{numberOfNodes}",
                        }
                    }
                };

                payloadDiff.Add(model);
            }

            foreach (var request in payloadDiff) {
                await FluentActions
                    .Invoking(async () => await _configService.PublishNodesAsync(request).ConfigureAwait(false))
                    .Should()
                    .NotThrowAsync()
                    .ConfigureAwait(false);
            }

            // Check
            CheckEndpointsAndNodes(numberOfEndpoints, numberOfNodes + 1);

            // Unpublish new nodes for each endpoint.
            foreach (var request in payloadDiff) {
                await FluentActions
                    .Invoking(async () => await _configService.UnpublishNodesAsync(request).ConfigureAwait(false))
                    .Should()
                    .NotThrowAsync()
                    .ConfigureAwait(false);
            }

            // Check
            CheckEndpointsAndNodes(numberOfEndpoints, numberOfNodes);
        }

        private static PublishedNodesEntryModel GenerateEndpoint(
            int dataSetIndex,
            List<OpcNodeModel> opcNodes,
            bool customEndpoint = false
        ) {
            return new PublishedNodesEntryModel {
                EndpointUrl = customEndpoint
                    ? new Uri($"opc.tcp://opcplc:{50000 + dataSetIndex}")
                    : new Uri("opc.tcp://opcplc:50000"),
                DataSetWriterId = $"DataSetWriterId{dataSetIndex}",
                DataSetWriterGroup = "DataSetWriterGroup",
                DataSetPublishingInterval = (dataSetIndex + 1) * 1000,
                OpcNodes = opcNodes.GetRange(0, dataSetIndex + 1).ToList(),
            };
        }

        private static void AssertSameNodes(PublishedNodesEntryModel endpoint, List<OpcNodeModel> nodes) {
            endpoint.PropagatePublishingIntervalToNodes();
            Assert.True(endpoint.OpcNodes.SetEqualsSafe(nodes, (a, b) => a.IsSame(b)));
        }
    }
}
