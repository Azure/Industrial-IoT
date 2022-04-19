// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatform_E2E_Tests.Standalone {
    using IIoTPlatform_E2E_Tests.Deploy;
    using IIoTPlatform_E2E_Tests.TestModels;
    using Microsoft.Azure.IIoT.Hub.Models;
    using Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models;
    using Microsoft.Azure.IIoT.Serializers;
    using System.Net;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using TestExtensions;
    using Xunit;
    using Xunit.Abstractions;
    using System.Linq;

    /// <summary>
    /// The test theory using different (ordered) test cases to go thru all required steps of publishing OPC UA node
    /// </summary>
    [TestCaseOrderer(TestCaseOrderer.FullName, TestConstants.TestAssemblyName)]
    [Collection("IIoT Standalone Direct Methods Test Collection")]
    [Trait(TestConstants.TraitConstants.PublisherModeTraitName, TestConstants.TraitConstants.PublisherModeStandaloneTraitValue)]
    public class A_PublishSingleNodeStandaloneDirectMethodTestTheory : DirectMethodTestBase {

        public A_PublishSingleNodeStandaloneDirectMethodTestTheory(
            ITestOutputHelper output,
            IIoTMultipleNodesTestContext context
        ) : base(output, context) { }

        [Theory]
        [InlineData(MessagingMode.Samples, false)]
        [InlineData(MessagingMode.PubSub, false)]
        [InlineData(MessagingMode.Samples, true)]
        [InlineData(MessagingMode.PubSub, true)]
        async Task SubscribeUnsubscribeDirectMethodTest(MessagingMode messagingMode, bool incremental) {
            var ioTHubEdgeBaseDeployment = new IoTHubEdgeBaseDeployment(_context);
            var ioTHubPublisherDeployment = new IoTHubPublisherDeployment(_context, messagingMode);

            _iotHubPublisherModuleName = ioTHubPublisherDeployment.ModuleName;

            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);

            // Make sure that there is no active monitoring.
            await TestHelper.StopMonitoringIncomingMessagesAsync(_context, cts.Token).ConfigureAwait(false);

            // Clean publishednodes.json.
            await TestHelper.CleanPublishedNodesJsonFilesAsync(_context).ConfigureAwait(false);

            // Create base edge deployment.
            var baseDeploymentResult = await ioTHubEdgeBaseDeployment.CreateOrUpdateLayeredDeploymentAsync(cts.Token).ConfigureAwait(false);
            Assert.True(baseDeploymentResult, "Failed to create/update new edge base deployment.");
            _output.WriteLine("Created/Updated new edge base deployment.");

            // Create layered edge deployment.
            var layeredDeploymentResult = await ioTHubPublisherDeployment.CreateOrUpdateLayeredDeploymentAsync(cts.Token).ConfigureAwait(false);
            Assert.True(layeredDeploymentResult, "Failed to create/update layered deployment for publisher module.");
            _output.WriteLine("Created/Updated layered deployment for publisher module.");

            await TestHelper.SwitchToStandaloneModeAsync(_context, cts.Token).ConfigureAwait(false);

            // We will wait for module to be deployed.
            var exception = Record.Exception(() => _context.RegistryHelper.WaitForIIoTModulesConnectedAsync(
                _context.DeviceConfig.DeviceId,
                cts.Token,
                new string[] { ioTHubPublisherDeployment.ModuleName }
            ).GetAwaiter().GetResult());
            Assert.Null(exception);

            // Call GetConfiguredEndpoints direct method, initially there should be no endpoints
            var responseGetConfiguredEndpoints = await CallMethodAsync(
                new MethodParameterModel {
                    Name = TestConstants.DirectMethodNames.GetConfiguredEndpoints
                },
                cts.Token
            ).ConfigureAwait(false);

            Assert.Equal((int)HttpStatusCode.OK, responseGetConfiguredEndpoints.Status);
            var configuredEndpointsResponse = _serializer.Deserialize<GetConfiguredEndpointsResponseApiModel>(responseGetConfiguredEndpoints.JsonPayload);
            Assert.Equal(0, configuredEndpointsResponse.Endpoints.Count);

            // Use test event processor to verify data send to IoT Hub (expected* set to zero
            // as data gap analysis is not part of this test case).
            await TestHelper.StartMonitoringIncomingMessagesAsync(_context, 0, 0, 0, cts.Token).ConfigureAwait(false);

            var model = await TestHelper.CreateSingleNodeModelAsync(_context, cts.Token).ConfigureAwait(false);
            var expectedModel = model.ToApiModel();
            expectedModel.OpcNodes = new List<PublishedNodeApiModel>();

            var initialOpcPublishingInterval = model.OpcNodes[0].OpcPublishingInterval;

            for (var i = 0; i < 4; i++) {

                model.OpcNodes[0].Id = $"nsu=http://microsoft.com/Opc/OpcPlc/;s=SlowUInt{i + 1}";
                if (incremental) {
                    model.OpcNodes[0].OpcPublishingInterval = (uint)(initialOpcPublishingInterval + i * 1000);
                    model.OpcNodes[0].OpcSamplingInterval = model.OpcNodes[0].OpcPublishingInterval / 2;
                }

                var request = model.ToApiModel();

                expectedModel.OpcNodes.Add(new PublishedNodeApiModel {
                    Id = request.OpcNodes[0].Id,
                    DataSetFieldId = request.OpcNodes[0].DataSetFieldId,
                    DisplayName = request.OpcNodes[0].DisplayName,
                    ExpandedNodeId = model.OpcNodes[0].ExpandedNodeId,
                    HeartbeatInterval = request.OpcNodes[0].HeartbeatInterval,
                    OpcPublishingInterval = request.OpcNodes[0].OpcSamplingInterval,
                    OpcSamplingInterval = request.OpcNodes[0].OpcSamplingInterval,
                    QueueSize = request.OpcNodes[0].QueueSize,
                    SkipFirst = request.OpcNodes[0].SkipFirst,
                });

                // Call Publish direct method
                var response = await CallMethodAsync(
                    new MethodParameterModel {
                        Name = TestConstants.DirectMethodNames.PublishNodes,
                        JsonPayload = _serializer.SerializeToString(request)
                    },
                    cts.Token
                ).ConfigureAwait(false);

                Assert.Equal((int)HttpStatusCode.OK, response.Status);

                // Wait some time to generate events to process.
                await Task.Delay(TestConstants.AwaitInitInMilliseconds, cts.Token).ConfigureAwait(false);

                // Create request for GetConfiguredNodesOnEndpoint method call
                var nodesOnEndpoint = new PublishedNodesEntryModel {
                    EndpointUrl = request.EndpointUrl,
                };
                var requestGetConfiguredNodesOnEndpoint = nodesOnEndpoint.ToApiModel();

                // Call GetConfiguredEndpoints direct method
                responseGetConfiguredEndpoints = await CallMethodAsync(
                    new MethodParameterModel {
                        Name = TestConstants.DirectMethodNames.GetConfiguredEndpoints
                    },
                    cts.Token
                ).ConfigureAwait(false);

                Assert.Equal((int)HttpStatusCode.OK, responseGetConfiguredEndpoints.Status);
                configuredEndpointsResponse = _serializer.Deserialize<GetConfiguredEndpointsResponseApiModel>(responseGetConfiguredEndpoints.JsonPayload);
                Assert.Equal(1, configuredEndpointsResponse.Endpoints.Count);
                TestHelper.Publisher.AssertEndpointModel(configuredEndpointsResponse.Endpoints[0], request);

                // Call GetConfiguredNodesOnEndpoint direct method
                var responseGetConfiguredNodesOnEndpoint = await CallMethodAsync(
                    new MethodParameterModel {
                        Name = TestConstants.DirectMethodNames.GetConfiguredNodesOnEndpoint,
                        JsonPayload = _serializer.SerializeToString(requestGetConfiguredNodesOnEndpoint)
                    },
                    cts.Token
                ).ConfigureAwait(false);

                Assert.Equal((int)HttpStatusCode.OK, responseGetConfiguredNodesOnEndpoint.Status);
                var jsonResponse = _serializer.Deserialize<GetConfiguredNodesOnEndpointResponseApiModel>(responseGetConfiguredNodesOnEndpoint.JsonPayload);
                Assert.Equal(jsonResponse.OpcNodes.Count, i + 1);
                Assert.Equal(jsonResponse.OpcNodes[i].Id, $"nsu=http://microsoft.com/Opc/OpcPlc/;s=SlowUInt{i + 1}");

            }

            // Call GetDiagnosticInfo direct method
            var responseGetDiagnosticInfo = await CallMethodAsync(
                new MethodParameterModel {
                    Name = TestConstants.DirectMethodNames.GetDiagnosticInfo,
                },
                cts.Token
            ).ConfigureAwait(false);

            Assert.Equal((int)HttpStatusCode.OK, responseGetDiagnosticInfo.Status);
            var diagInfo = _serializer.Deserialize<List<DiagnosticInfoApiModel>>(responseGetDiagnosticInfo.JsonPayload);
            Assert.Equal(diagInfo.Count, 1);

            TestHelper.Publisher.AssertEndpointDiagnosticInfoModel(expectedModel, diagInfo[0]);

            // Stop monitoring and get the result.
            var publishingMonitoringResultJson = await TestHelper.StopMonitoringIncomingMessagesAsync(_context, cts.Token).ConfigureAwait(false);
            Assert.True(publishingMonitoringResultJson.TotalValueChangesCount > 0, "No messages received at IoT Hub");
            Assert.True(publishingMonitoringResultJson.DroppedValueCount == 0,
                $"Dropped messages detected: {publishingMonitoringResultJson.DroppedValueCount}");
            Assert.True(publishingMonitoringResultJson.DuplicateValueCount == 0,
                $"Duplicate values detected: {publishingMonitoringResultJson.DuplicateValueCount}");
            Assert.Equal(0U, publishingMonitoringResultJson.DroppedSequenceCount);
            Assert.Equal(0U, publishingMonitoringResultJson.DuplicateSequenceCount);
            Assert.Equal(0U, publishingMonitoringResultJson.ResetSequenceCount);

            model.OpcNodes = null;
            // Call Unpublish direct method
            var responseUnpublish = await CallMethodAsync(
                new MethodParameterModel {
                    Name = TestConstants.DirectMethodNames.UnpublishAllNodes,
                    JsonPayload = _serializer.SerializeToString(model.ToApiModel())
                },
                cts.Token
            ).ConfigureAwait(false);

            Assert.Equal((int)HttpStatusCode.OK, responseUnpublish.Status);

            // Wait till the publishing has stopped.
            await Task.Delay(TestConstants.AwaitCleanupInMilliseconds, cts.Token).ConfigureAwait(false);

            // Call GetDiagnosticInfo direct method
            var responseGetDiagnosticInfoFinal = await CallMethodAsync(
                new MethodParameterModel {
                    Name = TestConstants.DirectMethodNames.GetDiagnosticInfo,
                },
                cts.Token
            ).ConfigureAwait(false);

            Assert.Equal((int)HttpStatusCode.OK, responseGetDiagnosticInfoFinal.Status);
            var diagInfoList = _serializer.Deserialize<List<DiagnosticInfoApiModel>>(responseGetDiagnosticInfoFinal.JsonPayload);
            Assert.Equal(diagInfoList.Count, 0);

            // Use test event processor to verify data send to IoT Hub (expected* set to zero
            // as data gap analysis is not part of this test case).
            await TestHelper.StartMonitoringIncomingMessagesAsync(_context, 0, 0, 0, cts.Token).ConfigureAwait(false);

            // Wait some time to generate events to process
            await Task.Delay(TestConstants.AwaitCleanupInMilliseconds, cts.Token).ConfigureAwait(false);

            // Stop monitoring and get the result.
            var unpublishingMonitoringResultJson = await TestHelper.StopMonitoringIncomingMessagesAsync(_context, cts.Token).ConfigureAwait(false);
            Assert.True(unpublishingMonitoringResultJson.TotalValueChangesCount == 0,
                $"Messages received at IoT Hub: {unpublishingMonitoringResultJson.TotalValueChangesCount}");
        }

        [Fact]
        async Task SubscribeUnsubscribeDirectMethodLegacyPublisherTest() {
            var ioTHubEdgeBaseDeployment = new IoTHubEdgeBaseDeployment(_context);
            var ioTHubLegacyPublisherDeployment = new IoTHubLegacyPublisherDeployments(_context);

            _iotHubPublisherModuleName = ioTHubLegacyPublisherDeployment.ModuleName;

            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);

            // Make sure that there is no active monitoring.
            await TestHelper.StopMonitoringIncomingMessagesAsync(_context, cts.Token).ConfigureAwait(false);

            // Clean publishednodes.json.
            await TestHelper.CleanPublishedNodesJsonFilesAsync(_context).ConfigureAwait(false);

            // Create base edge deployment.
            var baseDeploymentResult = await ioTHubEdgeBaseDeployment.CreateOrUpdateLayeredDeploymentAsync(cts.Token).ConfigureAwait(false);
            Assert.True(baseDeploymentResult, "Failed to create/update new edge base deployment.");
            _output.WriteLine("Created/Updated new edge base deployment.");

            // Create layered edge deployment.
            var layeredDeploymentResult1 = await ioTHubLegacyPublisherDeployment.CreateOrUpdateLayeredDeploymentAsync(cts.Token).ConfigureAwait(false);
            Assert.True(layeredDeploymentResult1, "Failed to create/update layered deployment for legacy publisher module.");
            _output.WriteLine("Created/Updated layered deployment for legacy publisher module.");

            var model = await TestHelper.CreateSingleNodeModelAsync(_context, cts.Token).ConfigureAwait(false);

            await TestHelper.SwitchToStandaloneModeAsync(_context, cts.Token).ConfigureAwait(false);

            // We will wait for module to be deployed.
            var exception = Record.Exception(() => _context.RegistryHelper.WaitForIIoTModulesConnectedAsync(
                _context.DeviceConfig.DeviceId,
                cts.Token,
                new string[] { ioTHubLegacyPublisherDeployment.ModuleName }
            ).GetAwaiter().GetResult());
            Assert.Null(exception);

            // Call GetConfiguredEndpoints direct method, initially there should be no endpoints
            var responseGetConfiguredEndpoints = await CallMethodAsync(
                new MethodParameterModel {
                    Name = TestConstants.DirectMethodLegacyNames.GetConfiguredEndpoints
                },
                cts.Token
            ).ConfigureAwait(false);

            Assert.Equal((int)HttpStatusCode.OK, responseGetConfiguredEndpoints.Status);
            var configuredEndpointsResponse = _serializer.Deserialize<GetConfiguredEndpointsResponseApiModel>(responseGetConfiguredEndpoints.JsonPayload);
            Assert.Equal(0, configuredEndpointsResponse.Endpoints.Count);

            var request = model.ToApiModel();

            // Call Publish direct method
            var response = await CallMethodAsync(
                new MethodParameterModel {
                    Name = TestConstants.DirectMethodLegacyNames.PublishNodes,
                    JsonPayload = _serializer.SerializeToString(request)
                },
                cts.Token
            ).ConfigureAwait(false);

            Assert.Equal((int)HttpStatusCode.OK, response.Status);

            // Use test event processor to verify data send to IoT Hub (expected* set to zero
            // as data gap analysis is not part of this test case).
            await TestHelper.StartMonitoringIncomingMessagesAsync(_context, 0, 0, 0, cts.Token).ConfigureAwait(false);

            // Wait some time to generate events to process.
            await Task.Delay(TestConstants.AwaitDataInMilliseconds, cts.Token).ConfigureAwait(false);

            // Call GetConfiguredEndpoints direct method
            responseGetConfiguredEndpoints = await CallMethodAsync(
                new MethodParameterModel {
                    Name = TestConstants.DirectMethodLegacyNames.GetConfiguredEndpoints
                },
                cts.Token
            ).ConfigureAwait(false);

            Assert.Equal((int)HttpStatusCode.OK, responseGetConfiguredEndpoints.Status);
            configuredEndpointsResponse = _serializer.Deserialize<GetConfiguredEndpointsResponseApiModel>(responseGetConfiguredEndpoints.JsonPayload);
            Assert.Equal(configuredEndpointsResponse.Endpoints.Count, 1);
            TestHelper.Publisher.AssertEndpointModel(configuredEndpointsResponse.Endpoints[0], request);

            // Create request for GetConfiguredNodesOnEndpoint method call
            var nodesOnEndpoint = new PublishedNodesEntryModel {
                EndpointUrl = request.EndpointUrl,
            };
            var requestGetConfiguredNodesOnEndpoint = nodesOnEndpoint.ToApiModel();

            // Call GetConfiguredNodesOnEndpoint direct method
            var responseGetConfiguredNodesOnEndpoint = await CallMethodAsync(
                new MethodParameterModel {
                    Name = TestConstants.DirectMethodLegacyNames.GetConfiguredNodesOnEndpoint,
                    JsonPayload = _serializer.SerializeToString(requestGetConfiguredNodesOnEndpoint)
                },
                cts.Token
            ).ConfigureAwait(false);

            Assert.Equal((int)HttpStatusCode.OK, responseGetConfiguredNodesOnEndpoint.Status);

            var jsonResponse = _serializer.Deserialize<GetConfiguredNodesOnEndpointResponseApiModel>(
                responseGetConfiguredNodesOnEndpoint.JsonPayload);
            Assert.Equal(jsonResponse.OpcNodes.Count, 1);
            Assert.Equal(jsonResponse.OpcNodes[0].Id, "nsu=http://microsoft.com/Opc/OpcPlc/;s=SlowUInt1");

            // Call GetDiagnosticInfo direct method
            var responseGetDiagnosticInfo = await CallMethodAsync(
                new MethodParameterModel {
                    Name = TestConstants.DirectMethodLegacyNames.GetDiagnosticInfo,
                },
                cts.Token
            ).ConfigureAwait(false);

            Assert.Equal((int)HttpStatusCode.OK, responseGetDiagnosticInfo.Status);
            var diagInfo = _serializer.Deserialize<DiagnosticInfoLegacyModel>(responseGetDiagnosticInfo.JsonPayload);
            Assert.Equal(diagInfo.NumberOfOpcSessionsConfigured, 1);
            Assert.Equal(diagInfo.NumberOfOpcSessionsConnected, 1);
            Assert.Equal(diagInfo.NumberOfOpcMonitoredItemsConfigured, 1);
            Assert.Equal(diagInfo.NumberOfOpcMonitoredItemsMonitored, 1);
            Assert.True(diagInfo.SentMessages > 0);
            Assert.Equal(diagInfo.FailedMessages, 0);
            Assert.Equal(diagInfo.TooLargeCount, 0);

            // Stop monitoring and get the result.
            var publishingMonitoringResultJson = await TestHelper.StopMonitoringIncomingMessagesAsync(_context, cts.Token).ConfigureAwait(false);
            Assert.True(publishingMonitoringResultJson.TotalValueChangesCount > 0, "No messages received at IoT Hub");
            Assert.True(publishingMonitoringResultJson.DroppedValueCount == 0,
                $"Dropped messages detected: {publishingMonitoringResultJson.DroppedValueCount}");
            Assert.True(publishingMonitoringResultJson.DuplicateValueCount == 0,
                $"Duplicate values detected: {publishingMonitoringResultJson.DuplicateValueCount}");
            Assert.Equal(0U, publishingMonitoringResultJson.DroppedSequenceCount);
            Assert.Equal(0U, publishingMonitoringResultJson.DuplicateSequenceCount);
            Assert.Equal(0U, publishingMonitoringResultJson.ResetSequenceCount);

            // Call Unpublish direct method
            response = await CallMethodAsync(
                new MethodParameterModel {
                    Name = TestConstants.DirectMethodLegacyNames.UnpublishNodes,
                    JsonPayload = _serializer.SerializeToString(request)
                },
                cts.Token
            ).ConfigureAwait(false);

            Assert.Equal((int)HttpStatusCode.OK, response.Status);

            // Wait till the publishing has stopped.
            await Task.Delay(TestConstants.AwaitCleanupInMilliseconds, cts.Token).ConfigureAwait(false);

            // Call GetDiagnosticInfo direct method
            responseGetDiagnosticInfo = await CallMethodAsync(
                new MethodParameterModel {
                    Name = TestConstants.DirectMethodLegacyNames.GetDiagnosticInfo,
                },
                cts.Token
            ).ConfigureAwait(false);

            Assert.Equal((int)HttpStatusCode.OK, responseGetDiagnosticInfo.Status);
            diagInfo = _serializer.Deserialize<DiagnosticInfoLegacyModel>(responseGetDiagnosticInfo.JsonPayload);
            Assert.Equal(diagInfo.NumberOfOpcSessionsConfigured, 0);

            // Use test event processor to verify data send to IoT Hub (expected* set to zero
            // as data gap analysis is not part of this test case).
            await TestHelper.StartMonitoringIncomingMessagesAsync(_context, 0, 0, 0, cts.Token).ConfigureAwait(false);

            // Wait some time to generate events to process
            await Task.Delay(TestConstants.AwaitCleanupInMilliseconds, cts.Token).ConfigureAwait(false);

            // Stop monitoring and get the result.
            var unpublishingMonitoringResultJson = await TestHelper.StopMonitoringIncomingMessagesAsync(_context, cts.Token);
            Assert.True(unpublishingMonitoringResultJson.TotalValueChangesCount == 0,
                $"Messages received at IoT Hub: {unpublishingMonitoringResultJson.TotalValueChangesCount}");
        }
    }
}
