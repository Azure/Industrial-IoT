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

    [Collection(WriteCollection.Name)]
    public sealed class MetadataScalarTests : IClassFixture<WebAppFixture>, IDisposable
    {
        public MetadataScalarTests(WebAppFixture factory, TestDataServer server, ITestOutputHelper output)
        {
            _factory = factory;
            _server = server;
            _client = factory.CreateClientScope(output, TestSerializerType.NewtonsoftJson);
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        private CallScalarMethodTests<string> GetTests()
        {
            var client = _factory.CreateClient(); // Call to create server
            var registry = _factory.Resolve<IEndpointManager<string>>();
            var endpointId = registry.RegisterEndpointAsync(_server.GetConnection().Endpoint).Result;
            return new CallScalarMethodTests<string>(() => // Create an adapter over the api
                new TwinWebApiAdapter(_client.Resolve<ITwinServiceApi>()), endpointId, newMetadata: true);
        }

        private readonly WebAppFixture _factory;
        private readonly TestDataServer _server;
        private readonly IContainer _client;

        [Fact]
        public Task NodeMethodMetadataStaticScalarMethod1TestAsync()
        {
            return GetTests().NodeMethodMetadataStaticScalarMethod1TestAsync();
        }

        [Fact]
        public Task NodeMethodMetadataStaticScalarMethod2TestAsync()
        {
            return GetTests().NodeMethodMetadataStaticScalarMethod2TestAsync();
        }

        [Fact]
        public Task NodeMethodMetadataStaticScalarMethod3TestAsync()
        {
            return GetTests().NodeMethodMetadataStaticScalarMethod3TestAsync();
        }

        [Fact]
        public Task NodeMethodMetadataStaticScalarMethod3WithBrowsePathTest1Async()
        {
            return GetTests().NodeMethodMetadataStaticScalarMethod3WithBrowsePathTest1Async();
        }

        [Fact]
        public Task NodeMethodMetadataStaticScalarMethod3WithBrowsePathTest2Async()
        {
            return GetTests().NodeMethodMetadataStaticScalarMethod3WithBrowsePathTest2Async();
        }
    }
}
