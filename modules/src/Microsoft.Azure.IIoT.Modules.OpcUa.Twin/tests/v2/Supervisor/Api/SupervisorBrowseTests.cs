// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Twin.v2.Supervisor.Api {
    using Microsoft.Azure.IIoT.Modules.OpcUa.Twin.Tests;
    using Microsoft.Azure.IIoT.OpcUa.Api.Twin.Models;
    using Microsoft.Azure.IIoT.OpcUa.Testing.Fixtures;
    using Microsoft.Azure.IIoT.OpcUa.Testing.Tests;
    using Microsoft.Azure.IIoT.OpcUa.Twin;
    using System.Net;
    using System.Threading.Tasks;
    using Xunit;
    using Autofac;

    [Collection(ReadCollection.Name)]
    public class SupervisorBrowseTests : IClassFixture<TwinModuleFixture> {

        public SupervisorBrowseTests(TestServerFixture server, TwinModuleFixture module) {
            _server = server;
            _module = module;
        }

        private BrowseServicesTests<EndpointApiModel> GetTests() {
            return new BrowseServicesTests<EndpointApiModel>(
                () => _module.HubContainer.Resolve<IBrowseServices<EndpointApiModel>>(),
                new EndpointApiModel {
                    Url = $"opc.tcp://{Dns.GetHostName()}:{_server.Port}/UA/SampleServer",
                    Certificate = _server.Certificate?.RawData
                });
        }

        private readonly TestServerFixture _server;
        private readonly TwinModuleFixture _module;

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

        [Fact]
        public async Task NodeBrowseStaticArrayVariablesRawModeTest() {
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
