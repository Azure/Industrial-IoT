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
    public sealed class ReadArrayTests : IClassFixture<WebAppFixture>, IDisposable
    {
        public ReadArrayTests(WebAppFixture factory, TestDataServer server, ITestOutputHelper output)
        {
            _factory = factory;
            _server = server;
            _client = factory.CreateClientScope(output, TestSerializerType.NewtonsoftJson);
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        private ReadArrayValueTests<string> GetTests()
        {
            var client = _factory.CreateClient(); // Call to create server
            var registry = _factory.Resolve<IEndpointManager<string>>();
            var endpointId = registry.RegisterEndpointAsync(_server.GetConnection().Endpoint).Result;
            return new ReadArrayValueTests<string>(() => // Create an adapter over the api
                new TwinWebApiAdapter(_client.Resolve<ControllerTestClient>()), endpointId,
                    (ep, n, s) => _server.Client.ReadValueAsync(_server.GetConnection(), n, s));
        }

        private readonly WebAppFixture _factory;
        private readonly TestDataServer _server;
        private readonly IContainer _client;

        [Fact]
        public async Task NodeReadStaticArrayBooleanValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticArrayBooleanValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticArraySByteValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticArraySByteValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticArrayByteValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticArrayByteValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticArrayInt16ValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticArrayInt16ValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticArrayUInt16ValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticArrayUInt16ValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticArrayInt32ValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticArrayInt32ValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticArrayUInt32ValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticArrayUInt32ValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticArrayInt64ValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticArrayInt64ValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticArrayUInt64ValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticArrayUInt64ValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticArrayFloatValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticArrayFloatValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticArrayDoubleValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticArrayDoubleValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticArrayStringValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticArrayStringValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticArrayDateTimeValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticArrayDateTimeValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticArrayGuidValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticArrayGuidValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticArrayByteStringValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticArrayByteStringValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticArrayXmlElementValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticArrayXmlElementValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticArrayNodeIdValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticArrayNodeIdValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticArrayExpandedNodeIdValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticArrayExpandedNodeIdValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticArrayQualifiedNameValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticArrayQualifiedNameValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticArrayLocalizedTextValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticArrayLocalizedTextValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticArrayStatusCodeValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticArrayStatusCodeValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticArrayVariantValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticArrayVariantValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticArrayEnumerationValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticArrayEnumerationValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticArrayStructureValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticArrayStructureValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticArrayNumberValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticArrayNumberValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticArrayIntegerValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticArrayIntegerValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeReadStaticArrayUIntegerValueVariableTestAsync()
        {
            await GetTests().NodeReadStaticArrayUIntegerValueVariableTestAsync();
        }
    }
}
