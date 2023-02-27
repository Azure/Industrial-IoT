// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Services.WebApi.Api.TestData.Binary
{
    using Azure.IIoT.OpcUa.Services.WebApi;
    using Azure.IIoT.OpcUa.Services.WebApi.Clients;
    using Azure.IIoT.OpcUa.Services.Sdk.Clients;
    using Azure.IIoT.OpcUa.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Testing.Tests;
    using Furly.Extensions.Serializers;
    using System.Threading.Tasks;
    using Xunit;

    [Collection(WriteCollection.Name)]
    public class CallScalarTests : IClassFixture<WebAppFixture>
    {
        public CallScalarTests(WebAppFixture factory, TestDataServer server)
        {
            _factory = factory;
            _server = server;
        }

        private CallScalarMethodTests<string> GetTests()
        {
            var client = _factory.CreateClient(); // Call to create server
            var registry = _factory.Resolve<IEndpointManager>();
            var endpointId = registry.RegisterEndpointAsync(_server.GetConnection().Endpoint).Result;
            var serializer = _factory.Resolve<IBinarySerializer>();
            return new CallScalarMethodTests<string>(() => // Create an adapter over the api
                new TwinWebApiAdapter(
                    new TwinServiceClient(_factory,
                    new TestConfig(client.BaseAddress), serializer)), endpointId);
        }

        private readonly WebAppFixture _factory;
        private readonly TestDataServer _server;

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

        [Fact]
        public Task NodeMethodCallStaticScalarMethod1Test1Async()
        {
            return GetTests().NodeMethodCallStaticScalarMethod1Test1Async();
        }

        [Fact]
        public Task NodeMethodCallStaticScalarMethod1Test2Async()
        {
            return GetTests().NodeMethodCallStaticScalarMethod1Test2Async();
        }

        [Fact]
        public Task NodeMethodCallStaticScalarMethod1Test3Async()
        {
            return GetTests().NodeMethodCallStaticScalarMethod1Test3Async();
        }

        [Fact]
        public Task NodeMethodCallStaticScalarMethod1Test4Async()
        {
            return GetTests().NodeMethodCallStaticScalarMethod1Test4Async();
        }

        [Fact]
        public Task NodeMethodCallStaticScalarMethod1Test5Async()
        {
            return GetTests().NodeMethodCallStaticScalarMethod1Test5Async();
        }

        [Fact]
        public Task NodeMethodCallStaticScalarMethod2Test1Async()
        {
            return GetTests().NodeMethodCallStaticScalarMethod2Test1Async();
        }

        [Fact]
        public Task NodeMethodCallStaticScalarMethod2Test2Async()
        {
            return GetTests().NodeMethodCallStaticScalarMethod2Test2Async();
        }

        [Fact]
        public Task NodeMethodCallStaticScalarMethod3Test1Async()
        {
            return GetTests().NodeMethodCallStaticScalarMethod3Test1Async();
        }

        [Fact]
        public Task NodeMethodCallStaticScalarMethod3Test2Async()
        {
            return GetTests().NodeMethodCallStaticScalarMethod3Test2Async();
        }

        [Fact]
        public Task NodeMethodCallStaticScalarMethod3WithBrowsePathNoIdsTestAsync()
        {
            return GetTests().NodeMethodCallStaticScalarMethod3WithBrowsePathNoIdsTestAsync();
        }

        [Fact]
        public Task NodeMethodCallStaticScalarMethod3WithObjectIdAndBrowsePathTestAsync()
        {
            return GetTests().NodeMethodCallStaticScalarMethod3WithObjectIdAndBrowsePathTestAsync();
        }

        [Fact]
        public Task NodeMethodCallStaticScalarMethod3WithObjectIdAndMethodIdAndBrowsePathTestAsync()
        {
            return GetTests().NodeMethodCallStaticScalarMethod3WithObjectIdAndMethodIdAndBrowsePathTestAsync();
        }

        [Fact]
        public Task NodeMethodCallStaticScalarMethod3WithObjectPathAndMethodIdAndBrowsePathTestAsync()
        {
            return GetTests().NodeMethodCallStaticScalarMethod3WithObjectPathAndMethodIdAndBrowsePathTestAsync();
        }

        [Fact]
        public Task NodeMethodCallStaticScalarMethod3WithObjectIdAndPathAndMethodIdAndPathTestAsync()
        {
            return GetTests().NodeMethodCallStaticScalarMethod3WithObjectIdAndPathAndMethodIdAndPathTestAsync();
        }

        [Fact]
        public Task NodeMethodCallBoiler2ResetTestAsync()
        {
            return GetTests().NodeMethodCallBoiler2ResetTestAsync();
        }

        [Fact]
        public Task NodeMethodCallBoiler1ResetTestAsync()
        {
            return GetTests().NodeMethodCallBoiler1ResetTestAsync();
        }
    }
}
