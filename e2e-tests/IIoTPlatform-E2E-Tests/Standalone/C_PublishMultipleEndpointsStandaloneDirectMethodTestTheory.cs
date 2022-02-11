// ------------------------------------------------------------
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
    public class C_PublishMultipleEndpointsStandaloneDirectMethodTestTheory : DirectMethodTestBase {

        public C_PublishMultipleEndpointsStandaloneDirectMethodTestTheory(
            ITestOutputHelper output,
            IIoTMultipleNodesTestContext context
        ) : base(output, context) {}

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

            var nodesToPublish0 = await TestHelper.CreateMultipleNodesModelAsync(_context, cts.Token, 2, 250).ConfigureAwait(false);
            var fastNodes0 = nodesToPublish0.OpcNodes.Where(node => node.Id.Contains("fast", StringComparison.OrdinalIgnoreCase)).ToList();
            var slowNodes0 = nodesToPublish0.OpcNodes.Where(node => node.Id.Contains("slow", StringComparison.OrdinalIgnoreCase)).ToList();
            var nodes0 = new List<OpcUaNodesModel>();
            nodes0.AddRange(fastNodes0.Take(25));
            nodes0.AddRange(slowNodes0.Take(100));
            nodesToPublish0.OpcNodes = nodes0.ToArray();

            var request0 = nodesToPublish0.ToApiModel();
            MethodResultModel response = null;

            //Publish nodes for endpoint 0
            if (useAddOrUpdate) {
                //Call AddOrUpdateEndpoints direct method
                response = await CallMethodAsync(
                    new MethodParameterModel {
                        Name = TestConstants.DirectMethodNames.AddOrUpdateEndpoints,
                        JsonPayload = _serializer.SerializeToString(new List<PublishNodesEndpointApiModel> { request0 })
                    },
                    cts.Token
                ).ConfigureAwait(false);
            }
            else {
                //Call PublishNodes direct method
                response = await CallMethodAsync(
                    new MethodParameterModel {
                        Name = TestConstants.DirectMethodNames.PublishNodes,
                        JsonPayload = _serializer.SerializeToString(request0)
                    },
                    cts.Token
                ).ConfigureAwait(false);
            }

            Assert.Equal((int)HttpStatusCode.OK, response.Status);

            //publish nodes on a different enpoint
            var nodesToPublish1 = await TestHelper.CreateMultipleNodesModelAsync(_context, cts.Token, 5, 250).ConfigureAwait(false);
            var fastNodes1 = nodesToPublish1.OpcNodes.Where(node => node.Id.Contains("fast", StringComparison.OrdinalIgnoreCase)).ToList();
            var slowNodes1 = nodesToPublish1.OpcNodes.Where(node => node.Id.Contains("slow", StringComparison.OrdinalIgnoreCase)).ToList();
            var nodes1 = new List<OpcUaNodesModel>();
            nodes1.AddRange(fastNodes1.Skip(25).Take(25));
            nodes1.AddRange(slowNodes1.Skip(100).Take(100));
            nodesToPublish1.OpcNodes = nodes1.ToArray();

            var request1 = nodesToPublish1.ToApiModel();

            //Publish nodes for endpoint 1
            if (useAddOrUpdate) {
                //Call AddOrUpdateEndpoints direct method
                response = await CallMethodAsync(
                    new MethodParameterModel {
                        Name = TestConstants.DirectMethodNames.AddOrUpdateEndpoints,
                        JsonPayload = _serializer.SerializeToString(new List<PublishNodesEndpointApiModel> { request1 })
                    },
                    cts.Token
                ).ConfigureAwait(false);
            }
            else {
                //Call PublishNodes direct method
                response = await CallMethodAsync(
                    new MethodParameterModel {
                        Name = TestConstants.DirectMethodNames.PublishNodes,
                        JsonPayload = _serializer.SerializeToString(request1)
                    },
                    cts.Token
                ).ConfigureAwait(false);
            }

            Assert.Equal((int)HttpStatusCode.OK, response.Status);

            // Use test event processor to verify data send to IoT Hub (expected* set to zero
            // as data gap analysis is not part of this test case)
            await TestHelper.StartMonitoringIncomingMessagesAsync(_context, 500, 10_000, 90_000_000, cts.Token).ConfigureAwait(false);

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
            Assert.Equal(configuredEndpointsResponse.Count, 2);
            TestHelper.Publisher.AssertEndpointModel(configuredEndpointsResponse[0], request0);
            TestHelper.Publisher.AssertEndpointModel(configuredEndpointsResponse[1], request1);

            //Create request for GetConfiguredNodesOnEndpoint method call for endpoint 0
            var nodesOnEndpoint0 = new PublishedNodesEntryModel {
                EndpointUrl = request0.EndpointUrl,
            };
            var requestGetConfiguredNodesOnEndpoint0 = nodesOnEndpoint0.ToApiModel();

            //Call GetConfiguredNodesOnEndpoint direct method for endpoint 0
            var responseGetConfiguredNodesOnEndpoint0 = await CallMethodAsync(
                new MethodParameterModel {
                    Name = TestConstants.DirectMethodNames.GetConfiguredNodesOnEndpoint,
                    JsonPayload = _serializer.SerializeToString(requestGetConfiguredNodesOnEndpoint0)
                },
                cts.Token
            ).ConfigureAwait(false);

            Assert.Equal((int)HttpStatusCode.OK, responseGetConfiguredNodesOnEndpoint0.Status);
            var jsonResponse0 = _serializer.Deserialize<List<PublishedNodeApiModel>>(responseGetConfiguredNodesOnEndpoint0.JsonPayload);
            Assert.Equal(jsonResponse0.Count, 125);
            Assert.Equal(jsonResponse0[0].Id, nodes0[0].Id);
            Assert.Equal(jsonResponse0[25].Id, nodes0[25].Id);

            //Create request for GetConfiguredNodesOnEndpoint method call for endpoint 1
            var nodesOnEndpoint1 = new PublishedNodesEntryModel {
                EndpointUrl = request1.EndpointUrl,
            };
            var requestGetConfiguredNodesOnEndpoint1 = nodesOnEndpoint1.ToApiModel();

            //Call GetConfiguredNodesOnEndpoint direct method for endpoint 1
            var responseGetConfiguredNodesOnEndpoint1 = await CallMethodAsync(
                new MethodParameterModel {
                    Name = TestConstants.DirectMethodNames.GetConfiguredNodesOnEndpoint,
                    JsonPayload = _serializer.SerializeToString(requestGetConfiguredNodesOnEndpoint1)
                },
                cts.Token
            ).ConfigureAwait(false);

            Assert.Equal((int)HttpStatusCode.OK, responseGetConfiguredNodesOnEndpoint1.Status);
            var jsonResponse1 = _serializer.Deserialize<List<PublishedNodeApiModel>>(responseGetConfiguredNodesOnEndpoint1.JsonPayload);
            Assert.Equal(jsonResponse1.Count, 125);
            Assert.Equal(jsonResponse1[0].Id, nodes1[0].Id);
            Assert.Equal(jsonResponse1[25].Id, nodes1[25].Id);

            //Call GetDiagnosticInfo direct method
            var responseGetDiagnosticInfo = await CallMethodAsync(
                new MethodParameterModel {
                    Name = TestConstants.DirectMethodNames.GetDiagnosticInfo,
                },
                cts.Token
            ).ConfigureAwait(false);

            Assert.Equal((int)HttpStatusCode.OK, responseGetDiagnosticInfo.Status);
            var diagInfoList = _serializer.Deserialize<List<JobDiagnosticInfoModel>>(responseGetDiagnosticInfo.JsonPayload);
            Assert.Equal(diagInfoList.Count, 2);

            foreach (var diagInfo in diagInfoList) {
                Assert.True(diagInfo.IngressValueChanges > 0);
                Assert.True(diagInfo.IngressDataChanges > 0);
                Assert.Equal(0, diagInfo.MonitoredOpcNodesFailedCount);
                Assert.Equal(10, diagInfo.MonitoredOpcNodesSucceededCount);
                Assert.True(diagInfo.OpcEndpointConnected);
                Assert.True(diagInfo.OutgressIoTMessageCount > 0);

                // Check that we are not dropping anything.
                Assert.Equal((uint)0, diagInfo.EncoderNotificationsDropped);
                Assert.Equal((ulong)0, diagInfo.OutgressInputBufferDropped);
            }

            // Stop monitoring and get the result.
            var publishingMonitoringResultJson = await TestHelper.StopMonitoringIncomingMessagesAsync(_context, cts.Token).ConfigureAwait(false);
            Assert.True(publishingMonitoringResultJson.TotalValueChangesCount > 0, "No messages received at IoT Hub");

            // Check that every published node is sending data.
            if (_context.ConsumedOpcUaNodes != null) {
                var expectedNodes = new HashSet<string> ();
                foreach (var consumedNodes in _context.ConsumedOpcUaNodes) {
                    foreach (var opcNode in consumedNodes.Value.OpcNodes) {
                        expectedNodes.Add(opcNode.Id);
                    }
                }

                foreach (var property in publishingMonitoringResultJson.ValueChangesByNodeId) {
                    var propertyName = property.Key;
                    var nodeId = propertyName.Split('#').Last();
                    var expected = expectedNodes.FirstOrDefault(n => n.EndsWith(nodeId));
                    Assert.True(expected != null, $"Publishing from unexpected node: {propertyName}");
                    expectedNodes.Remove(expected);
                }

                foreach(var expectedNode in expectedNodes) {
                    _context.OutputHelper.WriteLine(expectedNode);
                }
                Assert.Empty(expectedNodes);
            }

            //Unpublish all nodes for endpoint 0
            if (useAddOrUpdate) {
                //Call AddOrUpdateEndpoints direct method
                request0.OpcNodes?.Clear();
                response = await CallMethodAsync(
                    new MethodParameterModel {
                        Name = TestConstants.DirectMethodNames.AddOrUpdateEndpoints,
                        JsonPayload = _serializer.SerializeToString(new List<PublishNodesEndpointApiModel> { request0 })
                    },
                    cts.Token
                ).ConfigureAwait(false);
            }
            else {
                //Call UnpublishNodes direct method
                response = await CallMethodAsync(
                    new MethodParameterModel {
                        Name = TestConstants.DirectMethodNames.UnpublishNodes,
                        JsonPayload = _serializer.SerializeToString(request0)
                    },
                    cts.Token
                ).ConfigureAwait(false);
            }

            Assert.Equal((int)HttpStatusCode.OK, response.Status);

            //Call GetDiagnosticInfo direct method
            responseGetDiagnosticInfo = await CallMethodAsync(
                new MethodParameterModel {
                    Name = TestConstants.DirectMethodNames.GetDiagnosticInfo,
                },
                cts.Token
            ).ConfigureAwait(false);

            Assert.Equal((int)HttpStatusCode.OK, responseGetDiagnosticInfo.Status);
            diagInfoList = _serializer.Deserialize<List<JobDiagnosticInfoModel>>(responseGetDiagnosticInfo.JsonPayload);
            Assert.Equal(diagInfoList.Count, 1);

            //Call UnpublishAll direct method
            request1.OpcNodes?.Clear();
            if (useAddOrUpdate) {
                //Call AddOrUpdateEndpoints direct method
                response = await CallMethodAsync(
                    new MethodParameterModel {
                        Name = TestConstants.DirectMethodNames.AddOrUpdateEndpoints,
                        JsonPayload = _serializer.SerializeToString(new List<PublishNodesEndpointApiModel> { request1 })
                    },
                    cts.Token
                ).ConfigureAwait(false);
            }
            else {
                //Call UnpublishAllNodes direct method
                response = await CallMethodAsync(
                    new MethodParameterModel {
                        Name = TestConstants.DirectMethodNames.UnpublishAllNodes,
                        JsonPayload = _serializer.SerializeToString(request1)
                    },
                    cts.Token
                ).ConfigureAwait(false);
            }

            Assert.Equal((int)HttpStatusCode.OK, response.Status);

            //Call GetConfiguredEndpoints direct method
            responseGetConfiguredEndpoints = await CallMethodAsync(
                new MethodParameterModel {
                    Name = TestConstants.DirectMethodNames.GetConfiguredEndpoints
                },
                cts.Token
            ).ConfigureAwait(false);

            Assert.Equal((int)HttpStatusCode.OK, responseGetConfiguredEndpoints.Status);
            configuredEndpointsResponse = _serializer.Deserialize<List<PublishNodesEndpointApiModel>>(responseGetConfiguredEndpoints.JsonPayload);
            Assert.Equal(configuredEndpointsResponse.Count, 0);

            //Call GetDiagnosticInfo direct method
            responseGetDiagnosticInfo = await CallMethodAsync(
                new MethodParameterModel {
                    Name = TestConstants.DirectMethodNames.GetDiagnosticInfo,
                },
                cts.Token
            ).ConfigureAwait(false);

            Assert.Equal((int)HttpStatusCode.OK, responseGetDiagnosticInfo.Status);
            diagInfoList = _serializer.Deserialize<List<JobDiagnosticInfoModel>>(responseGetDiagnosticInfo.JsonPayload);
            Assert.Equal(diagInfoList.Count, 1);

            // Wait till the publishing has stopped.
            await Task.Delay(TestConstants.DefaultTimeoutInMilliseconds, cts.Token).ConfigureAwait(false);

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

        [Theory]
        [InlineData(MessagingMode.Samples)]
        [InlineData(MessagingMode.PubSub)]
        async Task AddOrUpdateEndpointsTest(MessagingMode messagingMode) {
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
            Assert.Equal(0, configuredEndpointsResponse.Count);

            // Use test event processor to verify data send to IoT Hub (expected* set to zero
            // as data gap analysis is not part of this test case)
            await TestHelper.StartMonitoringIncomingMessagesAsync(_context, 0, 10_000, 20_000, cts.Token).ConfigureAwait(false);

            var endpointsCount = _context.SimulatedPublishedNodes?.Count
                ?? _context.OpcPlcConfig.Urls
                    .Split(TestConstants.SimulationUrlsSeparator)
                    .Where(e => !string.IsNullOrWhiteSpace(e))
                    .Count();
            Assert.True(endpointsCount > 0, "no endpoints found to generate requests");

            var fullNodes = new List<PublishedNodesEntryModel>();
            for (int index = 0; index < endpointsCount; ++index) {
                var node = await TestHelper.CreateMultipleNodesModelAsync(_context, cts.Token, index, 250).ConfigureAwait(false);
                node.OpcNodes = node.OpcNodes
                    .Where(node => node.Id.Contains("slow", StringComparison.OrdinalIgnoreCase))
                    .Skip(index * endpointsCount)
                    .Take(endpointsCount)
                    .ToArray();

                fullNodes.Add(node);
            }

            var currentNodes = new List<PublishedNodesEntryModel>();
            for (int index = 0; index < endpointsCount; ++index) {
                var node = await TestHelper.CreateMultipleNodesModelAsync(_context, cts.Token, index, 0).ConfigureAwait(false);
                currentNodes.Add(node);
            }

            for (int index = 0; index < endpointsCount; ++index) {
                var request = new List<PublishNodesEndpointApiModel>();
                for (int i = 0; i <= index; ++i) {
                    currentNodes[i].OpcNodes = fullNodes[i].OpcNodes.Take(index + 1).ToArray();
                    request.Add(currentNodes[i].ToApiModel());
                }

                //Call AddOrUpdateEndpoints direct method
                //This will exercise incremental updates to the subscriptions.
                var response = await CallMethodAsync(
                    new MethodParameterModel {
                        Name = TestConstants.DirectMethodNames.AddOrUpdateEndpoints,
                        JsonPayload = _serializer.SerializeToString(request)
                    },
                    cts.Token
                ).ConfigureAwait(false);

                Assert.Equal((int)HttpStatusCode.OK, response.Status);

                await Task.Delay(2_000).ConfigureAwait(false);
            }

            //Call GetConfiguredEndpoints direct method
            responseGetConfiguredEndpoints = await CallMethodAsync(
                new MethodParameterModel {
                    Name = TestConstants.DirectMethodNames.GetConfiguredEndpoints
                },
                cts.Token
            ).ConfigureAwait(false);

            Assert.Equal((int)HttpStatusCode.OK, responseGetConfiguredEndpoints.Status);
            configuredEndpointsResponse = _serializer.Deserialize<List<PublishNodesEndpointApiModel>>(responseGetConfiguredEndpoints.JsonPayload);
            Assert.Equal(endpointsCount, configuredEndpointsResponse.Count);

            // Check that all endpoints are present.
            for (int index = 0; index < endpointsCount; ++index) {
                var receivedEndpointUrl = configuredEndpointsResponse[index].EndpointUrl.TrimEnd('/');
                Assert.NotNull(currentNodes.Find(endpoint => endpoint.EndpointUrl.TrimEnd('/').Equals(receivedEndpointUrl)));
            }

            // Check that all expected nodes on endpoints are present.
            for (int index = 0; index < endpointsCount; ++index) {
                currentNodes[index].OpcNodes = Array.Empty<OpcUaNodesModel>();

                //Call GetConfiguredNodesOnEndpoint direct method for endpoint 0
                var responseGetConfiguredNodesOnEndpoint = await CallMethodAsync(
                    new MethodParameterModel {
                        Name = TestConstants.DirectMethodNames.GetConfiguredNodesOnEndpoint,
                        JsonPayload = _serializer.SerializeToString(currentNodes[index].ToApiModel())
                    },
                    cts.Token
                ).ConfigureAwait(false);

                Assert.Equal((int)HttpStatusCode.OK, responseGetConfiguredNodesOnEndpoint.Status);
                var receivedNodes = _serializer.Deserialize<List<PublishedNodeApiModel>>(responseGetConfiguredNodesOnEndpoint.JsonPayload);
                Assert.Equal(fullNodes[index].OpcNodes.Length, receivedNodes.Count);
                Assert.Equal(fullNodes[index].OpcNodes[0].Id, receivedNodes[0].Id);
                Assert.Equal(fullNodes[index].OpcNodes[endpointsCount - 1].Id, receivedNodes[endpointsCount - 1].Id);
            }

            // Wait some time to generate events to process.
            await Task.Delay(2 * TestConstants.DefaultTimeoutInMilliseconds, cts.Token).ConfigureAwait(false);

            // Stop monitoring and get the result.
            var publishingMonitoringResultJson = await TestHelper.StopMonitoringIncomingMessagesAsync(_context, cts.Token);

            Assert.True(publishingMonitoringResultJson.TotalValueChangesCount > 0, "No messages received at IoT Hub");
            Assert.True(publishingMonitoringResultJson.DroppedValueCount == 0,
                $"Dropped messages detected: {publishingMonitoringResultJson.DroppedValueCount}");
            // ToDo: Uncomment the check once the issue with duplicate values is resolved.
            //Assert.True(publishingMonitoringResultJson.DuplicateValueCount == 0,
            //    $"Duplicate values detected: {publishingMonitoringResultJson.DuplicateValueCount}");
            Assert.Equal(endpointsCount * endpointsCount, publishingMonitoringResultJson.ValueChangesByNodeId.Count);

            // Use test event processor to verify data send to IoT Hub (expected* set to zero
            // as data gap analysis is not part of this test case).
            // Below we are going to remove endpoints one-by-one. This operation will exercise incremental
            // updates to the subscriptions which should also not yield dropped or duplicated messages.
            await TestHelper.StartMonitoringIncomingMessagesAsync(_context, 0, 10_000, 20_000, cts.Token).ConfigureAwait(false);

            // Wait some time before running unpublishing to allow test event processor to start.
            await Task.Delay(TestConstants.DefaultTimeoutInMilliseconds, cts.Token).ConfigureAwait(false);

            //Call GetDiagnosticInfo direct method and validate that we have data for all endpoints.
            var diagInfoListResponse = await CallMethodAsync(
                new MethodParameterModel {
                    Name = TestConstants.DirectMethodNames.GetDiagnosticInfo
                },
                cts.Token
            ).ConfigureAwait(false);

            Assert.Equal((int)HttpStatusCode.OK, diagInfoListResponse.Status);
            var diagInfoList = _serializer.Deserialize<List<JobDiagnosticInfoModel>>(diagInfoListResponse.JsonPayload);
            Assert.Equal(endpointsCount, diagInfoList.Count);

            foreach (var diagInfo in diagInfoList) {
                Assert.True(diagInfo.IngressValueChanges > 0);
                Assert.True(diagInfo.IngressDataChanges > 0);
                Assert.Equal(0, diagInfo.MonitoredOpcNodesFailedCount);
                Assert.Equal(10, diagInfo.MonitoredOpcNodesSucceededCount);
                Assert.True(diagInfo.OpcEndpointConnected);
                Assert.True(diagInfo.OutgressIoTMessageCount > 0);

                // Check that we are not dropping anything.
                Assert.Equal((uint)0, diagInfo.EncoderNotificationsDropped);
                Assert.Equal((ulong)0, diagInfo.OutgressInputBufferDropped);
            }

            // This will keep track of currently published nodes.
            for (int index = 0; index < endpointsCount; ++index) {
                currentNodes[index].OpcNodes = fullNodes[index].OpcNodes;
            }

            // Now let's unpublish nodes on all endpoints.
            for (int index = 0; index < endpointsCount; ++index) {
                switch(index % 3) {
                    case 0:
                        // Let's unpublish using UnpublishNodes
                        var unpublishNodesResponse = await CallMethodAsync(
                            new MethodParameterModel {
                                Name = TestConstants.DirectMethodNames.UnpublishNodes,
                                JsonPayload = _serializer.SerializeToString(currentNodes[index].ToApiModel())
                            },
                            cts.Token
                        ).ConfigureAwait(false);

                        Assert.Equal((int)HttpStatusCode.OK, unpublishNodesResponse.Status);

                        currentNodes[index].OpcNodes = Array.Empty<OpcUaNodesModel>();

                        break;
                    case 1:
                        // Let's unpublish using UnpublishAllNodes
                        currentNodes[index].OpcNodes = Array.Empty<OpcUaNodesModel>();

                        var unpublishAllNodesResponse = await CallMethodAsync(
                            new MethodParameterModel {
                                Name = TestConstants.DirectMethodNames.UnpublishAllNodes,
                                JsonPayload = _serializer.SerializeToString(currentNodes[index].ToApiModel())
                            },
                            cts.Token
                        ).ConfigureAwait(false);

                        Assert.Equal((int)HttpStatusCode.OK, unpublishAllNodesResponse.Status);

                        break;
                    case 2:
                        var request = new List<PublishNodesEndpointApiModel>();

                        // Removing endpoint at index
                        currentNodes[index].OpcNodes = Array.Empty<OpcUaNodesModel>();
                        request.Add(currentNodes[index].ToApiModel());

                        for (int i = index + 1; i < endpointsCount; ++i) {
                            currentNodes[i].OpcNodes = fullNodes[i].OpcNodes.Take(endpointsCount - index - 1).ToArray();
                            request.Add(currentNodes[i].ToApiModel());
                        }

                        // Let's unpublish using AddOrUpdateEndpoints
                        // This will exercise incremental updates to the subscriptions.
                        var addOrUpdateEndpointsResponse = await CallMethodAsync(
                            new MethodParameterModel {
                                Name = TestConstants.DirectMethodNames.AddOrUpdateEndpoints,
                                JsonPayload = _serializer.SerializeToString(request)
                            },
                            cts.Token
                        ).ConfigureAwait(false);

                        Assert.Equal((int)HttpStatusCode.OK, addOrUpdateEndpointsResponse.Status);

                        break;
                }

                // Check that there is one less entry in diagnostic info
                diagInfoListResponse = await CallMethodAsync(
                    new MethodParameterModel {
                        Name = TestConstants.DirectMethodNames.GetDiagnosticInfo
                    },
                    cts.Token
                ).ConfigureAwait(false);

                Assert.Equal((int)HttpStatusCode.OK, diagInfoListResponse.Status);
                diagInfoList = _serializer.Deserialize<List<JobDiagnosticInfoModel>>(diagInfoListResponse.JsonPayload);
                Assert.Equal(endpointsCount - 1 - index, diagInfoList.Count);

                // Check that there is one less entry in endpoints list
                responseGetConfiguredEndpoints = await CallMethodAsync(
                    new MethodParameterModel {
                        Name = TestConstants.DirectMethodNames.GetConfiguredEndpoints
                    },
                    cts.Token
                ).ConfigureAwait(false);

                Assert.Equal((int)HttpStatusCode.OK, responseGetConfiguredEndpoints.Status);
                configuredEndpointsResponse = _serializer.Deserialize<List<PublishNodesEndpointApiModel>>(
                    responseGetConfiguredEndpoints.JsonPayload);
                Assert.Equal(endpointsCount - 1 - index, configuredEndpointsResponse.Count);

                var removedEndpointUrl = currentNodes[index].EndpointUrl.TrimEnd('/');
                Assert.Null(configuredEndpointsResponse.Find(endpoint => endpoint.EndpointUrl.TrimEnd('/').Equals(removedEndpointUrl)));

                await Task.Delay(2_000).ConfigureAwait(false);
            }

            // Stop monitoring and get the result.
            publishingMonitoringResultJson = await TestHelper.StopMonitoringIncomingMessagesAsync(_context, cts.Token);

            Assert.True(publishingMonitoringResultJson.TotalValueChangesCount > 0, "No messages received at IoT Hub");
            Assert.True(publishingMonitoringResultJson.DroppedValueCount == 0,
                $"Dropped messages detected: {publishingMonitoringResultJson.DroppedValueCount}");
            // ToDo: Uncomment the check once the issue with duplicate values is resolved.
            //Assert.True(publishingMonitoringResultJson.DuplicateValueCount == 0,
            //    $"Duplicate values detected: {publishingMonitoringResultJson.DuplicateValueCount}");
            Assert.True(publishingMonitoringResultJson.ValueChangesByNodeId.Count > 0, "No messages received at IoT Hub");

            // Wait till the publishing has stopped.
            await Task.Delay(TestConstants.DefaultTimeoutInMilliseconds, cts.Token).ConfigureAwait(false);

            // Now check that no more data is coming.

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
            var layeredDeploymentResult1 = await ioTHubLegacyPublisherDeployment.CreateOrUpdateLayeredDeploymentAsync(cts.Token);
            Assert.True(layeredDeploymentResult1, "Failed to create/update layered deployment for legacy publisher module.");
            _output.WriteLine("Created/Updated layered deployment for legacy publisher module.");

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

            var nodesToPublish0 = await TestHelper.CreateMultipleNodesModelAsync(_context, cts.Token, 2, 250).ConfigureAwait(false);
            var fastNodes0 = nodesToPublish0.OpcNodes.Where(node => node.Id.Contains("fast", StringComparison.OrdinalIgnoreCase)).ToList();
            var slowNodes0 = nodesToPublish0.OpcNodes.Where(node => node.Id.Contains("slow", StringComparison.OrdinalIgnoreCase)).ToList();
            var nodes0 = new List<OpcUaNodesModel>();
            nodes0.AddRange(fastNodes0.Take(25));
            nodes0.AddRange(slowNodes0.Take(100));
            nodesToPublish0.OpcNodes = nodes0.ToArray();

            var request0 = nodesToPublish0.ToApiModel();

            //Call Publish direct method
            var response = await CallMethodAsync(
                new MethodParameterModel {
                    Name = TestConstants.DirectMethodLegacyNames.PublishNodes,
                    JsonPayload = _serializer.SerializeToString(request0)
                },
                cts.Token
            ).ConfigureAwait(false);

            Assert.Equal((int)HttpStatusCode.OK, response.Status);

            //publish nodes on a different enpoint
            var nodesToPublish1 = await TestHelper.CreateMultipleNodesModelAsync(_context, cts.Token, 5, 250).ConfigureAwait(false);
            var fastNodes1 = nodesToPublish1.OpcNodes.Where(node => node.Id.Contains("fast", StringComparison.OrdinalIgnoreCase)).ToList();
            var slowNodes1 = nodesToPublish1.OpcNodes.Where(node => node.Id.Contains("slow", StringComparison.OrdinalIgnoreCase)).ToList();
            var nodes1 = new List<OpcUaNodesModel>();
            nodes1.AddRange(fastNodes1.Skip(25).Take(25));
            nodes1.AddRange(slowNodes1.Skip(100).Take(100));
            nodesToPublish1.OpcNodes = nodes1.ToArray();

            var request1 = nodesToPublish1.ToApiModel();

            //Call Publish direct method
            response = await CallMethodAsync(
                new MethodParameterModel {
                    Name = TestConstants.DirectMethodLegacyNames.PublishNodes,
                    JsonPayload = _serializer.SerializeToString(request1)
                },
                cts.Token
            ).ConfigureAwait(false);

            Assert.Equal((int)HttpStatusCode.OK, response.Status);

            // Use test event processor to verify data send to IoT Hub (expected* set to zero
            // as data gap analysis is not part of this test case)
            await TestHelper.StartMonitoringIncomingMessagesAsync(_context, 500, 10_000, 90_000_000, cts.Token).ConfigureAwait(false);

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
            Assert.Equal(2, configuredEndpointsResponse.Count);
            TestHelper.Publisher.AssertEndpointModel(configuredEndpointsResponse[0], request0);
            TestHelper.Publisher.AssertEndpointModel(configuredEndpointsResponse[1], request1);

            //Create request for GetConfiguredNodesOnEndpoint method call for endpoint 0
            var nodesOnEndpoint0 = new PublishedNodesEntryModel {
                EndpointUrl = request0.EndpointUrl,
            };
            var requestGetConfiguredNodesOnEndpoint0 = nodesOnEndpoint0.ToApiModel();

            //Call GetConfiguredNodesOnEndpoint direct method for endpoint 0
            var responseGetConfiguredNodesOnEndpoint0 = await CallMethodAsync(
                new MethodParameterModel {
                    Name = TestConstants.DirectMethodLegacyNames.GetConfiguredNodesOnEndpoint,
                    JsonPayload = _serializer.SerializeToString(requestGetConfiguredNodesOnEndpoint0)
                },
                cts.Token
            ).ConfigureAwait(false);

            Assert.Equal((int)HttpStatusCode.OK, responseGetConfiguredNodesOnEndpoint0.Status);

            var obj0 = JObject.Parse(responseGetConfiguredNodesOnEndpoint0.JsonPayload);
            var opcNodes0 = _serializer.SerializeToString(obj0["OpcNodes"]);
            var jsonResponse0 = _serializer.Deserialize<List<PublishedNodeApiModel>>(opcNodes0);
            Assert.Equal(jsonResponse0.Count, 125);
            Assert.Equal(jsonResponse0[0].Id, nodes0[0].Id);
            Assert.Equal(jsonResponse0[25].Id, nodes0[25].Id);

            //Create request for GetConfiguredNodesOnEndpoint method call for endpoint 1
            var nodesOnEndpoint1 = new PublishedNodesEntryModel {
                EndpointUrl = request1.EndpointUrl,
            };
            var requestGetConfiguredNodesOnEndpoint1 = nodesOnEndpoint1.ToApiModel();

            //Call GetConfiguredNodesOnEndpoint direct method for endpoint 1
            var responseGetConfiguredNodesOnEndpoint1 = await CallMethodAsync(
                new MethodParameterModel {
                    Name = TestConstants.DirectMethodLegacyNames.GetConfiguredNodesOnEndpoint,
                    JsonPayload = _serializer.SerializeToString(requestGetConfiguredNodesOnEndpoint1)
                },
                cts.Token
            ).ConfigureAwait(false);

            Assert.Equal((int)HttpStatusCode.OK, responseGetConfiguredNodesOnEndpoint1.Status);

            var obj1 = JObject.Parse(responseGetConfiguredNodesOnEndpoint1.JsonPayload);
            var opcNodes1 = _serializer.SerializeToString(obj1["OpcNodes"]);
            var jsonResponse1 = _serializer.Deserialize<List<PublishedNodeApiModel>>(opcNodes1);
            Assert.Equal(jsonResponse1.Count, 125);
            Assert.Equal(jsonResponse1[0].Id, nodes1[0].Id);
            Assert.Equal(jsonResponse1[25].Id, nodes1[25].Id);

            //Call GetDiagnosticInfo direct method
            var responseGetDiagnosticInfo = await CallMethodAsync(
                new MethodParameterModel {
                    Name = TestConstants.DirectMethodLegacyNames.GetDiagnosticInfo,
                },
                cts.Token
            ).ConfigureAwait(false);

            Assert.Equal((int)HttpStatusCode.OK, responseGetDiagnosticInfo.Status);
            var diagInfo = _serializer.Deserialize<DiagnosticInfoLegacyModel>(responseGetDiagnosticInfo.JsonPayload);
            Assert.Equal(diagInfo.NumberOfOpcSessionsConfigured, 2);
            Assert.Equal(diagInfo.NumberOfOpcSessionsConnected, 2);
            Assert.Equal(diagInfo.NumberOfOpcMonitoredItemsConfigured, 250);
            Assert.Equal(diagInfo.NumberOfOpcMonitoredItemsMonitored, 250);
            Assert.True(diagInfo.SentMessages > 0);
            Assert.Equal(diagInfo.FailedMessages, 0);
            Assert.Equal(diagInfo.TooLargeCount, 0);

            // Stop monitoring and get the result.
            var publishingMonitoringResultJson = await TestHelper.StopMonitoringIncomingMessagesAsync(_context, cts.Token).ConfigureAwait(false);
            Assert.True(publishingMonitoringResultJson.TotalValueChangesCount > 0, "No messages received at IoT Hub");

            // Check that every published node is sending data.
            if (_context.ConsumedOpcUaNodes != null) {
                var expectedNodes = new HashSet<string>();
                foreach (var consumedNodes in _context.ConsumedOpcUaNodes) {
                    foreach (var opcNode in consumedNodes.Value.OpcNodes) {
                        expectedNodes.Add(opcNode.Id);
                    }
                }

                foreach (var property in publishingMonitoringResultJson.ValueChangesByNodeId) {
                    var propertyName = property.Key;
                    var nodeId = propertyName.Split('#').Last();
                    var expected = expectedNodes.FirstOrDefault(n => n.EndsWith(nodeId));
                    Assert.True(expected != null, $"Publishing from unexpected node: {propertyName}");
                    expectedNodes.Remove(expected);
                }

                foreach (var expectedNode in expectedNodes) {
                    _context.OutputHelper.WriteLine(expectedNode);
                }
                Assert.Empty(expectedNodes);
            }

            //Call Unpublish direct method
            response = await CallMethodAsync(
                new MethodParameterModel {
                    Name = TestConstants.DirectMethodLegacyNames.UnpublishNodes,
                    JsonPayload = _serializer.SerializeToString(request0)
                },
                cts.Token
            ).ConfigureAwait(false);

            Assert.Equal((int)HttpStatusCode.OK, response.Status);

            //Call GetDiagnosticInfo direct method
            responseGetDiagnosticInfo = await CallMethodAsync(
                new MethodParameterModel {
                    Name = TestConstants.DirectMethodLegacyNames.GetDiagnosticInfo,
                },
                cts.Token
            ).ConfigureAwait(false);

            Assert.Equal((int)HttpStatusCode.OK, responseGetDiagnosticInfo.Status);
            diagInfo = _serializer.Deserialize<DiagnosticInfoLegacyModel>(responseGetDiagnosticInfo.JsonPayload);
            Assert.Equal(diagInfo.NumberOfOpcSessionsConfigured, 1);

            //Call UnpublishAll direct method
            request1.OpcNodes?.Clear();
            response = await CallMethodAsync(
                new MethodParameterModel {
                    Name = TestConstants.DirectMethodLegacyNames.UnpublishAllNodes,
                    JsonPayload = _serializer.SerializeToString(request1)
                },
                cts.Token
            ).ConfigureAwait(false);

            Assert.Equal((int)HttpStatusCode.OK, response.Status);

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
            Assert.Equal(configuredEndpointsResponse.Count, 0);

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
            var unpublishingMonitoringResultJson = await TestHelper.StopMonitoringIncomingMessagesAsync(_context, cts.Token).ConfigureAwait(false);
            Assert.True(unpublishingMonitoringResultJson.TotalValueChangesCount == 0,
                $"Messages received at IoT Hub: {unpublishingMonitoringResultJson.TotalValueChangesCount}");
        }
    }
}

