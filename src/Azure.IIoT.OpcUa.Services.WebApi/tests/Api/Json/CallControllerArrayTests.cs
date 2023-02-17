// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Services.WebApi.Tests.Api.Json {
    using Azure.IIoT.OpcUa.Services.WebApi.Tests.Api;
    using Azure.IIoT.OpcUa.Services.WebApi.Tests;
    using Azure.IIoT.OpcUa.Api.Clients;
    using Azure.IIoT.OpcUa.Api.Models;
    using Azure.IIoT.OpcUa.Api.Publisher.Adapter;
    using Azure.IIoT.OpcUa.Protocol;
    using Azure.IIoT.OpcUa.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Testing.Tests;
    using Microsoft.Azure.IIoT.Http.Default;
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Azure.IIoT.Utils;
    using Opc.Ua;
    using Serilog;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading.Tasks;
    using Xunit;

    [Collection(WriteJsonCollection.Name)]
    public class CallControllerArrayTests : IClassFixture<WebApiTestFixture> {

        public CallControllerArrayTests(WebApiTestFixture factory, TestServerFixture server) {
            _factory = factory;
            _server = server;
            _hostEntry = Try.Op(() => Dns.GetHostEntry(Utils.GetHostName()))
                ?? Try.Op(() => Dns.GetHostEntry("localhost"));
        }

        private CallArrayMethodTests<string> GetTests() {
            var client = _factory.CreateClient(); // Call to create server
            var registry = _factory.Resolve<IEndpointManager>();
            var endpointId = registry.RegisterEndpointAsync(Endpoint).Result;
            var log = _factory.Resolve<ILogger>();
            var serializer = _factory.Resolve<IJsonSerializer>();
            return new CallArrayMethodTests<string>(() => // Create an adapter over the api
                new TwinServicesApiAdapter(
                    new TwinServiceClient(new HttpClient(_factory, log),
                    new TestConfig(client.BaseAddress), serializer)), endpointId);
        }

        public EndpointModel Endpoint => new() {
            Url = $"opc.tcp://{_hostEntry?.HostName ?? "localhost"}:{_server.Port}/UA/SampleServer",
                AlternativeUrls = _hostEntry?.AddressList
                .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork)
                .Select(ip => $"opc.tcp://{ip}:{_server.Port}/UA/SampleServer").ToHashSet(),
                Certificate = _server.Certificate?.RawData?.ToThumbprint()
        };

        private readonly WebApiTestFixture _factory;
        private readonly TestServerFixture _server;
        private readonly IPHostEntry _hostEntry;

        [Fact]
        public async Task NodeMethodMetadataStaticArrayMethod1TestAsync() {
            await GetTests().NodeMethodMetadataStaticArrayMethod1TestAsync();
        }

        [Fact]
        public async Task NodeMethodMetadataStaticArrayMethod2TestAsync() {
            await GetTests().NodeMethodMetadataStaticArrayMethod2TestAsync();
        }

        [Fact]
        public async Task NodeMethodMetadataStaticArrayMethod3TestAsync() {
            await GetTests().NodeMethodMetadataStaticArrayMethod3TestAsync();
        }

        [Fact]
        public async Task NodeMethodCallStaticArrayMethod1Test1Async() {
            await GetTests().NodeMethodCallStaticArrayMethod1Test1Async();
        }

        [Fact]
        public async Task NodeMethodCallStaticArrayMethod1Test2Async() {
            await GetTests().NodeMethodCallStaticArrayMethod1Test2Async();
        }

        [Fact]
        public async Task NodeMethodCallStaticArrayMethod1Test3Async() {
            await GetTests().NodeMethodCallStaticArrayMethod1Test3Async();
        }

        [Fact]
        public async Task NodeMethodCallStaticArrayMethod1Test4Async() {
            await GetTests().NodeMethodCallStaticArrayMethod1Test4Async();
        }

        [Fact]
        public async Task NodeMethodCallStaticArrayMethod1Test5Async() {
            await GetTests().NodeMethodCallStaticArrayMethod1Test5Async();
        }

        [Fact]
        public async Task NodeMethodCallStaticArrayMethod2Test1Async() {
            await GetTests().NodeMethodCallStaticArrayMethod2Test1Async();
        }

        [Fact]
        public async Task NodeMethodCallStaticArrayMethod2Test2Async() {
            await GetTests().NodeMethodCallStaticArrayMethod2Test2Async();
        }

        [Fact]
        public async Task NodeMethodCallStaticArrayMethod2Test3Async() {
            await GetTests().NodeMethodCallStaticArrayMethod2Test3Async();
        }

        [Fact]
        public async Task NodeMethodCallStaticArrayMethod2Test4Async() {
            await GetTests().NodeMethodCallStaticArrayMethod2Test4Async();
        }

        [Fact]
        public async Task NodeMethodCallStaticArrayMethod3Test1Async() {
            await GetTests().NodeMethodCallStaticArrayMethod3Test1Async();
        }

        [Fact]
        public async Task NodeMethodCallStaticArrayMethod3Test2Async() {
            await GetTests().NodeMethodCallStaticArrayMethod3Test2Async();
        }

        [Fact]
        public async Task NodeMethodCallStaticArrayMethod3Test3Async() {
            await GetTests().NodeMethodCallStaticArrayMethod3Test3Async();
        }

    }
}
