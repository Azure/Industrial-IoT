// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatform_E2E_Tests.Orchestrated
{
    using System;
    using System.Dynamic;
    using System.Linq;
    using System.Threading.Tasks;
    using Xunit;
    using Newtonsoft.Json;
    using RestSharp;
    using TestExtensions;
    using Xunit.Abstractions;
    using System.Threading;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// The test theory using different (ordered) test cases to go thru all required steps of publishing OPC UA node
    /// </summary>
    [TestCaseOrderer("IIoTPlatform_E2E_Tests.TestExtensions.TestOrderer", TestConstants.TestAssemblyName)]
    [Collection("IIoT Platform Test Collection")]
    [Trait(TestConstants.TraitConstants.PublisherModeTraitName, TestConstants.TraitConstants.PublisherModeOrchestratedTraitValue)]
    public class PublishMultipleNodesOrchestratedTestTheory {
        private readonly ITestOutputHelper _output;
        private readonly IIoTPlatformTestContext _context;

        public PublishMultipleNodesOrchestratedTestTheory(IIoTPlatformTestContext context, ITestOutputHelper output) {
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _context.OutputHelper = _output;
        }

        [Fact, PriorityOrder(0)]
        public async Task Test_SetUnmanagedTagFalse() {
            await TestHelper.SwitchToOrchestratedModeAsync(_context);
        }

        /// <summary>
        /// <see cref="PublishSingleNodeOrchestratedTestTheory"/> has separated all the steps in different test cases
        /// For this test theory required preparation steps are combine in this single test case
        /// </summary>
        /// <returns></returns>
        [Fact, PriorityOrder(1)]
        public async Task Test_PrepareTestDeploymentForTestCase_Expect_Success() {

            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);

            // We will wait for microservices of IIoT platform to be healthy and modules to be deployed.
            await TestHelper.WaitForServicesAsync(_context, cts.Token);
            await _context.RegistryHelper.WaitForIIoTModulesConnectedAsync(_context.DeviceConfig.DeviceId, cts.Token);

            var accessToken = await TestHelper.GetTokenAsync(_context, cts.Token);
            var simulatedOpcServer = await TestHelper.GetSimulatedPublishedNodesConfigurationAsync(_context, cts.Token);

            var client = new RestClient(_context.IIoTPlatformConfigHubConfig.BaseUrl) {Timeout = TestConstants.DefaultTimeoutInMilliseconds};

            var request = new RestRequest(Method.POST);
            request.AddHeader(TestConstants.HttpHeaderNames.Authorization, accessToken);
            request.Resource = TestConstants.APIRoutes.RegistryApplications;

            // use the second OPC PLC for testing
            cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);
            var testPlc = simulatedOpcServer.Values.Skip(1).First();
            var body = new {
                discoveryUrl = testPlc.EndpointUrl
            };

            request.AddJsonBody(JsonConvert.SerializeObject(body));

            var response = await client.ExecuteAsync(request, cts.Token);
            Assert.NotNull(response);

            if (!response.IsSuccessful) {
                _output.WriteLine($"StatusCode: {response.StatusCode}");
                _output.WriteLine($"ErrorMessage: {response.ErrorMessage}");
                Assert.True(response.IsSuccessful, "POST /registry/v2/application failed!");
            }

            // check that Application was registered
            cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);
            dynamic json = await TestHelper.WaitForDiscoveryToBeCompletedAsync(_context, cts.Token);
            bool found = false;
            for(int indexOfTestPlc = 0; indexOfTestPlc < (int)json.items.Count; indexOfTestPlc++) {

                var endpoint = ((string)json.items[indexOfTestPlc].discoveryUrls[0]).TrimEnd('/');
                if (endpoint == testPlc.EndpointUrl) {
                    found = true;
                    break;
                }
            }
            Assert.True(found, "OPC Application not activated");

            // Read OPC UA Endpoint ID
            cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);
            request = new RestRequest(Method.GET);
            request.AddHeader(TestConstants.HttpHeaderNames.Authorization, accessToken);
            request.Resource = TestConstants.APIRoutes.RegistryEndpoints;

            response = await client.ExecuteAsync(request, cts.Token);
            Assert.NotNull(response);

            if (!response.IsSuccessful) {
                _output.WriteLine($"StatusCode: {response.StatusCode}");
                _output.WriteLine($"ErrorMessage: {response.ErrorMessage}");
                Assert.True(response.IsSuccessful, "GET /registry/v2/endpoints failed!");
            }

            Assert.NotEmpty(response.Content);
            json = JsonConvert.DeserializeObject<ExpandoObject>(response.Content, new ExpandoObjectConverter());
            Assert.NotNull(json);

            found = false;
            for (int indexOfTestPlc = 0; indexOfTestPlc < (int)json.items.Count; indexOfTestPlc++) {
                var endpoint = ((string)json.items[indexOfTestPlc].registration.endpointUrl).TrimEnd('/');
                if (endpoint == testPlc.EndpointUrl) {
                    found = true;
                    _context.OpcUaEndpointId = (string)json.items[indexOfTestPlc].registration.id;
                    break;
                }
            }
            Assert.True(found, "Could not find endpoints of OPC Application");

            // Activate OPC UA Endpoint
            cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);

            request = new RestRequest(Method.POST);
            request.AddHeader(TestConstants.HttpHeaderNames.Authorization, accessToken);
            request.Resource = string.Format(TestConstants.APIRoutes.RegistryActivateEndpointsFormat, _context.OpcUaEndpointId);

            response = await client.ExecuteAsync(request, cts.Token);
            Assert.NotNull(response);

            if (!response.IsSuccessful) {
                _output.WriteLine($"StatusCode: {response.StatusCode}");
                _output.WriteLine($"ErrorMessage: {response.ErrorMessage}");
                Assert.True(response.IsSuccessful, "POST /registry/v2/endpoints/{endpointId}/activate failed!");
            }

            Assert.Empty(response.Content);

            // wait until OPC UA Endpoint is activated
            cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);
            json = await TestHelper.WaitForEndpointToBeActivatedAsync(_context, cts.Token);

            found = false;
            for (int indexOfTestPlc = 0; indexOfTestPlc < (int)json.items.Count; indexOfTestPlc++) {
                var endpoint = ((string)json.items[indexOfTestPlc].registration.endpointUrl).TrimEnd('/');
                if (endpoint == testPlc.EndpointUrl) {
                    found = true;
                    var endpointState = (string)json.items[0].endpointState;
                    Assert.Equal("Ready", endpointState);
                    break;
                }
            }

            Assert.True(found, "OPC UA Endpoint couldn't be activated");
        }


        [Fact, PriorityOrder(2)]
        public async Task Test_PublishNodeWithDefaults_Expect_DataAvailableAtIoTHub() {

            // used if running test cases separately (during development)
            if (string.IsNullOrWhiteSpace(_context.OpcUaEndpointId)) {
                await Test_PrepareTestDeploymentForTestCase_Expect_Success();
                Assert.False(string.IsNullOrWhiteSpace(_context.OpcUaEndpointId));
            }

            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);
            var accessToken = await TestHelper.GetTokenAsync(_context, cts.Token);
            var simulatedOpcServer = await TestHelper.GetSimulatedPublishedNodesConfigurationAsync(_context, cts.Token);
            var client = new RestClient(_context.IIoTPlatformConfigHubConfig.BaseUrl) {
                Timeout = TestConstants.DefaultTimeoutInMilliseconds
            };

            var request = new RestRequest(Method.POST);
            request.AddHeader(TestConstants.HttpHeaderNames.Authorization, accessToken);
            request.Resource = string.Format(TestConstants.APIRoutes.PublisherBulkFormat, _context.OpcUaEndpointId);

            var testPlc = simulatedOpcServer.Values.Skip(1).First();
            var body = new {
                NodesToAdd = testPlc.OpcNodes.Skip(250).Select(node => new {nodeId = node.Id}).ToArray()
            };

            request.AddJsonBody(JsonConvert.SerializeObject(body));

            var response = await client.ExecuteAsync(request, cts.Token);
            Assert.NotNull(response);

            if (!response.IsSuccessful) {
                _output.WriteLine($"StatusCode: {response.StatusCode}");
                _output.WriteLine($"ErrorMessage: {response.ErrorMessage}");
                Assert.True(response.IsSuccessful, "POST /publisher/v2/publish/{endpointId}/start failed!");
            }
        }

        [Fact, PriorityOrder(3)]
        public async Task Test_GetListOfJobs_Expect_JobWithEndpointId() {

            // used if running test cases separately (during development)
            if (string.IsNullOrWhiteSpace(_context.OpcUaEndpointId)) {
                await Test_PrepareTestDeploymentForTestCase_Expect_Success();
                Assert.False(string.IsNullOrWhiteSpace(_context.OpcUaEndpointId));
            }

            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);
            var accessToken = await TestHelper.GetTokenAsync(_context, cts.Token);
            var client = new RestClient(_context.IIoTPlatformConfigHubConfig.BaseUrl) {
                Timeout = TestConstants.DefaultTimeoutInMilliseconds
            };

            var request = new RestRequest(Method.GET);
            request.AddHeader(TestConstants.HttpHeaderNames.Authorization, accessToken);
            request.Resource = TestConstants.APIRoutes.PublisherJobs;

            var response = await client.ExecuteAsync(request, cts.Token);
            Assert.NotNull(response);

            if (!response.IsSuccessful) {
                _output.WriteLine($"StatusCode: {response.StatusCode}");
                _output.WriteLine($"ErrorMessage: {response.ErrorMessage}");
                Assert.True(response.IsSuccessful, "GET /publisher/v2/jobs failed!");
            }

            dynamic json = JsonConvert.DeserializeObject<ExpandoObject>(response.Content, new ExpandoObjectConverter());

            bool found = false;
            for (int jobIndex = 0; jobIndex < (int)json.jobs.Count; jobIndex++) {
                var id = (string)json.jobs[jobIndex].id;
                if (id == _context.OpcUaEndpointId) {
                    found = true;
                    break;
                }
            }
            Assert.True(found, "Publishing Job was not created!");
        }

        [Fact, PriorityOrder(4)]
        public async Task Test_VerifyDataAvailableAtIoTHub() {

            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);

            //use test event processor to verify data send to IoT Hub (expected* set to zero as data gap analysis is not part of this test case)
            await TestHelper.StartMonitoringIncomingMessagesAsync(_context, 50, 1000, 90_000_000, cts.Token);
            // wait some time to generate events to process
            await Task.Delay(90 * 1000, cts.Token);
            var json = await TestHelper.StopMonitoringIncomingMessagesAsync(_context, cts.Token);
            Assert.True((int)json.totalValueChangesCount > 0, "No messages received at IoT Hub");
        }

        //[Fact, PriorityOrder(11)]
        //public async Task RemoveJob_Expect_Success() {

        //    // used if running test cases separately (during development)
        //    if (string.IsNullOrWhiteSpace(_context.OpcUaEndpointId)) {
        //        await Test_GetEndpoints_Expect_OneWithMultipleAuthentication();
        //        Assert.False(string.IsNullOrWhiteSpace(_context.OpcUaEndpointId));
        //    }

        //    var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);
        //    var accessToken = await TestHelper.GetTokenAsync(_context, cts.Token);

        //    var client = new RestClient(_context.IIoTPlatformConfigHubConfig.BaseUrl) {
        //        Timeout = TestConstants.DefaultTimeoutInMilliseconds
        //    };

        //    var request = new RestRequest(Method.DELETE);
        //    request.AddHeader(TestConstants.HttpHeaderNames.Authorization, accessToken);
        //    request.Resource = string.Format(TestConstants.APIRoutes.PublisherJobsFormat, _context.OpcUaEndpointId);

        //    var response = await client.ExecuteAsync(request, cts.Token);
        //    Assert.NotNull(response);
        //    Assert.True(response.IsSuccessful, "DELETE /publisher/v2/jobs/{jobId} failed!");

        //    if (!response.IsSuccessful) {
        //        _output.WriteLine($"StatusCode: {response.StatusCode}");
        //        _output.WriteLine($"ErrorMessage: {response.ErrorMessage}");
        //    }
        //}

        //[Fact, PriorityOrder(12)]
        //public async Task Test_VerifyNoDataIncomingAtIoTHub() {
        //    var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);
        //    await Task.Delay(90 * 1000, cts.Token); //wait till the publishing has stopped
        //    //use test event processor to verify data send to IoT Hub (expected* set to zero as data gap analysis is not part of this test case)
        //    await TestHelper.StartMonitoringIncomingMessagesAsync(_context, 0, 0, 0, cts.Token);
        //    // wait some time to generate events to process
        //    await Task.Delay(90 * 1000, cts.Token);
        //    var json = await TestHelper.StopMonitoringIncomingMessagesAsync(_context, cts.Token);
        //    Assert.True((int)json.totalValueChangesCount == 0, "Messages received at IoT Hub");
        //}
    }
}
