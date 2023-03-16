// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Controller
{
    using Azure.IIoT.OpcUa.Publisher.Module.Controllers;
    using Azure.IIoT.OpcUa.Publisher;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Services;
    using Azure.IIoT.OpcUa.Publisher.Storage;
    using Azure.IIoT.OpcUa.Publisher.Tests.Utils;
    using Autofac;
    using Divergic.Logging.Xunit;
    using FluentAssertions;
    using Furly.Extensions.Serializers;
    using Furly.Extensions.Serializers.Newtonsoft;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Moq;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;

    /// <summary>
    /// Tests the Direct Methods API for the pubisher
    /// </summary>
    public class DmApiPublisherControllerTests : TempFileProviderBase
    {
        private readonly NewtonsoftJsonSerializer _newtonSoftJsonSerializer;
        private readonly ILoggerFactory _loggerFactory;
        private readonly PublishedNodesConverter _publishedNodesJobConverter;
        private readonly IOptions<PublisherOptions> _options;
        private readonly PublishedNodesProvider _publishedNodesProvider;
        private readonly Mock<IMessageSource> _triggerMock;
        private readonly IPublisherHost _publisher;
        private readonly Mock<IDiagnosticCollector> _diagnostic;

        /// <summary>
        /// Constructor that initializes common resources used by tests.
        /// </summary>
        /// <param name="output"></param>
        public DmApiPublisherControllerTests(ITestOutputHelper output)
        {
            _newtonSoftJsonSerializer = new NewtonsoftJsonSerializer();
            _loggerFactory = LogFactory.Create(output);

            _options = new PublisherConfig(new ConfigurationBuilder().Build()).ToOptions();
            _options.Value.PublishedNodesFile = _tempFile;
            _options.Value.MessagingProfile = MessagingProfile.Get(
                MessagingMode.PubSub, MessageEncoding.Json);

            _publishedNodesJobConverter = new PublishedNodesConverter(
                _loggerFactory.CreateLogger<PublishedNodesConverter>(), _newtonSoftJsonSerializer);

            // Note that each test is responsible for setting content of _tempFile;
            CopyContent("Controller/empty_pn.json", _tempFile);

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
            _publisher = new PublisherHostService(factoryMock.Object, _options,
                _loggerFactory.CreateLogger<PublisherHostService>());
            _diagnostic = new Mock<IDiagnosticCollector>();
            var mockDiag = new WriterGroupDiagnosticModel();
            _diagnostic.Setup(m => m.TryGetDiagnosticsForWriterGroup(It.IsAny<string>(), out mockDiag)).Returns(true);
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
                _newtonSoftJsonSerializer,
                _diagnostic.Object
            );
            configService.GetAwaiter().GetResult();
            return configService;
        }

        [Theory]
        [InlineData("Controller/DmApiPayloadCollection.json")]
        public async Task DmApiPublishUnpublishNodesTestAsync(string publishedNodesFile)
        {
            CopyContent("Controller/empty_pn.json", _tempFile);
            using var configService = InitPublisherConfigService();

            var methodsController = new PublisherMethodsController(configService);

            using var publishPayloads = new StreamReader(publishedNodesFile);
            var publishNodesRequest = _newtonSoftJsonSerializer.Deserialize<List<PublishedNodesEntryModel>>(
                await publishPayloads.ReadToEndAsync().ConfigureAwait(false));

            foreach (var request in publishNodesRequest)
            {
                var initialNode = request.OpcNodes[0];
                for (var i = 0; i < 10000; i++)
                {
                    request.OpcNodes.Add(new OpcNodeModel
                    {
                        Id = initialNode.Id + i.ToString(CultureInfo.InvariantCulture),
                        DataSetFieldId = initialNode.DataSetFieldId,
                        DisplayName = initialNode.DisplayName,
                        ExpandedNodeId = initialNode.ExpandedNodeId,
                        HeartbeatIntervalTimespan = initialNode.HeartbeatIntervalTimespan,
                        OpcPublishingInterval = initialNode.OpcPublishingInterval,
                        OpcSamplingInterval = initialNode.OpcSamplingInterval,
                        QueueSize = initialNode.QueueSize,
                        // ToDo: Implement mechanism for SkipFirst.
                        SkipFirst = initialNode.SkipFirst,
                        DataChangeTrigger = initialNode.DataChangeTrigger,
                        DeadbandType = initialNode.DeadbandType,
                        DeadbandValue = initialNode.DeadbandValue
                    });
                }

                await FluentActions
                    .Invoking(async () => await methodsController.PublishNodesAsync(request).ConfigureAwait(false))
                    .Should()
                    .NotThrowAsync()
                    .ConfigureAwait(false);
            }

            var writerGroup = Assert.Single(_publisher.WriterGroups);
            Assert.Equal(8, _publisher.Version);

            foreach (var request in publishNodesRequest)
            {
                await FluentActions
                    .Invoking(async () => await methodsController
                    .UnpublishNodesAsync(request).ConfigureAwait(false))
                    .Should()
                    .NotThrowAsync()
                    .ConfigureAwait(false);
            }

            Assert.Empty(_publisher.WriterGroups);
            Assert.Equal(15, _publisher.Version);
        }

        [Theory]
        [InlineData("Controller/DmApiPayloadCollection.json")]
        public async Task DmApiPublishUnpublishAllNodesTestAsync(string publishedNodesFile)
        {
            CopyContent("Controller/empty_pn.json", _tempFile);
            using var configService = InitPublisherConfigService();
            var methodsController = new PublisherMethodsController(configService);

            using var publishPayloads = new StreamReader(publishedNodesFile);
            var publishNodesRequest = _newtonSoftJsonSerializer.Deserialize<List<PublishedNodesEntryModel>>(
                await publishPayloads.ReadToEndAsync().ConfigureAwait(false));

            foreach (var request in publishNodesRequest)
            {
                var initialNode = request.OpcNodes[0];
                for (var i = 0; i < 10000; i++)
                {
                    request.OpcNodes.Add(new OpcNodeModel
                    {
                        Id = initialNode.Id + i.ToString(CultureInfo.InvariantCulture),
                        DataSetFieldId = initialNode.DataSetFieldId,
                        DisplayName = initialNode.DisplayName,
                        ExpandedNodeId = initialNode.ExpandedNodeId,
                        HeartbeatIntervalTimespan = initialNode.HeartbeatIntervalTimespan,
                        OpcPublishingInterval = initialNode.OpcPublishingInterval,
                        OpcSamplingInterval = initialNode.OpcSamplingInterval,
                        QueueSize = initialNode.QueueSize,
                        SkipFirst = initialNode.SkipFirst,
                        DataChangeTrigger = initialNode.DataChangeTrigger,
                        DeadbandType = initialNode.DeadbandType,
                        DeadbandValue = initialNode.DeadbandValue
                    });
                }

                await FluentActions
                    .Invoking(async () => await methodsController.PublishNodesAsync(request).ConfigureAwait(false))
                    .Should()
                    .NotThrowAsync()
                    .ConfigureAwait(false);
            }

            var writerGroup = Assert.Single(_publisher.WriterGroups);

            var unpublishAllNodesRequest = publishNodesRequest.GroupBy(pn => string.Concat(pn.EndpointUrl, pn.DataSetWriterId, pn.DataSetPublishingInterval))
                .Select(g => g.First()).ToList();

            foreach (var request in unpublishAllNodesRequest)
            {
                request.OpcNodes?.Clear();
                await FluentActions
                    .Invoking(async () => await methodsController
                    .UnpublishAllNodesAsync(request).ConfigureAwait(false))
                    .Should()
                    .NotThrowAsync()
                    .ConfigureAwait(false);
            }

            Assert.Empty(_publisher.WriterGroups);
        }

        [Theory]
        [InlineData("Controller/DmApiPayloadCollection.json")]
        public async Task DmApiPublishNodesToJobTestAsync(string publishedNodesFile)
        {
            CopyContent("Controller/empty_pn.json", _tempFile);
            using var configService = InitPublisherConfigService();

            var methodsController = new PublisherMethodsController(configService);

            using var publishPayloads = new StreamReader(publishedNodesFile);
            var publishNodesRequests = _newtonSoftJsonSerializer.Deserialize<List<PublishedNodesEntryModel>>
                (await publishPayloads.ReadToEndAsync().ConfigureAwait(false));

            foreach (var request in publishNodesRequests)
            {
                await FluentActions
                    .Invoking(async () => await methodsController
                    .PublishNodesAsync(request).ConfigureAwait(false))
                    .Should()
                    .NotThrowAsync()
                    .ConfigureAwait(false);
            }

            var writerGroup = Assert.Single(_publisher.WriterGroups);

            writerGroup.DataSetWriters.Count.Should().Be(6);

            Assert.All(writerGroup.DataSetWriters,
                writer => Assert.Equal(publishNodesRequests[0].EndpointUrl,
                    writer.DataSet.DataSetSource.Connection.Endpoint.Url));
            Assert.Equal(publishNodesRequests
                .Select(n => n.UseSecurity ? SecurityMode.Best : SecurityMode.None)
                .ToHashSet(),
                writerGroup.DataSetWriters
                .Select(w => w.DataSet.DataSetSource.Connection.Endpoint.SecurityMode.Value)
                .ToHashSet());
        }

        [Theory]
        [InlineData("Controller/DmApiPayloadTwoEndpoints.json")]
        public async Task DmApiGetConfiguredNodesOnEndpointAsyncTestAsync(string publishedNodesFile)
        {
            const string endpointUrl = "opc.tcp://opcplc:50010";

            var endpointRequest = new PublishedNodesEntryModel
            {
                EndpointUrl = endpointUrl
            };

            var (d, methodsController) = await PublishNodeAsync(publishedNodesFile).ConfigureAwait(false);
            var response = await FluentActions
                    .Invoking(async () => await methodsController
                    .GetConfiguredNodesOnEndpointAsync(endpointRequest).ConfigureAwait(false))
                    .Should()
                    .NotThrowAsync()
                    .ConfigureAwait(false);
            d.Dispose();

            response.Subject.OpcNodes.Count
                .Should()
                .Be(2);
            response.Subject.OpcNodes[0].Id
                .Should()
                .Be("ns=2;s=FastUInt1");
            response.Subject.OpcNodes[1].Id
                .Should()
                .Be("ns=2;s=FastUInt2");
        }

        [Theory]
        [InlineData("Controller/DmApiPayloadTwoEndpoints.json")]
        public async Task DmApiGetConfiguredNodesOnEndpointAsyncDataSetWriterGroupTestAsync(string publishedNodesFile)
        {
            const string endpointUrl = "opc.tcp://opcplc:50000";
            const string dataSetWriterGroup = "Leaf0";
            const string dataSetWriterId = "Leaf0_10000_3085991c-b85c-4311-9bfb-a916da952234";
            const string dataSetName = "Tag_Leaf0_10000_3085991c-b85c-4311-9bfb-a916da952234";
            const OpcAuthenticationMode authenticationMode = OpcAuthenticationMode.UsernamePassword;
            const string username = "usr";
            const string password = "pwd";

            var endpointRequest = new PublishedNodesEntryModel
            {
                EndpointUrl = endpointUrl,
                DataSetWriterGroup = dataSetWriterGroup,
                DataSetWriterId = dataSetWriterId,
                DataSetName = dataSetName,
                OpcAuthenticationMode = authenticationMode,
                OpcAuthenticationUsername = username,
                OpcAuthenticationPassword = password
            };

            var (d, methodsController) = await PublishNodeAsync(publishedNodesFile, a => a.DataSetWriterGroup == "Leaf0").ConfigureAwait(false);
            var response = await FluentActions
                    .Invoking(async () => await methodsController
                    .GetConfiguredNodesOnEndpointAsync(endpointRequest).ConfigureAwait(false))
                    .Should()
                    .NotThrowAsync()
                    .ConfigureAwait(false);
            d.Dispose();

            response.Subject.OpcNodes.Count
                .Should()
                .Be(1);
            response.Subject.OpcNodes[0].Id
                .Should()
                .Be("ns=2;s=SlowUInt1");
        }

        [Fact]
        public async Task DmApiGetConfiguredNodesOnEndpointAsyncDataSetWriterIdTestAsync()
        {
            // Testing that we can differentiate between endpoints
            // even if they only have different DataSetWriterIds.

            var opcNodes = Enumerable.Range(0, 5)
                .Select(i => new OpcNodeModel
                {
                    Id = $"nsu=http://microsoft.com/Opc/OpcPlc/;s=FastUInt{i}"
                })
                .ToList();

            var endpoints = Enumerable.Range(0, 5)
                .Select(i => new PublishedNodesEntryModel
                {
                    EndpointUrl = "opc.tcp://opcplc:50000",
                    DataSetWriterId = i != 0
                        ? $"DataSetWriterId{i}"
                        : null,
                    OpcNodes = opcNodes.GetRange(0, i + 1).ToList()
                })
                .ToList();

            var (d, methodsController) = await PublishNodeAsync("Controller/empty_pn.json").ConfigureAwait(false);

            for (var i = 0; i < 5; ++i)
            {
                await methodsController.PublishNodesAsync(endpoints[i]).ConfigureAwait(false);
            }

            for (var i = 0; i < 5; ++i)
            {
                var response = await FluentActions
                        .Invoking(async () => await methodsController
                        .GetConfiguredNodesOnEndpointAsync(endpoints[i]).ConfigureAwait(false))
                        .Should()
                        .NotThrowAsync()
                        .ConfigureAwait(false);

                response.Subject.OpcNodes.Count
                    .Should()
                    .Be(i + 1);
                response.Subject.OpcNodes.Last().Id
                    .Should()
                    .Be($"nsu=http://microsoft.com/Opc/OpcPlc/;s=FastUInt{i}");
            }
            d.Dispose();
        }

        [Theory]
        [InlineData("Controller/DmApiPayloadTwoEndpoints.json")]
        public async Task DmApiGetConfiguredNodesOnEndpointAsyncUseSecurityTestAsync(string publishedNodesFile)
        {
            const string endpointUrl = "opc.tcp://opcplc:50000";
            const bool useSecurity = false;

            var endpointRequest = new PublishedNodesEntryModel
            {
                EndpointUrl = endpointUrl,
                UseSecurity = useSecurity
            };

            var (d, methodsController) = await PublishNodeAsync(publishedNodesFile).ConfigureAwait(false);

            var response = await FluentActions
                    .Invoking(async () => await methodsController
                    .GetConfiguredNodesOnEndpointAsync(endpointRequest).ConfigureAwait(false))
                    .Should()
                    .NotThrowAsync()
                    .ConfigureAwait(false);
            d.Dispose();

            response.Subject.OpcNodes.Count
                .Should()
                .Be(1);
            response.Subject.OpcNodes[0].Id
                .Should()
                .Be("ns=2;s=SlowUInt3");
        }

        [Theory]
        [InlineData("Controller/DmApiPayloadTwoEndpoints.json")]
        public async Task DmApiGetConfiguredNodesOnEndpointAsyncOpcAuthenticationModeTestAsync(string publishedNodesFile)
        {
            const string endpointUrl = "opc.tcp://opcplc:50000";
            const string dataSetWriterGroup = "Leaf1";
            const string dataSetWriterId = "Leaf1_10000_3085991c-b85c-4311-9bfb-a916da952235";
            const string dataSetName = "Tag_Leaf1_10000_3085991c-b85c-4311-9bfb-a916da952235";
            const int dataSetPublishingInterval = 3000;
            const OpcAuthenticationMode authenticationMode = OpcAuthenticationMode.Anonymous;

            var endpointRequest = new PublishedNodesEntryModel
            {
                EndpointUrl = endpointUrl,
                DataSetWriterGroup = dataSetWriterGroup,
                DataSetWriterId = dataSetWriterId,
                DataSetName = dataSetName,
                DataSetPublishingInterval = dataSetPublishingInterval,
                OpcAuthenticationMode = authenticationMode
            };

            var (d, methodsController) = await PublishNodeAsync(publishedNodesFile, a => a.DataSetWriterGroup == "Leaf1").ConfigureAwait(false);

            var response = await FluentActions
                    .Invoking(async () => await methodsController
                    .GetConfiguredNodesOnEndpointAsync(endpointRequest).ConfigureAwait(false))
                    .Should()
                    .NotThrowAsync()
                    .ConfigureAwait(false);
            d.Dispose();

            response.Subject.OpcNodes.Count
                .Should()
                .Be(1);
            response.Subject.OpcNodes[0].Id
                .Should()
                .Be("ns=2;s=SlowUInt2");
        }

        [Theory]
        [InlineData("Controller/DmApiPayloadTwoEndpoints.json")]
        public async Task DmApiGetConfiguredNodesOnEndpointAsyncUsernamePasswordTestAsync(string publishedNodesFile)
        {
            const string endpointUrl = "opc.tcp://opcplc:50000";
            const OpcAuthenticationMode authenticationMode = OpcAuthenticationMode.UsernamePassword;
            const string username = "usr";
            const string password = "pwd";

            var endpointRequest = new PublishedNodesEntryModel
            {
                EndpointUrl = endpointUrl,
                OpcAuthenticationMode = authenticationMode,
                OpcAuthenticationUsername = username,
                OpcAuthenticationPassword = password
            };

            var (d, methodsController) = await PublishNodeAsync(publishedNodesFile).ConfigureAwait(false);

            var response = await FluentActions
                    .Invoking(async () => await methodsController
                    .GetConfiguredNodesOnEndpointAsync(endpointRequest).ConfigureAwait(false))
                    .Should()
                    .NotThrowAsync()
                    .ConfigureAwait(false);
            d.Dispose();

            response.Subject.OpcNodes.Count
                .Should()
                .Be(2);
            response.Subject.OpcNodes[0].Id
                .Should()
                .Be("ns=2;s=FastUInt3");
            response.Subject.OpcNodes[1].Id
                .Should()
                .Be("ns=2;s=FastUInt4");
        }

        /// <summary>
        /// publish nodes from publishedNodesFile
        /// </summary>
        /// <param name="publishedNodesFile"></param>
        /// <param name="predicate"></param>
        private async Task<(PublisherConfigurationService, PublisherMethodsController)> PublishNodeAsync(string publishedNodesFile,
            Func<PublishedNodesEntryModel, bool> predicate = null)
        {
            CopyContent("Controller/empty_pn.json", _tempFile);
            var configService = InitPublisherConfigService();

            var methodsController = new PublisherMethodsController(configService);

            using var publishPayloads = new StreamReader(publishedNodesFile);
            var publishNodesRequest = _newtonSoftJsonSerializer.Deserialize<List<PublishedNodesEntryModel>>(
                await publishPayloads.ReadToEndAsync().ConfigureAwait(false));

            foreach (var request in publishNodesRequest.Where(predicate ?? (_ => true)))
            {
                await FluentActions
                    .Invoking(async () => await methodsController.PublishNodesAsync(request).ConfigureAwait(false))
                    .Should()
                    .NotThrowAsync()
                    .ConfigureAwait(false);
            }
            return (configService, methodsController);
        }

        [Theory]
        [InlineData("Controller/DmApiPayloadCollection.json")]
        public async Task DmApiGetConfiguredEndpointsTestAsync(string publishedNodesFile)
        {
            CopyContent("Controller/empty_pn.json", _tempFile);
            using var configService = InitPublisherConfigService();
            var methodsController = new PublisherMethodsController(configService);

            using var publishPayloads = new StreamReader(publishedNodesFile);
            var publishNodesRequests = _newtonSoftJsonSerializer.Deserialize<List<PublishedNodesEntryModel>>
                (await publishPayloads.ReadToEndAsync().ConfigureAwait(false));

            // Check that GetConfiguredEndpointsAsync returns empty list
            var endpoints = await FluentActions
                .Invoking(async () => await methodsController
                .GetConfiguredEndpointsAsync().ConfigureAwait(false))
                .Should()
                .NotThrowAsync()
                .ConfigureAwait(false);

            endpoints.Subject.Endpoints.Count.Should().Be(0);

            // Publish nodes
            foreach (var request in publishNodesRequests)
            {
                await FluentActions
                    .Invoking(async () => await methodsController
                    .PublishNodesAsync(request).ConfigureAwait(false))
                    .Should()
                    .NotThrowAsync()
                    .ConfigureAwait(false);
            }

            // Check configured endpoints count
            endpoints = await FluentActions
                .Invoking(async () => await methodsController
                .GetConfiguredEndpointsAsync().ConfigureAwait(false))
                .Should()
                .NotThrowAsync()
                .ConfigureAwait(false);

            endpoints.Subject.Endpoints.Count.Should().Be(5);
            var tags = endpoints.Subject.Endpoints.Select(e => e.DataSetName).ToHashSet();
            tags.Should().Contain("Tag_Leaf0_10000_3085991c-b85c-4311-9bfb-a916da952234");
            tags.Should().Contain("Tag_Leaf1_10000_2e4fc28f-ffa2-4532-9f22-378d47bbee5d");
            tags.Should().Contain("Tag_Leaf2_10000_3085991c-b85c-4311-9bfb-a916da952234");
            tags.Should().Contain("Tag_Leaf3_10000_2e4fc28f-ffa2-4532-9f22-378d47bbee5d");
            tags.Should().Contain((string)null);

            var endpointsHash = endpoints.Subject.Endpoints.ConvertAll(e => e.GetHashCode());
            Assert.Equal(endpointsHash.Distinct().Count(), endpointsHash.Count);
        }

        [Fact]
        public async Task DmApiGetDiagnosticInfoTestAsync()
        {
            CopyContent("Controller/empty_pn.json", _tempFile);
            using var configService = InitPublisherConfigService();
            var methodsController = new PublisherMethodsController(configService);

            var response = await FluentActions
                    .Invoking(async () => await methodsController
                    .GetDiagnosticInfoAsync().ConfigureAwait(false))
                    .Should()
                    .NotThrowAsync()
                    .ConfigureAwait(false);

            response.Subject
                .Should()
                .NotBeNull();
        }

        /// <summary>
        /// Copy content of source file to destination file.
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <param name="destinationPath"></param>
        /// <returns></returns>
        private static void CopyContent(string sourcePath, string destinationPath)
        {
            var content = GetFileContent(sourcePath);

            using (var fileStream = new FileStream(destinationPath, FileMode.Open, FileAccess.Write, FileShare.ReadWrite))
            {
                fileStream.Write(Encoding.UTF8.GetBytes(content));
            }

            static string GetFileContent(string path)
            {
                using var payloadReader = new StreamReader(path);
                return payloadReader.ReadToEnd();
            }
        }
    }
}
