// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Twin.v1.Supervisor.History.Endpoint {
    using Microsoft.Azure.IIoT.Modules.OpcUa.Twin.Tests;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Testing.Fixtures;
    using Microsoft.Azure.IIoT.OpcUa.Testing.Tests;
    using Microsoft.Azure.IIoT.OpcUa.History;
    using System.Net;
    using System.Threading.Tasks;
    using Xunit;
    using Autofac;

    [Collection(ReadHistoryCollection.Name)]
    public class SupervisorReadValuesTests : IClassFixture<TwinModuleFixture> {

        public SupervisorReadValuesTests(HistoryServerFixture server, TwinModuleFixture module) {
            _server = server;
            _module = module;
        }

        private HistoryReadValuesTests<EndpointRegistrationModel> GetTests() {
            return new HistoryReadValuesTests<EndpointRegistrationModel>(
                () => _module.HubContainer.Resolve<IHistorianServices<EndpointRegistrationModel>>(),
                new EndpointRegistrationModel {
                    Endpoint = new EndpointModel {
                        Url = $"opc.tcp://{Dns.GetHostName()}:{_server.Port}/UA/SampleServer"
                    },
                    Id = "testid",
                    SupervisorId = SupervisorModelEx.CreateSupervisorId(
                        _module.DeviceId, _module.ModuleId)
                });
        }

        private readonly HistoryServerFixture _server;
        private readonly TwinModuleFixture _module;


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
