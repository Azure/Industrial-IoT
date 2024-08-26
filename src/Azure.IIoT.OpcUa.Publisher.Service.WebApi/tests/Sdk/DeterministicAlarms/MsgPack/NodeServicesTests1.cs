// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.WebApi.Tests.Sdk.DeterministicAlarms.MsgPack
{
    using Azure.IIoT.OpcUa.Publisher.Service.WebApi.Tests;
    using Azure.IIoT.OpcUa.Publisher.Service.WebApi.Tests.Clients;
    using Azure.IIoT.OpcUa.Publisher.Service;
    using Azure.IIoT.OpcUa.Publisher.Service.Sdk;
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Testing.Tests;
    using Autofac;
    using System;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;

    public sealed class NodeServicesTests1 : IClassFixture<WebAppFixture>, IClassFixture<DeterministicAlarmsServer1>, IDisposable
    {
        public NodeServicesTests1(WebAppFixture factory, DeterministicAlarmsServer1 server, ITestOutputHelper output)
        {
            _factory = factory;
            _server = server;
            _client = factory.CreateClientScope(output, TestSerializerType.MsgPack);
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        private DeterministicAlarmsTests1<string> GetTests()
        {
            var client = _factory.CreateClient(); // Call to create server
            var registry = _factory.Resolve<IEndpointManager<string>>();
            var endpointId = registry.RegisterEndpointAsync(_server.GetConnection().Endpoint).Result;
            return new DeterministicAlarmsTests1<string>(() => // Create an adapter over the api
                new TwinWebApiAdapter(_client.Resolve<ITwinServiceApi>()), endpointId);
        }

        private readonly WebAppFixture _factory;
        private readonly DeterministicAlarmsServer1 _server;
        private readonly IContainer _client;

        [Fact]
        public Task BrowseAreaPathVendingMachine1TemperatureHighTestAsync()
        {
            return GetTests().BrowseAreaPathVendingMachine1TemperatureHighTestAsync();
        }

        [Fact]
        public Task BrowseAreaPathVendingMachine2LightOffTestAsync()
        {
            return GetTests().BrowseAreaPathVendingMachine2LightOffTestAsync();
        }

        [Fact]
        public Task BrowseAreaPathVendingMachine1DoorOpenTestAsync()
        {
            return GetTests().BrowseAreaPathVendingMachine1DoorOpenTestAsync();
        }

        [Fact]
        public Task BrowseAreaPathVendingMachine2DoorOpenTestAsync()
        {
            return GetTests().BrowseAreaPathVendingMachine2DoorOpenTestAsync();
        }
    }
}
