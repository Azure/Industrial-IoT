// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Twin.v2.Twin.Endpoint {
    using Microsoft.Azure.IIoT.Modules.OpcUa.Twin.Tests;
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
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

    [Collection(WriteCollection.Name)]
    public class TwinValueWriteScalarTests : IClassFixture<TwinModuleFixture> {

        public TwinValueWriteScalarTests(TestServerFixture server, TwinModuleFixture module) {
            _server = server;
            _module = module;
            _hostEntry = Try.Op(() => Dns.GetHostEntry(Utils.GetHostName()))
                ?? Try.Op(() => Dns.GetHostEntry("localhost"));
        }

        private WriteScalarValueTests<string> GetTests() {
            var endpoint = new EndpointRegistrationModel {
                Endpoint = new EndpointModel {
                    Url = $"opc.tcp://{_hostEntry?.HostName ?? "localhost"}:{_server.Port}/UA/SampleServer",
                    AlternativeUrls = _hostEntry?.AddressList
                        .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork)
                        .Select(ip => $"opc.tcp://{ip}:{_server.Port}/UA/SampleServer").ToHashSet(),
                    Certificate = _server.Certificate?.RawData?.ToThumbprint()
                },
                Id = "testid",
                SupervisorId = SupervisorModelEx.CreateSupervisorId(
                  _module.DeviceId, _module.ModuleId)
            };
            endpoint = _module.RegisterAndActivateTwinId(endpoint);
            return new WriteScalarValueTests<string>(
                () => _module.HubContainer.Resolve<INodeServices<string>>(),
                endpoint.Id, (ep, n) => _server.Client.ReadValueAsync(endpoint.Endpoint, n));
        }

        private readonly TestServerFixture _server;
        private readonly TwinModuleFixture _module;
        private readonly IPHostEntry _hostEntry;


        [Fact]
        public async Task NodeWriteStaticScalarBooleanValueVariableTestAsync() {
            await GetTests().NodeWriteStaticScalarBooleanValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticScalarBooleanValueVariableWithBrowsePathTest1Async() {
            await GetTests().NodeWriteStaticScalarBooleanValueVariableWithBrowsePathTest1Async();
        }

        [Fact]
        public async Task NodeWriteStaticScalarBooleanValueVariableWithBrowsePathTest2Async() {
            await GetTests().NodeWriteStaticScalarBooleanValueVariableWithBrowsePathTest2Async();
        }

        [Fact]
        public async Task NodeWriteStaticScalarBooleanValueVariableWithBrowsePathTest3Async() {
            await GetTests().NodeWriteStaticScalarBooleanValueVariableWithBrowsePathTest3Async();
        }

        [Fact]
        public async Task NodeWriteStaticScalarSByteValueVariableTestAsync() {
            await GetTests().NodeWriteStaticScalarSByteValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticScalarByteValueVariableTestAsync() {
            await GetTests().NodeWriteStaticScalarByteValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticScalarInt16ValueVariableTestAsync() {
            await GetTests().NodeWriteStaticScalarInt16ValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticScalarUInt16ValueVariableTestAsync() {
            await GetTests().NodeWriteStaticScalarUInt16ValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticScalarInt32ValueVariableTestAsync() {
            await GetTests().NodeWriteStaticScalarInt32ValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticScalarUInt32ValueVariableTestAsync() {
            await GetTests().NodeWriteStaticScalarUInt32ValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticScalarInt64ValueVariableTestAsync() {
            await GetTests().NodeWriteStaticScalarInt64ValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticScalarUInt64ValueVariableTestAsync() {
            await GetTests().NodeWriteStaticScalarUInt64ValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticScalarFloatValueVariableTestAsync() {
            await GetTests().NodeWriteStaticScalarFloatValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticScalarDoubleValueVariableTestAsync() {
            await GetTests().NodeWriteStaticScalarDoubleValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticScalarStringValueVariableTestAsync() {
            await GetTests().NodeWriteStaticScalarStringValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticScalarDateTimeValueVariableTestAsync() {
            await GetTests().NodeWriteStaticScalarDateTimeValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticScalarGuidValueVariableTestAsync() {
            await GetTests().NodeWriteStaticScalarGuidValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticScalarByteStringValueVariableTestAsync() {
            await GetTests().NodeWriteStaticScalarByteStringValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticScalarXmlElementValueVariableTestAsync() {
            await GetTests().NodeWriteStaticScalarXmlElementValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticScalarNodeIdValueVariableTestAsync() {
            await GetTests().NodeWriteStaticScalarNodeIdValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticScalarExpandedNodeIdValueVariableTestAsync() {
            await GetTests().NodeWriteStaticScalarExpandedNodeIdValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticScalarQualifiedNameValueVariableTestAsync() {
            await GetTests().NodeWriteStaticScalarQualifiedNameValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticScalarLocalizedTextValueVariableTestAsync() {
            await GetTests().NodeWriteStaticScalarLocalizedTextValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticScalarStatusCodeValueVariableTestAsync() {
            await GetTests().NodeWriteStaticScalarStatusCodeValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticScalarVariantValueVariableTestAsync() {
            await GetTests().NodeWriteStaticScalarVariantValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticScalarEnumerationValueVariableTestAsync() {
            await GetTests().NodeWriteStaticScalarEnumerationValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticScalarStructuredValueVariableTestAsync() {
            await GetTests().NodeWriteStaticScalarStructuredValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticScalarNumberValueVariableTestAsync() {
            await GetTests().NodeWriteStaticScalarNumberValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticScalarIntegerValueVariableTestAsync() {
            await GetTests().NodeWriteStaticScalarIntegerValueVariableTestAsync();
        }

        [Fact]
        public async Task NodeWriteStaticScalarUIntegerValueVariableTestAsync() {
            await GetTests().NodeWriteStaticScalarUIntegerValueVariableTestAsync();
        }
    }
}
