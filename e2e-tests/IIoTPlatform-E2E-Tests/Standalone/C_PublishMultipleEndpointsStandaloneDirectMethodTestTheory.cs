﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatform_E2E_Tests.Standalone {
    using IIoTPlatform_E2E_Tests.Deploy;
    using IIoTPlatform_E2E_Tests.TestModels;
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models;
    using Microsoft.Azure.Devices;
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using TestExtensions;
    using Xunit;
    using Xunit.Abstractions;
    using Microsoft.Azure.IIoT.Hub.Models;
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Azure.IIoT.Serializers.NewtonSoft;
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
    public class C_PublishMultipleEndpointsStandaloneDirectMethodTestTheory {

        private readonly ITestOutputHelper _output;
        private readonly IIoTMultipleNodesTestContext _context;
        private readonly ServiceClient _iotHubClient;
        private readonly IJsonSerializer _serializer;
        private string _iotHubConnectionString;
        private string _iotHubPublisherDeviceName;
        private string _iotHubPublisherModuleName;

        public C_PublishMultipleEndpointsStandaloneDirectMethodTestTheory(
            ITestOutputHelper output,
            IIoTMultipleNodesTestContext context
        ) {
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _context.OutputHelper = _output;
            _iotHubConnectionString = _context.IoTHubConfig.IoTHubConnectionString;
            _iotHubPublisherDeviceName = _context.DeviceConfig.DeviceId;
            _serializer = new NewtonSoftJsonSerializer();

            // Initialize DeviceServiceClient from IoT Hub connection string.
            _iotHubClient = TestHelper.DeviceServiceClient(
                _iotHubConnectionString,
                TransportType.Amqp_WebSocket_Only
            );
        }

        [Theory]
        [InlineData(MessagingMode.Samples)]
        [InlineData(MessagingMode.PubSub)]
        async Task SubscribeUnsubscribeDirectMethodTest(MessagingMode messagingMode) {
            var ioTHubEdgeBaseDeployment = new IoTHubEdgeBaseDeployment(_context);
            var ioTHubPublisherDeployment = new IoTHubPublisherDeployment(_context, messagingMode);

            _iotHubPublisherModuleName = "publisher_standalone";

            // Clear context.
            _context.Reset();

            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);

            // Make sure that there is no active monitoring.
            await TestHelper.StopMonitoringIncomingMessagesAsync(_context, cts.Token);

            // Clean publishednodes.json.
            await TestHelper.PublishNodesAsync(Array.Empty<PublishedNodesEntryModel>(), _context);

            // Create base edge deployment.
            var baseDeploymentResult = await ioTHubEdgeBaseDeployment.CreateOrUpdateLayeredDeploymentAsync(cts.Token);
            Assert.True(baseDeploymentResult, "Failed to create/update new edge base deployment.");
            _output.WriteLine("Created/Updated new edge base deployment.");

            // Create layered edge deployment.
            var layeredDeploymentResult = await ioTHubPublisherDeployment.CreateOrUpdateLayeredDeploymentAsync(cts.Token);
            Assert.True(layeredDeploymentResult, "Failed to create/update layered deployment for publisher module.");
            _output.WriteLine("Created/Updated layered deployment for publisher module.");

            var nodesToPublish = await TestHelper.CreateMultipleNodesModelAsync(_context, cts.Token);

            // We will wait for module to be deployed.
            var exception = Record.Exception(() => _context.RegistryHelper.WaitForIIoTModulesConnectedAsync(
                _context.DeviceConfig.DeviceId,
                cts.Token,
                new string[] { "publisher_standalone" }
            ).GetAwaiter().GetResult());
            Assert.Null(exception);

            //Call GetConfiguredEndpoints direct method, initially there should be no endpoints
            var responseGetConfiguredEndpoints = await TestHelper.CallMethodAsync(_iotHubClient, _iotHubPublisherDeviceName, _iotHubPublisherModuleName, new MethodParameterModel {
                Name = TestConstants.DirectMethodNames.GetConfiguredEndpoints
            }, _context, cts.Token).ConfigureAwait(false);

            Assert.Equal((int)HttpStatusCode.OK, responseGetConfiguredEndpoints.Status);
            var configuredEndpointsResponse = _serializer.Deserialize<List<PublishNodesEndpointApiModel>>(responseGetConfiguredEndpoints.JsonPayload);
            Assert.Equal(configuredEndpointsResponse.Count, 0);

            var request = nodesToPublish.ToApiModel();

            //Call Publish direct method
            var response = await TestHelper.CallMethodAsync(_iotHubClient, _iotHubPublisherDeviceName, _iotHubPublisherModuleName, new MethodParameterModel {
                Name = TestConstants.DirectMethodNames.PublishNodes,
                JsonPayload = _serializer.SerializeToString(request)
            }, _context, cts.Token).ConfigureAwait(false);

            Assert.Equal((int)HttpStatusCode.OK, response.Status);

            //publish nodes on a different enpoint
            nodesToPublish = await TestHelper.CreateMultipleNodesModelAsync(_context, cts.Token, 5, 100);
            var request1 = nodesToPublish.ToApiModel();

            //Call Publish direct method
            response = await TestHelper.CallMethodAsync(_iotHubClient, _iotHubPublisherDeviceName, _iotHubPublisherModuleName, new MethodParameterModel {
                Name = TestConstants.DirectMethodNames.PublishNodes,
                JsonPayload = _serializer.SerializeToString(request1)
            }, _context, cts.Token).ConfigureAwait(false);

            Assert.Equal((int)HttpStatusCode.OK, response.Status);

            // Use test event processor to verify data send to IoT Hub (expected* set to zero
            // as data gap analysis is not part of this test case)
            await TestHelper.StartMonitoringIncomingMessagesAsync(_context, 500, 10_000, 90_000_000, cts.Token);

            // Wait some time to generate events to process.
            await Task.Delay(TestConstants.DefaultTimeoutInMilliseconds, cts.Token);

            //Call GetConfiguredEndpoints direct method
            responseGetConfiguredEndpoints = await TestHelper.CallMethodAsync(_iotHubClient, _iotHubPublisherDeviceName, _iotHubPublisherModuleName, new MethodParameterModel {
                Name = TestConstants.DirectMethodNames.GetConfiguredEndpoints
            }, _context, cts.Token).ConfigureAwait(false);

            Assert.Equal((int)HttpStatusCode.OK, responseGetConfiguredEndpoints.Status);
            configuredEndpointsResponse = _serializer.Deserialize<List<PublishNodesEndpointApiModel>>(responseGetConfiguredEndpoints.JsonPayload);
            Assert.Equal(configuredEndpointsResponse.Count, 2);
            TestHelper.Publisher.AssertEndpointModel(configuredEndpointsResponse[0], request);
            TestHelper.Publisher.AssertEndpointModel(configuredEndpointsResponse[1], request1);

            //Create request for GetConfiguredNodesOnEndpoint method call
            var nodesOnEndpoint = new PublishedNodesEntryModel();
            nodesOnEndpoint.EndpointUrl = request1.EndpointUrl;
            var requestGetConfiguredNodesOnEndpoint = nodesOnEndpoint.ToApiModel();

            //Call GetConfiguredNodesOnEndpoint direct method
            var responseGetConfiguredNodesOnEndpoint = await TestHelper.CallMethodAsync(_iotHubClient, _iotHubPublisherDeviceName, _iotHubPublisherModuleName, new MethodParameterModel {
                Name = TestConstants.DirectMethodNames.GetConfiguredNodesOnEndpoint,
                JsonPayload = _serializer.SerializeToString(requestGetConfiguredNodesOnEndpoint)
            }, _context, cts.Token).ConfigureAwait(false);

            Assert.Equal((int)HttpStatusCode.OK, responseGetConfiguredNodesOnEndpoint.Status);
            var jsonResponse = _serializer.Deserialize<List<PublishedNodeApiModel>>(responseGetConfiguredNodesOnEndpoint.JsonPayload);
            Assert.Equal(jsonResponse.Count, 100);

            // Stop monitoring and get the result.
            var publishingMonitoringResultJson = await TestHelper.StopMonitoringIncomingMessagesAsync(_context, cts.Token);
            Assert.True((int)publishingMonitoringResultJson.totalValueChangesCount > 0, "No messages received at IoT Hub");

            // Check that every published node is sending data.
            if (_context.ConsumedOpcUaNodes != null) {
                var expectedNodes = _context.ConsumedOpcUaNodes.First().Value.OpcNodes.Select(n => n.Id).ToList();
                foreach (dynamic property in publishingMonitoringResultJson.valueChangesByNodeId) {
                    var propertyName = (string)property.Name;
                    var nodeId = propertyName.Split('#').Last();
                    var expected = expectedNodes.FirstOrDefault(n => n.EndsWith(nodeId));
                    Assert.True(expected != null, $"Publishing from unexpected node: {propertyName}");
                    expectedNodes.Remove(expected);
                }

                expectedNodes.ForEach(n => _context.OutputHelper.WriteLine(n));
                Assert.Empty(expectedNodes);
            }

            //Call Unpublish direct method
            response = await TestHelper.CallMethodAsync(_iotHubClient, _iotHubPublisherDeviceName, _iotHubPublisherModuleName, new MethodParameterModel {
                Name = TestConstants.DirectMethodNames.UnPublishNodes,
                JsonPayload = _serializer.SerializeToString(request)
            }, _context, cts.Token).ConfigureAwait(false);

            Assert.Equal((int)HttpStatusCode.OK, response.Status);

            //Call Unpublish direct method
            response = await TestHelper.CallMethodAsync(_iotHubClient, _iotHubPublisherDeviceName, _iotHubPublisherModuleName, new MethodParameterModel {
                Name = TestConstants.DirectMethodNames.UnPublishNodes,
                JsonPayload = _serializer.SerializeToString(request1)
            }, _context, cts.Token).ConfigureAwait(false);

            Assert.Equal((int)HttpStatusCode.OK, response.Status);

            //Call GetConfiguredEndpoints direct method
            responseGetConfiguredEndpoints = await TestHelper.CallMethodAsync(_iotHubClient, _iotHubPublisherDeviceName, _iotHubPublisherModuleName, new MethodParameterModel {
                Name = TestConstants.DirectMethodNames.GetConfiguredEndpoints
            }, _context, cts.Token).ConfigureAwait(false);

            Assert.Equal((int)HttpStatusCode.OK, responseGetConfiguredEndpoints.Status);
            configuredEndpointsResponse = _serializer.Deserialize<List<PublishNodesEndpointApiModel>>(responseGetConfiguredEndpoints.JsonPayload);
            Assert.Equal(configuredEndpointsResponse.Count, 0);

            // Wait till the publishing has stopped.
            await Task.Delay(TestConstants.DefaultTimeoutInMilliseconds, cts.Token);

            // Use test event processor to verify data send to IoT Hub (expected* set to zero
            // as data gap analysis is not part of this test case)
            await TestHelper.StartMonitoringIncomingMessagesAsync(_context, 0, 0, 0, cts.Token);

            // Wait some time to generate events to process.
            await Task.Delay(TestConstants.DefaultTimeoutInMilliseconds, cts.Token);

            // Stop monitoring and get the result.
            var unpublishingMonitoringResultJson = await TestHelper.StopMonitoringIncomingMessagesAsync(_context, cts.Token);
            Assert.True((int)unpublishingMonitoringResultJson.totalValueChangesCount == 0,
                $"Messages received at IoT Hub: {(int)unpublishingMonitoringResultJson.totalValueChangesCount}");
        }


        [Fact]
        async Task SubscribeUnsubscribeDirectMethodLegacyPublisherTest() {
            var ioTHubEdgeBaseDeployment = new IoTHubEdgeBaseDeployment(_context);
            var ioTHubLegacyPublisherDeployment = new IoTHubLegacyPublisherDeployments(_context);

            _iotHubPublisherModuleName = "publisher_standalone_legacy";

            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);

            // Make sure that there is no active monitoring.
            await TestHelper.StopMonitoringIncomingMessagesAsync(_context, cts.Token);

            // Clean publishednodes.json.
            await TestHelper.PublishNodesAsync(Array.Empty<PublishedNodesEntryModel>(), _context);

            // Create base edge deployment.
            var baseDeploymentResult = await ioTHubEdgeBaseDeployment.CreateOrUpdateLayeredDeploymentAsync(cts.Token);
            Assert.True(baseDeploymentResult, "Failed to create/update new edge base deployment.");
            _output.WriteLine("Created/Updated new edge base deployment.");

            // Create layered edge deployment.
            var layeredDeploymentResult1 = await ioTHubLegacyPublisherDeployment.CreateOrUpdateLayeredDeploymentAsync(cts.Token);
            Assert.True(layeredDeploymentResult1, "Failed to create/update layered deployment for legacy publisher module.");
            _output.WriteLine("Created/Updated layered deployment for legacy publisher module.");

            var nodesToPublish = await TestHelper.CreateMultipleNodesModelAsync(_context, cts.Token);

            // We will wait for module to be deployed.
            var exception = Record.Exception(() => _context.RegistryHelper.WaitForIIoTModulesConnectedAsync(
                _context.DeviceConfig.DeviceId,
                cts.Token,
                new string[] { "publisher_standalone_legacy" }
            ).GetAwaiter().GetResult());
            Assert.Null(exception);

            //Call GetConfiguredEndpoints direct method, initially there should be no endpoints
            var responseGetConfiguredEndpoints = await TestHelper.CallMethodAsync(_iotHubClient, _iotHubPublisherDeviceName, _iotHubPublisherModuleName, new MethodParameterModel {
                Name = TestConstants.DirectMethodLegacyNames.GetConfiguredEndpoints
            }, _context, cts.Token).ConfigureAwait(false);

            Assert.Equal((int)HttpStatusCode.OK, responseGetConfiguredEndpoints.Status);
            var epObj = JObject.Parse(responseGetConfiguredEndpoints.JsonPayload);
            var endpoints = _serializer.SerializeToString(epObj["Endpoints"]);
            var configuredEndpointsResponse = _serializer.Deserialize<List<PublishNodesEndpointApiModel>>(endpoints);
            Assert.Equal(configuredEndpointsResponse.Count, 0);

            var request = nodesToPublish.ToApiModel();

            //Call Publish direct method
            var response = await TestHelper.CallMethodAsync(_iotHubClient, _iotHubPublisherDeviceName, _iotHubPublisherModuleName, new MethodParameterModel {
                Name = TestConstants.DirectMethodLegacyNames.PublishNodes,
                JsonPayload = _serializer.SerializeToString(request)
            }, _context, cts.Token).ConfigureAwait(false);

            Assert.Equal((int)HttpStatusCode.OK, response.Status);

            //publish nodes on a different enpoint
            nodesToPublish = await TestHelper.CreateMultipleNodesModelAsync(_context, cts.Token, 5, 100);
            var request1 = nodesToPublish.ToApiModel();

            //Call Publish direct method
            response = await TestHelper.CallMethodAsync(_iotHubClient, _iotHubPublisherDeviceName, _iotHubPublisherModuleName, new MethodParameterModel {
                Name = TestConstants.DirectMethodLegacyNames.PublishNodes,
                JsonPayload = _serializer.SerializeToString(request1)
            }, _context, cts.Token).ConfigureAwait(false);

            Assert.Equal((int)HttpStatusCode.OK, response.Status);

            // Use test event processor to verify data send to IoT Hub (expected* set to zero
            // as data gap analysis is not part of this test case)
            await TestHelper.StartMonitoringIncomingMessagesAsync(_context, 500, 10_000, 90_000_000, cts.Token);

            // Wait some time to generate events to process.
            await Task.Delay(TestConstants.DefaultTimeoutInMilliseconds, cts.Token);

            //Call GetConfiguredEndpoints direct method
            responseGetConfiguredEndpoints = await TestHelper.CallMethodAsync(_iotHubClient, _iotHubPublisherDeviceName, _iotHubPublisherModuleName, new MethodParameterModel {
                Name = TestConstants.DirectMethodLegacyNames.GetConfiguredEndpoints
            }, _context, cts.Token).ConfigureAwait(false);

            Assert.Equal((int)HttpStatusCode.OK, responseGetConfiguredEndpoints.Status);
            epObj = JObject.Parse(responseGetConfiguredEndpoints.JsonPayload);
            endpoints = _serializer.SerializeToString(epObj["Endpoints"]);
            configuredEndpointsResponse = _serializer.Deserialize<List<PublishNodesEndpointApiModel>>(endpoints);
            Assert.Equal(2, configuredEndpointsResponse.Count);
            TestHelper.Publisher.AssertEndpointModel(configuredEndpointsResponse[0], request);
            TestHelper.Publisher.AssertEndpointModel(configuredEndpointsResponse[1], request1);

            //Create request for GetConfiguredNodesOnEndpoint method call
            var nodesOnEndpoint = new PublishedNodesEntryModel();
            nodesOnEndpoint.EndpointUrl = request1.EndpointUrl;
            var requestGetConfiguredNodesOnEndpoint = nodesOnEndpoint.ToApiModel();

            //Call GetConfiguredNodesOnEndpoint direct method
            var responseGetConfiguredNodesOnEndpoint = await TestHelper.CallMethodAsync(_iotHubClient, _iotHubPublisherDeviceName, _iotHubPublisherModuleName, new MethodParameterModel {
                Name = TestConstants.DirectMethodLegacyNames.GetConfiguredNodesOnEndpoint,
                JsonPayload = _serializer.SerializeToString(requestGetConfiguredNodesOnEndpoint)
            }, _context, cts.Token).ConfigureAwait(false);

            Assert.Equal((int)HttpStatusCode.OK, responseGetConfiguredNodesOnEndpoint.Status);

            var obj = JObject.Parse(responseGetConfiguredNodesOnEndpoint.JsonPayload);
            var opcNodes = _serializer.SerializeToString(obj["OpcNodes"]);
            var jsonResponse = _serializer.Deserialize<List<PublishedNodeApiModel>>(opcNodes);
            Assert.Equal(jsonResponse.Count, 100);

            // Stop monitoring and get the result.
            var publishingMonitoringResultJson = await TestHelper.StopMonitoringIncomingMessagesAsync(_context, cts.Token);
            Assert.True((int)publishingMonitoringResultJson.totalValueChangesCount > 0, "No messages received at IoT Hub");
            
            // Check that every published node is sending data.
            if (_context.ConsumedOpcUaNodes != null) {
                var expectedNodes = _context.ConsumedOpcUaNodes.First().Value.OpcNodes.Select(n => n.Id).ToList();
                foreach (dynamic property in publishingMonitoringResultJson.valueChangesByNodeId) {
                    var propertyName = (string)property.Name;
                    var nodeId = propertyName.Split('#').Last();
                    var expected = expectedNodes.FirstOrDefault(n => n.EndsWith(nodeId));
                    Assert.True(expected != null, $"Publishing from unexpected node: {propertyName}");
                    expectedNodes.Remove(expected);
                }

                expectedNodes.ForEach(n => _context.OutputHelper.WriteLine(n));
                Assert.Empty(expectedNodes);
            }

            //Call Unpublish direct method
            response = await TestHelper.CallMethodAsync(_iotHubClient, _iotHubPublisherDeviceName, _iotHubPublisherModuleName, new MethodParameterModel {
                Name = TestConstants.DirectMethodLegacyNames.UnPublishNodes,
                JsonPayload = _serializer.SerializeToString(request)
            }, _context, cts.Token).ConfigureAwait(false);

            Assert.Equal((int)HttpStatusCode.OK, response.Status);

            //Call Unpublish direct method
            response = await TestHelper.CallMethodAsync(_iotHubClient, _iotHubPublisherDeviceName, _iotHubPublisherModuleName, new MethodParameterModel {
                Name = TestConstants.DirectMethodLegacyNames.UnPublishNodes,
                JsonPayload = _serializer.SerializeToString(request1)
            }, _context, cts.Token).ConfigureAwait(false);

            Assert.Equal((int)HttpStatusCode.OK, response.Status);

            //Call GetConfiguredEndpoints direct method
            responseGetConfiguredEndpoints = await TestHelper.CallMethodAsync(_iotHubClient, _iotHubPublisherDeviceName, _iotHubPublisherModuleName, new MethodParameterModel {
                Name = TestConstants.DirectMethodLegacyNames.GetConfiguredEndpoints
            }, _context, cts.Token).ConfigureAwait(false);

            Assert.Equal((int)HttpStatusCode.OK, responseGetConfiguredEndpoints.Status);
            epObj = JObject.Parse(responseGetConfiguredEndpoints.JsonPayload);
            endpoints = _serializer.SerializeToString(epObj["Endpoints"]);
            configuredEndpointsResponse = _serializer.Deserialize<List<PublishNodesEndpointApiModel>>(endpoints);
            Assert.Equal(configuredEndpointsResponse.Count, 0);

            // Wait till the publishing has stopped.
            await Task.Delay(TestConstants.DefaultTimeoutInMilliseconds, cts.Token);

            // Use test event processor to verify data send to IoT Hub (expected* set to zero
            // as data gap analysis is not part of this test case)
            await TestHelper.StartMonitoringIncomingMessagesAsync(_context, 0, 0, 0, cts.Token);

            // Wait some time to generate events to process.
            await Task.Delay(TestConstants.DefaultTimeoutInMilliseconds, cts.Token);

            // Stop monitoring and get the result.
            var unpublishingMonitoringResultJson = await TestHelper.StopMonitoringIncomingMessagesAsync(_context, cts.Token);
            Assert.True((int)unpublishingMonitoringResultJson.totalValueChangesCount == 0,
                $"Messages received at IoT Hub: {(int)unpublishingMonitoringResultJson.totalValueChangesCount}");
        }
    }
}
