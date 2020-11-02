// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatform_E2E_Tests
{
    using System;
    using Xunit;
    using System.Collections;
    using RestSharp;
    using Xunit.Abstractions;

    public class DummyTests
    {
        private readonly ITestOutputHelper _output;

        public DummyTests(ITestOutputHelper output) {
            _output = output ?? throw new ArgumentNullException(nameof(output));
        }

        [Fact]
        public void Test_CollectOAuthToken() {
            var token = TestHelper.GetToken();
            Assert.NotEmpty(token);
        }

        [Fact]
        public void Test_GetApplicationsFromRegistry_ExpectEmptyCollection() {

            var accessToken = TestHelper.GetToken();

            var client = new RestClient(TestHelper.GetBaseUrl()) {Timeout = 30};

            var request = new RestRequest(Method.GET);
            request.AddHeader("Authorization", $"Bearer {accessToken}");
            request.Resource = TestConstants.APIRoutes.RegistryApplications;

            var response = client.Execute(request);
            Assert.NotNull(response);
            Assert.True(response.IsSuccessful);
            if (!response.IsSuccessful) {
                _output.WriteLine(response.ErrorMessage);
            }
            Assert.Empty(response.Content);
        }
    }
}
