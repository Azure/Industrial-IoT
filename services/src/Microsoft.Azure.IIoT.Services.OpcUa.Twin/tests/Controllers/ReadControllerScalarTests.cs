// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Twin.Controllers {
    using Microsoft.Azure.IIoT.Services.OpcUa.Twin.Controllers.Test;
    using Microsoft.Azure.IIoT.Http.Default;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.Api.Twin;
    using Microsoft.Azure.IIoT.OpcUa.Testing.Fixtures;
    using Microsoft.Azure.IIoT.OpcUa.Testing.Tests;
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Serilog;
    using System.Net;
    using System.Threading.Tasks;
    using Xunit;

    [Collection(ReadCollection.Name)]
    public class ReadControllerScalarTests : IClassFixture<WebAppFixture> {

        public ReadControllerScalarTests(WebAppFixture factory, TestServerFixture server) {
            _factory = factory;
            _server = server;
        }

        private ReadScalarValueTests<string> GetTests() {
            var client = _factory.CreateClient(); // Call to create server
            var module = _factory.Resolve<ITestModule>();
            module.Endpoint = Endpoint;
            var log = _factory.Resolve<ILogger>();
            return new ReadScalarValueTests<string>(() => // Create an adapter over the api
                new TwinAdapter(
                    new ControllerTestClient(
                       new HttpClient(_factory, log), new TestConfig(client.BaseAddress))),
                       "fakeid", (ep, n) => _server.Client.ReadValueAsync(Endpoint, n));
        }

        public EndpointModel Endpoint => new EndpointModel {
            Url = $"opc.tcp://{Dns.GetHostName()}:{_server.Port}/UA/SampleServer",
            Certificate = _server.Certificate?.RawData
        };

        private readonly WebAppFixture _factory;
        private readonly TestServerFixture _server;

        [Fact]
        public async Task NodeReadStaticScalarBooleanValueVariableTestAsync() {
            await GetTests().NodeReadStaticScalarBooleanValueVariableTestAsync();
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
