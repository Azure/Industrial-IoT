// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Twin.Api.Binary {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.Api.Twin;
    using Microsoft.Azure.IIoT.OpcUa.Api.Twin.Clients;
    using Microsoft.Azure.IIoT.OpcUa.Testing.Fixtures;
    using Microsoft.Azure.IIoT.OpcUa.Testing.Tests;
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.Http.Default;
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Azure.IIoT.Utils;
    using Opc.Ua;
    using Serilog;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading.Tasks;
    using Xunit;

    [Collection(WriteBinaryCollection.Name)]
    public class WriteControllerArrayTests : IClassFixture<WebAppFixture> {

        public WriteControllerArrayTests(WebAppFixture factory, TestServerFixture server) {
            _factory = factory;
            _server = server;
            _hostEntry = Try.Op(() => Dns.GetHostEntry(Utils.GetHostName()))
                ?? Try.Op(() => Dns.GetHostEntry("localhost"));
        }

        private WriteArrayValueTests<string> GetTests() {
            var client = _factory.CreateClient(); // Call to create server
            var module = _factory.Resolve<ITestModule>();
            module.Endpoint = Endpoint;
            var log = _factory.Resolve<ILogger>();
            var serializer = _factory.Resolve<IBinarySerializer>();
            return new WriteArrayValueTests<string>(() => // Create an adapter over the api
                new TwinServicesApiAdapter(
                    new TwinServiceClient(new HttpClient(_factory, log),
                    new TestConfig(client.BaseAddress), serializer)), "fakeid",
                    (ep, n) => _server.Client.ReadValueAsync(Endpoint, n));
        }

        public EndpointModel Endpoint => new EndpointModel {
            Url = $"opc.tcp://{_hostEntry?.HostName ?? "localhost"}:{_server.Port}/UA/SampleServer",
            AlternativeUrls = _hostEntry?.AddressList
                .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork)
                .Select(ip => $"opc.tcp://{ip}:{_server.Port}/UA/SampleServer").ToHashSet(),
            Certificate = _server.Certificate?.RawData?.ToThumbprint()
        };

        private readonly WebAppFixture _factory;
        private readonly TestServerFixture _server;
        private readonly IPHostEntry _hostEntry;

        [Fact]
        public async Task NodeWriteStaticArrayBooleanValueVariableTestAsync() {
            await GetTests().NodeWriteStaticArrayBooleanValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticArraySByteValueVariableTestAsync() {
            await GetTests().NodeWriteStaticArraySByteValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticArrayByteValueVariableTestAsync() {
            await GetTests().NodeWriteStaticArrayByteValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticArrayInt16ValueVariableTestAsync() {
            await GetTests().NodeWriteStaticArrayInt16ValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticArrayUInt16ValueVariableTestAsync() {
            await GetTests().NodeWriteStaticArrayUInt16ValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticArrayInt32ValueVariableTestAsync() {
            await GetTests().NodeWriteStaticArrayInt32ValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticArrayUInt32ValueVariableTestAsync() {
            await GetTests().NodeWriteStaticArrayUInt32ValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticArrayInt64ValueVariableTestAsync() {
            await GetTests().NodeWriteStaticArrayInt64ValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticArrayUInt64ValueVariableTestAsync() {
            await GetTests().NodeWriteStaticArrayUInt64ValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticArrayFloatValueVariableTestAsync() {
            await GetTests().NodeWriteStaticArrayFloatValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticArrayDoubleValueVariableTestAsync() {
            await GetTests().NodeWriteStaticArrayDoubleValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticArrayStringValueVariableTest1Async() {
            await GetTests().NodeWriteStaticArrayStringValueVariableTest1Async();
        }

        [Fact]
        public async Task NodeWriteStaticArrayStringValueVariableTest2Async() {
            await GetTests().NodeWriteStaticArrayStringValueVariableTest2Async();
        }

        [Fact]
        public async Task NodeWriteStaticArrayDateTimeValueVariableTestAsync() {
            await GetTests().NodeWriteStaticArrayDateTimeValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticArrayGuidValueVariableTestAsync() {
            await GetTests().NodeWriteStaticArrayGuidValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticArrayByteStringValueVariableTestAsync() {
            await GetTests().NodeWriteStaticArrayByteStringValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticArrayXmlElementValueVariableTestAsync() {
            await GetTests().NodeWriteStaticArrayXmlElementValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticArrayNodeIdValueVariableTestAsync() {
            await GetTests().NodeWriteStaticArrayNodeIdValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticArrayExpandedNodeIdValueVariableTestAsync() {
            await GetTests().NodeWriteStaticArrayExpandedNodeIdValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticArrayQualifiedNameValueVariableTestAsync() {
            await GetTests().NodeWriteStaticArrayQualifiedNameValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticArrayLocalizedTextValueVariableTestAsync() {
            await GetTests().NodeWriteStaticArrayLocalizedTextValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticArrayStatusCodeValueVariableTestAsync() {
            await GetTests().NodeWriteStaticArrayStatusCodeValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticArrayVariantValueVariableTest1Async() {
            await GetTests().NodeWriteStaticArrayVariantValueVariableTest1Async();
        }

        [Fact]
        public async Task NodeWriteStaticArrayEnumerationValueVariableTestAsync() {
            await GetTests().NodeWriteStaticArrayEnumerationValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticArrayStructureValueVariableTestAsync() {
            await GetTests().NodeWriteStaticArrayStructureValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticArrayNumberValueVariableTest1Async() {
            await GetTests().NodeWriteStaticArrayNumberValueVariableTest1Async();
        }

        [Fact]
        public async Task NodeWriteStaticArrayNumberValueVariableTest2Async() {
            await GetTests().NodeWriteStaticArrayNumberValueVariableTest2Async();
        }

        [Fact]
        public async Task NodeWriteStaticArrayIntegerValueVariableTest1Async() {
            await GetTests().NodeWriteStaticArrayIntegerValueVariableTest1Async();
        }

        [Fact]
        public async Task NodeWriteStaticArrayIntegerValueVariableTest2Async() {
            await GetTests().NodeWriteStaticArrayIntegerValueVariableTest2Async();
        }

        [Fact]
        public async Task NodeWriteStaticArrayUIntegerValueVariableTest1Async() {
            await GetTests().NodeWriteStaticArrayUIntegerValueVariableTest1Async();
        }

        [Fact]
        public async Task NodeWriteStaticArrayUIntegerValueVariableTest2Async() {
            await GetTests().NodeWriteStaticArrayUIntegerValueVariableTest2Async();
        }

    }
}
