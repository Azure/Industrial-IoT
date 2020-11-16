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
    public class DummyTests
    {
        private readonly ITestOutputHelper _output;

        public DummyTests(ITestOutputHelper output) {
            _output = output ?? throw new ArgumentNullException(nameof(output));
        }

        [Fact, PriorityOrder(1)]
        public void Test_CollectOAuthToken() {
            var token = TestHelper.GetToken();
            Assert.NotEmpty(token);
        }

        [Fact, PriorityOrder(2)]
        public async void Test_RegisterOPCServer_Expect_ServerEndpointsCanBeRetrieved() {
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
            Assert.Equal(1, numberOfItems);
        }


    }
}
