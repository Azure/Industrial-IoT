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

    [Collection(ReadCollection.Name)]
    public class BrowseControllerTest : IClassFixture<WebAppFixture> {

        public BrowseControllerTest(WebAppFixture factory, TestServerFixture server) {
            _factory = factory;
            _server = server;
        }

        private BrowseServicesTests<string> GetTests() {
            var client = _factory.CreateClient(); // Call to create server
            var module = _factory.Resolve<ITestModule>();
            module.Endpoint = Endpoint;
            var log = _factory.Resolve<ILogger>();
            return new BrowseServicesTests<string>(() => // Create an adapter over the api
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
        public async Task NodeBrowseInRootTest1() {
            await GetTests().NodeBrowseInRootTest1();
        }

        [Fact]
        public async Task NodeBrowseFirstInRootTest1() {
            await GetTests().NodeBrowseFirstInRootTest1();
        }

        [Fact]
        public async Task NodeBrowseFirstInRootTest2() {
            await GetTests().NodeBrowseFirstInRootTest2();
        }

        [Fact]
        public async Task NodeBrowseBoilersObjectsTest1() {
            await GetTests().NodeBrowseBoilersObjectsTest1();
        }

        [Fact]
        public async Task NodeBrowseBoilersObjectsTest2() {
            await GetTests().NodeBrowseBoilersObjectsTest2();
        }

        [Fact]
        public async Task NodeBrowseStaticScalarVariablesTest() {
            await GetTests().NodeBrowseStaticScalarVariablesTest();
        }

        [Fact]
        public async Task NodeBrowseStaticArrayVariablesTest() {
            await GetTests().NodeBrowseStaticArrayVariablesTest();
        }

        [Fact]
        public async Task NodeBrowseStaticArrayVariablesWithValuesTest() {
            await GetTests().NodeBrowseStaticArrayVariablesWithValuesTest();
        }

        [SkippableFact]
        public async Task NodeBrowseStaticArrayVariablesRawModeTest() {
            Skip.If(true, "No API impl.");
            await GetTests().NodeBrowseStaticArrayVariablesRawModeTest();
        }

        [Fact]
        public async Task NodeBrowsePathStaticScalarMethod3Test1() {
            await GetTests().NodeBrowsePathStaticScalarMethod3Test1();
        }

        [Fact]
        public async Task NodeBrowsePathStaticScalarMethod3Test2() {
            await GetTests().NodeBrowsePathStaticScalarMethod3Test2();
        }

        [Fact]
        public async Task NodeBrowsePathStaticScalarMethod3Test3() {
            await GetTests().NodeBrowsePathStaticScalarMethod3Test3();
        }

        [Fact]
        public async Task NodeBrowsePathStaticScalarMethodsTest() {
            await GetTests().NodeBrowsePathStaticScalarMethodsTest();
        }

        [Fact]
        public async Task NodeBrowseDiagnosticsNoneTest() {
            await GetTests().NodeBrowseDiagnosticsNoneTest();
        }

        [Fact]
        public async Task NodeBrowseDiagnosticsStatusTest() {
            await GetTests().NodeBrowseDiagnosticsStatusTest();
        }

        [Fact]
        public async Task NodeBrowseDiagnosticsOperationsTest() {
            await GetTests().NodeBrowseDiagnosticsOperationsTest();
        }

        [Fact]
        public async Task NodeBrowseDiagnosticsVerboseTest() {
            await GetTests().NodeBrowseDiagnosticsVerboseTest();
        }
    }
}
