// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Tests.Engine {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Agent.Framework;
    using Agent.Framework.Models;
    using Diagnostics;
    using FluentAssertions;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Storage;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Tests.Utils;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Config.Models;
    using Models;
    using Moq;
    using Publisher.Engine;
    using Serializers.NewtonSoft;
    using Serilog;
    using Xunit;
    using static Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Agent.PublisherJobsConfiguration;

    /// <summary>
    /// Tests the LegacyJobOrchestrator class
    /// </summary>
    public class LegacyJobOrchestratorTests : TempFileProviderBase {

        private readonly AgentConfigModel _agentConfigModel;
        private readonly Mock<IAgentConfigProvider> _agentConfigProviderMock;
        private readonly NewtonSoftJsonSerializer _newtonSoftJsonSerializer;
        private readonly PublisherJobSerializer _publisherJobSerializer;
        private readonly ILogger _logger;
        private readonly PublishedNodesJobConverter _publishedNodesJobConverter;
        private readonly LegacyCliModel _legacyCliModel;
        private readonly Mock<ILegacyCliModelProvider> _legacyCliModelProviderMock;
        LegacyJobOrchestrator _legacyJobOrchestrator;
        private readonly PublishedNodesProvider _publishedNodesProvider;

        /// <summary>
        /// Constructor that initializes common resources used by tests.
        /// </summary>
        public LegacyJobOrchestratorTests() {
            _agentConfigModel = new AgentConfigModel();
            _agentConfigProviderMock = new Mock<IAgentConfigProvider>();
            _agentConfigProviderMock.Setup(p => p.Config).Returns(_agentConfigModel);

            _newtonSoftJsonSerializer = new NewtonSoftJsonSerializer();
            _publisherJobSerializer = new PublisherJobSerializer(_newtonSoftJsonSerializer);
            _logger = TraceLogger.Create();
            _publishedNodesJobConverter = new PublishedNodesJobConverter(_logger, _newtonSoftJsonSerializer);

            // Note that each test is responsible for setting content of _tempFile;
            Utils.CopyContent("Engine/empty_pn.json", _tempFile);
            _legacyCliModel = new LegacyCliModel {
                PublishedNodesFile = _tempFile,
                PublishedNodesSchemaFile = "Storage/publishednodesschema.json"
            };

            _legacyCliModelProviderMock = new Mock<ILegacyCliModelProvider>();
            _legacyCliModelProviderMock.Setup(p => p.LegacyCliModel).Returns(_legacyCliModel);

            _publishedNodesProvider = new PublishedNodesProvider(_legacyCliModelProviderMock.Object, _logger);
        }

        /// <summary>
        /// Initializes _legacyJobOrchestrator.
        /// This method should be called only after content of _tempFile is set.
        /// </summary>
        private void InitLegacyJobOrchestrator() {
            _legacyJobOrchestrator = new LegacyJobOrchestrator(
                _publishedNodesJobConverter,
                _legacyCliModelProviderMock.Object,
                _agentConfigProviderMock.Object,
                _publisherJobSerializer,
                _logger,
                _publishedNodesProvider,
                _newtonSoftJsonSerializer
            );
        }

        [Theory]
        [InlineData("Engine/publishednodes.json")]
        [InlineData("Engine/publishednodeswithoptionalfields.json")]
        public async Task GetAvailableJobAsyncMulithreading(string publishedNodesFile) {
            Utils.CopyContent(publishedNodesFile, _tempFile);
            InitLegacyJobOrchestrator();

            var tasks = new List<Task<JobProcessingInstructionModel>>();
            for (var i = 0; i < 10; i++) {
                tasks.Add(_legacyJobOrchestrator.GetAvailableJobAsync(i.ToString(), new JobRequestModel()));
            }

            await Task.WhenAll(tasks);

            Assert.Equal(2, tasks.Count(t => t.Result != null));
            var distinctConfigurations = tasks
                .Where(t => t.Result != null)
                .Select(t => t.Result.Job.JobConfiguration)
                .Distinct();
            Assert.Equal(2, distinctConfigurations.Count());
        }

        [Theory]
        [InlineData("Engine/pn_assets.json")]
        [InlineData("Engine/pn_assets_with_optional_fields.json")]
        public void Test_PnJson_With_Multiple_Jobs_Expect_DifferentJobIds(string publishedNodesFile) {
            Utils.CopyContent(publishedNodesFile, _tempFile);
            InitLegacyJobOrchestrator();

            var job1 = _legacyJobOrchestrator.GetAvailableJobAsync(1.ToString(), new JobRequestModel()).GetAwaiter().GetResult();
            Assert.NotNull(job1);
            var job2 = _legacyJobOrchestrator.GetAvailableJobAsync(2.ToString(), new JobRequestModel()).GetAwaiter().GetResult();
            Assert.NotNull(job2);
            var job3 = _legacyJobOrchestrator.GetAvailableJobAsync(3.ToString(), new JobRequestModel()).GetAwaiter().GetResult();
            Assert.Null(job3);

            Assert.NotEqual(job1.Job.Id, job2.Job.Id);
        }

        [Fact]
        public async Task Test_AddOrUpdateEndpoints_NullRequest() {
            InitLegacyJobOrchestrator();

            // Check call with null.
            await FluentActions
                .Invoking(async () => await _legacyJobOrchestrator
                    .AddOrUpdateEndpointsAsync(null)
                    .ConfigureAwait(false))
                .Should()
                .ThrowAsync<MethodCallStatusException>()
                .WithMessage($"Response 400 null request provided: {{}}")
                .ConfigureAwait(false);
        }

        [Fact]
        public async Task Test_AddOrUpdateEndpoints_AddAndRemove() {
            _legacyCliModel.DefaultMaxNodesPerDataSet = 2;

            InitLegacyJobOrchestrator();

            var job0 = _legacyJobOrchestrator.GetAvailableJobAsync(0.ToString(), new JobRequestModel()).GetAwaiter().GetResult();
            Assert.Null(job0);

            var opcNodes = Enumerable.Range(0, 5)
                .Select(i => new OpcNodeModel {
                    Id = $"nsu=http://microsoft.com/Opc/OpcPlc/;s=FastUInt{i}",
                })
                .ToList();

            // Helper method.
            PublishedNodesEntryModel GenerateEndpoint(int i, List<OpcNodeModel> opcNodes) {
                return new PublishedNodesEntryModel {
                    EndpointUrl = new Uri("opc.tcp://opcplc:50000"),
                    DataSetWriterId = $"DataSetWriterId{i}",
                    DataSetWriterGroup = "DataSetWriterGroup",
                    DataSetPublishingInterval = (i + 1) * 1000,
                    OpcNodes = opcNodes.GetRange(0, i + 1).ToList(),
                };
            }

            // Helper method.
            void AssertSameNodes(PublishedNodesEntryModel endpoint, List<OpcNodeModel> nodes) {
                Assert.Equal(endpoint.OpcNodes.Count, nodes.Count);
                for (var k = 0; k < endpoint.OpcNodes.Count; k++) {
                    Assert.True(endpoint.OpcNodes[k].IsSame(nodes[k]));
                }
            }

            // Helper method.
            async Task AssertGetConfiguredNodesOnEndpointThrows(
                LegacyJobOrchestrator legacyJobOrchestrator,
                PublishedNodesEntryModel endpoint
            ) {
                await FluentActions
                    .Invoking(async () => await legacyJobOrchestrator
                        .GetConfiguredNodesOnEndpointAsync(endpoint)
                        .ConfigureAwait(false))
                    .Should()
                    .ThrowAsync<MethodCallStatusException>()
                    .WithMessage($"Response 404 Endpoint not found: {endpoint.EndpointUrl}: {{}}")
                    .ConfigureAwait(false);
            }

            var endpoints = Enumerable.Range(0, 5)
                .Select(i => GenerateEndpoint(i, opcNodes))
                .ToList();

            var tasks = new List<Task<List<string>>>();
            for (var i = 0; i < 3; i++) {
                tasks.Add(_legacyJobOrchestrator.PublishNodesAsync(endpoints[i]));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);

            for (var i = 0; i < 3; i++) {
                var endpointNodes = await _legacyJobOrchestrator
                    .GetConfiguredNodesOnEndpointAsync(endpoints[i])
                    .ConfigureAwait(false);

                AssertSameNodes(endpoints[i], endpointNodes);
            }

            // Those calls should throw.
            for (var i = 3; i < 5; i++) {
                await AssertGetConfiguredNodesOnEndpointThrows(_legacyJobOrchestrator, endpoints[i])
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
                .Invoking(async () => await _legacyJobOrchestrator
                    .AddOrUpdateEndpointsAsync(updateRequest)
                    .ConfigureAwait(false))
                .Should()
                .ThrowAsync<MethodCallStatusException>()
                .WithMessage($"Response 404 Endpoint not found: {updateRequest[3].EndpointUrl}: {{}}")
                .ConfigureAwait(false);

            updateRequest.RemoveAt(3);
            var result = await _legacyJobOrchestrator
                .AddOrUpdateEndpointsAsync(updateRequest)
                .ConfigureAwait(false);

            Assert.Equal(result.Count, 4);

            // Check endpoint 0.
            await AssertGetConfiguredNodesOnEndpointThrows(_legacyJobOrchestrator, endpoints[0])
                .ConfigureAwait(false);

            // Check endpoint 1.
            await AssertGetConfiguredNodesOnEndpointThrows(_legacyJobOrchestrator, endpoints[1])
                .ConfigureAwait(false);

            // Check endpoint 2.
            var endpointNodes2 = await _legacyJobOrchestrator
                .GetConfiguredNodesOnEndpointAsync(endpoints[2])
                .ConfigureAwait(false);

            AssertSameNodes(updateRequest[2], endpointNodes2);

            // Check endpoint 3.
            await AssertGetConfiguredNodesOnEndpointThrows(_legacyJobOrchestrator, endpoints[3])
                .ConfigureAwait(false);

            // Check endpoint 4.
            var endpointNodes4 = await _legacyJobOrchestrator
                .GetConfiguredNodesOnEndpointAsync(endpoints[4])
                .ConfigureAwait(false);

            AssertSameNodes(updateRequest[3], endpointNodes4);
        }
    }
}
