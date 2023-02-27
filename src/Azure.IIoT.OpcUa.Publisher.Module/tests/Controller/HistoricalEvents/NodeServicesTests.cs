// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Controller.HistoricalEvents
{
    using Azure.IIoT.OpcUa.Publisher.Module.Tests.Fixtures;
    using Azure.IIoT.OpcUa.Models;
    using Azure.IIoT.OpcUa.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Testing.Tests;
    using Autofac;
    using System.Threading.Tasks;
    using Xunit;

    [Collection(ReadCollection.Name)]
    public class NodeServicesTests : IClassFixture<PublisherModuleFixture>
    {
        public NodeServicesTests(HistoricalEventsServer server, PublisherModuleFixture module)
        {
            _server = server;
            _module = module;
        }

        private NodeHistoricalEventsTests<ConnectionModel> GetTests()
        {
            return new NodeHistoricalEventsTests<ConnectionModel>(
                () => _module.HubContainer.Resolve<INodeServices<ConnectionModel>>(),
                _server.GetConnection());
        }

        private readonly HistoricalEventsServer _server;
        private readonly PublisherModuleFixture _module;

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
