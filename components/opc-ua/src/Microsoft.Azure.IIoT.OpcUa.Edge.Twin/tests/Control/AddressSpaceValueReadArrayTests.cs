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
    using Microsoft.Azure.IIoT.Utils;
    using Opc.Ua;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading.Tasks;
    using Xunit;

    [Collection(ReadCollection.Name)]
    public class AddressSpaceValueReadArrayTests {

        public AddressSpaceValueReadArrayTests(TestServerFixture server) {
            _server = server;
            _hostEntry = Try.Op(() => Dns.GetHostEntry(Utils.GetHostName()))
                ?? Try.Op(() => Dns.GetHostEntry("localhost"));
        }

        private ReadArrayValueTests<EndpointModel> GetTests() {
            return new ReadArrayValueTests<EndpointModel>(
                () => new AddressSpaceServices(_server.Client,
                    new VariantEncoderFactory(), _server.Logger),
                new EndpointModel {
                    Url = $"opc.tcp://{_hostEntry?.HostName ?? "localhost"}:{_server.Port}/UA/SampleServer",
                    AlternativeUrls = _hostEntry?.AddressList
                        .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork)
                        .Select(ip => $"opc.tcp://{ip}:{_server.Port}/UA/SampleServer").ToHashSet(),
                    Certificate = _server.Certificate?.RawData?.ToThumbprint()
                }, _server.Client.ReadValueAsync);
        }

        private readonly TestServerFixture _server;
        private readonly IPHostEntry _hostEntry;

        [Fact]
        public async Task NodeReadAllStaticArrayVariableNodeClassTest1Async() {
            await GetTests().NodeReadAllStaticArrayVariableNodeClassTest1Async();
        }

        [Fact]
        public async Task NodeReadAllStaticArrayVariableAccessLevelTest1Async() {
            await GetTests().NodeReadAllStaticArrayVariableAccessLevelTest1Async();
        }

        [Fact]
        public async Task NodeReadAllStaticArrayVariableWriteMaskTest1Async() {
            await GetTests().NodeReadAllStaticArrayVariableWriteMaskTest1Async();
        }

        [Fact]
        public async Task NodeReadAllStaticArrayVariableWriteMaskTest2Async() {
            await GetTests().NodeReadAllStaticArrayVariableWriteMaskTest2Async();
        }

        [Fact]
        public async Task NodeReadStaticArrayBooleanValueVariableTestAsync() {
            await GetTests().NodeReadStaticArrayBooleanValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticArraySByteValueVariableTestAsync() {
            await GetTests().NodeReadStaticArraySByteValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticArrayByteValueVariableTestAsync() {
            await GetTests().NodeReadStaticArrayByteValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticArrayInt16ValueVariableTestAsync() {
            await GetTests().NodeReadStaticArrayInt16ValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticArrayUInt16ValueVariableTestAsync() {
            await GetTests().NodeReadStaticArrayUInt16ValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticArrayInt32ValueVariableTestAsync() {
            await GetTests().NodeReadStaticArrayInt32ValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticArrayUInt32ValueVariableTestAsync() {
            await GetTests().NodeReadStaticArrayUInt32ValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticArrayInt64ValueVariableTestAsync() {
            await GetTests().NodeReadStaticArrayInt64ValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticArrayUInt64ValueVariableTestAsync() {
            await GetTests().NodeReadStaticArrayUInt64ValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticArrayFloatValueVariableTestAsync() {
            await GetTests().NodeReadStaticArrayFloatValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticArrayDoubleValueVariableTestAsync() {
            await GetTests().NodeReadStaticArrayDoubleValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticArrayStringValueVariableTestAsync() {
            await GetTests().NodeReadStaticArrayStringValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticArrayDateTimeValueVariableTestAsync() {
            await GetTests().NodeReadStaticArrayDateTimeValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticArrayGuidValueVariableTestAsync() {
            await GetTests().NodeReadStaticArrayGuidValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticArrayByteStringValueVariableTestAsync() {
            await GetTests().NodeReadStaticArrayByteStringValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticArrayXmlElementValueVariableTestAsync() {
            await GetTests().NodeReadStaticArrayXmlElementValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticArrayNodeIdValueVariableTestAsync() {
            await GetTests().NodeReadStaticArrayNodeIdValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticArrayExpandedNodeIdValueVariableTestAsync() {
            await GetTests().NodeReadStaticArrayExpandedNodeIdValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticArrayQualifiedNameValueVariableTestAsync() {
            await GetTests().NodeReadStaticArrayQualifiedNameValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticArrayLocalizedTextValueVariableTestAsync() {
            await GetTests().NodeReadStaticArrayLocalizedTextValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticArrayStatusCodeValueVariableTestAsync() {
            await GetTests().NodeReadStaticArrayStatusCodeValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticArrayVariantValueVariableTestAsync() {
            await GetTests().NodeReadStaticArrayVariantValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticArrayEnumerationValueVariableTestAsync() {
            await GetTests().NodeReadStaticArrayEnumerationValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticArrayStructureValueVariableTestAsync() {
            await GetTests().NodeReadStaticArrayStructureValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticArrayNumberValueVariableTestAsync() {
            await GetTests().NodeReadStaticArrayNumberValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticArrayIntegerValueVariableTestAsync() {
            await GetTests().NodeReadStaticArrayIntegerValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticArrayUIntegerValueVariableTestAsync() {
            await GetTests().NodeReadStaticArrayUIntegerValueVariableTestAsync();
        }
    }
}
