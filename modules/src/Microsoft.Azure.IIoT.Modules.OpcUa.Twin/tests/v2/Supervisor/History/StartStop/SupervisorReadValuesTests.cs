// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Twin.v2.Supervisor.History.StartStop {
    using Microsoft.Azure.IIoT.Modules.OpcUa.Twin.Tests;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.Testing.Fixtures;
    using Microsoft.Azure.IIoT.OpcUa.Testing.Tests;
    using Microsoft.Azure.IIoT.OpcUa.History;
    using System.Net;
    using System.Threading.Tasks;
    using Xunit;
    using Autofac;
    using System;

    [Collection(ReadHistoryCollection.Name)]
    public class SupervisorReadValuesTests {

        public SupervisorReadValuesTests(HistoryServerFixture server) {
            _server = server;
        }

        private HistoryReadValuesTests<EndpointRegistrationModel> GetTests(
            string deviceId, string moduleId, IContainer services) {
            return new HistoryReadValuesTests<EndpointRegistrationModel>(
                () => services.Resolve<IHistorianServices<EndpointRegistrationModel>>(),
                new EndpointRegistrationModel {
                    Endpoint = new EndpointModel {
                        Url = $"opc.tcp://{Dns.GetHostName()}:{_server.Port}/UA/SampleServer",
                        Certificate = _server.Certificate?.RawData?.ToThumbprint()
                    },
                    Id = "testid",
                    SupervisorId = SupervisorModelEx.CreateSupervisorId(deviceId, moduleId)
                });
        }

        private readonly HistoryServerFixture _server;
#if DEBUG
        private readonly bool _runAll = true;
#else
        private readonly bool _runAll = Environment.GetEnvironmentVariable("TEST_ALL") != null;
#endif


        [SkippableFact]
        public async Task HistoryReadInt64ValuesTest1Async() {
            // Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).HistoryReadInt64ValuesTest1Async();
                });
            }
        }

        [SkippableFact]
        public async Task HistoryReadInt64ValuesTest2Async() {
            // Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).HistoryReadInt64ValuesTest2Async();
                });
            }
        }

        [SkippableFact]
        public async Task HistoryReadInt64ValuesTest3Async() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).HistoryReadInt64ValuesTest3Async();
                });
            }
        }

        [SkippableFact]
        public async Task HistoryReadInt64ValuesTest4Async() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).HistoryReadInt64ValuesTest4Async();
                });
            }
        }
    }
}
