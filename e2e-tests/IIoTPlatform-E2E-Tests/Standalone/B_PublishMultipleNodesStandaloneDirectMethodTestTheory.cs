﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatform_E2E_Tests.Standalone {

    using IIoTPlatform_E2E_Tests.Deploy;
    using IIoTPlatform_E2E_Tests.TestModels;
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models;
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using TestExtensions;
    using Xunit;
    using Xunit.Abstractions;
    using Microsoft.Azure.IIoT.Hub.Models;
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Models;
    using System.Net;
    using System.Linq;
    using System.Collections.Generic;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// The test theory using different (ordered) test cases to go thru all required steps of publishing OPC UA node
    /// </summary>
    [TestCaseOrderer(TestCaseOrderer.FullName, TestConstants.TestAssemblyName)]
    [Collection("IIoT Standalone Direct Methods Test Collection")]
    [Trait(TestConstants.TraitConstants.PublisherModeTraitName, TestConstants.TraitConstants.PublisherModeStandaloneTraitValue)]
    public class B_PublishMultipleNodesStandaloneDirectMethodTestTheory : DirectMethodTestBase {

        public B_PublishMultipleNodesStandaloneDirectMethodTestTheory(
            ITestOutputHelper output,
            IIoTMultipleNodesTestContext context
        ) : base(output, context) { }

        [Theory]
        [InlineData(MessagingMode.Samples, false)]
        [InlineData(MessagingMode.Samples, true)]
        [InlineData(MessagingMode.PubSub, false)]
        [InlineData(MessagingMode.PubSub, true)]
        async Task SubscribeUnsubscribeDirectMethodTest(MessagingMode messagingMode, bool useAddOrUpdate) {
            // When useAddOrUpdate is true, all publishing and unpublishing operations
            // will be performed through AddOrUpdateEndpoints direct method.

            var ioTHubEdgeBaseDeployment = new IoTHubEdgeBaseDeployment(_context);
            var ioTHubPublisherDeployment = new IoTHubPublisherDeployment(_context, messagingMode);

            _iotHubPublisherModuleName = ioTHubPublisherDeployment.ModuleName;

            // Clear context.
            _context.Reset();

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

            //Call GetConfiguredEndpoints direct method, initially there should be no endpoints
            var responseGetConfiguredEndpoints = await CallMethodAsync(
                new MethodParameterModel {
                    Name = TestConstants.DirectMethodNames.GetConfiguredEndpoints
                },
                cts.Token
            ).ConfigureAwait(false);

            Assert.Equal((int)HttpStatusCode.OK, responseGetConfiguredEndpoints.Status);
            var configuredEndpointsResponse = _serializer.Deserialize<List<PublishNodesEndpointApiModel>>(responseGetConfiguredEndpoints.JsonPayload);
            Assert.Equal(configuredEndpointsResponse.Count, 0);

            var nodesToPublish = await TestHelper.CreateMultipleNodesModelAsync(_context, cts.Token).ConfigureAwait(false);
            var request = nodesToPublish.ToApiModel();
            MethodResultModel response = null;

            //Publish nodes for the endpoint
            if (useAddOrUpdate) {
                //Call AddOrUpdateEndpoints direct method
                response = await CallMethodAsync(
                    new MethodParameterModel {
                        Name = TestConstants.DirectMethodNames.AddOrUpdateEndpoints,
                        JsonPayload = _serializer.SerializeToString(new List<PublishNodesEndpointApiModel> { request })
                    },
                    cts.Token
                ).ConfigureAwait(false);
            }
            else {
                //Call PublishNodes direct method
                response = await CallMethodAsync(
                    new MethodParameterModel {
                        Name = TestConstants.DirectMethodNames.PublishNodes,
                        JsonPayload = _serializer.SerializeToString(request)
                    },
                    cts.Token
                ).ConfigureAwait(false);
            }

            Assert.Equal((int)HttpStatusCode.OK, response.Status);

            // Use test event processor to verify data send to IoT Hub (expected* set to zero
            // as data gap analysis is not part of this test case)
            await TestHelper.StartMonitoringIncomingMessagesAsync(_context, 250, 10_000, 90_000_000, cts.Token).ConfigureAwait(false);

            // Wait some time to generate events to process.
            await Task.Delay(TestConstants.DefaultTimeoutInMilliseconds, cts.Token).ConfigureAwait(false);

            //Call GetConfiguredEndpoints direct method
            responseGetConfiguredEndpoints = await CallMethodAsync(
                new MethodParameterModel {
                    Name = TestConstants.DirectMethodNames.GetConfiguredEndpoints
                },
                cts.Token
            ).ConfigureAwait(false);

            Assert.Equal((int)HttpStatusCode.OK, responseGetConfiguredEndpoints.Status);
            configuredEndpointsResponse = _serializer.Deserialize<List<PublishNodesEndpointApiModel>>(responseGetConfiguredEndpoints.JsonPayload);
            Assert.Equal(1, configuredEndpointsResponse.Count);
            TestHelper.Publisher.AssertEndpointModel(configuredEndpointsResponse[0], request);

            //Create request for GetConfiguredNodesOnEndpoint method call
            var nodesOnEndpoint = new PublishedNodesEntryModel();
            nodesOnEndpoint.EndpointUrl = request.EndpointUrl;
            var requestGetConfiguredNodesOnEndpoint = nodesOnEndpoint.ToApiModel();

            //Call GetConfiguredNodesOnEndpoint direct method
            var responseGetConfiguredNodesOnEndpoint = await CallMethodAsync(
                new MethodParameterModel {
                    Name = TestConstants.DirectMethodNames.GetConfiguredNodesOnEndpoint,
                    JsonPayload = _serializer.SerializeToString(requestGetConfiguredNodesOnEndpoint)
                },
                cts.Token
            ).ConfigureAwait(false);

            Assert.Equal((int)HttpStatusCode.OK, responseGetConfiguredNodesOnEndpoint.Status);
            var jsonResponse = _serializer.Deserialize<List<PublishedNodeApiModel>>(responseGetConfiguredNodesOnEndpoint.JsonPayload);
            Assert.Equal(jsonResponse.Count, 250);

            //Call GetDiagnosticInfo direct method
            var responseGetDiagnosticInfo = await CallMethodAsync(
                new MethodParameterModel {
                    Name = TestConstants.DirectMethodNames.GetDiagnosticInfo,
                },
                cts.Token
            ).ConfigureAwait(false);

            Assert.Equal((int)HttpStatusCode.OK, responseGetDiagnosticInfo.Status);
            var diagInfoList = _serializer.Deserialize<List<DiagnosticInfoApiModel>>(responseGetDiagnosticInfo.JsonPayload);
            Assert.Equal(diagInfoList.Count, 1);

            TestHelper.Publisher.AssertEndpointDiagnosticInfoModel(request, diagInfoList[0]);

            // Stop monitoring and get the result.
            var publishingMonitoringResultJson = await TestHelper.StopMonitoringIncomingMessagesAsync(_context, cts.Token).ConfigureAwait(false);
            Assert.True(publishingMonitoringResultJson.TotalValueChangesCount > 0, "No messages received at IoT Hub");
            Assert.Equal(publishingMonitoringResultJson.ValueChangesByNodeId.Count, request.OpcNodes.Count);
            Assert.True(publishingMonitoringResultJson.DroppedValueCount == 0,
                $"Dropped messages detected: {publishingMonitoringResultJson.DroppedValueCount}");
            Assert.True(publishingMonitoringResultJson.DuplicateValueCount == 0,
                $"Duplicate values detected: {publishingMonitoringResultJson.DuplicateValueCount}");

            // Check that every published node is sending data.
            if (_context.ConsumedOpcUaNodes != null) {
                var expectedNodes = _context.ConsumedOpcUaNodes.First().Value.OpcNodes.Select(n => n.Id).ToList();
                foreach (var property in publishingMonitoringResultJson.ValueChangesByNodeId) {
                    var propertyName = property.Key;
                    var nodeId = propertyName.Split('#').Last();
                    var expected = expectedNodes.FirstOrDefault(n => n.EndsWith(nodeId));
                    Assert.True(expected != null, $"Publishing from unexpected node: {propertyName}");
                    expectedNodes.Remove(expected);
                }

                expectedNodes.ForEach(n => _context.OutputHelper.WriteLine(n));
                Assert.Empty(expectedNodes);
            }

            //Unpublish all nodes for the endpoint
            if (useAddOrUpdate) {
                //Call AddOrUpdateEndpoints direct method
                request.OpcNodes = null;
                response = await CallMethodAsync(
                    new MethodParameterModel {
                        Name = TestConstants.DirectMethodNames.AddOrUpdateEndpoints,
                        JsonPayload = _serializer.SerializeToString(new List<PublishNodesEndpointApiModel> { request })
                    },
                    cts.Token
                ).ConfigureAwait(false);
            }
            else {
                //Call UnPublishNodes direct method
                response = await CallMethodAsync(
                    new MethodParameterModel {
                        Name = TestConstants.DirectMethodNames.UnpublishNodes,
                        JsonPayload = _serializer.SerializeToString(request)
                    },
                    cts.Token
                ).ConfigureAwait(false);
            }

            Assert.Equal((int)HttpStatusCode.OK, response.Status);

            // Wait till the publishing has stopped.
            await Task.Delay(TestConstants.DefaultTimeoutInMilliseconds, cts.Token).ConfigureAwait(false);

            //Call GetDiagnosticInfo direct method
            responseGetDiagnosticInfo = await CallMethodAsync(
                new MethodParameterModel {
                    Name = TestConstants.DirectMethodNames.GetDiagnosticInfo,
                },
                cts.Token
            ).ConfigureAwait(false);

            Assert.Equal((int)HttpStatusCode.OK, responseGetDiagnosticInfo.Status);
            diagInfoList = _serializer.Deserialize<List<DiagnosticInfoApiModel>>(responseGetDiagnosticInfo.JsonPayload);
            Assert.Equal(diagInfoList.Count, 0);

            // Use test event processor to verify data send to IoT Hub (expected* set to zero
            // as data gap analysis is not part of this test case)
            await TestHelper.StartMonitoringIncomingMessagesAsync(_context, 0, 0, 0, cts.Token).ConfigureAwait(false);

            // Wait some time to generate events to process.
            await Task.Delay(TestConstants.DefaultTimeoutInMilliseconds, cts.Token).ConfigureAwait(false);

            // Stop monitoring and get the result.
            var unpublishingMonitoringResultJson = await TestHelper.StopMonitoringIncomingMessagesAsync(_context, cts.Token);
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

            await TestHelper.SwitchToStandaloneModeAsync(_context, cts.Token).ConfigureAwait(false);

            var nodesToPublish = await TestHelper.CreateMultipleNodesModelAsync(_context, cts.Token).ConfigureAwait(false);

            // We will wait for module to be deployed.
            var exception = Record.Exception(() => _context.RegistryHelper.WaitForIIoTModulesConnectedAsync(
                _context.DeviceConfig.DeviceId,
                cts.Token,
                new string[] { ioTHubLegacyPublisherDeployment.ModuleName }
            ).GetAwaiter().GetResult());
            Assert.Null(exception);

            //Call GetConfiguredEndpoints direct method, initially there should be no endpoints
            var responseGetConfiguredEndpoints = await CallMethodAsync(
                new MethodParameterModel {
                    Name = TestConstants.DirectMethodLegacyNames.GetConfiguredEndpoints
                },
                cts.Token
            ).ConfigureAwait(false);

            Assert.Equal((int)HttpStatusCode.OK, responseGetConfiguredEndpoints.Status);
            var epObj = JObject.Parse(responseGetConfiguredEndpoints.JsonPayload);
            var endpoints = _serializer.SerializeToString(epObj["Endpoints"]);
            var configuredEndpointsResponse = _serializer.Deserialize<List<PublishNodesEndpointApiModel>>(endpoints);
            Assert.Equal(configuredEndpointsResponse.Count, 0);

            var request = nodesToPublish.ToApiModel();

            //Call Publish direct method
            var response = await CallMethodAsync(
                new MethodParameterModel {
                    Name = TestConstants.DirectMethodLegacyNames.PublishNodes,
                    JsonPayload = _serializer.SerializeToString(request)
                },
                cts.Token
            ).ConfigureAwait(false);

            Assert.Equal((int)HttpStatusCode.OK, response.Status);

            // Use test event processor to verify data send to IoT Hub (expected* set to zero
            // as data gap analysis is not part of this test case)
            await TestHelper.StartMonitoringIncomingMessagesAsync(_context, 250, 10_000, 90_000_000, cts.Token).ConfigureAwait(false);

            // Wait some time to generate events to process.
            await Task.Delay(TestConstants.DefaultTimeoutInMilliseconds, cts.Token).ConfigureAwait(false);

            //Call GetConfiguredEndpoints direct method
            responseGetConfiguredEndpoints = await CallMethodAsync(
                new MethodParameterModel {
                    Name = TestConstants.DirectMethodLegacyNames.GetConfiguredEndpoints
                },
                cts.Token
            ).ConfigureAwait(false);

            Assert.Equal((int)HttpStatusCode.OK, responseGetConfiguredEndpoints.Status);
            epObj = JObject.Parse(responseGetConfiguredEndpoints.JsonPayload);
            endpoints = _serializer.SerializeToString(epObj["Endpoints"]);
            configuredEndpointsResponse = _serializer.Deserialize<List<PublishNodesEndpointApiModel>>(endpoints);
            Assert.Equal(1, configuredEndpointsResponse.Count);
            TestHelper.Publisher.AssertEndpointModel(configuredEndpointsResponse[0], request);

            //Create request for GetConfiguredNodesOnEndpoint method call
            var nodesOnEndpoint = new PublishedNodesEntryModel();
            nodesOnEndpoint.EndpointUrl = request.EndpointUrl;
            var requestGetConfiguredNodesOnEndpoint = nodesOnEndpoint.ToApiModel();

            //Call GetConfiguredNodesOnEndpoint direct method
            var responseGetConfiguredNodesOnEndpoint = await CallMethodAsync(
                new MethodParameterModel {
                    Name = TestConstants.DirectMethodLegacyNames.GetConfiguredNodesOnEndpoint,
                    JsonPayload = _serializer.SerializeToString(requestGetConfiguredNodesOnEndpoint)
                },
                cts.Token
            ).ConfigureAwait(false);

            Assert.Equal((int)HttpStatusCode.OK, responseGetConfiguredNodesOnEndpoint.Status);

            var obj = JObject.Parse(responseGetConfiguredNodesOnEndpoint.JsonPayload);
            var opcNodes = _serializer.SerializeToString(obj["OpcNodes"]);
            var jsonResponse = _serializer.Deserialize<List<PublishedNodeApiModel>>(opcNodes);
            Assert.Equal(jsonResponse.Count, 250);

            //Call GetDiagnosticInfo direct method
            var responseGetDiagnosticInfo = await CallMethodAsync(
                new MethodParameterModel {
                    Name = TestConstants.DirectMethodLegacyNames.GetDiagnosticInfo,
                },
                cts.Token
            ).ConfigureAwait(false);

            Assert.Equal((int)HttpStatusCode.OK, responseGetDiagnosticInfo.Status);
            var diagInfo = _serializer.Deserialize<DiagnosticInfoLegacyModel>(responseGetDiagnosticInfo.JsonPayload);
            Assert.Equal(diagInfo.NumberOfOpcSessionsConfigured, 1);
            Assert.Equal(diagInfo.NumberOfOpcSessionsConfigured, 1);
            Assert.Equal(diagInfo.NumberOfOpcSessionsConnected, 1);
            Assert.Equal(diagInfo.NumberOfOpcMonitoredItemsConfigured, 250);
            Assert.Equal(diagInfo.NumberOfOpcMonitoredItemsMonitored, 250);
            Assert.True(diagInfo.SentMessages > 0);
            Assert.Equal(diagInfo.FailedMessages, 0);
            Assert.Equal(diagInfo.TooLargeCount, 0);

            // Stop monitoring and get the result.
            var publishingMonitoringResultJson = await TestHelper.StopMonitoringIncomingMessagesAsync(_context, cts.Token).ConfigureAwait(false);
            Assert.True(publishingMonitoringResultJson.TotalValueChangesCount > 0, "No messages received at IoT Hub");
            Assert.Equal(publishingMonitoringResultJson.ValueChangesByNodeId.Count, request.OpcNodes.Count);
            Assert.True(publishingMonitoringResultJson.DroppedValueCount == 0,
                $"Dropped messages detected: {publishingMonitoringResultJson.DroppedValueCount}");
            Assert.True(publishingMonitoringResultJson.DuplicateValueCount == 0,
                $"Duplicate values detected: {publishingMonitoringResultJson.DuplicateValueCount}");

            // Check that every published node is sending data.
            if (_context.ConsumedOpcUaNodes != null) {
                var expectedNodes = _context.ConsumedOpcUaNodes.First().Value.OpcNodes.Select(n => n.Id).ToList();
                foreach (var property in publishingMonitoringResultJson.ValueChangesByNodeId) {
                    var propertyName = property.Key;
                    var nodeId = propertyName.Split('#').Last();
                    var expected = expectedNodes.FirstOrDefault(n => n.EndsWith(nodeId));
                    Assert.True(expected != null, $"Publishing from unexpected node: {propertyName}");
                    expectedNodes.Remove(expected);
                }

                expectedNodes.ForEach(n => _context.OutputHelper.WriteLine(n));
                Assert.Empty(expectedNodes);
            }

            //Call Unpublish direct method
            response = await CallMethodAsync(
                new MethodParameterModel {
                    Name = TestConstants.DirectMethodLegacyNames.UnpublishNodes,
                    JsonPayload = _serializer.SerializeToString(request)
                },
                cts.Token
            ).ConfigureAwait(false);

            Assert.Equal((int)HttpStatusCode.OK, response.Status);

            // Wait till the publishing has stopped.
            await Task.Delay(TestConstants.DefaultTimeoutInMilliseconds, cts.Token).ConfigureAwait(false);

            //Call GetDiagnosticInfo direct method
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
            // as data gap analysis is not part of this test case)
            await TestHelper.StartMonitoringIncomingMessagesAsync(_context, 0, 0, 0, cts.Token).ConfigureAwait(false);

            // Wait some time to generate events to process.
            await Task.Delay(TestConstants.DefaultTimeoutInMilliseconds, cts.Token).ConfigureAwait(false);

            // Stop monitoring and get the result.
            var unpublishingMonitoringResultJson = await TestHelper.StopMonitoringIncomingMessagesAsync(_context, cts.Token);
            Assert.True(unpublishingMonitoringResultJson.TotalValueChangesCount == 0,
                $"Messages received at IoT Hub: {unpublishingMonitoringResultJson.TotalValueChangesCount}");
        }
    }
}
