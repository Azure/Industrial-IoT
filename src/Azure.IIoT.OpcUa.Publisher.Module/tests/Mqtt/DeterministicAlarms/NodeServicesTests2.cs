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

    public sealed class NodeServicesTests2 : TwinIntegrationTestBase,
        IClassFixture<DeterministicAlarmsServer2>, IClassFixture<PublisherModuleMqttv5Fixture>
    {
        public NodeServicesTests2(DeterministicAlarmsServer2 server,
            PublisherModuleMqttv5Fixture module, ITestOutputHelper output) : base(output)
        {
            _server = server;
            _module = module;
        }

        private DeterministicAlarmsTests2<ConnectionModel> GetTests()
        {
            return new DeterministicAlarmsTests2<ConnectionModel>(
                _module.SdkContainer.Resolve<INodeServices<ConnectionModel>>,
                _server.GetConnection());
        }

        private readonly DeterministicAlarmsServer2 _server;
        private readonly PublisherModuleMqttv5Fixture _module;

        [Fact]
        public Task BrowseAreaPathVendingMachine1DoorOpenTestAsync()
        {
            return GetTests().BrowseAreaPathVendingMachine1DoorOpenTestAsync(Ct);
        }
    }
}
