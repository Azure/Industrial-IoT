// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Services.WebApi.Tests.Api.TestData.Json
{
    using Azure.IIoT.OpcUa.Publisher.Sdk.Services.Adapter;
    using Azure.IIoT.OpcUa.Services.Sdk.Clients;
    using Azure.IIoT.OpcUa.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Testing.Tests;
    using Furly.Extensions.Serializers;
    using Microsoft.Extensions.Logging;
    using System.Threading.Tasks;
    using Xunit;

    [Collection(ReadCollection.Name)]
    public class BrowseStreamTests
    {
        public BrowseStreamTests(WebAppFixture factory, TestDataServer server)
        {
            _factory = factory;
            _server = server;
        }

        private BrowseStreamTests<string> GetTests()
        {
            var client = _factory.CreateClient(); // Call to create server
            var registry = _factory.Resolve<IEndpointManager>();
            var endpointId = registry.RegisterEndpointAsync(_server.GetConnection().Endpoint).Result;
            var serializer = _factory.Resolve<IJsonSerializer>();
            return new BrowseStreamTests<string>(() => // Create an adapter over the api
                new TwinWebApiAdapter(
                    new TwinServiceClient(_factory,
                    new TestConfig(client.BaseAddress), serializer)), endpointId);
        }

        private readonly WebAppFixture _factory;
        private readonly TestDataServer _server;

        [Fact]
        public Task NodeBrowseInRootTest1Async()
        {
            return GetTests().NodeBrowseInRootTest1Async();
        }

        [Fact]
        public Task NodeBrowseInRootTest2Async()
        {
            return GetTests().NodeBrowseInRootTest2Async();
        }

        [Fact]
        public Task NodeBrowseBoilersObjectsTest1Async()
        {
            return GetTests().NodeBrowseBoilersObjectsTest1Async();
        }

        [Fact]
        public Task NodeBrowseDataAccessObjectsTest1Async()
        {
            return GetTests().NodeBrowseDataAccessObjectsTest1Async();
        }

        [Fact]
        public Task NodeBrowseStaticScalarVariablesTestAsync()
        {
            return GetTests().NodeBrowseStaticScalarVariablesTestAsync();
        }

        [Fact]
        public Task NodeBrowseStaticArrayVariablesTestAsync()
        {
            return GetTests().NodeBrowseStaticArrayVariablesTestAsync();
        }

        [Fact]
        public Task NodeBrowseStaticScalarVariablesTestWithFilter1Async()
        {
            return GetTests().NodeBrowseStaticScalarVariablesTestWithFilter1Async();
        }

        [Fact]
        public Task NodeBrowseStaticScalarVariablesTestWithFilter2Async()
        {
            return GetTests().NodeBrowseStaticScalarVariablesTestWithFilter2Async();
        }

        [Fact]
        public Task NodeBrowseStaticScalarVariablesTestWithFilter3Async()
        {
            return GetTests().NodeBrowseStaticScalarVariablesTestWithFilter3Async();
        }

        [Fact]
        public Task NodeBrowseStaticScalarVariablesTestWithFilter4Async()
        {
            return GetTests().NodeBrowseStaticScalarVariablesTestWithFilter4Async();
        }

        [Fact]
        public Task NodeBrowseStaticScalarVariablesTestWithFilter5Async()
        {
            return GetTests().NodeBrowseStaticScalarVariablesTestWithFilter5Async();
        }

        [Fact]
        public Task NodeBrowseDiagnosticsNoneTestAsync()
        {
            return GetTests().NodeBrowseDiagnosticsNoneTestAsync();
        }

        [Fact]
        public Task NodeBrowseDiagnosticsStatusTestAsync()
        {
            return GetTests().NodeBrowseDiagnosticsStatusTestAsync();
        }

        [Fact]
        public Task NodeBrowseDiagnosticsInfoTestAsync()
        {
            return GetTests().NodeBrowseDiagnosticsInfoTestAsync();
        }

        [Fact]
        public Task NodeBrowseDiagnosticsVerboseTestAsync()
        {
            return GetTests().NodeBrowseDiagnosticsVerboseTestAsync();
        }
    }
}
