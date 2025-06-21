// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Controller.HistoricalAccess.MsgPack
{
    using Autofac;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Module.Tests.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Testing.Tests;
    using System;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;

    [Collection(ReadCollection.Name)]
    public sealed class ReadModifiedTests : IClassFixture<PublisherModuleFixture>, IDisposable
    {
        public ReadModifiedTests(HistoricalAccessServer server, PublisherModuleFixture module, ITestOutputHelper output)
        {
            _server = server;
            _client = module.CreateRestClientContainer(output, TestSerializerType.MsgPack);
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        private HistoryReadValuesModifiedTests<ConnectionModel> GetTests()
        {
            return new HistoryReadValuesModifiedTests<ConnectionModel>(_server,
                _client.Resolve<IHistoryServices<ConnectionModel>>,
                _server.GetConnection());
        }

        private readonly HistoricalAccessServer _server;
        private readonly IContainer _client;

        [Fact]
        public Task HistoryReadInt16ValuesModifiedTestAsync()
        {
            return GetTests().HistoryReadInt16ValuesModifiedTestAsync();
        }

        [SkippableFact]
        public Task HistoryStreamInt16ValuesModifiedTestAsync()
        {
            return GetTests().HistoryStreamInt16ValuesModifiedTestAsync();
        }
    }
}
