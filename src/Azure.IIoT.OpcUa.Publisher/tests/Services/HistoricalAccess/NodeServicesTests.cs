// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Services.HistoricalAccess
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Services;
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Testing.Tests;
    using Microsoft.Extensions.Configuration;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;

    [Collection(ReadCollection.Name)]
    public class NodeServicesTests
    {
        public NodeServicesTests(HistoricalAccessServer server, ITestOutputHelper output)
        {
            _server = server;
            _output = output;
        }

        private NodeHistoricalAccessTests<ConnectionModel> GetTests()
        {
            return new NodeHistoricalAccessTests<ConnectionModel>(
                () => new NodeServices<ConnectionModel>(_server.Client, _server.Parser,
                    _output.BuildLoggerFor<NodeServices<ConnectionModel>>(Logging.Level),
                    new PublisherConfig(new ConfigurationBuilder().Build()).ToOptions()),
                _server.GetConnection());
        }

        private readonly HistoricalAccessServer _server;
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
