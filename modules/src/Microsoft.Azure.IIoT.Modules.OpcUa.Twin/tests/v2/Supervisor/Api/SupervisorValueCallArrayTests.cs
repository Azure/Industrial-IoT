// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Twin.v2.Supervisor.Api {
    using Microsoft.Azure.IIoT.Modules.OpcUa.Twin.Tests;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.Api.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.Testing.Fixtures;
    using Microsoft.Azure.IIoT.OpcUa.Testing.Tests;
    using Microsoft.Azure.IIoT.OpcUa.Twin;
    using Microsoft.Azure.IIoT.Utils;
    using Opc.Ua;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading.Tasks;
    using Xunit;
    using Autofac;

    [Collection(WriteCollection.Name)]
    public class SupervisorValueCallArrayTests : IClassFixture<TwinModuleFixture> {

        public SupervisorValueCallArrayTests(TestServerFixture server, TwinModuleFixture module) {
            _server = server;
            _module = module;
            _hostEntry = Try.Op(() => Dns.GetHostEntry(Utils.GetHostName()))
                ?? Try.Op(() => Dns.GetHostEntry("localhost"));
        }

        private CallArrayMethodTests<EndpointApiModel> GetTests() {
            return new CallArrayMethodTests<EndpointApiModel>(
                () => _module.HubContainer.Resolve<INodeServices<EndpointApiModel>>(),
                new EndpointApiModel {
                    Url = $"opc.tcp://{_hostEntry?.HostName ?? "localhost"}:{_server.Port}/UA/SampleServer",
                    AlternativeUrls = _hostEntry?.AddressList
                        .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork)
                        .Select(ip => $"opc.tcp://{ip}:{_server.Port}/UA/SampleServer").ToHashSet(),
                    Certificate = _server.Certificate?.RawData?.ToThumbprint()
                });
        }

        private readonly TestServerFixture _server;
        private readonly TwinModuleFixture _module;
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
