// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.WebApi.Tests.Controllers.TestData.MsgPack
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
            _client = factory.CreateClientScope(output, TestSerializerType.MsgPack);
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
        public Task NodeReadStaticArrayBooleanValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticArrayBooleanValueVariableTestAsync();
        }

        [Fact]
        public Task NodeReadStaticArraySByteValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticArraySByteValueVariableTestAsync();
        }

        [Fact]
        public Task NodeReadStaticArrayByteValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticArrayByteValueVariableTestAsync();
        }

        [Fact]
        public Task NodeReadStaticArrayInt16ValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticArrayInt16ValueVariableTestAsync();
        }

        [Fact]
        public Task NodeReadStaticArrayUInt16ValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticArrayUInt16ValueVariableTestAsync();
        }

        [Fact]
        public Task NodeReadStaticArrayInt32ValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticArrayInt32ValueVariableTestAsync();
        }

        [Fact]
        public Task NodeReadStaticArrayUInt32ValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticArrayUInt32ValueVariableTestAsync();
        }

        [Fact]
        public Task NodeReadStaticArrayInt64ValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticArrayInt64ValueVariableTestAsync();
        }

        [Fact]
        public Task NodeReadStaticArrayUInt64ValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticArrayUInt64ValueVariableTestAsync();
        }

        [Fact]
        public Task NodeReadStaticArrayFloatValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticArrayFloatValueVariableTestAsync();
        }

        [Fact]
        public Task NodeReadStaticArrayDoubleValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticArrayDoubleValueVariableTestAsync();
        }

        [Fact]
        public Task NodeReadStaticArrayStringValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticArrayStringValueVariableTestAsync();
        }

        [Fact]
        public Task NodeReadStaticArrayDateTimeValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticArrayDateTimeValueVariableTestAsync();
        }

        [Fact]
        public Task NodeReadStaticArrayGuidValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticArrayGuidValueVariableTestAsync();
        }

        [Fact]
        public Task NodeReadStaticArrayByteStringValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticArrayByteStringValueVariableTestAsync();
        }

        [Fact]
        public Task NodeReadStaticArrayXmlElementValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticArrayXmlElementValueVariableTestAsync();
        }

        [Fact]
        public Task NodeReadStaticArrayNodeIdValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticArrayNodeIdValueVariableTestAsync();
        }

        [Fact]
        public Task NodeReadStaticArrayExpandedNodeIdValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticArrayExpandedNodeIdValueVariableTestAsync();
        }

        [Fact]
        public Task NodeReadStaticArrayQualifiedNameValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticArrayQualifiedNameValueVariableTestAsync();
        }

        [Fact]
        public Task NodeReadStaticArrayLocalizedTextValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticArrayLocalizedTextValueVariableTestAsync();
        }

        [Fact]
        public Task NodeReadStaticArrayStatusCodeValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticArrayStatusCodeValueVariableTestAsync();
        }

        [Fact]
        public Task NodeReadStaticArrayVariantValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticArrayVariantValueVariableTestAsync();
        }

        [Fact]
        public Task NodeReadStaticArrayEnumerationValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticArrayEnumerationValueVariableTestAsync();
        }

        [Fact]
        public Task NodeReadStaticArrayStructureValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticArrayStructureValueVariableTestAsync();
        }

        [Fact]
        public Task NodeReadStaticArrayNumberValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticArrayNumberValueVariableTestAsync();
        }

        [Fact]
        public Task NodeReadStaticArrayIntegerValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticArrayIntegerValueVariableTestAsync();
        }

        [Fact]
        public Task NodeReadStaticArrayUIntegerValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticArrayUIntegerValueVariableTestAsync();
        }
    }
}
