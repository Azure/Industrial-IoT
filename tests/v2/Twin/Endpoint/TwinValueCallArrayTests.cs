// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Twin.v2.Twin.Endpoint {
    using Microsoft.Azure.IIoT.Modules.OpcUa.Twin.Tests;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Testing.Fixtures;
    using Microsoft.Azure.IIoT.OpcUa.Testing.Tests;
    using Microsoft.Azure.IIoT.OpcUa.Twin;
    using System.Net;
    using System.Threading.Tasks;
    using Xunit;
    using Autofac;

    [Collection(WriteCollection.Name)]
    public class TwinValueCallArrayTests : IClassFixture<TwinModuleFixture> {

        public TwinValueCallArrayTests(TestServerFixture server, TwinModuleFixture module) {
            _server = server;
            _module = module;
        }

        private CallArrayMethodTests<string> GetTests() {
            var endpoint = new EndpointRegistrationModel {
                Endpoint = new EndpointModel {
                    Url = $"opc.tcp://{Dns.GetHostName()}:{_server.Port}/UA/SampleServer"
                },
                Id = "testid",
                SupervisorId = SupervisorModelEx.CreateSupervisorId(
                  _module.DeviceId, _module.ModuleId)
            };
            endpoint = _module.RegisterAndActivateTwinId(endpoint);
            return new CallArrayMethodTests<string>(
                () => _module.HubContainer.Resolve<INodeServices<string>>(), endpoint.Id);
        }

        private readonly TestServerFixture _server;
        private readonly TwinModuleFixture _module;

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
