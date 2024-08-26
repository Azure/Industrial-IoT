// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.WebApi.Tests.Controllers.TestData.Json
{
    using Azure.IIoT.OpcUa.Publisher.Service.WebApi.Tests.Clients;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Testing.Tests;
    using Autofac;
    using System;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;

    [Collection(ReadCollection.Name)]
    public sealed class ReadScalarTests : IClassFixture<WebAppFixture>, IDisposable
    {
        public ReadScalarTests(WebAppFixture factory, TestDataServer server, ITestOutputHelper output)
        {
            _factory = factory;
            _server = server;
            _client = factory.CreateClientScope(output, TestSerializerType.NewtonsoftJson);
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        private ReadScalarValueTests<string> GetTests()
        {
            var client = _factory.CreateClient(); // Call to create server
            var registry = _factory.Resolve<IEndpointManager<string>>();
            var endpointId = registry.RegisterEndpointAsync(_server.GetConnection().Endpoint).Result;
            return new ReadScalarValueTests<string>(() => // Create an adapter over the api
                new TwinWebApiAdapter(_client.Resolve<ControllerTestClient>()), endpointId,
                    (ep, n, s) => _server.Client.ReadValueAsync(_server.GetConnection(), n, s));
        }

        private readonly WebAppFixture _factory;
        private readonly TestDataServer _server;
        private readonly IContainer _client;

        [Fact]
        public async Task NodeReadStaticScalarBooleanValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticScalarBooleanValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticScalarSByteValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticScalarSByteValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticScalarByteValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticScalarByteValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticScalarInt16ValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticScalarInt16ValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticScalarUInt16ValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticScalarUInt16ValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticScalarInt32ValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticScalarInt32ValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticScalarUInt32ValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticScalarUInt32ValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticScalarInt64ValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticScalarInt64ValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticScalarUInt64ValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticScalarUInt64ValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticScalarFloatValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticScalarFloatValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticScalarDoubleValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticScalarDoubleValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticScalarStringValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticScalarStringValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticScalarDateTimeValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticScalarDateTimeValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticScalarGuidValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticScalarGuidValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticScalarByteStringValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticScalarByteStringValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticScalarXmlElementValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticScalarXmlElementValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticScalarNodeIdValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticScalarNodeIdValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticScalarExpandedNodeIdValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticScalarExpandedNodeIdValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticScalarQualifiedNameValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticScalarQualifiedNameValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticScalarLocalizedTextValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticScalarLocalizedTextValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticScalarStatusCodeValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticScalarStatusCodeValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticScalarVariantValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticScalarVariantValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticScalarEnumerationValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticScalarEnumerationValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticScalarStructuredValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticScalarStructuredValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticScalarNumberValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticScalarNumberValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticScalarIntegerValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticScalarIntegerValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticScalarUIntegerValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticScalarUIntegerValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadDataAccessMeasurementFloatValueTestAsync()
        {
            await GetTests().NodeReadDataAccessMeasurementFloatValueTestAsync();
        }

        [Fact]
        public async Task NodeReadDiagnosticsStatusTestAsync()
        {
            await GetTests().NodeReadDiagnosticsStatusTestAsync();
        }

        [Fact]
        public async Task NodeReadDiagnosticsDebugTestAsync()
        {
            await GetTests().NodeReadDiagnosticsStatusTestAsync();
        }

        [Fact]
        public async Task NodeReadDiagnosticsVerboseTestAsync()
        {
            await GetTests().NodeReadDiagnosticsStatusTestAsync();
        }
    }
}
