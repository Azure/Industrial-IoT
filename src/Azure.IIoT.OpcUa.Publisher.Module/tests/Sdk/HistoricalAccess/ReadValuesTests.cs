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
    public class ReadValuesTests : IClassFixture<PublisherModuleFixture>
    {
        public ReadValuesTests(HistoricalAccessServer server, PublisherModuleFixture module)
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
        private readonly PublisherModuleFixture _module;

        [Fact]
        public Task HistoryReadInt64ValuesTest1Async()
        {
            return GetTests().HistoryReadInt64ValuesTest1Async();
        }

        [Fact]
        public Task HistoryReadInt64ValuesTest2Async()
        {
            return GetTests().HistoryReadInt64ValuesTest2Async();
        }

        [Fact]
        public Task HistoryReadInt64ValuesTest3Async()
        {
            return GetTests().HistoryReadInt64ValuesTest3Async();
        }

        [Fact]
        public Task HistoryReadInt64ValuesTest4Async()
        {
            return GetTests().HistoryReadInt64ValuesTest4Async();
        }

        [SkippableFact]
        public Task HistoryStreamInt64ValuesTest1Async()
        {
            Skip.If(true, "Not yet supported");
            return GetTests().HistoryStreamInt64ValuesTest1Async();
        }

        [SkippableFact]
        public Task HistoryStreamInt64ValuesTest2Async()
        {
            Skip.If(true, "Not yet supported");
            return GetTests().HistoryStreamInt64ValuesTest2Async();
        }

        [SkippableFact]
        public Task HistoryStreamInt64ValuesTest3Async()
        {
            Skip.If(true, "Not yet supported");
            return GetTests().HistoryStreamInt64ValuesTest3Async();
        }

        [SkippableFact]
        public Task HistoryStreamInt64ValuesTest4Async()
        {
            Skip.If(true, "Not yet supported");
            return GetTests().HistoryStreamInt64ValuesTest4Async();
        }
    }
}
