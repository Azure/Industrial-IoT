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
    public class ReadModifiedTests : IClassFixture<WebAppFixture>
    {
        public ReadModifiedTests(WebAppFixture factory, HistoricalAccessServer server)
        {
            _factory = factory;
            _server = server;
        }

        private HistoryReadValuesModifiedTests<string> GetTests()
        {
            var client = _factory.CreateClient(); // Call to create server
            var registry = _factory.Resolve<IEndpointManager>();
            var endpointId = registry.RegisterEndpointAsync(_server.GetConnection().Endpoint).Result;
            var serializer = _factory.Resolve<IBinarySerializer>();
            return new HistoryReadValuesModifiedTests<string>(() => // Create an adapter over the api
                new HistoryWebApiAdapter(
                    new HistoryServiceClient(_factory,
                    new TestConfig(client.BaseAddress), serializer)), endpointId);
        }

        private readonly WebAppFixture _factory;
        private readonly HistoricalAccessServer _server;

        [Fact]
        public Task HistoryReadInt16ValuesModifiedTestAsync()
        {
            return GetTests().HistoryReadInt16ValuesModifiedTestAsync();
        }

        [SkippableFact]
        public Task HistoryStreamInt16ValuesModifiedTestAsync()
        {
            Skip.If(true, "not implemented yet");
            return GetTests().HistoryStreamInt16ValuesModifiedTestAsync();
        }
    }
}
