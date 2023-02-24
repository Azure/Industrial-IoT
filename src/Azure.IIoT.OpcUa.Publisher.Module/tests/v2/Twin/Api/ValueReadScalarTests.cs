// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.v2.Twin.Api
{
    using Autofac;
    using Azure.IIoT.OpcUa.Publisher.Module.Tests.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Models;
    using Azure.IIoT.OpcUa.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Testing.Tests;
    using Furly.Extensions.Utils;
    using Opc.Ua;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading.Tasks;
    using Xunit;

    [Collection(TestServerReadCollection.Name)]
    public class ValueReadScalarTests : IClassFixture<PublisherModuleFixture>
    {
        public ValueReadScalarTests(TestDataServer server, PublisherModuleFixture module)
        {
            _server = server;
            _module = module;
            _hostEntry = Try.Op(() => Dns.GetHostEntry(Utils.GetHostName()))
                ?? Try.Op(() => Dns.GetHostEntry("localhost"));
        }

        private ReadScalarValueTests<ConnectionModel> GetTests()
        {
            return new ReadScalarValueTests<ConnectionModel>(
                () => _module.HubContainer.Resolve<INodeServices<ConnectionModel>>(),
                new ConnectionModel
                {
                    Endpoint = new EndpointModel
                    {
                        Url = $"opc.tcp://{_hostEntry?.HostName ?? "localhost"}:{_server.Port}/UA/SampleServer",
                        AlternativeUrls = _hostEntry?.AddressList
                        .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork)
                        .Select(ip => $"opc.tcp://{ip}:{_server.Port}/UA/SampleServer").ToHashSet(),
                        Certificate = _server.Certificate?.RawData?.ToThumbprint()
                    }
                }, (ep, n, s) => _server.Client.ReadValueAsync(new ConnectionModel
                {
                    Endpoint = new EndpointModel
                    {
                        Url = ep.Endpoint.Url,
                        Certificate = _server.Certificate?.RawData?.ToThumbprint()
                    }
                }, n, s));
        }

        private readonly TestDataServer _server;
        private readonly PublisherModuleFixture _module;
        private readonly IPHostEntry _hostEntry;

        [Fact]
        public async Task NodeReadAllStaticScalarVariableNodeClassTest1Async()
        {
            await GetTests().NodeReadAllStaticScalarVariableNodeClassTest1Async().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeReadAllStaticScalarVariableAccessLevelTest1Async()
        {
            await GetTests().NodeReadAllStaticScalarVariableAccessLevelTest1Async().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeReadAllStaticScalarVariableWriteMaskTest1Async()
        {
            await GetTests().NodeReadAllStaticScalarVariableWriteMaskTest1Async().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeReadAllStaticScalarVariableWriteMaskTest2Async()
        {
            await GetTests().NodeReadAllStaticScalarVariableWriteMaskTest2Async().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeReadStaticScalarBooleanValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticScalarBooleanValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeReadStaticScalarBooleanValueVariableWithBrowsePathTest1Async()
        {
            await GetTests().NodeReadStaticScalarBooleanValueVariableWithBrowsePathTest1Async().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeReadStaticScalarBooleanValueVariableWithBrowsePathTest2Async()
        {
            await GetTests().NodeReadStaticScalarBooleanValueVariableWithBrowsePathTest2Async().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeReadStaticScalarBooleanValueVariableWithBrowsePathTest3Async()
        {
            await GetTests().NodeReadStaticScalarBooleanValueVariableWithBrowsePathTest3Async().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeReadStaticScalarSByteValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticScalarSByteValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeReadStaticScalarByteValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticScalarByteValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeReadStaticScalarInt16ValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticScalarInt16ValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeReadStaticScalarUInt16ValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticScalarUInt16ValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeReadStaticScalarInt32ValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticScalarInt32ValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeReadStaticScalarUInt32ValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticScalarUInt32ValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeReadStaticScalarInt64ValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticScalarInt64ValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeReadStaticScalarUInt64ValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticScalarUInt64ValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeReadStaticScalarFloatValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticScalarFloatValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeReadStaticScalarDoubleValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticScalarDoubleValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeReadStaticScalarStringValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticScalarStringValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeReadStaticScalarDateTimeValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticScalarDateTimeValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeReadStaticScalarGuidValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticScalarGuidValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeReadStaticScalarByteStringValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticScalarByteStringValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeReadStaticScalarXmlElementValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticScalarXmlElementValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeReadStaticScalarNodeIdValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticScalarNodeIdValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeReadStaticScalarExpandedNodeIdValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticScalarExpandedNodeIdValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeReadStaticScalarQualifiedNameValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticScalarQualifiedNameValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeReadStaticScalarLocalizedTextValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticScalarLocalizedTextValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeReadStaticScalarStatusCodeValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticScalarStatusCodeValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeReadStaticScalarVariantValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticScalarVariantValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeReadStaticScalarEnumerationValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticScalarEnumerationValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeReadStaticScalarStructuredValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticScalarStructuredValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeReadStaticScalarNumberValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticScalarNumberValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeReadStaticScalarIntegerValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticScalarIntegerValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeReadStaticScalarUIntegerValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticScalarUIntegerValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeReadDataAccessMeasurementFloatValueTestAsync()
        {
            await GetTests().NodeReadDataAccessMeasurementFloatValueTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeReadDiagnosticsNoneTestAsync()
        {
            await GetTests().NodeReadDiagnosticsNoneTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeReadDiagnosticsStatusTestAsync()
        {
            await GetTests().NodeReadDiagnosticsStatusTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeReadDiagnosticsDebugTestAsync()
        {
            await GetTests().NodeReadDiagnosticsStatusTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeReadDiagnosticsVerboseTestAsync()
        {
            await GetTests().NodeReadDiagnosticsStatusTestAsync().ConfigureAwait(false);
        }
    }
}
