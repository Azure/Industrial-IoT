// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.WebApi.Controllers.TestData.Json
{
    using Azure.IIoT.OpcUa.Publisher.Service.WebApi;
    using Azure.IIoT.OpcUa.Publisher.Service.WebApi.Clients;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Testing.Tests;
    using Furly.Extensions.Serializers;
    using System.Threading.Tasks;
    using Xunit;

    [Collection(ReadCollection.Name)]
    public class ReadScalarTests : IClassFixture<WebAppFixture>
    {
        public ReadScalarTests(WebAppFixture factory, TestDataServer server)
        {
            _factory = factory;
            _server = server;
        }

        private ReadScalarValueTests<string> GetTests()
        {
            var client = _factory.CreateClient(); // Call to create server
            var registry = _factory.Resolve<IEndpointManager>();
            var endpointId = registry.RegisterEndpointAsync(_server.GetConnection().Endpoint).Result;
            var serializer = _factory.Resolve<IJsonSerializer>();
            return new ReadScalarValueTests<string>(() => // Create an adapter over the api
                new TwinWebApiAdapter(
                    new ControllerTestClient(_factory,
                    new TestConfig(client.BaseAddress), serializer)), endpointId,
                    (ep, n, s) => _server.Client.ReadValueAsync(_server.GetConnection(), n, s));
        }

        private readonly WebAppFixture _factory;
        private readonly TestDataServer _server;

        [Fact]
        public async Task NodeReadStaticScalarBooleanValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticScalarBooleanValueVariableTestAsync().ConfigureAwait(false);
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
