// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.WebApi.Controllers.TestData.Json
{
    using Azure.IIoT.OpcUa.Publisher.Service.WebApi;
    using Azure.IIoT.OpcUa.Publisher.Service.WebApi.Clients;
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Testing.Tests;
    using Furly.Extensions.Serializers;
    using System.Threading.Tasks;
    using Xunit;

    [Collection(ReadCollection.Name)]
    public class BrowseTestEx : IClassFixture<WebAppFixture>
    {
        public BrowseTestEx(WebAppFixture factory, TestDataServer server)
        {
            _factory = factory;
            _server = server;
        }

        private BrowseServicesTests<string> GetTests()
        {
            var client = _factory.CreateClient(); // Call to create server
            var registry = _factory.Resolve<IEndpointManager>();
            var endpointId = registry.RegisterEndpointAsync(_server.GetConnection().Endpoint).Result;
            var serializer = _factory.Resolve<IJsonSerializer>();
            return new BrowseServicesTests<string>(() => // Create an adapter over the api
                new TwinWebApiAdapter(
                    new ControllerTestClient(
                       _factory, new TestConfig(client.BaseAddress),
                            serializer)), endpointId);
        }

        private readonly WebAppFixture _factory;
        private readonly TestDataServer _server;

        [Fact]
        public async Task NodeBrowseInRootTest2Async()
        {
            await GetTests().NodeBrowseInRootTest2Async().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeBrowseBoilersObjectsTest1Async()
        {
            await GetTests().NodeBrowseBoilersObjectsTest1Async().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeBrowseDataAccessObjectsTest3Async()
        {
            await GetTests().NodeBrowseDataAccessObjectsTest3Async().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeBrowseDataAccessObjectsTest4Async()
        {
            await GetTests().NodeBrowseDataAccessObjectsTest4Async().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeBrowseDataAccessFC1001Test2Async()
        {
            await GetTests().NodeBrowseDataAccessFC1001Test2Async().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeBrowseStaticScalarVariablesTestAsync()
        {
            await GetTests().NodeBrowseStaticScalarVariablesTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeBrowseStaticArrayVariablesTestAsync()
        {
            await GetTests().NodeBrowseStaticArrayVariablesTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeBrowseStaticArrayVariablesWithValuesTestAsync()
        {
            await GetTests().NodeBrowseStaticArrayVariablesWithValuesTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeBrowseDiagnosticsStatusTestAsync()
        {
            await GetTests().NodeBrowseDiagnosticsStatusTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeBrowseDiagnosticsVerboseTestAsync()
        {
            await GetTests().NodeBrowseDiagnosticsVerboseTestAsync().ConfigureAwait(false);
        }
    }
}
