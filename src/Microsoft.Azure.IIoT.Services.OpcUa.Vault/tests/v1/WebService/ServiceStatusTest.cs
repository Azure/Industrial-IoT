// Copyright (c) Microsoft. All rights reserved.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.Test.Helpers;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.Test.Helpers.Http;
using System.Net;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.Test.WebService
{
    public class ServiceStatusTest
    {
        private readonly ITestOutputHelper _log;
        private readonly IHttpClient _httpClient;

        public ServiceStatusTest(ITestOutputHelper log)
        {
            this._log = log;
            this._httpClient = new HttpClient(this._log);
        }

        /// <summary>
        /// Integration test using a real HTTP instance.
        /// Bootstrap a real HTTP server and test a request to the
        /// status endpoint.
        /// </summary>
        [Fact(Skip = "not yet implemented"), Trait(Constants.Type, Constants.IntegrationTest)]
        public void TheServiceIsHealthyViaHttpServer()
        {
            // Arrange
            var address = WebServiceHost.GetBaseAddress();
            var host = new WebHostBuilder()
                .UseUrls(address)
                .UseKestrel()
                .UseStartup<Startup>()
                .Build();
            host.Start();

            // Act
            var request = new HttpRequest(address + "/v1/status");
            request.AddHeader("X-Foo", "Bar");
            var response = this._httpClient.GetAsync(request).Result;

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        /// <summary>
        /// Integration test using a test server.
        /// Bootstrap a test server and test a request to the
        /// status endpoint
        /// </summary>
        [Fact(Skip = "not yet implemented"), Trait(Constants.Type, Constants.IntegrationTest)]
        public void TheServiceIsHealthyViaTestServer()
        {
            // Arrange
            var hostBuilder = new WebHostBuilder().UseStartup<Startup>();

            using (var server = new TestServer(hostBuilder))
            {
                // Act
                var request = server.CreateRequest("/v1/status");
                request.AddHeader("X-Foo", "Bar");
                var response = request.GetAsync().Result;

                // Assert
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }
    }
}
