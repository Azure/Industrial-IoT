// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Edge.History {
    using Azure.IIoT.OpcUa.Encoders;
    using Azure.IIoT.OpcUa.Publisher.Services;
    using Azure.IIoT.OpcUa.Shared.Models;
    using Azure.IIoT.OpcUa.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Testing.Tests;
    using Furly.Extensions.Utils;
    using Opc.Ua;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading.Tasks;
    using Xunit;

    [Collection(HistoryReadCollection.Name)]
    public class AddressSpaceReadValuesTests {

        public AddressSpaceReadValuesTests(HistoryServerFixture server) {
            _server = server;
            _hostEntry = Try.Op(() => Dns.GetHostEntry(Utils.GetHostName()))
                ?? Try.Op(() => Dns.GetHostEntry("localhost"));
        }

        private HistoryReadValuesTests<ConnectionModel> GetTests() {
            return new HistoryReadValuesTests<ConnectionModel>(
                () => new HistoryServices<ConnectionModel>(new NodeServices<ConnectionModel>(_server.Client,
                    _server.Logger)), new ConnectionModel {
                        Endpoint = new EndpointModel {
                            Url = $"opc.tcp://{_hostEntry?.HostName ?? "localhost"}:{_server.Port}/UA/SampleServer",
                            AlternativeUrls = _hostEntry?.AddressList
                        .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork)
                        .Select(ip => $"opc.tcp://{ip}:{_server.Port}/UA/SampleServer").ToHashSet(),
                            Certificate = _server.Certificate?.RawData?.ToThumbprint()
                        }
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
