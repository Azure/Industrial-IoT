// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Mqtt.Plc
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
        IClassFixture<PlcServer>, IClassFixture<PublisherModuleMqttv5Fixture>
    {
        public NodeServicesTests1(PlcServer server,
            PublisherModuleMqttv5Fixture module, ITestOutputHelper output) : base(output)
        {
            _server = server;
            _module = module;
        }

        private SimulatorNodesTests<ConnectionModel> GetTests()
        {
            return new SimulatorNodesTests<ConnectionModel>(_server,
                _module.SdkContainer.Resolve<INodeServices<ConnectionModel>>,
                _server.GetConnection());
        }

        private readonly PlcServer _server;
        private readonly PublisherModuleMqttv5Fixture _module;

        [Fact]
        public Task AlternatingBooleanTelemetryChangesWithPeriodAsync()
        {
            return GetTests().AlternatingBooleanTelemetryChangesWithPeriodAsync(Ct);
        }

        [Fact]
        public Task BadNodeHasAlternatingStatusCodeAsync()
        {
            return GetTests().BadNodeHasAlternatingStatusCodeAsync(Ct);
        }

        [Fact]
        public Task FastLimitNumberOfUpdatesStopsUpdatingAfterLimitAsync()
        {
            return GetTests().FastLimitNumberOfUpdatesStopsUpdatingAfterLimitAsync(Ct);
        }

        [Fact]
        public Task FastUIntScalar1TelemetryChangesWithPeriodAsync()
        {
            return GetTests().FastUIntScalar1TelemetryChangesWithPeriodAsync(Ct);
        }

        [Fact]
        public Task NegativeTrendDataNodeHasValueWithTrendAsync()
        {
            return GetTests().NegativeTrendDataNodeHasValueWithTrendAsync(Ct);
        }

        [Fact]
        public Task NegativeTrendDataTelemetryChangesWithPeriodAsync()
        {
            return GetTests().NegativeTrendDataTelemetryChangesWithPeriodAsync(Ct);
        }

        [Fact]
        public Task PositiveTrendDataNodeHasValueWithTrendAsync()
        {
            return GetTests().PositiveTrendDataNodeHasValueWithTrendAsync(Ct);
        }

        [Fact]
        public Task PositiveTrendDataTelemetryChangesWithPeriodAsync()
        {
            return GetTests().PositiveTrendDataTelemetryChangesWithPeriodAsync(Ct);
        }

        [Fact]
        public Task RandomSignedInt32TelemetryChangesWithPeriodAsync()
        {
            return GetTests().RandomSignedInt32TelemetryChangesWithPeriodAsync(Ct);
        }

        [Fact]
        public Task RandomUnsignedInt32TelemetryChangesWithPeriodAsync()
        {
            return GetTests().RandomUnsignedInt32TelemetryChangesWithPeriodAsync(Ct);
        }

        [Fact]
        public Task SlowLimitNumberOfUpdatesStopsUpdatingAfterLimitAsync()
        {
            return GetTests().SlowLimitNumberOfUpdatesStopsUpdatingAfterLimitAsync(Ct);
        }

        [Fact]
        public Task SlowUIntScalar1TelemetryChangesWithPeriodAsync()
        {
            return GetTests().SlowUIntScalar1TelemetryChangesWithPeriodAsync(Ct);
        }

        [Fact]
        public Task TelemetryContainsOutlierInDipDataAsync()
        {
            return GetTests().TelemetryContainsOutlierInDipDataAsync(Ct);
        }

        [Fact]
        public Task TelemetryContainsOutlierInSpikeDataAsync()
        {
            return GetTests().TelemetryContainsOutlierInSpikeDataAsync(Ct);
        }

        [Fact]
        public Task TelemetryFastNodeTestAsync()
        {
            return GetTests().TelemetryFastNodeTestAsync(Ct);
        }

        [Fact]
        public Task TelemetryStepUpTestAsync()
        {
            return GetTests().TelemetryStepUpTestAsync(Ct);
        }
    }
}
