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
    public class BPublishMultipleNodesStandaloneDirectMethodTestTheory : DirectMethodTestBase
    {
        private readonly CancellationTokenSource _cts;

        public BPublishMultipleNodesStandaloneDirectMethodTestTheory(
            ITestOutputHelper output,
            IIoTMultipleNodesTestContext context
        ) : base(output, context)
        {
            _cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);

            // Clean publishednodes.json.
            TestHelper.CleanPublishedNodesJsonFilesAsync(_context).Wait();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cts.Dispose();
            }
            base.Dispose(disposing);
        }

        [Theory]
        [InlineData(MessagingMode.Samples, false)]
        [InlineData(MessagingMode.Samples, true)]
        [InlineData(MessagingMode.PubSub, false)]
        [InlineData(MessagingMode.PubSub, true)]
        public async Task SubscribeUnsubscribeDirectMethodTest(MessagingMode messagingMode, bool useAddOrUpdate)
        {
            // Clear context.
            _context.Reset();
            _iotHubPublisherModuleName = await _context.RegistryHelper.DeployStandalonePublisherAsync(messagingMode, ct: _cts.Token);

            // Call GetConfiguredEndpoints direct method, initially there should be no endpoints
            var responseGetConfiguredEndpoints = await CallMethodAsync(
                new MethodParameterModel
                {
                    Name = TestConstants.DirectMethodNames.GetConfiguredEndpoints
                },
                _cts.Token
            );

            Assert.Equal((int)HttpStatusCode.OK, responseGetConfiguredEndpoints.Status);
            var configuredEndpointsResponse = _serializer.Deserialize<GetConfiguredEndpointsResponseModel>(responseGetConfiguredEndpoints.JsonPayload);
            Assert.Empty(configuredEndpointsResponse.Endpoints);

            const int numberOfNodes = 250;

            var request = await TestHelper
                .CreateMultipleNodesModelAsync(_context, _cts.Token, numberOfNodes: numberOfNodes)
;
            MethodResultModel response = null;

            // Publish nodes for the endpoint
            if (useAddOrUpdate)
            {
                // Call AddOrUpdateEndpoints direct method
                response = await CallMethodAsync(
                    new MethodParameterModel
                    {
                        Name = TestConstants.DirectMethodNames.AddOrUpdateEndpoints,
                        JsonPayload = _serializer.SerializeToString(new List<PublishedNodesEntryModel> { request })
                    },
                    _cts.Token
                );
            }
            else
            {
                // Call PublishNodes direct method
                response = await CallMethodAsync(
                    new MethodParameterModel
                    {
                        Name = TestConstants.DirectMethodNames.PublishNodes,
                        JsonPayload = _serializer.SerializeToString(request)
                    },
                    _cts.Token
                );
            }

            Assert.Equal((int)HttpStatusCode.OK, response.Status);

            // Use test event processor to verify data send to IoT Hub (expected* set to zero
            // as data gap analysis is not part of this test case)
            using (var validator = TelemetryValidator.Start(_context, numberOfNodes, 0, 90_000_000))
            {
                // Wait some time to generate events to process.
                await Task.Delay(TestConstants.AwaitDataInMilliseconds, _cts.Token);

                // Call GetConfiguredEndpoints direct method
                responseGetConfiguredEndpoints = await CallMethodAsync(
                    new MethodParameterModel
                    {
                        Name = TestConstants.DirectMethodNames.GetConfiguredEndpoints
                    },
                    _cts.Token
                );

                Assert.Equal((int)HttpStatusCode.OK, responseGetConfiguredEndpoints.Status);
                configuredEndpointsResponse = _serializer.Deserialize<GetConfiguredEndpointsResponseModel>(responseGetConfiguredEndpoints.JsonPayload);
                Assert.Single(configuredEndpointsResponse.Endpoints);
                TestHelper.Publisher.AssertEndpointModel(configuredEndpointsResponse.Endpoints[0], request);

                // Create request for GetConfiguredNodesOnEndpoint method call
                var requestGetConfiguredNodesOnEndpoint = new PublishedNodesEntryModel
                {
                    EndpointUrl = request.EndpointUrl,
                    UseSecurity = request.UseSecurity
                };

                // Call GetConfiguredNodesOnEndpoint direct method
                var responseGetConfiguredNodesOnEndpoint = await CallMethodAsync(
                    new MethodParameterModel
                    {
                        Name = TestConstants.DirectMethodNames.GetConfiguredNodesOnEndpoint,
                        JsonPayload = _serializer.SerializeObjectToString(requestGetConfiguredNodesOnEndpoint)
                    },
                    _cts.Token
                );

                Assert.Equal((int)HttpStatusCode.OK, responseGetConfiguredNodesOnEndpoint.Status);
                var jsonResponse = _serializer.Deserialize<GetConfiguredNodesOnEndpointResponseModel>(responseGetConfiguredNodesOnEndpoint.JsonPayload);
                Assert.Equal(numberOfNodes, jsonResponse.OpcNodes.Count);

                // Call GetDiagnosticInfo direct method
                var responseGetDiagnosticInfo = await CallMethodAsync(
                    new MethodParameterModel
                    {
                        Name = TestConstants.DirectMethodNames.GetDiagnosticInfo
                    },
                    _cts.Token
                );

                Assert.Equal((int)HttpStatusCode.OK, responseGetDiagnosticInfo.Status);
                var diagInfoList = _serializer.Deserialize<List<PublishDiagnosticInfoModel>>(responseGetDiagnosticInfo.JsonPayload);
                Assert.Single(diagInfoList);

                TestHelper.Publisher.AssertEndpointDiagnosticInfoModel(request.YieldReturn(), diagInfoList[0]);

                // Stop monitoring and get the result.
                var result = await validator.StopAsync();
                Assert.True(result.TotalValueChangesCount > 0, "No messages received at IoT Hub");
                Assert.Equal(result.ValueChangesByNodeId.Count, request.OpcNodes.Count);
                Assert.True(result.DroppedValueCount == 0,
                    $"Dropped messages detected: {result.DroppedValueCount}");
                Assert.True(result.DuplicateValueCount == 0,
                    $"Duplicate values detected: {result.DuplicateValueCount}");
                Assert.True(result.DroppedSequenceCount == 0,
                    $"Dropped Sequence detected: {result.DroppedSequenceCount}");
                Assert.Equal(0U, result.DuplicateSequenceCount);
                Assert.Equal(0U, result.ResetSequenceCount);

                // Check that every published node is sending data.
                if (_context.ConsumedOpcUaNodes != null)
                {
                    var expectedNodes = _context.ConsumedOpcUaNodes.First().Value.OpcNodes.Select(n => n.Id).ToList();
                    foreach (var property in result.ValueChangesByNodeId)
                    {
                        var propertyName = property.Key;
                        var nodeId = propertyName.Split('#').Last();
                        var expected = expectedNodes.Find(n => n.EndsWith(nodeId, StringComparison.Ordinal));
                        Assert.True(expected != null, $"Publishing from unexpected node: {propertyName}");
                        expectedNodes.Remove(expected);
                    }

                    expectedNodes.ForEach(_context.OutputHelper.WriteLine);
                    Assert.Empty(expectedNodes);
                }

                // Unpublish all nodes for the endpoint
                if (useAddOrUpdate)
                {
                    // Call AddOrUpdateEndpoints direct method
                    request.OpcNodes = null;
                    response = await CallMethodAsync(
                        new MethodParameterModel
                        {
                            Name = TestConstants.DirectMethodNames.AddOrUpdateEndpoints,
                            JsonPayload = _serializer.SerializeToString(new List<PublishedNodesEntryModel> { request })
                        },
                        _cts.Token
                    );
                }
                else
                {
                    // Call UnPublishNodes direct method
                    response = await CallMethodAsync(
                        new MethodParameterModel
                        {
                            Name = TestConstants.DirectMethodNames.UnpublishNodes,
                            JsonPayload = _serializer.SerializeToString(request)
                        },
                        _cts.Token
                    );
                }

                Assert.Equal((int)HttpStatusCode.OK, response.Status);

                // Wait till the publishing has stopped.
                await Task.Delay(TestConstants.AwaitCleanupInMilliseconds, _cts.Token);

                // Call GetDiagnosticInfo direct method
                responseGetDiagnosticInfo = await CallMethodAsync(
                    new MethodParameterModel
                    {
                        Name = TestConstants.DirectMethodNames.GetDiagnosticInfo
                    },
                    _cts.Token
                );

                Assert.Equal((int)HttpStatusCode.OK, responseGetDiagnosticInfo.Status);
                diagInfoList = _serializer.Deserialize<List<PublishDiagnosticInfoModel>>(responseGetDiagnosticInfo.JsonPayload);
                Assert.Empty(diagInfoList);
            }

            // Use test event processor to verify data send to IoT Hub (expected* set to zero
            // as data gap analysis is not part of this test case)
            using (var validator = TelemetryValidator.Start(_context, 0, 0, 0))
            {
                // Wait some time to generate events to process.
                await Task.Delay(TestConstants.AwaitNoDataInMilliseconds, _cts.Token);

                // Stop monitoring and get the result.
                var result = await validator.StopAsync();
                Assert.True(result.TotalValueChangesCount == 0,
                    $"Messages received at IoT Hub: {result.TotalValueChangesCount}");
            }
        }
    }
}
