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

    [Collection(WriteCollection.Name)]
    public class SupervisorValueWriteScalarTests : IClassFixture<TwinModuleFixture> {

        public SupervisorValueWriteScalarTests(TestServerFixture server, TwinModuleFixture module) {
            _server = server;
            _module = module;
        }

        private WriteScalarValueTests<EndpointApiModel> GetTests() {
            return new WriteScalarValueTests<EndpointApiModel>(
                () => _module.HubContainer.Resolve<INodeServices<EndpointApiModel>>(),
                new EndpointApiModel {
                    Url = $"opc.tcp://{Dns.GetHostName()}:{_server.Port}/UA/SampleServer",
                    Certificate = _server.Certificate?.RawData
                }, (ep, n) => _server.Client.ReadValueAsync(new EndpointModel { Url = ep.Url }, n));
        }

        private readonly TestServerFixture _server;
        private readonly TwinModuleFixture _module;

        [Fact]
        public async Task NodeWriteStaticScalarBooleanValueVariableTest() {
            await GetTests().NodeWriteStaticScalarBooleanValueVariableTest();
        }

        [Fact]
        public async Task NodeWriteStaticScalarBooleanValueVariableWithBrowsePathTest1() {
            await GetTests().NodeWriteStaticScalarBooleanValueVariableWithBrowsePathTest1();
        }

        [Fact]
        public async Task NodeWriteStaticScalarBooleanValueVariableWithBrowsePathTest2() {
            await GetTests().NodeWriteStaticScalarBooleanValueVariableWithBrowsePathTest2();
        }

        [Fact]
        public async Task NodeWriteStaticScalarBooleanValueVariableWithBrowsePathTest3() {
            await GetTests().NodeWriteStaticScalarBooleanValueVariableWithBrowsePathTest3();
        }

        [Fact]
        public async Task NodeWriteStaticScalarSByteValueVariableTest() {
            await GetTests().NodeWriteStaticScalarSByteValueVariableTest();
        }

        [Fact]
        public async Task NodeWriteStaticScalarByteValueVariableTest() {
            await GetTests().NodeWriteStaticScalarByteValueVariableTest();
        }

        [Fact]
        public async Task NodeWriteStaticScalarInt16ValueVariableTest() {
            await GetTests().NodeWriteStaticScalarInt16ValueVariableTest();
        }

        [Fact]
        public async Task NodeWriteStaticScalarUInt16ValueVariableTest() {
            await GetTests().NodeWriteStaticScalarUInt16ValueVariableTest();
        }

        [Fact]
        public async Task NodeWriteStaticScalarInt32ValueVariableTest() {
            await GetTests().NodeWriteStaticScalarInt32ValueVariableTest();
        }

        [Fact]
        public async Task NodeWriteStaticScalarUInt32ValueVariableTest() {
            await GetTests().NodeWriteStaticScalarUInt32ValueVariableTest();
        }

        [Fact]
        public async Task NodeWriteStaticScalarInt64ValueVariableTest() {
            await GetTests().NodeWriteStaticScalarInt64ValueVariableTest();
        }

        [Fact]
        public async Task NodeWriteStaticScalarUInt64ValueVariableTest() {
            await GetTests().NodeWriteStaticScalarUInt64ValueVariableTest();
        }

        [Fact]
        public async Task NodeWriteStaticScalarFloatValueVariableTest() {
            await GetTests().NodeWriteStaticScalarFloatValueVariableTest();
        }

        [Fact]
        public async Task NodeWriteStaticScalarDoubleValueVariableTest() {
            await GetTests().NodeWriteStaticScalarDoubleValueVariableTest();
        }

        [Fact]
        public async Task NodeWriteStaticScalarStringValueVariableTest() {
            await GetTests().NodeWriteStaticScalarStringValueVariableTest();
        }

        [Fact]
        public async Task NodeWriteStaticScalarDateTimeValueVariableTest() {
            await GetTests().NodeWriteStaticScalarDateTimeValueVariableTest();
        }

        [Fact]
        public async Task NodeWriteStaticScalarGuidValueVariableTest() {
            await GetTests().NodeWriteStaticScalarGuidValueVariableTest();
        }

        [Fact]
        public async Task NodeWriteStaticScalarByteStringValueVariableTest() {
            await GetTests().NodeWriteStaticScalarByteStringValueVariableTest();
        }

        [Fact]
        public async Task NodeWriteStaticScalarXmlElementValueVariableTest() {
            await GetTests().NodeWriteStaticScalarXmlElementValueVariableTest();
        }

        [Fact]
        public async Task NodeWriteStaticScalarNodeIdValueVariableTest() {
            await GetTests().NodeWriteStaticScalarNodeIdValueVariableTest();
        }

        [Fact]
        public async Task NodeWriteStaticScalarExpandedNodeIdValueVariableTest() {
            await GetTests().NodeWriteStaticScalarExpandedNodeIdValueVariableTest();
        }

        [Fact]
        public async Task NodeWriteStaticScalarQualifiedNameValueVariableTest() {
            await GetTests().NodeWriteStaticScalarQualifiedNameValueVariableTest();
        }

        [Fact]
        public async Task NodeWriteStaticScalarLocalizedTextValueVariableTest() {
            await GetTests().NodeWriteStaticScalarLocalizedTextValueVariableTest();
        }

        [Fact]
        public async Task NodeWriteStaticScalarStatusCodeValueVariableTest() {
            await GetTests().NodeWriteStaticScalarStatusCodeValueVariableTest();
        }

        [Fact]
        public async Task NodeWriteStaticScalarVariantValueVariableTest() {
            await GetTests().NodeWriteStaticScalarVariantValueVariableTest();
        }

        [Fact]
        public async Task NodeWriteStaticScalarEnumerationValueVariableTest() {
            await GetTests().NodeWriteStaticScalarEnumerationValueVariableTest();
        }

        [Fact]
        public async Task NodeWriteStaticScalarStructuredValueVariableTest() {
            await GetTests().NodeWriteStaticScalarStructuredValueVariableTest();
        }

        [Fact]
        public async Task NodeWriteStaticScalarNumberValueVariableTest() {
            await GetTests().NodeWriteStaticScalarNumberValueVariableTest();
        }

        [Fact]
        public async Task NodeWriteStaticScalarIntegerValueVariableTest() {
            await GetTests().NodeWriteStaticScalarIntegerValueVariableTest();
        }

        [Fact]
        public async Task NodeWriteStaticScalarUIntegerValueVariableTest() {
            await GetTests().NodeWriteStaticScalarUIntegerValueVariableTest();
        }
    }
}
