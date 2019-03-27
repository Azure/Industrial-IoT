// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.History.v2.Controllers {
    using Microsoft.Azure.IIoT.Http.Default;
    using Microsoft.Azure.IIoT.OpcUa.Api.History;
    using Microsoft.Azure.IIoT.OpcUa.Api.History.Clients;
    using Microsoft.Azure.IIoT.OpcUa.History.Clients;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Services;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Testing.Fixtures;
    using Microsoft.Azure.IIoT.OpcUa.Testing.Tests;
    using Serilog;
    using System.Net;
    using System.Threading.Tasks;
    using Xunit;

    [Collection(ReadCollection.Name)]
    public class ReadControllerValuesTests : IClassFixture<WebAppFixture> {

        public ReadControllerValuesTests(WebAppFixture factory, HistoryServerFixture server) {
            _factory = factory;
            _server = server;
        }

        private HistoryReadValuesTests<string> GetTests() {
            var client = _factory.CreateClient(); // Call to create server
            var module = _factory.Resolve<ITestModule>();
            module.Endpoint = Endpoint;
            var log = _factory.Resolve<ILogger>();
            return new HistoryReadValuesTests<string>(() => // Create an adapter over the api
                new HistoricAccessAdapter<string>(
                    new HistoryRawAdapter(
                        new HistoryServiceClient(
                           new HttpClient(_factory, log), new TestConfig(client.BaseAddress), log),
                                log), new JsonVariantEncoder(), log), "fakeid");
        }

        public EndpointModel Endpoint => new EndpointModel {
            Url = $"opc.tcp://{Dns.GetHostName()}:{_server.Port}/UA/SampleServer"
        };

        private readonly WebAppFixture _factory;
        private readonly HistoryServerFixture _server;

        [Fact]
        public async Task HistoryReadInt64ValuesTest1() {
            await GetTests().HistoryReadInt64ValuesTest1();
        }

        [Fact]
        public async Task HistoryReadInt64ValuesTest2() {
            await GetTests().HistoryReadInt64ValuesTest2();
        }

        [Fact]
        public async Task HistoryReadInt64ValuesTest3() {
            await GetTests().HistoryReadInt64ValuesTest3();
        }

        [Fact]
        public async Task HistoryReadInt64ValuesTest4() {
            await GetTests().HistoryReadInt64ValuesTest4();
        }
    }
}
