// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Services.TestData.Tests
{
    using Azure.IIoT.OpcUa.Publisher.Services;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Shared.Models;
    using Azure.IIoT.OpcUa.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Testing.Tests;
    using Furly.Extensions.Utils;
    using Opc.Ua;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading.Tasks;
    using Xunit;

    [Collection(WriteCollection.Name)]
    public class ValueWriteScalarTests
    {
        public ValueWriteScalarTests(TestServerFixture server)
        {
            _server = server;
            _hostEntry = Try.Op(() => Dns.GetHostEntry(Utils.GetHostName()))
                ?? Try.Op(() => Dns.GetHostEntry("localhost"));
        }

        private WriteScalarValueTests<ConnectionModel> GetTests()
        {
            return new WriteScalarValueTests<ConnectionModel>(
                () => new NodeServices<ConnectionModel>(_server.Client,
                    _server.Logger), new ConnectionModel
                    {
                        Endpoint = new EndpointModel
                        {
                            Url = $"opc.tcp://{_hostEntry?.HostName ?? "localhost"}:{_server.Port}/UA/SampleServer",
                            AlternativeUrls = _hostEntry?.AddressList
                        .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork)
                        .Select(ip => $"opc.tcp://{ip}:{_server.Port}/UA/SampleServer").ToHashSet(),
                            Certificate = _server.Certificate?.RawData?.ToThumbprint()
                        }
                    }, (c, n, s) => _server.Client.ReadValueAsync(c, n, s));
        }

        private readonly TestServerFixture _server;
        private readonly IPHostEntry _hostEntry;

        [Fact]
        public async Task NodeWriteStaticScalarBooleanValueVariableTestAsync()
        {
            await GetTests().NodeWriteStaticScalarBooleanValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticScalarBooleanValueVariableWithBrowsePathTest1Async()
        {
            await GetTests().NodeWriteStaticScalarBooleanValueVariableWithBrowsePathTest1Async().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticScalarBooleanValueVariableWithBrowsePathTest2Async()
        {
            await GetTests().NodeWriteStaticScalarBooleanValueVariableWithBrowsePathTest2Async().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticScalarBooleanValueVariableWithBrowsePathTest3Async()
        {
            await GetTests().NodeWriteStaticScalarBooleanValueVariableWithBrowsePathTest3Async().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticScalarSByteValueVariableTestAsync()
        {
            await GetTests().NodeWriteStaticScalarSByteValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticScalarByteValueVariableTestAsync()
        {
            await GetTests().NodeWriteStaticScalarByteValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticScalarInt16ValueVariableTestAsync()
        {
            await GetTests().NodeWriteStaticScalarInt16ValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticScalarUInt16ValueVariableTestAsync()
        {
            await GetTests().NodeWriteStaticScalarUInt16ValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticScalarInt32ValueVariableTestAsync()
        {
            await GetTests().NodeWriteStaticScalarInt32ValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticScalarUInt32ValueVariableTestAsync()
        {
            await GetTests().NodeWriteStaticScalarUInt32ValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticScalarInt64ValueVariableTestAsync()
        {
            await GetTests().NodeWriteStaticScalarInt64ValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticScalarUInt64ValueVariableTestAsync()
        {
            await GetTests().NodeWriteStaticScalarUInt64ValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticScalarFloatValueVariableTestAsync()
        {
            await GetTests().NodeWriteStaticScalarFloatValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticScalarDoubleValueVariableTestAsync()
        {
            await GetTests().NodeWriteStaticScalarDoubleValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticScalarStringValueVariableTestAsync()
        {
            await GetTests().NodeWriteStaticScalarStringValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticScalarDateTimeValueVariableTestAsync()
        {
            await GetTests().NodeWriteStaticScalarDateTimeValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticScalarGuidValueVariableTestAsync()
        {
            await GetTests().NodeWriteStaticScalarGuidValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticScalarByteStringValueVariableTestAsync()
        {
            await GetTests().NodeWriteStaticScalarByteStringValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticScalarXmlElementValueVariableTestAsync()
        {
            await GetTests().NodeWriteStaticScalarXmlElementValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticScalarNodeIdValueVariableTestAsync()
        {
            await GetTests().NodeWriteStaticScalarNodeIdValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticScalarExpandedNodeIdValueVariableTestAsync()
        {
            await GetTests().NodeWriteStaticScalarExpandedNodeIdValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticScalarQualifiedNameValueVariableTestAsync()
        {
            await GetTests().NodeWriteStaticScalarQualifiedNameValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticScalarLocalizedTextValueVariableTestAsync()
        {
            await GetTests().NodeWriteStaticScalarLocalizedTextValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticScalarStatusCodeValueVariableTestAsync()
        {
            await GetTests().NodeWriteStaticScalarStatusCodeValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticScalarVariantValueVariableTestAsync()
        {
            await GetTests().NodeWriteStaticScalarVariantValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticScalarEnumerationValueVariableTestAsync()
        {
            await GetTests().NodeWriteStaticScalarEnumerationValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticScalarStructuredValueVariableTestAsync()
        {
            await GetTests().NodeWriteStaticScalarStructuredValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticScalarNumberValueVariableTestAsync()
        {
            await GetTests().NodeWriteStaticScalarNumberValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticScalarIntegerValueVariableTestAsync()
        {
            await GetTests().NodeWriteStaticScalarIntegerValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticScalarUIntegerValueVariableTestAsync()
        {
            await GetTests().NodeWriteStaticScalarUIntegerValueVariableTestAsync().ConfigureAwait(false);
        }
    }
}
