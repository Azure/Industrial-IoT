// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Services.HistoricalAccess.Tests
{
    using Azure.IIoT.OpcUa.Publisher.Services;
    using Azure.IIoT.OpcUa.Models;
    using Azure.IIoT.OpcUa.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Testing.Tests;
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
            return new HistoryReadValuesModifiedTests<ConnectionModel>(
                () => new HistoryServices<ConnectionModel>(
                    new NodeServices<ConnectionModel>(_server.Client,
                    _output.BuildLoggerFor<NodeServices<ConnectionModel>>())), _server.GetConnection());
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
