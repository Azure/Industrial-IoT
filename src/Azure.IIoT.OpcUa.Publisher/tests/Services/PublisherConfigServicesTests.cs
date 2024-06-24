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
        public async Task TestCreateUpdateWithAmbigousNodesArguments()
        {
            await using var configService = InitPublisherConfigService();

            const int numberOfEndpoints = 3;
            var opcNodes = Enumerable.Range(0, numberOfEndpoints)
                .Select(i => new OpcNodeModel
                {
                    Id = $"nsu=http://microsoft.com/Opc/OpcPlc/;s=FastUInt{i}",
                    DataSetFieldId = "alwaysthesameid"
                })
                .ToList();
            var writer = GenerateEndpoint(0, opcNodes, false);
            writer.OpcNodes = opcNodes;

            // The call should throw an exception.
            await FluentActions
                .Invoking(async () => await configService.CreateOrUpdateDataSetWriterEntryAsync(writer))
                .Should()
                .ThrowAsync<BadRequestException>()
                .WithMessage("Field ids in writer entry must be present and unique.");
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task TestCreateUpdateWithNodesWithPublishingIntervalSetArguments(bool timespan)
        {
            await using var configService = InitPublisherConfigService();

            const int numberOfEndpoints = 3;
            var opcNodes = Enumerable.Range(0, numberOfEndpoints)
                .Select(i => new OpcNodeModel
                {
                    Id = $"nsu=http://microsoft.com/Opc/OpcPlc/;s=FastUInt{i}",
                    DataSetFieldId = $"test{i}",
                    OpcPublishingIntervalTimespan = timespan ? TimeSpan.FromSeconds(1) : null,
                    OpcPublishingInterval = timespan ? null : 1
                })
                .ToList();
            var writer = GenerateEndpoint(0, opcNodes, false);
            writer.OpcNodes = opcNodes;

            // The call should throw an exception.
            await FluentActions
                .Invoking(async () => await configService.CreateOrUpdateDataSetWriterEntryAsync(writer))
                .Should()
                .ThrowAsync<BadRequestException>()
                .WithMessage("Publishing interval not allowed on node level. Must be set at writer level.");
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task TestAddNodesWithPublishingIntervalSetArguments(bool timespan)
        {
            await using var configService = InitPublisherConfigService();

            const int numberOfEndpoints = 3;
            var opcNodes = Enumerable.Range(0, numberOfEndpoints)
                .Select(i => new OpcNodeModel
                {
                    Id = $"nsu=http://microsoft.com/Opc/OpcPlc/;s=FastUInt{i}",
                    DataSetFieldId = $"test{i}"
                })
                .ToList();
            var writer = GenerateEndpoint(0, opcNodes, false);
            await configService.CreateOrUpdateDataSetWriterEntryAsync(writer);

            if (timespan)
            {
                opcNodes.ForEach(n => n.OpcPublishingIntervalTimespan = TimeSpan.FromSeconds(1));
            }
            else
            {
                opcNodes.ForEach(n => n.OpcPublishingInterval = 1);
            }

            // The call should throw an exception.
            await FluentActions
                .Invoking(async () => await configService.AddOrUpdateNodesAsync(writer.DataSetWriterGroup!,
                    writer.DataSetWriterId!, opcNodes.Skip(1).ToList()))
                .Should()
                .ThrowAsync<BadRequestException>()
                .WithMessage("Publishing interval not allowed on node level. Must be set at writer level.");
        }

        [Fact]
        public async Task TestCreateUpdateRemoveWriterEntries()
        {
            await using var configService = InitPublisherConfigService();

            await FluentActions
                .Invoking(async () => await configService
                    .GetDataSetWriterEntryAsync("test12", "test2"))
                .Should()
                .ThrowAsync<ResourceNotFoundException>()
                .WithMessage("Could not find entry with provided writer id and writer group.");

            const int numberOfNodes = 3;
            var opcNodes = Enumerable.Range(0, numberOfNodes)
                .Select(i => new OpcNodeModel
                {
                    Id = $"nsu=http://microsoft.com/Opc/OpcPlc/;s=FastUInt{i}",
                    DataSetFieldId = $"{i}"
                })
                .ToList();

            var writers = await configService.GetConfiguredEndpointsAsync();
            writers.Should().BeEmpty();

            var writer = GenerateEndpoint(0, opcNodes, false);
            writer.OpcNodes = null;
            await FluentActions
                .Invoking(async () => await configService
                    .CreateOrUpdateDataSetWriterEntryAsync(writer))
                .Should()
                .ThrowAsync<BadRequestException>()
                .WithMessage("null or empty OpcNodes is provided in request");
            writer.OpcNodes = Array.Empty<OpcNodeModel>();
            await FluentActions
                .Invoking(async () => await configService
                    .CreateOrUpdateDataSetWriterEntryAsync(writer))
                .Should()
                .ThrowAsync<BadRequestException>()
                .WithMessage("null or empty OpcNodes is provided in request");

            // Create
            writer.OpcNodes = opcNodes;
            await configService.CreateOrUpdateDataSetWriterEntryAsync(writer);
            writers = await configService.GetConfiguredEndpointsAsync();
            writers.Count.Should().Be(1);
            var writerResult = await configService.GetDataSetWriterEntryAsync(
                writer.DataSetWriterGroup!, writer.DataSetWriterId!);
            writerResult.DisableSubscriptionTransfer.Should().BeNull();
            writerResult.OpcNodes.Should().BeNull();

            // Update
            var updatedWriter = writer with { DisableSubscriptionTransfer = true };
            await configService.CreateOrUpdateDataSetWriterEntryAsync(updatedWriter);
            writerResult = await configService.GetDataSetWriterEntryAsync(
                updatedWriter.DataSetWriterGroup!, updatedWriter.DataSetWriterId!);
            writerResult.DisableSubscriptionTransfer.Should().BeTrue();
            writerResult.OpcNodes.Should().BeNull();
            writers = await configService.GetConfiguredEndpointsAsync();
            writers.Count.Should().Be(1);
            var nodes = await configService.GetNodesAsync(updatedWriter.DataSetWriterGroup!,
                updatedWriter.DataSetWriterId!);
            nodes.Count.Should().Be(numberOfNodes);

            // Add
            opcNodes.ForEach(o => o.OpcPublishingIntervalTimespan = null); // Reset in memory changes
            var writer2 = GenerateEndpoint(1, opcNodes, false);
            await configService.CreateOrUpdateDataSetWriterEntryAsync(writer2);
            writers = await configService.GetConfiguredEndpointsAsync();
            writers.Count.Should().Be(2);

            // Create ambigous entry
            opcNodes.ForEach(o => o.OpcPublishingIntervalTimespan = null); // Reset in memory changes
            updatedWriter = writer2 with { DisableSubscriptionTransfer = true };
            await configService.AddOrUpdateEndpointsAsync(updatedWriter.YieldReturn().ToList());
            writers = await configService.GetConfiguredEndpointsAsync();
            writers.Count.Should().Be(3);

            opcNodes.ForEach(o => o.OpcPublishingIntervalTimespan = null); // Reset in memory changes
            await FluentActions
                .Invoking(async () => await configService
                    .CreateOrUpdateDataSetWriterEntryAsync(writer2))
                .Should()
                .ThrowAsync<ResourceInvalidStateException>()
                .WithMessage("Trying to find entry with provided writer id produced ambigious results.");

            // Remove
            await configService.RemoveDataSetWriterEntryAsync(
                writer.DataSetWriterGroup!, writer.DataSetWriterId!);
            writers = await configService.GetConfiguredEndpointsAsync();
            writers.Count.Should().Be(2);

            await FluentActions
                .Invoking(async () => await configService
                    .GetDataSetWriterEntryAsync(writer.DataSetWriterGroup!, writer.DataSetWriterId!))
                .Should()
                .ThrowAsync<ResourceNotFoundException>()
                .WithMessage("Could not find entry with provided writer id and writer group.");
        }

        [Fact]
        public async Task TestAddQueryAndRemoveNodes()
        {
            await using var configService = InitPublisherConfigService();

            const int numberOfNodes = 3;
            var opcNodes = Enumerable.Range(0, numberOfNodes)
                .Select(i => new OpcNodeModel
                {
                    Id = $"nsu=http://microsoft.com/Opc/OpcPlc/;s=FastUInt{i}",
                    DataSetFieldId = $"{i}"
                })
                .ToList();

            // Create writer
            var writer = GenerateEndpoint(0, opcNodes, false);
            await configService.CreateOrUpdateDataSetWriterEntryAsync(writer);
            var writerResult = await configService.GetDataSetWriterEntryAsync(
                writer.DataSetWriterGroup!, writer.DataSetWriterId!);

            var nodes = await configService.GetNodesAsync(writer.DataSetWriterGroup!, writer.DataSetWriterId!);
            nodes.Count.Should().Be(1);

            // Add
            await configService.AddOrUpdateNodesAsync(writer.DataSetWriterGroup!, writer.DataSetWriterId!, opcNodes.Skip(1).ToList());
            nodes = await configService.GetNodesAsync(writer.DataSetWriterGroup!, writer.DataSetWriterId!);
            nodes.Count.Should().Be(numberOfNodes);

            // Update
            opcNodes.ForEach(node => node.OpcSamplingIntervalTimespan = TimeSpan.FromSeconds(3));
            await configService.AddOrUpdateNodesAsync(writer.DataSetWriterGroup!, writer.DataSetWriterId!, opcNodes);
            nodes = await configService.GetNodesAsync(writer.DataSetWriterGroup!, writer.DataSetWriterId!);
            nodes.Count.Should().Be(numberOfNodes);
            nodes.Should().AllSatisfy(nodes => nodes.OpcSamplingIntervalTimespan.Should().Be(TimeSpan.FromSeconds(3)));

            // Remove
            await configService.RemoveNodesAsync(writer.DataSetWriterGroup!, writer.DataSetWriterId!,
                "1".YieldReturn().ToList());
            nodes = await configService.GetNodesAsync(writer.DataSetWriterGroup!, writer.DataSetWriterId!);
            nodes.Count.Should().Be(numberOfNodes - 1);
            nodes.Should().NotContain(node => node.DataSetFieldId == "1");

            // Add a lot of nodes
            opcNodes = Enumerable.Range(0, 10000)
                .Select(i => new OpcNodeModel
                {
                    Id = $"nsu=http://microsoft.com/Opc/OpcPlc/;s=FastUInt{i}",
                    DataSetFieldId = $"{i}"
                })
                .ToList();

            await configService.AddOrUpdateNodesAsync(writer.DataSetWriterGroup!, writer.DataSetWriterId!, opcNodes);
            nodes = await configService.GetNodesAsync(writer.DataSetWriterGroup!, writer.DataSetWriterId!);
            nodes.Count.Should().Be(10000);

            // Query tests
            nodes = await configService.GetNodesAsync(writer.DataSetWriterGroup!, writer.DataSetWriterId!, "99", 100);
            nodes.Count.Should().Be(100);
            nodes[0].Should().NotBeNull().And.Match<OpcNodeModel>(node => node.DataSetFieldId == "100");

            nodes = await configService.GetNodesAsync(writer.DataSetWriterGroup!, writer.DataSetWriterId!, nodes[^1].DataSetFieldId, 10000);
            nodes[0].Should().NotBeNull().And.Match<OpcNodeModel>(node => node.DataSetFieldId == "200");
            nodes.Count.Should().Be(9800);

            nodes = await configService.GetNodesAsync(writer.DataSetWriterGroup!, writer.DataSetWriterId!, null, 5000);
            nodes.Count.Should().Be(5000);
            nodes[0].Should().NotBeNull().And.Match<OpcNodeModel>(node => node.DataSetFieldId == "0");

            nodes = await configService.GetNodesAsync(writer.DataSetWriterGroup!, writer.DataSetWriterId!, nodes[^1].DataSetFieldId);
            nodes[0].Should().NotBeNull().And.Match<OpcNodeModel>(node => node.DataSetFieldId == "5000");
            nodes.Count.Should().Be(5000);

            nodes = await configService.GetNodesAsync(writer.DataSetWriterGroup!, writer.DataSetWriterId!, nodes[^1].DataSetFieldId);
            nodes.Count.Should().Be(0);

            nodes = await configService.GetNodesAsync(writer.DataSetWriterGroup!, writer.DataSetWriterId!, "testtesttest");
            nodes.Count.Should().Be(0);

            // Remove 200 items
            nodes = await configService.GetNodesAsync(writer.DataSetWriterGroup!, writer.DataSetWriterId!, "99", 200);
            await configService.RemoveNodesAsync(writer.DataSetWriterGroup!, writer.DataSetWriterId!,
                nodes.Select(node => node.DataSetFieldId).ToList());
            nodes = await configService.GetNodesAsync(writer.DataSetWriterGroup!, writer.DataSetWriterId!, null, 20000);
            nodes.Count.Should().Be(9800);

            nodes = await configService.GetNodesAsync(writer.DataSetWriterGroup!, writer.DataSetWriterId!, "99", 100);
            nodes.Count.Should().Be(100);
            nodes[0].Should().NotBeNull().And.Match<OpcNodeModel>(node => node.DataSetFieldId == "300");

            // Remove every other
            await configService.RemoveNodesAsync(writer.DataSetWriterGroup!, writer.DataSetWriterId!,
                Enumerable.Range(0, 10000).Where(i => i % 2 == 0).Select(i => $"{i}").ToList());
            nodes = await configService.GetNodesAsync(writer.DataSetWriterGroup!, writer.DataSetWriterId!);
            nodes.Count.Should().Be(4900);

            // Remove something not there
            await configService.RemoveNodesAsync(writer.DataSetWriterGroup!, writer.DataSetWriterId!,
                Enumerable.Range(0, 10000).Where(i => i % 2 == 0).Select(i => $"{i}").ToList());
            nodes = await configService.GetNodesAsync(writer.DataSetWriterGroup!, writer.DataSetWriterId!);
            nodes.Count.Should().Be(4900);

            // Remove all but original
            await configService.CreateOrUpdateDataSetWriterEntryAsync(writer);
            nodes = await configService.GetNodesAsync(writer.DataSetWriterGroup!, writer.DataSetWriterId!);
            nodes.Count.Should().Be(1);

            writerResult = await configService.GetDataSetWriterEntryAsync(
                writer.DataSetWriterGroup!, writer.DataSetWriterId!);
            writerResult.DisableSubscriptionTransfer.Should().BeNull();
            writerResult.OpcNodes.Should().BeNull();
        }

        [Fact]
        public async Task TestInsertNodes()
        {
            await using var configService = InitPublisherConfigService();

            const int numberOfNodes = 3;
            var opcNodes = Enumerable.Range(0, numberOfNodes)
                .Select(i => new OpcNodeModel
                {
                    Id = $"nsu=http://microsoft.com/Opc/OpcPlc/;s=FastUInt{i}",
                    DataSetFieldId = $"{i}"
                })
                .ToList();

            // Create writer
            var writer = GenerateEndpoint(0, opcNodes, false);
            await configService.CreateOrUpdateDataSetWriterEntryAsync(writer);
            var writerResult = await configService.GetDataSetWriterEntryAsync(
                writer.DataSetWriterGroup!, writer.DataSetWriterId!);

            var nodes = await configService.GetNodesAsync(writer.DataSetWriterGroup!,
                writer.DataSetWriterId!);
            nodes.Count.Should().Be(1);

            var i = 1;
            while (i < 1000)
            {
                var batchSize = (Random.Shared.Next() % 3) + 1;
                var offset = (Random.Shared.Next() % i);
                opcNodes = Enumerable.Range(i, batchSize)
                    .Select(i => new OpcNodeModel
                    {
                        Id = $"nsu=http://microsoft.com/Opc/OpcPlc/;s=FastUInt{i}",
                        DataSetFieldId = $"{i}"
                    })
                    .ToList();
                i += batchSize;
                await configService.AddOrUpdateNodesAsync(writer.DataSetWriterGroup!,
                    writer.DataSetWriterId!, opcNodes, $"{offset}");
            }

            nodes = await configService.GetNodesAsync(writer.DataSetWriterGroup!, writer.DataSetWriterId!);
            nodes.Count.Should().Be(i);

            await FluentActions
                .Invoking(async () => await configService
                    .AddOrUpdateNodesAsync(writer.DataSetWriterGroup!, writer.DataSetWriterId!,
                        opcNodes, $"{i + 1}"))
                .Should()
                .ThrowAsync<ResourceNotFoundException>()
                .WithMessage("Field to insert after not found.");
        }

        [Fact]
        public async Task AddWithoutDistinctIdsResultsInError()
        {
            await using var configService = InitPublisherConfigService();
            var opcNodes = Enumerable.Range(0, 10)
                .Select(i => new OpcNodeModel
                {
                    Id = $"nsu=http://microsoft.com/Opc/OpcPlc/;s=FastUInt{i}",
                    DataSetFieldId = "alwaysthesameid"
                })
                .ToList();

            // Create writer
            var writer = GenerateEndpoint(0, opcNodes, false);
            await configService.CreateOrUpdateDataSetWriterEntryAsync(writer);
            var writerResult = await configService.GetDataSetWriterEntryAsync(
                writer.DataSetWriterGroup!, writer.DataSetWriterId!);

            var nodes = await configService.GetNodesAsync(writer.DataSetWriterGroup!, writer.DataSetWriterId!);
            nodes.Count.Should().Be(1);

            await FluentActions
                .Invoking(async () => await configService.AddOrUpdateNodesAsync(writer.DataSetWriterGroup!,
                    writer.DataSetWriterId!, opcNodes.Skip(1).ToList()))
                .Should()
                .ThrowAsync<BadRequestException>()
                .WithMessage("Field ids must be present and unique.");
        }

        [Fact]
        public async Task RemoveWithoutDistinctIdsResultsInError()
        {
            await using var configService = InitPublisherConfigService();
            var opcNodes = Enumerable.Range(0, 10)
                .Select(i => new OpcNodeModel
                {
                    Id = $"nsu=http://microsoft.com/Opc/OpcPlc/;s=FastUInt{i}",
                    DataSetFieldId = "alwaysthesameid"
                })
                .ToList();

            // Create writer
            var writer = GenerateEndpoint(0, opcNodes, false);
            await configService.CreateOrUpdateDataSetWriterEntryAsync(writer);
            var writerResult = await configService.GetDataSetWriterEntryAsync(
                writer.DataSetWriterGroup!, writer.DataSetWriterId!);

            var nodes = await configService.GetNodesAsync(writer.DataSetWriterGroup!, writer.DataSetWriterId!);
            nodes.Count.Should().Be(1);

            await FluentActions
                .Invoking(async () => await configService.RemoveNodesAsync(writer.DataSetWriterGroup!,
                    writer.DataSetWriterId!, new[] { "1", "1", "2" }))
                .Should()
                .ThrowAsync<BadRequestException>()
                .WithMessage("Field ids must be unique.");
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
        public async Task UpdateConfiguredEndpoints()
        {
            await using var configService = InitPublisherConfigService();
            var opcNodes = Enumerable.Range(0, 101)
                .Select(i => new OpcNodeModel
                {
                    Id = $"nsu=http://microsoft.com/Opc/OpcPlc/;s=FastUInt{i}",
                    DataSetFieldId = "alwaysthesameid"
                })
                .ToList();
            var items = Enumerable.Range(1, 100).Select(i => GenerateEndpoint(i, opcNodes, false)).ToList();
            await configService.SetConfiguredEndpointsAsync(items);

            var results = await configService.GetConfiguredEndpointsAsync(false);
            results.Count.Should().Be(100);

            await configService.UnpublishAllNodesAsync(results[50]);
            results = await configService.GetConfiguredEndpointsAsync(false);
            results.Count.Should().Be(99);

            // purge
            await configService.UnpublishAllNodesAsync(new PublishedNodesEntryModel());
            results = await configService.GetConfiguredEndpointsAsync(false);
            results.Should().BeEmpty();
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
            var exceptionResponse = "Nodes not found: \n" + details;
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
        public async Task TestPublishNodesEmpty()
        {
            await using var configService = InitPublisherConfigService();

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

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public async Task TestUnpublishNodesEmptyOpcNodes(
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

        private readonly NewtonsoftJsonSerializer _newtonSoftJsonSerializer;
        private readonly ILoggerFactory _loggerFactory;
        private readonly PublishedNodesConverter _publishedNodesJobConverter;
        private readonly IOptions<PublisherOptions> _options;
        private readonly PublishedNodesProvider _publishedNodesProvider;
        private readonly Mock<IMessageSource> _triggerMock;
        private readonly IPublisher _publisher;
    }
}
