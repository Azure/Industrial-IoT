// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatform_E2E_Tests
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Xunit;
    using Newtonsoft.Json;
    using RestSharp;
    using TestExtensions;
    using Xunit.Abstractions;
    using IIoTPlatform_E2E_Tests.Runtime;
    using System.Threading;

    /// <summary>
    /// The test theory using different (ordered) test cases to go thru all required steps of publishing OPC UA node
    /// </summary>
    [TestCaseOrderer("IIoTPlatform_E2E_Tests.TestExtensions.TestOrderer", TestConstants.TestAssemblyName)]
    [Collection("IIoT Platform Test Collection")]
    public class PublishSingleNodesTestTheory
    {
        private readonly ITestOutputHelper _output;
        private readonly IIoTPlatformTestContext _context;

        public PublishSingleNodesTestTheory(IIoTPlatformTestContext context, ITestOutputHelper output) {
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        [Fact, PriorityOrder(1)]
        public async Task Test_CollectOAuthToken() {
            var token = await TestHelper.GetTokenAsync();
            Assert.NotEmpty(token);
        }

        [Fact, PriorityOrder(2)]
        public async Task Test_ReadSimulatedOpcUaNodes() {
            var simulatedOpcServer = await TestHelper.GetSimulatedOpcUaNodesAsync();
            Assert.NotNull(simulatedOpcServer);
            Assert.NotEmpty(simulatedOpcServer.Keys);
            Assert.NotEmpty(simulatedOpcServer.Values);
        }

        [Fact, PriorityOrder(3)]
        public async Task Test_RegisterOPCServer_Expect_Success() {

            var configuration = TestHelper.GetConfiguration();

            var iotHubConfig = new IoTHubConfig(configuration);
            var iotDeviceConfig = new DeviceConfig(configuration);

            var registryHelper = new RegistryHelper(iotHubConfig);

            var millisecondsDelay = 60 * 1000;
            var cts = new CancellationTokenSource(millisecondsDelay);

            await registryHelper.WaitForIIoTModulesConnectedAsync(iotDeviceConfig.DeviceId, cts.Token);

            var accessToken = await TestHelper.GetTokenAsync();
            var simulatedOpcServer = await TestHelper.GetSimulatedOpcUaNodesAsync();

            var client = new RestClient(TestHelper.GetBaseUrl()) {Timeout = 30000};

            var request = new RestRequest(Method.POST);
            request.AddHeader(TestConstants.HttpHeaderNames.Authorization, accessToken);
            request.Resource = TestConstants.APIRoutes.RegistryApplications;

            var body = new {
                discoveryUrl = simulatedOpcServer.Values.First().EndpointUrl
            };

            request.AddJsonBody(JsonConvert.SerializeObject(body));

            var response = await client.ExecuteAsync(request);
            Assert.NotNull(response);
            Assert.True(response.IsSuccessful, "POST /registry/v2/application failed!");

            if (!response.IsSuccessful) {
                _output.WriteLine($"StatusCode: {response.StatusCode}");
                _output.WriteLine($"ErrorMessage: {response.ErrorMessage}");
            }
        }

        [Fact, PriorityOrder(4)]
        public async Task Test_GetApplicationsFromRegistry_ExpectOneRegisteredApplication() {

            var accessToken = await TestHelper.GetTokenAsync();
            var client = new RestClient(TestHelper.GetBaseUrl()) { Timeout = 30000 };

            var request = new RestRequest(Method.GET);
            request.AddHeader(TestConstants.HttpHeaderNames.Authorization, accessToken);
            request.Resource = TestConstants.APIRoutes.RegistryApplications;

            var response = await client.ExecuteAsync(request);
            Assert.NotNull(response);
            Assert.True(response.IsSuccessful, "GET /registry/v2/application failed!");

            if (!response.IsSuccessful) {
                _output.WriteLine($"StatusCode: {response.StatusCode}");
                _output.WriteLine($"ErrorMessage: {response.ErrorMessage}");
            }

            Assert.NotEmpty(response.Content);
            dynamic json = JsonConvert.DeserializeObject(response.Content);

            var numberOfItems = (int)json.items.Count;
            Assert.True(numberOfItems > 0, $"number of applications registered need to be higher than 0 but was {numberOfItems}");
        }


        [Fact, PriorityOrder(5)]
        public async Task Test_GetEndpoints_Expect_OneWithMultipleAuthentication() {

            var accessToken = await TestHelper.GetTokenAsync();
            var client = new RestClient(TestHelper.GetBaseUrl()) { Timeout = 30000 };

            var request = new RestRequest(Method.GET);
            request.AddHeader(TestConstants.HttpHeaderNames.Authorization, accessToken);
            request.Resource = TestConstants.APIRoutes.RegistryEndpoints;

            var response = await client.ExecuteAsync(request);
            Assert.NotNull(response);
            Assert.True(response.IsSuccessful, "GET /registry/v2/endpoints failed!");

            if (!response.IsSuccessful) {
                _output.WriteLine($"StatusCode: {response.StatusCode}");
                _output.WriteLine($"ErrorMessage: {response.ErrorMessage}");
            }

            Assert.NotEmpty(response.Content);
            dynamic json = JsonConvert.DeserializeObject(response.Content);

            Assert.NotEmpty(json);
            var count = (int)json.items.Count;
            Assert.Equal(1, count);
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

            var accessToken = await TestHelper.GetTokenAsync();
            var client = new RestClient(TestHelper.GetBaseUrl()) { Timeout = 30000 };

            var request = new RestRequest(Method.POST);
            request.AddHeader(TestConstants.HttpHeaderNames.Authorization, accessToken);
            request.Resource = string.Format(TestConstants.APIRoutes.RegistryActivateEndpointsFormat, _context.OpcUaEndpointId);

            var response = await client.ExecuteAsync(request);
            Assert.NotNull(response);
            Assert.True(response.IsSuccessful, "POST /registry/v2/endpoints/{endpointId}/activate failed!");

            if (!response.IsSuccessful) {
                _output.WriteLine($"StatusCode: {response.StatusCode}");
                _output.WriteLine($"ErrorMessage: {response.ErrorMessage}");
            }

            Assert.Empty(response.Content);
        }

        [Fact, PriorityOrder(7)]
        public async Task Test_CheckIfEndpointWasActivated_Expect_ActivatedAndConnected() {
            var accessToken = await TestHelper.GetTokenAsync();
            var client = new RestClient(TestHelper.GetBaseUrl()) { Timeout = 30000 };

            var request = new RestRequest(Method.GET);
            request.AddHeader(TestConstants.HttpHeaderNames.Authorization, accessToken);
            request.Resource = TestConstants.APIRoutes.RegistryEndpoints;

            var response = await client.ExecuteAsync(request);
            Assert.NotNull(response);
            Assert.True(response.IsSuccessful, "GET /registry/v2/endpoints failed!");

            if (!response.IsSuccessful) {
                _output.WriteLine($"StatusCode: {response.StatusCode}");
                _output.WriteLine($"ErrorMessage: {response.ErrorMessage}");
            }

            Assert.NotEmpty(response.Content);
            dynamic json = JsonConvert.DeserializeObject(response.Content);

            var activationState = (string)json.items[0].activationState;
            Assert.Equal("ActivatedAndConnected", activationState);
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

            var accessToken = await TestHelper.GetTokenAsync();
            var simulatedOpcServer = await TestHelper.GetSimulatedOpcUaNodesAsync();
            var client = new RestClient(TestHelper.GetBaseUrl()) { Timeout = 30000 };

            var request = new RestRequest(Method.POST);
            request.AddHeader(TestConstants.HttpHeaderNames.Authorization, accessToken);
            request.Resource = string.Format(TestConstants.APIRoutes.PublisherStartFormat, _context.OpcUaEndpointId);

            var body = new {
                item = new {
                    nodeId = simulatedOpcServer.Values.First().OpcNodes.First().Id
                }
            };

            request.AddJsonBody(JsonConvert.SerializeObject(body));

            var response = await client.ExecuteAsync(request);
            Assert.NotNull(response);
            Assert.True(response.IsSuccessful, "POST /publisher/v2/publish/{endpointId}/start failed!");

            if (!response.IsSuccessful) {
                _output.WriteLine($"StatusCode: {response.StatusCode}");
                _output.WriteLine($"ErrorMessage: {response.ErrorMessage}");
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

            var accessToken = await TestHelper.GetTokenAsync();
            var simulatedOpcServer = await TestHelper.GetSimulatedOpcUaNodesAsync();
            var client = new RestClient(TestHelper.GetBaseUrl()) { Timeout = 30000 };

            var request = new RestRequest(Method.GET);
            request.AddHeader(TestConstants.HttpHeaderNames.Authorization, accessToken);
            request.Resource = TestConstants.APIRoutes.PublisherJobs;

            var response = await client.ExecuteAsync(request);
            Assert.NotNull(response);
            Assert.True(response.IsSuccessful, "GET /publisher/v2/jobs failed!");

            if (!response.IsSuccessful) {
                _output.WriteLine($"StatusCode: {response.StatusCode}");
                _output.WriteLine($"ErrorMessage: {response.ErrorMessage}");
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
        public void Test_VerifyDataAvailableAtIoTHub() {

            //todo use test event processor to verify data send to IoT Hub
            Assert.True(true);
        }

        [Fact, PriorityOrder(11)]
        public async Task RemoveJob_Expect_Success() {

            // used if running test cases separately (during development)
            if (string.IsNullOrWhiteSpace(_context.OpcUaEndpointId)) {
                await Test_GetEndpoints_Expect_OneWithMultipleAuthentication();
                Assert.False(string.IsNullOrWhiteSpace(_context.OpcUaEndpointId));
            }
            var accessToken = await TestHelper.GetTokenAsync();

            var client = new RestClient(TestHelper.GetBaseUrl()) { Timeout = 30000 };

            var request = new RestRequest(Method.DELETE);
            request.AddHeader(TestConstants.HttpHeaderNames.Authorization, accessToken);
            request.Resource = string.Format(TestConstants.APIRoutes.PublisherJobsFormat, _context.OpcUaEndpointId);

            var response = await client.ExecuteAsync(request);
            Assert.NotNull(response);
            Assert.True(response.IsSuccessful, "DELETE /publisher/v2/jobs/{jobId} failed!");

            if (!response.IsSuccessful) {
                _output.WriteLine($"StatusCode: {response.StatusCode}");
                _output.WriteLine($"ErrorMessage: {response.ErrorMessage}");
            }
        }
    }
}
