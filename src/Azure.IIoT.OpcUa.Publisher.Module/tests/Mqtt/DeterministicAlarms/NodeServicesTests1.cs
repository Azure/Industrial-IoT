// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Mqtt.DeterministicAlarms
{
    using Azure.IIoT.OpcUa.Publisher.Module.Tests.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Testing.Tests;
    using Autofac;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;

    public sealed class NodeServicesTests1 : TwinIntegrationTestBase,
        IClassFixture<DeterministicAlarmsServer1>, IClassFixture<PublisherModuleMqttv311Fixture>
    {
        public NodeServicesTests1(DeterministicAlarmsServer1 server,
            PublisherModuleMqttv311Fixture module, ITestOutputHelper output) : base(output)
        {
            _server = server;
            _module = module;
        }

        private DeterministicAlarmsTests1<ConnectionModel> GetTests()
        {
            return new DeterministicAlarmsTests1<ConnectionModel>(
                _module.SdkContainer.Resolve<INodeServices<ConnectionModel>>,
                _server.GetConnection());
        }

        private readonly DeterministicAlarmsServer1 _server;
        private readonly PublisherModuleMqttv311Fixture _module;

        [Fact]
        public Task BrowseAreaPathVendingMachine1TemperatureHighTestAsync()
        {
            return GetTests().BrowseAreaPathVendingMachine1TemperatureHighTestAsync(Ct);
        }

        [Fact]
        public Task BrowseAreaPathVendingMachine2LightOffTestAsync()
        {
            return GetTests().BrowseAreaPathVendingMachine2LightOffTestAsync(Ct);
        }

        [Fact]
        public Task BrowseAreaPathVendingMachine1DoorOpenTestAsync()
        {
            return GetTests().BrowseAreaPathVendingMachine1DoorOpenTestAsync(Ct);
        }

        [Fact]
        public Task BrowseAreaPathVendingMachine2DoorOpenTestAsync()
        {
            return GetTests().BrowseAreaPathVendingMachine2DoorOpenTestAsync(Ct);
        }
    }
}
