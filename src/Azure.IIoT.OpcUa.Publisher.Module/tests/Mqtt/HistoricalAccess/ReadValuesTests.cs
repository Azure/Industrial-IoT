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
    public class ReadValuesTests : TwinIntegrationTestBase, IClassFixture<PublisherModuleMqttv5Fixture>
    {
        public ReadValuesTests(HistoricalAccessServer server,
            PublisherModuleMqttv5Fixture module, ITestOutputHelper output) : base(output)
        {
            _server = server;
            _module = module;
        }

        private HistoryReadValuesTests<ConnectionModel> GetTests()
        {
            return new HistoryReadValuesTests<ConnectionModel>(_server,
                _module.SdkContainer.Resolve<IHistoryServices<ConnectionModel>>,
                _server.GetConnection());
        }

        private readonly HistoricalAccessServer _server;
        private readonly PublisherModuleMqttv5Fixture _module;

        [Fact]
        public Task HistoryReadInt64ValuesTest1Async()
        {
            return GetTests().HistoryReadInt64ValuesTest1Async(Ct);
        }

        [Fact]
        public Task HistoryReadInt64ValuesTest2Async()
        {
            return GetTests().HistoryReadInt64ValuesTest2Async(Ct);
        }

        [Fact]
        public Task HistoryReadInt64ValuesTest3Async()
        {
            return GetTests().HistoryReadInt64ValuesTest3Async(Ct);
        }

        [Fact]
        public Task HistoryReadInt64ValuesTest4Async()
        {
            return GetTests().HistoryReadInt64ValuesTest4Async(Ct);
        }

        [SkippableFact]
        public Task HistoryStreamInt64ValuesTest1Async()
        {
            Skip.If(true, "Not yet supported");
            return GetTests().HistoryStreamInt64ValuesTest1Async(Ct);
        }

        [SkippableFact]
        public Task HistoryStreamInt64ValuesTest2Async()
        {
            Skip.If(true, "Not yet supported");
            return GetTests().HistoryStreamInt64ValuesTest2Async(Ct);
        }

        [SkippableFact]
        public Task HistoryStreamInt64ValuesTest3Async()
        {
            Skip.If(true, "Not yet supported");
            return GetTests().HistoryStreamInt64ValuesTest3Async(Ct);
        }

        [SkippableFact]
        public Task HistoryStreamInt64ValuesTest4Async()
        {
            Skip.If(true, "Not yet supported");
            return GetTests().HistoryStreamInt64ValuesTest4Async(Ct);
        }
    }
}
