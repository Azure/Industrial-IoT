// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Twin.v1.Supervisor.StartStop {
    using Microsoft.Azure.IIoT.Modules.OpcUa.Twin.Tests;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Testing.Fixtures;
    using Microsoft.Azure.IIoT.OpcUa.Testing.Tests;
    using Microsoft.Azure.IIoT.OpcUa.Twin;
    using System.Net;
    using System.Threading.Tasks;
    using Xunit;
    using Autofac;
    using System;

    [Collection(ReadCollection.Name)]
    public class SupervisorBrowseTests {

        public SupervisorBrowseTests(TestServerFixture server) {
            _server = server;
        }

        private BrowseServicesTests<EndpointRegistrationModel> GetTests(
            string deviceId, string moduleId, IContainer services) {
            return new BrowseServicesTests<EndpointRegistrationModel>(
                () => services.Resolve<IBrowseServices<EndpointRegistrationModel>>(),
                new EndpointRegistrationModel {
                    Endpoint = new EndpointModel {
                        Url = $"opc.tcp://{Dns.GetHostName()}:{_server.Port}/UA/SampleServer"
                    },
                    Id = "testid",
                    SupervisorId = SupervisorModelEx.CreateSupervisorId(deviceId, moduleId)
                });
        }

        private readonly TestServerFixture _server;
        private static readonly bool _runAll = Environment.GetEnvironmentVariable("TEST_ALL") != null;

        [SkippableFact]
        public async Task NodeBrowseInRootTest1() {
            // Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeBrowseInRootTest1();
                });
            }
        }

        [SkippableFact]
        public async Task NodeBrowseFirstInRootTest1() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeBrowseFirstInRootTest1();
                });
            }
        }

        [SkippableFact]
        public async Task NodeBrowseFirstInRootTest2() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeBrowseFirstInRootTest2();
                });
            }
        }

        [SkippableFact]
        public async Task NodeBrowseBoilersObjectsTest1() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeBrowseBoilersObjectsTest1();
                });
            }
        }

        [SkippableFact]
        public async Task NodeBrowseBoilersObjectsTest2() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeBrowseBoilersObjectsTest2();
                });
            }
        }

        [SkippableFact]
        public async Task NodeBrowseStaticScalarVariablesTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeBrowseStaticScalarVariablesTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeBrowseStaticArrayVariablesTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeBrowseStaticArrayVariablesTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeBrowseStaticArrayVariablesWithValuesTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeBrowseStaticArrayVariablesWithValuesTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeBrowseStaticArrayVariablesRawModeTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeBrowseStaticArrayVariablesRawModeTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeBrowsePathStaticScalarMethod3Test1() {
            // Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeBrowsePathStaticScalarMethod3Test1();
                });
            }
        }

        [SkippableFact]
        public async Task NodeBrowsePathStaticScalarMethod3Test2() {
            // Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeBrowsePathStaticScalarMethod3Test2();
                });
            }
        }

        [SkippableFact]
        public async Task NodeBrowsePathStaticScalarMethod3Test3() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeBrowsePathStaticScalarMethod3Test3();
                });
            }
        }

        [SkippableFact]
        public async Task NodeBrowsePathStaticScalarMethodsTest() {
            // Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeBrowsePathStaticScalarMethodsTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeBrowseDiagnosticsNoneTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeBrowseDiagnosticsNoneTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeBrowseDiagnosticsStatusTest() {
            // Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeBrowseDiagnosticsStatusTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeBrowseDiagnosticsOperationsTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeBrowseDiagnosticsOperationsTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeBrowseDiagnosticsVerboseTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeBrowseDiagnosticsVerboseTest();
                });
            }
        }
    }
}
