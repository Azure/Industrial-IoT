// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Services.WebApi.Api.HistoricalAccess.Json
{
    using Azure.IIoT.OpcUa.Services.WebApi.Clients;
    using Azure.IIoT.OpcUa.Services.Sdk.Clients;
    using Azure.IIoT.OpcUa.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Testing.Tests;
    using Furly.Extensions.Serializers;
    using System.Threading.Tasks;
    using Xunit;

    [Collection(ReadCollection.Name)]
    public class UpdateValuesTests
    {
        public UpdateValuesTests(WebAppFixture factory, HistoricalAccessServer server)
        {
            _factory = factory;
            _server = server;
        }

        private HistoryUpdateValuesTests<string> GetTests()
        {
            var client = _factory.CreateClient(); // Call to create server
            var registry = _factory.Resolve<IEndpointManager>();
            var endpointId = registry.RegisterEndpointAsync(_server.GetConnection().Endpoint).Result;
            var serializer = _factory.Resolve<IJsonSerializer>();
            return new HistoryUpdateValuesTests<string>(() => // Create an adapter over the api
                new HistoryWebApiAdapter(
                    new HistoryServiceClient(_factory,
                    new TestConfig(client.BaseAddress), serializer)), endpointId);
        }

        private readonly WebAppFixture _factory;
        private readonly HistoricalAccessServer _server;

        [Fact]
        public Task HistoryUpsertUInt32ValuesTest1Async()
        {
            return GetTests().HistoryUpsertUInt32ValuesTest1Async();
        }

        [Fact]
        public Task HistoryUpsertUInt32ValuesTest2Async()
        {
            return GetTests().HistoryUpsertUInt32ValuesTest2Async();
        }

        [Fact]
        public Task HistoryInsertUInt32ValuesTest1Async()
        {
            return GetTests().HistoryInsertUInt32ValuesTest1Async();
        }

        [Fact]
        public Task HistoryInsertUInt32ValuesTest2Async()
        {
            return GetTests().HistoryInsertUInt32ValuesTest2Async();
        }

        [Fact]
        public Task HistoryReplaceUInt32ValuesTest1Async()
        {
            return GetTests().HistoryReplaceUInt32ValuesTest1Async();
        }

        [Fact]
        public Task HistoryReplaceUInt32ValuesTest2Async()
        {
            return GetTests().HistoryReplaceUInt32ValuesTest2Async();
        }

        [Fact]
        public Task HistoryInsertDeleteUInt32ValuesTest1Async()
        {
            return GetTests().HistoryInsertDeleteUInt32ValuesTest1Async();
        }

        [Fact]
        public Task HistoryInsertDeleteUInt32ValuesTest2Async()
        {
            return GetTests().HistoryInsertDeleteUInt32ValuesTest2Async();
        }

        [Fact]
        public Task HistoryInsertDeleteUInt32ValuesTest3Async()
        {
            return GetTests().HistoryInsertDeleteUInt32ValuesTest3Async();
        }

        [Fact]
        public Task HistoryInsertDeleteUInt32ValuesTest4Async()
        {
            return GetTests().HistoryInsertDeleteUInt32ValuesTest3Async();
        }

        [Fact]
        public Task HistoryDeleteUInt32ValuesTest1Async()
        {
            return GetTests().HistoryDeleteUInt32ValuesTest1Async();
        }
    }
}
