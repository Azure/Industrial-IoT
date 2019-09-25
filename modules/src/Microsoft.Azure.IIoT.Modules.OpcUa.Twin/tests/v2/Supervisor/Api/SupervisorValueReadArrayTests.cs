// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Twin.v2.Supervisor.Api {
    using Microsoft.Azure.IIoT.Modules.OpcUa.Twin.Tests;
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.OpcUa.Api.Twin.Models;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Testing.Fixtures;
    using Microsoft.Azure.IIoT.OpcUa.Testing.Tests;
    using Microsoft.Azure.IIoT.OpcUa.Twin;
    using System.Net;
    using System.Threading.Tasks;
    using Xunit;
    using Autofac;

    [Collection(ReadCollection.Name)]
    public class SupervisorValueReadArrayTests : IClassFixture<TwinModuleFixture> {

        public SupervisorValueReadArrayTests(TestServerFixture server, TwinModuleFixture module) {
            _server = server;
            _module = module;
        }

        private ReadArrayValueTests<EndpointApiModel> GetTests() {
            return new ReadArrayValueTests<EndpointApiModel>(
                () => _module.HubContainer.Resolve<INodeServices<EndpointApiModel>>(),
                new EndpointApiModel {
                    Url = $"opc.tcp://{Dns.GetHostName()}:{_server.Port}/UA/SampleServer",
                    Certificate = _server.Certificate?.RawData
                }, (ep, n) => _server.Client.ReadValueAsync(new EndpointModel { Url = ep.Url }, n));
        }

        private readonly TestServerFixture _server;
        private readonly TwinModuleFixture _module;

        [Fact]
        public async Task NodeReadAllStaticArrayVariableNodeClassTest1() {
            await GetTests().NodeReadAllStaticArrayVariableNodeClassTest1();
        }

        [Fact]
        public async Task NodeReadAllStaticArrayVariableAccessLevelTest1() {
            await GetTests().NodeReadAllStaticArrayVariableAccessLevelTest1();
        }

        [Fact]
        public async Task NodeReadAllStaticArrayVariableWriteMaskTest1() {
            await GetTests().NodeReadAllStaticArrayVariableWriteMaskTest1();
        }

        [Fact]
        public async Task NodeReadAllStaticArrayVariableWriteMaskTest2() {
            await GetTests().NodeReadAllStaticArrayVariableWriteMaskTest2();
        }

        [Fact]
        public async Task NodeReadStaticArrayBooleanValueVariableTest() {
            await GetTests().NodeReadStaticArrayBooleanValueVariableTest();
        }

        [Fact]
        public async Task NodeReadStaticArraySByteValueVariableTest() {
            await GetTests().NodeReadStaticArraySByteValueVariableTest();
        }

        [Fact]
        public async Task NodeReadStaticArrayByteValueVariableTest() {
            await GetTests().NodeReadStaticArrayByteValueVariableTest();
        }

        [Fact]
        public async Task NodeReadStaticArrayInt16ValueVariableTest() {
            await GetTests().NodeReadStaticArrayInt16ValueVariableTest();
        }

        [Fact]
        public async Task NodeReadStaticArrayUInt16ValueVariableTest() {
            await GetTests().NodeReadStaticArrayUInt16ValueVariableTest();
        }

        [Fact]
        public async Task NodeReadStaticArrayInt32ValueVariableTest() {
            await GetTests().NodeReadStaticArrayInt32ValueVariableTest();
        }

        [Fact]
        public async Task NodeReadStaticArrayUInt32ValueVariableTest() {
            await GetTests().NodeReadStaticArrayUInt32ValueVariableTest();
        }

        [Fact]
        public async Task NodeReadStaticArrayInt64ValueVariableTest() {
            await GetTests().NodeReadStaticArrayInt64ValueVariableTest();
        }

        [Fact]
        public async Task NodeReadStaticArrayUInt64ValueVariableTest() {
            await GetTests().NodeReadStaticArrayUInt64ValueVariableTest();
        }

        [Fact]
        public async Task NodeReadStaticArrayFloatValueVariableTest() {
            await GetTests().NodeReadStaticArrayFloatValueVariableTest();
        }

        [Fact]
        public async Task NodeReadStaticArrayDoubleValueVariableTest() {
            await GetTests().NodeReadStaticArrayDoubleValueVariableTest();
        }

        [Fact]
        public async Task NodeReadStaticArrayStringValueVariableTest() {
            await GetTests().NodeReadStaticArrayStringValueVariableTest();
        }

        [Fact]
        public async Task NodeReadStaticArrayDateTimeValueVariableTest() {
            await GetTests().NodeReadStaticArrayDateTimeValueVariableTest();
        }

        [Fact]
        public async Task NodeReadStaticArrayGuidValueVariableTest() {
            await GetTests().NodeReadStaticArrayGuidValueVariableTest();
        }

        [Fact]
        public async Task NodeReadStaticArrayByteStringValueVariableTest() {
            await GetTests().NodeReadStaticArrayByteStringValueVariableTest();
        }

        [Fact]
        public async Task NodeReadStaticArrayXmlElementValueVariableTest() {
            await GetTests().NodeReadStaticArrayXmlElementValueVariableTest();
        }

        [Fact]
        public async Task NodeReadStaticArrayNodeIdValueVariableTest() {
            await GetTests().NodeReadStaticArrayNodeIdValueVariableTest();
        }

        [Fact]
        public async Task NodeReadStaticArrayExpandedNodeIdValueVariableTest() {
            await GetTests().NodeReadStaticArrayExpandedNodeIdValueVariableTest();
        }

        [Fact]
        public async Task NodeReadStaticArrayQualifiedNameValueVariableTest() {
            await GetTests().NodeReadStaticArrayQualifiedNameValueVariableTest();
        }

        [Fact]
        public async Task NodeReadStaticArrayLocalizedTextValueVariableTest() {
            await GetTests().NodeReadStaticArrayLocalizedTextValueVariableTest();
        }

        [Fact]
        public async Task NodeReadStaticArrayStatusCodeValueVariableTest() {
            await GetTests().NodeReadStaticArrayStatusCodeValueVariableTest();
        }

        [Fact]
        public async Task NodeReadStaticArrayVariantValueVariableTest() {
            await GetTests().NodeReadStaticArrayVariantValueVariableTest();
        }

        [Fact]
        public async Task NodeReadStaticArrayEnumerationValueVariableTest() {
            await GetTests().NodeReadStaticArrayEnumerationValueVariableTest();
        }

        [Fact]
        public async Task NodeReadStaticArrayStructureValueVariableTest() {
            await GetTests().NodeReadStaticArrayStructureValueVariableTest();
        }

        [Fact]
        public async Task NodeReadStaticArrayNumberValueVariableTest() {
            await GetTests().NodeReadStaticArrayNumberValueVariableTest();
        }

        [Fact]
        public async Task NodeReadStaticArrayIntegerValueVariableTest() {
            await GetTests().NodeReadStaticArrayIntegerValueVariableTest();
        }

        [Fact]
        public async Task NodeReadStaticArrayUIntegerValueVariableTest() {
            await GetTests().NodeReadStaticArrayUIntegerValueVariableTest();
        }

    }
}
