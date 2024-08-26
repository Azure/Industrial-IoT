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

    [Collection(WriteCollection.Name)]
    public sealed class CallControllerArrayTests : IClassFixture<WebAppFixture>, IDisposable
    {
        public CallControllerArrayTests(WebAppFixture factory, TestDataServer server, ITestOutputHelper output)
        {
            _factory = factory;
            _server = server;
            _client = factory.CreateClientScope(output, TestSerializerType.MsgPack);
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        private CallArrayMethodTests<string> GetTests()
        {
            var client = _factory.CreateClient(); // Call to create server
            var registry = _factory.Resolve<IEndpointManager<string>>();
            var endpointId = registry.RegisterEndpointAsync(_server.GetConnection().Endpoint).Result;
            return new CallArrayMethodTests<string>(() => // Create an adapter over the api
                new TwinWebApiAdapter(_client.Resolve<ITwinServiceApi>()), endpointId);
        }

        private readonly WebAppFixture _factory;
        private readonly TestDataServer _server;
        private readonly IContainer _client;

        [Fact]
        public Task NodeMethodMetadataStaticArrayMethod1TestAsync()
        {
            return GetTests().NodeMethodMetadataStaticArrayMethod1TestAsync();
        }

        [Fact]
        public Task NodeMethodMetadataStaticArrayMethod2TestAsync()
        {
            return GetTests().NodeMethodMetadataStaticArrayMethod2TestAsync();
        }

        [Fact]
        public Task NodeMethodMetadataStaticArrayMethod3TestAsync()
        {
            return GetTests().NodeMethodMetadataStaticArrayMethod3TestAsync();
        }

        [Fact]
        public Task NodeMethodCallStaticArrayMethod1Test1Async()
        {
            return GetTests().NodeMethodCallStaticArrayMethod1Test1Async();
        }

        [Fact]
        public Task NodeMethodCallStaticArrayMethod1Test2Async()
        {
            return GetTests().NodeMethodCallStaticArrayMethod1Test2Async();
        }

        [Fact]
        public Task NodeMethodCallStaticArrayMethod1Test3Async()
        {
            return GetTests().NodeMethodCallStaticArrayMethod1Test3Async();
        }

        [Fact]
        public Task NodeMethodCallStaticArrayMethod1Test4Async()
        {
            return GetTests().NodeMethodCallStaticArrayMethod1Test4Async();
        }

        [Fact]
        public Task NodeMethodCallStaticArrayMethod1Test5Async()
        {
            return GetTests().NodeMethodCallStaticArrayMethod1Test5Async();
        }

        [Fact]
        public Task NodeMethodCallStaticArrayMethod2Test1Async()
        {
            return GetTests().NodeMethodCallStaticArrayMethod2Test1Async();
        }

        [Fact]
        public Task NodeMethodCallStaticArrayMethod2Test2Async()
        {
            return GetTests().NodeMethodCallStaticArrayMethod2Test2Async();
        }

        [Fact]
        public Task NodeMethodCallStaticArrayMethod2Test3Async()
        {
            return GetTests().NodeMethodCallStaticArrayMethod2Test3Async();
        }

        [Fact]
        public Task NodeMethodCallStaticArrayMethod2Test4Async()
        {
            return GetTests().NodeMethodCallStaticArrayMethod2Test4Async();
        }

        [Fact]
        public Task NodeMethodCallStaticArrayMethod3Test1Async()
        {
            return GetTests().NodeMethodCallStaticArrayMethod3Test1Async();
        }

        [Fact]
        public Task NodeMethodCallStaticArrayMethod3Test2Async()
        {
            return GetTests().NodeMethodCallStaticArrayMethod3Test2Async();
        }

        [Fact]
        public Task NodeMethodCallStaticArrayMethod3Test3Async()
        {
            return GetTests().NodeMethodCallStaticArrayMethod3Test3Async();
        }
    }
}
