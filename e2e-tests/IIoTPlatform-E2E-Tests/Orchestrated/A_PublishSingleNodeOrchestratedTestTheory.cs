// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatform_E2E_Tests.Orchestrated
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Xunit;
    using Newtonsoft.Json;
    using RestSharp;
    using TestExtensions;
    using Xunit.Abstractions;
    using System.Threading;

    /// <summary>
    /// The test theory using different (ordered) test cases to go thru all required steps of publishing OPC UA node
    /// </summary>
    [TestCaseOrderer(TestCaseOrderer.FullName, TestConstants.TestAssemblyName)]
    [Collection("IIoT Platform Test Collection")]
    [Trait(TestConstants.TraitConstants.PublisherModeTraitName, TestConstants.TraitConstants.PublisherModeOrchestratedTraitValue)]
    public class A_PublishSingleNodeOrchestratedTestTheory
    {
        private readonly ITestOutputHelper _output;
        private readonly IIoTPlatformTestContext _context;

        public A_PublishSingleNodeOrchestratedTestTheory(IIoTPlatformTestContext context, ITestOutputHelper output) {
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _context.OutputHelper = _output;
        }

        [Fact, PriorityOrder(0)]
        public async Task Test_SetUnmanagedTagFalse() {
            await TestHelper.SwitchToOrchestratedModeAsync(_context);
        }

        [Fact, PriorityOrder(1)]
        public async Task Test_CollectOAuthToken() {
            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);
            var token = await TestHelper.GetTokenAsync(_context, cts.Token);
            Assert.NotEmpty(token);
        }

        [Fact, PriorityOrder(2)]
        public async Task Test_ReadSimulatedOpcUaNodes() {
            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);
            var simulatedOpcServer = await TestHelper.GetSimulatedPublishedNodesConfigurationAsync(_context, cts.Token);
            Assert.NotNull(simulatedOpcServer);
            Assert.NotEmpty(simulatedOpcServer.Keys);
            Assert.NotEmpty(simulatedOpcServer.Values);
        }

        [Fact, PriorityOrder(3)]
        public async Task Test_RegisterOPCServer_Expect_Success() {

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

            var body = new {
                discoveryUrl = simulatedOpcServer.Values.First().EndpointUrl
            };

            request.AddJsonBody(JsonConvert.SerializeObject(body));

            var response = await client.ExecuteAsync(request, cts.Token);
            Assert.NotNull(response);

            if (!response.IsSuccessful) {
                _output.WriteLine($"StatusCode: {response.StatusCode}");
                _output.WriteLine($"ErrorMessage: {response.ErrorMessage}");
                Assert.True(response.IsSuccessful, "POST /registry/v2/application failed!");
            }
        }

        [Fact, PriorityOrder(4)]
        public async Task Test_GetApplicationsFromRegistry_ExpectOneRegisteredApplication() {

            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);
            var simulatedOpcServer = await TestHelper.GetSimulatedPublishedNodesConfigurationAsync(_context, cts.Token);
            dynamic json = await TestHelper.WaitForDiscoveryToBeCompletedAsync(_context, cts.Token);

            var numberOfItems = (int)json.items.Count;
            bool found = false;
            var testPlc = simulatedOpcServer.Values.First();
            for (int indexOfTestPlc = 0; indexOfTestPlc < numberOfItems; indexOfTestPlc++) {

                var endpoint = ((string)json.items[indexOfTestPlc].discoveryUrls[0]).TrimEnd('/');
                if (endpoint == testPlc.EndpointUrl) {
                    found = true;
                    break;
                }
            }
            Assert.True(found, "OPC Application not activated");
        }


        [Fact, PriorityOrder(5)]
        public async Task Test_GetEndpoints_Expect_OneWithMultipleAuthentication() {
            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);

            var accessToken = await TestHelper.GetTokenAsync(_context, cts.Token);
            var client = new RestClient(_context.IIoTPlatformConfigHubConfig.BaseUrl) {
                Timeout = TestConstants.DefaultTimeoutInMilliseconds
            };

            // TODO use TestHelper.WaitForEndpointToBeActivatedAsync(...) instead of calling it directly
            var request = new RestRequest(Method.GET);
            request.AddHeader(TestConstants.HttpHeaderNames.Authorization, accessToken);
            request.Resource = TestConstants.APIRoutes.RegistryEndpoints;

            var response = await client.ExecuteAsync(request, cts.Token);
            Assert.NotNull(response);

            if (!response.IsSuccessful) {
                _output.WriteLine($"StatusCode: {response.StatusCode}");
                _output.WriteLine($"ErrorMessage: {response.ErrorMessage}");
                Assert.True(response.IsSuccessful, "GET /registry/v2/endpoints failed!");
            }

            Assert.NotEmpty(response.Content);
            dynamic json = JsonConvert.DeserializeObject(response.Content);

            Assert.NotEmpty(json);
            var count = (int)json.items.Count;
            Assert.True(count > 1, "no applications registered");
            var id = (string)json.items[0].registration.id;
            Assert.NotEmpty(id);
            var securityMode = (string)json.items[0].registration.endpoint.securityMode;
            Assert.Equal("SignAndEncrypt", securityMode);
            var authenticationModeNone = (string)json.items[0].registration.authenticationMethods[0].credentialType;
            Assert.Equal("None", authenticationModeNone);
            var authenticationModeUserName = (string)json.items[0].registration.authenticationMethods[1].credentialType;
            Assert.Equal("UserName", authenticationModeUserName);
            var authenticationModeCertificate = (string)json.items[0].registration.authenticationMethods[2].credentialType;
            Assert.Equal("X509Certificate", authenticationModeCertificate);

            _context.OpcUaEndpointId = id;
        }

        [Fact, PriorityOrder(6)]
        public async Task Test_ActivateEndpoint_Expect_Success() {

            // used if running test cases separately (during development)
            if (string.IsNullOrWhiteSpace(_context.OpcUaEndpointId)) {
                await Test_GetEndpoints_Expect_OneWithMultipleAuthentication();
                Assert.False(string.IsNullOrWhiteSpace(_context.OpcUaEndpointId));
            }

            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);
            var accessToken = await TestHelper.GetTokenAsync(_context, cts.Token);
            var client = new RestClient(_context.IIoTPlatformConfigHubConfig.BaseUrl) {
                Timeout = TestConstants.DefaultTimeoutInMilliseconds
            };

            var request = new RestRequest(Method.POST);
            request.AddHeader(TestConstants.HttpHeaderNames.Authorization, accessToken);
            request.Resource = string.Format(TestConstants.APIRoutes.RegistryActivateEndpointsFormat, _context.OpcUaEndpointId);

            var response = await client.ExecuteAsync(request, cts.Token);
            Assert.NotNull(response);

            if (!response.IsSuccessful) {
                _output.WriteLine($"StatusCode: {response.StatusCode}");
                _output.WriteLine($"ErrorMessage: {response.ErrorMessage}");
                Assert.True(response.IsSuccessful, "POST /registry/v2/endpoints/{endpointId}/activate failed!");
            }

            Assert.Empty(response.Content);
        }

        [Fact, PriorityOrder(7)]
        public async Task Test_CheckIfEndpointWasActivated_Expect_ActivatedAndConnected() {

            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);
            dynamic json = await TestHelper.WaitForEndpointToBeActivatedAsync(_context, cts.Token);

            var endpointState = (string)json.items[0].endpointState;
            Assert.Equal("Ready", endpointState);
        }

        [Fact, PriorityOrder(8)]
        public async Task Test_PublishNodeWithDefaults_Expect_DataAvailableAtIoTHub() {

            // used if running test cases separately (during development)
            if (string.IsNullOrWhiteSpace(_context.OpcUaEndpointId)) {
                await Test_GetEndpoints_Expect_OneWithMultipleAuthentication();
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
            request.Resource = string.Format(TestConstants.APIRoutes.PublisherStartFormat, _context.OpcUaEndpointId);

            var body = new {
                item = new {
                    nodeId = simulatedOpcServer.Values.First().OpcNodes.First().Id
                }
            };

            request.AddJsonBody(JsonConvert.SerializeObject(body));

            var response = await client.ExecuteAsync(request, cts.Token);
            Assert.NotNull(response);

            if (!response.IsSuccessful) {
                _output.WriteLine($"StatusCode: {response.StatusCode}");
                _output.WriteLine($"ErrorMessage: {response.ErrorMessage}");
                Assert.True(response.IsSuccessful, "POST /publisher/v2/publish/{endpointId}/start failed!");
            }

            Assert.Equal("{}",response.Content);

        }

        [Fact, PriorityOrder(9)]
        public async Task Test_GetListOfJobs_Expect_OneJobWithPublishingOneNode() {

            // used if running test cases separately (during development)
            if (string.IsNullOrWhiteSpace(_context.OpcUaEndpointId)) {
                await Test_GetEndpoints_Expect_OneWithMultipleAuthentication();
                Assert.False(string.IsNullOrWhiteSpace(_context.OpcUaEndpointId));
            }

            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);
            var accessToken = await TestHelper.GetTokenAsync(_context, cts.Token);
            var simulatedOpcServer = await TestHelper.GetSimulatedPublishedNodesConfigurationAsync(_context, cts.Token);
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

            dynamic json = JsonConvert.DeserializeObject(response.Content);

            var count = (int)json.jobs.Count;
            Assert.Equal(1, count);
            Assert.NotNull(json.jobs[0].jobConfiguration);
            Assert.NotNull(json.jobs[0].jobConfiguration.writerGroup);
            Assert.NotNull(json.jobs[0].jobConfiguration.writerGroup.dataSetWriters);
            count = (int)json.jobs[0].jobConfiguration.writerGroup.dataSetWriters.Count;
            Assert.Equal(1, count);
            Assert.NotNull(json.jobs[0].jobConfiguration.writerGroup.dataSetWriters[0].dataSet);
            Assert.NotNull(json.jobs[0].jobConfiguration.writerGroup.dataSetWriters[0].dataSet.dataSetSource);
            Assert.NotNull(json.jobs[0].jobConfiguration.writerGroup.dataSetWriters[0].dataSet.dataSetSource.publishedVariables.publishedData);
            count = (int)json.jobs[0].jobConfiguration.writerGroup.dataSetWriters[0].dataSet.dataSetSource.publishedVariables.publishedData.Count;
            Assert.Equal(1, count);
            Assert.NotEmpty((string)json.jobs[0].jobConfiguration.writerGroup.dataSetWriters[0].dataSet.dataSetSource.publishedVariables.publishedData[0].publishedVariableNodeId);
            var publishedNodeId = (string)json.jobs[0].jobConfiguration.writerGroup.dataSetWriters[0].dataSet.dataSetSource.publishedVariables.publishedData[0].publishedVariableNodeId;
            Assert.Equal(simulatedOpcServer.Values.First().OpcNodes.First().Id, publishedNodeId);
        }

        [Fact, PriorityOrder(10)]
        public async Task Test_VerifyDataAvailableAtIoTHub() {

            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);

            //use test event processor to verify data send to IoT Hub (expected* set to zero as data gap analysis is not part of this test case)
            await TestHelper.StartMonitoringIncomingMessagesAsync(_context, 0, 0, 0, cts.Token);
            // wait some time to generate events to process
            await Task.Delay(90 * 1000, cts.Token);
            var json = await TestHelper.StopMonitoringIncomingMessagesAsync(_context, cts.Token);
            Assert.True((int)json.totalValueChangesCount > 0, "No messages received at IoT Hub");
        }

        [Fact, PriorityOrder(11)]
        public async Task RemoveJob_Expect_Success() {

            // used if running test cases separately (during development)
            if (string.IsNullOrWhiteSpace(_context.OpcUaEndpointId)) {
                await Test_GetEndpoints_Expect_OneWithMultipleAuthentication();
                Assert.False(string.IsNullOrWhiteSpace(_context.OpcUaEndpointId));
            }

            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);
            var accessToken = await TestHelper.GetTokenAsync(_context, cts.Token);

            var client = new RestClient(_context.IIoTPlatformConfigHubConfig.BaseUrl) {
                Timeout = TestConstants.DefaultTimeoutInMilliseconds
            };

            var request = new RestRequest(Method.DELETE);
            request.AddHeader(TestConstants.HttpHeaderNames.Authorization, accessToken);
            request.Resource = string.Format(TestConstants.APIRoutes.PublisherJobsFormat, _context.OpcUaEndpointId);

            var response = await client.ExecuteAsync(request, cts.Token);
            Assert.NotNull(response);

            if (!response.IsSuccessful) {
                _output.WriteLine($"StatusCode: {response.StatusCode}");
                _output.WriteLine($"ErrorMessage: {response.ErrorMessage}");
                Assert.True(response.IsSuccessful, "DELETE /publisher/v2/jobs/{jobId} failed!");
            }
        }

        [Fact, PriorityOrder(12)]
        public async Task Test_VerifyNoDataIncomingAtIoTHub() {
            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);
            await Task.Delay(90 * 1000, cts.Token); //wait till the publishing has stopped
            //use test event processor to verify data send to IoT Hub (expected* set to zero as data gap analysis is not part of this test case)
            await TestHelper.StartMonitoringIncomingMessagesAsync(_context, 0, 0, 0, cts.Token);
            // wait some time to generate events to process
            await Task.Delay(90 * 1000, cts.Token);
            var json = await TestHelper.StopMonitoringIncomingMessagesAsync(_context, cts.Token);
            Assert.True((int)json.totalValueChangesCount == 0, "Messages received at IoT Hub");
        }

        [Fact, PriorityOrder(13)]
        public async Task Test_RemoveAllApplications() {
            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);

            var accessToken = await TestHelper.GetTokenAsync(_context, cts.Token);
            var client = new RestClient(_context.IIoTPlatformConfigHubConfig.BaseUrl) { Timeout = TestConstants.DefaultTimeoutInMilliseconds };

            var request = new RestRequest(Method.DELETE);
            request.AddHeader(TestConstants.HttpHeaderNames.Authorization, accessToken);
            request.Resource = TestConstants.APIRoutes.RegistryApplications;

            var response = await client.ExecuteAsync(request, cts.Token);
            Assert.NotNull(response);

            if (!response.IsSuccessful) {
                _output.WriteLine($"StatusCode: {response.StatusCode}");
                _output.WriteLine($"ErrorMessage: {response.ErrorMessage}");
                Assert.True(response.IsSuccessful, "DELETE /registry/v2/application failed!");
            }
        }
    }
}
