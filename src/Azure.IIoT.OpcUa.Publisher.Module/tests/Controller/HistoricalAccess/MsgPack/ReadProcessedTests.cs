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
    public sealed class ReadProcessedTests : IClassFixture<PublisherModuleFixture>, IDisposable
    {
        public ReadProcessedTests(HistoricalAccessServer server, PublisherModuleFixture module, ITestOutputHelper output)
        {
            _server = server;
            _client = module.CreateRestClientContainer(output, TestSerializerType.MsgPack);
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        private HistoryReadValuesProcessedTests<ConnectionModel> GetTests()
        {
            return new HistoryReadValuesProcessedTests<ConnectionModel>(_server,
                _client.Resolve<IHistoryServices<ConnectionModel>>,
                _server.GetConnection());
        }

        private readonly HistoricalAccessServer _server;
        private readonly IContainer _client;

        [Fact]
        public Task HistoryReadUInt64ProcessedValuesTest1Async()
        {
            return GetTests().HistoryReadUInt64ProcessedValuesTest1Async();
        }

        [Fact]
        public Task HistoryReadUInt64ProcessedValuesTest2Async()
        {
            return GetTests().HistoryReadUInt64ProcessedValuesTest2Async();
        }

        [SkippableFact]
        public Task HistoryReadUInt64ProcessedValuesTest3Async()
        {
            return GetTests().HistoryReadUInt64ProcessedValuesTest3Async();
        }

        [SkippableFact]
        public Task HistoryStreamUInt64ProcessedValuesTest1Async()
        {
            return GetTests().HistoryStreamUInt64ProcessedValuesTest1Async();
        }

        [SkippableFact]
        public Task HistoryStreamUInt64ProcessedValuesTest2Async()
        {
            return GetTests().HistoryStreamUInt64ProcessedValuesTest2Async();
        }

        [SkippableFact]
        public Task HistoryStreamUInt64ProcessedValuesTest3Async()
        {
            return GetTests().HistoryStreamUInt64ProcessedValuesTest3Async();
        }
    }
}
