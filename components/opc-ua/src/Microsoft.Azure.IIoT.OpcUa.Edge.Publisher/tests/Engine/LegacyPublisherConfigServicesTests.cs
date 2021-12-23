// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Tests.Engine {
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Config.Models;
    using Microsoft.Azure.IIoT.Serializers;
    using System.Collections.Generic;
    using System.Linq;
    using System.IO;
    using System.Threading.Tasks;
    using Agent.Framework;
    using Agent.Framework.Models;
    using Diagnostics;
    using FluentAssertions;
    using Models;
    using Module;
    using Moq;
    using Publisher.Engine;
    using Serializers.NewtonSoft;
    using Xunit;
    using static Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Agent.PublisherJobsConfiguration;
    using System.Text;
    using System;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;

    /// <summary>
    /// Tests the Direct methods configuration for the LegacyJobOrchestrator class
    /// </summary>
    public class LegacyPublisherConfigServicesTests : IDisposable {

        private readonly string _tempPublishedNodesFile;

        public LegacyPublisherConfigServicesTests() {
            _tempPublishedNodesFile = Path.GetTempFileName();
        }

        public void Dispose() {
            try {
                // Remove temporary published nodes file if one was created.
                if (File.Exists(_tempPublishedNodesFile)) {
                    File.Delete(_tempPublishedNodesFile);
                }
            }
            catch (Exception) {
                // Nothign to do.
            }
        }

        [Theory]
        [InlineData("Engine/publishednodes.json")]
        [InlineData("Engine/publishednodeswithoptionalfields.json")]
        [InlineData("Engine/pn_assets.json")]
        [InlineData("Engine/pn_assets_with_optional_fields.json")]
        public async Task PublishNodesOnEmptyConfiguration(string publishedNodesFile) {
            var legacyCliModelProviderMock = new Mock<ILegacyCliModelProvider>();
            var agentConfigProviderMock = new Mock<IAgentConfigProvider>();
            var identityMock = new Mock<IIdentity>();
            var newtonSoftJsonSerializer = new NewtonSoftJsonSerializer();
            var jobSerializer = new PublisherJobSerializer(newtonSoftJsonSerializer);
            var publishedNodesJobConverter = new PublishedNodesJobConverter(TraceLogger.Create(), newtonSoftJsonSerializer);
            var legacyCliModel = new LegacyCliModel { PublishedNodesFile = "Engine/empty_pn.json", PublishedNodesSchemaFile = "Storage/publishednodesschema.json" };
            legacyCliModelProviderMock.Setup(p => p.LegacyCliModel).Returns(legacyCliModel);
            agentConfigProviderMock.Setup(p => p.Config).Returns(new AgentConfigModel());

            var orchestrator = new LegacyJobOrchestrator(publishedNodesJobConverter, legacyCliModelProviderMock.Object, agentConfigProviderMock.Object, jobSerializer, TraceLogger.Create(), identityMock.Object);

            using var payloads = new StreamReader(publishedNodesFile);
            var publishNodesRequest = newtonSoftJsonSerializer.Deserialize<List<PublishedNodesEntryModel>>(await payloads.ReadToEndAsync().ConfigureAwait(false));

            foreach (var request in publishNodesRequest) {
                var publishNodesResult = await FluentActions
                    .Invoking(async () => await orchestrator.PublishNodesAsync(request).ConfigureAwait(false))
                    .Should()
                    .NotThrowAsync()
                    .ConfigureAwait(false);

                publishNodesResult.Subject.First()
                    .Should()
                    .Contain("succeeded");
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
        public async Task PublishNodesOnExistingConfiguration(string existingConfig, string newConfig) {
            var legacyCliModelProviderMock = new Mock<ILegacyCliModelProvider>();
            var agentConfigProviderMock = new Mock<IAgentConfigProvider>();
            var identityMock = new Mock<IIdentity>();
            var newtonSoftJsonSerializer = new NewtonSoftJsonSerializer();
            var jobSerializer = new PublisherJobSerializer(newtonSoftJsonSerializer);
            var publishedNodesJobConverter = new PublishedNodesJobConverter(TraceLogger.Create(), newtonSoftJsonSerializer);

            var legacyCliModel = new LegacyCliModel { PublishedNodesFile = existingConfig, PublishedNodesSchemaFile = "Storage/publishednodesschema.json" };
            legacyCliModelProviderMock.Setup(p => p.LegacyCliModel).Returns(legacyCliModel);
            agentConfigProviderMock.Setup(p => p.Config).Returns(new AgentConfigModel());

            var orchestrator = new LegacyJobOrchestrator(publishedNodesJobConverter, legacyCliModelProviderMock.Object, agentConfigProviderMock.Object, jobSerializer, TraceLogger.Create(), identityMock.Object);

            using var payloads = new StreamReader(newConfig);
            var publishNodesRequest = newtonSoftJsonSerializer.Deserialize<List<PublishedNodesEntryModel>>(await payloads.ReadToEndAsync().ConfigureAwait(false));

            foreach (var request in publishNodesRequest) {
                var publishNodesResult = await FluentActions
                    .Invoking(async () => await orchestrator.PublishNodesAsync(request).ConfigureAwait(false))
                    .Should()
                    .NotThrowAsync()
                    .ConfigureAwait(false);

                publishNodesResult.Subject.First()
                    .Should()
                    .Contain("succeeded");
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
            var legacyCliModelProviderMock = new Mock<ILegacyCliModelProvider>();
            var agentConfigProviderMock = new Mock<IAgentConfigProvider>();
            var identityMock = new Mock<IIdentity>();
            var newtonSoftJsonSerializer = new NewtonSoftJsonSerializer();
            var jobSerializer = new PublisherJobSerializer(newtonSoftJsonSerializer);
            var publishedNodesJobConverter = new PublishedNodesJobConverter(TraceLogger.Create(), newtonSoftJsonSerializer);

            var legacyCliModel = new LegacyCliModel { PublishedNodesFile = existingConfig, PublishedNodesSchemaFile = "Storage/publishednodesschema.json" };
            legacyCliModelProviderMock.Setup(p => p.LegacyCliModel).Returns(legacyCliModel);
            agentConfigProviderMock.Setup(p => p.Config).Returns(new AgentConfigModel());

            var orchestrator = new LegacyJobOrchestrator(publishedNodesJobConverter, legacyCliModelProviderMock.Object, agentConfigProviderMock.Object, jobSerializer, TraceLogger.Create(), identityMock.Object);

            using var payloads = new StreamReader(newConfig);
            var publishNodesRequest = newtonSoftJsonSerializer.Deserialize<List<PublishedNodesEntryModel>>(await payloads.ReadToEndAsync().ConfigureAwait(false));

            foreach (var request in publishNodesRequest) {
                var publishNodesResult = await FluentActions
                    .Invoking(async () => await orchestrator.PublishNodesAsync(request).ConfigureAwait(false))
                    .Should()
                    .NotThrowAsync()
                    .ConfigureAwait(false);

                publishNodesResult.Subject.First()
                    .Should()
                    .Contain("succeeded");
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
            var legacyCliModelProviderMock = new Mock<ILegacyCliModelProvider>();
            var agentConfigProviderMock = new Mock<IAgentConfigProvider>();
            var identityMock = new Mock<IIdentity>();
            var newtonSoftJsonSerializer = new NewtonSoftJsonSerializer();
            var jobSerializer = new PublisherJobSerializer(newtonSoftJsonSerializer);
            var publishedNodesJobConverter = new PublishedNodesJobConverter(TraceLogger.Create(), newtonSoftJsonSerializer);

            var legacyCliModel = new LegacyCliModel { PublishedNodesFile = publishedNodesFile, PublishedNodesSchemaFile = "Storage/publishednodesschema.json" };
            legacyCliModelProviderMock.Setup(p => p.LegacyCliModel).Returns(legacyCliModel);
            agentConfigProviderMock.Setup(p => p.Config).Returns(new AgentConfigModel());

            var orchestrator = new LegacyJobOrchestrator(publishedNodesJobConverter, legacyCliModelProviderMock.Object, agentConfigProviderMock.Object, jobSerializer, TraceLogger.Create(), identityMock.Object);

            using var payloads = new StreamReader(publishedNodesFile);
            var unpublishNodesRequest = newtonSoftJsonSerializer.Deserialize<List<PublishedNodesEntryModel>>(await payloads.ReadToEndAsync().ConfigureAwait(false));

            foreach (var request in unpublishNodesRequest) {
                var unpublishNodesResult = await FluentActions
                    .Invoking(async () => await orchestrator.UnpublishNodesAsync(request).ConfigureAwait(false))
                    .Should()
                    .NotThrowAsync()
                    .ConfigureAwait(false);

                unpublishNodesResult.Subject.First()
                    .Should()
                    .Contain("succeeded");
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
            var legacyCliModelProviderMock = new Mock<ILegacyCliModelProvider>();
            var agentConfigProviderMock = new Mock<IAgentConfigProvider>();
            var identityMock = new Mock<IIdentity>();
            var newtonSoftJsonSerializer = new NewtonSoftJsonSerializer();
            var jobSerializer = new PublisherJobSerializer(newtonSoftJsonSerializer);
            var publishedNodesJobConverter = new PublishedNodesJobConverter(TraceLogger.Create(), newtonSoftJsonSerializer);

            var legacyCliModel = new LegacyCliModel { PublishedNodesFile = existingConfig, PublishedNodesSchemaFile = "Storage/publishednodesschema.json" };
            legacyCliModelProviderMock.Setup(p => p.LegacyCliModel).Returns(legacyCliModel);
            agentConfigProviderMock.Setup(p => p.Config).Returns(new AgentConfigModel());

            var orchestrator = new LegacyJobOrchestrator(publishedNodesJobConverter, legacyCliModelProviderMock.Object, agentConfigProviderMock.Object, jobSerializer, TraceLogger.Create(), identityMock.Object);

            using var payloads = new StreamReader(newConfig);
            var unpublishNodesRequest = newtonSoftJsonSerializer.Deserialize<List<PublishedNodesEntryModel>>(await payloads.ReadToEndAsync());

            foreach (var request in unpublishNodesRequest) {
                var unpublishNodesResult = await FluentActions
                    .Invoking(async () => await orchestrator.UnpublishNodesAsync(request).ConfigureAwait(false))
                    .Should()
                    .ThrowAsync<MethodCallStatusException>()
                    .WithMessage("Response 404 Endpoint not found.: {}")
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
            var legacyCliModelProviderMock = new Mock<ILegacyCliModelProvider>();
            var agentConfigProviderMock = new Mock<IAgentConfigProvider>();
            var identityMock = new Mock<IIdentity>();
            var newtonSoftJsonSerializer = new NewtonSoftJsonSerializer();
            var jobSerializer = new PublisherJobSerializer(newtonSoftJsonSerializer);
            var publishedNodesJobConverter = new PublishedNodesJobConverter(TraceLogger.Create(), newtonSoftJsonSerializer);

            using (var fileStream = new FileStream(_tempPublishedNodesFile, FileMode.Open, FileAccess.Write)) {
                fileStream.Write(Encoding.UTF8.GetBytes("[]"));
            }

            var legacyCliModel = new LegacyCliModel {
                PublishedNodesFile = _tempPublishedNodesFile,
                PublishedNodesSchemaFile = "Storage/publishednodesschema.json"
            };
            legacyCliModelProviderMock.Setup(p => p.LegacyCliModel).Returns(legacyCliModel);
            agentConfigProviderMock.Setup(p => p.Config).Returns(new AgentConfigModel());

            var orchestrator = new LegacyJobOrchestrator(
                publishedNodesJobConverter,
                legacyCliModelProviderMock.Object,
                agentConfigProviderMock.Object,
                jobSerializer,
                TraceLogger.Create(),
                identityMock.Object
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
                var publishNodesResult = await orchestrator.PublishNodesAsync(request).ConfigureAwait(false);
                publishNodesResult.First()
                    .Should()
                    .Contain("succeeded");
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
                var publishNodesResult = await orchestrator.PublishNodesAsync(request).ConfigureAwait(false);
                publishNodesResult.First()
                    .Should()
                    .Contain("succeeded");
            }

            // Check
            await CheckEndpointsAndNodes(numberOfEndpoints, numberOfNodes + 1).ConfigureAwait(false);

            // Unpublish new nodes for each endpoint.
            foreach (var request in payloadDiff) {
                var publishNodesResult = await orchestrator.UnpublishNodesAsync(request).ConfigureAwait(false);
                publishNodesResult.First()
                    .Should()
                    .Contain("succeeded");
            }

            // Check
            await CheckEndpointsAndNodes(numberOfEndpoints, numberOfNodes).ConfigureAwait(false);
        }
    }
}
