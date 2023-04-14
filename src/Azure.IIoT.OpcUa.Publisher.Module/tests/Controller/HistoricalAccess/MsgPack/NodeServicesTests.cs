// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Controller.HistoricalAccess.MsgPack
{
    using Azure.IIoT.OpcUa.Publisher.Module.Tests.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Testing.Tests;
    using Autofac;
    using System;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;

    [Collection(ReadCollection.Name)]
    public sealed class NodeServicesTests : IClassFixture<PublisherModuleFixture>, IDisposable
    {
        public NodeServicesTests(HistoricalAccessServer server, PublisherModuleFixture module, ITestOutputHelper output)
        {
            _server = server;
            _client = module.CreateRestClientContainer(output, TestSerializerType.MsgPack);
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        private NodeHistoricalAccessTests<ConnectionModel> GetTests()
        {
            return new NodeHistoricalAccessTests<ConnectionModel>(
                _client.Resolve<INodeServices<ConnectionModel>>,
                _server.GetConnection());
        }

        private readonly HistoricalAccessServer _server;
        private readonly IContainer _client;

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
