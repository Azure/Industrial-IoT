// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Controller.HistoricalAccess.Json
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
    public sealed class UpdateValuesTests : IClassFixture<PublisherModuleFixture>, IDisposable
    {
        public UpdateValuesTests(HistoricalAccessServer server, PublisherModuleFixture module, ITestOutputHelper output)
        {
            _server = server;
            _client = module.CreateRestClientContainer(output, TestSerializerType.Json);
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        private HistoryUpdateValuesTests<ConnectionModel> GetTests()
        {
            return new HistoryUpdateValuesTests<ConnectionModel>(
                _client.Resolve<IHistoryServices<ConnectionModel>>,
                _server.GetConnection());
        }

        private readonly HistoricalAccessServer _server;
        private readonly IContainer _client;

        [Fact]
        public Task HistoryUpsertUInt32ValuesTest1Async()
        {
            return GetTests().HistoryUpsertUInt32ValuesTest1Async();
        }

        [Fact]
        public Task HistoryUpsertUInt32ValuesTest2Async()
        {
            return GetTests().HistoryUpsertUInt32ValuesTest2Async();
        }

        [Fact]
        public Task HistoryInsertUInt32ValuesTest1Async()
        {
            return GetTests().HistoryInsertUInt32ValuesTest1Async();
        }

        [Fact]
        public Task HistoryInsertUInt32ValuesTest2Async()
        {
            return GetTests().HistoryInsertUInt32ValuesTest2Async();
        }

        [Fact]
        public Task HistoryReplaceUInt32ValuesTest1Async()
        {
            return GetTests().HistoryReplaceUInt32ValuesTest1Async();
        }

        [Fact]
        public Task HistoryReplaceUInt32ValuesTest2Async()
        {
            return GetTests().HistoryReplaceUInt32ValuesTest2Async();
        }

        [Fact]
        public Task HistoryInsertDeleteUInt32ValuesTest1Async()
        {
            return GetTests().HistoryInsertDeleteUInt32ValuesTest1Async();
        }

        [Fact]
        public Task HistoryInsertDeleteUInt32ValuesTest2Async()
        {
            return GetTests().HistoryInsertDeleteUInt32ValuesTest2Async();
        }

        [Fact]
        public Task HistoryInsertDeleteUInt32ValuesTest3Async()
        {
            return GetTests().HistoryInsertDeleteUInt32ValuesTest3Async();
        }

        [Fact]
        public Task HistoryInsertDeleteUInt32ValuesTest4Async()
        {
            return GetTests().HistoryInsertDeleteUInt32ValuesTest4Async();
        }

        [Fact]
        public Task HistoryDeleteUInt32ValuesTest1Async()
        {
            return GetTests().HistoryDeleteUInt32ValuesTest1Async();
        }
    }
}
