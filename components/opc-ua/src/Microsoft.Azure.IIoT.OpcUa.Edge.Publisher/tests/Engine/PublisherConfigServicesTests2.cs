// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Tests.Engine {
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Storage;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Tests.Utils;
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.OpcUa.Publisher;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Config.Models;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Serializers;
    using Autofac;
    using Diagnostics;
    using FluentAssertions;
    using Models;
    using Moq;
    using Publisher.Engine;
    using Serializers.NewtonSoft;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Xunit;

    /// <summary>
    /// Tests the Direct methods configuration for the standaloneJobOrchestrator class
    /// </summary>
    public class PublisherConfigServicesTests2 : TempFileProviderBase {

        [Theory]
        [InlineData("Engine/publishednodes.json")]
        [InlineData("Engine/publishednodeswithoptionalfields.json")]
        [InlineData("Engine/pn_assets.json")]
        [InlineData("Engine/pn_assets_with_optional_fields.json")]
        [InlineData("Engine/pn_events.json")]
        [InlineData("Engine/pn_pending_alarms.json")]
        public async Task PublishNodesOnEmptyConfiguration(string publishedNodesFile) {
            var standaloneCliModelProviderMock = new Mock<IStandaloneCliModelProvider>();
            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var newtonSoftJsonSerializer = new NewtonSoftJsonSerializer();
            var logger = TraceLogger.Create();
            var publishedNodesJobConverter = new PublishedNodesJobConverter(logger, newtonSoftJsonSerializer,
                engineConfigMock.Object, clientConfignMock.Object);
            var triggerMock = new Mock<IMessageTrigger>();
            var factoryMock = new Mock<IWriterGroupContainerFactory>();
            var lifetime = new Mock<IWriterGroup>();
            lifetime.SetupGet(l => l.Source).Returns(triggerMock.Object);
            factoryMock
                .Setup(factory => factory.CreateWriterGroupScope(It.IsAny<IWriterGroupConfig>()))
                .Returns(lifetime.Object);
            var publisher = new PublisherHostService(factoryMock.Object, logger);

            Utils.CopyContent("Engine/empty_pn.json", _tempFile);
            var standaloneCliModel = new StandaloneCliModel {
                PublishedNodesFile = _tempFile,
                PublishedNodesSchemaFile = "Storage/publishednodesschema.json"
            };

            standaloneCliModelProviderMock.Setup(p => p.StandaloneCliModel).Returns(standaloneCliModel);

            var publishedNodesProvider = new PublishedNodesProvider(standaloneCliModelProviderMock.Object, logger);

            var orchestrator = new PublisherConfigService(
                publishedNodesJobConverter,
                standaloneCliModelProviderMock.Object,
                publisher,
                logger,
                publishedNodesProvider,
                newtonSoftJsonSerializer
            );

            string payload = Utils.GetFileContent(publishedNodesFile);
            var publishNodesRequest = newtonSoftJsonSerializer.Deserialize<List<PublishedNodesEntryModel>>(payload);

            foreach (var request in publishNodesRequest) {
                await FluentActions
                    .Invoking(async () => await orchestrator.PublishNodesAsync(request).ConfigureAwait(false))
                    .Should()
                    .NotThrowAsync()
                    .ConfigureAwait(false);
            }

            publisher.WriterGroups.Count()
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
            var standaloneCliModelProviderMock = new Mock<IStandaloneCliModelProvider>();
            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var newtonSoftJsonSerializer = new NewtonSoftJsonSerializer();
            var logger = TraceLogger.Create();
            var publishedNodesJobConverter = new PublishedNodesJobConverter(logger, newtonSoftJsonSerializer,
                engineConfigMock.Object, clientConfignMock.Object);
            var triggerMock = new Mock<IMessageTrigger>();
            var factoryMock = new Mock<IWriterGroupContainerFactory>();
            var lifetime = new Mock<IWriterGroup>();
            lifetime.SetupGet(l => l.Source).Returns(triggerMock.Object);
            factoryMock
                .Setup(factory => factory.CreateWriterGroupScope(It.IsAny<IWriterGroupConfig>()))
                .Returns(lifetime.Object);
            var publisher = new PublisherHostService(factoryMock.Object, logger);

            Utils.CopyContent(existingConfig, _tempFile);
            var standaloneCliModel = new StandaloneCliModel {
                PublishedNodesFile = _tempFile,
                PublishedNodesSchemaFile = "Storage/publishednodesschema.json"
            };
            standaloneCliModelProviderMock.Setup(p => p.StandaloneCliModel).Returns(standaloneCliModel);

            var publishedNodesProvider = new PublishedNodesProvider(standaloneCliModelProviderMock.Object, logger);

            var orchestrator = new PublisherConfigService(
                publishedNodesJobConverter,
                standaloneCliModelProviderMock.Object,
                publisher,
                logger,
                publishedNodesProvider,
                newtonSoftJsonSerializer
            );

            string payload = Utils.GetFileContent(newConfig);
            var publishNodesRequest = newtonSoftJsonSerializer.Deserialize<List<PublishedNodesEntryModel>>(payload);

            foreach (var request in publishNodesRequest) {
                await FluentActions
                    .Invoking(async () => await orchestrator.PublishNodesAsync(request).ConfigureAwait(false))
                    .Should()
                    .NotThrowAsync()
                    .ConfigureAwait(false);
            }

            publisher.WriterGroups.Count()
                .Should()
                .Be(1);
        }

        [Theory]
        [InlineData("Engine/publishednodes.json", "Engine/pn_assets.json")]
        [InlineData("Engine/publishednodeswithoptionalfields.json", "Engine/pn_assets_with_optional_fields.json")]
        [InlineData("Engine/pn_assets.json", "Engine/publishednodes.json")]
        [InlineData("Engine/pn_assets_with_optional_fields.json", "Engine/publishednodeswithoptionalfields.json")]
        public async Task PublishNodesOnNewConfiguration(string existingConfig, string newConfig) {
            var standaloneCliModelProviderMock = new Mock<IStandaloneCliModelProvider>();
            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var newtonSoftJsonSerializer = new NewtonSoftJsonSerializer();
            var logger = TraceLogger.Create();
            var publishedNodesJobConverter = new PublishedNodesJobConverter(logger, newtonSoftJsonSerializer,
                engineConfigMock.Object, clientConfignMock.Object);
            var triggerMock = new Mock<IMessageTrigger>();
            var factoryMock = new Mock<IWriterGroupContainerFactory>();
            var lifetime = new Mock<IWriterGroup>();
            lifetime.SetupGet(l => l.Source).Returns(triggerMock.Object);
            factoryMock
                .Setup(factory => factory.CreateWriterGroupScope(It.IsAny<IWriterGroupConfig>()))
                .Returns(lifetime.Object);
            var publisher = new PublisherHostService(factoryMock.Object, logger);

            Utils.CopyContent(existingConfig, _tempFile);
            var standaloneCliModel = new StandaloneCliModel {
                PublishedNodesFile = _tempFile,
                PublishedNodesSchemaFile = "Storage/publishednodesschema.json"
            };
            standaloneCliModelProviderMock.Setup(p => p.StandaloneCliModel).Returns(standaloneCliModel);

            var publishedNodesProvider = new PublishedNodesProvider(standaloneCliModelProviderMock.Object, logger);

            var orchestrator = new PublisherConfigService(
                publishedNodesJobConverter,
                standaloneCliModelProviderMock.Object,
                publisher,
                logger,
                publishedNodesProvider,
                newtonSoftJsonSerializer
            );

            string payload = Utils.GetFileContent(newConfig);
            var publishNodesRequest = newtonSoftJsonSerializer.Deserialize<List<PublishedNodesEntryModel>>(payload);

            foreach (var request in publishNodesRequest) {
                await FluentActions
                    .Invoking(async () => await orchestrator.PublishNodesAsync(request).ConfigureAwait(false))
                    .Should()
                    .NotThrowAsync()
                    .ConfigureAwait(false);
            }

            publisher.WriterGroups.Count()
                .Should()
                .Be(1);
        }

        [Theory]
        [InlineData("Engine/publishednodes.json")]
        [InlineData("Engine/publishednodeswithoptionalfields.json")]
        [InlineData("Engine/pn_assets.json")]
        [InlineData("Engine/pn_assets_with_optional_fields.json")]
        public async Task UnpublishNodesOnExistingConfiguration(string publishedNodesFile) {
            var standaloneCliModelProviderMock = new Mock<IStandaloneCliModelProvider>();
            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var newtonSoftJsonSerializer = new NewtonSoftJsonSerializer();
            var logger = TraceLogger.Create();
            var publishedNodesJobConverter = new PublishedNodesJobConverter(logger, newtonSoftJsonSerializer,
                engineConfigMock.Object, clientConfignMock.Object);
            var triggerMock = new Mock<IMessageTrigger>();
            var factoryMock = new Mock<IWriterGroupContainerFactory>();
            var lifetime = new Mock<IWriterGroup>();
            lifetime.SetupGet(l => l.Source).Returns(triggerMock.Object);
            factoryMock
                .Setup(factory => factory.CreateWriterGroupScope(It.IsAny<IWriterGroupConfig>()))
                .Returns(lifetime.Object);
            var publisher = new PublisherHostService(factoryMock.Object, logger);

            Utils.CopyContent(publishedNodesFile, _tempFile);
            var standaloneCliModel = new StandaloneCliModel {
                PublishedNodesFile = _tempFile,
                PublishedNodesSchemaFile = "Storage/publishednodesschema.json"
            };
            standaloneCliModelProviderMock.Setup(p => p.StandaloneCliModel).Returns(standaloneCliModel);

            var publishedNodesProvider = new PublishedNodesProvider(standaloneCliModelProviderMock.Object, logger);

            var orchestrator = new PublisherConfigService(
                publishedNodesJobConverter,
                standaloneCliModelProviderMock.Object,
                publisher,
                logger,
                publishedNodesProvider,
                newtonSoftJsonSerializer
            );

            string payload = Utils.GetFileContent(publishedNodesFile);
            var unpublishNodesRequest = newtonSoftJsonSerializer.Deserialize<List<PublishedNodesEntryModel>>(payload);

            foreach (var request in unpublishNodesRequest) {
                await FluentActions
                    .Invoking(async () => await orchestrator.UnpublishNodesAsync(request).ConfigureAwait(false))
                    .Should()
                    .NotThrowAsync()
                    .ConfigureAwait(false);
            }

            publisher.WriterGroups.Count()
                .Should()
                .Be(0);
        }

        [Theory]
        [InlineData("Engine/publishednodes.json", "Engine/pn_assets.json")]
        [InlineData("Engine/publishednodeswithoptionalfields.json", "Engine/pn_assets_with_optional_fields.json")]
        [InlineData("Engine/pn_assets.json", "Engine/publishednodes.json")]
        [InlineData("Engine/pn_assets_with_optional_fields.json", "Engine/publishednodeswithoptionalfields.json")]
        public async Task UnpublishNodesOnNonExistingConfiguration(string existingConfig, string newConfig) {
            var standaloneCliModelProviderMock = new Mock<IStandaloneCliModelProvider>();
            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var newtonSoftJsonSerializer = new NewtonSoftJsonSerializer();
            var logger = TraceLogger.Create();
            var publishedNodesJobConverter = new PublishedNodesJobConverter(logger, newtonSoftJsonSerializer,
                engineConfigMock.Object, clientConfignMock.Object);
            var triggerMock = new Mock<IMessageTrigger>();
            var factoryMock = new Mock<IWriterGroupContainerFactory>();
            var lifetime = new Mock<IWriterGroup>();
            lifetime.SetupGet(l => l.Source).Returns(triggerMock.Object);
            factoryMock
                .Setup(factory => factory.CreateWriterGroupScope(It.IsAny<IWriterGroupConfig>()))
                .Returns(lifetime.Object);
            var publisher = new PublisherHostService(factoryMock.Object, logger);

            Utils.CopyContent(existingConfig, _tempFile);
            var standaloneCliModel = new StandaloneCliModel {
                PublishedNodesFile = _tempFile,
                PublishedNodesSchemaFile = "Storage/publishednodesschema.json"
            };
            standaloneCliModelProviderMock.Setup(p => p.StandaloneCliModel).Returns(standaloneCliModel);

            var publishedNodesProvider = new PublishedNodesProvider(standaloneCliModelProviderMock.Object, logger);

            var orchestrator = new PublisherConfigService(
                publishedNodesJobConverter,
                standaloneCliModelProviderMock.Object,
                publisher,
                logger,
                publishedNodesProvider,
                newtonSoftJsonSerializer
            );

            string payload = Utils.GetFileContent(newConfig);
            var unpublishNodesRequest = newtonSoftJsonSerializer.Deserialize<List<PublishedNodesEntryModel>>(payload);

            foreach (var request in unpublishNodesRequest) {
                await FluentActions
                    .Invoking(async () => await orchestrator.UnpublishNodesAsync(request).ConfigureAwait(false))
                    .Should()
                    .ThrowAsync<MethodCallStatusException>()
                    .WithMessage($"{{\"Message\":\"Response 404 Endpoint not found: {request.EndpointUrl}\",\"Details\":{{}}}}")
                    .ConfigureAwait(false);
            }

            publisher.WriterGroups.Sum(g => g.WriterGroup.DataSetWriters.Count)
                .Should()
                .Be(2);
        }

        [Theory]
        [InlineData(2, 10)]
        [InlineData(100, 1000)]
        public async Task PublishNodesStressTest(int numberOfEndpoints, int numberOfNodes) {
            var standaloneCliModelProviderMock = new Mock<IStandaloneCliModelProvider>();
            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var newtonSoftJsonSerializer = new NewtonSoftJsonSerializer();
            var logger = TraceLogger.Create();
            var publishedNodesJobConverter = new PublishedNodesJobConverter(logger, newtonSoftJsonSerializer,
                engineConfigMock.Object, clientConfignMock.Object);
            var triggerMock = new Mock<IMessageTrigger>();
            var factoryMock = new Mock<IWriterGroupContainerFactory>();
            var lifetime = new Mock<IWriterGroup>();
            lifetime.SetupGet(l => l.Source).Returns(triggerMock.Object);
            factoryMock
                .Setup(factory => factory.CreateWriterGroupScope(It.IsAny<IWriterGroupConfig>()))
                .Returns(lifetime.Object);
            var publisher = new PublisherHostService(factoryMock.Object, logger);

            using (var fileStream = new FileStream(_tempFile, FileMode.Open, FileAccess.Write)) {
                fileStream.Write(Encoding.UTF8.GetBytes("[]"));
            }

            var standaloneCliModel = new StandaloneCliModel {
                PublishedNodesFile = _tempFile,
                PublishedNodesSchemaFile = "Storage/publishednodesschema.json"
            };
            standaloneCliModelProviderMock.Setup(p => p.StandaloneCliModel).Returns(standaloneCliModel);

            var publishedNodesProvider = new PublishedNodesProvider(standaloneCliModelProviderMock.Object, logger);

            var orchestrator = new PublisherConfigService(
                publishedNodesJobConverter,
                standaloneCliModelProviderMock.Object,
                publisher,
                logger,
                publishedNodesProvider,
                newtonSoftJsonSerializer
            );

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
                    .Invoking(async () => await orchestrator.PublishNodesAsync(request).ConfigureAwait(false))
                    .Should()
                    .NotThrowAsync()
                    .ConfigureAwait(false);
            }

            void CheckEndpointsAndNodes(
                int expectedNumberOfEndpoints,
                int expectedNumberOfNodesPerEndpoint
            ) {
                var writerGroups = publisher.WriterGroups;
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
                    .Invoking(async () => await orchestrator.PublishNodesAsync(request).ConfigureAwait(false))
                    .Should()
                    .NotThrowAsync()
                    .ConfigureAwait(false);
            }

            // Check
            CheckEndpointsAndNodes(numberOfEndpoints, numberOfNodes + 1);

            // Unpublish new nodes for each endpoint.
            foreach (var request in payloadDiff) {
                await FluentActions
                    .Invoking(async () => await orchestrator.UnpublishNodesAsync(request).ConfigureAwait(false))
                    .Should()
                    .NotThrowAsync()
                    .ConfigureAwait(false);
            }

            // Check
            CheckEndpointsAndNodes(numberOfEndpoints, numberOfNodes);
        }
    }
}
