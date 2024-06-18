// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Services
{
    using Azure.IIoT.OpcUa.Publisher.Tests.Utils;
    using Azure.IIoT.OpcUa.Publisher;
    using Azure.IIoT.OpcUa.Publisher.Config.Models;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack.Runtime;
    using Azure.IIoT.OpcUa.Publisher.Storage;
    using FluentAssertions;
    using Furly.Exceptions;
    using Furly.Extensions.Serializers;
    using Furly.Extensions.Serializers.Newtonsoft;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Moq;
    using Neovolve.Logging.Xunit;
    using Publisher.Services;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;

    /// <summary>
    /// Tests the PublisherConfigService class
    /// </summary>
    public class PublisherConfigServicesTests : TempFileProviderBase
    {
        private readonly NewtonsoftJsonSerializer _newtonSoftJsonSerializer;
        private readonly ILoggerFactory _loggerFactory;
        private readonly PublishedNodesConverter _publishedNodesJobConverter;
        private readonly IOptions<PublisherOptions> _options;
        private readonly PublishedNodesProvider _publishedNodesProvider;
        private readonly Mock<IMessageSource> _triggerMock;
        private readonly IPublisher _publisher;

        /// <summary>
        /// Constructor that initializes common resources used by tests.
        /// </summary>
        /// <param name="output"></param>
        public PublisherConfigServicesTests(ITestOutputHelper output)
        {
            _newtonSoftJsonSerializer = new NewtonsoftJsonSerializer();
            _loggerFactory = LogFactory.Create(output, Logging.Config);

            var clientConfigMock = new OpcUaClientConfig(new ConfigurationBuilder().Build()).ToOptions();

            _options = new PublisherConfig(new ConfigurationBuilder().Build()).ToOptions();
            _options.Value.PublishedNodesFile = _tempFile;
            _options.Value.UseFileChangePolling = true;
            _options.Value.DefaultTransport = WriterGroupTransport.Mqtt;
            _options.Value.MessagingProfile = MessagingProfile.Get(
                MessagingMode.PubSub, MessageEncoding.Json);

            _publishedNodesJobConverter = new PublishedNodesConverter(
                _loggerFactory.CreateLogger<PublishedNodesConverter>(), _newtonSoftJsonSerializer, _options);

            // Note that each test is responsible for setting content of _tempFile;
            Utils.CopyContent("Publisher/empty_pn.json", _tempFile);

            _publishedNodesProvider = new PublishedNodesProvider(_options,
                _loggerFactory.CreateLogger<PublishedNodesProvider>());
            _triggerMock = new Mock<IMessageSource>();
            var factoryMock = new Mock<IWriterGroupScopeFactory>();
            var writerGroup = new Mock<IWriterGroup>();
            writerGroup.SetupGet(l => l.Source).Returns(_triggerMock.Object);
            var lifetime = new Mock<IWriterGroupScope>();
            lifetime.SetupGet(l => l.WriterGroup).Returns(writerGroup.Object);
            factoryMock
                .Setup(factory => factory.Create(It.IsAny<WriterGroupModel>()))
                .Returns(lifetime.Object);
            _publisher = new PublisherService(factoryMock.Object, _options,
                _loggerFactory.CreateLogger<PublisherService>());
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _loggerFactory.Dispose();
                _publishedNodesProvider.Dispose();
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// This method should be called only after content of _tempFile is set.
        /// </summary>
        private PublisherConfigurationService InitPublisherConfigService()
        {
            var configService = new PublisherConfigurationService(
                _publishedNodesJobConverter,
                _options,
                _publisher,
                _loggerFactory.CreateLogger<PublisherConfigurationService>(),
                _publishedNodesProvider,
                _newtonSoftJsonSerializer
            );
            configService.GetAwaiter().GetResult();
            return configService;
        }

        [Fact]
        public async Task Legacy25PublishedNodesFile()
        {
            Utils.CopyContent("Publisher/pn_2.5_legacy.json", _tempFile);
            await using (var configService = InitPublisherConfigService())
            {
                var endpoints = await configService.GetConfiguredEndpointsAsync();
                Assert.Single(endpoints);

                var endpoint = endpoints[0];
                Assert.Equal("opc.tcp://opcplc:50000", endpoint.EndpointUrl);
                Assert.False(endpoint.UseSecurity);
                Assert.Equal(OpcAuthenticationMode.UsernamePassword, endpoint.OpcAuthenticationMode);
                Assert.Equal("username", endpoint.OpcAuthenticationUsername);
                Assert.Null(endpoint.OpcAuthenticationPassword);

                endpoint.OpcAuthenticationPassword = "password";

                var nodes = await configService.GetConfiguredNodesOnEndpointAsync(endpoint);
                Assert.Single(nodes);
                Assert.Equal("nsu=http://microsoft.com/Opc/OpcPlc/;s=FastUInt1", nodes[0].Id);

                endpoint.OpcNodes = new List<OpcNodeModel>
                {
                    new() {
                        Id = "nsu=http://microsoft.com/Opc/OpcPlc/;s=FastUInt2"
                    }
                };

                await configService.PublishNodesAsync(endpoint);

                endpoints = await configService.GetConfiguredEndpointsAsync();
                Assert.Single(endpoints);

                endpoint.OpcNodes = null;
                nodes = await configService.GetConfiguredNodesOnEndpointAsync(endpoint);
                Assert.Equal(2, nodes.Count);
                Assert.Equal("nsu=http://microsoft.com/Opc/OpcPlc/;s=FastUInt1", nodes[0].Id);
                Assert.Equal("nsu=http://microsoft.com/Opc/OpcPlc/;s=FastUInt2", nodes[1].Id);
            }
            // Simulate restart.
            await using (var configService = InitPublisherConfigService())
            {
                // We should get the same endpoint and nodes after restart.
                var endpoints = await configService.GetConfiguredEndpointsAsync();
                Assert.Single(endpoints);

                var endpoint = endpoints[0];
                Assert.Equal("opc.tcp://opcplc:50000", endpoint.EndpointUrl);
                Assert.False(endpoint.UseSecurity);
                Assert.Equal(OpcAuthenticationMode.UsernamePassword, endpoint.OpcAuthenticationMode);
                Assert.Equal("username", endpoint.OpcAuthenticationUsername);
                Assert.Null(endpoint.OpcAuthenticationPassword);

                endpoint.OpcAuthenticationPassword = "password";

                var nodes = await configService.GetConfiguredNodesOnEndpointAsync(endpoint);
                Assert.Equal(2, nodes.Count);
                Assert.Equal("nsu=http://microsoft.com/Opc/OpcPlc/;s=FastUInt1", nodes[0].Id);
                Assert.Equal("nsu=http://microsoft.com/Opc/OpcPlc/;s=FastUInt2", nodes[1].Id);
            }
        }

        [Fact]
        public async Task Legacy25PublishedNodesFileError()
        {
            Utils.CopyContent("Publisher/pn_2.5_legacy_error.json", _tempFile);
            await using var configService = InitPublisherConfigService();

            // Transformation of published nodes entries should throw a serialization error since
            // Engine/pn_2.5_legacy_error.json contains both NodeId and OpcNodes.
            // So as a result, we should end up with zero endpoints.
            var endpoints = await configService.GetConfiguredEndpointsAsync();
            Assert.Empty(endpoints);
        }

        [Theory]
        [InlineData("Publisher/pn_assets.json")]
        [InlineData("Publisher/pn_assets_with_optional_fields.json")]
        public void TestPnJsonWithMultipleJobsExpectDifferentJobIds(string publishedNodesFile)
        {
            Utils.CopyContent(publishedNodesFile, _tempFile);
            using var configService = InitPublisherConfigService();
            Assert.Equal(2, _publisher.WriterGroups.Count);
        }

        [Fact]
        public async Task TestSerializableExceptionResponse()
        {
            await using var configService = InitPublisherConfigService();

            var exceptionResponse = "null request is provided";

            // Check null request.
            await FluentActions
                .Invoking(async () => await configService.PublishNodesAsync(null))
                .Should()
                .ThrowAsync<BadRequestException>()
                .WithMessage(exceptionResponse);

            const int numberOfEndpoints = 1;
            var opcNodes = Enumerable.Range(0, numberOfEndpoints)
                .Select(i => new OpcNodeModel
                {
                    Id = $"nsu=http://microsoft.com/Opc/OpcPlc/;s=FastUInt{i}"
                })
                .ToList();

            var endpoints = Enumerable.Range(0, numberOfEndpoints)
                .Select(i => GenerateEndpoint(i, opcNodes, false))
                .ToList();

            await configService.PublishNodesAsync(endpoints[0]);

            const string details = "{\"DataSetWriterId\":\"DataSetWriterId0\",\"DataSetWriterGroup\":\"DataSetWriterGroup\",\"OpcNodes\":[{\"Id\":\"nsu=http://microsoft.com/Opc/OpcPlc/;s=SlowUInt0\",\"OpcPublishingIntervalTimespan\":\"00:00:01\"}],\"EndpointUrl\":\"opc.tcp://opcplc:50000\",\"UseSecurity\":null,\"OpcAuthenticationMode\":\"anonymous\"}";
            exceptionResponse = "Nodes not found: \n" + details;
            var opcNodes1 = Enumerable.Range(0, numberOfEndpoints)
                .Select(i => new OpcNodeModel
                {
                    Id = $"nsu=http://microsoft.com/Opc/OpcPlc/;s=SlowUInt{i}"
                })
                .ToList();
            var endpointsToDelete = Enumerable.Range(0, numberOfEndpoints)
                .Select(i => GenerateEndpoint(i, opcNodes1, false))
                .ToList();

            // try to unpublish a not published nodes.
            await FluentActions
                .Invoking(async () => await configService.UnpublishNodesAsync(endpointsToDelete[0]))
                .Should()
                .ThrowAsync<ResourceNotFoundException>()
                .WithMessage(exceptionResponse);
        }

        [Fact]
        public async Task TestPublishNodesNullOrEmpty()
        {
            await using var configService = InitPublisherConfigService();

            // Check null request.
            await FluentActions
                .Invoking(async () => await configService
                    .PublishNodesAsync(null))
                .Should()
                .ThrowAsync<BadRequestException>()
                .WithMessage("null request is provided");

            var request = new PublishedNodesEntryModel
            {
                EndpointUrl = "opc.tcp://opcplc:50000"
            };

            // Check null OpcNodes in request.
            await FluentActions
                .Invoking(async () => await configService
                    .PublishNodesAsync(request))
                .Should()
                .ThrowAsync<BadRequestException>()
                .WithMessage("null or empty OpcNodes is provided in request");

            request.OpcNodes = new List<OpcNodeModel>();

            // Check empty OpcNodes in request.
            await FluentActions
                .Invoking(async () => await configService
                    .PublishNodesAsync(request))
                .Should()
                .ThrowAsync<BadRequestException>()
                .WithMessage("null or empty OpcNodes is provided in request");
        }

        [Fact]
        public async Task TestUnpublishNodesNullRequest()
        {
            await using var configService = InitPublisherConfigService();

            // Check null request.
            await FluentActions
                .Invoking(async () => await configService
                    .UnpublishNodesAsync(null))
                .Should()
                .ThrowAsync<BadRequestException>()
                .WithMessage("null request is provided");
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public async Task TestUnpublishNodesNullOrEmptyOpcNodes(
            bool useEmptyOpcNodes,
            bool customEndpoint)
        {
            await using var configService = InitPublisherConfigService();

            const int numberOfEndpoints = 3;
            var opcNodes = Enumerable.Range(0, numberOfEndpoints)
                .Select(i => new OpcNodeModel
                {
                    Id = $"nsu=http://microsoft.com/Opc/OpcPlc/;s=FastUInt{i}"
                })
                .ToList();

            var endpoints = Enumerable.Range(0, numberOfEndpoints)
                .Select(i => GenerateEndpoint(i, opcNodes, customEndpoint))
                .ToList();

            await configService.PublishNodesAsync(endpoints[0]);
            await configService.PublishNodesAsync(endpoints[1]);
            await configService.PublishNodesAsync(endpoints[2]);

            endpoints[1] = GenerateEndpoint(1, opcNodes, customEndpoint);
            endpoints[1].OpcNodes = useEmptyOpcNodes
                ? new List<OpcNodeModel>()
                : null;

            // Check null or empty OpcNodes in request.
            await FluentActions
                .Invoking(async () => await configService
                    .UnpublishNodesAsync(endpoints[1]))
                .Should()
                .NotThrowAsync();

            var configuredEndpoints = await configService
                .GetConfiguredEndpointsAsync();

            Assert.Equal(2, configuredEndpoints.Count);

            Assert.True(endpoints[0].HasSameDataSet(configuredEndpoints[0]));
            Assert.True(endpoints[2].HasSameDataSet(configuredEndpoints[1]));
        }

        [Fact]
        public async Task TestGetConfiguredNodesOnEndpointNullRequest()
        {
            await using var configService = InitPublisherConfigService();

            // Check call with null.
            await FluentActions
                .Invoking(async () => await configService
                    .GetConfiguredNodesOnEndpointAsync(null))
                .Should()
                .ThrowAsync<BadRequestException>()
                .WithMessage("null request is provided");
        }

        [Fact]
        public async Task TestAddOrUpdateEndpointsNullRequest()
        {
            await using var configService = InitPublisherConfigService();

            // Check call with null.
            await FluentActions
                .Invoking(async () => await configService
                    .AddOrUpdateEndpointsAsync(null))
                .Should()
                .ThrowAsync<BadRequestException>()
                .WithMessage("null request is provided");
        }

        [Fact]
        public async Task TestAddOrUpdateEndpointsMultipleEndpointEntries()
        {
            await using var configService = InitPublisherConfigService();

            const int numberOfEndpoints = 3;
            var opcNodes = Enumerable.Range(0, numberOfEndpoints)
                .Select(i => new OpcNodeModel
                {
                    Id = $"nsu=http://microsoft.com/Opc/OpcPlc/;s=FastUInt{i}"
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
                .Invoking(async () => await configService
                    .AddOrUpdateEndpointsAsync(endpoints))
                .Should()
                .ThrowAsync<BadRequestException>()
                .WithMessage("Request contains two entries for the same endpoint at index 0 and 2");
        }

        [Fact]
        public async Task TestAddOrUpdateEndpointsWithNonDefaultWriterGroupTransportAndSecurity()
        {
            await using var configService = InitPublisherConfigService();

            const int numberOfEndpoints = 3;
            var opcNodes = Enumerable.Range(0, numberOfEndpoints)
                .Select(i => new OpcNodeModel
                {
                    Id = $"nsu=http://microsoft.com/Opc/OpcPlc/;s=FastUInt{i}"
                })
                .ToList();

            var endpoints = Enumerable.Range(0, numberOfEndpoints)
                .Select(i => GenerateEndpoint(i, opcNodes, false))
                .ToList();

            endpoints.ForEach(ep => ep.WriterGroupTransport = WriterGroupTransport.FileSystem);
            endpoints.ForEach(ep => ep.EndpointSecurityMode = SecurityMode.Best);

            await configService.AddOrUpdateEndpointsAsync(endpoints);

            var configuredEndpoints = await configService.GetConfiguredEndpointsAsync();
            Assert.All(configuredEndpoints, ep =>
            {
                Assert.Equal(WriterGroupTransport.FileSystem, ep.WriterGroupTransport);
                Assert.Equal(SecurityMode.Best, ep.EndpointSecurityMode);
            });
        }

        [Fact]
        public async Task TestAddOrUpdateEndpointsMultipleEndpointEntriesTimespan()
        {
            await using var configService = InitPublisherConfigService();

            const int numberOfEndpoints = 3;
            var opcNodes = Enumerable.Range(0, numberOfEndpoints)
                .Select(i => new OpcNodeModel
                {
                    Id = $"nsu=http://microsoft.com/Opc/OpcPlc/;s=FastUInt{i}"
                })
                .ToList();

            var endpoints = Enumerable.Range(0, numberOfEndpoints)
                .Select(i => GenerateEndpoint(i, opcNodes, false))
                .ToList();

            endpoints.ForEach(e =>
                e.DataSetPublishingIntervalTimespan =
                TimeSpan.FromMilliseconds(e.DataSetPublishingInterval ?? 1000));

            // Make endpoint at index 0 and 2 the same.
            endpoints[2].DataSetWriterId = endpoints[0].DataSetWriterId;
            endpoints[2].DataSetWriterGroup = endpoints[0].DataSetWriterGroup;
            endpoints[2].DataSetPublishingIntervalTimespan = endpoints[0].DataSetPublishingIntervalTimespan;

            // The call should throw an exception.
            await FluentActions
                .Invoking(async () => await configService
                    .AddOrUpdateEndpointsAsync(endpoints))
                .Should()
                .ThrowAsync<BadRequestException>()
                .WithMessage("Request contains two entries for the same endpoint at index 0 and 2");
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task TestAddOrUpdateEndpointsAddEndpoints(
            bool useDataSetSpecificEndpoints
        )
        {
            _options.Value.MaxNodesPerDataSet = 2;

            await using var configService = InitPublisherConfigService();

            Assert.Empty(_publisher.WriterGroups);

            const int numberOfEndpoints = 3;

            var opcNodes = Enumerable.Range(0, numberOfEndpoints)
                .Select(i => new OpcNodeModel
                {
                    Id = $"nsu=http://microsoft.com/Opc/OpcPlc/;s=FastUInt{i}"
                })
                .ToList();

            var endpoints = Enumerable.Range(0, numberOfEndpoints)
                .Select(i => GenerateEndpoint(i, opcNodes, useDataSetSpecificEndpoints))
                .ToList();

            var tasks = new List<Task>();
            for (var i = 0; i < numberOfEndpoints; i++)
            {
                tasks.Add(configService.AddOrUpdateEndpointsAsync(
                    new List<PublishedNodesEntryModel> { endpoints[i] }));
            }

            await Task.WhenAll(tasks);

            for (var i = 0; i < numberOfEndpoints; i++)
            {
                var endpointNodes = await configService
                    .GetConfiguredNodesOnEndpointAsync(endpoints[i]);

                AssertSameNodes(endpoints[i], endpointNodes);
            }

            Assert.Single(_publisher.WriterGroups);
        }

        [Theory]
        [InlineData("Publisher/pn_events.json")]
        [InlineData("Publisher/pn_pending_alarms.json")]
        [InlineData("Publisher/empty_pn.json")]
        [InlineData("Publisher/pn_assets.json")]
        [InlineData("Publisher/pn_assets_with_optional_fields.json")]
        [InlineData("Publisher/publishednodes.json")]
        [InlineData("Publisher/publishednodeswithoptionalfields.json")]
        [InlineData("Publisher/publishednodes_with_duplicates.json")]
        public async Task TestAddOrUpdateEndpointsRemoveEndpoints(string publishedNodesFile)
        {
            Utils.CopyContent(publishedNodesFile, _tempFile);
            await using var configService = InitPublisherConfigService();

            var payload = Utils.GetFileContent(publishedNodesFile);
            var payloadRequests = _newtonSoftJsonSerializer.Deserialize<List<PublishedNodesEntryModel>>(payload);

            var index = 0;
            foreach (var request in payloadRequests)
            {
                request.OpcNodes = index % 2 == 0
                    ? null
                    : new List<OpcNodeModel>();
                ++index;

                var shouldThrow = !_publishedNodesJobConverter.ToPublishedNodes(0, default, _publisher.WriterGroups)
                    .Select(p => p.PropagatePublishingIntervalToNodes())
                    .Any(dataSet => dataSet.HasSameDataSet(request.PropagatePublishingIntervalToNodes()));
                if (shouldThrow)
                {
                    await FluentActions
                        .Invoking(async () => await configService
                            .AddOrUpdateEndpointsAsync(new List<PublishedNodesEntryModel> { request }))
                        .Should()
                        .ThrowAsync<ResourceNotFoundException>()
                        .WithMessage($"Endpoint not found: {request.EndpointUrl}");
                }
                else
                {
                    await FluentActions
                        .Invoking(async () => await configService
                            .AddOrUpdateEndpointsAsync(new List<PublishedNodesEntryModel> { request }))
                        .Should()
                        .NotThrowAsync();
                }
            }

            var configuredEndpoints = await configService
                .GetConfiguredEndpointsAsync();
            Assert.Empty(configuredEndpoints);
        }

        [Fact]
        public async Task TestAddOrUpdateEndpointsAddAndRemove()
        {
            _options.Value.MaxNodesPerDataSet = 2;

            await using var configService = InitPublisherConfigService();

            Assert.Empty(_publisher.WriterGroups);

            var opcNodes = Enumerable.Range(0, 5)
                .Select(i => new OpcNodeModel
                {
                    Id = $"nsu=http://microsoft.com/Opc/OpcPlc/;s=FastUInt{i}"
                })
                .ToList();

            var endpoints = Enumerable.Range(0, 5)
                .Select(i => GenerateEndpoint(i, opcNodes))
                .ToList();

            var tasks = new List<Task>();
            for (var i = 0; i < 3; i++)
            {
                tasks.Add(configService.PublishNodesAsync(endpoints[i]));
            }

            await Task.WhenAll(tasks);

            for (var i = 0; i < 3; i++)
            {
                var endpointNodes = await configService
                    .GetConfiguredNodesOnEndpointAsync(endpoints[i]);

                AssertSameNodes(endpoints[i], endpointNodes);
            }

            // Helper method.
            async Task AssertGetConfiguredNodesOnEndpointThrows(
                PublisherConfigurationService publisherConfigurationService,
                PublishedNodesEntryModel endpoint
            )
            {
                await FluentActions
                    .Invoking(async () => await publisherConfigurationService
                        .GetConfiguredNodesOnEndpointAsync(endpoint))
                    .Should()
                    .ThrowAsync<ResourceNotFoundException>()
                    .WithMessage($"Endpoint not found: {endpoint.EndpointUrl}");
            }

            // Those calls should throw.
            for (var i = 3; i < 5; i++)
            {
                await AssertGetConfiguredNodesOnEndpointThrows(configService, endpoints[i]);
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
                .Invoking(async () => await configService
                    .AddOrUpdateEndpointsAsync(updateRequest))
                .Should()
                .ThrowAsync<ResourceNotFoundException>()
                .WithMessage($"Endpoint not found: {updateRequest[3].EndpointUrl}");

            updateRequest.RemoveAt(3);
            await configService.AddOrUpdateEndpointsAsync(updateRequest);

            // Check endpoint 0.
            await AssertGetConfiguredNodesOnEndpointThrows(configService, endpoints[0]);

            // Check endpoint 1.
            await AssertGetConfiguredNodesOnEndpointThrows(configService, endpoints[1]);

            // Check endpoint 2.
            var endpointNodes2 = await configService
                .GetConfiguredNodesOnEndpointAsync(endpoints[2]);

            AssertSameNodes(updateRequest[2], endpointNodes2);

            // Check endpoint 3.
            await AssertGetConfiguredNodesOnEndpointThrows(configService, endpoints[3])
;

            // Check endpoint 4.
            var endpointNodes4 = await configService
                .GetConfiguredNodesOnEndpointAsync(endpoints[4]);

            AssertSameNodes(updateRequest[3], endpointNodes4);
        }

        [Theory]
        [InlineData("Publisher/pn_opc_nodes_empty.json")]
        [InlineData("Publisher/pn_opc_nodes_null.json")]
        [InlineData("Publisher/pn_opc_nodes_empty_and_null.json")]
        public async Task TestInitStandaloneJobOrchestratorFromEmptyOpcNodes(string publishedNodesFile)
        {
            Utils.CopyContent(publishedNodesFile, _tempFile);
            await using var configService = InitPublisherConfigService();

            // Engine/empty_opc_nodes.json contains entries with null or empty OpcNodes.
            // Those entries should not result in any endpoint entries in publisherConfigurationService.
            var configuredEndpoints = await configService
                .GetConfiguredEndpointsAsync();
            Assert.Empty(configuredEndpoints);

            // There should also not be any job entries.
            Assert.Empty(_publisher.WriterGroups);
        }

        [Theory]
        [InlineData("Publisher/pn_assets_with_optional_fields.json")]
        public async Task OptionalFieldsPublishedNodesFile(string publishedNodesFile)
        {
            Utils.CopyContent(publishedNodesFile, _tempFile);
            await using (var configService = InitPublisherConfigService())
            {
                var endpoints = await configService.GetConfiguredEndpointsAsync();
                Assert.Equal(2, endpoints.Count);

                Assert.Equal("Leaf0", endpoints[0].DataSetWriterGroup);
                Assert.Equal("opc.tcp://opcplc:50000", endpoints[0].EndpointUrl);
                Assert.False(endpoints[0].UseSecurity);
                Assert.Equal(OpcAuthenticationMode.Anonymous, endpoints[0].OpcAuthenticationMode);
                Assert.Equal("Leaf0_10000_3085991c-b85c-4311-9bfb-a916da952234", endpoints[0].DataSetWriterId);
                Assert.Equal("Tag_Leaf0_10000_3085991c-b85c-4311-9bfb-a916da952234", endpoints[0].DataSetName);

                Assert.Equal("Leaf1", endpoints[1].DataSetWriterGroup);
                Assert.Equal("opc.tcp://opcplc:50000", endpoints[1].EndpointUrl);
                Assert.False(endpoints[1].UseSecurity);
                Assert.Equal(OpcAuthenticationMode.UsernamePassword, endpoints[1].OpcAuthenticationMode);
                Assert.Equal("Leaf1_10000_2e4fc28f-ffa2-4532-9f22-378d47bbee5d", endpoints[1].DataSetWriterId);
                Assert.Equal("Tag_Leaf1_10000_2e4fc28f-ffa2-4532-9f22-378d47bbee5d", endpoints[1].DataSetName);
                endpoints[0].OpcNodes = null;
                var nodes = await configService.GetConfiguredNodesOnEndpointAsync(endpoints[0]);
                Assert.Single(nodes);
                Assert.Equal("nsu=http://microsoft.com/Opc/OpcPlc/;s=StepUp", nodes[0].Id);

                endpoints[0].OpcNodes = new List<OpcNodeModel>
                {
                    new() {
                        Id = "nsu=http://microsoft.com/Opc/OpcPlc/;s=FastUInt2"
                    }
                };

                await configService.PublishNodesAsync(endpoints[0]);

                endpoints = await configService.GetConfiguredEndpointsAsync();
                Assert.Single(endpoints);

                endpoints[0].OpcNodes = null;
                nodes = await configService.GetConfiguredNodesOnEndpointAsync(endpoints[0]);
                Assert.Equal(2, nodes.Count);
                Assert.Equal("nsu=http://microsoft.com/Opc/OpcPlc/;s=StepUp", nodes[0].Id);
                Assert.Equal("nsu=http://microsoft.com/Opc/OpcPlc/;s=FastUInt2", nodes[1].Id);
            }
            // Simulate restart.
            await using (var configService = InitPublisherConfigService())
            {
                // We should get the same endpoint and nodes after restart.
                var endpoints = await configService.GetConfiguredEndpointsAsync();
                Assert.Single(endpoints);

                Assert.Equal("Leaf0", endpoints[0].DataSetWriterGroup);
                Assert.Equal("opc.tcp://opcplc:50000", endpoints[0].EndpointUrl);
                Assert.False(endpoints[0].UseSecurity);
                Assert.Equal(OpcAuthenticationMode.Anonymous, endpoints[0].OpcAuthenticationMode);
                Assert.Equal("Leaf0_10000_3085991c-b85c-4311-9bfb-a916da952234", endpoints[0].DataSetWriterId);
                Assert.Equal("Tag_Leaf0_10000_3085991c-b85c-4311-9bfb-a916da952234", endpoints[0].DataSetName);

                var nodes = await configService.GetConfiguredNodesOnEndpointAsync(endpoints[0]);
                Assert.Equal(2, nodes.Count);
                Assert.Equal("nsu=http://microsoft.com/Opc/OpcPlc/;s=StepUp", nodes[0].Id);
                Assert.Equal("nsu=http://microsoft.com/Opc/OpcPlc/;s=FastUInt2", nodes[1].Id);
            }
        }

        [Theory]
        [InlineData("Publisher/publishednodes.json")]
        [InlineData("Publisher/publishednodeswithoptionalfields.json")]
        [InlineData("Publisher/pn_assets.json")]
        [InlineData("Publisher/pn_assets_with_optional_fields.json")]
        [InlineData("Publisher/pn_events.json")]
        [InlineData("Publisher/pn_pending_alarms.json")]
        public async Task PublishNodesOnEmptyConfiguration(string publishedNodesFile)
        {
            Utils.CopyContent("Publisher/empty_pn.json", _tempFile);
            await using var configService = InitPublisherConfigService();

            var payload = Utils.GetFileContent(publishedNodesFile);
            foreach (var request in _newtonSoftJsonSerializer.Deserialize<List<PublishedNodesEntryModel>>(payload))
            {
                await FluentActions
                    .Invoking(async () => await configService.PublishNodesAsync(request))
                    .Should()
                    .NotThrowAsync();
            }

            _publisher.WriterGroups.Count
                .Should()
                .Be(1);
        }

        [Theory]
        [InlineData("Publisher/publishednodes.json", "Publisher/publishednodeswithoptionalfields.json")]
        [InlineData("Publisher/publishednodeswithoptionalfields.json", "Publisher/publishednodes.json")]
        [InlineData("Publisher/pn_assets.json", "Publisher/pn_assets_with_optional_fields.json")]
        [InlineData("Publisher/pn_assets_with_optional_fields.json", "Publisher/pn_assets.json")]
        [InlineData("Publisher/pn_events.json", "Publisher/pn_pending_alarms.json")]
        public async Task PublishNodesOnExistingConfiguration(string existingConfig, string newConfig)
        {
            Utils.CopyContent(existingConfig, _tempFile);
            await using var configService = InitPublisherConfigService();

            var payload = Utils.GetFileContent(newConfig);
            foreach (var request in _newtonSoftJsonSerializer.Deserialize<List<PublishedNodesEntryModel>>(payload))
            {
                await FluentActions
                    .Invoking(async () => await configService.PublishNodesAsync(request))
                    .Should()
                    .NotThrowAsync();
            }

            _publisher.WriterGroups.Count
                .Should()
                .Be(1);
        }

        [Theory]
        [InlineData("Publisher/publishednodes.json", "Publisher/pn_assets.json")]
        [InlineData("Publisher/publishednodeswithoptionalfields.json", "Publisher/pn_assets_with_optional_fields.json")]
        [InlineData("Publisher/pn_assets.json", "Publisher/publishednodes.json")]
        [InlineData("Publisher/pn_assets_with_optional_fields.json", "Publisher/publishednodeswithoptionalfields.json")]
        public async Task PublishNodesOnNewConfiguration(string existingConfig, string newConfig)
        {
            Utils.CopyContent(existingConfig, _tempFile);
            await using var configService = InitPublisherConfigService();

            var payload = Utils.GetFileContent(newConfig);
            foreach (var request in _newtonSoftJsonSerializer.Deserialize<List<PublishedNodesEntryModel>>(payload))
            {
                await FluentActions
                    .Invoking(async () => await configService.PublishNodesAsync(request))
                    .Should()
                    .NotThrowAsync();
            }

            _publisher.WriterGroups.Count
                .Should()
                .Be(1);
        }

        [Theory]
        [InlineData("Publisher/publishednodes.json")]
        [InlineData("Publisher/publishednodeswithoptionalfields.json")]
        [InlineData("Publisher/pn_assets.json")]
        [InlineData("Publisher/pn_assets_with_optional_fields.json")]
        public async Task UnpublishNodesOnExistingConfiguration(string publishedNodesFile)
        {
            Utils.CopyContent(publishedNodesFile, _tempFile);
            await using var configService = InitPublisherConfigService();

            var payload = Utils.GetFileContent(publishedNodesFile);
            foreach (var request in _newtonSoftJsonSerializer.Deserialize<List<PublishedNodesEntryModel>>(payload))
            {
                await FluentActions
                    .Invoking(async () => await configService.UnpublishNodesAsync(request))
                    .Should()
                    .NotThrowAsync();
            }

            _publisher.WriterGroups.Count
                .Should()
                .Be(0);
        }

        [Theory]
        [InlineData("Publisher/publishednodes.json", "Publisher/pn_assets.json")]
        [InlineData("Publisher/publishednodeswithoptionalfields.json", "Publisher/pn_assets_with_optional_fields.json")]
        [InlineData("Publisher/pn_assets.json", "Publisher/publishednodes.json")]
        [InlineData("Publisher/pn_assets_with_optional_fields.json", "Publisher/publishednodeswithoptionalfields.json")]
        public async Task UnpublishNodesOnNonExistingConfiguration(string existingConfig, string newConfig)
        {
            Utils.CopyContent(existingConfig, _tempFile);
            await using var configService = InitPublisherConfigService();

            var payload = Utils.GetFileContent(newConfig);
            foreach (var request in _newtonSoftJsonSerializer.Deserialize<List<PublishedNodesEntryModel>>(payload))
            {
                await FluentActions
                    .Invoking(async () => await configService.UnpublishNodesAsync(request))
                    .Should()
                    .ThrowAsync<ResourceNotFoundException>()
                    .WithMessage($"Endpoint not found: {request.EndpointUrl}");
            }

            _publisher.WriterGroups.Sum(writerGroup => writerGroup.DataSetWriters.Count)
                .Should()
                .Be(2);
        }

        [Theory]
        [InlineData(2, 10)]
        // [InlineData(100, 1000)]
        public async Task PublishNodesStressTest(int numberOfEndpoints, int numberOfNodes)
        {
            await using (var fileStream = new FileStream(_tempFile, FileMode.Open, FileAccess.Write))
            {
                fileStream.Write(Encoding.UTF8.GetBytes("[]"));
            }
            await using var configService = InitPublisherConfigService();

            var payload = new List<PublishedNodesEntryModel>();
            for (var endpointIndex = 0; endpointIndex < numberOfEndpoints; ++endpointIndex)
            {
                var model = new PublishedNodesEntryModel
                {
                    EndpointUrl = $"opc.tcp://server{endpointIndex}:49580",
                    OpcNodes = new List<OpcNodeModel>()
                };
                for (var nodeIndex = 0; nodeIndex < numberOfNodes; ++nodeIndex)
                {
                    model.OpcNodes.Add(new OpcNodeModel
                    {
                        Id = $"ns=2;s=Node-Server-{nodeIndex}"
                    });
                }

                payload.Add(model);
            }

            // Publish all nodes.
            foreach (var request in payload)
            {
                await FluentActions
                    .Invoking(async () => await configService.PublishNodesAsync(request))
                    .Should()
                    .NotThrowAsync();
            }

            void CheckEndpointsAndNodes(
                int expectedNumberOfEndpoints,
                int expectedNumberOfNodesPerEndpoint
            )
            {
                var writerGroups = _publisher.WriterGroups;
                writerGroups
                    .SelectMany(writerGroup => writerGroup.DataSetWriters)
                    .Count(v => v.DataSet.DataSetSource.PublishedVariables.PublishedData.Count == expectedNumberOfNodesPerEndpoint)
                    .Should()
                    .Be(expectedNumberOfEndpoints);
            }

            // Check
            CheckEndpointsAndNodes(numberOfEndpoints, numberOfNodes);

            // Publish one more node for each endpoint.
            var payloadDiff = new List<PublishedNodesEntryModel>();
            for (var endpointIndex = 0; endpointIndex < numberOfEndpoints; ++endpointIndex)
            {
                var model = new PublishedNodesEntryModel
                {
                    EndpointUrl = $"opc.tcp://server{endpointIndex}:49580",
                    OpcNodes = new List<OpcNodeModel> {
                        new() {
                            Id = $"ns=2;s=Node-Server-{numberOfNodes}"
                        }
                    }
                };

                payloadDiff.Add(model);
            }

            foreach (var request in payloadDiff)
            {
                await FluentActions
                    .Invoking(async () => await configService.PublishNodesAsync(request))
                    .Should()
                    .NotThrowAsync();
            }

            // Check
            CheckEndpointsAndNodes(numberOfEndpoints, numberOfNodes + 1);

            // Unpublish new nodes for each endpoint.
            foreach (var request in payloadDiff)
            {
                await FluentActions
                    .Invoking(async () => await configService.UnpublishNodesAsync(request))
                    .Should()
                    .NotThrowAsync();
            }

            // Check
            CheckEndpointsAndNodes(numberOfEndpoints, numberOfNodes);
        }

        private static PublishedNodesEntryModel GenerateEndpoint(
            int dataSetIndex,
            List<OpcNodeModel> opcNodes,
            bool customEndpoint = false
        )
        {
            return new PublishedNodesEntryModel
            {
                EndpointUrl = customEndpoint
                    ? $"opc.tcp://opcplc:{50000 + dataSetIndex}"
                    : "opc.tcp://opcplc:50000",
                DataSetWriterId = $"DataSetWriterId{dataSetIndex}",
                DataSetWriterGroup = "DataSetWriterGroup",
                DataSetPublishingInterval = (dataSetIndex + 1) * 1000,
                OpcNodes = opcNodes.GetRange(0, dataSetIndex + 1).ToList()
            };
        }

        private static void AssertSameNodes(PublishedNodesEntryModel endpoint, List<OpcNodeModel> nodes)
        {
            endpoint.PropagatePublishingIntervalToNodes();
            Assert.True(endpoint.OpcNodes.SetEqualsSafe(nodes, (a, b) => a.IsSame(b)));
        }
    }
}
