// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Services.HistoricalEvents
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Services;
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Testing.Tests;
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
                () => new NodeServices<ConnectionModel>(_server.Client, _server.Parser,
                    _output.BuildLoggerFor<NodeServices<ConnectionModel>>(Logging.Level)),
                _server.GetConnection());
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
