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

    [Collection(TestServerWriteCollection.Name)]
    public class ValueCallArrayTests : IClassFixture<PublisherModuleFixture>
    {
        public ValueCallArrayTests(TestServerFixture server, PublisherModuleFixture module)
        {
            _server = server;
            _module = module;
            _hostEntry = Try.Op(() => Dns.GetHostEntry(Utils.GetHostName()))
                ?? Try.Op(() => Dns.GetHostEntry("localhost"));
        }

        private CallArrayMethodTests<ConnectionModel> GetTests()
        {
            return new CallArrayMethodTests<ConnectionModel>(
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
        public async Task NodeMethodMetadataStaticArrayMethod1TestAsync()
        {
            await GetTests().NodeMethodMetadataStaticArrayMethod1TestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeMethodMetadataStaticArrayMethod2TestAsync()
        {
            await GetTests().NodeMethodMetadataStaticArrayMethod2TestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeMethodMetadataStaticArrayMethod3TestAsync()
        {
            await GetTests().NodeMethodMetadataStaticArrayMethod3TestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeMethodCallStaticArrayMethod1Test1Async()
        {
            await GetTests().NodeMethodCallStaticArrayMethod1Test1Async().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeMethodCallStaticArrayMethod1Test2Async()
        {
            await GetTests().NodeMethodCallStaticArrayMethod1Test2Async().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeMethodCallStaticArrayMethod1Test3Async()
        {
            await GetTests().NodeMethodCallStaticArrayMethod1Test3Async().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeMethodCallStaticArrayMethod1Test4Async()
        {
            await GetTests().NodeMethodCallStaticArrayMethod1Test4Async().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeMethodCallStaticArrayMethod1Test5Async()
        {
            await GetTests().NodeMethodCallStaticArrayMethod1Test5Async().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeMethodCallStaticArrayMethod2Test1Async()
        {
            await GetTests().NodeMethodCallStaticArrayMethod2Test1Async().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeMethodCallStaticArrayMethod2Test2Async()
        {
            await GetTests().NodeMethodCallStaticArrayMethod2Test2Async().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeMethodCallStaticArrayMethod2Test3Async()
        {
            await GetTests().NodeMethodCallStaticArrayMethod2Test3Async().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeMethodCallStaticArrayMethod2Test4Async()
        {
            await GetTests().NodeMethodCallStaticArrayMethod2Test4Async().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeMethodCallStaticArrayMethod3Test1Async()
        {
            await GetTests().NodeMethodCallStaticArrayMethod3Test1Async().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeMethodCallStaticArrayMethod3Test2Async()
        {
            await GetTests().NodeMethodCallStaticArrayMethod3Test2Async().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeMethodCallStaticArrayMethod3Test3Async()
        {
            await GetTests().NodeMethodCallStaticArrayMethod3Test3Async().ConfigureAwait(false);
        }
    }
}
