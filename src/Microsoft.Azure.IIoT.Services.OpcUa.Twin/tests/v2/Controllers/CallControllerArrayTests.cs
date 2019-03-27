// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Twin.v2.Controllers {
    using Microsoft.Azure.IIoT.Http.Default;
    using Microsoft.Azure.IIoT.OpcUa.Api.Twin;
    using Microsoft.Azure.IIoT.OpcUa.Api.Twin.Clients;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Testing.Fixtures;
    using Microsoft.Azure.IIoT.OpcUa.Testing.Tests;
    using Serilog;
    using System.Net;
    using System.Threading.Tasks;
    using Xunit;

    [Collection(WriteCollection.Name)]
    public class CallControllerArrayTests : IClassFixture<WebAppFixture> {

        public CallControllerArrayTests(WebAppFixture factory, TestServerFixture server) {
            _factory = factory;
            _server = server;
        }

        private CallArrayMethodTests<string> GetTests() {
            var client = _factory.CreateClient(); // Call to create server
            var module = _factory.Resolve<ITestModule>();
            module.Endpoint = Endpoint;
            var log = _factory.Resolve<ILogger>();
            return new CallArrayMethodTests<string>(() => // Create an adapter over the api
                new TwinAdapter(
                    new TwinServiceClient(
                       new HttpClient(_factory, log), new TestConfig(client.BaseAddress), log),
                            log), "fakeid");
        }

        public EndpointModel Endpoint => new EndpointModel {
            Url = $"opc.tcp://{Dns.GetHostName()}:{_server.Port}/UA/SampleServer"
        };

        private readonly WebAppFixture _factory;
        private readonly TestServerFixture _server;

        [Fact]
        public async Task NodeMethodMetadataStaticArrayMethod1Test() {
            await GetTests().NodeMethodMetadataStaticArrayMethod1Test();
        }

        [Fact]
        public async Task NodeMethodMetadataStaticArrayMethod2Test() {
            await GetTests().NodeMethodMetadataStaticArrayMethod2Test();
        }

        [Fact]
        public async Task NodeMethodMetadataStaticArrayMethod3Test() {
            await GetTests().NodeMethodMetadataStaticArrayMethod3Test();
        }

        [Fact]
        public async Task NodeMethodCallStaticArrayMethod1Test1() {
            await GetTests().NodeMethodCallStaticArrayMethod1Test1();
        }

        [Fact]
        public async Task NodeMethodCallStaticArrayMethod1Test2() {
            await GetTests().NodeMethodCallStaticArrayMethod1Test2();
        }

        [Fact]
        public async Task NodeMethodCallStaticArrayMethod1Test3() {
            await GetTests().NodeMethodCallStaticArrayMethod1Test3();
        }

        [Fact]
        public async Task NodeMethodCallStaticArrayMethod1Test4() {
            await GetTests().NodeMethodCallStaticArrayMethod1Test4();
        }

        [Fact]
        public async Task NodeMethodCallStaticArrayMethod1Test5() {
            await GetTests().NodeMethodCallStaticArrayMethod1Test5();
        }

        [Fact]
        public async Task NodeMethodCallStaticArrayMethod2Test1() {
            await GetTests().NodeMethodCallStaticArrayMethod2Test1();
        }

        [Fact]
        public async Task NodeMethodCallStaticArrayMethod2Test2() {
            await GetTests().NodeMethodCallStaticArrayMethod2Test2();
        }

        [Fact]
        public async Task NodeMethodCallStaticArrayMethod2Test3() {
            await GetTests().NodeMethodCallStaticArrayMethod2Test3();
        }

        [Fact]
        public async Task NodeMethodCallStaticArrayMethod2Test4() {
            await GetTests().NodeMethodCallStaticArrayMethod2Test4();
        }

        [Fact]
        public async Task NodeMethodCallStaticArrayMethod3Test1() {
            await GetTests().NodeMethodCallStaticArrayMethod3Test1();
        }

        [Fact]
        public async Task NodeMethodCallStaticArrayMethod3Test2() {
            await GetTests().NodeMethodCallStaticArrayMethod3Test2();
        }

        [Fact]
        public async Task NodeMethodCallStaticArrayMethod3Test3() {
            await GetTests().NodeMethodCallStaticArrayMethod3Test3();
        }

    }
}
