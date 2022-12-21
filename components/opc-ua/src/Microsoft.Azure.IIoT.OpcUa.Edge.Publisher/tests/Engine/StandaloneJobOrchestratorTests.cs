// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Tests.Engine {
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Storage;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Tests.Utils;
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.OpcUa.Publisher;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Config.Models;
    using Microsoft.Azure.IIoT.Serializers;
    using Agent.Framework;
    using Agent.Framework.Models;
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
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;
    using static Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Agent.PublisherJobsConfiguration;

    /// <summary>
    /// Tests the StandaloneJobOrchestrator class
    /// </summary>
    public class StandaloneJobOrchestratorTests : TempFileProviderBase {

        private readonly AgentConfigModel _agentConfigModel;
        private readonly Mock<IAgentConfigProvider> _agentConfigProviderMock;
        private readonly NewtonSoftJsonSerializer _newtonSoftJsonSerializer;
        private readonly NewtonSoftJsonSerializerRaw _newtonSoftJsonSerializerRaw;
        private readonly PublisherJobSerializer _publisherJobSerializer;
        private readonly ILogger _logger;
        private readonly PublishedNodesJobConverter _publishedNodesJobConverter;
        private readonly StandaloneCliModel _standaloneCliModel;
        private readonly Mock<IStandaloneCliModelProvider> _standaloneCliModelProviderMock;
        private StandaloneJobOrchestrator _standaloneJobOrchestrator;
        private readonly PublishedNodesProvider _publishedNodesProvider;

        /// <summary>
        /// Constructor that initializes common resources used by tests.
        /// </summary>
        public StandaloneJobOrchestratorTests() {
            _agentConfigModel = new AgentConfigModel();
            _agentConfigProviderMock = new Mock<IAgentConfigProvider>();
            _agentConfigProviderMock.Setup(p => p.Config).Returns(_agentConfigModel);

            _newtonSoftJsonSerializer = new NewtonSoftJsonSerializer();
            _newtonSoftJsonSerializerRaw = new NewtonSoftJsonSerializerRaw();
            _publisherJobSerializer = new PublisherJobSerializer(_newtonSoftJsonSerializer);
            _logger = TraceLogger.Create();

            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();

            _publishedNodesJobConverter = new PublishedNodesJobConverter(_logger, _newtonSoftJsonSerializer,
                engineConfigMock.Object, clientConfignMock.Object);

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
        [InlineData("Engine/pn_2.5_legacy.json")]
        public async Task Leacy25PublishedNodesFile(string publishedNodesFile) {
            Utils.CopyContent(publishedNodesFile, _tempFile);
            InitStandaloneJobOrchestrator();

            var endpoints = await _standaloneJobOrchestrator.GetConfiguredEndpointsAsync().ConfigureAwait(false);
            Assert.Equal(1, endpoints.Count);

            var endpoint = endpoints[0];
            Assert.Equal(endpoint.EndpointUrl, new Uri("opc.tcp://opcplc:50000"));
            Assert.Equal(endpoint.UseSecurity, false);
            Assert.Equal(endpoint.OpcAuthenticationMode, OpcAuthenticationMode.UsernamePassword);
            Assert.Equal(endpoint.OpcAuthenticationUsername, "username");
            Assert.Equal(endpoint.OpcAuthenticationPassword, null);

            endpoint.OpcAuthenticationPassword = "password";

            var nodes = await _standaloneJobOrchestrator.GetConfiguredNodesOnEndpointAsync(endpoint).ConfigureAwait(false);
            Assert.Equal(1, nodes.Count);
            Assert.Equal("nsu=http://microsoft.com/Opc/OpcPlc/;s=FastUInt1", nodes[0].Id);

            endpoint.OpcNodes = new List<OpcNodeModel>();
            endpoint.OpcNodes.Add(new OpcNodeModel {
                Id = "nsu=http://microsoft.com/Opc/OpcPlc/;s=FastUInt2",
            });

            await _standaloneJobOrchestrator.PublishNodesAsync(endpoint).ConfigureAwait(false);

            endpoints = await _standaloneJobOrchestrator.GetConfiguredEndpointsAsync().ConfigureAwait(false);
            Assert.Equal(1, endpoints.Count);

            endpoint.OpcNodes = null;
            nodes = await _standaloneJobOrchestrator.GetConfiguredNodesOnEndpointAsync(endpoint).ConfigureAwait(false);
            Assert.Equal(2, nodes.Count);
            Assert.Equal("nsu=http://microsoft.com/Opc/OpcPlc/;s=FastUInt1", nodes[0].Id);
            Assert.Equal("nsu=http://microsoft.com/Opc/OpcPlc/;s=FastUInt2", nodes[1].Id);

            // Simulate restart.
            _standaloneJobOrchestrator.Dispose();
            _standaloneJobOrchestrator = null;
            InitStandaloneJobOrchestrator();

            // We should get the same endpoint and nodes after restart.
            endpoints = await _standaloneJobOrchestrator.GetConfiguredEndpointsAsync().ConfigureAwait(false);
            Assert.Equal(1, endpoints.Count);

            endpoint = endpoints[0];
            Assert.Equal(endpoint.EndpointUrl, new Uri("opc.tcp://opcplc:50000"));
            Assert.Equal(endpoint.UseSecurity, false);
            Assert.Equal(endpoint.OpcAuthenticationMode, OpcAuthenticationMode.UsernamePassword);
            Assert.Equal(endpoint.OpcAuthenticationUsername, "username");
            Assert.Equal(endpoint.OpcAuthenticationPassword, null);

            endpoint.OpcAuthenticationPassword = "password";

            nodes = await _standaloneJobOrchestrator.GetConfiguredNodesOnEndpointAsync(endpoint).ConfigureAwait(false);
            Assert.Equal(2, nodes.Count);
            Assert.Equal("nsu=http://microsoft.com/Opc/OpcPlc/;s=FastUInt1", nodes[0].Id);
            Assert.Equal("nsu=http://microsoft.com/Opc/OpcPlc/;s=FastUInt2", nodes[1].Id);
        }

        [Theory]
        [InlineData("Engine/pn_2.5_legacy_error.json", false)]
        [InlineData("Engine/pn_2.5_legacy_error.json", true)]
        public async Task Leacy25PublishedNodesFileError(string publishedNodesFile, bool useSchemaValidation) {
            if (!useSchemaValidation) {
                _standaloneCliModel.PublishedNodesSchemaFile = null;
            }

            Utils.CopyContent(publishedNodesFile, _tempFile);
            InitStandaloneJobOrchestrator();

            // Transformation of published nodes entries should throw a serialization error since
            // Engine/pn_2.5_legacy_error.json contains both NodeId and OpcNodes.
            // So as a result, we should end up with zero endpoints.
            var endpoints = await _standaloneJobOrchestrator.GetConfiguredEndpointsAsync().ConfigureAwait(false);
            Assert.Equal(0, endpoints.Count);
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
        public async Task Test_SerializableExceptionResponse() {
            InitStandaloneJobOrchestrator();

            var exceptionResponse = $"{{\"Message\":\"Response 400 null request is provided\",\"Details\":{{}}}}";

            // Check null request.
            await FluentActions
                .Invoking(async () => await _standaloneJobOrchestrator
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

            await _standaloneJobOrchestrator.PublishNodesAsync(endpoints[0]).ConfigureAwait(false);

            exceptionResponse = $"{{\"Message\":\"Response 404 Nodes not found\"," +
                $"\"Details\":{{\"DataSetWriterId\":\"DataSetWriterId0\"," +
                $"\"DataSetWriterGroup\":\"DataSetWriterGroup\"," +
                $"\"DataSetPublishingInterval\":1000," +
                $"\"EndpointUrl\":\"opc.tcp://opcplc:50000\"," +
                $"\"UseSecurity\":false,\"OpcAuthenticationMode\":\"anonymous\"," +
                $"\"OpcNodes\":[{{\"Id\":\"nsu=http://microsoft.com/Opc/OpcPlc/;s=SlowUInt0\"}}]}}}}";

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
                .Invoking(async () => await _standaloneJobOrchestrator
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
                    $"\"DataSetPublishingInterval\":1000,\"EndpointUrl\":\"opc.tcp://opcplc:50000\"," +
                    $"\"UseSecurity\":false,\"OpcAuthenticationMode\":\"anonymous\"," +
                    $"\"OpcNodes\":[{{\"Id\":\"nsu=http://microsoft.com/Opc/OpcPlc/;s=SlowUInt0\"}}]}}",
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
            InitStandaloneJobOrchestrator();

            // Check null request.
            await FluentActions
                .Invoking(async () => await _standaloneJobOrchestrator
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
                .Invoking(async () => await _standaloneJobOrchestrator
                    .PublishNodesAsync(request)
                    .ConfigureAwait(false))
                .Should()
                .ThrowAsync<MethodCallStatusException>()
                .WithMessage($"{{\"Message\":\"Response 400 null or empty OpcNodes is provided in request\",\"Details\":{{}}}}")
                .ConfigureAwait(false);

            request.OpcNodes = new List<OpcNodeModel>();

            // Check empty OpcNodes in request.
            await FluentActions
                .Invoking(async () => await _standaloneJobOrchestrator
                    .PublishNodesAsync(request)
                    .ConfigureAwait(false))
                .Should()
                .ThrowAsync<MethodCallStatusException>()
                .WithMessage($"{{\"Message\":\"Response 400 null or empty OpcNodes is provided in request\",\"Details\":{{}}}}")
                .ConfigureAwait(false);
        }

        [Fact]
        public async Task Test_UnpublishNodes_NullRequest() {
            InitStandaloneJobOrchestrator();

            // Check null request.
            await FluentActions
                .Invoking(async () => await _standaloneJobOrchestrator
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

            InitStandaloneJobOrchestrator();

            var numberOfEndpoints = 3;
            var opcNodes = Enumerable.Range(0, numberOfEndpoints)
                .Select(i => new OpcNodeModel {
                    Id = $"nsu=http://microsoft.com/Opc/OpcPlc/;s=FastUInt{i}",
                })
                .ToList();

            var endpoints = Enumerable.Range(0, numberOfEndpoints)
                .Select(i => GenerateEndpoint(i, opcNodes, customEndpoint))
                .ToList();

            await _standaloneJobOrchestrator.PublishNodesAsync(endpoints[0]).ConfigureAwait(false);
            await _standaloneJobOrchestrator.PublishNodesAsync(endpoints[1]).ConfigureAwait(false);
            await _standaloneJobOrchestrator.PublishNodesAsync(endpoints[2]).ConfigureAwait(false);

            endpoints[1] = GenerateEndpoint(1, opcNodes, customEndpoint);
            endpoints[1].OpcNodes = useEmptyOpcNodes
                ? new List<OpcNodeModel>()
                : null;

            // Check null or empty OpcNodes in request.
            await FluentActions
                .Invoking(async () => await _standaloneJobOrchestrator
                    .UnpublishNodesAsync(endpoints[1])
                    .ConfigureAwait(false))
                .Should()
                .NotThrowAsync()
                .ConfigureAwait(false);

            var configuredEndpoints = await _standaloneJobOrchestrator
                .GetConfiguredEndpointsAsync().ConfigureAwait(false);

            Assert.Equal(2, configuredEndpoints.Count);

            Assert.True(endpoints[0].HasSameDataSet(configuredEndpoints[0]));
            Assert.True(endpoints[2].HasSameDataSet(configuredEndpoints[1]));
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
                .WithMessage($"{{\"Message\":\"Response 400 null request is provided\",\"Details\":{{}}}}")
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
                .WithMessage($"{{\"Message\":\"Response 400 null request is provided\",\"Details\":{{}}}}")
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
                .WithMessage($"{{\"Message\":\"Response 400 Request contains two entries for the same endpoint at index 0 and 2\",\"Details\":{{}}}}")
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
                .WithMessage($"{{\"Message\":\"Response 400 Request contains two entries for the same endpoint at index 0 and 2\",\"Details\":{{}}}}")
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
                        .WithMessage($"{{\"Message\":\"Response 404 Endpoint not found: {request.EndpointUrl}\",\"Details\":{{}}}}")
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
                    .WithMessage($"{{\"Message\":\"Response 404 Endpoint not found: {endpoint.EndpointUrl}\",\"Details\":{{}}}}")
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
                .WithMessage($"{{\"Message\":\"Response 404 Endpoint not found: {updateRequest[3].EndpointUrl}\",\"Details\":{{}}}}")
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

        [Theory]
        [InlineData("Engine/pn_assets_with_optional_fields.json")]
        public async Task OptionalFieldsPublishedNodesFile(string publishedNodesFile) {
            Utils.CopyContent(publishedNodesFile, _tempFile);
            InitStandaloneJobOrchestrator();

            var endpoints = await _standaloneJobOrchestrator.GetConfiguredEndpointsAsync().ConfigureAwait(false);
            Assert.Equal(2, endpoints.Count);

            Assert.Equal(endpoints[0].DataSetWriterGroup, "Leaf0");
            Assert.Equal(endpoints[0].EndpointUrl, new Uri("opc.tcp://opcplc:50000"));
            Assert.Equal(endpoints[0].UseSecurity, false);
            Assert.Equal(endpoints[0].OpcAuthenticationMode, OpcAuthenticationMode.Anonymous);
            Assert.Equal(endpoints[0].DataSetWriterId, "Leaf0_10000_3085991c-b85c-4311-9bfb-a916da952234");
            Assert.Equal(endpoints[0].Tag, "Tag_Leaf0_10000_3085991c-b85c-4311-9bfb-a916da952234");

            Assert.Equal(endpoints[1].DataSetWriterGroup, "Leaf1");
            Assert.Equal(endpoints[1].EndpointUrl, new Uri("opc.tcp://opcplc:50000"));
            Assert.Equal(endpoints[1].UseSecurity, false);
            Assert.Equal(endpoints[1].OpcAuthenticationMode, OpcAuthenticationMode.UsernamePassword);
            Assert.Equal(endpoints[1].DataSetWriterId, "Leaf1_10000_2e4fc28f-ffa2-4532-9f22-378d47bbee5d");
            Assert.Equal(endpoints[1].Tag, "Tag_Leaf1_10000_2e4fc28f-ffa2-4532-9f22-378d47bbee5d");
            endpoints[0].OpcNodes = null;
            var nodes = await _standaloneJobOrchestrator.GetConfiguredNodesOnEndpointAsync(endpoints[0]).ConfigureAwait(false);
            Assert.Equal(1, nodes.Count);
            Assert.Equal("nsu=http://microsoft.com/Opc/OpcPlc/;s=StepUp", nodes[0].Id);

            endpoints[0].OpcNodes = new List<OpcNodeModel>();
            endpoints[0].OpcNodes.Add(new OpcNodeModel {
                Id = "nsu=http://microsoft.com/Opc/OpcPlc/;s=FastUInt2",
            });

            await _standaloneJobOrchestrator.PublishNodesAsync(endpoints[0]).ConfigureAwait(false);

            endpoints = await _standaloneJobOrchestrator.GetConfiguredEndpointsAsync().ConfigureAwait(false);
            Assert.Equal(2, endpoints.Count);

            endpoints[0].OpcNodes = null;
            nodes = await _standaloneJobOrchestrator.GetConfiguredNodesOnEndpointAsync(endpoints[0]).ConfigureAwait(false);
            Assert.Equal(2, nodes.Count);
            Assert.Equal("nsu=http://microsoft.com/Opc/OpcPlc/;s=StepUp", nodes[0].Id);
            Assert.Equal("nsu=http://microsoft.com/Opc/OpcPlc/;s=FastUInt2", nodes[1].Id);

            // Simulate restart.
            _standaloneJobOrchestrator.Dispose();
            _standaloneJobOrchestrator = null;
            InitStandaloneJobOrchestrator();

            // We should get the same endpoint and nodes after restart.
            endpoints = await _standaloneJobOrchestrator.GetConfiguredEndpointsAsync().ConfigureAwait(false);
            Assert.Equal(2, endpoints.Count);

            Assert.Equal(endpoints[0].DataSetWriterGroup, "Leaf0");
            Assert.Equal(endpoints[0].EndpointUrl, new Uri("opc.tcp://opcplc:50000"));
            Assert.Equal(endpoints[0].UseSecurity, false);
            Assert.Equal(endpoints[0].OpcAuthenticationMode, OpcAuthenticationMode.Anonymous);
            Assert.Equal(endpoints[0].DataSetWriterId, "Leaf0_10000_3085991c-b85c-4311-9bfb-a916da952234");
            Assert.Equal(endpoints[0].Tag, "Tag_Leaf0_10000_3085991c-b85c-4311-9bfb-a916da952234");

            nodes = await _standaloneJobOrchestrator.GetConfiguredNodesOnEndpointAsync(endpoints[0]).ConfigureAwait(false);
            Assert.Equal(2, nodes.Count);
            Assert.Equal("nsu=http://microsoft.com/Opc/OpcPlc/;s=StepUp", nodes[0].Id);
            Assert.Equal("nsu=http://microsoft.com/Opc/OpcPlc/;s=FastUInt2", nodes[1].Id);
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
