// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Controller.HistoricalAccess.MsgPack
{
    using Azure.IIoT.OpcUa.Publisher.Module.Tests.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Testing.Tests;
    using Autofac;
    using System;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;

    [Collection(ReadCollection.Name)]
    public sealed class ReadValuesTests : IClassFixture<PublisherModuleFixture>, IDisposable
    {
        public ReadValuesTests(HistoricalAccessServer server, PublisherModuleFixture module, ITestOutputHelper output)
        {
            _server = server;
            _client = module.CreateRestClientContainer(output, TestSerializerType.MsgPack);
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        private HistoryReadValuesTests<ConnectionModel> GetTests()
        {
            return new HistoryReadValuesTests<ConnectionModel>(_server,
                _client.Resolve<IHistoryServices<ConnectionModel>>,
                _server.GetConnection());
        }

        private readonly HistoricalAccessServer _server;
        private readonly IContainer _client;

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
            return GetTests().HistoryStreamInt64ValuesTest1Async();
        }

        [SkippableFact]
        public Task HistoryStreamInt64ValuesTest2Async()
        {
            return GetTests().HistoryStreamInt64ValuesTest2Async();
        }

        [SkippableFact]
        public Task HistoryStreamInt64ValuesTest3Async()
        {
            return GetTests().HistoryStreamInt64ValuesTest3Async();
        }

        [SkippableFact]
        public Task HistoryStreamInt64ValuesTest4Async()
        {
            return GetTests().HistoryStreamInt64ValuesTest4Async();
        }
    }
}
