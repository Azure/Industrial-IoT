// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.WebApi.Api.HistoricalAccess.Binary
{
    using Azure.IIoT.OpcUa.Publisher.Service.WebApi.Clients;
    using Azure.IIoT.OpcUa.Publisher.Service.Sdk.Clients;
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Testing.Tests;
    using Furly.Extensions.Serializers;
    using System.Threading.Tasks;
    using Xunit;

    [Collection(ReadCollection.Name)]
    public class ReadProcessedTests : IClassFixture<WebAppFixture>
    {
        public ReadProcessedTests(WebAppFixture factory, HistoricalAccessServer server)
        {
            _factory = factory;
            _server = server;
        }

        private HistoryReadValuesProcessedTests<string> GetTests()
        {
            var client = _factory.CreateClient(); // Call to create server
            var registry = _factory.Resolve<IEndpointManager>();
            var endpointId = registry.RegisterEndpointAsync(_server.GetConnection().Endpoint).Result;
            var serializer = _factory.Resolve<IBinarySerializer>();
            return new HistoryReadValuesProcessedTests<string>(_server, () => // Create an adapter over the api
                new HistoryWebApiAdapter(
                    new HistoryServiceClient(_factory,
                    new TestConfig(client.BaseAddress), serializer)), endpointId);
        }

        private readonly WebAppFixture _factory;
        private readonly HistoricalAccessServer _server;

        [Fact]
        public Task HistoryReadUInt64ProcessedValuesTest1Async()
        {
            return GetTests().HistoryReadUInt64ProcessedValuesTest1Async();
        }

        [Fact]
        public Task HistoryReadUInt64ProcessedValuesTest2Async()
        {
            return GetTests().HistoryReadUInt64ProcessedValuesTest2Async();
        }

        [Fact]
        public Task HistoryReadUInt64ProcessedValuesTest3Async()
        {
            return GetTests().HistoryReadUInt64ProcessedValuesTest3Async();
        }

        [SkippableFact]
        public Task HistoryStreamUInt64ProcessedValuesTest1Async()
        {
            Skip.If(true, "not implemented yet");
            return GetTests().HistoryStreamUInt64ProcessedValuesTest1Async();
        }

        [SkippableFact]
        public Task HistoryStreamUInt64ProcessedValuesTest2Async()
        {
            Skip.If(true, "not implemented yet");
            return GetTests().HistoryStreamUInt64ProcessedValuesTest2Async();
        }

        [SkippableFact]
        public Task HistoryStreamUInt64ProcessedValuesTest3Async()
        {
            Skip.If(true, "not implemented yet");
            return GetTests().HistoryStreamUInt64ProcessedValuesTest3Async();
        }
    }
}
