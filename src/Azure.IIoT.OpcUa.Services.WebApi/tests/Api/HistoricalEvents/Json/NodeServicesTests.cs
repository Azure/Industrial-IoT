// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Services.WebApi.Tests.Api.HistoricalEvents.Json
{
    using Azure.IIoT.OpcUa.Publisher.Sdk.Services.Adapter;
    using Azure.IIoT.OpcUa.Services.Sdk.Clients;
    using Azure.IIoT.OpcUa.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Testing.Tests;
    using Furly.Extensions.Serializers;
    using Microsoft.Extensions.Logging;
    using System.Threading.Tasks;
    using Xunit;

    [Collection(ReadCollection.Name)]
    public class NodeServicesTests
    {
        public NodeServicesTests(WebAppFixture factory, HistoricalEventsServer server)
        {
            _factory = factory;
            _server = server;
        }

        private NodeHistoricalEventsTests<string> GetTests()
        {
            var client = _factory.CreateClient(); // Call to create server
            var registry = _factory.Resolve<IEndpointManager>();
            var endpointId = registry.RegisterEndpointAsync(_server.GetConnection().Endpoint).Result;
            var serializer = _factory.Resolve<IJsonSerializer>();
            return new NodeHistoricalEventsTests<string>(() => // Create an adapter over the api
                new TwinWebApiAdapter(
                    new TwinServiceClient(_factory,
                    new TestConfig(client.BaseAddress), serializer)), endpointId);
        }

        private readonly WebAppFixture _factory;
        private readonly HistoricalEventsServer _server;

        [Fact]
        public Task GetServerCapabilitiesTestAsync()
        {
            return GetTests().GetServerCapabilitiesTestAsync();
        }

        [Fact]
        public Task HistoryGetServerCapabilitiesTestAsync()
        {
            return GetTests().HistoryGetServerCapabilitiesTestAsync();
        }
    }
}
