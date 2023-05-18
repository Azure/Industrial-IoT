// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Services.DeterministicAlarms
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Services;
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Testing.Tests;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;

    public sealed class NodeServicesTests2 : IClassFixture<DeterministicAlarmsServer2>
    {
        public NodeServicesTests2(DeterministicAlarmsServer2 server, ITestOutputHelper output)
        {
            _output = output;
            _server = server;
        }

        private DeterministicAlarmsTests2<ConnectionModel> GetTests()
        {
            return new DeterministicAlarmsTests2<ConnectionModel>(
                () => new NodeServices<ConnectionModel>(_server.Client, _server.Parser,
                    _output.BuildLoggerFor<NodeServices<ConnectionModel>>(Logging.Level)),
                _server.GetConnection(), _server);
        }

        private readonly ITestOutputHelper _output;
        private readonly DeterministicAlarmsServer2 _server;

        [Fact]
        public Task BrowseAreaPathVendingMachine1DoorOpenTestAsync()
        {
            return GetTests().BrowseAreaPathVendingMachine1DoorOpenTestAsync();
        }
    }
}
