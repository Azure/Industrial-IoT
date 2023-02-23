// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.v2.Twin.Api
{
    using Autofac;
    using Azure.IIoT.OpcUa.Publisher.Module.Tests.Fixtures;
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

    [Collection(TestServerReadCollection.Name)]
    public class BrowsePathTests : IClassFixture<PublisherModuleFixture>
    {
        public BrowsePathTests(TestServerFixture server, PublisherModuleFixture module)
        {
            _server = server;
            _module = module;
            _hostEntry = Try.Op(() => Dns.GetHostEntry(Utils.GetHostName()))
                ?? Try.Op(() => Dns.GetHostEntry("localhost"));
        }

        private BrowsePathTests<ConnectionModel> GetTests()
        {
            return new BrowsePathTests<ConnectionModel>(
                () => _module.HubContainer.Resolve<INodeServices<ConnectionModel>>(),
                new ConnectionModel
                {
                    Endpoint = new EndpointModel
                    {
                        Url = $"opc.tcp://{_hostEntry?.HostName ?? "localhost"}:{_server.Port}/UA/SampleServer",
                        AlternativeUrls = _hostEntry?.AddressList
                        .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork)
                        .Select(ip => $"opc.tcp://{ip}:{_server.Port}/UA/SampleServer").ToHashSet(),
                        Certificate = _server.Certificate?.RawData?.ToThumbprint()
                    }
                });
        }

        private readonly TestServerFixture _server;
        private readonly PublisherModuleFixture _module;
        private readonly IPHostEntry _hostEntry;

        [Fact]
        public async Task NodeBrowsePathStaticScalarMethod3Test1Async()
        {
            await GetTests().NodeBrowsePathStaticScalarMethod3Test1Async().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeBrowsePathStaticScalarMethod3Test2Async()
        {
            await GetTests().NodeBrowsePathStaticScalarMethod3Test2Async().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeBrowsePathStaticScalarMethod3Test3Async()
        {
            await GetTests().NodeBrowsePathStaticScalarMethod3Test3Async().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeBrowsePathStaticScalarMethodsTestAsync()
        {
            await GetTests().NodeBrowsePathStaticScalarMethodsTestAsync().ConfigureAwait(false);
        }
    }
}
