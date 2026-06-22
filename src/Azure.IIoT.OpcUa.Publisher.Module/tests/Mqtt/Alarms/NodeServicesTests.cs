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

        [Fact(Skip = "Temporarily disabled to isolate newer-Windows (2022/2025) test-host native crash in alarms ConditionState")]
        public Task BrowseAreaPathTestAsync()
        {
            return GetTests().BrowseAreaPathTestAsync(Ct);
        }

        [Fact(Skip = "Temporarily disabled to isolate newer-Windows (2022/2025) test-host native crash in alarms ConditionState")]
        public Task BrowseMetalsSouthMotorTestAsync()
        {
            return GetTests().BrowseMetalsSouthMotorTestAsync(Ct);
        }

        [Fact(Skip = "Temporarily disabled to isolate newer-Windows (2022/2025) test-host native crash in alarms ConditionState")]
        public Task BrowseColoursEastTankTestAsync()
        {
            return GetTests().BrowseColoursEastTankTestAsync(Ct);
        }

        [Fact(Skip = "Temporarily disabled to isolate newer-Windows (2022/2025) test-host native crash in alarms ConditionState")]
        public Task CompileAlarmQueryTest1Async()
        {
            return GetTests().CompileAlarmQueryTest1Async();
        }

        [Fact(Skip = "Temporarily disabled to isolate newer-Windows (2022/2025) test-host native crash in alarms ConditionState")]
        public Task CompileAlarmQueryTest2Async()
        {
            return GetTests().CompileAlarmQueryTest2Async();
        }

        [Fact(Skip = "Temporarily disabled to isolate newer-Windows (2022/2025) test-host native crash in alarms ConditionState")]
        public Task CompileSimpleBaseEventQueryTestAsync()
        {
            return GetTests().CompileSimpleBaseEventQueryTestAsync();
        }

        [Fact(Skip = "Temporarily disabled to isolate newer-Windows (2022/2025) test-host native crash in alarms ConditionState")]
        public Task CompileSimpleTripAlarmQueryTestAsync()
        {
            return GetTests().CompileSimpleTripAlarmQueryTestAsync();
        }
    }
}
