// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Sdk.Plc
{
    using Azure.IIoT.OpcUa.Publisher.Module.Tests.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Testing.Tests;
    using Autofac;
    using System.Threading.Tasks;
    using Xunit;

    public sealed class NodeServicesTests1 : IClassFixture<PlcServer>, IClassFixture<PublisherModuleFixture>
    {
        public NodeServicesTests1(PlcServer server, PublisherModuleFixture module)
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
        private readonly PublisherModuleFixture _module;

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
