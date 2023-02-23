// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Services.TestData.Tests {
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

    [Collection(ReadCollection.Name)]
    public class BrowsePathTests {
        public BrowsePathTests(TestServerFixture server) {
            _server = server;
            _hostEntry = Try.Op(() => Dns.GetHostEntry(Utils.GetHostName()))
                ?? Try.Op(() => Dns.GetHostEntry("localhost"));
        }

        private BrowsePathTests<ConnectionModel> GetTests() {
            return new BrowsePathTests<ConnectionModel>(
                () => new NodeServices<ConnectionModel>(_server.Client,
                    _server.Logger), new ConnectionModel {
                        Endpoint = new EndpointModel {
                            Url = $"opc.tcp://{_hostEntry?.HostName ?? "localhost"}:{_server.Port}/UA/SampleServer",
                            AlternativeUrls = _hostEntry?.AddressList
                        .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork)
                        .Select(ip => $"opc.tcp://{ip}:{_server.Port}/UA/SampleServer").ToHashSet(),
                            Certificate = _server.Certificate?.RawData?.ToThumbprint()
                        }
                    });
        }

        private readonly TestServerFixture _server;
        private readonly IPHostEntry _hostEntry;

        [Fact]
        public async Task NodeBrowsePathStaticScalarMethod3Test1Async() {
            await GetTests().NodeBrowsePathStaticScalarMethod3Test1Async().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeBrowsePathStaticScalarMethod3Test2Async() {
            await GetTests().NodeBrowsePathStaticScalarMethod3Test2Async().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeBrowsePathStaticScalarMethod3Test3Async() {
            await GetTests().NodeBrowsePathStaticScalarMethod3Test3Async().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeBrowsePathStaticScalarMethodsTestAsync() {
            await GetTests().NodeBrowsePathStaticScalarMethodsTestAsync().ConfigureAwait(false);
        }
    }
}
