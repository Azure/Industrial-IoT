// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Sdk.HistoricalAccess
{
    using Autofac;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Module.Tests.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Testing.Tests;
    using System.Threading.Tasks;
    using Xunit;

    [Collection(ReadCollection.Name)]
    public class NodeServicesTests : IClassFixture<PublisherModuleFixture>
    {
        public NodeServicesTests(HistoricalAccessServer server, PublisherModuleFixture module)
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
        private readonly PublisherModuleFixture _module;

        [Fact]
        public Task GetServerCapabilitiesTestAsync()
        {
            return GetTests().GetServerCapabilitiesTestAsync();
        }

        [Fact]
        public Task HistoryGetServerCapabilitiesTestAsync()
        {
            return GetTests().HistoryGetServerCapabilitiesTestAsync();
        }

        [Fact]
        public Task HistoryGetInt16NodeHistoryConfiguration()
        {
            return GetTests().HistoryGetInt16NodeHistoryConfigurationAsync();
        }

        [Fact]
        public Task HistoryGetInt64NodeHistoryConfigurationAsync()
        {
            return GetTests().HistoryGetInt64NodeHistoryConfigurationAsync();
        }

        [Fact]
        public Task HistoryGetNodeHistoryConfigurationFromBadNode()
        {
            return GetTests().HistoryGetNodeHistoryConfigurationFromBadNodeAsync();
        }
    }
}
