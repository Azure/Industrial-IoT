// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Mqtt.Alarms
{
    using Azure.IIoT.OpcUa.Publisher.Module.Tests.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Testing.Tests;
    using Autofac;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;

    public sealed class NodeServicesTests : TwinIntegrationTestBase,
        IClassFixture<AlarmsServer>, IClassFixture<PublisherModuleMqttv5Fixture>
    {
        public NodeServicesTests(AlarmsServer server,
            PublisherModuleMqttv5Fixture module, ITestOutputHelper output) : base(output)
        {
            _server = server;
            _module = module;
        }

        private AlarmServerTests<ConnectionModel> GetTests()
        {
            return new AlarmServerTests<ConnectionModel>(
                _module.SdkContainer.Resolve<INodeServices<ConnectionModel>>,
                _server.GetConnection());
        }

        private readonly AlarmsServer _server;
        private readonly PublisherModuleMqttv5Fixture _module;

        [Fact]
        public Task BrowseAreaPathTestAsync()
        {
            return GetTests().BrowseAreaPathTestAsync(Ct);
        }

        [Fact]
        public Task BrowseMetalsSouthMotorTestAsync()
        {
            return GetTests().BrowseMetalsSouthMotorTestAsync(Ct);
        }

        [Fact]
        public Task BrowseColoursEastTankTestAsync()
        {
            return GetTests().BrowseColoursEastTankTestAsync(Ct);
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
