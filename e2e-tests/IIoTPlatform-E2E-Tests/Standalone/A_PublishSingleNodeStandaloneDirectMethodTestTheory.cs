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
            using var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);
            _iotHubPublisherModuleName = await _context.RegistryHelper.DeployStandalonePublisherAsync(messagingMode, cts.Token);

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
            using (var validator = TelemetryValidator.Start(_context, 0, 0, 0))
            {
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

                TestHelper.Publisher.AssertEndpointDiagnosticInfoModel(expectedModel.YieldReturn(), diagInfo[0]);

                // Stop monitoring and get the result.
                var result = await validator.StopAsync();
                Assert.True(result.TotalValueChangesCount > 0, "No messages received at IoT Hub");
                Assert.True(result.DroppedValueCount == 0,
                    $"Dropped messages detected: {result.DroppedValueCount}");
                Assert.True(result.DuplicateValueCount == 0,
                    $"Duplicate values detected: {result.DuplicateValueCount}");
                Assert.True(result.DroppedSequenceCount == 0,
                    $"Dropped Sequence detected: {result.DroppedSequenceCount}");
                Assert.Equal(0U, result.DuplicateSequenceCount);
                Assert.Equal(0U, result.ResetSequenceCount);

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
            }

            // Use test event processor to verify data send to IoT Hub (expected* set to zero
            // as data gap analysis is not part of this test case).
            using (var validator = TelemetryValidator.Start(_context, 0, 0, 0))
            {
                // Wait some time to generate events to process
                await Task.Delay(TestConstants.AwaitNoDataInMilliseconds, cts.Token);

                // Stop monitoring and get the result.
                var result = await validator.StopAsync();
                Assert.True(result.TotalValueChangesCount == 0,
                    $"Messages received at IoT Hub: {result.TotalValueChangesCount}");
            }
        }

        [Fact]
        public async Task RestartAnnouncementTest()
        {
            using var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);
            _iotHubPublisherModuleName = await _context.RegistryHelper.DeployStandalonePublisherAsync(ct: cts.Token);

            // Start monitoring before restarting the module.
            using var validator = TelemetryValidator.Start(_context, 0, 0, 0);

            // Restart OPC Publisher.
            var moduleRestartResponse = await RestartModuleAsync(_iotHubPublisherModuleName, cts.Token)
;
            Assert.Equal((int)HttpStatusCode.OK, moduleRestartResponse.Status);

            // Wait some time.
            await Task.Delay(TestConstants.AwaitInitInMilliseconds, cts.Token);

            // Stop monitoring and check that restart announcement was received.
            var result = await validator.StopAsync();

            Assert.True(result.RestartAnnouncementReceived);
        }
    }
}
