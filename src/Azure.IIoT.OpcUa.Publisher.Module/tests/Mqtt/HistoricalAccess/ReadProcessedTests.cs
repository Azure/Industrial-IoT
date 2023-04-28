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
    public class ReadProcessedTests : TwinIntegrationTestBase, IClassFixture<PublisherModuleMqttv5Fixture>
    {
        public ReadProcessedTests(HistoricalAccessServer server,
            PublisherModuleMqttv5Fixture module, ITestOutputHelper output) : base(output)
        {
            _server = server;
            _module = module;
        }

        private HistoryReadValuesProcessedTests<ConnectionModel> GetTests()
        {
            return new HistoryReadValuesProcessedTests<ConnectionModel>(_server,
                _module.SdkContainer.Resolve<IHistoryServices<ConnectionModel>>,
                _server.GetConnection());
        }

        private readonly HistoricalAccessServer _server;
        private readonly PublisherModuleMqttv5Fixture _module;

        [Fact]
        public Task HistoryReadUInt64ProcessedValuesTest1Async()
        {
            return GetTests().HistoryReadUInt64ProcessedValuesTest1Async(Ct);
        }

        [Fact]
        public Task HistoryReadUInt64ProcessedValuesTest2Async()
        {
            return GetTests().HistoryReadUInt64ProcessedValuesTest2Async(Ct);
        }

        [SkippableFact]
        public Task HistoryReadUInt64ProcessedValuesTest3Async()
        {
            Skip.If(true, "Not yet supported");
            return GetTests().HistoryReadUInt64ProcessedValuesTest3Async(Ct);
        }

        [SkippableFact]
        public Task HistoryStreamUInt64ProcessedValuesTest1Async()
        {
            Skip.If(true, "Not yet supported");
            return GetTests().HistoryStreamUInt64ProcessedValuesTest1Async(Ct);
        }

        [SkippableFact]
        public Task HistoryStreamUInt64ProcessedValuesTest2Async()
        {
            Skip.If(true, "Not yet supported");
            return GetTests().HistoryStreamUInt64ProcessedValuesTest2Async(Ct);
        }

        [SkippableFact]
        public Task HistoryStreamUInt64ProcessedValuesTest3Async()
        {
            Skip.If(true, "Not yet supported");
            return GetTests().HistoryStreamUInt64ProcessedValuesTest3Async(Ct);
        }
    }
}
