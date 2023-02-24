// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Services.HistoricalEvents.Tests
{
    using Azure.IIoT.OpcUa.Shared.Models;
    using Azure.IIoT.OpcUa.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Testing.Tests;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;

    [Collection(ReadCollection.Name)]
    public class NodeServicesTests
    {
        public NodeServicesTests(HistoricalEventsServer server, ITestOutputHelper output)
        {
            _server = server;
            _output = output;
        }

        private NodeHistoricalEventsTests<ConnectionModel> GetTests()
        {
            return new NodeHistoricalEventsTests<ConnectionModel>(
                () => new NodeServices<ConnectionModel>(_server.Client,
                    _output.BuildLogger()), _server.GetConnection());
        }

        private readonly HistoricalEventsServer _server;
        private readonly ITestOutputHelper _output;

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
