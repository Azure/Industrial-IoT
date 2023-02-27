// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Services.WebApi.Api.TestData.Json
{
    using Azure.IIoT.OpcUa.Services.WebApi.Clients;
    using Azure.IIoT.OpcUa.Services.Sdk.Clients;
    using Azure.IIoT.OpcUa.Services.WebApi;
    using Azure.IIoT.OpcUa.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Testing.Tests;
    using Furly.Extensions.Serializers;
    using System.Threading.Tasks;
    using Xunit;

    [Collection(ReadCollection.Name)]
    public class BrowsePathControllerTest : IClassFixture<WebAppFixture>
    {
        public BrowsePathControllerTest(WebAppFixture factory, TestDataServer server)
        {
            _factory = factory;
            _server = server;
        }

        private BrowsePathTests<string> GetTests()
        {
            var client = _factory.CreateClient(); // Call to create server
            var registry = _factory.Resolve<IEndpointManager>();
            var endpointId = registry.RegisterEndpointAsync(_server.GetConnection().Endpoint).Result;
            var serializer = _factory.Resolve<IJsonSerializer>();
            return new BrowsePathTests<string>(() => // Create an adapter over the api
                new TwinWebApiAdapter(
                    new TwinServiceClient(_factory,
                    new TestConfig(client.BaseAddress), serializer)), endpointId);
        }

        private readonly WebAppFixture _factory;
        private readonly TestDataServer _server;

        [Fact]
        public Task NodeBrowsePathStaticScalarMethod3Test1Async()
        {
            return GetTests().NodeBrowsePathStaticScalarMethod3Test1Async();
        }

        [Fact]
        public Task NodeBrowsePathStaticScalarMethod3Test2Async()
        {
            return GetTests().NodeBrowsePathStaticScalarMethod3Test2Async();
        }

        [Fact]
        public Task NodeBrowsePathStaticScalarMethod3Test3Async()
        {
            return GetTests().NodeBrowsePathStaticScalarMethod3Test3Async();
        }

        [Fact]
        public Task NodeBrowsePathStaticScalarMethodsTestAsync()
        {
            return GetTests().NodeBrowsePathStaticScalarMethodsTestAsync();
        }
    }
}
