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
    public class ReadAtTimesTests : TwinIntegrationTestBase, IClassFixture<PublisherModuleMqttv5Fixture>
    {
        public ReadAtTimesTests(HistoricalAccessServer server,
            PublisherModuleMqttv5Fixture module, ITestOutputHelper output) : base(output)
        {
            _server = server;
            _module = module;
        }

        private HistoryReadValuesAtTimesTests<ConnectionModel> GetTests()
        {
            return new HistoryReadValuesAtTimesTests<ConnectionModel>(_server,
                _module.SdkContainer.Resolve<IHistoryServices<ConnectionModel>>,
                _server.GetConnection());
        }

        private readonly HistoricalAccessServer _server;
        private readonly PublisherModuleMqttv5Fixture _module;

        [Fact]
        public Task HistoryReadInt32ValuesAtTimesTest1Async()
        {
            return GetTests().HistoryReadInt32ValuesAtTimesTest1Async(Ct);
        }

        [Fact]
        public Task HistoryReadInt32ValuesAtTimesTest2Async()
        {
            return GetTests().HistoryReadInt32ValuesAtTimesTest2Async(Ct);
        }

        [Fact]
        public Task HistoryReadInt32ValuesAtTimesTest3Async()
        {
            return GetTests().HistoryReadInt32ValuesAtTimesTest3Async(Ct);
        }

        [Fact]
        public Task HistoryReadInt32ValuesAtTimesTest4Async()
        {
            return GetTests().HistoryReadInt32ValuesAtTimesTest4Async(Ct);
        }

        [SkippableFact]
        public Task HistoryStreamInt32ValuesAtTimesTest1Async()
        {
            Skip.If(true, "Not yet supported");
            return GetTests().HistoryStreamInt32ValuesAtTimesTest1Async(Ct);
        }

        [SkippableFact]
        public Task HistoryStreamInt32ValuesAtTimesTest2Async()
        {
            Skip.If(true, "Not yet supported");
            return GetTests().HistoryStreamInt32ValuesAtTimesTest2Async(Ct);
        }
    }
}
