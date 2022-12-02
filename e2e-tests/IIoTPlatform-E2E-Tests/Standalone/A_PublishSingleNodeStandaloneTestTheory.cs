// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatform_E2E_Tests.Standalone {
    using IIoTPlatform_E2E_Tests.Deploy;
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using TestExtensions;
    using TestModels;
    using Xunit;
    using Xunit.Abstractions;
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models;

    /// <summary>
    /// The test theory using different (ordered) test cases to go thru all required steps of publishing OPC UA node
    /// </summary>
    [TestCaseOrderer(TestCaseOrderer.FullName, TestConstants.TestAssemblyName)]
    [Collection("IIoT Standalone Test Collection")]
    [Trait(TestConstants.TraitConstants.PublisherModeTraitName, TestConstants.TraitConstants.PublisherModeTraitValue)]
    public class A_PublishSingleNodeStandaloneTestTheory {

        private readonly ITestOutputHelper _output;
        private readonly IIoTMultipleNodesTestContext _context;

        public A_PublishSingleNodeStandaloneTestTheory(
            ITestOutputHelper output,
            IIoTMultipleNodesTestContext context
        ) {
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _context.OutputHelper = _output;
        }

        [Theory]
        [InlineData(MessagingMode.Samples)]
        [InlineData(MessagingMode.PubSub)]
        public async Task SubscribeUnsubscribeTest(MessagingMode messagingMode) {
            var ioTHubEdgeBaseDeployment = new IoTHubEdgeBaseDeployment(_context);
            var ioTHubPublisherDeployment = new IoTHubPublisherDeployment(_context, messagingMode);

            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);

            // Make sure that there is no active monitoring.
            await TestHelper.StopMonitoringIncomingMessagesAsync(_context, cts.Token);

            // Clean publishednodes.json.
            await TestHelper.CleanPublishedNodesJsonFilesAsync(_context).ConfigureAwait(false);

            // Create base edge deployment.
            var baseDeploymentResult = await ioTHubEdgeBaseDeployment.CreateOrUpdateLayeredDeploymentAsync(cts.Token);
            Assert.True(baseDeploymentResult, "Failed to create/update new edge base deployment.");
            _output.WriteLine("Created/Updated new edge base deployment.");

            // Create layered edge deployment.
            var layeredDeploymentResult = await ioTHubPublisherDeployment.CreateOrUpdateLayeredDeploymentAsync(cts.Token);
            Assert.True(layeredDeploymentResult, "Failed to create/update layered deployment for publisher module.");
            _output.WriteLine("Created/Updated layered deployment for publisher module.");

            var model = await TestHelper.CreateSingleNodeModelAsync(_context, cts.Token);

            await TestHelper.PublishNodesAsync(
                _context,
                new[] { model }
            ).ConfigureAwait(false);

            await TestHelper.SwitchToStandaloneModeAsync(_context, cts.Token).ConfigureAwait(false);

            // Wait some time till the updated pn.json is reflected.
            await Task.Delay(TestConstants.DefaultTimeoutInMilliseconds);

            // We will wait for module to be deployed.
            await _context.RegistryHelper.WaitForSuccessfulDeploymentAsync(
                ioTHubPublisherDeployment.GetDeploymentConfiguration(),
                cts.Token
            ).ConfigureAwait(false);

            await _context.RegistryHelper.WaitForIIoTModulesConnectedAsync(
                _context.DeviceConfig.DeviceId,
                cts.Token,
                new string[] { ioTHubPublisherDeployment.ModuleName }
            ).ConfigureAwait(false);

            // Use test event processor to verify data send to IoT Hub (expected* set to zero
            // as data gap analysis is not part of this test case).
            await TestHelper.StartMonitoringIncomingMessagesAsync(_context, 0, 0, 0, cts.Token);

            // Wait some time to generate events to process.
            await Task.Delay(TestConstants.DefaultTimeoutInMilliseconds, cts.Token);

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

            // Stop publishing nodes.
            await TestHelper.PublishNodesAsync(
                _context,
                Array.Empty<PublishedNodesEntryModel>()
            ).ConfigureAwait(false);

            // Wait till the publishing has stopped.
            await Task.Delay(TestConstants.DefaultTimeoutInMilliseconds, cts.Token);

            // Use test event processor to verify data send to IoT Hub (expected* set to zero
            // as data gap analysis is not part of this test case).
            await TestHelper.StartMonitoringIncomingMessagesAsync(_context, 0, 0, 0, cts.Token);

            // Wait some time to generate events to process
            await Task.Delay(TestConstants.DefaultTimeoutInMilliseconds, cts.Token);

            // Stop monitoring and get the result.
            var unpublishingMonitoringResultJson = await TestHelper.StopMonitoringIncomingMessagesAsync(_context, cts.Token);
            Assert.True(unpublishingMonitoringResultJson.TotalValueChangesCount == 0,
                $"Messages received at IoT Hub: {unpublishingMonitoringResultJson.TotalValueChangesCount}");

            // Publish node with data change trigger status only
            model = await TestHelper.CreateSingleNodeModelAsync(_context, cts.Token, DataChangeTriggerType.Status);
            await TestHelper.PublishNodesAsync(
                _context,
                new[] { model }
            ).ConfigureAwait(false);

            // Wait till the publishing has stopped.
            await Task.Delay(TestConstants.DefaultTimeoutInMilliseconds, cts.Token);

            // Use test event processor to verify data send to IoT Hub (expected* set to zero
            // as data gap analysis is not part of this test case).
            await TestHelper.StartMonitoringIncomingMessagesAsync(_context, 0, 0, 0, cts.Token);

            // Wait some time to generate events to process
            await Task.Delay(TestConstants.DefaultTimeoutInMilliseconds, cts.Token);

            // Stop monitoring and get the result.
            publishingMonitoringResultJson = await TestHelper.StopMonitoringIncomingMessagesAsync(_context, cts.Token);
            Assert.True(publishingMonitoringResultJson.TotalValueChangesCount == 0,
                $"Messages received at IoT Hub: {publishingMonitoringResultJson.TotalValueChangesCount}");

            // Stop publishing nodes.
            await TestHelper.PublishNodesAsync(
                _context,
                Array.Empty<PublishedNodesEntryModel>()
            ).ConfigureAwait(false);

            // Wait till the publishing has stopped.
            await Task.Delay(TestConstants.DefaultTimeoutInMilliseconds, cts.Token);

            // Publish node with data change trigger status value timestamp
            model = await TestHelper.CreateSingleNodeModelAsync(_context, cts.Token, DataChangeTriggerType.StatusValueTimestamp);
            await TestHelper.PublishNodesAsync(
                _context,
                new[] { model }
            ).ConfigureAwait(false);

            // Wait till the publishing has stopped.
            await Task.Delay(TestConstants.DefaultTimeoutInMilliseconds, cts.Token);

            // Use test event processor to verify data send to IoT Hub (expected* set to zero
            // as data gap analysis is not part of this test case).
            await TestHelper.StartMonitoringIncomingMessagesAsync(_context, 0, 0, 0, cts.Token);

            // Wait some time to generate events to process
            await Task.Delay(TestConstants.DefaultTimeoutInMilliseconds, cts.Token);

            // Stop monitoring and get the result.
            publishingMonitoringResultJson = await TestHelper.StopMonitoringIncomingMessagesAsync(_context, cts.Token);
            Assert.True(publishingMonitoringResultJson.TotalValueChangesCount > 0,
                $"Messages received at IoT Hub: {publishingMonitoringResultJson.TotalValueChangesCount}");
        }
    }
}
