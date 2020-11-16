// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatform_E2E_Tests
{
    using System;
    using System.Linq;
    using System.Net.Http.Headers;
    using System.Reflection;
    using Xunit;
    using Newtonsoft.Json;
    using RestSharp;
    using TestExtensions;
    using Xunit.Abstractions;

    [TestCaseOrderer("IIoTPlatform_E2E_Tests.TestExtensions.TestOrderer", TestConstants.TestAssemblyName)]
    [Collection("IIoT Platform Test Collection")]
    public class DummyTests
    {
        private readonly ITestOutputHelper _output;
        private readonly IIoTPlatformTestContext _context;

        public DummyTests(IIoTPlatformTestContext context, ITestOutputHelper output) {
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        [Fact, PriorityOrder(1)]
        public void Test_CollectOAuthToken() {
            var token = TestHelper.GetToken();
            Assert.NotEmpty(token);
        }

        [Fact, PriorityOrder(2)]
        public async void Test_RegisterOPCServer_Expect_Success() {
            var accessToken = TestHelper.GetToken();
            var simulatedOpcServer = await TestHelper.GetSimulatedOpcUaNodes();

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

        [Fact, PriorityOrder(3)]
        public void Test_GetApplicationsFromRegistry_ExpectOneRegisteredApplication() {

            var accessToken = TestHelper.GetToken();
            var client = new RestClient(TestHelper.GetBaseUrl()) { Timeout = 30000 };

            var request = new RestRequest(Method.GET);
            request.AddHeader(TestConstants.HttpHeaderNames.Authorization, accessToken);
            request.Resource = TestConstants.APIRoutes.RegistryApplications;

            var response = client.Execute(request);
            Assert.NotNull(response);
            Assert.True(response.IsSuccessful, "Get /registry/v2/application failed!");

            if (!response.IsSuccessful) {
                _output.WriteLine($"StatusCode: {response.StatusCode}");
                _output.WriteLine($"ErrorMessage: {response.ErrorMessage}");
            }

            Assert.NotEmpty(response.Content);
            dynamic json = JsonConvert.DeserializeObject(response.Content);

            var numberOfItems = (int)json.items.Count;
            Assert.True(numberOfItems > 0, $"number of applications registered need to be higher than 0 but was {numberOfItems}");
        }


        [Fact, PriorityOrder(4)]
        public void Test_GetEndpoints_Expect_OneWithMultipleAuthentication() {

            var accessToken = TestHelper.GetToken();
            var client = new RestClient(TestHelper.GetBaseUrl()) { Timeout = 30000 };

            var request = new RestRequest(Method.GET);
            request.AddHeader(TestConstants.HttpHeaderNames.Authorization, accessToken);
            request.Resource = TestConstants.APIRoutes.RegistryEndpoints;

            var response = client.Execute(request);
            Assert.NotNull(response);
            Assert.True(response.IsSuccessful, "Get /registry/v2/endpoints failed!");

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

        [Fact, PriorityOrder(5)]
        public void Test_ActivateEndpoint_Expect_Success() {

            if (string.IsNullOrWhiteSpace(_context.OpcUaEndpointId)) {
                Test_GetEndpoints_Expect_OneWithMultipleAuthentication();
            }

            var accessToken = TestHelper.GetToken();
            var client = new RestClient(TestHelper.GetBaseUrl()) { Timeout = 30000 };

            var request = new RestRequest(Method.POST);
            request.AddHeader(TestConstants.HttpHeaderNames.Authorization, accessToken);
            request.Resource = string.Format(TestConstants.APIRoutes.RegistryActivateEndpoints, _context.OpcUaEndpointId);

            var response = client.Execute(request);
            Assert.NotNull(response);
            Assert.True(response.IsSuccessful, "Get /registry/v2/endpoints/{endpointId}/activate failed!");

            if (!response.IsSuccessful) {
                _output.WriteLine($"StatusCode: {response.StatusCode}");
                _output.WriteLine($"ErrorMessage: {response.ErrorMessage}");
            }

            Assert.Empty(response.Content);
        }

        [Fact, PriorityOrder(6)]
        public void Test_CheckIfEndpointWasActivated_Expect_ActivatedAndConnected() {
            var accessToken = TestHelper.GetToken();
            var client = new RestClient(TestHelper.GetBaseUrl()) { Timeout = 30000 };

            var request = new RestRequest(Method.GET);
            request.AddHeader(TestConstants.HttpHeaderNames.Authorization, accessToken);
            request.Resource = TestConstants.APIRoutes.RegistryEndpoints;

            var response = client.Execute(request);
            Assert.NotNull(response);
            Assert.True(response.IsSuccessful, "Get /registry/v2/endpoints failed!");

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
    }
}
