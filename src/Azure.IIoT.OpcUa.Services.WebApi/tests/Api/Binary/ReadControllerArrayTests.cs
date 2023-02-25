// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Services.WebApi.Tests.Api.Binary
{
    using Azure.IIoT.OpcUa.Publisher.Sdk.Services.Adapter;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Services.Sdk.Clients;
    using Azure.IIoT.OpcUa.Services.WebApi.Tests;
    using Azure.IIoT.OpcUa.Services.WebApi.Tests.Api;
    using Azure.IIoT.OpcUa.Models;
    using Azure.IIoT.OpcUa.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Testing.Tests;
    using Furly.Extensions.Serializers;
    using Furly.Extensions.Utils;
    using Microsoft.Azure.IIoT.Http.Default;
    using Microsoft.Extensions.Logging;
    using Opc.Ua;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading.Tasks;
    using Xunit;

    [Collection(ReadBinaryCollection.Name)]
    public class ReadControllerArrayTests : IClassFixture<WebApiTestFixture>
    {
        public ReadControllerArrayTests(WebApiTestFixture factory, TestDataServer server)
        {
            _factory = factory;
            _server = server;
            _hostEntry = Try.Op(() => Dns.GetHostEntry(Utils.GetHostName()))
                ?? Try.Op(() => Dns.GetHostEntry("localhost"));
        }

        private ReadArrayValueTests<string> GetTests()
        {
            var client = _factory.CreateClient(); // Call to create server
            var registry = _factory.Resolve<IEndpointManager>();
            var endpointId = registry.RegisterEndpointAsync(Endpoint).Result;
            var log = _factory.Resolve<ILogger>();
            var serializer = _factory.Resolve<IBinarySerializer>();
            return new ReadArrayValueTests<string>(() => // Create an adapter over the api
                new TwinWebApiAdapter(
                    new TwinServiceClient(_factory,
                    new TestConfig(client.BaseAddress), serializer)), endpointId,
                    (ep, n, s) => _server.Client.ReadValueAsync(new ConnectionModel { Endpoint = Endpoint }, n, s));
        }

        public EndpointModel Endpoint => new()
        {
            Url = $"opc.tcp://{_hostEntry?.HostName ?? "localhost"}:{_server.Port}/UA/SampleServer",
            AlternativeUrls = _hostEntry?.AddressList
                .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork)
                .Select(ip => $"opc.tcp://{ip}:{_server.Port}/UA/SampleServer").ToHashSet(),
            Certificate = _server.Certificate?.RawData?.ToThumbprint()
        };

        private readonly WebApiTestFixture _factory;
        private readonly TestDataServer _server;
        private readonly IPHostEntry _hostEntry;

        [Fact]
        public async Task NodeReadAllStaticArrayVariableNodeClassTest1Async()
        {
            await GetTests().NodeReadAllStaticArrayVariableNodeClassTest1Async().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeReadAllStaticArrayVariableAccessLevelTest1Async()
        {
            await GetTests().NodeReadAllStaticArrayVariableAccessLevelTest1Async().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeReadAllStaticArrayVariableWriteMaskTest1Async()
        {
            await GetTests().NodeReadAllStaticArrayVariableWriteMaskTest1Async().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeReadAllStaticArrayVariableWriteMaskTest2Async()
        {
            await GetTests().NodeReadAllStaticArrayVariableWriteMaskTest2Async().ConfigureAwait(false);
        }

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
