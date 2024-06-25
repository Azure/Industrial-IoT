// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatformE2ETests.Standalone
{
    using IIoTPlatformE2ETests.Deploy;
    using IIoTPlatformE2ETests.TestEventProcessor;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Furly.Extensions.Serializers;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using TestExtensions;
    using Xunit;
    using Xunit.Abstractions;

    /// <summary>
    /// The test theory using different (ordered) test cases to go thru all required steps of publishing OPC UA node
    /// </summary>
    [TestCaseOrderer(TestCaseOrderer.FullName, TestConstants.TestAssemblyName)]
    [Collection("IIoT Standalone Direct Methods Test Collection")]
    [Trait(TestConstants.TraitConstants.PublisherModeTraitName, TestConstants.TraitConstants.PublisherModeTraitValue)]
    public class CPublishMultipleEndpointsStandaloneDirectMethodTestTheory : DirectMethodTestBase
    {
        public CPublishMultipleEndpointsStandaloneDirectMethodTestTheory(
            ITestOutputHelper output,
            IIoTMultipleNodesTestContext context
        ) : base(output, context) { }

        [Theory]
        [InlineData(MessagingMode.Samples, false)]
        [InlineData(MessagingMode.Samples, true)]
        [InlineData(MessagingMode.PubSub, false)]
        [InlineData(MessagingMode.PubSub, true)]
        public async Task SubscribeUnsubscribeDirectMethodTest(MessagingMode messagingMode, bool useAddOrUpdate)
        {
            // Clear context.
            _context.Reset();

            using var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);
            _iotHubPublisherModuleName = await _context.RegistryHelper.DeployStandalonePublisherAsync(messagingMode, ct: cts.Token);

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

            var nodesToPublish0 = await TestHelper.CreateMultipleNodesModelAsync(_context, cts.Token, 2, 250);
            var fastNodes0 = nodesToPublish0.OpcNodes.Where(node => node.Id.Contains("fast", StringComparison.OrdinalIgnoreCase)).ToList();
            var slowNodes0 = nodesToPublish0.OpcNodes.Where(node => node.Id.Contains("slow", StringComparison.OrdinalIgnoreCase)).ToList();
            var nodes0 = new List<OpcNodeModel>();
            nodes0.AddRange(fastNodes0.Take(25));
            nodes0.AddRange(slowNodes0.Take(100));
            nodesToPublish0.OpcNodes = nodes0.ToList();

            var request0 = nodesToPublish0;
            MethodResultModel response = null;

            // Publish nodes for endpoint 0
            if (useAddOrUpdate)
            {
                // Call AddOrUpdateEndpoints direct method
                response = await CallMethodAsync(
                    new MethodParameterModel
                    {
                        Name = TestConstants.DirectMethodNames.AddOrUpdateEndpoints,
                        JsonPayload = _serializer.SerializeToString(new List<PublishedNodesEntryModel> { request0 })
                    },
                    cts.Token
                );
            }
            else
            {
                // Call PublishNodes direct method
                response = await CallMethodAsync(
                    new MethodParameterModel
                    {
                        Name = TestConstants.DirectMethodNames.PublishNodes,
                        JsonPayload = _serializer.SerializeToString(request0)
                    },
                    cts.Token
                );
            }

            Assert.Equal((int)HttpStatusCode.OK, response.Status);

            // Publish nodes on a different enpoint
            var nodesToPublish1 = await TestHelper.CreateMultipleNodesModelAsync(_context, cts.Token, 5, 250);
            var fastNodes1 = nodesToPublish1.OpcNodes.Where(node => node.Id.Contains("fast", StringComparison.OrdinalIgnoreCase)).ToList();
            var slowNodes1 = nodesToPublish1.OpcNodes.Where(node => node.Id.Contains("slow", StringComparison.OrdinalIgnoreCase)).ToList();
            var nodes1 = new List<OpcNodeModel>();
            nodes1.AddRange(fastNodes1.Skip(25).Take(25));
            nodes1.AddRange(slowNodes1.Skip(100).Take(100));
            nodesToPublish1.OpcNodes = nodes1.ToList();

            var request1 = nodesToPublish1;

            // Publish nodes for endpoint 1
            if (useAddOrUpdate)
            {
                // Call AddOrUpdateEndpoints direct method
                response = await CallMethodAsync(
                    new MethodParameterModel
                    {
                        Name = TestConstants.DirectMethodNames.AddOrUpdateEndpoints,
                        JsonPayload = _serializer.SerializeToString(new List<PublishedNodesEntryModel> { request1 })
                    },
                    cts.Token
                );
            }
            else
            {
                // Call PublishNodes direct method
                response = await CallMethodAsync(
                    new MethodParameterModel
                    {
                        Name = TestConstants.DirectMethodNames.PublishNodes,
                        JsonPayload = _serializer.SerializeToString(request1)
                    },
                    cts.Token
                );
            }

            Assert.Equal((int)HttpStatusCode.OK, response.Status);

            // Use test event processor to verify data send to IoT Hub (expected* set to zero
            // as data gap analysis is not part of this test case)
            using (var validator = TelemetryValidator.Start(_context, 500, 10_000, 90_000_000))
            {
                // Wait some time to generate events to process.
                await Task.Delay(TestConstants.AwaitDataInMilliseconds, cts.Token);

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
                Assert.Equal(2, configuredEndpointsResponse.Endpoints.Count);
                TestHelper.Publisher.AssertEndpointModel(configuredEndpointsResponse.Endpoints[0], request0);
                TestHelper.Publisher.AssertEndpointModel(configuredEndpointsResponse.Endpoints[1], request1);

                // Create request for GetConfiguredNodesOnEndpoint method call for endpoint 0
                var requestGetConfiguredNodesOnEndpoint0 = new PublishedNodesEntryModel
                {
                    EndpointUrl = request0.EndpointUrl,
                    UseSecurity = request0.UseSecurity
                };

                // Call GetConfiguredNodesOnEndpoint direct method for endpoint 0
                var responseGetConfiguredNodesOnEndpoint0 = await CallMethodAsync(
                    new MethodParameterModel
                    {
                        Name = TestConstants.DirectMethodNames.GetConfiguredNodesOnEndpoint,
                        JsonPayload = _serializer.SerializeToString(requestGetConfiguredNodesOnEndpoint0)
                    },
                    cts.Token
                );

                Assert.Equal((int)HttpStatusCode.OK, responseGetConfiguredNodesOnEndpoint0.Status);
                var jsonResponse0 = _serializer.Deserialize<GetConfiguredNodesOnEndpointResponseModel>(responseGetConfiguredNodesOnEndpoint0.JsonPayload);
                Assert.Equal(125, jsonResponse0.OpcNodes.Count);
                Assert.Equal(nodes0[0].Id, jsonResponse0.OpcNodes[0].Id);
                Assert.Equal(nodes0[25].Id, jsonResponse0.OpcNodes[25].Id);

                // Create request for GetConfiguredNodesOnEndpoint method call for endpoint 1
                var requestGetConfiguredNodesOnEndpoint1 = new PublishedNodesEntryModel
                {
                    EndpointUrl = request1.EndpointUrl,
                    UseSecurity = request1.UseSecurity
                };

                // Call GetConfiguredNodesOnEndpoint direct method for endpoint 1
                var responseGetConfiguredNodesOnEndpoint1 = await CallMethodAsync(
                    new MethodParameterModel
                    {
                        Name = TestConstants.DirectMethodNames.GetConfiguredNodesOnEndpoint,
                        JsonPayload = _serializer.SerializeToString(requestGetConfiguredNodesOnEndpoint1)
                    },
                    cts.Token
                );

                Assert.Equal((int)HttpStatusCode.OK, responseGetConfiguredNodesOnEndpoint1.Status);
                var jsonResponse1 = _serializer.Deserialize<GetConfiguredNodesOnEndpointResponseModel>(responseGetConfiguredNodesOnEndpoint1.JsonPayload);
                Assert.Equal(125, jsonResponse1.OpcNodes.Count);
                Assert.Equal(nodes1[0].Id, jsonResponse1.OpcNodes[0].Id);
                Assert.Equal(nodes1[25].Id, jsonResponse1.OpcNodes[25].Id);

                // Call GetDiagnosticInfo direct method
                var responseGetDiagnosticInfo = await CallMethodAsync(
                    new MethodParameterModel
                    {
                        Name = TestConstants.DirectMethodNames.GetDiagnosticInfo
                    },
                    cts.Token
                );

                Assert.Equal((int)HttpStatusCode.OK, responseGetDiagnosticInfo.Status);
                var diagInfoList = _serializer.Deserialize<List<PublishDiagnosticInfoModel>>(responseGetDiagnosticInfo.JsonPayload);

                var diagInfo = Assert.Single(diagInfoList);
                TestHelper.Publisher.AssertEndpointDiagnosticInfoModel(request0.YieldReturn().Append(request1), diagInfo);

                // Stop monitoring and get the result.
                var result = await validator.StopAsync();
                Assert.True(result.TotalValueChangesCount > 0, "No messages received at IoT Hub");
                // We cannot perform sequence number checks in multi-endpoint tests as we the values will be unique only per endpoint.
                // ToDo: Add sequence number checks once we support multi-endpoint validation.

                // Check that every published node is sending data.
                if (_context.ConsumedOpcUaNodes != null)
                {
                    var expectedNodes = new HashSet<string>();
                    foreach (var consumedNodes in _context.ConsumedOpcUaNodes)
                    {
                        foreach (var opcNode in consumedNodes.Value.OpcNodes)
                        {
                            expectedNodes.Add(opcNode.Id);
                        }
                    }

                    foreach (var property in result.ValueChangesByNodeId)
                    {
                        var propertyName = property.Key;
                        var nodeId = propertyName.Split('#').Last();
                        var expected = expectedNodes.FirstOrDefault(n => n.EndsWith(nodeId, StringComparison.Ordinal));
                        Assert.True(expected != null, $"Publishing from unexpected node: {propertyName}");
                        expectedNodes.Remove(expected);
                    }

                    foreach (var expectedNode in expectedNodes)
                    {
                        _context.OutputHelper.WriteLine(expectedNode);
                    }
                    Assert.Empty(expectedNodes);
                }

                // Unpublish all nodes for endpoint 0
                if (useAddOrUpdate)
                {
                    // Call AddOrUpdateEndpoints direct method
                    request0.OpcNodes?.Clear();
                    response = await CallMethodAsync(
                        new MethodParameterModel
                        {
                            Name = TestConstants.DirectMethodNames.AddOrUpdateEndpoints,
                            JsonPayload = _serializer.SerializeToString(new List<PublishedNodesEntryModel> { request0 })
                        },
                        cts.Token
                    );
                }
                else
                {
                    // Call UnpublishNodes direct method
                    response = await CallMethodAsync(
                        new MethodParameterModel
                        {
                            Name = TestConstants.DirectMethodNames.UnpublishNodes,
                            JsonPayload = _serializer.SerializeToString(request0)
                        },
                        cts.Token
                    );
                }

                Assert.Equal((int)HttpStatusCode.OK, response.Status);

                // Call GetDiagnosticInfo direct method
                responseGetDiagnosticInfo = await CallMethodAsync(
                    new MethodParameterModel
                    {
                        Name = TestConstants.DirectMethodNames.GetDiagnosticInfo
                    },
                    cts.Token
                );

                Assert.Equal((int)HttpStatusCode.OK, responseGetDiagnosticInfo.Status);
                diagInfoList = _serializer.Deserialize<List<PublishDiagnosticInfoModel>>(responseGetDiagnosticInfo.JsonPayload);
                Assert.Single(diagInfoList);

                // Unpublish all nodes for endpoint 1
                request1.OpcNodes?.Clear();
                if (useAddOrUpdate)
                {
                    // Call AddOrUpdateEndpoints direct method
                    response = await CallMethodAsync(
                        new MethodParameterModel
                        {
                            Name = TestConstants.DirectMethodNames.AddOrUpdateEndpoints,
                            JsonPayload = _serializer.SerializeToString(new List<PublishedNodesEntryModel> { request1 })
                        },
                        cts.Token
                    );
                }
                else
                {
                    // Call UnpublishAllNodes direct method
                    response = await CallMethodAsync(
                        new MethodParameterModel
                        {
                            Name = TestConstants.DirectMethodNames.UnpublishAllNodes,
                            JsonPayload = _serializer.SerializeToString(request1)
                        },
                        cts.Token
                    );
                }

                Assert.Equal((int)HttpStatusCode.OK, response.Status);

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
                Assert.Empty(configuredEndpointsResponse.Endpoints);

                // Call GetDiagnosticInfo direct method
                responseGetDiagnosticInfo = await CallMethodAsync(
                    new MethodParameterModel
                    {
                        Name = TestConstants.DirectMethodNames.GetDiagnosticInfo
                    },
                    cts.Token
                );

                Assert.Equal((int)HttpStatusCode.OK, responseGetDiagnosticInfo.Status);
                diagInfoList = _serializer.Deserialize<List<PublishDiagnosticInfoModel>>(responseGetDiagnosticInfo.JsonPayload);
                Assert.Empty(diagInfoList);
            }

            // Wait till the publishing has stopped.
            await Task.Delay(TestConstants.AwaitCleanupInMilliseconds, cts.Token);

            // Use test event processor to verify data send to IoT Hub (expected* set to zero
            // as data gap analysis is not part of this test case)
            using (var validator = TelemetryValidator.Start(_context, 0, 0, 0))
            {
                // Wait some time to generate events to process.
                await Task.Delay(TestConstants.AwaitNoDataInMilliseconds, cts.Token);

                // Stop monitoring and get the result.
                var result = await validator.StopAsync();
                Assert.True(result.TotalValueChangesCount == 0,
                    $"Messages received at IoT Hub: {result.TotalValueChangesCount}");
            }
        }

        [Theory]
        [InlineData(MessagingMode.Samples)]
        [InlineData(MessagingMode.PubSub)]
        public async Task AddOrUpdateEndpointsTest(MessagingMode messagingMode)
        {
            // Clear context.
            _context.Reset();

            using var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);
            _iotHubPublisherModuleName = await _context.RegistryHelper.DeployStandalonePublisherAsync(messagingMode, ct: cts.Token);

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

            _context.OutputHelper.WriteLine("OPC Publisher module is up and running.");
            await _context.LoadSimulatedPublishedNodesAsync(cts.Token);
            var endpointsCount = _context.SimulatedPublishedNodes?.Count ?? 0;
            Assert.True(endpointsCount > 0, "no endpoints found to generate requests");

            var fullNodes = new List<PublishedNodesEntryModel>();
            for (var index = 0; index < endpointsCount; ++index)
            {
                var node = await TestHelper.CreateMultipleNodesModelAsync(_context, cts.Token, index, 250);
                node.OpcNodes = node.OpcNodes
                    .Where(node => node.Id.Contains("slow", StringComparison.OrdinalIgnoreCase))
                    .Skip(index * endpointsCount)
                    .Take(endpointsCount)
                    .ToList();

                fullNodes.Add(node);
            }

            var currentNodes = new List<PublishedNodesEntryModel>();
            for (var index = 0; index < endpointsCount; ++index)
            {
                var node = await TestHelper.CreateMultipleNodesModelAsync(_context, cts.Token, index, 0);
                currentNodes.Add(node);
            }

            // Use test event processor to verify data send to IoT Hub (expected* set to zero
            // as data gap analysis is not part of this test case)
            using (var validator = TelemetryValidator.Start(_context, 0, 10_000, 20_000))
            {
                for (var index = 0; index < endpointsCount; ++index)
                {
                    var request = new List<PublishedNodesEntryModel>();
                    for (var i = 0; i <= index; ++i)
                    {
                        currentNodes[i].OpcNodes = fullNodes[i].OpcNodes.Take(index + 1).ToList();
                        request.Add(currentNodes[i]);
                    }

                    // Call AddOrUpdateEndpoints direct method
                    // This will exercise incremental updates to the subscriptions.
                    var response = await CallMethodAsync(
                        new MethodParameterModel
                        {
                            Name = TestConstants.DirectMethodNames.AddOrUpdateEndpoints,
                            JsonPayload = _serializer.SerializeToString(request)
                        },
                        cts.Token
                    );

                    Assert.Equal((int)HttpStatusCode.OK, response.Status);

                    await Task.Delay(2_000);
                }

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
                Assert.Equal(endpointsCount, configuredEndpointsResponse.Endpoints.Count);

                // Check that all endpoints are present.
                for (var index = 0; index < endpointsCount; ++index)
                {
                    var receivedEndpointUrl = configuredEndpointsResponse.Endpoints[index].EndpointUrl;
                    Assert.NotNull(currentNodes.Find(endpoint => endpoint.EndpointUrl.Equals(receivedEndpointUrl, StringComparison.Ordinal)));
                }

                // Check that all expected nodes on endpoints are present.
                for (var index = 0; index < endpointsCount; ++index)
                {
                    currentNodes[index].OpcNodes = new List<OpcNodeModel>();

                    // Call GetConfiguredNodesOnEndpoint direct method for endpoint 0
                    var responseGetConfiguredNodesOnEndpoint = await CallMethodAsync(
                        new MethodParameterModel
                        {
                            Name = TestConstants.DirectMethodNames.GetConfiguredNodesOnEndpoint,
                            JsonPayload = _serializer.SerializeToString(currentNodes[index])
                        },
                        cts.Token
                    );

                    Assert.Equal((int)HttpStatusCode.OK, responseGetConfiguredNodesOnEndpoint.Status);
                    var receivedNodes = _serializer.Deserialize<GetConfiguredNodesOnEndpointResponseModel>(responseGetConfiguredNodesOnEndpoint.JsonPayload);
                    Assert.Equal(fullNodes[index].OpcNodes.Count, receivedNodes.OpcNodes.Count);
                    Assert.Equal(fullNodes[index].OpcNodes[0].Id, receivedNodes.OpcNodes[0].Id);
                    Assert.Equal(fullNodes[index].OpcNodes[endpointsCount - 1].Id, receivedNodes.OpcNodes[endpointsCount - 1].Id);
                }

                // Wait some time to generate events to process.
                await Task.Delay(TestConstants.AwaitDataInMilliseconds, cts.Token);

                // Stop monitoring and get the result.
                var result = await validator.StopAsync();

                Assert.True(result.TotalValueChangesCount > 0, "No messages received at IoT Hub");
                Assert.True(result.DroppedValueCount == 0,
                    $"Dropped messages detected: {result.DroppedValueCount}");
                Assert.True(result.DuplicateValueCount == 0,
                    $"Duplicate values detected: {result.DuplicateValueCount}");
                Assert.Equal(endpointsCount * endpointsCount, result.ValueChangesByNodeId.Count);
                // We cannot perform sequence number checks in multi-endpoint tests as we the values will be unique only per endpoint.
            }

            await Task.Delay(TestConstants.DefaultDelayMilliseconds, cts.Token);

            // Use test event processor to verify data send to IoT Hub (expected* set to zero
            // as data gap analysis is not part of this test case).
            // Below we are going to remove endpoints one-by-one. This operation will exercise incremental
            // updates to the subscriptions which should also not yield dropped or duplicated messages.
            using (var validator = TelemetryValidator.Start(_context, 0, 10_000, 20_000))
            {
                // Call GetDiagnosticInfo direct method and validate that we have data for all endpoints.
                var diagInfoListResponse = await CallMethodAsync(
                    new MethodParameterModel
                    {
                        Name = TestConstants.DirectMethodNames.GetDiagnosticInfo
                    },
                    cts.Token
                );

                Assert.Equal((int)HttpStatusCode.OK, diagInfoListResponse.Status);
                var diagInfoList = _serializer.Deserialize<List<PublishDiagnosticInfoModel>>(diagInfoListResponse.JsonPayload);
                var diagInfo = Assert.Single(diagInfoList);

                TestHelper.Publisher.AssertEndpointDiagnosticInfoModel(fullNodes, diagInfo);
                Assert.True(diagInfo.IngressValueChanges > 0);
                Assert.True(diagInfo.IngressDataChanges > 0);
                Assert.Equal(0, diagInfo.MonitoredOpcNodesFailedCount);
                Assert.Equal(endpointsCount * endpointsCount, diagInfo.MonitoredOpcNodesSucceededCount);
                Assert.True(diagInfo.OpcEndpointConnected);
                Assert.True(diagInfo.OutgressIoTMessageCount > 0);
                Assert.Equal(0U, diagInfo.EncoderNotificationsDropped);
                Assert.Equal(0L, diagInfo.OutgressInputBufferDropped);

                // This will keep track of currently published nodes.
                for (var index = 0; index < endpointsCount; ++index)
                {
                    currentNodes[index].OpcNodes = fullNodes[index].OpcNodes;
                }

                // Now let's unpublish nodes on all endpoints.
                for (var index = 0; index < endpointsCount; ++index)
                {
                    switch (index % 3)
                    {
                        case 0:
                            // Let's unpublish using UnpublishNodes
                            var unpublishNodesResponse = await CallMethodAsync(
                                new MethodParameterModel
                                {
                                    Name = TestConstants.DirectMethodNames.UnpublishNodes,
                                    JsonPayload = _serializer.SerializeToString(currentNodes[index])
                                },
                                cts.Token
                            );

                            Assert.Equal((int)HttpStatusCode.OK, unpublishNodesResponse.Status);

                            currentNodes[index].OpcNodes = new List<OpcNodeModel>();

                            break;
                        case 1:
                            // Let's unpublish using UnpublishAllNodes
                            currentNodes[index].OpcNodes = new List<OpcNodeModel>();

                            var unpublishAllNodesResponse = await CallMethodAsync(
                                new MethodParameterModel
                                {
                                    Name = TestConstants.DirectMethodNames.UnpublishAllNodes,
                                    JsonPayload = _serializer.SerializeToString(currentNodes[index])
                                },
                                cts.Token
                            );

                            Assert.Equal((int)HttpStatusCode.OK, unpublishAllNodesResponse.Status);

                            break;
                        case 2:
                            var request = new List<PublishedNodesEntryModel>();

                            // Removing endpoint at index
                            currentNodes[index].OpcNodes = new List<OpcNodeModel>();
                            request.Add(currentNodes[index]);

                            for (var i = index + 1; i < endpointsCount; ++i)
                            {
                                currentNodes[i].OpcNodes = fullNodes[i].OpcNodes.Take(endpointsCount - index - 1).ToList();
                                request.Add(currentNodes[i]);
                            }

                            // Let's unpublish using AddOrUpdateEndpoints
                            // This will exercise incremental updates to the subscriptions.
                            var addOrUpdateEndpointsResponse = await CallMethodAsync(
                                new MethodParameterModel
                                {
                                    Name = TestConstants.DirectMethodNames.AddOrUpdateEndpoints,
                                    JsonPayload = _serializer.SerializeToString(request)
                                },
                                cts.Token
                            );

                            Assert.Equal((int)HttpStatusCode.OK, addOrUpdateEndpointsResponse.Status);

                            break;
                    }

                    // Check that there is one less entry in diagnostic info
                    diagInfoListResponse = await CallMethodAsync(
                        new MethodParameterModel
                        {
                            Name = TestConstants.DirectMethodNames.GetDiagnosticInfo
                        },
                        cts.Token
                    );

                    Assert.Equal((int)HttpStatusCode.OK, diagInfoListResponse.Status);
                    diagInfoList = _serializer.Deserialize<List<PublishDiagnosticInfoModel>>(diagInfoListResponse.JsonPayload);
                    if (endpointsCount - 1 > index)
                    {
                        var diagInfo2 = Assert.Single(diagInfoList);
                        Assert.Equal(endpointsCount - 1 - index, diagInfo2.Endpoints.Count);
                    }
                    else
                    {
                        Assert.Empty(diagInfoList);
                    }

                    // Check that there is one less entry in endpoints list
                    responseGetConfiguredEndpoints = await CallMethodAsync(
                        new MethodParameterModel
                        {
                            Name = TestConstants.DirectMethodNames.GetConfiguredEndpoints
                        },
                        cts.Token
                    );

                    Assert.Equal((int)HttpStatusCode.OK, responseGetConfiguredEndpoints.Status);
                    configuredEndpointsResponse = _serializer.Deserialize<GetConfiguredEndpointsResponseModel>(
                        responseGetConfiguredEndpoints.JsonPayload);
                    Assert.Equal(endpointsCount - 1 - index, configuredEndpointsResponse.Endpoints.Count);

                    var removedEndpointUrl = currentNodes[index].EndpointUrl;
                    Assert.Empty(configuredEndpointsResponse.Endpoints
                        .Where(endpoint => endpoint.EndpointUrl.Equals(removedEndpointUrl, StringComparison.Ordinal)));

                    await Task.Delay(2_000);
                }

                // Stop monitoring and get the result.
                var result = await validator.StopAsync();

                Assert.True(result.TotalValueChangesCount > 0, "No messages received at IoT Hub");
                Assert.True(result.DroppedValueCount == 0,
                    $"Dropped messages detected: {result.DroppedValueCount}");
                Assert.True(result.DuplicateValueCount == 0,
                    $"Duplicate values detected: {result.DuplicateValueCount}");
                Assert.True(result.ValueChangesByNodeId.Count > 0, "No messages received at IoT Hub");
                // We cannot perform sequence number checks in multi-endpoint tests as we the values will be unique only per endpoint.
                // ToDo: Add sequence number checks once we support multi-endpoint validation.
            }

            // Wait till the publishing has stopped.
            await Task.Delay(TestConstants.AwaitCleanupInMilliseconds, cts.Token);

            // Now check that no more data is coming.
            using (var validator = TelemetryValidator.Start(_context, 0, 0, 0))
            {
                // Wait some time to generate events to process.
                await Task.Delay(TestConstants.AwaitNoDataInMilliseconds, cts.Token);

                // Stop monitoring and get the result.
                var result = await validator.StopAsync();
                Assert.True(result.TotalValueChangesCount == 0,
                    $"Messages received at IoT Hub: {result.TotalValueChangesCount}");
            }
        }
    }
}
