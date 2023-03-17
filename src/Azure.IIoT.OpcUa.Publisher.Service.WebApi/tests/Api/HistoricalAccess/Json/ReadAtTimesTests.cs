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
    public class ReadAtTimesTests : IClassFixture<WebAppFixture>
    {
        public ReadAtTimesTests(WebAppFixture factory, HistoricalAccessServer server)
        {
            _factory = factory;
            _server = server;
        }

        private HistoryReadValuesAtTimesTests<string> GetTests()
        {
            var client = _factory.CreateClient(); // Call to create server
            var registry = _factory.Resolve<IEndpointManager>();
            var endpointId = registry.RegisterEndpointAsync(_server.GetConnection().Endpoint).Result;
            var serializer = _factory.Resolve<IJsonSerializer>();
            return new HistoryReadValuesAtTimesTests<string>(_server, () => // Create an adapter over the api
                new HistoryWebApiAdapter(
                    new HistoryServiceClient(_factory,
                    new TestConfig(client.BaseAddress), serializer)), endpointId);
        }

        private readonly WebAppFixture _factory;
        private readonly HistoricalAccessServer _server;

        [Fact]
        public Task HistoryReadInt32ValuesAtTimesTest1Async()
        {
            return GetTests().HistoryReadInt32ValuesAtTimesTest1Async();
        }

        [Fact]
        public Task HistoryReadInt32ValuesAtTimesAtTimesTest2Async()
        {
            return GetTests().HistoryReadInt32ValuesAtTimesTest2Async();
        }

        [Fact]
        public Task HistoryReadInt32ValuesAtTimesTest3Async()
        {
            return GetTests().HistoryReadInt32ValuesAtTimesTest3Async();
        }

        [Fact]
        public Task HistoryReadInt32ValuesAtTimesTest4Async()
        {
            return GetTests().HistoryReadInt32ValuesAtTimesTest4Async();
        }

        [SkippableFact]
        public Task HistoryStreamInt32ValuesAtTimesTest1Async()
        {
            Skip.If(true, "not implemented yet");
            return GetTests().HistoryStreamInt32ValuesAtTimesTest1Async();
        }

        [SkippableFact]
        public Task HistoryStreamInt32ValuesAtTimesTest2Async()
        {
            Skip.If(true, "not implemented yet");
            return GetTests().HistoryStreamInt32ValuesAtTimesTest2Async();
        }
    }
}
