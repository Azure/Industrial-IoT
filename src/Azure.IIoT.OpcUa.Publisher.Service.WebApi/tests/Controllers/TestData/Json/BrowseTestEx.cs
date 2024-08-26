// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.WebApi.Tests.Controllers.TestData.Json
{
    using Azure.IIoT.OpcUa.Publisher.Service.WebApi.Tests.Clients;
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Testing.Tests;
    using Autofac;
    using System;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;

    [Collection(ReadCollection.Name)]
    public sealed class BrowseTestEx : IClassFixture<WebAppFixture>, IDisposable
    {
        public BrowseTestEx(WebAppFixture factory, TestDataServer server, ITestOutputHelper output)
        {
            _factory = factory;
            _server = server;
            _client = factory.CreateClientScope(output, TestSerializerType.NewtonsoftJson);
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        private BrowseServicesTests<string> GetTests()
        {
            var client = _factory.CreateClient(); // Call to create server
            var registry = _factory.Resolve<IEndpointManager<string>>();
            var endpointId = registry.RegisterEndpointAsync(_server.GetConnection().Endpoint).Result;
            return new BrowseServicesTests<string>(() => // Create an adapter over the api
                new TwinWebApiAdapter(_client.Resolve<ControllerTestClient>()), endpointId);
        }

        private readonly WebAppFixture _factory;
        private readonly TestDataServer _server;
        private readonly IContainer _client;

        [Fact]
        public async Task NodeBrowseInRootTest2Async()
        {
            await GetTests().NodeBrowseInRootTest2Async();
        }

        [Fact]
        public async Task NodeBrowseBoilersObjectsTest1Async()
        {
            await GetTests().NodeBrowseBoilersObjectsTest1Async();
        }

        [Fact]
        public async Task NodeBrowseDataAccessObjectsTest3Async()
        {
            await GetTests().NodeBrowseDataAccessObjectsTest3Async();
        }

        [Fact]
        public async Task NodeBrowseDataAccessObjectsTest4Async()
        {
            await GetTests().NodeBrowseDataAccessObjectsTest4Async();
        }

        [Fact]
        public async Task NodeBrowseDataAccessFC1001Test2Async()
        {
            await GetTests().NodeBrowseDataAccessFC1001Test2Async();
        }

        [Fact]
        public async Task NodeBrowseStaticScalarVariablesTestAsync()
        {
            await GetTests().NodeBrowseStaticScalarVariablesTestAsync();
        }

        [Fact]
        public async Task NodeBrowseStaticArrayVariablesTestAsync()
        {
            await GetTests().NodeBrowseStaticArrayVariablesTestAsync();
        }

        [Fact]
        public async Task NodeBrowseStaticArrayVariablesWithValuesTestAsync()
        {
            await GetTests().NodeBrowseStaticArrayVariablesWithValuesTestAsync();
        }

        [Fact]
        public async Task NodeBrowseDiagnosticsStatusTestAsync()
        {
            await GetTests().NodeBrowseDiagnosticsStatusTestAsync();
        }

        [Fact]
        public async Task NodeBrowseDiagnosticsVerboseTestAsync()
        {
            await GetTests().NodeBrowseDiagnosticsVerboseTestAsync();
        }
    }
}
