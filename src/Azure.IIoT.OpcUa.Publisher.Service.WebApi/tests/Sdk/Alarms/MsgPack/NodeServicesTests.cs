// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.WebApi.Tests.Sdk.Alarms.MsgPack
{
    using Azure.IIoT.OpcUa.Publisher.Service.WebApi.Tests.Clients;
    using Azure.IIoT.OpcUa.Publisher.Service.Sdk;
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Testing.Tests;
    using Autofac;
    using System;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;

    public sealed class NodeServicesTests : IClassFixture<WebAppFixture>, IClassFixture<AlarmsServer>, IDisposable
    {
        public NodeServicesTests(WebAppFixture factory, AlarmsServer server, ITestOutputHelper output)
        {
            _factory = factory;
            _server = server;
            _client = factory.CreateClientScope(output, TestSerializerType.MsgPack);
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        private AlarmServerTests<string> GetTests()
        {
            var client = _factory.CreateClient(); // Call to create server
            var registry = _factory.Resolve<IEndpointManager<string>>();
            var endpointId = registry.RegisterEndpointAsync(_server.GetConnection().Endpoint).Result;
            return new AlarmServerTests<string>(() => // Create an adapter over the api
                new TwinWebApiAdapter(_client.Resolve<ITwinServiceApi>()), endpointId);
        }

        private readonly WebAppFixture _factory;
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

        [SkippableFact]
        public Task CompileAlarmQueryTest1Async()
        {
            Skip.IfNot(false, "Not supported");
            return GetTests().CompileAlarmQueryTest1Async();
        }

        [SkippableFact]
        public Task CompileAlarmQueryTest2Async()
        {
            Skip.IfNot(false, "Not supported");
            return GetTests().CompileAlarmQueryTest2Async();
        }

        [SkippableFact]
        public Task CompileSimpleBaseEventQueryTestAsync()
        {
            Skip.IfNot(false, "Not supported");
            return GetTests().CompileSimpleBaseEventQueryTestAsync();
        }

        [SkippableFact]
        public Task CompileSimpleTripAlarmQueryTestAsync()
        {
            Skip.IfNot(false, "Not supported");
            return GetTests().CompileSimpleTripAlarmQueryTestAsync();
        }
    }
}
