// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.v2.Twin.Api {
    using Azure.IIoT.OpcUa.Publisher.Module.Tests.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Shared.Models;
    using Azure.IIoT.OpcUa.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Testing.Tests;
    using Autofac;
    using Furly.Extensions.Utils;
    using Opc.Ua;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading.Tasks;
    using Xunit;

    [Collection(TestServerWriteCollection.Name)]
    public class ValueWriteArrayTests : IClassFixture<PublisherModuleFixture> {
        public ValueWriteArrayTests(TestServerFixture server, PublisherModuleFixture module) {
            _server = server;
            _module = module;
            _hostEntry = Try.Op(() => Dns.GetHostEntry(Utils.GetHostName()))
                ?? Try.Op(() => Dns.GetHostEntry("localhost"));
        }

        private WriteArrayValueTests<ConnectionModel> GetTests() {
            return new WriteArrayValueTests<ConnectionModel>(
                () => _module.HubContainer.Resolve<INodeServices<ConnectionModel>>(),
                new ConnectionModel {
                    Endpoint = new EndpointModel {
                        Url = $"opc.tcp://{_hostEntry?.HostName ?? "localhost"}:{_server.Port}/UA/SampleServer",
                        AlternativeUrls = _hostEntry?.AddressList
                        .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork)
                        .Select(ip => $"opc.tcp://{ip}:{_server.Port}/UA/SampleServer").ToHashSet(),
                        Certificate = _server.Certificate?.RawData?.ToThumbprint()
                    }
                }, (ep, n, s) => _server.Client.ReadValueAsync(new ConnectionModel {
                    Endpoint = new EndpointModel {
                        Url = ep.Endpoint.Url,
                        Certificate = _server.Certificate?.RawData?.ToThumbprint()
                    }
                }, n, s));
        }

        private readonly TestServerFixture _server;
        private readonly PublisherModuleFixture _module;
        private readonly IPHostEntry _hostEntry;

        [Fact]
        public async Task NodeWriteStaticArrayBooleanValueVariableTestAsync() {
            await GetTests().NodeWriteStaticArrayBooleanValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticArraySByteValueVariableTestAsync() {
            await GetTests().NodeWriteStaticArraySByteValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticArrayByteValueVariableTestAsync() {
            await GetTests().NodeWriteStaticArrayByteValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticArrayInt16ValueVariableTestAsync() {
            await GetTests().NodeWriteStaticArrayInt16ValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticArrayUInt16ValueVariableTestAsync() {
            await GetTests().NodeWriteStaticArrayUInt16ValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticArrayInt32ValueVariableTestAsync() {
            await GetTests().NodeWriteStaticArrayInt32ValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticArrayUInt32ValueVariableTestAsync() {
            await GetTests().NodeWriteStaticArrayUInt32ValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticArrayInt64ValueVariableTestAsync() {
            await GetTests().NodeWriteStaticArrayInt64ValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticArrayUInt64ValueVariableTestAsync() {
            await GetTests().NodeWriteStaticArrayUInt64ValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticArrayFloatValueVariableTestAsync() {
            await GetTests().NodeWriteStaticArrayFloatValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticArrayDoubleValueVariableTestAsync() {
            await GetTests().NodeWriteStaticArrayDoubleValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticArrayStringValueVariableTest1Async() {
            await GetTests().NodeWriteStaticArrayStringValueVariableTest1Async().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticArrayStringValueVariableTest2Async() {
            await GetTests().NodeWriteStaticArrayStringValueVariableTest2Async().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticArrayDateTimeValueVariableTestAsync() {
            await GetTests().NodeWriteStaticArrayDateTimeValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticArrayGuidValueVariableTestAsync() {
            await GetTests().NodeWriteStaticArrayGuidValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticArrayByteStringValueVariableTestAsync() {
            await GetTests().NodeWriteStaticArrayByteStringValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticArrayXmlElementValueVariableTestAsync() {
            await GetTests().NodeWriteStaticArrayXmlElementValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticArrayNodeIdValueVariableTestAsync() {
            await GetTests().NodeWriteStaticArrayNodeIdValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticArrayExpandedNodeIdValueVariableTestAsync() {
            await GetTests().NodeWriteStaticArrayExpandedNodeIdValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticArrayQualifiedNameValueVariableTestAsync() {
            await GetTests().NodeWriteStaticArrayQualifiedNameValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticArrayLocalizedTextValueVariableTestAsync() {
            await GetTests().NodeWriteStaticArrayLocalizedTextValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticArrayStatusCodeValueVariableTestAsync() {
            await GetTests().NodeWriteStaticArrayStatusCodeValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticArrayVariantValueVariableTest1Async() {
            await GetTests().NodeWriteStaticArrayVariantValueVariableTest1Async().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticArrayEnumerationValueVariableTestAsync() {
            await GetTests().NodeWriteStaticArrayEnumerationValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticArrayStructureValueVariableTestAsync() {
            await GetTests().NodeWriteStaticArrayStructureValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticArrayNumberValueVariableTest1Async() {
            await GetTests().NodeWriteStaticArrayNumberValueVariableTest1Async().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticArrayNumberValueVariableTest2Async() {
            await GetTests().NodeWriteStaticArrayNumberValueVariableTest2Async().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticArrayIntegerValueVariableTest1Async() {
            await GetTests().NodeWriteStaticArrayIntegerValueVariableTest1Async().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticArrayIntegerValueVariableTest2Async() {
            await GetTests().NodeWriteStaticArrayIntegerValueVariableTest2Async().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticArrayUIntegerValueVariableTest1Async() {
            await GetTests().NodeWriteStaticArrayUIntegerValueVariableTest1Async().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticArrayUIntegerValueVariableTest2Async() {
            await GetTests().NodeWriteStaticArrayUIntegerValueVariableTest2Async().ConfigureAwait(false);
        }
    }
}
