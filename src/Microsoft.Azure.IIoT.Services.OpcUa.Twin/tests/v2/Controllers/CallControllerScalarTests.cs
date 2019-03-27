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
    public class CallControllerScalarTests : IClassFixture<WebAppFixture> {

        public CallControllerScalarTests(WebAppFixture factory, TestServerFixture server) {
            _factory = factory;
            _server = server;
        }

        private CallScalarMethodTests<string> GetTests() {
            var client = _factory.CreateClient(); // Call to create server
            var module = _factory.Resolve<ITestModule>();
            module.Endpoint = Endpoint;
            var log = _factory.Resolve<ILogger>();
            return new CallScalarMethodTests<string>(() => // Create an adapter over the api
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
        public async Task NodeMethodMetadataStaticScalarMethod1Test() {
            await GetTests().NodeMethodMetadataStaticScalarMethod1Test();
        }

        [Fact]
        public async Task NodeMethodMetadataStaticScalarMethod2Test() {
            await GetTests().NodeMethodMetadataStaticScalarMethod2Test();
        }

        [Fact]
        public async Task NodeMethodMetadataStaticScalarMethod3Test() {
            await GetTests().NodeMethodMetadataStaticScalarMethod3Test();
        }

        [Fact]
        public async Task NodeMethodMetadataStaticScalarMethod3WithBrowsePathTest1() {
            await GetTests().NodeMethodMetadataStaticScalarMethod3WithBrowsePathTest1();
        }

        [Fact]
        public async Task NodeMethodMetadataStaticScalarMethod3WithBrowsePathTest2() {
            await GetTests().NodeMethodMetadataStaticScalarMethod3WithBrowsePathTest2();
        }

        [Fact]
        public async Task NodeMethodCallStaticScalarMethod1Test1() {
            await GetTests().NodeMethodCallStaticScalarMethod1Test1();
        }

        [Fact]
        public async Task NodeMethodCallStaticScalarMethod1Test2() {
            await GetTests().NodeMethodCallStaticScalarMethod1Test2();
        }

        [Fact]
        public async Task NodeMethodCallStaticScalarMethod1Test3() {
            await GetTests().NodeMethodCallStaticScalarMethod1Test3();
        }

        [Fact]
        public async Task NodeMethodCallStaticScalarMethod1Test4() {
            await GetTests().NodeMethodCallStaticScalarMethod1Test4();
        }

        [Fact]
        public async Task NodeMethodCallStaticScalarMethod1Test5() {
            await GetTests().NodeMethodCallStaticScalarMethod1Test5();
        }

        [Fact]
        public async Task NodeMethodCallStaticScalarMethod2Test1() {
            await GetTests().NodeMethodCallStaticScalarMethod2Test1();
        }

        [Fact]
        public async Task NodeMethodCallStaticScalarMethod2Test2() {
            await GetTests().NodeMethodCallStaticScalarMethod2Test2();
        }

        [Fact]
        public async Task NodeMethodCallStaticScalarMethod3Test1() {
            await GetTests().NodeMethodCallStaticScalarMethod3Test1();
        }

        [Fact]
        public async Task NodeMethodCallStaticScalarMethod3Test2() {
            await GetTests().NodeMethodCallStaticScalarMethod3Test2();
        }

        [Fact]
        public async Task NodeMethodCallStaticScalarMethod3WithBrowsePathNoIdsTest() {
            await GetTests().NodeMethodCallStaticScalarMethod3WithBrowsePathNoIdsTest();
        }

        [Fact]
        public async Task NodeMethodCallStaticScalarMethod3WithObjectIdAndBrowsePathTest() {
            await GetTests().NodeMethodCallStaticScalarMethod3WithObjectIdAndBrowsePathTest();
        }

        [Fact]
        public async Task NodeMethodCallStaticScalarMethod3WithObjectIdAndMethodIdAndBrowsePathTest() {
            await GetTests().NodeMethodCallStaticScalarMethod3WithObjectIdAndMethodIdAndBrowsePathTest();
        }

        [Fact]
        public async Task NodeMethodCallStaticScalarMethod3WithObjectPathAndMethodIdAndBrowsePathTest() {
            await GetTests().NodeMethodCallStaticScalarMethod3WithObjectPathAndMethodIdAndBrowsePathTest();
        }

        [Fact]
        public async Task NodeMethodCallStaticScalarMethod3WithObjectIdAndPathAndMethodIdAndPathTest() {
            await GetTests().NodeMethodCallStaticScalarMethod3WithObjectIdAndPathAndMethodIdAndPathTest();
        }

        [Fact]
        public async Task NodeMethodCallBoiler2ResetTest() {
            await GetTests().NodeMethodCallBoiler2ResetTest();
        }

        [Fact]
        public async Task NodeMethodCallBoiler1ResetTest() {
            await GetTests().NodeMethodCallBoiler1ResetTest();
        }
    }
}
