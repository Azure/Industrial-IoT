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
    public class ReadModifiedTests
    {
        public ReadModifiedTests(HistoricalAccessServer server, ITestOutputHelper output)
        {
            _server = server;
            _output = output;
        }

        private HistoryReadValuesModifiedTests<ConnectionModel> GetTests()
        {
            return new HistoryReadValuesModifiedTests<ConnectionModel>(_server,
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
        public Task HistoryReadInt16ValuesModifiedTestAsync()
        {
            return GetTests().HistoryReadInt16ValuesModifiedTestAsync();
        }

        [Fact]
        public Task HistoryStreamInt16ValuesModifiedTestAsync()
        {
            return GetTests().HistoryStreamInt16ValuesModifiedTestAsync();
        }
    }
}
