// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.WebApi.Tests.Sdk.TestData.Json
{
    using Azure.IIoT.OpcUa.Publisher.Service.WebApi.Tests.Clients;
    using Azure.IIoT.OpcUa.Publisher.Service.Sdk;
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Testing.Tests;
    using Autofac;
    using System;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;

    [Collection(ReadCollection.Name)]
    public sealed class BrowseControllerTest : IClassFixture<WebAppFixture>, IDisposable
    {
        public BrowseControllerTest(WebAppFixture factory, TestDataServer server, ITestOutputHelper output)
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
                new TwinWebApiAdapter(_client.Resolve<ITwinServiceApi>()), endpointId);
        }

        private readonly WebAppFixture _factory;
        private readonly TestDataServer _server;
        private readonly IContainer _client;

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
        public Task NodeBrowseFirstInRootTest1Async()
        {
            return GetTests().NodeBrowseFirstInRootTest1Async();
        }

        [Fact]
        public Task NodeBrowseFirstInRootTest2Async()
        {
            return GetTests().NodeBrowseFirstInRootTest2Async();
        }

        [Fact]
        public Task NodeBrowseBoilersObjectsTest1Async()
        {
            return GetTests().NodeBrowseBoilersObjectsTest1Async();
        }

        [Fact]
        public Task NodeBrowseBoilersObjectsTest2Async()
        {
            return GetTests().NodeBrowseBoilersObjectsTest2Async();
        }

        [Fact]
        public Task NodeBrowseDataAccessObjectsTest1Async()
        {
            return GetTests().NodeBrowseDataAccessObjectsTest1Async();
        }

        [Fact]
        public Task NodeBrowseDataAccessObjectsTest2Async()
        {
            return GetTests().NodeBrowseDataAccessObjectsTest2Async();
        }

        [Fact]
        public Task NodeBrowseDataAccessObjectsTest3Async()
        {
            return GetTests().NodeBrowseDataAccessObjectsTest3Async();
        }

        [Fact]
        public Task NodeBrowseDataAccessObjectsTest4Async()
        {
            return GetTests().NodeBrowseDataAccessObjectsTest4Async();
        }

        [Fact]
        public Task NodeBrowseDataAccessFC1001Test1Async()
        {
            return GetTests().NodeBrowseDataAccessFC1001Test1Async();
        }

        [Fact]
        public Task NodeBrowseDataAccessFC1001Test2Async()
        {
            return GetTests().NodeBrowseDataAccessFC1001Test2Async();
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
        public Task NodeBrowseStaticArrayVariablesWithValuesTestAsync()
        {
            return GetTests().NodeBrowseStaticArrayVariablesWithValuesTestAsync();
        }

        [Fact]
        public Task NodeBrowseStaticArrayVariablesRawModeTestAsync()
        {
            return GetTests().NodeBrowseStaticArrayVariablesRawModeTestAsync();
        }

        [Fact]
        public Task NodeBrowseContinuationTest1Async()
        {
            return GetTests().NodeBrowseContinuationTest1Async();
        }

        [Fact]
        public Task NodeBrowseContinuationTest2Async()
        {
            return GetTests().NodeBrowseContinuationTest2Async();
        }

        [Fact]
        public Task NodeBrowseContinuationTest3Async()
        {
            return GetTests().NodeBrowseContinuationTest3Async();
        }

        [Fact]
        public Task NodeBrowseContinuationTest4Async()
        {
            return GetTests().NodeBrowseContinuationTest4Async();
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
