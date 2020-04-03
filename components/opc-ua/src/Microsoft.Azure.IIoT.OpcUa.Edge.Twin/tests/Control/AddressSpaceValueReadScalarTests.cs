// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Control.Services {
    using Microsoft.Azure.IIoT.OpcUa.Edge.Tests;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Services;
    using Microsoft.Azure.IIoT.OpcUa.Testing.Fixtures;
    using Microsoft.Azure.IIoT.OpcUa.Testing.Tests;
    using System.Net;
    using System.Threading.Tasks;
    using Xunit;

    [Collection(ReadCollection.Name)]
    public class AddressSpaceValueReadScalarTests {

        public AddressSpaceValueReadScalarTests(TestServerFixture server) {
            _server = server;
        }

        private ReadScalarValueTests<EndpointModel> GetTests() {
            return new ReadScalarValueTests<EndpointModel>(
                () => new AddressSpaceServices(_server.Client,
                    new VariantEncoderFactory(), _server.Logger),
                new EndpointModel {
                    Url = $"opc.tcp://{Dns.GetHostName()}:{_server.Port}/UA/SampleServer",
                    Certificate = _server.Certificate?.RawData?.ToThumbprint()
                }, _server.Client.ReadValueAsync);
        }

        private readonly TestServerFixture _server;

        [Fact]
        public async Task NodeReadAllStaticScalarVariableNodeClassTest1Async() {
            await GetTests().NodeReadAllStaticScalarVariableNodeClassTest1Async();
        }

        [Fact]
        public async Task NodeReadAllStaticScalarVariableAccessLevelTest1Async() {
            await GetTests().NodeReadAllStaticScalarVariableAccessLevelTest1Async();
        }

        [Fact]
        public async Task NodeReadAllStaticScalarVariableWriteMaskTest1Async() {
            await GetTests().NodeReadAllStaticScalarVariableWriteMaskTest1Async();
        }

        [Fact]
        public async Task NodeReadAllStaticScalarVariableWriteMaskTest2Async() {
            await GetTests().NodeReadAllStaticScalarVariableWriteMaskTest2Async();
        }

        [Fact]
        public async Task NodeReadStaticScalarBooleanValueVariableTestAsync() {
            await GetTests().NodeReadStaticScalarBooleanValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticScalarBooleanValueVariableWithBrowsePathTest1Async() {
            await GetTests().NodeReadStaticScalarBooleanValueVariableWithBrowsePathTest1Async();
        }

        [Fact]
        public async Task NodeReadStaticScalarBooleanValueVariableWithBrowsePathTest2Async() {
            await GetTests().NodeReadStaticScalarBooleanValueVariableWithBrowsePathTest2Async();
        }

        [Fact]
        public async Task NodeReadStaticScalarBooleanValueVariableWithBrowsePathTest3Async() {
            await GetTests().NodeReadStaticScalarBooleanValueVariableWithBrowsePathTest3Async();
        }

        [Fact]
        public async Task NodeReadStaticScalarSByteValueVariableTestAsync() {
            await GetTests().NodeReadStaticScalarSByteValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticScalarByteValueVariableTestAsync() {
            await GetTests().NodeReadStaticScalarByteValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticScalarInt16ValueVariableTestAsync() {
            await GetTests().NodeReadStaticScalarInt16ValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticScalarUInt16ValueVariableTestAsync() {
            await GetTests().NodeReadStaticScalarUInt16ValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticScalarInt32ValueVariableTestAsync() {
            await GetTests().NodeReadStaticScalarInt32ValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticScalarUInt32ValueVariableTestAsync() {
            await GetTests().NodeReadStaticScalarUInt32ValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticScalarInt64ValueVariableTestAsync() {
            await GetTests().NodeReadStaticScalarInt64ValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticScalarUInt64ValueVariableTestAsync() {
            await GetTests().NodeReadStaticScalarUInt64ValueVariableTestAsync();
        }


        [Fact]
        public async Task NodeReadStaticScalarFloatValueVariableTestAsync() {
            await GetTests().NodeReadStaticScalarFloatValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticScalarDoubleValueVariableTestAsync() {
            await GetTests().NodeReadStaticScalarDoubleValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticScalarStringValueVariableTestAsync() {
            await GetTests().NodeReadStaticScalarStringValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticScalarDateTimeValueVariableTestAsync() {
            await GetTests().NodeReadStaticScalarDateTimeValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticScalarGuidValueVariableTestAsync() {
            await GetTests().NodeReadStaticScalarGuidValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticScalarByteStringValueVariableTestAsync() {
            await GetTests().NodeReadStaticScalarByteStringValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticScalarXmlElementValueVariableTestAsync() {
            await GetTests().NodeReadStaticScalarXmlElementValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticScalarNodeIdValueVariableTestAsync() {
            await GetTests().NodeReadStaticScalarNodeIdValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticScalarExpandedNodeIdValueVariableTestAsync() {
            await GetTests().NodeReadStaticScalarExpandedNodeIdValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticScalarQualifiedNameValueVariableTestAsync() {
            await GetTests().NodeReadStaticScalarQualifiedNameValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticScalarLocalizedTextValueVariableTestAsync() {
            await GetTests().NodeReadStaticScalarLocalizedTextValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticScalarStatusCodeValueVariableTestAsync() {
            await GetTests().NodeReadStaticScalarStatusCodeValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticScalarVariantValueVariableTestAsync() {
            await GetTests().NodeReadStaticScalarVariantValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticScalarEnumerationValueVariableTestAsync() {
            await GetTests().NodeReadStaticScalarEnumerationValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticScalarStructuredValueVariableTestAsync() {
            await GetTests().NodeReadStaticScalarStructuredValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticScalarNumberValueVariableTestAsync() {
            await GetTests().NodeReadStaticScalarNumberValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticScalarIntegerValueVariableTestAsync() {
            await GetTests().NodeReadStaticScalarIntegerValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticScalarUIntegerValueVariableTestAsync() {
            await GetTests().NodeReadStaticScalarUIntegerValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadDataAccessMeasurementFloatValueTestAsync() {
            await GetTests().NodeReadDataAccessMeasurementFloatValueTestAsync();
        }

        [Fact]
        public async Task NodeReadDiagnosticsNoneTestAsync() {
            await GetTests().NodeReadDiagnosticsNoneTestAsync();
        }

        [Fact]
        public async Task NodeReadDiagnosticsStatusTestAsync() {
            await GetTests().NodeReadDiagnosticsStatusTestAsync();
        }

        [Fact]
        public async Task NodeReadDiagnosticsOperationsTestAsync() {
            await GetTests().NodeReadDiagnosticsStatusTestAsync();
        }

        [Fact]
        public async Task NodeReadDiagnosticsVerboseTestAsync() {
            await GetTests().NodeReadDiagnosticsStatusTestAsync();
        }

    }
}
