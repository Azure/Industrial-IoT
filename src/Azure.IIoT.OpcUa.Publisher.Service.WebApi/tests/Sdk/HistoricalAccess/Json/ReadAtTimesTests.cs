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
    public sealed class ReadAtTimesTests : IClassFixture<WebAppFixture>, IDisposable
    {
        public ReadAtTimesTests(WebAppFixture factory, HistoricalAccessServer server, ITestOutputHelper output)
        {
            _factory = factory;
            _server = server;
            _client = factory.CreateClientScope(output, TestSerializerType.NewtonsoftJson);
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        private HistoryReadValuesAtTimesTests<string> GetTests()
        {
            var client = _factory.CreateClient(); // Call to create server
            var registry = _factory.Resolve<IEndpointManager>();
            var endpointId = registry.RegisterEndpointAsync(_server.GetConnection().Endpoint).Result;
            return new HistoryReadValuesAtTimesTests<string>(_server, () => // Create an adapter over the api
                new HistoryWebApiAdapter(_client.Resolve<IHistoryServiceApi>()), endpointId);
        }

        private readonly WebAppFixture _factory;
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
            Skip.If(true, "not implemented yet");
            return GetTests().HistoryStreamInt32ValuesAtTimesTest1Async();
        }

        [SkippableFact]
        public Task HistoryStreamInt32ValuesAtTimesTest2Async()
        {
            Skip.If(true, "not implemented yet");
            return GetTests().HistoryStreamInt32ValuesAtTimesTest2Async();
        }
    }
}
