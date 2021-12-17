// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatform_E2E_Tests.Standalone {
    using IIoTPlatform_E2E_Tests.Deploy;
    using Microsoft.Azure.Devices;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using TestExtensions;
    using TestModels;
    using Xunit;
    using Xunit.Abstractions;
    using Autofac;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Hub.Models;
    //using Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models;
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Azure.IIoT.Serializers.NewtonSoft;
    using Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Models;
    using System.Net;

    /// <summary>
    /// The test theory using different (ordered) test cases to go thru all required steps of publishing OPC UA node
    /// </summary>
    [TestCaseOrderer(TestCaseOrderer.FullName, TestConstants.TestAssemblyName)]
    [Collection("IIoT Standalone Direct Methods Test Collection")]
    [Trait(TestConstants.TraitConstants.PublisherModeTraitName, TestConstants.TraitConstants.PublisherModeStandaloneTraitValue)]
    public class A_PublishSingleNodeStandaloneDirectMethodTestTheory {

        private readonly ITestOutputHelper _output;
        private readonly IIoTMultipleNodesTestContext _context;
        private readonly ServiceClient _iotHubClient;
        private readonly IIoTHubTwinServices _iotHubClient1;
        private readonly IContainer _container;
        private readonly IJsonSerializer _serializer;
        private string _iotHubConnectionString;
        private string _iotHubPublisherDeviceName;
        private string _iotHubPublisherModuleName; 

        public A_PublishSingleNodeStandaloneDirectMethodTestTheory(
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

            _iotHubClient1 = _container.Resolve<IIoTHubTwinServices>();
        }

        // the test case for now are just empty container that deploy publisher resource
        // when direct methods will be implemented, also the test should be implemented.

        [Theory]
        [InlineData(MessagingMode.Samples)]
        [InlineData(MessagingMode.PubSub)]
        async Task SubscribeUnsubscribeDirectMethodTest(MessagingMode messagingMode) {
            var ioTHubEdgeBaseDeployment = new IoTHubEdgeBaseDeployment(_context);
            var ioTHubPublisherDeployment = new IoTHubPublisherDeployment(_context, messagingMode);

            _iotHubPublisherModuleName = "publisher_standalone";

            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);

            // Make sure that there is no active monitoring.
            await TestHelper.StopMonitoringIncomingMessagesAsync(_context, cts.Token);

            // Create base edge deployment.
            var baseDeploymentResult = await ioTHubEdgeBaseDeployment.CreateOrUpdateLayeredDeploymentAsync(cts.Token);
            Assert.True(baseDeploymentResult, "Failed to create/update new edge base deployment.");
            _output.WriteLine("Created/Updated new edge base deployment.");

            // Create layered edge deployment.
            var layeredDeploymentResult = await ioTHubPublisherDeployment.CreateOrUpdateLayeredDeploymentAsync(cts.Token);
            Assert.True(layeredDeploymentResult, "Failed to create/update layered deployment for publisher module.");
            _output.WriteLine("Created/Updated layered deployment for publisher module.");

            var model = await TestHelper.CreateSingleNodeModelAsync(_context, cts.Token);

            await TestHelper.SwitchToStandaloneModeAsync(_context, cts.Token);

            // We will wait for module to be deployed.
            var exception = Record.Exception(() => _context.RegistryHelper.WaitForIIoTModulesConnectedAsync(
                _context.DeviceConfig.DeviceId,
                cts.Token,
                new string[] { "publisher_standalone" }
            ).GetAwaiter().GetResult());
            Assert.Null(exception);

            //Todo
            //Here instantiate _directMethod class and call the Publish direct method
            //This is just a sample call
            //_directMethods = new DirectMethodsAPIClient(_iotHubConnectionString, _iotHubPublisherDeviceName, _iotHubPublisherModuleName);

            //var publishingResult = await _directMethods.PublishNodesAsync(
            //            model.EndpointUrl,
            //            model.OpcNodes,
            //            ct: cts.Token
            //        ).ConfigureAwait(false);

            //var request = new PublishNodesRequestApiModel {
            //    EndpointUrl = model.EndpointUrl,
            //    UseSecurity = false,
            //    UserName = null,
            //    Password = null,
            //    OpcNodes = model.OpcNodes
            //};

            var request = model.ToApiModel();

            var response = await _iotHubClient1.CallMethodAsync(_iotHubPublisherDeviceName, new MethodParameterModel {
                Name = "PublishNodes_V1",
                JsonPayload = _serializer.SerializeToString(request)
            }, cts.Token);

            Assert.Equal((int)HttpStatusCode.OK, response.Status);

            // Use test event processor to verify data send to IoT Hub (expected* set to zero
            // as data gap analysis is not part of this test case).
            await TestHelper.StartMonitoringIncomingMessagesAsync(_context, 0, 0, 0, cts.Token);

            // Wait some time to generate events to process.
            await Task.Delay(TestConstants.DefaultTimeoutInMilliseconds, cts.Token);

            // Stop monitoring and get the result.
            var publishingMonitoringResultJson = await TestHelper.StopMonitoringIncomingMessagesAsync(_context, cts.Token);
            Assert.True((int)publishingMonitoringResultJson.totalValueChangesCount > 0, "No messages received at IoT Hub");
            Assert.True((uint)publishingMonitoringResultJson.droppedValueCount == 0,
                $"Dropped messages detected: {(uint)publishingMonitoringResultJson.droppedValueCount}");
            Assert.True((uint)publishingMonitoringResultJson.duplicateValueCount == 0,
                $"Duplicate values detected: {(uint)publishingMonitoringResultJson.duplicateValueCount}");

            //Todo
            //Here call the Unpublish direct method. This is just a sample call
            //publishingResult = await _directMethods.UnPublishNodesAsync(
            //            model.EndpointUrl,
            //            model.OpcNodes,
            //            ct: cts.Token
            //        ).ConfigureAwait(false);

            response = await _iotHubClient1.CallMethodAsync(_iotHubPublisherDeviceName, new MethodParameterModel {
                Name = "UnPublishNodes_V1",
                JsonPayload = _serializer.SerializeToString(request)
            }, cts.Token);

            Assert.Equal((int)HttpStatusCode.OK, response.Status);

            // Wait till the publishing has stopped.
            await Task.Delay(TestConstants.DefaultTimeoutInMilliseconds, cts.Token);

            // Use test event processor to verify data send to IoT Hub (expected* set to zero
            // as data gap analysis is not part of this test case).
            await TestHelper.StartMonitoringIncomingMessagesAsync(_context, 0, 0, 0, cts.Token);

            // Wait some time to generate events to process
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

            // Create base edge deployment.
            var baseDeploymentResult = await ioTHubEdgeBaseDeployment.CreateOrUpdateLayeredDeploymentAsync(cts.Token);
            Assert.True(baseDeploymentResult, "Failed to create/update new edge base deployment.");
            _output.WriteLine("Created/Updated new edge base deployment.");

            // Create layered edge deployment.
            var layeredDeploymentResult1 = await ioTHubLegacyPublisherDeployment.CreateOrUpdateLayeredDeploymentAsync(cts.Token);
            Assert.True(layeredDeploymentResult1, "Failed to create/update layered deployment for legacy publisher module.");
            _output.WriteLine("Created/Updated layered deployment for legacy publisher module.");

            var model = await TestHelper.CreateSingleNodeModelAsync(_context, cts.Token);

            await TestHelper.SwitchToStandaloneModeAsync(_context, cts.Token);

            // We will wait for module to be deployed.
            var exception = Record.Exception(() => _context.RegistryHelper.WaitForIIoTModulesConnectedAsync(
                _context.DeviceConfig.DeviceId,
                cts.Token,
                new string[] { "publisher_standalone_legacy" }
            ).GetAwaiter().GetResult());
            Assert.Null(exception);

            //Todo
            //Here instantiate _directMethod class and call the Publish direct method
            //This is just a sample call
            //_directMethods = new DirectMethodsAPIClient(_iotHubConnectionString, _iotHubPublisherDeviceName, _iotHubPublisherModuleName);

            //var publishingResult = await _directMethods.PublishNodesAsync(
            //            model.EndpointUrl,
            //            model.OpcNodes,
            //            ct: cts.Token
            //        ).ConfigureAwait(false);

            // Use test event processor to verify data send to IoT Hub (expected* set to zero
            // as data gap analysis is not part of this test case).
            await TestHelper.StartMonitoringIncomingMessagesAsync(_context, 0, 0, 0, cts.Token);

            // Wait some time to generate events to process.
            await Task.Delay(TestConstants.DefaultTimeoutInMilliseconds, cts.Token);

            // Stop monitoring and get the result.
            var publishingMonitoringResultJson = await TestHelper.StopMonitoringIncomingMessagesAsync(_context, cts.Token);
            Assert.True((int)publishingMonitoringResultJson.totalValueChangesCount > 0, "No messages received at IoT Hub");
            Assert.True((uint)publishingMonitoringResultJson.droppedValueCount == 0,
                $"Dropped messages detected: {(uint)publishingMonitoringResultJson.droppedValueCount}");
            Assert.True((uint)publishingMonitoringResultJson.duplicateValueCount == 0,
                $"Duplicate values detected: {(uint)publishingMonitoringResultJson.duplicateValueCount}");

            //Todo
            //Here call the Unpublish direct method. This is just a sample call
            //publishingResult = await _directMethods.UnPublishNodesAsync(
            //            model.EndpointUrl,
            //            model.OpcNodes,
            //            ct: cts.Token
            //        ).ConfigureAwait(false);

            // Wait till the publishing has stopped.
            await Task.Delay(TestConstants.DefaultTimeoutInMilliseconds, cts.Token);

            // Use test event processor to verify data send to IoT Hub (expected* set to zero
            // as data gap analysis is not part of this test case).
            await TestHelper.StartMonitoringIncomingMessagesAsync(_context, 0, 0, 0, cts.Token);

            // Wait some time to generate events to process
            await Task.Delay(TestConstants.DefaultTimeoutInMilliseconds, cts.Token);

            // Stop monitoring and get the result.
            var unpublishingMonitoringResultJson = await TestHelper.StopMonitoringIncomingMessagesAsync(_context, cts.Token);
            Assert.True((int)unpublishingMonitoringResultJson.totalValueChangesCount == 0,
                $"Messages received at IoT Hub: {(int)unpublishingMonitoringResultJson.totalValueChangesCount}");
        }

    }
}
