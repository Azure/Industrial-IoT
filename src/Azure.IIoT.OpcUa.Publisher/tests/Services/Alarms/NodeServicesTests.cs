// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Services.Alarms
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Services;
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Testing.Tests;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;

    public sealed class NodeServicesTests : IClassFixture<AlarmsServer>
    {
        public NodeServicesTests(AlarmsServer server, ITestOutputHelper output)
        {
            _server = server;
            _output = output;
        }

        private AlarmServerTests<ConnectionModel> GetTests()
        {
            return new AlarmServerTests<ConnectionModel>(
                () => new NodeServices<ConnectionModel>(_server.Client, _server.Parser,
                    _output.BuildLoggerFor<NodeServices<ConnectionModel>>(Logging.Level)),
                _server.GetConnection());
        }

        private readonly ITestOutputHelper _output;
        private readonly AlarmsServer _server;

        [Fact]
        public Task BrowseAreaPathTestAsync()
        {
            return GetTests().BrowseAreaPathTestAsync();
        }

        [Fact]
        public Task BrowseMetalsSouthMotorTestAsync()
        {
            return GetTests().BrowseMetalsSouthMotorTestAsync();
        }

        [Fact]
        public Task BrowseColoursEastTankTestAsync()
        {
            return GetTests().BrowseColoursEastTankTestAsync();
        }

        [Fact]
        public Task CompileAlarmQueryTest1Async()
        {
            return GetTests().CompileAlarmQueryTest1Async();
        }

        [Fact]
        public Task CompileAlarmQueryTest2Async()
        {
            return GetTests().CompileAlarmQueryTest2Async();
        }

        [Fact]
        public Task CompileSimpleBaseEventQueryTestAsync()
        {
            return GetTests().CompileSimpleBaseEventQueryTestAsync();
        }

        [Fact]
        public Task CompileSimpleTripAlarmQueryTestAsync()
        {
            return GetTests().CompileSimpleTripAlarmQueryTestAsync();
        }
    }
}
