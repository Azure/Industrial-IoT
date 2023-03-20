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
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;
    using System;

    [Collection(ReadCollection.Name)]
    public sealed class ReadAtTimesTests : IClassFixture<PublisherModuleFixture>, IDisposable
    {
        public ReadAtTimesTests(HistoricalAccessServer server, PublisherModuleFixture module, ITestOutputHelper output)
        {
            _server = server;
            _client = module.CreateRestClientContainer(output, TestSerializerType.NewtonsoftJson);
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        private HistoryReadValuesAtTimesTests<ConnectionModel> GetTests()
        {
            return new HistoryReadValuesAtTimesTests<ConnectionModel>(_server,
                () => _client.Resolve<IHistoryServices<ConnectionModel>>(),
                _server.GetConnection());
        }

        private readonly HistoricalAccessServer _server;
        private readonly IContainer _client;

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
