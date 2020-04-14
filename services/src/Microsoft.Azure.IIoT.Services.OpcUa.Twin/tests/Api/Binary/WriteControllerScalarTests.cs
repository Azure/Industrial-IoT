// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Twin.Api.Binary {
    using Microsoft.Azure.IIoT.Http.Default;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.Api.Twin;
    using Microsoft.Azure.IIoT.OpcUa.Api.Twin.Clients;
    using Microsoft.Azure.IIoT.OpcUa.Testing.Fixtures;
    using Microsoft.Azure.IIoT.OpcUa.Testing.Tests;
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.Serializers;
    using Serilog;
    using System.Net;
    using System.Threading.Tasks;
    using Xunit;

    [Collection(WriteBinaryCollection.Name)]
    public class WriteControllerScalarTests : IClassFixture<WebAppFixture> {

        public WriteControllerScalarTests(WebAppFixture factory, TestServerFixture server) {
            _factory = factory;
            _server = server;
        }

        private WriteScalarValueTests<string> GetTests() {
            var client = _factory.CreateClient(); // Call to create server
            var module = _factory.Resolve<ITestModule>();
            module.Endpoint = Endpoint;
            var log = _factory.Resolve<ILogger>();
            var serializer = _factory.Resolve<IBinarySerializer>();
            return new WriteScalarValueTests<string>(() => // Create an adapter over the api
                new TwinServicesApiAdapter(
                    new TwinServiceClient(new HttpClient(_factory, log),
                    new TestConfig(client.BaseAddress), serializer)),
                        "fakeid", (ep, n) => _server.Client.ReadValueAsync(Endpoint, n));
        }

        public EndpointModel Endpoint => new EndpointModel {
            Url = $"opc.tcp://{Dns.GetHostName()}:{_server.Port}/UA/SampleServer",
            Certificate = _server.Certificate?.RawData?.ToThumbprint()
        };

        private readonly WebAppFixture _factory;
        private readonly TestServerFixture _server;

        [Fact]
        public async Task NodeWriteStaticScalarBooleanValueVariableTestAsync() {
            await GetTests().NodeWriteStaticScalarBooleanValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticScalarBooleanValueVariableWithBrowsePathTest1Async() {
            await GetTests().NodeWriteStaticScalarBooleanValueVariableWithBrowsePathTest1Async();
        }

        [Fact]
        public async Task NodeWriteStaticScalarBooleanValueVariableWithBrowsePathTest2Async() {
            await GetTests().NodeWriteStaticScalarBooleanValueVariableWithBrowsePathTest2Async();
        }

        [Fact]
        public async Task NodeWriteStaticScalarBooleanValueVariableWithBrowsePathTest3Async() {
            await GetTests().NodeWriteStaticScalarBooleanValueVariableWithBrowsePathTest3Async();
        }

        [Fact]
        public async Task NodeWriteStaticScalarSByteValueVariableTestAsync() {
            await GetTests().NodeWriteStaticScalarSByteValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticScalarByteValueVariableTestAsync() {
            await GetTests().NodeWriteStaticScalarByteValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticScalarInt16ValueVariableTestAsync() {
            await GetTests().NodeWriteStaticScalarInt16ValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticScalarUInt16ValueVariableTestAsync() {
            await GetTests().NodeWriteStaticScalarUInt16ValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticScalarInt32ValueVariableTestAsync() {
            await GetTests().NodeWriteStaticScalarInt32ValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticScalarUInt32ValueVariableTestAsync() {
            await GetTests().NodeWriteStaticScalarUInt32ValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticScalarInt64ValueVariableTestAsync() {
            await GetTests().NodeWriteStaticScalarInt64ValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticScalarUInt64ValueVariableTestAsync() {
            await GetTests().NodeWriteStaticScalarUInt64ValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticScalarFloatValueVariableTestAsync() {
            await GetTests().NodeWriteStaticScalarFloatValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticScalarDoubleValueVariableTestAsync() {
            await GetTests().NodeWriteStaticScalarDoubleValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticScalarStringValueVariableTestAsync() {
            await GetTests().NodeWriteStaticScalarStringValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticScalarDateTimeValueVariableTestAsync() {
            await GetTests().NodeWriteStaticScalarDateTimeValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticScalarGuidValueVariableTestAsync() {
            await GetTests().NodeWriteStaticScalarGuidValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticScalarByteStringValueVariableTestAsync() {
            await GetTests().NodeWriteStaticScalarByteStringValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticScalarXmlElementValueVariableTestAsync() {
            await GetTests().NodeWriteStaticScalarXmlElementValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticScalarNodeIdValueVariableTestAsync() {
            await GetTests().NodeWriteStaticScalarNodeIdValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticScalarExpandedNodeIdValueVariableTestAsync() {
            await GetTests().NodeWriteStaticScalarExpandedNodeIdValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticScalarQualifiedNameValueVariableTestAsync() {
            await GetTests().NodeWriteStaticScalarQualifiedNameValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticScalarLocalizedTextValueVariableTestAsync() {
            await GetTests().NodeWriteStaticScalarLocalizedTextValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticScalarStatusCodeValueVariableTestAsync() {
            await GetTests().NodeWriteStaticScalarStatusCodeValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticScalarVariantValueVariableTestAsync() {
            await GetTests().NodeWriteStaticScalarVariantValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticScalarEnumerationValueVariableTestAsync() {
            await GetTests().NodeWriteStaticScalarEnumerationValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticScalarStructuredValueVariableTestAsync() {
            await GetTests().NodeWriteStaticScalarStructuredValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticScalarNumberValueVariableTestAsync() {
            await GetTests().NodeWriteStaticScalarNumberValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticScalarIntegerValueVariableTestAsync() {
            await GetTests().NodeWriteStaticScalarIntegerValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticScalarUIntegerValueVariableTestAsync() {
            await GetTests().NodeWriteStaticScalarUIntegerValueVariableTestAsync();
        }

    }
}
