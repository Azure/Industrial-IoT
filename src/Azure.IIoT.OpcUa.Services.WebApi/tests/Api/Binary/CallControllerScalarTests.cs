// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Services.WebApi.Tests.Api.Binary
{
    using Azure.IIoT.OpcUa.Publisher.Sdk.Services.Adapter;
    using Azure.IIoT.OpcUa.Services.Sdk.Clients;
    using Azure.IIoT.OpcUa.Services.WebApi.Tests;
    using Azure.IIoT.OpcUa.Services.WebApi.Tests.Api;
    using Azure.IIoT.OpcUa.Models;
    using Azure.IIoT.OpcUa.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Testing.Tests;
    using Furly.Extensions.Serializers;
    using Furly.Extensions.Utils;
    using Microsoft.Azure.IIoT.Http.Default;
    using Microsoft.Extensions.Logging;
    using Opc.Ua;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading.Tasks;
    using Xunit;

    [Collection(WriteBinaryCollection.Name)]
    public class CallControllerScalarTests : IClassFixture<WebApiTestFixture>
    {
        public CallControllerScalarTests(WebApiTestFixture factory, TestDataServer server)
        {
            _factory = factory;
            _server = server;
            _hostEntry = Try.Op(() => Dns.GetHostEntry(Utils.GetHostName()))
                ?? Try.Op(() => Dns.GetHostEntry("localhost"));
        }

        private CallScalarMethodTests<string> GetTests()
        {
            var client = _factory.CreateClient(); // Call to create server
            var registry = _factory.Resolve<IEndpointManager>();
            var endpointId = registry.RegisterEndpointAsync(Endpoint).Result;
            var log = _factory.Resolve<ILogger>();
            var serializer = _factory.Resolve<IBinarySerializer>();
            return new CallScalarMethodTests<string>(() => // Create an adapter over the api
                new TwinWebApiAdapter(
                    new TwinServiceClient(new HttpClient(_factory, log),
                    new TestConfig(client.BaseAddress), serializer)), endpointId);
        }

        public EndpointModel Endpoint => new()
        {
            Url = $"opc.tcp://{_hostEntry?.HostName ?? "localhost"}:{_server.Port}/UA/SampleServer",
            AlternativeUrls = _hostEntry?.AddressList
                .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork)
                .Select(ip => $"opc.tcp://{ip}:{_server.Port}/UA/SampleServer").ToHashSet(),
            Certificate = _server.Certificate?.RawData?.ToThumbprint()
        };

        private readonly WebApiTestFixture _factory;
        private readonly TestDataServer _server;
        private readonly IPHostEntry _hostEntry;

        [Fact]
        public async Task NodeMethodMetadataStaticScalarMethod1TestAsync()
        {
            await GetTests().NodeMethodMetadataStaticScalarMethod1TestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeMethodMetadataStaticScalarMethod2TestAsync()
        {
            await GetTests().NodeMethodMetadataStaticScalarMethod2TestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeMethodMetadataStaticScalarMethod3TestAsync()
        {
            await GetTests().NodeMethodMetadataStaticScalarMethod3TestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeMethodMetadataStaticScalarMethod3WithBrowsePathTest1Async()
        {
            await GetTests().NodeMethodMetadataStaticScalarMethod3WithBrowsePathTest1Async().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeMethodMetadataStaticScalarMethod3WithBrowsePathTest2Async()
        {
            await GetTests().NodeMethodMetadataStaticScalarMethod3WithBrowsePathTest2Async().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeMethodCallStaticScalarMethod1Test1Async()
        {
            await GetTests().NodeMethodCallStaticScalarMethod1Test1Async().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeMethodCallStaticScalarMethod1Test2Async()
        {
            await GetTests().NodeMethodCallStaticScalarMethod1Test2Async().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeMethodCallStaticScalarMethod1Test3Async()
        {
            await GetTests().NodeMethodCallStaticScalarMethod1Test3Async().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeMethodCallStaticScalarMethod1Test4Async()
        {
            await GetTests().NodeMethodCallStaticScalarMethod1Test4Async().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeMethodCallStaticScalarMethod1Test5Async()
        {
            await GetTests().NodeMethodCallStaticScalarMethod1Test5Async().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeMethodCallStaticScalarMethod2Test1Async()
        {
            await GetTests().NodeMethodCallStaticScalarMethod2Test1Async().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeMethodCallStaticScalarMethod2Test2Async()
        {
            await GetTests().NodeMethodCallStaticScalarMethod2Test2Async().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeMethodCallStaticScalarMethod3Test1Async()
        {
            await GetTests().NodeMethodCallStaticScalarMethod3Test1Async().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeMethodCallStaticScalarMethod3Test2Async()
        {
            await GetTests().NodeMethodCallStaticScalarMethod3Test2Async().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeMethodCallStaticScalarMethod3WithBrowsePathNoIdsTestAsync()
        {
            await GetTests().NodeMethodCallStaticScalarMethod3WithBrowsePathNoIdsTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeMethodCallStaticScalarMethod3WithObjectIdAndBrowsePathTestAsync()
        {
            await GetTests().NodeMethodCallStaticScalarMethod3WithObjectIdAndBrowsePathTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeMethodCallStaticScalarMethod3WithObjectIdAndMethodIdAndBrowsePathTestAsync()
        {
            await GetTests().NodeMethodCallStaticScalarMethod3WithObjectIdAndMethodIdAndBrowsePathTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeMethodCallStaticScalarMethod3WithObjectPathAndMethodIdAndBrowsePathTestAsync()
        {
            await GetTests().NodeMethodCallStaticScalarMethod3WithObjectPathAndMethodIdAndBrowsePathTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeMethodCallStaticScalarMethod3WithObjectIdAndPathAndMethodIdAndPathTestAsync()
        {
            await GetTests().NodeMethodCallStaticScalarMethod3WithObjectIdAndPathAndMethodIdAndPathTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeMethodCallBoiler2ResetTestAsync()
        {
            await GetTests().NodeMethodCallBoiler2ResetTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeMethodCallBoiler1ResetTestAsync()
        {
            await GetTests().NodeMethodCallBoiler1ResetTestAsync().ConfigureAwait(false);
        }
    }
}
