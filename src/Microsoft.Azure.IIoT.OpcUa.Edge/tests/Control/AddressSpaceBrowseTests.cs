// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Control {
    using Microsoft.Azure.IIoT.OpcUa.Edge.Tests;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Services;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Testing.Fixtures;
    using Microsoft.Azure.IIoT.OpcUa.Testing.Tests;
    using System.Net;
    using System.Threading.Tasks;
    using Xunit;

    [Collection(ReadCollection.Name)]
    public class AddressSpaceBrowseTests {

        public AddressSpaceBrowseTests(TestServerFixture server) {
            _server = server;
        }

        private BrowseServicesTests<EndpointModel> GetTests() {
            return new BrowseServicesTests<EndpointModel>(
                () => new AddressSpaceServices(_server.Client,
                    new JsonVariantEncoder(), _server.Logger),
                new EndpointModel {
                    Url = $"opc.tcp://{Dns.GetHostName()}:{_server.Port}/UA/SampleServer"
                });
        }

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
