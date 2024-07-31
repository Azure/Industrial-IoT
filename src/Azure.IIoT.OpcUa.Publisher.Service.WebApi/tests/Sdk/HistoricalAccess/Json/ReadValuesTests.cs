// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.WebApi.Tests.Sdk.HistoricalAccess.Json
{
    using Azure.IIoT.OpcUa.Publisher.Service.WebApi.Tests.Clients;
    using Azure.IIoT.OpcUa.Publisher.Service.Sdk;
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Testing.Tests;
    using Autofac;
    using System;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;

    [Collection(ReadCollection.Name)]
    public sealed class ReadValuesTests : IClassFixture<WebAppFixture>, IDisposable
    {
        public ReadValuesTests(WebAppFixture factory, HistoricalAccessServer server, ITestOutputHelper output)
        {
            _factory = factory;
            _server = server;
            _client = factory.CreateClientScope(output, TestSerializerType.NewtonsoftJson);
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        private HistoryReadValuesTests<string> GetTests()
        {
            var client = _factory.CreateClient(); // Call to create server
            var registry = _factory.Resolve<IEndpointManager<string>>();
            var endpointId = registry.RegisterEndpointAsync(_server.GetConnection().Endpoint).Result;
            return new HistoryReadValuesTests<string>(_server, () => // Create an adapter over the api
                new HistoryWebApiAdapter(_client.Resolve<IHistoryServiceApi>()), endpointId);
        }

        private readonly WebAppFixture _factory;
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
            Skip.If(true, "not implemented yet");
            return GetTests().HistoryStreamInt64ValuesTest1Async();
        }

        [SkippableFact]
        public Task HistoryStreamInt64ValuesTest2Async()
        {
            Skip.If(true, "not implemented yet");
            return GetTests().HistoryStreamInt64ValuesTest2Async();
        }

        [SkippableFact]
        public Task HistoryStreamInt64ValuesTest3Async()
        {
            Skip.If(true, "not implemented yet");
            return GetTests().HistoryStreamInt64ValuesTest3Async();
        }

        [SkippableFact]
        public Task HistoryStreamInt64ValuesTest4Async()
        {
            Skip.If(true, "not implemented yet");
            return GetTests().HistoryStreamInt64ValuesTest4Async();
        }
    }
}
