// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Twin.v2.Supervisor.Api {
    using Microsoft.Azure.IIoT.Modules.OpcUa.Twin.Tests;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.Api.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.Testing.Fixtures;
    using Microsoft.Azure.IIoT.OpcUa.Testing.Tests;
    using Microsoft.Azure.IIoT.OpcUa.Twin;
    using Microsoft.Azure.IIoT.Utils;
    using Opc.Ua;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading.Tasks;
    using Xunit;
    using Autofac;

    [Collection(ReadCollection.Name)]
    public class SupervisorBrowseTests : IClassFixture<TwinModuleFixture> {

        public SupervisorBrowseTests(TestServerFixture server, TwinModuleFixture module) {
            _server = server;
            _module = module;
            _hostEntry = Try.Op(() => Dns.GetHostEntry(Utils.GetHostName()))
                ?? Try.Op(() => Dns.GetHostEntry("localhost"));
        }

        private BrowseServicesTests<EndpointApiModel> GetTests() {
            return new BrowseServicesTests<EndpointApiModel>(
                () => _module.HubContainer.Resolve<IBrowseServices<EndpointApiModel>>(),
                new EndpointApiModel {
                    Url = $"opc.tcp://{_hostEntry?.HostName ?? "localhost"}:{_server.Port}/UA/SampleServer",
                    AlternativeUrls = _hostEntry?.AddressList
                        .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork)
                        .Select(ip => $"opc.tcp://{ip}:{_server.Port}/UA/SampleServer").ToHashSet(),
                    Certificate = _server.Certificate?.RawData?.ToThumbprint()
                });
        }

        private readonly TestServerFixture _server;
        private readonly TwinModuleFixture _module;
        private readonly IPHostEntry _hostEntry;

        [Fact]
        public async Task NodeBrowseInRootTest1Async() {
            await GetTests().NodeBrowseInRootTest1Async();
        }

        [Fact]
        public async Task NodeBrowseInRootTest2Async() {
            await GetTests().NodeBrowseInRootTest2Async();
        }

        [Fact]
        public async Task NodeBrowseFirstInRootTest1Async() {
            await GetTests().NodeBrowseFirstInRootTest1Async();
        }

        [Fact]
        public async Task NodeBrowseFirstInRootTest2Async() {
            await GetTests().NodeBrowseFirstInRootTest2Async();
        }

        [Fact]
        public async Task NodeBrowseBoilersObjectsTest1Async() {
            await GetTests().NodeBrowseBoilersObjectsTest1Async();
        }

        [Fact]
        public async Task NodeBrowseBoilersObjectsTest2Async() {
            await GetTests().NodeBrowseBoilersObjectsTest2Async();
        }

        [Fact]
        public async Task NodeBrowseDataAccessObjectsTest1Async() {
            await GetTests().NodeBrowseDataAccessObjectsTest1Async();
        }

        [Fact]
        public async Task NodeBrowseDataAccessObjectsTest2Async() {
            await GetTests().NodeBrowseDataAccessObjectsTest2Async();
        }

        [Fact]
        public async Task NodeBrowseDataAccessObjectsTest3Async() {
            await GetTests().NodeBrowseDataAccessObjectsTest3Async();
        }

        [Fact]
        public async Task NodeBrowseDataAccessObjectsTest4Async() {
            await GetTests().NodeBrowseDataAccessObjectsTest4Async();
        }

        [Fact]
        public async Task NodeBrowseDataAccessFC1001Test1Async() {
            await GetTests().NodeBrowseDataAccessFC1001Test1Async();
        }

        [Fact]
        public async Task NodeBrowseDataAccessFC1001Test2Async() {
            await GetTests().NodeBrowseDataAccessFC1001Test2Async();
        }

        [Fact]
        public async Task NodeBrowseStaticScalarVariablesTestAsync() {
            await GetTests().NodeBrowseStaticScalarVariablesTestAsync();
        }

        [Fact]
        public async Task NodeBrowseStaticScalarVariablesTestWithFilter1Async() {
            await GetTests().NodeBrowseStaticScalarVariablesTestWithFilter1Async();
        }

        [Fact]
        public async Task NodeBrowseStaticScalarVariablesTestWithFilter2Async() {
            await GetTests().NodeBrowseStaticScalarVariablesTestWithFilter2Async();
        }

        [Fact]
        public async Task NodeBrowseStaticArrayVariablesTestAsync() {
            await GetTests().NodeBrowseStaticArrayVariablesTestAsync();
        }

        [SkippableFact]
        public async Task NodeBrowseStaticArrayVariablesRawModeTestAsync() {
            Skip.If(true, "No API impl.");
            await GetTests().NodeBrowseStaticArrayVariablesRawModeTestAsync();
        }

        [Fact]
        public async Task NodeBrowseStaticArrayVariablesWithValuesTestAsync() {
            await GetTests().NodeBrowseStaticArrayVariablesWithValuesTestAsync();
        }

        [Fact]
        public async Task NodeBrowseContinuationTest1Async() {
            await GetTests().NodeBrowseContinuationTest1Async();
        }

        [Fact]
        public async Task NodeBrowseContinuationTest2Async() {
            await GetTests().NodeBrowseContinuationTest2Async();
        }

        [Fact]
        public async Task NodeBrowseContinuationTest3Async() {
            await GetTests().NodeBrowseContinuationTest3Async();
        }

        [Fact]
        public async Task NodeBrowsePathStaticScalarMethod3Test1Async() {
            await GetTests().NodeBrowsePathStaticScalarMethod3Test1Async();
        }

        [Fact]
        public async Task NodeBrowsePathStaticScalarMethod3Test2Async() {
            await GetTests().NodeBrowsePathStaticScalarMethod3Test2Async();
        }

        [Fact]
        public async Task NodeBrowsePathStaticScalarMethod3Test3Async() {
            await GetTests().NodeBrowsePathStaticScalarMethod3Test3Async();
        }

        [Fact]
        public async Task NodeBrowsePathStaticScalarMethodsTestAsync() {
            await GetTests().NodeBrowsePathStaticScalarMethodsTestAsync();
        }

        [Fact]
        public async Task NodeBrowseDiagnosticsNoneTestAsync() {
            await GetTests().NodeBrowseDiagnosticsNoneTestAsync();
        }

        [Fact]
        public async Task NodeBrowseDiagnosticsStatusTestAsync() {
            await GetTests().NodeBrowseDiagnosticsStatusTestAsync();
        }

        [Fact]
        public async Task NodeBrowseDiagnosticsOperationsTestAsync() {
            await GetTests().NodeBrowseDiagnosticsOperationsTestAsync();
        }

        [Fact]
        public async Task NodeBrowseDiagnosticsVerboseTestAsync() {
            await GetTests().NodeBrowseDiagnosticsVerboseTestAsync();
        }
    }
}
