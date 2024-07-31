// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.WebApi.Tests.Sdk.Plc.MsgPack
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

    public sealed class NodeServicesTests1 : IClassFixture<WebAppFixture>, IClassFixture<PlcServer>, IDisposable
    {
        public NodeServicesTests1(WebAppFixture factory, PlcServer server, ITestOutputHelper output)
        {
            _factory = factory;
            _server = server;
            _client = factory.CreateClientScope(output, TestSerializerType.MsgPack);
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        private SimulatorNodesTests<string> GetTests()
        {
            var client = _factory.CreateClient(); // Call to create server
            var registry = _factory.Resolve<IEndpointManager<string>>();
            var endpointId = registry.RegisterEndpointAsync(_server.GetConnection().Endpoint).Result;
            return new SimulatorNodesTests<string>(_server, () => // Create an adapter over the api
                new TwinWebApiAdapter(_client.Resolve<ITwinServiceApi>()), endpointId);
        }

        private readonly WebAppFixture _factory;
        private readonly PlcServer _server;
        private readonly IContainer _client;

        [Fact]
        public Task AlternatingBooleanTelemetryChangesWithPeriodAsync()
        {
            return GetTests().AlternatingBooleanTelemetryChangesWithPeriodAsync();
        }

        [Fact]
        public Task BadNodeHasAlternatingStatusCodeAsync()
        {
            return GetTests().BadNodeHasAlternatingStatusCodeAsync();
        }

        [Fact]
        public Task FastLimitNumberOfUpdatesStopsUpdatingAfterLimitAsync()
        {
            return GetTests().FastLimitNumberOfUpdatesStopsUpdatingAfterLimitAsync();
        }

        [Fact]
        public Task FastUIntScalar1TelemetryChangesWithPeriodAsync()
        {
            return GetTests().FastUIntScalar1TelemetryChangesWithPeriodAsync();
        }

        [Fact]
        public Task NegativeTrendDataNodeHasValueWithTrendAsync()
        {
            return GetTests().NegativeTrendDataNodeHasValueWithTrendAsync();
        }

        [Fact]
        public Task NegativeTrendDataTelemetryChangesWithPeriodAsync()
        {
            return GetTests().NegativeTrendDataTelemetryChangesWithPeriodAsync();
        }

        [Fact]
        public Task PositiveTrendDataNodeHasValueWithTrendAsync()
        {
            return GetTests().PositiveTrendDataNodeHasValueWithTrendAsync();
        }

        [Fact]
        public Task PositiveTrendDataTelemetryChangesWithPeriodAsync()
        {
            return GetTests().PositiveTrendDataTelemetryChangesWithPeriodAsync();
        }

        [Fact]
        public Task RandomSignedInt32TelemetryChangesWithPeriodAsync()
        {
            return GetTests().RandomSignedInt32TelemetryChangesWithPeriodAsync();
        }

        [Fact]
        public Task RandomUnsignedInt32TelemetryChangesWithPeriodAsync()
        {
            return GetTests().RandomUnsignedInt32TelemetryChangesWithPeriodAsync();
        }

        [Fact]
        public Task SlowLimitNumberOfUpdatesStopsUpdatingAfterLimitAsync()
        {
            return GetTests().SlowLimitNumberOfUpdatesStopsUpdatingAfterLimitAsync();
        }

        [Fact]
        public Task SlowUIntScalar1TelemetryChangesWithPeriodAsync()
        {
            return GetTests().SlowUIntScalar1TelemetryChangesWithPeriodAsync();
        }

        [Fact]
        public Task TelemetryContainsOutlierInDipDataAsync()
        {
            return GetTests().TelemetryContainsOutlierInDipDataAsync();
        }

        [Fact]
        public Task TelemetryContainsOutlierInSpikeDataAsync()
        {
            return GetTests().TelemetryContainsOutlierInSpikeDataAsync();
        }

        [Fact]
        public Task TelemetryFastNodeTestAsync()
        {
            return GetTests().TelemetryFastNodeTestAsync();
        }

        [Fact]
        public Task TelemetryStepUpTestAsync()
        {
            return GetTests().TelemetryStepUpTestAsync();
        }
    }
}
