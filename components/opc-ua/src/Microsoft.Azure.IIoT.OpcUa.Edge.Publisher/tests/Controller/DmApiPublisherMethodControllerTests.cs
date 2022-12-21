// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Tests.Engine {
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.OpcUa.Publisher;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Controller;
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
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Storage;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Tests.Utils;

    /// <summary>
    /// Tests the Direct Methods API for the pubisher
    /// </summary>
    public class DmApiPublisherControllerTests : TempFileProviderBase {

        [Theory]
        [InlineData("Controller/DmApiPayloadCollection.json")]
        public async Task DmApiPublishUnpublishNodesTest(string publishedNodesFile) {
            var standaloneCliModelProviderMock = new Mock<IStandaloneCliModelProvider>();
            var agentConfigProviderMock = new Mock<IAgentConfigProvider>();
            var identityMock = new Mock<IIdentity>();
            var newtonSoftJsonSerializer = new NewtonSoftJsonSerializer();
            var jobSerializer = new PublisherJobSerializer(newtonSoftJsonSerializer);
            var logger = TraceLogger.Create();
            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();

            var publishedNodesJobConverter = new PublishedNodesJobConverter(logger, newtonSoftJsonSerializer,
                engineConfigMock.Object, clientConfignMock.Object);

            Utils.CopyContent("Engine/empty_pn.json", _tempFile);
            var standaloneCli = new StandaloneCliModel {
                PublishedNodesFile = _tempFile,
                PublishedNodesSchemaFile = "Storage/publishednodesschema.json"
            };

            standaloneCliModelProviderMock.Setup(p => p.StandaloneCliModel).Returns(standaloneCli);
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

            var methodsController = new PublisherMethodsController(orchestrator);

            using var publishPayloads = new StreamReader(publishedNodesFile);
            var publishNodesRequest = newtonSoftJsonSerializer.Deserialize<List<PublishNodesEndpointApiModel>>(
                await publishPayloads.ReadToEndAsync().ConfigureAwait(false));

            foreach (var request in publishNodesRequest) {
                var initialNode = request.OpcNodes.First();
                for (int i = 0; i < 10000; i++) {
                    request.OpcNodes.Add(new PublishedNodeApiModel {
                        Id = initialNode.Id + i.ToString(),
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

            var tasks = new List<Task<JobProcessingInstructionModel>>();
            for (var i = 0; i < 10; i++) {
                tasks.Add(orchestrator.GetAvailableJobAsync(i.ToString(), new JobRequestModel()));
            }

            tasks.Where(t => t.Result != null)
                .Select(t => t.Result.Job.JobConfiguration)
                .Distinct().Count()
                .Should()
                .Be(2);

            foreach (var request in publishNodesRequest) {
                await FluentActions
                    .Invoking(async () => await methodsController
                    .UnpublishNodesAsync(request).ConfigureAwait(false))
                    .Should()
                    .NotThrowAsync()
                    .ConfigureAwait(false);
            }

            tasks = new List<Task<JobProcessingInstructionModel>>();
            for (var i = 0; i < 10; i++) {
                tasks.Add(orchestrator.GetAvailableJobAsync(i.ToString(), new JobRequestModel()));
            }

            tasks.Where(t => t.Result != null).Count()
                .Should()
                .Be(0);
        }

        [Theory]
        [InlineData("Controller/DmApiPayloadCollection.json")]
        public async Task DmApiPublishUnpublishAllNodesTest(string publishedNodesFile) {
            var standaloneCliModelProviderMock = new Mock<IStandaloneCliModelProvider>();
            var agentConfigProviderMock = new Mock<IAgentConfigProvider>();
            var identityMock = new Mock<IIdentity>();
            var newtonSoftJsonSerializer = new NewtonSoftJsonSerializer();
            var jobSerializer = new PublisherJobSerializer(newtonSoftJsonSerializer);
            var logger = TraceLogger.Create();
            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();

            var publishedNodesJobConverter = new PublishedNodesJobConverter(logger, newtonSoftJsonSerializer,
                engineConfigMock.Object, clientConfignMock.Object);

            Utils.CopyContent("Engine/empty_pn.json", _tempFile);
            var standaloneCli = new StandaloneCliModel {
                PublishedNodesFile = _tempFile,
                PublishedNodesSchemaFile = "Storage/publishednodesschema.json"
            };

            standaloneCliModelProviderMock.Setup(p => p.StandaloneCliModel).Returns(standaloneCli);
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
            var methodsController = new PublisherMethodsController(orchestrator);

            using var publishPayloads = new StreamReader(publishedNodesFile);
            var publishNodesRequest = newtonSoftJsonSerializer.Deserialize<List<PublishNodesEndpointApiModel>>(
                await publishPayloads.ReadToEndAsync().ConfigureAwait(false));

            foreach (var request in publishNodesRequest) {
                var initialNode = request.OpcNodes.First();
                for (int i = 0; i < 10000; i++) {
                    request.OpcNodes.Add(new PublishedNodeApiModel {
                        Id = initialNode.Id + i.ToString(),
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

            var tasks = new List<Task<JobProcessingInstructionModel>>();
            for (var i = 0; i < 10; i++) {
                tasks.Add(orchestrator.GetAvailableJobAsync(i.ToString(), new JobRequestModel()));
            }

            tasks.Where(t => t.Result != null)
                .Select(t => t.Result.Job.JobConfiguration)
                .Distinct().Count()
                .Should()
                .Be(2);

            var unpublishAllNodesRequest = publishNodesRequest.GroupBy(pn => string.Concat(pn.EndpointUrl, pn.DataSetWriterId, pn.DataSetPublishingInterval))
                .Select(g => g.First()).ToList();

            foreach (var request in unpublishAllNodesRequest) {
                request.OpcNodes?.Clear();
                await FluentActions
                    .Invoking(async () => await methodsController
                    .UnpublishAllNodesAsync(request).ConfigureAwait(false))
                    .Should()
                    .NotThrowAsync()
                    .ConfigureAwait(false);
            }

            tasks = new List<Task<JobProcessingInstructionModel>>();
            for (var i = 0; i < 10; i++) {
                tasks.Add(orchestrator.GetAvailableJobAsync(i.ToString(), new JobRequestModel()));
            }

            tasks.Where(t => t.Result != null).Count()
                .Should()
                .Be(0);
        }

        [Theory]
        [InlineData("Controller/DmApiPayloadCollection.json")]
        public async Task DmApiPublishNodesToJobTest(string publishedNodesFile) {
            var standaloneCliModelProviderMock = new Mock<IStandaloneCliModelProvider>();
            var agentConfigProviderMock = new Mock<IAgentConfigProvider>();
            var identityMock = new Mock<IIdentity>();
            var newtonSoftJsonSerializer = new NewtonSoftJsonSerializer();
            var jobSerializer = new PublisherJobSerializer(newtonSoftJsonSerializer);
            var logger = TraceLogger.Create();
            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();

            var publishedNodesJobConverter = new PublishedNodesJobConverter(logger, newtonSoftJsonSerializer,
                engineConfigMock.Object, clientConfignMock.Object);

            Utils.CopyContent("Engine/empty_pn.json", _tempFile);
            var standaloneCli = new StandaloneCliModel {
                PublishedNodesFile = _tempFile,
                PublishedNodesSchemaFile = "Storage/publishednodesschema.json"
            };

            standaloneCliModelProviderMock.Setup(p => p.StandaloneCliModel).Returns(standaloneCli);
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

            var methodsController = new PublisherMethodsController(orchestrator);

            using var publishPayloads = new StreamReader(publishedNodesFile);
            var publishNodesRequests = newtonSoftJsonSerializer.Deserialize<List<PublishNodesEndpointApiModel>>
                (await publishPayloads.ReadToEndAsync().ConfigureAwait(false));

            foreach (var request in publishNodesRequests) {

                await FluentActions
                    .Invoking(async () => await methodsController
                    .PublishNodesAsync(request).ConfigureAwait(false))
                    .Should()
                    .NotThrowAsync()
                    .ConfigureAwait(false);
            }

            var tasks = new List<Task<JobProcessingInstructionModel>>();
            for (var i = 0; i < 10; i++) {
                tasks.Add(orchestrator.GetAvailableJobAsync(i.ToString(), new JobRequestModel()));
            }

            var job = tasks.Where(t => t.Result != null)
                .Select(t => t.Result.Job)
                .Distinct();
            job.Count()
                .Should()
                .Be(2);

            var jobModel = jobSerializer.DeserializeJobConfiguration(
                job.First().JobConfiguration, job.First().JobConfigurationType) as WriterGroupJobModel;

            jobModel.WriterGroup.DataSetWriters.Count.Should().Be(5);
            foreach (var datasetWriter in jobModel.WriterGroup.DataSetWriters) {
                datasetWriter.DataSet.DataSetSource.Connection.Endpoint.Url
                    .Should()
                    .Be(publishNodesRequests.First().EndpointUrl);
                datasetWriter.DataSet.DataSetSource.Connection.Endpoint.SecurityMode
                    .Should()
                    .Be(publishNodesRequests.First().UseSecurity ? SecurityMode.Best : SecurityMode.None);

                datasetWriter.DataSet.DataSetSource.Connection.User.
                    IsSameAs(new CredentialModel {
                        Type = publishNodesRequests.First().OpcAuthenticationMode == AuthenticationMode.Anonymous ?
                                    CredentialType.None :
                                    CredentialType.UserName,
                        Value = newtonSoftJsonSerializer.FromObject(
                                new {
                                    user = publishNodesRequests.First().UserName,
                                    password = publishNodesRequests.First().Password,
                                })
                    })
                    .Should()
                    .BeTrue();
            }
        }

        [Theory]
        [InlineData("Controller/DmApiPayloadTwoEndpoints.json")]
        public async Task DmApiGetConfiguredNodesOnEndpointAsyncTest(string publishedNodesFile) {
            var endpointUrl = "opc.tcp://opcplc:50010";

            var endpointRequest = new PublishNodesEndpointApiModel {
                EndpointUrl = endpointUrl,
            };

            var methodsController = await PublishNodeAsync(publishedNodesFile);
            var response = await FluentActions
                    .Invoking(async () => await methodsController
                    .GetConfiguredNodesOnEndpointAsync(endpointRequest).ConfigureAwait(false))
                    .Should()
                    .NotThrowAsync()
                    .ConfigureAwait(false);

            response.Subject.OpcNodes.Count()
                .Should()
                .Be(2);
            response.Subject.OpcNodes.First().Id
                .Should()
                .Be("ns=2;s=FastUInt1");
            response.Subject.OpcNodes[1].Id
                .Should()
                .Be("ns=2;s=FastUInt2");
        }

        [Theory]
        [InlineData("Controller/DmApiPayloadTwoEndpoints.json")]
        public async Task DmApiGetConfiguredNodesOnEndpointAsyncDataSetWriterGroupTest(string publishedNodesFile) {
            var endpointUrl = "opc.tcp://opcplc:50000";
            var dataSetWriterGroup = "Leaf0";
            var dataSetWriterId = "Leaf0_10000_3085991c-b85c-4311-9bfb-a916da952234";
            var authenticationMode = AuthenticationMode.UsernamePassword;
            var username = "usr";
            var password = "pwd";

            var endpointRequest = new PublishNodesEndpointApiModel {
                EndpointUrl = endpointUrl,
                DataSetWriterGroup = dataSetWriterGroup,
                DataSetWriterId = dataSetWriterId,
                OpcAuthenticationMode = authenticationMode,
                UserName = username,
                Password = password,
            };

            var methodsController = await PublishNodeAsync(publishedNodesFile);

            var response = await FluentActions
                    .Invoking(async () => await methodsController
                    .GetConfiguredNodesOnEndpointAsync(endpointRequest).ConfigureAwait(false))
                    .Should()
                    .NotThrowAsync()
                    .ConfigureAwait(false);

            response.Subject.OpcNodes.Count()
                .Should()
                .Be(1);
            response.Subject.OpcNodes.First().Id
                .Should()
                .Be("ns=2;s=SlowUInt1");
        }

        [Fact]
        public async Task DmApiGetConfiguredNodesOnEndpointAsyncDataSetWriterIdTest() {
            // Testing that we can differentiate between endpoints
            // even if they only have different DataSetWriterIds.

            var opcNodes = Enumerable.Range(0, 5)
                .Select(i => new PublishedNodeApiModel {
                    Id = $"nsu=http://microsoft.com/Opc/OpcPlc/;s=FastUInt{i}",
                })
                .ToList();

            var endpoints = Enumerable.Range(0, 5)
                .Select(i => new PublishNodesEndpointApiModel {
                    EndpointUrl = "opc.tcp://opcplc:50000",
                    DataSetWriterId = i > 1
                        ? $"DataSetWriterId{i}"
                        : (i == 1 ? "" : null),
                    OpcNodes = opcNodes.GetRange(0, i + 1).ToList(),
                })
                .ToList();

            var methodsController = await PublishNodeAsync("Engine/empty_pn.json");

            for (var i = 0; i < 5; ++i) {
                await methodsController.PublishNodesAsync(endpoints[i]).ConfigureAwait(false);
            }

            for (var i = 0; i < 5; ++i) {
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
        }

        [Theory]
        [InlineData("Controller/DmApiPayloadTwoEndpoints.json")]
        public async Task DmApiGetConfiguredNodesOnEndpointAsyncUseSecurityTest(string publishedNodesFile) {
            var endpointUrl = "opc.tcp://opcplc:50000";
            var useSecurity = false;

            var endpointRequest = new PublishNodesEndpointApiModel {
                EndpointUrl = endpointUrl,
                UseSecurity = useSecurity,
            };

            var methodsController = await PublishNodeAsync(publishedNodesFile);

            var response = await FluentActions
                    .Invoking(async () => await methodsController
                    .GetConfiguredNodesOnEndpointAsync(endpointRequest).ConfigureAwait(false))
                    .Should()
                    .NotThrowAsync()
                    .ConfigureAwait(false);

            response.Subject.OpcNodes.Count()
                .Should()
                .Be(1);
            response.Subject.OpcNodes.First().Id
                .Should()
                .Be("ns=2;s=SlowUInt3");
        }

        [Theory]
        [InlineData("Controller/DmApiPayloadTwoEndpoints.json")]
        public async Task DmApiGetConfiguredNodesOnEndpointAsyncOpcAuthenticationModeTest(string publishedNodesFile) {
            var endpointUrl = "opc.tcp://opcplc:50000";
            var dataSetWriterGroup = "Leaf1";
            var dataSetWriterId = "Leaf1_10000_3085991c-b85c-4311-9bfb-a916da952235";
            var dataSetPublishingInterval = 3000;
            var authenticationMode = AuthenticationMode.Anonymous;

            var endpointRequest = new PublishNodesEndpointApiModel {
                EndpointUrl = endpointUrl,
                DataSetWriterGroup = dataSetWriterGroup,
                DataSetWriterId = dataSetWriterId,
                DataSetPublishingInterval = dataSetPublishingInterval,
                OpcAuthenticationMode = authenticationMode,
            };

            var methodsController = await PublishNodeAsync(publishedNodesFile);

            var response = await FluentActions
                    .Invoking(async () => await methodsController
                    .GetConfiguredNodesOnEndpointAsync(endpointRequest).ConfigureAwait(false))
                    .Should()
                    .NotThrowAsync()
                    .ConfigureAwait(false);

            response.Subject.OpcNodes.Count()
                .Should()
                .Be(1);
            response.Subject.OpcNodes.First().Id
                .Should()
                .Be("ns=2;s=SlowUInt2");
        }

        [Theory]
        [InlineData("Controller/DmApiPayloadTwoEndpoints.json")]
        public async Task DmApiGetConfiguredNodesOnEndpointAsyncUsernamePasswordTest(string publishedNodesFile) {
            var endpointUrl = "opc.tcp://opcplc:50000";
            var authenticationMode = AuthenticationMode.UsernamePassword;
            var username = "usr";
            var password = "pwd";

            var endpointRequest = new PublishNodesEndpointApiModel {
                EndpointUrl = endpointUrl,
                OpcAuthenticationMode = authenticationMode,
                UserName = username,
                Password = password,
            };

            var methodsController = await PublishNodeAsync(publishedNodesFile);

            var response = await FluentActions
                    .Invoking(async () => await methodsController
                    .GetConfiguredNodesOnEndpointAsync(endpointRequest).ConfigureAwait(false))
                    .Should()
                    .NotThrowAsync()
                    .ConfigureAwait(false);

            response.Subject.OpcNodes.Count()
                .Should()
                .Be(2);
            response.Subject.OpcNodes.First().Id
                .Should()
                .Be("ns=2;s=FastUInt3");
            response.Subject.OpcNodes[1].Id
                .Should()
                .Be("ns=2;s=FastUInt4");
        }

        /// <summary>
        /// publish nodes from publishedNodesFile
        /// </summary>
        private async Task<PublisherMethodsController> PublishNodeAsync(string publishedNodesFile) {
            var standaloneCliModelProviderMock = new Mock<IStandaloneCliModelProvider>();
            var agentConfigProviderMock = new Mock<IAgentConfigProvider>();
            var identityMock = new Mock<IIdentity>();
            var newtonSoftJsonSerializer = new NewtonSoftJsonSerializer();
            var jobSerializer = new PublisherJobSerializer(newtonSoftJsonSerializer);
            var logger = TraceLogger.Create();
            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();

            var publishedNodesJobConverter = new PublishedNodesJobConverter(logger, newtonSoftJsonSerializer,
                engineConfigMock.Object, clientConfignMock.Object);

            Utils.CopyContent("Engine/empty_pn.json", _tempFile);
            var standaloneCli = new StandaloneCliModel {
                PublishedNodesFile = _tempFile,
                PublishedNodesSchemaFile = "Storage/publishednodesschema.json"
            };

            standaloneCliModelProviderMock.Setup(p => p.StandaloneCliModel).Returns(standaloneCli);
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

            var methodsController = new PublisherMethodsController(orchestrator);

            using var publishPayloads = new StreamReader(publishedNodesFile);
            var publishNodesRequest = newtonSoftJsonSerializer.Deserialize<List<PublishNodesEndpointApiModel>>(
                await publishPayloads.ReadToEndAsync().ConfigureAwait(false));

            foreach (var request in publishNodesRequest) {
                await FluentActions
                    .Invoking(async () => await methodsController.PublishNodesAsync(request).ConfigureAwait(false))
                    .Should()
                    .NotThrowAsync()
                    .ConfigureAwait(false);
            }
            return methodsController;
        }

        [Theory]
        [InlineData("Controller/DmApiPayloadCollection.json")]
        public async Task DmApiGetConfiguredEndpointsTest(string publishedNodesFile) {
            var standaloneCliModelProviderMock = new Mock<IStandaloneCliModelProvider>();
            var agentConfigProviderMock = new Mock<IAgentConfigProvider>();
            var identityMock = new Mock<IIdentity>();
            var newtonSoftJsonSerializer = new NewtonSoftJsonSerializer();
            var jobSerializer = new PublisherJobSerializer(newtonSoftJsonSerializer);
            var logger = TraceLogger.Create();
            var engineConfigMock = new Mock<IEngineConfiguration>();
            var clientConfignMock = new Mock<IClientServicesConfig>();

            var publishedNodesJobConverter = new PublishedNodesJobConverter(logger, newtonSoftJsonSerializer,
                engineConfigMock.Object, clientConfignMock.Object);

            Utils.CopyContent("Engine/empty_pn.json", _tempFile);
            var standaloneCli = new StandaloneCliModel {
                PublishedNodesFile = _tempFile,
                PublishedNodesSchemaFile = "Storage/publishednodesschema.json"
            };

            standaloneCliModelProviderMock.Setup(p => p.StandaloneCliModel).Returns(standaloneCli);
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

            var methodsController = new PublisherMethodsController(orchestrator);

            using var publishPayloads = new StreamReader(publishedNodesFile);
            var publishNodesRequests = newtonSoftJsonSerializer.Deserialize<List<PublishNodesEndpointApiModel>>
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
            foreach (var request in publishNodesRequests) {
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
            endpoints.Subject.Endpoints[0].Tag.Should().Be("Tag_Leaf0_10000_3085991c-b85c-4311-9bfb-a916da952234");
            endpoints.Subject.Endpoints[1].Tag.Should().Be("Tag_Leaf1_10000_2e4fc28f-ffa2-4532-9f22-378d47bbee5d");
            endpoints.Subject.Endpoints[2].Tag.Should().Be("Tag_Leaf2_10000_3085991c-b85c-4311-9bfb-a916da952234");
            endpoints.Subject.Endpoints[3].Tag.Should().Be("Tag_Leaf3_10000_2e4fc28f-ffa2-4532-9f22-378d47bbee5d");
            endpoints.Subject.Endpoints[4].Tag.Should().BeNull();

            var endpointsHash = endpoints.Subject.Endpoints.Select(e => e.GetHashCode()).ToList();
            Assert.True(endpointsHash.Distinct().Count() == endpointsHash.Count());
        }

        [Theory]
        [InlineData("Controller/DmApiPayloadTwoEndpoints.json")]
        public async Task DmApiGetDiagnosticInfoTest(string publishedNodesFile) {

            var newtonSoftJsonSerializer = new NewtonSoftJsonSerializer();

            //publish nodes
            var methodsController = await PublishNodeAsync(publishedNodesFile);

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
    }
}