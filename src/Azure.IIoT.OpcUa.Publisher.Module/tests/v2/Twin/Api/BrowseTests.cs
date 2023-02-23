// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.v2.Twin.Api {
    using Azure.IIoT.OpcUa.Publisher.Module.Tests.Fixtures;
    using Azure.IIoT.OpcUa.Shared.Models;
    using Azure.IIoT.OpcUa.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Testing.Tests;
    using Autofac;
    using Furly.Extensions.Utils;
    using Opc.Ua;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading.Tasks;
    using Xunit;

    [Collection(TestServerReadCollection.Name)]
    public class BrowseTests : IClassFixture<PublisherModuleFixture> {
        public BrowseTests(TestServerFixture server, PublisherModuleFixture module) {
            _server = server;
            _module = module;
            _hostEntry = Try.Op(() => Dns.GetHostEntry(Utils.GetHostName()))
                ?? Try.Op(() => Dns.GetHostEntry("localhost"));
        }

        private BrowseServicesTests<ConnectionModel> GetTests() {
            return new BrowseServicesTests<ConnectionModel>(
                () => _module.HubContainer.Resolve<INodeServices<ConnectionModel>>(),
                new ConnectionModel {
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
        private readonly PublisherModuleFixture _module;
        private readonly IPHostEntry _hostEntry;

        [Fact]
        public async Task NodeBrowseInRootTest1Async() {
            await GetTests().NodeBrowseInRootTest1Async().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeBrowseInRootTest2Async() {
            await GetTests().NodeBrowseInRootTest2Async().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeBrowseFirstInRootTest1Async() {
            await GetTests().NodeBrowseFirstInRootTest1Async().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeBrowseFirstInRootTest2Async() {
            await GetTests().NodeBrowseFirstInRootTest2Async().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeBrowseBoilersObjectsTest1Async() {
            await GetTests().NodeBrowseBoilersObjectsTest1Async().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeBrowseBoilersObjectsTest2Async() {
            await GetTests().NodeBrowseBoilersObjectsTest2Async().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeBrowseDataAccessObjectsTest1Async() {
            await GetTests().NodeBrowseDataAccessObjectsTest1Async().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeBrowseDataAccessObjectsTest2Async() {
            await GetTests().NodeBrowseDataAccessObjectsTest2Async().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeBrowseDataAccessObjectsTest3Async() {
            await GetTests().NodeBrowseDataAccessObjectsTest3Async().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeBrowseDataAccessObjectsTest4Async() {
            await GetTests().NodeBrowseDataAccessObjectsTest4Async().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeBrowseDataAccessFC1001Test1Async() {
            await GetTests().NodeBrowseDataAccessFC1001Test1Async().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeBrowseDataAccessFC1001Test2Async() {
            await GetTests().NodeBrowseDataAccessFC1001Test2Async().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeBrowseStaticScalarVariablesTestAsync() {
            await GetTests().NodeBrowseStaticScalarVariablesTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeBrowseStaticScalarVariablesTestWithFilter1Async() {
            await GetTests().NodeBrowseStaticScalarVariablesTestWithFilter1Async().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeBrowseStaticScalarVariablesTestWithFilter2Async() {
            await GetTests().NodeBrowseStaticScalarVariablesTestWithFilter2Async().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeBrowseStaticArrayVariablesTestAsync() {
            await GetTests().NodeBrowseStaticArrayVariablesTestAsync().ConfigureAwait(false);
        }

        [SkippableFact]
        public async Task NodeBrowseStaticArrayVariablesRawModeTestAsync() {
            Skip.If(true, "No API impl.");
            await GetTests().NodeBrowseStaticArrayVariablesRawModeTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeBrowseStaticArrayVariablesWithValuesTestAsync() {
            await GetTests().NodeBrowseStaticArrayVariablesWithValuesTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeBrowseContinuationTest1Async() {
            await GetTests().NodeBrowseContinuationTest1Async().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeBrowseContinuationTest2Async() {
            await GetTests().NodeBrowseContinuationTest2Async().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeBrowseContinuationTest3Async() {
            await GetTests().NodeBrowseContinuationTest3Async().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeBrowseDiagnosticsNoneTestAsync() {
            await GetTests().NodeBrowseDiagnosticsNoneTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeBrowseDiagnosticsStatusTestAsync() {
            await GetTests().NodeBrowseDiagnosticsStatusTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeBrowseDiagnosticsInfoTestAsync() {
            await GetTests().NodeBrowseDiagnosticsInfoTestAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task NodeBrowseDiagnosticsVerboseTestAsync() {
            await GetTests().NodeBrowseDiagnosticsVerboseTestAsync().ConfigureAwait(false);
        }
    }
}
