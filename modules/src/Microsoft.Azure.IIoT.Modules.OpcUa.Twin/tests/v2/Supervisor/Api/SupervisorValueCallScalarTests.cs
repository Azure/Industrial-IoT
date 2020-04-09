// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Twin.v2.Supervisor.Api {
    using Microsoft.Azure.IIoT.Modules.OpcUa.Twin.Tests;
    using Microsoft.Azure.IIoT.OpcUa.Api.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.Testing.Fixtures;
    using Microsoft.Azure.IIoT.OpcUa.Testing.Tests;
    using Microsoft.Azure.IIoT.OpcUa.Twin;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using System.Net;
    using System.Threading.Tasks;
    using Xunit;
    using Autofac;

    [Collection(WriteCollection.Name)]
    public class SupervisorValueCallScalarTests : IClassFixture<TwinModuleFixture> {

        public SupervisorValueCallScalarTests(TestServerFixture server, TwinModuleFixture module) {
            _server = server;
            _module = module;
        }

        private CallScalarMethodTests<EndpointApiModel> GetTests() {
            return new CallScalarMethodTests<EndpointApiModel>(
                () => _module.HubContainer.Resolve<INodeServices<EndpointApiModel>>(),
                new EndpointApiModel {
                    Url = $"opc.tcp://{Dns.GetHostName()}:{_server.Port}/UA/SampleServer",
                    Certificate = _server.Certificate?.RawData?.ToThumbprint()
                });
        }

        private readonly TestServerFixture _server;
        private readonly TwinModuleFixture _module;

        [Fact]
        public async Task NodeMethodMetadataStaticScalarMethod1TestAsync() {
            await GetTests().NodeMethodMetadataStaticScalarMethod1TestAsync();
        }

        [Fact]
        public async Task NodeMethodMetadataStaticScalarMethod2TestAsync() {
            await GetTests().NodeMethodMetadataStaticScalarMethod2TestAsync();
        }

        [Fact]
        public async Task NodeMethodMetadataStaticScalarMethod3TestAsync() {
            await GetTests().NodeMethodMetadataStaticScalarMethod3TestAsync();
        }

        [Fact]
        public async Task NodeMethodMetadataStaticScalarMethod3WithBrowsePathTest1Async() {
            await GetTests().NodeMethodMetadataStaticScalarMethod3WithBrowsePathTest1Async();
        }

        [Fact]
        public async Task NodeMethodMetadataStaticScalarMethod3WithBrowsePathTest2Async() {
            await GetTests().NodeMethodMetadataStaticScalarMethod3WithBrowsePathTest2Async();
        }

        [Fact]
        public async Task NodeMethodCallStaticScalarMethod1Test1Async() {
            await GetTests().NodeMethodCallStaticScalarMethod1Test1Async();
        }

        [Fact]
        public async Task NodeMethodCallStaticScalarMethod1Test2Async() {
            await GetTests().NodeMethodCallStaticScalarMethod1Test2Async();
        }

        [Fact]
        public async Task NodeMethodCallStaticScalarMethod1Test3Async() {
            await GetTests().NodeMethodCallStaticScalarMethod1Test3Async();
        }

        [Fact]
        public async Task NodeMethodCallStaticScalarMethod1Test4Async() {
            await GetTests().NodeMethodCallStaticScalarMethod1Test4Async();
        }

        [Fact]
        public async Task NodeMethodCallStaticScalarMethod1Test5Async() {
            await GetTests().NodeMethodCallStaticScalarMethod1Test5Async();
        }

        [Fact]
        public async Task NodeMethodCallStaticScalarMethod2Test1Async() {
            await GetTests().NodeMethodCallStaticScalarMethod2Test1Async();
        }

        [Fact]
        public async Task NodeMethodCallStaticScalarMethod2Test2Async() {
            await GetTests().NodeMethodCallStaticScalarMethod2Test2Async();
        }

        [Fact]
        public async Task NodeMethodCallStaticScalarMethod3Test1Async() {
            await GetTests().NodeMethodCallStaticScalarMethod3Test1Async();
        }

        [Fact]
        public async Task NodeMethodCallStaticScalarMethod3Test2Async() {
            await GetTests().NodeMethodCallStaticScalarMethod3Test2Async();
        }

        [Fact]
        public async Task NodeMethodCallStaticScalarMethod3WithBrowsePathNoIdsTestAsync() {
            await GetTests().NodeMethodCallStaticScalarMethod3WithBrowsePathNoIdsTestAsync();
        }

        [Fact]
        public async Task NodeMethodCallStaticScalarMethod3WithObjectIdAndBrowsePathTestAsync() {
            await GetTests().NodeMethodCallStaticScalarMethod3WithObjectIdAndBrowsePathTestAsync();
        }

        [Fact]
        public async Task NodeMethodCallStaticScalarMethod3WithObjectIdAndMethodIdAndBrowsePathTestAsync() {
            await GetTests().NodeMethodCallStaticScalarMethod3WithObjectIdAndMethodIdAndBrowsePathTestAsync();
        }

        [Fact]
        public async Task NodeMethodCallStaticScalarMethod3WithObjectPathAndMethodIdAndBrowsePathTestAsync() {
            await GetTests().NodeMethodCallStaticScalarMethod3WithObjectPathAndMethodIdAndBrowsePathTestAsync();
        }

        [Fact]
        public async Task NodeMethodCallStaticScalarMethod3WithObjectIdAndPathAndMethodIdAndPathTestAsync() {
            await GetTests().NodeMethodCallStaticScalarMethod3WithObjectIdAndPathAndMethodIdAndPathTestAsync();
        }

        [Fact]
        public async Task NodeMethodCallBoiler2ResetTestAsync() {
            await GetTests().NodeMethodCallBoiler2ResetTestAsync();
        }

        [Fact]
        public async Task NodeMethodCallBoiler1ResetTestAsync() {
            await GetTests().NodeMethodCallBoiler1ResetTestAsync();
        }
    }
}
