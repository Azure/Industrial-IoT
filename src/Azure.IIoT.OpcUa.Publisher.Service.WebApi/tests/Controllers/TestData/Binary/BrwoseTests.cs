// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.WebApi.Tests.Controllers.TestData.MsgPack
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
    public sealed class BrwoseTests : IClassFixture<WebAppFixture>, IDisposable
    {
        public BrwoseTests(WebAppFixture factory, TestDataServer server, ITestOutputHelper output)
        {
            _factory = factory;
            _server = server;
            _client = factory.CreateClientScope(output, TestSerializerType.MsgPack);
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
        public Task NodeBrowseStaticArrayVariablesWithValuesTestAsync()
        {
            return GetTests().NodeBrowseStaticArrayVariablesWithValuesTestAsync();
        }

        [Fact]
        public Task NodeBrowseDiagnosticsStatusTestAsync()
        {
            return GetTests().NodeBrowseDiagnosticsStatusTestAsync();
        }

        [Fact]
        public Task NodeBrowseDiagnosticsVerboseTestAsync()
        {
            return GetTests().NodeBrowseDiagnosticsVerboseTestAsync();
        }
    }
}
