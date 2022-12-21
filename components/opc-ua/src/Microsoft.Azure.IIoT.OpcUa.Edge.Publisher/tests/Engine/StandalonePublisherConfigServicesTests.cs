// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Tests.Engine {
    using Agent.Framework;
    using Agent.Framework.Models;
    using Diagnostics;
    using FluentAssertions;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Storage;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Tests.Utils;
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.OpcUa.Publisher;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Config.Models;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.Azure.IIoT.Serializers;
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
    using static Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Agent.PublisherJobsConfiguration;

    /// <summary>
    /// Tests the Direct methods configuration for the standaloneJobOrchestrator class
    /// </summary>
    public class StandalonePublisherConfigServicesTests : TempFileProviderBase {

        [Theory]
        [InlineData("Engine/publishednodes.json")]
        [InlineData("Engine/publishednodeswithoptionalfields.json")]
        [InlineData("Engine/pn_assets.json")]
        [InlineData("Engine/pn_assets_with_optional_fields.json")]
        [InlineData("Engine/pn_events.json")]
        [InlineData("Engine/pn_pending_alarms.json")]
        public async Task PublishNodesOnEmptyConfiguration(string publishedNodesFile) {
            var standaloneCliModelProviderMock = new Mock<IStandaloneCliModelProvider>();
            var agentConfigProviderMock = new Mock<IAgentConfigProvider>();
            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var newtonSoftJsonSerializer = new NewtonSoftJsonSerializer();
            var jobSerializer = new PublisherJobSerializer(newtonSoftJsonSerializer);
            var logger = TraceLogger.Create();
            var publishedNodesJobConverter = new PublishedNodesJobConverter(logger, newtonSoftJsonSerializer,
                engineConfigMock.Object, clientConfignMock.Object);

            Utils.CopyContent("Engine/empty_pn.json", _tempFile);
            var standaloneCliModel = new StandaloneCliModel {
                PublishedNodesFile = _tempFile,
                PublishedNodesSchemaFile = "Storage/publishednodesschema.json"
            };

            standaloneCliModelProviderMock.Setup(p => p.StandaloneCliModel).Returns(standaloneCliModel);
            agentConfigProviderMock.Setup(p => p.Config).Returns(new AgentConfigModel());

            var publishedNodesProvider = new PublishedNodesProvider(standaloneCliModelProviderMock.Object, logger);

            var orchestrator = new StandaloneJobOrchestrator(
                publishedNodesJobConverter,
                standaloneCliModelProviderMock.Object,
                agentConfigProviderMock.Object,
                jobSerializer,
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

            var tasks = new List<Task<JobProcessingInstructionModel>>();
            for (var i = 0; i < 10; i++) {
                tasks.Add(orchestrator.GetAvailableJobAsync(i.ToString(), new JobRequestModel()));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
            tasks.Count(t => t.Result != null)
                .Should()
                .Be(2);

            var distinctConfigurations = tasks
                .Where(t => t.Result != null)
                .Select(t => t.Result.Job.JobConfiguration)
                .Distinct();
            distinctConfigurations.Count()
                .Should()
                .Be(2);
        }

        [Theory]
        [InlineData("Engine/publishednodes.json", "Engine/publishednodeswithoptionalfields.json")]
        [InlineData("Engine/publishednodeswithoptionalfields.json", "Engine/publishednodes.json")]
        [InlineData("Engine/pn_assets.json", "Engine/pn_assets_with_optional_fields.json")]
        [InlineData("Engine/pn_assets_with_optional_fields.json", "Engine/pn_assets.json")]
        [InlineData("Engine/pn_events.json", "Engine/pn_pending_alarms.json")]
        public async Task PublishNodesOnExistingConfiguration(string existingConfig, string newConfig) {
            var standaloneCliModelProviderMock = new Mock<IStandaloneCliModelProvider>();
            var agentConfigProviderMock = new Mock<IAgentConfigProvider>();
            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var newtonSoftJsonSerializer = new NewtonSoftJsonSerializer();
            var jobSerializer = new PublisherJobSerializer(newtonSoftJsonSerializer);
            var logger = TraceLogger.Create();
            var publishedNodesJobConverter = new PublishedNodesJobConverter(logger, newtonSoftJsonSerializer,
                engineConfigMock.Object, clientConfignMock.Object);

            Utils.CopyContent(existingConfig, _tempFile);
            var standaloneCliModel = new StandaloneCliModel {
                PublishedNodesFile = _tempFile,
                PublishedNodesSchemaFile = "Storage/publishednodesschema.json"
            };
            standaloneCliModelProviderMock.Setup(p => p.StandaloneCliModel).Returns(standaloneCliModel);
            agentConfigProviderMock.Setup(p => p.Config).Returns(new AgentConfigModel());

            var publishedNodesProvider = new PublishedNodesProvider(standaloneCliModelProviderMock.Object, logger);

            var orchestrator = new StandaloneJobOrchestrator(
                publishedNodesJobConverter,
                standaloneCliModelProviderMock.Object,
                agentConfigProviderMock.Object,
                jobSerializer,
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

            var tasks = new List<Task<JobProcessingInstructionModel>>();
            for (var i = 0; i < 10; i++) {
                tasks.Add(orchestrator.GetAvailableJobAsync(i.ToString(), new JobRequestModel()));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);

            tasks.Count(t => t.Result != null)
                .Should()
                .Be(2);

            var distinctConfigurations = tasks
                .Where(t => t.Result != null)
                .Select(t => t.Result.Job.JobConfiguration)
                .Distinct();
            distinctConfigurations.Count()
                .Should()
                .Be(2);
        }

        [Theory]
        [InlineData("Engine/publishednodes.json", "Engine/pn_assets.json")]
        [InlineData("Engine/publishednodeswithoptionalfields.json", "Engine/pn_assets_with_optional_fields.json")]
        [InlineData("Engine/pn_assets.json", "Engine/publishednodes.json")]
        [InlineData("Engine/pn_assets_with_optional_fields.json", "Engine/publishednodeswithoptionalfields.json")]
        public async Task PublishNodesOnNewConfiguration(string existingConfig, string newConfig) {
            var standaloneCliModelProviderMock = new Mock<IStandaloneCliModelProvider>();
            var agentConfigProviderMock = new Mock<IAgentConfigProvider>();
            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var newtonSoftJsonSerializer = new NewtonSoftJsonSerializer();
            var jobSerializer = new PublisherJobSerializer(newtonSoftJsonSerializer);
            var logger = TraceLogger.Create();
            var publishedNodesJobConverter = new PublishedNodesJobConverter(logger, newtonSoftJsonSerializer,
                engineConfigMock.Object, clientConfignMock.Object);

            Utils.CopyContent(existingConfig, _tempFile);
            var standaloneCliModel = new StandaloneCliModel {
                PublishedNodesFile = _tempFile,
                PublishedNodesSchemaFile = "Storage/publishednodesschema.json"
            };
            standaloneCliModelProviderMock.Setup(p => p.StandaloneCliModel).Returns(standaloneCliModel);
            agentConfigProviderMock.Setup(p => p.Config).Returns(new AgentConfigModel());

            var publishedNodesProvider = new PublishedNodesProvider(standaloneCliModelProviderMock.Object, logger);

            var orchestrator = new StandaloneJobOrchestrator(
                publishedNodesJobConverter,
                standaloneCliModelProviderMock.Object,
                agentConfigProviderMock.Object,
                jobSerializer,
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

            var tasks = new List<Task<JobProcessingInstructionModel>>();
            for (var i = 0; i < 10; i++) {
                tasks.Add(orchestrator.GetAvailableJobAsync(i.ToString(), new JobRequestModel()));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
            tasks.Count(t => t.Result != null)
                .Should()
                .Be(4);

            var distinctConfigurations = tasks
                .Where(t => t.Result != null)
                .Select(t => t.Result.Job.JobConfiguration)
                .Distinct();
            distinctConfigurations.Count()
                .Should()
                .Be(4);
        }

        [Theory]
        [InlineData("Engine/publishednodes.json")]
        [InlineData("Engine/publishednodeswithoptionalfields.json")]
        [InlineData("Engine/pn_assets.json")]
        [InlineData("Engine/pn_assets_with_optional_fields.json")]
        public async Task UnpublishNodesOnExistingConfiguration(string publishedNodesFile) {
            var standaloneCliModelProviderMock = new Mock<IStandaloneCliModelProvider>();
            var agentConfigProviderMock = new Mock<IAgentConfigProvider>();
            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var newtonSoftJsonSerializer = new NewtonSoftJsonSerializer();
            var jobSerializer = new PublisherJobSerializer(newtonSoftJsonSerializer);
            var logger = TraceLogger.Create();
            var publishedNodesJobConverter = new PublishedNodesJobConverter(logger, newtonSoftJsonSerializer,
                engineConfigMock.Object, clientConfignMock.Object);

            Utils.CopyContent(publishedNodesFile, _tempFile);
            var standaloneCliModel = new StandaloneCliModel {
                PublishedNodesFile = _tempFile,
                PublishedNodesSchemaFile = "Storage/publishednodesschema.json"
            };
            standaloneCliModelProviderMock.Setup(p => p.StandaloneCliModel).Returns(standaloneCliModel);
            agentConfigProviderMock.Setup(p => p.Config).Returns(new AgentConfigModel());

            var publishedNodesProvider = new PublishedNodesProvider(standaloneCliModelProviderMock.Object, logger);

            var orchestrator = new StandaloneJobOrchestrator(
                publishedNodesJobConverter,
                standaloneCliModelProviderMock.Object,
                agentConfigProviderMock.Object,
                jobSerializer,
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

            var tasks = new List<Task<JobProcessingInstructionModel>>();
            for (var i = 0; i < 10; i++) {
                tasks.Add(orchestrator.GetAvailableJobAsync(i.ToString(), new JobRequestModel()));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
            tasks.Count(t => t.Result != null)
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
            var agentConfigProviderMock = new Mock<IAgentConfigProvider>();
            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var newtonSoftJsonSerializer = new NewtonSoftJsonSerializer();
            var jobSerializer = new PublisherJobSerializer(newtonSoftJsonSerializer);
            var logger = TraceLogger.Create();
            var publishedNodesJobConverter = new PublishedNodesJobConverter(logger, newtonSoftJsonSerializer,
                engineConfigMock.Object, clientConfignMock.Object);

            Utils.CopyContent(existingConfig, _tempFile);
            var standaloneCliModel = new StandaloneCliModel {
                PublishedNodesFile = _tempFile,
                PublishedNodesSchemaFile = "Storage/publishednodesschema.json"
            };
            standaloneCliModelProviderMock.Setup(p => p.StandaloneCliModel).Returns(standaloneCliModel);
            agentConfigProviderMock.Setup(p => p.Config).Returns(new AgentConfigModel());

            var publishedNodesProvider = new PublishedNodesProvider(standaloneCliModelProviderMock.Object, logger);

            var orchestrator = new StandaloneJobOrchestrator(
                publishedNodesJobConverter,
                standaloneCliModelProviderMock.Object,
                agentConfigProviderMock.Object,
                jobSerializer,
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

            var tasks = new List<Task<JobProcessingInstructionModel>>();
            for (var i = 0; i < 10; i++) {
                tasks.Add(orchestrator.GetAvailableJobAsync(i.ToString(), new JobRequestModel()));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
            tasks.Count(t => t.Result != null)
                .Should()
                .Be(2);

            var distinctConfigurations = tasks
                .Where(t => t.Result != null)
                .Select(t => t.Result.Job.JobConfiguration)
                .Distinct();
            distinctConfigurations.Count()
                .Should()
                .Be(2);
        }

        [Fact]
        public async Task PublishNodesStressTest() {
            var standaloneCliModelProviderMock = new Mock<IStandaloneCliModelProvider>();
            var agentConfigProviderMock = new Mock<IAgentConfigProvider>();
            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();
            var newtonSoftJsonSerializer = new NewtonSoftJsonSerializer();
            var jobSerializer = new PublisherJobSerializer(newtonSoftJsonSerializer);
            var logger = TraceLogger.Create();
            var publishedNodesJobConverter = new PublishedNodesJobConverter(logger, newtonSoftJsonSerializer,
                engineConfigMock.Object, clientConfignMock.Object);

            using (var fileStream = new FileStream(_tempFile, FileMode.Open, FileAccess.Write)) {
                fileStream.Write(Encoding.UTF8.GetBytes("[]"));
            }

            var standaloneCliModel = new StandaloneCliModel {
                PublishedNodesFile = _tempFile,
                PublishedNodesSchemaFile = "Storage/publishednodesschema.json"
            };
            standaloneCliModelProviderMock.Setup(p => p.StandaloneCliModel).Returns(standaloneCliModel);
            agentConfigProviderMock.Setup(p => p.Config).Returns(new AgentConfigModel());

            var publishedNodesProvider = new PublishedNodesProvider(standaloneCliModelProviderMock.Object, logger);

            var orchestrator = new StandaloneJobOrchestrator(
                publishedNodesJobConverter,
                standaloneCliModelProviderMock.Object,
                agentConfigProviderMock.Object,
                jobSerializer,
                logger,
                publishedNodesProvider,
                newtonSoftJsonSerializer
            );

            var numberOfEndpoints = 100;
            var numberOfNodes = 1000;

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

            async Task CheckEndpointsAndNodes(
                int expectedNumberOfEndpoints,
                int expectedNumberOfNodes
            ) {
                var tasks = new List<Task<JobProcessingInstructionModel>>();
                for (var i = 0; i < expectedNumberOfEndpoints + 1; i++) {
                    tasks.Add(orchestrator.GetAvailableJobAsync(i.ToString(), new JobRequestModel()));
                }

                await Task.WhenAll(tasks).ConfigureAwait(false);
                tasks.Count(t => t.Result != null)
                    .Should()
                    .Be(expectedNumberOfEndpoints);

                var distinctConfigurations = tasks
                    .Where(t => t.Result != null)
                    .Select(t => t.Result.Job.JobConfiguration)
                    .Distinct();
                distinctConfigurations.Count()
                    .Should()
                    .Be(expectedNumberOfEndpoints);

                var writerGroups = tasks
                    .Where(t => t.Result != null)
                    .Select(t => jobSerializer.DeserializeJobConfiguration(
                        t.Result.Job.JobConfiguration, t.Result.Job.JobConfigurationType) as WriterGroupJobModel);
                writerGroups.Select(
                        jobModel => jobModel.WriterGroup.DataSetWriters
                        .Select(writer => writer.DataSet.DataSetSource.PublishedVariables.PublishedData.Count())
                        .Sum()
                     ).Count(v => v == expectedNumberOfNodes)
                     .Should()
                     .Be(expectedNumberOfEndpoints);
            }

            // Check
            await CheckEndpointsAndNodes(numberOfEndpoints, numberOfNodes).ConfigureAwait(false);

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
            await CheckEndpointsAndNodes(numberOfEndpoints, numberOfNodes + 1).ConfigureAwait(false);

            // Unpublish new nodes for each endpoint.
            foreach (var request in payloadDiff) {
                await FluentActions
                    .Invoking(async () => await orchestrator.UnpublishNodesAsync(request).ConfigureAwait(false))
                    .Should()
                    .NotThrowAsync()
                    .ConfigureAwait(false);
            }

            // Check
            await CheckEndpointsAndNodes(numberOfEndpoints, numberOfNodes).ConfigureAwait(false);
        }
    }
}
