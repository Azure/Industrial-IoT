// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Mqtt.HistoricalAccess
{
    using Azure.IIoT.OpcUa.Publisher.Module.Tests.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Testing.Tests;
    using Autofac;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;

    [Collection(ReadCollection.Name)]
    public class NodeServicesTests : TwinIntegrationTestBase, IClassFixture<PublisherModuleMqttv5Fixture>
    {
        public NodeServicesTests(HistoricalAccessServer server,
            PublisherModuleMqttv5Fixture module, ITestOutputHelper output) : base(output)
        {
            _server = server;
            _module = module;
        }

        private NodeHistoricalAccessTests<ConnectionModel> GetTests()
        {
            return new NodeHistoricalAccessTests<ConnectionModel>(
                _module.SdkContainer.Resolve<INodeServices<ConnectionModel>>,
                _server.GetConnection());
        }

        private readonly HistoricalAccessServer _server;
        private readonly PublisherModuleMqttv5Fixture _module;

        [Fact]
        public Task GetServerCapabilitiesTestAsync()
        {
            return GetTests().GetServerCapabilitiesTestAsync(Ct);
        }

        [Fact]
        public Task HistoryGetServerCapabilitiesTestAsync()
        {
            return GetTests().HistoryGetServerCapabilitiesTestAsync(Ct);
        }

        [Fact]
        public Task HistoryGetInt16NodeHistoryConfiguration()
        {
            return GetTests().HistoryGetInt16NodeHistoryConfigurationAsync(Ct);
        }

        [Fact]
        public Task HistoryGetInt64NodeHistoryConfigurationAsync()
        {
            return GetTests().HistoryGetInt64NodeHistoryConfigurationAsync(Ct);
        }

        [Fact]
        public Task HistoryGetNodeHistoryConfigurationFromBadNode()
        {
            return GetTests().HistoryGetNodeHistoryConfigurationFromBadNodeAsync(Ct);
        }
    }
}
