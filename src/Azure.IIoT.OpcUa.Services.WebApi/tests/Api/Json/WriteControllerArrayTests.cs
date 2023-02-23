// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Services.WebApi.Tests.Api.Json
{
    using Azure.IIoT.OpcUa.Publisher.Sdk.Services.Adapter;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Services.Sdk.Clients;
    using Azure.IIoT.OpcUa.Services.WebApi.Tests;
    using Azure.IIoT.OpcUa.Services.WebApi.Tests.Api;
    using Azure.IIoT.OpcUa.Shared.Models;
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

    [Collection(WriteJsonCollection.Name)]
    public class WriteControllerArrayTests : IClassFixture<WebApiTestFixture>
    {
        public WriteControllerArrayTests(WebApiTestFixture factory, TestServerFixture server)
        {
            _factory = factory;
            _server = server;
            _hostEntry = Try.Op(() => Dns.GetHostEntry(Utils.GetHostName()))
                ?? Try.Op(() => Dns.GetHostEntry("localhost"));
        }

        private WriteArrayValueTests<string> GetTests()
        {
            var client = _factory.CreateClient(); // Call to create server
            var registry = _factory.Resolve<IEndpointManager>();
            var endpointId = registry.RegisterEndpointAsync(Endpoint).Result;
            var log = _factory.Resolve<ILogger>();
            var serializer = _factory.Resolve<IJsonSerializer>();
            return new WriteArrayValueTests<string>(() => // Create an adapter over the api
                new TwinWebApiAdapter(
                    new TwinServiceClient(new HttpClient(_factory, log),
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
        private readonly TestServerFixture _server;
        private readonly IPHostEntry _hostEntry;

        [Fact]
        public async Task NodeWriteStaticArrayBooleanValueVariableTestAsync()
        {
            await GetTests().NodeWriteStaticArrayBooleanValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticArraySByteValueVariableTestAsync()
        {
            await GetTests().NodeWriteStaticArraySByteValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticArrayByteValueVariableTestAsync()
        {
            await GetTests().NodeWriteStaticArrayByteValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticArrayInt16ValueVariableTestAsync()
        {
            await GetTests().NodeWriteStaticArrayInt16ValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticArrayUInt16ValueVariableTestAsync()
        {
            await GetTests().NodeWriteStaticArrayUInt16ValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticArrayInt32ValueVariableTestAsync()
        {
            await GetTests().NodeWriteStaticArrayInt32ValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticArrayUInt32ValueVariableTestAsync()
        {
            await GetTests().NodeWriteStaticArrayUInt32ValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticArrayInt64ValueVariableTestAsync()
        {
            await GetTests().NodeWriteStaticArrayInt64ValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticArrayUInt64ValueVariableTestAsync()
        {
            await GetTests().NodeWriteStaticArrayUInt64ValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticArrayFloatValueVariableTestAsync()
        {
            await GetTests().NodeWriteStaticArrayFloatValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticArrayDoubleValueVariableTestAsync()
        {
            await GetTests().NodeWriteStaticArrayDoubleValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticArrayStringValueVariableTest1Async()
        {
            await GetTests().NodeWriteStaticArrayStringValueVariableTest1Async().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticArrayStringValueVariableTest2Async()
        {
            await GetTests().NodeWriteStaticArrayStringValueVariableTest2Async().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticArrayDateTimeValueVariableTestAsync()
        {
            await GetTests().NodeWriteStaticArrayDateTimeValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticArrayGuidValueVariableTestAsync()
        {
            await GetTests().NodeWriteStaticArrayGuidValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticArrayByteStringValueVariableTestAsync()
        {
            await GetTests().NodeWriteStaticArrayByteStringValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticArrayXmlElementValueVariableTestAsync()
        {
            await GetTests().NodeWriteStaticArrayXmlElementValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticArrayNodeIdValueVariableTestAsync()
        {
            await GetTests().NodeWriteStaticArrayNodeIdValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticArrayExpandedNodeIdValueVariableTestAsync()
        {
            await GetTests().NodeWriteStaticArrayExpandedNodeIdValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticArrayQualifiedNameValueVariableTestAsync()
        {
            await GetTests().NodeWriteStaticArrayQualifiedNameValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticArrayLocalizedTextValueVariableTestAsync()
        {
            await GetTests().NodeWriteStaticArrayLocalizedTextValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticArrayStatusCodeValueVariableTestAsync()
        {
            await GetTests().NodeWriteStaticArrayStatusCodeValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticArrayVariantValueVariableTest1Async()
        {
            await GetTests().NodeWriteStaticArrayVariantValueVariableTest1Async().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticArrayEnumerationValueVariableTestAsync()
        {
            await GetTests().NodeWriteStaticArrayEnumerationValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticArrayStructureValueVariableTestAsync()
        {
            await GetTests().NodeWriteStaticArrayStructureValueVariableTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticArrayNumberValueVariableTest1Async()
        {
            await GetTests().NodeWriteStaticArrayNumberValueVariableTest1Async().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticArrayNumberValueVariableTest2Async()
        {
            await GetTests().NodeWriteStaticArrayNumberValueVariableTest2Async().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticArrayIntegerValueVariableTest1Async()
        {
            await GetTests().NodeWriteStaticArrayIntegerValueVariableTest1Async().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticArrayIntegerValueVariableTest2Async()
        {
            await GetTests().NodeWriteStaticArrayIntegerValueVariableTest2Async().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticArrayUIntegerValueVariableTest1Async()
        {
            await GetTests().NodeWriteStaticArrayUIntegerValueVariableTest1Async().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeWriteStaticArrayUIntegerValueVariableTest2Async()
        {
            await GetTests().NodeWriteStaticArrayUIntegerValueVariableTest2Async().ConfigureAwait(false);
        }
    }
}
