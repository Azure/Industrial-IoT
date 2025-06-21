// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Controller.Alarms.MsgPack
{
    using Autofac;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Module.Tests.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Testing.Tests;
    using System;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;

    public sealed class NodeServicesTests : IClassFixture<AlarmsServer>, IClassFixture<PublisherModuleFixture>, IDisposable
    {
        public NodeServicesTests(AlarmsServer server, PublisherModuleFixture module, ITestOutputHelper output)
        {
            _server = server;
            _client = module.CreateRestClientContainer(output, TestSerializerType.MsgPack);
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        private AlarmServerTests<ConnectionModel> GetTests()
        {
            return new AlarmServerTests<ConnectionModel>(
                _client.Resolve<INodeServices<ConnectionModel>>,
                _server.GetConnection());
        }

        private readonly AlarmsServer _server;
        private readonly IContainer _client;

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
