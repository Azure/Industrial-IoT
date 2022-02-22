// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Tests.Engine {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Agent.Framework;
    using Agent.Framework.Models;
    using Diagnostics;
    using FluentAssertions;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Storage;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Tests.Utils;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Config.Models;
    using Microsoft.Azure.IIoT.Serializers;
    using Models;
    using Moq;
    using Publisher.Engine;
    using Serializers.NewtonSoft;
    using Serilog;
    using Xunit;
    using static Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Agent.PublisherJobsConfiguration;

    /// <summary>
    /// Tests the StandaloneJobOrchestrator class
    /// </summary>
    public class StandaloneJobOrchestratorTests : TempFileProviderBase {

        private readonly AgentConfigModel _agentConfigModel;
        private readonly Mock<IAgentConfigProvider> _agentConfigProviderMock;
        private readonly NewtonSoftJsonSerializer _newtonSoftJsonSerializer;
        private readonly PublisherJobSerializer _publisherJobSerializer;
        private readonly ILogger _logger;
        private readonly PublishedNodesJobConverter _publishedNodesJobConverter;
        private readonly StandaloneCliModel _standaloneCliModel;
        private readonly Mock<IStandaloneCliModelProvider> _standaloneCliModelProviderMock;
        StandaloneJobOrchestrator _standaloneJobOrchestrator;
        private readonly PublishedNodesProvider _publishedNodesProvider;

        /// <summary>
        /// Constructor that initializes common resources used by tests.
        /// </summary>
        public StandaloneJobOrchestratorTests() {
            _agentConfigModel = new AgentConfigModel();
            _agentConfigProviderMock = new Mock<IAgentConfigProvider>();
            _agentConfigProviderMock.Setup(p => p.Config).Returns(_agentConfigModel);

            _newtonSoftJsonSerializer = new NewtonSoftJsonSerializer();
            _publisherJobSerializer = new PublisherJobSerializer(_newtonSoftJsonSerializer);
            _logger = TraceLogger.Create();
            _publishedNodesJobConverter = new PublishedNodesJobConverter(_logger, _newtonSoftJsonSerializer);

            // Note that each test is responsible for setting content of _tempFile;
            Utils.CopyContent("Engine/empty_pn.json", _tempFile);
            _standaloneCliModel = new StandaloneCliModel {
                PublishedNodesFile = _tempFile,
                PublishedNodesSchemaFile = "Storage/publishednodesschema.json"
            };

            _standaloneCliModelProviderMock = new Mock<IStandaloneCliModelProvider>();
            _standaloneCliModelProviderMock.Setup(p => p.StandaloneCliModel).Returns(_standaloneCliModel);

            _publishedNodesProvider = new PublishedNodesProvider(_standaloneCliModelProviderMock.Object, _logger);
        }

        /// <summary>
        /// Initializes _standaloneJobOrchestrator.
        /// This method should be called only after content of _tempFile is set.
        /// </summary>
        private void InitStandaloneJobOrchestrator() {
            _standaloneJobOrchestrator = new StandaloneJobOrchestrator(
                _publishedNodesJobConverter,
                _standaloneCliModelProviderMock.Object,
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
            InitStandaloneJobOrchestrator();

            var tasks = new List<Task<JobProcessingInstructionModel>>();
            for (var i = 0; i < 10; i++) {
                tasks.Add(_standaloneJobOrchestrator.GetAvailableJobAsync(i.ToString(), new JobRequestModel()));
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
            InitStandaloneJobOrchestrator();

            var job1 = _standaloneJobOrchestrator.GetAvailableJobAsync(1.ToString(), new JobRequestModel()).GetAwaiter().GetResult();
            Assert.NotNull(job1);
            var job2 = _standaloneJobOrchestrator.GetAvailableJobAsync(2.ToString(), new JobRequestModel()).GetAwaiter().GetResult();
            Assert.NotNull(job2);
            var job3 = _standaloneJobOrchestrator.GetAvailableJobAsync(3.ToString(), new JobRequestModel()).GetAwaiter().GetResult();
            Assert.Null(job3);

            Assert.NotEqual(job1.Job.Id, job2.Job.Id);
        }

        [Fact]
        public async Task Test_PublishNodes_NullOrEmpty() {
            InitStandaloneJobOrchestrator();

            // Check null request.
            await FluentActions
                .Invoking(async () => await _standaloneJobOrchestrator
                    .PublishNodesAsync(null)
                    .ConfigureAwait(false))
                .Should()
                .ThrowAsync<MethodCallStatusException>()
                .WithMessage($"Response 400 null request is provided: {{}}")
                .ConfigureAwait(false);


            var request = new PublishedNodesEntryModel {
                EndpointUrl = new Uri("opc.tcp://opcplc:50000"),
            };

            // Check null OpcNodes in request.
            await FluentActions
                .Invoking(async () => await _standaloneJobOrchestrator
                    .PublishNodesAsync(request)
                    .ConfigureAwait(false))
                .Should()
                .ThrowAsync<MethodCallStatusException>()
                .WithMessage($"Response 400 null or empty OpcNodes is provided in request: {{}}")
                .ConfigureAwait(false);

            request.OpcNodes = new List<OpcNodeModel>();

            // Check empty OpcNodes in request.
            await FluentActions
                .Invoking(async () => await _standaloneJobOrchestrator
                    .PublishNodesAsync(request)
                    .ConfigureAwait(false))
                .Should()
                .ThrowAsync<MethodCallStatusException>()
                .WithMessage($"Response 400 null or empty OpcNodes is provided in request: {{}}")
                .ConfigureAwait(false);
        }

        [Fact]
        public async Task Test_UnpublishNodes_NullOrEmpty() {
            InitStandaloneJobOrchestrator();

            // Check null request.
            await FluentActions
                .Invoking(async () => await _standaloneJobOrchestrator
                    .UnpublishNodesAsync(null)
                    .ConfigureAwait(false))
                .Should()
                .ThrowAsync<MethodCallStatusException>()
                .WithMessage($"Response 400 null request is provided: {{}}")
                .ConfigureAwait(false);


            var request = new PublishedNodesEntryModel {
                EndpointUrl = new Uri("opc.tcp://opcplc:50000"),
            };

            // Check null OpcNodes in request.
            await FluentActions
                .Invoking(async () => await _standaloneJobOrchestrator
                    .UnpublishNodesAsync(request)
                    .ConfigureAwait(false))
                .Should()
                .ThrowAsync<MethodCallStatusException>()
                .WithMessage($"Response 400 null or empty OpcNodes is provided in request: {{}}")
                .ConfigureAwait(false);

            request.OpcNodes = new List<OpcNodeModel>();

            // Check empty OpcNodes in request.
            await FluentActions
                .Invoking(async () => await _standaloneJobOrchestrator
                    .UnpublishNodesAsync(request)
                    .ConfigureAwait(false))
                .Should()
                .ThrowAsync<MethodCallStatusException>()
                .WithMessage($"Response 400 null or empty OpcNodes is provided in request: {{}}")
                .ConfigureAwait(false);
        }

        [Fact]
        public async Task Test_GetConfiguredNodesOnEndpoint_NullRequest() {
            InitStandaloneJobOrchestrator();

            // Check call with null.
            await FluentActions
                .Invoking(async () => await _standaloneJobOrchestrator
                    .GetConfiguredNodesOnEndpointAsync(null)
                    .ConfigureAwait(false))
                .Should()
                .ThrowAsync<MethodCallStatusException>()
                .WithMessage($"Response 400 null request is provided: {{}}")
                .ConfigureAwait(false);
        }

        [Fact]
        public async Task Test_AddOrUpdateEndpoints_NullRequest() {
            InitStandaloneJobOrchestrator();

            // Check call with null.
            await FluentActions
                .Invoking(async () => await _standaloneJobOrchestrator
                    .AddOrUpdateEndpointsAsync(null)
                    .ConfigureAwait(false))
                .Should()
                .ThrowAsync<MethodCallStatusException>()
                .WithMessage($"Response 400 null request is provided: {{}}")
                .ConfigureAwait(false);
        }

        [Fact]
        public async Task Test_AddOrUpdateEndpoints_MultipleEndpointEntries() {
            InitStandaloneJobOrchestrator();

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
                .Invoking(async () => await _standaloneJobOrchestrator
                    .AddOrUpdateEndpointsAsync(endpoints)
                    .ConfigureAwait(false))
                .Should()
                .ThrowAsync<MethodCallStatusException>()
                .WithMessage($"Response 400 Request contains two entries for the same endpoint at index 0 and 2: {{}}")
                .ConfigureAwait(false);
        }

        [Fact]
        public async Task Test_AddOrUpdateEndpoints_MultipleEndpointEntries_Timesapn() {
            InitStandaloneJobOrchestrator();

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
                .Invoking(async () => await _standaloneJobOrchestrator
                    .AddOrUpdateEndpointsAsync(endpoints)
                    .ConfigureAwait(false))
                .Should()
                .ThrowAsync<MethodCallStatusException>()
                .WithMessage($"Response 400 Request contains two entries for the same endpoint at index 0 and 2: {{}}")
                .ConfigureAwait(false);
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public async Task Test_AddOrUpdateEndpoints_AddEndpoints(
            bool useDataSetSpecificEndpoints,
            bool enableAvailableJobQuerying
        ) {
            _standaloneCliModel.MaxNodesPerDataSet = 2;

            InitStandaloneJobOrchestrator();

            var job0 = _standaloneJobOrchestrator.GetAvailableJobAsync(0.ToString(), new JobRequestModel()).GetAwaiter().GetResult();
            Assert.Null(job0);

            var numberOfEndpoints = 100;

            var opcNodes = Enumerable.Range(0, numberOfEndpoints)
                .Select(i => new OpcNodeModel {
                    Id = $"nsu=http://microsoft.com/Opc/OpcPlc/;s=FastUInt{i}",
                })
                .ToList();

            var endpoints = Enumerable.Range(0, numberOfEndpoints)
                .Select(i => GenerateEndpoint(i, opcNodes, useDataSetSpecificEndpoints))
                .ToList();

            async Task CallGetAvailableJobAsync(
                StandaloneJobOrchestrator standaloneJobOrchestrator,
                string workerId,
                CancellationToken cancellationToken
            ) {
                try {
                    while (!cancellationToken.IsCancellationRequested) {
                        var jobProcessingInstructionModel = await standaloneJobOrchestrator
                            .GetAvailableJobAsync(workerId, null, cancellationToken)
                            .ConfigureAwait(false);

                        // Wait in between calls.
                        await Task.Delay(100, cancellationToken).ConfigureAwait(false);
                    }
                }
                catch(OperationCanceledException) {
                    // Nothing to do, just ignore.
                }
            }

            var cts = new CancellationTokenSource();
            var getAvailableJobTasks = new List<Task>();
            if (enableAvailableJobQuerying) {
                for (var i = 0; i < numberOfEndpoints; i++) {
                    getAvailableJobTasks.Add(CallGetAvailableJobAsync(_standaloneJobOrchestrator, i.ToString(), cts.Token));
                }
            }

            var tasks = new List<Task>();
            for (var i = 0; i < numberOfEndpoints; i++) {
                tasks.Add(_standaloneJobOrchestrator.AddOrUpdateEndpointsAsync(
                    new List<PublishedNodesEntryModel> { endpoints[i] }));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);

            for (var i = 0; i < numberOfEndpoints; i++) {
                var endpointNodes = await _standaloneJobOrchestrator
                    .GetConfiguredNodesOnEndpointAsync(endpoints[i])
                    .ConfigureAwait(false);

                AssertSameNodes(endpoints[i], endpointNodes);
            }

            cts.Cancel();
            await Task.WhenAll(getAvailableJobTasks).ConfigureAwait(false);
        }

        [Theory]
        [InlineData("Engine/empty_pn.json")]
        [InlineData("Engine/pn_assets.json")]
        [InlineData("Engine/pn_assets_with_optional_fields.json")]
        [InlineData("Engine/publishednodes.json")]
        [InlineData("Engine/publishednodeswithoptionalfields.json")]
        [InlineData("Controller/DmApiPayloadCollection.json")]
        [InlineData("Controller/DmApiPayloadTwoEndpoints.json")]
        public async Task Test_AddOrUpdateEndpoints_RemoveEndpoints(string publishedNodesFile) {
            Utils.CopyContent(publishedNodesFile, _tempFile);
            InitStandaloneJobOrchestrator();

            string payload = Utils.GetFileContent(publishedNodesFile);
            var payloadRequests = _newtonSoftJsonSerializer.Deserialize<List<PublishedNodesEntryModel>>(payload);

            var previousDataSets = new List<PublishedNodesEntryModel>();

            int index = 0;
            foreach (var request in payloadRequests) {
                request.OpcNodes = index % 2 == 0
                    ? null
                    : new List<OpcNodeModel>();
                ++index;

                var sameDataSetRemoved = false;
                foreach(var dataSet in previousDataSets) {
                    sameDataSetRemoved = sameDataSetRemoved || request.HasSameDataSet(dataSet);
                }

                if (sameDataSetRemoved) {
                    await FluentActions
                        .Invoking(async () => await _standaloneJobOrchestrator
                            .AddOrUpdateEndpointsAsync(new List<PublishedNodesEntryModel> { request })
                            .ConfigureAwait(false))
                        .Should()
                        .ThrowAsync<MethodCallStatusException>()
                        .WithMessage($"Response 404 Endpoint not found: {request.EndpointUrl}: {{}}")
                        .ConfigureAwait(false);
                }
                else {
                    await FluentActions
                        .Invoking(async () => await _standaloneJobOrchestrator
                            .AddOrUpdateEndpointsAsync(new List<PublishedNodesEntryModel> { request })
                            .ConfigureAwait(false))
                        .Should()
                        .NotThrowAsync()
                        .ConfigureAwait(false);

                    previousDataSets.Add(request);
                }
            }

            var configuredEndpoints = await _standaloneJobOrchestrator
                .GetConfiguredEndpointsAsync()
                .ConfigureAwait(false);
            Assert.Equal(0, configuredEndpoints.Count);
        }

        [Fact]
        public async Task Test_AddOrUpdateEndpoints_AddAndRemove() {
            _standaloneCliModel.MaxNodesPerDataSet = 2;

            InitStandaloneJobOrchestrator();

            var job0 = _standaloneJobOrchestrator.GetAvailableJobAsync(0.ToString(), new JobRequestModel()).GetAwaiter().GetResult();
            Assert.Null(job0);

            var opcNodes = Enumerable.Range(0, 5)
                .Select(i => new OpcNodeModel {
                    Id = $"nsu=http://microsoft.com/Opc/OpcPlc/;s=FastUInt{i}",
                })
                .ToList();

            // Helper method.
            async Task AssertGetConfiguredNodesOnEndpointThrows(
                StandaloneJobOrchestrator standaloneJobOrchestrator,
                PublishedNodesEntryModel endpoint
            ) {
                await FluentActions
                    .Invoking(async () => await standaloneJobOrchestrator
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

            var tasks = new List<Task>();
            for (var i = 0; i < 3; i++) {
                tasks.Add(_standaloneJobOrchestrator.PublishNodesAsync(endpoints[i]));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);

            for (var i = 0; i < 3; i++) {
                var endpointNodes = await _standaloneJobOrchestrator
                    .GetConfiguredNodesOnEndpointAsync(endpoints[i])
                    .ConfigureAwait(false);

                AssertSameNodes(endpoints[i], endpointNodes);
            }

            // Those calls should throw.
            for (var i = 3; i < 5; i++) {
                await AssertGetConfiguredNodesOnEndpointThrows(_standaloneJobOrchestrator, endpoints[i])
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
                .Invoking(async () => await _standaloneJobOrchestrator
                    .AddOrUpdateEndpointsAsync(updateRequest)
                    .ConfigureAwait(false))
                .Should()
                .ThrowAsync<MethodCallStatusException>()
                .WithMessage($"Response 404 Endpoint not found: {updateRequest[3].EndpointUrl}: {{}}")
                .ConfigureAwait(false);

            updateRequest.RemoveAt(3);
            await _standaloneJobOrchestrator.AddOrUpdateEndpointsAsync(updateRequest).ConfigureAwait(false);

            // Check endpoint 0.
            await AssertGetConfiguredNodesOnEndpointThrows(_standaloneJobOrchestrator, endpoints[0])
                .ConfigureAwait(false);

            // Check endpoint 1.
            await AssertGetConfiguredNodesOnEndpointThrows(_standaloneJobOrchestrator, endpoints[1])
                .ConfigureAwait(false);

            // Check endpoint 2.
            var endpointNodes2 = await _standaloneJobOrchestrator
                .GetConfiguredNodesOnEndpointAsync(endpoints[2])
                .ConfigureAwait(false);

            AssertSameNodes(updateRequest[2], endpointNodes2);

            // Check endpoint 3.
            await AssertGetConfiguredNodesOnEndpointThrows(_standaloneJobOrchestrator, endpoints[3])
                .ConfigureAwait(false);

            // Check endpoint 4.
            var endpointNodes4 = await _standaloneJobOrchestrator
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
            InitStandaloneJobOrchestrator();

            // Engine/empty_opc_nodes.json contains entries with null or empty OpcNodes.
            // Those entries should not result in any endpoint entries in standaloneJobOrchestrator.
            var configuredEndpoints = await _standaloneJobOrchestrator
                .GetConfiguredEndpointsAsync()
                .ConfigureAwait(false);
            Assert.Equal(0, configuredEndpoints.Count);

            // There should also not be any job entries.
            var jobModel = await _standaloneJobOrchestrator.GetAvailableJobAsync("0", new JobRequestModel());
            Assert.Null(jobModel);
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
            Assert.Equal(endpoint.OpcNodes.Count, nodes.Count);
            for (var k = 0; k < endpoint.OpcNodes.Count; k++) {
                Assert.True(endpoint.OpcNodes[k].IsSame(nodes[k]));
            }
        }
    }
}
