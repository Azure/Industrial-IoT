// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.WebApi.Tests.Sdk.TestData.Binary
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
    public sealed class BrowseStreamTests : IClassFixture<WebAppFixture>, IDisposable
    {
        public BrowseStreamTests(WebAppFixture factory, TestDataServer server, ITestOutputHelper output)
        {
            _factory = factory;
            _server = server;
            _client = factory.CreateClientScope(output, TestSerializerType.MsgPack);
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        private BrowseStreamTests<string> GetTests()
        {
            var client = _factory.CreateClient(); // Call to create server
            var registry = _factory.Resolve<IEndpointManager<string>>();
            var endpointId = registry.RegisterEndpointAsync(_server.GetConnection().Endpoint).Result;
            return new BrowseStreamTests<string>(() => // Create an adapter over the api
                new TwinWebApiAdapter(_client.Resolve<ITwinServiceApi>()), endpointId);
        }

        private readonly WebAppFixture _factory;
        private readonly TestDataServer _server;
        private readonly IContainer _client;

        [SkippableFact]
        public Task NodeBrowseInRootTest1Async()
        {
            Skip.If(true, "not implemented yet");
            return GetTests().NodeBrowseInRootTest1Async();
        }

        [SkippableFact]
        public Task NodeBrowseInRootTest2Async()
        {
            Skip.If(true, "not implemented yet");
            return GetTests().NodeBrowseInRootTest2Async();
        }

        [SkippableFact]
        public Task NodeBrowseBoilersObjectsTest1Async()
        {
            Skip.If(true, "not implemented yet");
            return GetTests().NodeBrowseBoilersObjectsTest1Async();
        }

        [SkippableFact]
        public Task NodeBrowseDataAccessObjectsTest1Async()
        {
            Skip.If(true, "not implemented yet");
            return GetTests().NodeBrowseDataAccessObjectsTest1Async();
        }

        [SkippableFact]
        public Task NodeBrowseStaticScalarVariablesTestAsync()
        {
            Skip.If(true, "not implemented yet");
            return GetTests().NodeBrowseStaticScalarVariablesTestAsync();
        }

        [SkippableFact]
        public Task NodeBrowseStaticArrayVariablesTestAsync()
        {
            Skip.If(true, "not implemented yet");
            return GetTests().NodeBrowseStaticArrayVariablesTestAsync();
        }

        [SkippableFact]
        public Task NodeBrowseStaticScalarVariablesTestWithFilter1Async()
        {
            Skip.If(true, "not implemented yet");
            return GetTests().NodeBrowseStaticScalarVariablesTestWithFilter1Async();
        }

        [SkippableFact]
        public Task NodeBrowseStaticScalarVariablesTestWithFilter2Async()
        {
            Skip.If(true, "not implemented yet");
            return GetTests().NodeBrowseStaticScalarVariablesTestWithFilter2Async();
        }

        [SkippableFact]
        public Task NodeBrowseStaticScalarVariablesTestWithFilter3Async()
        {
            Skip.If(true, "not implemented yet");
            return GetTests().NodeBrowseStaticScalarVariablesTestWithFilter3Async();
        }

        [SkippableFact]
        public Task NodeBrowseStaticScalarVariablesTestWithFilter4Async()
        {
            Skip.If(true, "not implemented yet");
            return GetTests().NodeBrowseStaticScalarVariablesTestWithFilter4Async();
        }

        [SkippableFact]
        public Task NodeBrowseStaticScalarVariablesTestWithFilter5Async()
        {
            Skip.If(true, "not implemented yet");
            return GetTests().NodeBrowseStaticScalarVariablesTestWithFilter5Async();
        }

        [SkippableFact]
        public Task NodeBrowseDiagnosticsNoneTestAsync()
        {
            Skip.If(true, "not implemented yet");
            return GetTests().NodeBrowseDiagnosticsNoneTestAsync();
        }

        [SkippableFact]
        public Task NodeBrowseDiagnosticsStatusTestAsync()
        {
            Skip.If(true, "not implemented yet");
            return GetTests().NodeBrowseDiagnosticsStatusTestAsync();
        }

        [SkippableFact]
        public Task NodeBrowseDiagnosticsInfoTestAsync()
        {
            Skip.If(true, "not implemented yet");
            return GetTests().NodeBrowseDiagnosticsInfoTestAsync();
        }

        [SkippableFact]
        public Task NodeBrowseDiagnosticsVerboseTestAsync()
        {
            Skip.If(true, "not implemented yet");
            return GetTests().NodeBrowseDiagnosticsVerboseTestAsync();
        }
    }
}
