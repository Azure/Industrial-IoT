// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace OpcPublisher_AE_E2E_Tests.Standalone {
    using OpcPublisher_AE_E2E_Tests.TestExtensions;
    using Azure.Messaging.EventHubs.Consumer;
    using Microsoft.Azure.Devices;
    using Microsoft.Azure.IIoT.Hub.Models;
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models;
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Azure.IIoT.Serializers.NewtonSoft;
    using System;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using TestModels;
    using Xunit;
    using Xunit.Abstractions;
    using Newtonsoft.Json.Linq;
    using FluentAssertions;
    using System.Linq;
    using System.Collections.Generic;

    /// <summary>
    /// Base class for standalone tests using dynamic ACI
    /// </summary>
    [TestCaseOrderer(TestCaseOrderer.FullName, TestConstants.TestAssemblyName)]
    [Trait(TestConstants.TraitConstants.PublisherModeTraitName, TestConstants.TraitConstants.PublisherModeTraitValue)]
    public abstract class DynamicAciTestBase : IDisposable {
        protected readonly ITestOutputHelper _output;
        protected readonly IIoTStandaloneTestContext _context;
        protected readonly CancellationToken _timeoutToken;
        protected readonly EventHubConsumerClient _consumer;
        protected readonly string _writerId;
        private readonly ISerializer _serializer;
        private readonly ServiceClient _iotHubClient;
        private readonly CancellationTokenSource _timeoutTokenSource;
        private readonly string _iotHubPublisherDeviceName;
        private readonly string _iotHubPublisherModuleName;

        protected DynamicAciTestBase(IIoTStandaloneTestContext context, ITestOutputHelper output) {
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _context.OutputHelper = _output;
            _timeoutTokenSource = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);
            _timeoutToken = _timeoutTokenSource.Token;
            _iotHubPublisherDeviceName = _context.DeviceConfig.DeviceId;
            _iotHubPublisherModuleName = _context.IoTHubPublisherDeployment.ModuleName;
            _consumer = _context.GetEventHubConsumerClient();
            _writerId = Guid.NewGuid().ToString();
            _serializer = new NewtonSoftJsonSerializer();

            // Initialize DeviceServiceClient from IoT Hub connection string.
            _iotHubClient = TestHelper.DeviceServiceClient(
                _context.IoTHubConfig.IoTHubConnectionString,
                Microsoft.Azure.Devices.TransportType.Amqp_WebSocket_Only
            );
        }

        public void Dispose() {
            _consumer?.CloseAsync(CancellationToken.None).GetAwaiter().GetResult();
            _timeoutTokenSource?.Dispose();
        }

        [Fact, PriorityOrder(1)]
        public Task Test_SwitchToStandaloneMode() {
            return TestHelper.SwitchToStandaloneModeAsync(_context, _timeoutToken);
        }

        [Fact, PriorityOrder(2)]
        public async Task Test_CreateEdgeBaseDeployment_Expect_Success() {
            var result = await _context.IoTHubEdgeBaseDeployment.CreateOrUpdateLayeredDeploymentAsync(_timeoutToken);
            _output.WriteLine("Created/Updated new EdgeBase deployment");
            Assert.True(result);
        }

        [Fact, PriorityOrder(3)]
        public async Task Test_CreatePublisherLayeredDeployment_Expect_Success() {
            var result = await _context.IoTHubPublisherDeployment.CreateOrUpdateLayeredDeploymentAsync(_timeoutToken);
            Assert.True(result, "Failed to create/update layered deployment for publisher module.");
            _output.WriteLine("Created/Updated layered deployment for publisher module");
        }

        [Fact, PriorityOrder(4)]
        public async Task Test_WaitForModuleDeployed() {
            // We will wait for module to be deployed.
            await _context.RegistryHelper.WaitForSuccessfulDeploymentAsync(
                _context.IoTHubPublisherDeployment.GetDeploymentConfiguration(), _timeoutToken);
            _output.WriteLine("Publisher module deployed.");
        }

        [Fact, PriorityOrder(5)]
        public async Task Test_WaitForModuleConnected() {
            // We will wait for module to be deployed.
            await _context.RegistryHelper.WaitForIIoTModulesConnectedAsync(_context.DeviceConfig.DeviceId,
                _timeoutToken, new[] { "publisher_standalone" });
            _output.WriteLine("Publisher module connected.");
        }

        [Fact, PriorityOrder(998)]
        public async Task Test_StopPublishingAllNodes_Expect_Success() {
            await TestHelper.SwitchToStandaloneModeAndPublishNodesAsync("[]", _context, _timeoutToken);
        }

        [Fact, PriorityOrder(999)]
        public void Test_DeleteAci() {
            TestHelper.DeleteSimulationContainer(_context);
        }

        /// <summary>
        /// Perform direct method call.
        /// </summary>
        /// <param name="parameters"> Direct method parameters. </param>
        /// <param name="ct"> Cancellation token. </param>
        /// <returns></returns>
        protected async Task<MethodResultModel> CallMethodAsync(
            MethodParameterModel parameters,
            CancellationToken ct
        ) {
            return await TestHelper.CallMethodAsync(
                _iotHubClient,
                _iotHubPublisherDeviceName,
                _iotHubPublisherModuleName,
                parameters,
                _context,
                ct
            ).ConfigureAwait(false);
        }

        protected async Task PublishNodesAsync(string json, CancellationToken ct) {
            await UnpublishAllNodesAsync(ct);
            var entries = _serializer.Deserialize<PublishNodesEndpointApiModel[]>(json);
            foreach (var entry in entries) {

                // Call PublishNodes direct method
                var result = await CallMethodAsync(
                    new MethodParameterModel {
                        Name = TestConstants.DirectMethodNames.PublishNodes,
                        JsonPayload = _serializer.SerializeToString(entry)
                    },
                    ct
                ).ConfigureAwait(false);

                Assert.Equal((int)HttpStatusCode.OK, result.Status);
            }

            var result1 = await CallMethodAsync(
                new MethodParameterModel {
                    Name = TestConstants.DirectMethodNames.GetConfiguredEndpoints
                },
                ct
            ).ConfigureAwait(false);

            Assert.Equal((int)HttpStatusCode.OK, result1.Status);
            var response = _serializer.Deserialize<GetConfiguredEndpointsResponseApiModel>(result1.JsonPayload);
            Assert.Equal(entries.Length, response.Endpoints.Count);
        }

        protected async Task UnpublishAllNodesAsync(CancellationToken ct = default) {
            var result = await CallMethodAsync(
                new MethodParameterModel {
                    Name = TestConstants.DirectMethodNames.UnpublishAllNodes,

                    // TODO: Remove this line to test fix for null request crash
                    JsonPayload = _serializer.SerializeToString(new PublishNodesEndpointApiModel())
                },
                ct
            ).ConfigureAwait(false);
            Assert.Equal((int)HttpStatusCode.OK, result.Status);

            var result1 = await CallMethodAsync(
                new MethodParameterModel {
                    Name = TestConstants.DirectMethodNames.GetConfiguredEndpoints
                },
                ct
            ).ConfigureAwait(false);

            Assert.Equal((int)HttpStatusCode.OK, result1.Status);
            var response = _serializer.Deserialize<GetConfiguredEndpointsResponseApiModel>(result1.JsonPayload);
            Assert.Equal(0, response.Endpoints.Count);
        }

        protected string SimpleEvents(string messageTypeDefinitionId, string messageBrowsePath,
            string cycleIdDefinitionId, string cycleIdBrowsePath, string filterTypeDefinitionId) {
            return _context.PublishedNodesJson(
                50000,
                _writerId,
                new JArray(
                    new JObject(
                        new JProperty("Id", "ns=0;i=2253"),
                        new JProperty("QueueSize", 10),
                        new JProperty("DisplayName", "SimpleEvents"),
                        new JProperty("EventFilter", new JObject(
                            new JProperty("SelectClauses", new JArray(
                                new JObject(
                                    new JProperty("TypeDefinitionId", messageTypeDefinitionId),
                                    new JProperty("BrowsePath", new JArray(
                                        new JValue(messageBrowsePath)))),
                                new JObject(
                                    new JProperty("TypeDefinitionId", cycleIdDefinitionId),
                                    new JProperty("BrowsePath", new JArray(
                                        new JValue(cycleIdBrowsePath)))))),
                            new JProperty("WhereClause", new JObject(
                                new JProperty("Elements", new JArray(
                                    new JObject(
                                        new JProperty("FilterOperator", new JValue("OfType")),
                                        new JProperty("FilterOperands", new JArray(
                                            new JObject(
                                                new JProperty("Value", filterTypeDefinitionId))))))))))))));
        }

        protected static void VerifyPayloads(IEnumerable<SystemCycleStatusEventTypePayload> payloads) {
            foreach (var payload in payloads) {
                payload.Message.Value.Should().Match("The system cycle '*' has started.");
                payload.CycleId.Value.Should().MatchRegex("^\\d+$");
            }
        }

        protected static void ValidatePendingConditionsView(IEnumerable<ConditionTypePayload> eventData) {
            foreach (var pendingMessage in eventData) {
                pendingMessage.ConditionId.Value.Should().StartWith("http://microsoft.com/Opc/OpcPlc/AlarmsInstance#");
                pendingMessage.Retain.Value.Should().BeTrue();
            }
        }
    }
}
