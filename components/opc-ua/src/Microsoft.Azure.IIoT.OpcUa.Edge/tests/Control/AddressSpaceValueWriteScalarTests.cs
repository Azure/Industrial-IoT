// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Control {
    using Microsoft.Azure.IIoT.OpcUa.Edge.Tests;
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Services;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Testing.Fixtures;
    using Microsoft.Azure.IIoT.OpcUa.Testing.Tests;
    using System.Net;
    using System.Threading.Tasks;
    using Xunit;

    [Collection(WriteCollection.Name)]
    public class AddressSpaceValueWriteScalarTests {

        public AddressSpaceValueWriteScalarTests(TestServerFixture server) {
            _server = server;
        }

        private WriteScalarValueTests<EndpointModel> GetTests() {
            return new WriteScalarValueTests<EndpointModel>(
                () => new AddressSpaceServices(_server.Client,
                    new JsonVariantEncoder(), _server.Logger),
                new EndpointModel {
                    Url = $"opc.tcp://{Dns.GetHostName()}:{_server.Port}/UA/SampleServer",
                    Certificate = _server.Certificate?.RawData
                }, _server.Client.ReadValueAsync);
        }

        private readonly TestServerFixture _server;


        [Fact]
        public async Task NodeWriteStaticScalarBooleanValueVariableTest() {
            await GetTests().NodeWriteStaticScalarBooleanValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticScalarBooleanValueVariableWithBrowsePathTest1() {
            await GetTests().NodeWriteStaticScalarBooleanValueVariableWithBrowsePathTest1Async();
        }

        [Fact]
        public async Task NodeWriteStaticScalarBooleanValueVariableWithBrowsePathTest2() {
            await GetTests().NodeWriteStaticScalarBooleanValueVariableWithBrowsePathTest2Async();
        }

        [Fact]
        public async Task NodeWriteStaticScalarBooleanValueVariableWithBrowsePathTest3() {
            await GetTests().NodeWriteStaticScalarBooleanValueVariableWithBrowsePathTest3Async();
        }

        [Fact]
        public async Task NodeWriteStaticScalarSByteValueVariableTest() {
            await GetTests().NodeWriteStaticScalarSByteValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticScalarByteValueVariableTest() {
            await GetTests().NodeWriteStaticScalarByteValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticScalarInt16ValueVariableTest() {
            await GetTests().NodeWriteStaticScalarInt16ValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticScalarUInt16ValueVariableTest() {
            await GetTests().NodeWriteStaticScalarUInt16ValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticScalarInt32ValueVariableTest() {
            await GetTests().NodeWriteStaticScalarInt32ValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticScalarUInt32ValueVariableTest() {
            await GetTests().NodeWriteStaticScalarUInt32ValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticScalarInt64ValueVariableTest() {
            await GetTests().NodeWriteStaticScalarInt64ValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticScalarUInt64ValueVariableTest() {
            await GetTests().NodeWriteStaticScalarUInt64ValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticScalarFloatValueVariableTest() {
            await GetTests().NodeWriteStaticScalarFloatValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticScalarDoubleValueVariableTest() {
            await GetTests().NodeWriteStaticScalarDoubleValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticScalarStringValueVariableTest() {
            await GetTests().NodeWriteStaticScalarStringValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticScalarDateTimeValueVariableTest() {
            await GetTests().NodeWriteStaticScalarDateTimeValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticScalarGuidValueVariableTest() {
            await GetTests().NodeWriteStaticScalarGuidValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticScalarByteStringValueVariableTest() {
            await GetTests().NodeWriteStaticScalarByteStringValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticScalarXmlElementValueVariableTest() {
            await GetTests().NodeWriteStaticScalarXmlElementValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticScalarNodeIdValueVariableTest() {
            await GetTests().NodeWriteStaticScalarNodeIdValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticScalarExpandedNodeIdValueVariableTest() {
            await GetTests().NodeWriteStaticScalarExpandedNodeIdValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticScalarQualifiedNameValueVariableTest() {
            await GetTests().NodeWriteStaticScalarQualifiedNameValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticScalarLocalizedTextValueVariableTest() {
            await GetTests().NodeWriteStaticScalarLocalizedTextValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticScalarStatusCodeValueVariableTest() {
            await GetTests().NodeWriteStaticScalarStatusCodeValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticScalarVariantValueVariableTest() {
            await GetTests().NodeWriteStaticScalarVariantValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticScalarEnumerationValueVariableTest() {
            await GetTests().NodeWriteStaticScalarEnumerationValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticScalarStructuredValueVariableTest() {
            await GetTests().NodeWriteStaticScalarStructuredValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticScalarNumberValueVariableTest() {
            await GetTests().NodeWriteStaticScalarNumberValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticScalarIntegerValueVariableTest() {
            await GetTests().NodeWriteStaticScalarIntegerValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticScalarUIntegerValueVariableTest() {
            await GetTests().NodeWriteStaticScalarUIntegerValueVariableTestAsync();
        }

    }
}
