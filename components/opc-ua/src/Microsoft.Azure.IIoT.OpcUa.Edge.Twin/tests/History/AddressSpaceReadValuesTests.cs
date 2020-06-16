// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.History {
    using Microsoft.Azure.IIoT.OpcUa.Edge.Control.Services;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.History.Clients;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Services;
    using Microsoft.Azure.IIoT.OpcUa.Testing.Fixtures;
    using Microsoft.Azure.IIoT.OpcUa.Testing.Tests;
    using Microsoft.Azure.IIoT.Utils;
    using Opc.Ua;
    using System.Threading.Tasks;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using Xunit;


    [Collection(HistoryReadCollection.Name)]
    public class AddressSpaceReadValuesTests {

        public AddressSpaceReadValuesTests(HistoryServerFixture server) {
            _server = server;
            _hostEntry = Try.Op(() => Dns.GetHostEntry(Utils.GetHostName()))
                ?? Try.Op(() => Dns.GetHostEntry("localhost"));
        }

        private HistoryReadValuesTests<EndpointModel> GetTests() {
            var codec = new VariantEncoderFactory();
            return new HistoryReadValuesTests<EndpointModel>(
                () => new HistoricAccessAdapter<EndpointModel>(new AddressSpaceServices(_server.Client,
                    codec, _server.Logger), codec),
                new EndpointModel {
                    Url = $"opc.tcp://{_hostEntry?.HostName ?? "localhost"}:{_server.Port}/UA/SampleServer",
                    AlternativeUrls = _hostEntry?.AddressList
                        .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork)
                        .Select(ip => $"opc.tcp://{ip}:{_server.Port}/UA/SampleServer").ToHashSet(),
                    Certificate = _server.Certificate?.RawData?.ToThumbprint()
                });
        }

        [Fact]
        public async Task HistoryReadInt64ValuesTest1Async() {
            await GetTests().HistoryReadInt64ValuesTest1Async();
        }

        [Fact]
        public async Task HistoryReadInt64ValuesTest2Async() {
            await GetTests().HistoryReadInt64ValuesTest2Async();
        }

        [Fact]
        public async Task HistoryReadInt64ValuesTest3Async() {
            await GetTests().HistoryReadInt64ValuesTest3Async();
        }

        [Fact]
        public async Task HistoryReadInt64ValuesTest4Async() {
            await GetTests().HistoryReadInt64ValuesTest4Async();
        }

        private readonly HistoryServerFixture _server;
        private readonly IPHostEntry _hostEntry;
    }
}
