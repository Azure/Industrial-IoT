// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatformE2ETests.Standalone
{
    using IIoTPlatformE2ETests.Deploy;
    using System.Net;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using TestExtensions;
    using Xunit;
    using Xunit.Abstractions;
    using Furly.Extensions.Serializers;
    using Azure.IIoT.OpcUa.Publisher.Models;

    /// <summary>
    /// The test theory using different (ordered) test cases to go thru all required steps of publishing OPC UA node
    /// </summary>
    [TestCaseOrderer(TestCaseOrderer.FullName, TestConstants.TestAssemblyName)]
    [Collection("IIoT Standalone Direct Methods Test Collection")]
    [Trait(TestConstants.TraitConstants.PublisherModeTraitName, TestConstants.TraitConstants.PublisherModeTraitValue)]
    public class APublishSingleNodeStandaloneDirectMethodTestTheory : DirectMethodTestBase
    {
        public APublishSingleNodeStandaloneDirectMethodTestTheory(
            ITestOutputHelper output,
            IIoTMultipleNodesTestContext context
        ) : base(output, context) { }

        [Theory]
        [InlineData(MessagingMode.Samples, false)]
        [InlineData(MessagingMode.PubSub, false)]
        [InlineData(MessagingMode.Samples, true)]
        [InlineData(MessagingMode.PubSub, true)]
        public async Task SubscribeUnsubscribeDirectMethodTest(MessagingMode messagingMode, bool incremental)
        {
            var ioTHubEdgeBaseDeployment = new IoTHubEdgeBaseDeployment(_context);
            var ioTHubPublisherDeployment = new IoTHubPublisherDeployment(_context, messagingMode);

            _iotHubPublisherModuleName = ioTHubPublisherDeployment.ModuleName;

            using var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);

            // Make sure that there is no active monitoring.
            await TestHelper.StopMonitoringIncomingMessagesAsync(_context, cts.Token);

            // Clean publishednodes.json.
            await TestHelper.CleanPublishedNodesJsonFilesAsync(_context);

            // Create base edge deployment.
            var baseDeploymentResult = await ioTHubEdgeBaseDeployment.CreateOrUpdateLayeredDeploymentAsync(cts.Token);
            Assert.True(baseDeploymentResult, "Failed to create/update new edge base deployment.");
            _output.WriteLine("Created/Updated new edge base deployment.");

            // Create layered edge deployment.
            var layeredDeploymentResult = await ioTHubPublisherDeployment.CreateOrUpdateLayeredDeploymentAsync(cts.Token);
            Assert.True(layeredDeploymentResult, "Failed to create/update layered deployment for publisher module.");
            _output.WriteLine("Created/Updated layered deployment for publisher module.");

            await TestHelper.SwitchToStandaloneModeAsync(_context, cts.Token);

            // We will wait for module to be deployed.
            await _context.RegistryHelper.WaitForSuccessfulDeploymentAsync(
                ioTHubPublisherDeployment.GetDeploymentConfiguration(),
                cts.Token
            );

            await _context.RegistryHelper.WaitForIIoTModulesConnectedAsync(
                _context.DeviceConfig.DeviceId,
                cts.Token,
                new string[] { ioTHubPublisherDeployment.ModuleName }
            );

            // We've observed situations when even after the above waits the module did not yet restart.
            // That leads to situations where the publishing of nodes happens just before the restart to apply
            // new container creation options. After restart persisted nodes are picked up, but on the telemetry side
            // the restart causes dropped messages to be detected. That happens because just before the restart OPC Publisher
            // manages to send some telemetry. This wait makes sure that we do not run the test while restart is happening.
            await Task.Delay(TestConstants.AwaitInitInMilliseconds, cts.Token);

            // Call GetConfiguredEndpoints direct method, initially there should be no endpoints
            var responseGetConfiguredEndpoints = await CallMethodAsync(
                new MethodParameterModel
                {
                    Name = TestConstants.DirectMethodNames.GetConfiguredEndpoints
                },
                cts.Token
            );

            Assert.Equal((int)HttpStatusCode.OK, responseGetConfiguredEndpoints.Status);
            var configuredEndpointsResponse = _serializer.Deserialize<GetConfiguredEndpointsResponseModel>(responseGetConfiguredEndpoints.JsonPayload);
            Assert.Empty(configuredEndpointsResponse.Endpoints);

            // Use test event processor to verify data send to IoT Hub (expected* set to zero
            // as data gap analysis is not part of this test case).
            await TestHelper.StartMonitoringIncomingMessagesAsync(_context, 0, 0, 0, cts.Token);

            var model = await TestHelper.CreateSingleNodeModelAsync(_context, cts.Token);
            var expectedModel = model with { OpcNodes = new List<OpcNodeModel>() };

            var initialOpcPublishingInterval = model.OpcNodes[0].OpcPublishingInterval;

            for (var i = 0; i < 4; i++)
            {
                model.OpcNodes[0].Id = $"nsu=http://microsoft.com/Opc/OpcPlc/;s=SlowUInt{i + 1}";
                if (incremental)
                {
                    model.OpcNodes[0].OpcPublishingInterval = (int)(initialOpcPublishingInterval + (i * 1000));
                    model.OpcNodes[0].OpcSamplingInterval = model.OpcNodes[0].OpcPublishingInterval / 2;
                }

                var request = model;

                expectedModel.OpcNodes.Add(new OpcNodeModel
                {
                    Id = request.OpcNodes[0].Id,
                    DataSetFieldId = request.OpcNodes[0].DataSetFieldId,
                    DisplayName = request.OpcNodes[0].DisplayName,
                    ExpandedNodeId = model.OpcNodes[0].ExpandedNodeId,
                    HeartbeatInterval = request.OpcNodes[0].HeartbeatInterval,
                    OpcPublishingInterval = request.OpcNodes[0].OpcSamplingInterval,
                    OpcSamplingInterval = request.OpcNodes[0].OpcSamplingInterval,
                    QueueSize = request.OpcNodes[0].QueueSize,
                    SkipFirst = request.OpcNodes[0].SkipFirst
                });

                // Call Publish direct method
                var response = await CallMethodAsync(
                    new MethodParameterModel
                    {
                        Name = TestConstants.DirectMethodNames.PublishNodes,
                        JsonPayload = _serializer.SerializeToString(request)
                    },
                    cts.Token
                );

                Assert.Equal((int)HttpStatusCode.OK, response.Status);

                // Wait some time to generate events to process.
                await Task.Delay(TestConstants.AwaitInitInMilliseconds, cts.Token);

                // Create request for GetConfiguredNodesOnEndpoint method call
                var requestGetConfiguredNodesOnEndpoint = new PublishedNodesEntryModel
                {
                    EndpointUrl = request.EndpointUrl,
                    UseSecurity = request.UseSecurity
                };

                // Call GetConfiguredEndpoints direct method
                responseGetConfiguredEndpoints = await CallMethodAsync(
                    new MethodParameterModel
                    {
                        Name = TestConstants.DirectMethodNames.GetConfiguredEndpoints
                    },
                    cts.Token
                );

                Assert.Equal((int)HttpStatusCode.OK, responseGetConfiguredEndpoints.Status);
                configuredEndpointsResponse = _serializer.Deserialize<GetConfiguredEndpointsResponseModel>(responseGetConfiguredEndpoints.JsonPayload);
                Assert.Single(configuredEndpointsResponse.Endpoints);
                TestHelper.Publisher.AssertEndpointModel(configuredEndpointsResponse.Endpoints[0], request);

                // Call GetConfiguredNodesOnEndpoint direct method
                var responseGetConfiguredNodesOnEndpoint = await CallMethodAsync(
                    new MethodParameterModel
                    {
                        Name = TestConstants.DirectMethodNames.GetConfiguredNodesOnEndpoint,
                        JsonPayload = _serializer.SerializeToString(requestGetConfiguredNodesOnEndpoint)
                    },
                    cts.Token
                );

                Assert.Equal((int)HttpStatusCode.OK, responseGetConfiguredNodesOnEndpoint.Status);
                var jsonResponse = _serializer.Deserialize<GetConfiguredNodesOnEndpointResponseModel>(responseGetConfiguredNodesOnEndpoint.JsonPayload);
                Assert.Equal(jsonResponse.OpcNodes.Count, i + 1);
                Assert.Equal(jsonResponse.OpcNodes[i].Id, $"nsu=http://microsoft.com/Opc/OpcPlc/;s=SlowUInt{i + 1}");
            }

            // Call GetDiagnosticInfo direct method
            var responseGetDiagnosticInfo = await CallMethodAsync(
                new MethodParameterModel
                {
                    Name = TestConstants.DirectMethodNames.GetDiagnosticInfo
                },
                cts.Token
            );

            Assert.Equal((int)HttpStatusCode.OK, responseGetDiagnosticInfo.Status);
            var diagInfo = _serializer.Deserialize<List<PublishDiagnosticInfoModel>>(responseGetDiagnosticInfo.JsonPayload);
            Assert.Single(diagInfo);

            TestHelper.Publisher.AssertEndpointDiagnosticInfoModel(expectedModel, diagInfo[0]);

            // Stop monitoring and get the result.
            var publishingMonitoringResultJson = await TestHelper.StopMonitoringIncomingMessagesAsync(_context, cts.Token);
            Assert.True(publishingMonitoringResultJson.TotalValueChangesCount > 0, "No messages received at IoT Hub");
            Assert.True(publishingMonitoringResultJson.DroppedValueCount == 0,
                $"Dropped messages detected: {publishingMonitoringResultJson.DroppedValueCount}");
            Assert.True(publishingMonitoringResultJson.DuplicateValueCount == 0,
                $"Duplicate values detected: {publishingMonitoringResultJson.DuplicateValueCount}");
            Assert.Equal(0U, publishingMonitoringResultJson.DroppedSequenceCount);
            // Uncomment once bug generating duplicate sequence numbers is resolved.
            //Assert.Equal(0U, publishingMonitoringResultJson.DuplicateSequenceCount);
            Assert.Equal(0U, publishingMonitoringResultJson.ResetSequenceCount);

            model.OpcNodes = null;
            // Call Unpublish direct method
            var responseUnpublish = await CallMethodAsync(
                new MethodParameterModel
                {
                    Name = TestConstants.DirectMethodNames.UnpublishAllNodes,
                    JsonPayload = _serializer.SerializeToString(model)
                },
                cts.Token
            );

            Assert.Equal((int)HttpStatusCode.OK, responseUnpublish.Status);

            // Wait till the publishing has stopped.
            await Task.Delay(TestConstants.AwaitCleanupInMilliseconds, cts.Token);

            // Call GetDiagnosticInfo direct method
            var responseGetDiagnosticInfoFinal = await CallMethodAsync(
                new MethodParameterModel
                {
                    Name = TestConstants.DirectMethodNames.GetDiagnosticInfo
                },
                cts.Token
            );

            Assert.Equal((int)HttpStatusCode.OK, responseGetDiagnosticInfoFinal.Status);
            var diagInfoList = _serializer.Deserialize<List<PublishDiagnosticInfoModel>>(responseGetDiagnosticInfoFinal.JsonPayload);
            Assert.Empty(diagInfoList);

            // Use test event processor to verify data send to IoT Hub (expected* set to zero
            // as data gap analysis is not part of this test case).
            await TestHelper.StartMonitoringIncomingMessagesAsync(_context, 0, 0, 0, cts.Token);

            // Wait some time to generate events to process
            await Task.Delay(TestConstants.AwaitCleanupInMilliseconds, cts.Token);

            // Stop monitoring and get the result.
            var unpublishingMonitoringResultJson = await TestHelper.StopMonitoringIncomingMessagesAsync(_context, cts.Token);
            Assert.True(unpublishingMonitoringResultJson.TotalValueChangesCount == 0,
                $"Messages received at IoT Hub: {unpublishingMonitoringResultJson.TotalValueChangesCount}");
        }

        [Fact]
        public async Task RestartAnnouncementTest()
        {
            var ioTHubEdgeBaseDeployment = new IoTHubEdgeBaseDeployment(_context);
            var ioTHubPublisherDeployment = new IoTHubPublisherDeployment(_context, MessagingMode.PubSub);

            using var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);

            // Make sure that there is no active monitoring.
            await TestHelper.StopMonitoringIncomingMessagesAsync(_context, cts.Token);

            // Clean publishednodes.json.
            await TestHelper.CleanPublishedNodesJsonFilesAsync(_context);

            // Create base edge deployment.
            var baseDeploymentResult = await ioTHubEdgeBaseDeployment.CreateOrUpdateLayeredDeploymentAsync(cts.Token);
            Assert.True(baseDeploymentResult, "Failed to create/update new edge base deployment.");
            _output.WriteLine("Created/Updated new edge base deployment.");

            // Create layered edge deployment.
            var layeredDeploymentResult = await ioTHubPublisherDeployment.CreateOrUpdateLayeredDeploymentAsync(cts.Token);
            Assert.True(layeredDeploymentResult, "Failed to create/update layered deployment for publisher module.");
            _output.WriteLine("Created/Updated layered deployment for publisher module.");

            await TestHelper.SwitchToStandaloneModeAsync(_context, cts.Token);

            // We will wait for module to be deployed.
            await _context.RegistryHelper.WaitForSuccessfulDeploymentAsync(
                ioTHubPublisherDeployment.GetDeploymentConfiguration(),
                cts.Token
            );

            await _context.RegistryHelper.WaitForIIoTModulesConnectedAsync(
                _context.DeviceConfig.DeviceId,
                cts.Token,
                new string[] { ioTHubPublisherDeployment.ModuleName }
            );

            // Start monitoring before restarting the module.
            await TestHelper.StartMonitoringIncomingMessagesAsync(_context, 0, 0, 0, cts.Token);

            // Restart OPC Publisher.
            var moduleRestartResponse = await RestartModuleAsync(ioTHubPublisherDeployment.ModuleName, cts.Token)
;
            Assert.Equal((int)HttpStatusCode.OK, moduleRestartResponse.Status);

            // Wait some time.
            await Task.Delay(TestConstants.AwaitInitInMilliseconds, cts.Token);

            // Stop monitoring and check that restart announcement was received.
            var monitoringResult = await TestHelper.StopMonitoringIncomingMessagesAsync(_context, cts.Token);

            Assert.True(monitoringResult.RestartAnnouncementReceived);
        }
    }
}
