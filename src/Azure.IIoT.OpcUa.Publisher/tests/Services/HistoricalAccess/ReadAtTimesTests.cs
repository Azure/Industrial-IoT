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
    public class ReadAtTimesTests
    {
        public ReadAtTimesTests(HistoricalAccessServer server, ITestOutputHelper output)
        {
            _server = server;
            _output = output;
        }

        private HistoryReadValuesAtTimesTests<ConnectionModel> GetTests()
        {
            return new HistoryReadValuesAtTimesTests<ConnectionModel>(_server,
                () => new HistoryServices<ConnectionModel>(
                    new PublisherConfig(new ConfigurationBuilder().Build()).ToOptions(),
                    new NodeServices<ConnectionModel>(_server.Client, _server.Parser,
                        _output.BuildLoggerFor<NodeServices<ConnectionModel>>(Logging.Level),
                        new PublisherConfig(new ConfigurationBuilder().Build()).ToOptions())),
                _server.GetConnection());
        }

        private readonly HistoricalAccessServer _server;
        private readonly ITestOutputHelper _output;

        [Fact]
        public Task HistoryReadInt32ValuesAtTimesTest1Async()
        {
            return GetTests().HistoryReadInt32ValuesAtTimesTest1Async();
        }

        [Fact]
        public Task HistoryReadInt32ValuesAtTimesTest2Async()
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

        [Fact]
        public Task HistoryStreamInt32ValuesAtTimesTest1Async()
        {
            return GetTests().HistoryStreamInt32ValuesAtTimesTest1Async();
        }

        [Fact]
        public Task HistoryStreamInt32ValuesAtTimesTest2Async()
        {
            return GetTests().HistoryStreamInt32ValuesAtTimesTest2Async();
        }
    }
}
