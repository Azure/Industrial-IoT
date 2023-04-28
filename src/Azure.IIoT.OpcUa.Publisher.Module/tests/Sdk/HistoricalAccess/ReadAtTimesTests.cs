// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Sdk.HistoricalAccess
{
    using Azure.IIoT.OpcUa.Publisher.Module.Tests.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Testing.Tests;
    using Autofac;
    using System.Threading.Tasks;
    using Xunit;

    [Collection(ReadCollection.Name)]
    public class ReadAtTimesTests : IClassFixture<PublisherModuleFixture>
    {
        public ReadAtTimesTests(HistoricalAccessServer server, PublisherModuleFixture module)
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
        private readonly PublisherModuleFixture _module;

        [Fact]
        public Task HistoryReadInt32ValuesAtTimesTest1Async()
        {
            return GetTests().HistoryReadInt32ValuesAtTimesTest1Async();
        }

        [Fact]
        public Task HistoryReadInt32ValuesAtTimesTest2Async()
        {
            return GetTests().HistoryReadInt32ValuesAtTimesTest2Async();
        }

        [Fact]
        public Task HistoryReadInt32ValuesAtTimesTest3Async()
        {
            return GetTests().HistoryReadInt32ValuesAtTimesTest3Async();
        }

        [Fact]
        public Task HistoryReadInt32ValuesAtTimesTest4Async()
        {
            return GetTests().HistoryReadInt32ValuesAtTimesTest4Async();
        }

        [SkippableFact]
        public Task HistoryStreamInt32ValuesAtTimesTest1Async()
        {
            Skip.If(true, "Not yet supported");
            return GetTests().HistoryStreamInt32ValuesAtTimesTest1Async();
        }

        [SkippableFact]
        public Task HistoryStreamInt32ValuesAtTimesTest2Async()
        {
            Skip.If(true, "Not yet supported");
            return GetTests().HistoryStreamInt32ValuesAtTimesTest2Async();
        }
    }
}
