// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.History {
    using Microsoft.Azure.IIoT.OpcUa.Edge.Control;
    using Microsoft.Azure.IIoT.OpcUa.History.Clients;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Services;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Testing.Fixtures;
    using Microsoft.Azure.IIoT.OpcUa.Testing.Tests;
    using System.Net;
    using System.Threading.Tasks;
    using Xunit;

    [Collection(HistoryReadCollection.Name)]
    public class AddressSpaceReadValuesTests {

        public AddressSpaceReadValuesTests(HistoryServerFixture server) {
            _server = server;
        }

        private HistoryReadValuesTests<EndpointModel> GetTests() {
            var codec = new JsonVariantEncoder();
            return new HistoryReadValuesTests<EndpointModel>(
                () => new HistoricAccessAdapter<EndpointModel>(new AddressSpaceServices(_server.Client,
                    codec, _server.Logger), codec, _server.Logger),
                new EndpointModel {
                    Url = $"opc.tcp://{Dns.GetHostName()}:{_server.Port}/UA/SampleServer"
                });
        }

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

        private readonly HistoryServerFixture _server;
    }
}
