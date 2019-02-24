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

    [Collection(WriteCollection.Name)]
    public class AddressSpaceValueCallArrayTests {

        public AddressSpaceValueCallArrayTests(TestServerFixture server) {
            _server = server;
        }

        private CallArrayMethodTests<EndpointModel> GetTests() {
            return new CallArrayMethodTests<EndpointModel>(
                () => new AddressSpaceServices(_server.Client,
                    new JsonVariantEncoder(), _server.Logger),
                new EndpointModel {
                    Url = $"opc.tcp://{Dns.GetHostName()}:{_server.Port}/UA/SampleServer"
                });
        }

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
