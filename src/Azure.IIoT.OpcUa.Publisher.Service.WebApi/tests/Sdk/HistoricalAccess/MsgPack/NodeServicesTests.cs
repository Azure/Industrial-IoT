// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.WebApi.Tests.Sdk.HistoricalAccess.MsgPack
{
    using Azure.IIoT.OpcUa.Publisher.Service.WebApi.Tests.Clients;
    using Azure.IIoT.OpcUa.Publisher.Service.Sdk;
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Testing.Tests;
    using Autofac;
    using System;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;

    [Collection(ReadCollection.Name)]
    public sealed class NodeServicesTests : IClassFixture<WebAppFixture>, IDisposable
    {
        public NodeServicesTests(WebAppFixture factory, HistoricalAccessServer server, ITestOutputHelper output)
        {
            _factory = factory;
            _server = server;
            _client = factory.CreateClientScope(output, TestSerializerType.MsgPack);
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        private NodeHistoricalAccessTests<string> GetTests()
        {
            var client = _factory.CreateClient(); // Call to create server
            var registry = _factory.Resolve<IEndpointManager<string>>();
            var endpointId = registry.RegisterEndpointAsync(_server.GetConnection().Endpoint).Result;
            return new NodeHistoricalAccessTests<string>(() => // Create an adapter over the api
                new TwinWebApiAdapter(_client.Resolve<ITwinServiceApi>()), endpointId);
        }

        private readonly WebAppFixture _factory;
        private readonly HistoricalAccessServer _server;
        private readonly IContainer _client;

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

        [Fact]
        public Task HistoryGetInt16NodeHistoryConfiguration()
        {
            return GetTests().HistoryGetInt16NodeHistoryConfigurationAsync();
        }

        [Fact]
        public Task HistoryGetInt64NodeHistoryConfigurationAsync()
        {
            return GetTests().HistoryGetInt64NodeHistoryConfigurationAsync();
        }

        [Fact]
        public Task HistoryGetNodeHistoryConfigurationFromBadNode()
        {
            return GetTests().HistoryGetNodeHistoryConfigurationFromBadNodeAsync();
        }
    }
}
