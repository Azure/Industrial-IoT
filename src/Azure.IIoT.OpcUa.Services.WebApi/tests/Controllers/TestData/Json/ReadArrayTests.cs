// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Services.WebApi.Controllers.TestData.Json
{
    using Azure.IIoT.OpcUa.Services.WebApi.Clients;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Services.WebApi;
    using Azure.IIoT.OpcUa.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Testing.Tests;
    using Furly.Extensions.Serializers;
    using System.Threading.Tasks;
    using Xunit;

    [Collection(ReadCollection.Name)]
    public class ReadArrayTests : IClassFixture<WebAppFixture>
    {
        public ReadArrayTests(WebAppFixture factory, TestDataServer server)
        {
            _factory = factory;
            _server = server;
        }

        private ReadArrayValueTests<string> GetTests()
        {
            var client = _factory.CreateClient(); // Call to create server
            var registry = _factory.Resolve<IEndpointManager>();
            var endpointId = registry.RegisterEndpointAsync(_server.GetConnection().Endpoint).Result;
            var serializer = _factory.Resolve<IJsonSerializer>();
            return new ReadArrayValueTests<string>(() => // Create an adapter over the api
                new TwinWebApiAdapter(
                    new ControllerTestClient(_factory,
                    new TestConfig(client.BaseAddress), serializer)), endpointId,
                    (ep, n, s) => _server.Client.ReadValueAsync(_server.GetConnection(), n, s));
        }

        private readonly WebAppFixture _factory;
        private readonly TestDataServer _server;

        [Fact]
        public async Task NodeReadStaticArrayBooleanValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticArrayBooleanValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeReadStaticArraySByteValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticArraySByteValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeReadStaticArrayByteValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticArrayByteValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeReadStaticArrayInt16ValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticArrayInt16ValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeReadStaticArrayUInt16ValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticArrayUInt16ValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeReadStaticArrayInt32ValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticArrayInt32ValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeReadStaticArrayUInt32ValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticArrayUInt32ValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeReadStaticArrayInt64ValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticArrayInt64ValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeReadStaticArrayUInt64ValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticArrayUInt64ValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeReadStaticArrayFloatValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticArrayFloatValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeReadStaticArrayDoubleValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticArrayDoubleValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeReadStaticArrayStringValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticArrayStringValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeReadStaticArrayDateTimeValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticArrayDateTimeValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeReadStaticArrayGuidValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticArrayGuidValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeReadStaticArrayByteStringValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticArrayByteStringValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeReadStaticArrayXmlElementValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticArrayXmlElementValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeReadStaticArrayNodeIdValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticArrayNodeIdValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeReadStaticArrayExpandedNodeIdValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticArrayExpandedNodeIdValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeReadStaticArrayQualifiedNameValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticArrayQualifiedNameValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeReadStaticArrayLocalizedTextValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticArrayLocalizedTextValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeReadStaticArrayStatusCodeValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticArrayStatusCodeValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeReadStaticArrayVariantValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticArrayVariantValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeReadStaticArrayEnumerationValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticArrayEnumerationValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeReadStaticArrayStructureValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticArrayStructureValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeReadStaticArrayNumberValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticArrayNumberValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeReadStaticArrayIntegerValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticArrayIntegerValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeReadStaticArrayUIntegerValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticArrayUIntegerValueVariableTestAsync().ConfigureAwait(false);
        }
    }
}
