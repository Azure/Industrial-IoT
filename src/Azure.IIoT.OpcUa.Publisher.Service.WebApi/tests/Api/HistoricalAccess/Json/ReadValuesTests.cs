// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.WebApi.Api.HistoricalAccess.Json
{
    using Azure.IIoT.OpcUa.Publisher.Service.WebApi.Clients;
    using Azure.IIoT.OpcUa.Publisher.Service.Sdk.Clients;
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Testing.Tests;
    using Furly.Extensions.Serializers;
    using System.Threading.Tasks;
    using Xunit;

    [Collection(ReadCollection.Name)]
    public class ReadValuesTests : IClassFixture<WebAppFixture>
    {
        public ReadValuesTests(WebAppFixture factory, HistoricalAccessServer server)
        {
            _factory = factory;
            _server = server;
        }

        private HistoryReadValuesTests<string> GetTests()
        {
            var client = _factory.CreateClient(); // Call to create server
            var registry = _factory.Resolve<IEndpointManager>();
            var endpointId = registry.RegisterEndpointAsync(_server.GetConnection().Endpoint).Result;
            var serializer = _factory.Resolve<IJsonSerializer>();
            return new HistoryReadValuesTests<string>(_server, () => // Create an adapter over the api
                new HistoryWebApiAdapter(
                    new HistoryServiceClient(_factory,
                    new TestConfig(client.BaseAddress), serializer)), endpointId);
        }

        private readonly WebAppFixture _factory;
        private readonly HistoricalAccessServer _server;

        [Fact]
        public Task HistoryReadInt64ValuesTest1Async()
        {
            return GetTests().HistoryReadInt64ValuesTest1Async();
        }

        [Fact]
        public Task HistoryReadInt64ValuesTest2Async()
        {
            return GetTests().HistoryReadInt64ValuesTest2Async();
        }

        [Fact]
        public Task HistoryReadInt64ValuesTest3Async()
        {
            return GetTests().HistoryReadInt64ValuesTest3Async();
        }

        [Fact]
        public Task HistoryReadInt64ValuesTest4Async()
        {
            return GetTests().HistoryReadInt64ValuesTest4Async();
        }

        [SkippableFact]
        public Task HistoryStreamInt64ValuesTest1Async()
        {
            Skip.If(true, "not implemented yet");
            return GetTests().HistoryStreamInt64ValuesTest1Async();
        }

        [SkippableFact]
        public Task HistoryStreamInt64ValuesTest2Async()
        {
            Skip.If(true, "not implemented yet");
            return GetTests().HistoryStreamInt64ValuesTest2Async();
        }

        [SkippableFact]
        public Task HistoryStreamInt64ValuesTest3Async()
        {
            Skip.If(true, "not implemented yet");
            return GetTests().HistoryStreamInt64ValuesTest3Async();
        }

        [SkippableFact]
        public Task HistoryStreamInt64ValuesTest4Async()
        {
            Skip.If(true, "not implemented yet");
            return GetTests().HistoryStreamInt64ValuesTest4Async();
        }
    }
}
