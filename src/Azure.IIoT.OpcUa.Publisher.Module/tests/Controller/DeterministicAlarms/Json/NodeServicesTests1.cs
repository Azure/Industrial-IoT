// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Controller.DeterministicAlarms.Json
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

    public sealed class NodeServicesTests1 : IClassFixture<DeterministicAlarmsServer1>, IClassFixture<PublisherModuleFixture>, IDisposable
    {
        public NodeServicesTests1(DeterministicAlarmsServer1 server, PublisherModuleFixture module, ITestOutputHelper output)
        {
            _server = server;
            _client = module.CreateRestClientContainer(output, TestSerializerType.Json);
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        private DeterministicAlarmsTests1<ConnectionModel> GetTests()
        {
            return new DeterministicAlarmsTests1<ConnectionModel>(
                _client.Resolve<INodeServices<ConnectionModel>>,
                _server.GetConnection());
        }

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
