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
    public class SupervisorValueReadScalarTests : IClassFixture<TwinModuleFixture> {

        public SupervisorValueReadScalarTests(TestServerFixture server, TwinModuleFixture module) {
            _server = server;
            _module = module;
        }

        private ReadScalarValueTests<EndpointApiModel> GetTests() {
            return new ReadScalarValueTests<EndpointApiModel>(
                () => _module.HubContainer.Resolve<INodeServices<EndpointApiModel>>(),
                new EndpointApiModel {
                    Url = $"opc.tcp://{Dns.GetHostName()}:{_server.Port}/UA/SampleServer",
                    Certificate = _server.Certificate?.RawData
                }, (ep, n) => _server.Client.ReadValueAsync(new EndpointModel { Url = ep.Url }, n));
        }

        private readonly TestServerFixture _server;
        private readonly TwinModuleFixture _module;

        [Fact]
        public async Task NodeReadAllStaticScalarVariableNodeClassTest1() {
            await GetTests().NodeReadAllStaticScalarVariableNodeClassTest1();
        }

        [Fact]
        public async Task NodeReadAllStaticScalarVariableAccessLevelTest1() {
            await GetTests().NodeReadAllStaticScalarVariableAccessLevelTest1();
        }

        [Fact]
        public async Task NodeReadAllStaticScalarVariableWriteMaskTest1() {
            await GetTests().NodeReadAllStaticScalarVariableWriteMaskTest1();
        }

        [Fact]
        public async Task NodeReadAllStaticScalarVariableWriteMaskTest2() {
            await GetTests().NodeReadAllStaticScalarVariableWriteMaskTest2();
        }

        [Fact]
        public async Task NodeReadStaticScalarBooleanValueVariableTest() {
            await GetTests().NodeReadStaticScalarBooleanValueVariableTest();
        }

        [Fact]
        public async Task NodeReadStaticScalarBooleanValueVariableWithBrowsePathTest1() {
            await GetTests().NodeReadStaticScalarBooleanValueVariableWithBrowsePathTest1();
        }

        [Fact]
        public async Task NodeReadStaticScalarBooleanValueVariableWithBrowsePathTest2() {
            await GetTests().NodeReadStaticScalarBooleanValueVariableWithBrowsePathTest2();
        }

        [Fact]
        public async Task NodeReadStaticScalarBooleanValueVariableWithBrowsePathTest3() {
            await GetTests().NodeReadStaticScalarBooleanValueVariableWithBrowsePathTest3();
        }

        [Fact]
        public async Task NodeReadStaticScalarSByteValueVariableTest() {
            await GetTests().NodeReadStaticScalarSByteValueVariableTest();
        }

        [Fact]
        public async Task NodeReadStaticScalarByteValueVariableTest() {
            await GetTests().NodeReadStaticScalarByteValueVariableTest();
        }

        [Fact]
        public async Task NodeReadStaticScalarInt16ValueVariableTest() {
            await GetTests().NodeReadStaticScalarInt16ValueVariableTest();
        }

        [Fact]
        public async Task NodeReadStaticScalarUInt16ValueVariableTest() {
            await GetTests().NodeReadStaticScalarUInt16ValueVariableTest();
        }

        [Fact]
        public async Task NodeReadStaticScalarInt32ValueVariableTest() {
            await GetTests().NodeReadStaticScalarInt32ValueVariableTest();
        }

        [Fact]
        public async Task NodeReadStaticScalarUInt32ValueVariableTest() {
            await GetTests().NodeReadStaticScalarUInt32ValueVariableTest();
        }

        [Fact]
        public async Task NodeReadStaticScalarInt64ValueVariableTest() {
            await GetTests().NodeReadStaticScalarInt64ValueVariableTest();
        }

        [Fact]
        public async Task NodeReadStaticScalarUInt64ValueVariableTest() {
            await GetTests().NodeReadStaticScalarUInt64ValueVariableTest();
        }


        [Fact]
        public async Task NodeReadStaticScalarFloatValueVariableTest() {
            await GetTests().NodeReadStaticScalarFloatValueVariableTest();
        }

        [Fact]
        public async Task NodeReadStaticScalarDoubleValueVariableTest() {
            await GetTests().NodeReadStaticScalarDoubleValueVariableTest();
        }

        [Fact]
        public async Task NodeReadStaticScalarStringValueVariableTest() {
            await GetTests().NodeReadStaticScalarStringValueVariableTest();
        }

        [Fact]
        public async Task NodeReadStaticScalarDateTimeValueVariableTest() {
            await GetTests().NodeReadStaticScalarDateTimeValueVariableTest();
        }

        [Fact]
        public async Task NodeReadStaticScalarGuidValueVariableTest() {
            await GetTests().NodeReadStaticScalarGuidValueVariableTest();
        }

        [Fact]
        public async Task NodeReadStaticScalarByteStringValueVariableTest() {
            await GetTests().NodeReadStaticScalarByteStringValueVariableTest();
        }

        [Fact]
        public async Task NodeReadStaticScalarXmlElementValueVariableTest() {
            await GetTests().NodeReadStaticScalarXmlElementValueVariableTest();
        }

        [Fact]
        public async Task NodeReadStaticScalarNodeIdValueVariableTest() {
            await GetTests().NodeReadStaticScalarNodeIdValueVariableTest();
        }

        [Fact]
        public async Task NodeReadStaticScalarExpandedNodeIdValueVariableTest() {
            await GetTests().NodeReadStaticScalarExpandedNodeIdValueVariableTest();
        }

        [Fact]
        public async Task NodeReadStaticScalarQualifiedNameValueVariableTest() {
            await GetTests().NodeReadStaticScalarQualifiedNameValueVariableTest();
        }

        [Fact]
        public async Task NodeReadStaticScalarLocalizedTextValueVariableTest() {
            await GetTests().NodeReadStaticScalarLocalizedTextValueVariableTest();
        }

        [Fact]
        public async Task NodeReadStaticScalarStatusCodeValueVariableTest() {
            await GetTests().NodeReadStaticScalarStatusCodeValueVariableTest();
        }

        [Fact]
        public async Task NodeReadStaticScalarVariantValueVariableTest() {
            await GetTests().NodeReadStaticScalarVariantValueVariableTest();
        }

        [Fact]
        public async Task NodeReadStaticScalarEnumerationValueVariableTest() {
            await GetTests().NodeReadStaticScalarEnumerationValueVariableTest();
        }

        [Fact]
        public async Task NodeReadStaticScalarStructuredValueVariableTest() {
            await GetTests().NodeReadStaticScalarStructuredValueVariableTest();
        }

        [Fact]
        public async Task NodeReadStaticScalarNumberValueVariableTest() {
            await GetTests().NodeReadStaticScalarNumberValueVariableTest();
        }

        [Fact]
        public async Task NodeReadStaticScalarIntegerValueVariableTest() {
            await GetTests().NodeReadStaticScalarIntegerValueVariableTest();
        }

        [Fact]
        public async Task NodeReadStaticScalarUIntegerValueVariableTest() {
            await GetTests().NodeReadStaticScalarUIntegerValueVariableTest();
        }

        [Fact]
        public async Task NodeReadDiagnosticsNoneTest() {
            await GetTests().NodeReadDiagnosticsNoneTest();
        }

        [Fact]
        public async Task NodeReadDiagnosticsStatusTest() {
            await GetTests().NodeReadDiagnosticsStatusTest();
        }

        [Fact]
        public async Task NodeReadDiagnosticsOperationsTest() {
            await GetTests().NodeReadDiagnosticsStatusTest();
        }

        [Fact]
        public async Task NodeReadDiagnosticsVerboseTest() {
            await GetTests().NodeReadDiagnosticsStatusTest();
        }

    }
}
