// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Twin.v2.Supervisor.StartStop {
    using Microsoft.Azure.IIoT.Modules.OpcUa.Twin.Tests;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
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
                        Url = $"opc.tcp://{Dns.GetHostName()}:{_server.Port}/UA/SampleServer",
                        Certificate = _server.Certificate?.RawData?.ToThumbprint()
                    },
                    Id = "testid",
                    SupervisorId = SupervisorModelEx.CreateSupervisorId(deviceId, moduleId)
                });
        }

        private readonly TestServerFixture _server;
#if DEBUG
        private readonly bool _runAll = true;
#else
        private readonly bool _runAll = Environment.GetEnvironmentVariable("TEST_ALL") != null;
#endif

        [SkippableFact]
        public async Task NodeBrowseInRootTest1Async() {
            // Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeBrowseInRootTest1Async();
                });
            }
        }

        [SkippableFact]
        public async Task NodeBrowseInRootTest2Async() {
            // Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeBrowseInRootTest2Async();
                });
            }
        }

        [SkippableFact]
        public async Task NodeBrowseFirstInRootTest1Async() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeBrowseFirstInRootTest1Async();
                });
            }
        }

        [SkippableFact]
        public async Task NodeBrowseFirstInRootTest2Async() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeBrowseFirstInRootTest2Async();
                });
            }
        }

        [SkippableFact]
        public async Task NodeBrowseBoilersObjectsTest1Async() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeBrowseBoilersObjectsTest1Async();
                });
            }
        }

        [SkippableFact]
        public async Task NodeBrowseBoilersObjectsTest2Async() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeBrowseBoilersObjectsTest2Async();
                });
            }
        }

        [SkippableFact]
        public async Task NodeBrowseDataAccessObjectsTest1Async() {
            // Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeBrowseDataAccessObjectsTest1Async();
                });
            }
        }

        [SkippableFact]
        public async Task NodeBrowseDataAccessObjectsTest2Async() {
            // Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeBrowseDataAccessObjectsTest2Async();
                });
            }
        }

        [SkippableFact]
        public async Task NodeBrowseDataAccessObjectsTest3Async() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeBrowseDataAccessObjectsTest3Async();
                });
            }
        }

        [SkippableFact]
        public async Task NodeBrowseDataAccessObjectsTest4Async() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeBrowseDataAccessObjectsTest4Async();
                });
            }
        }

        [SkippableFact]
        public async Task NodeBrowseDataAccessFC1001Test1Async() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeBrowseDataAccessFC1001Test1Async();
                });
            }
        }

        [SkippableFact]
        public async Task NodeBrowseDataAccessFC1001Test2Async() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeBrowseDataAccessFC1001Test1Async();
                });
            }
        }

        [SkippableFact]
        public async Task NodeBrowseStaticScalarVariablesTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeBrowseStaticScalarVariablesTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeBrowseStaticScalarVariablesTestWithFilter1Async() {
            // Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeBrowseStaticScalarVariablesTestWithFilter1Async();
                });
            }
        }

        [SkippableFact]
        public async Task NodeBrowseStaticScalarVariablesTestWithFilter2Async() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeBrowseStaticScalarVariablesTestWithFilter2Async();
                });
            }
        }

        [SkippableFact]
        public async Task NodeBrowseStaticArrayVariablesTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeBrowseStaticArrayVariablesTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeBrowseStaticArrayVariablesWithValuesTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeBrowseStaticArrayVariablesWithValuesTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeBrowseStaticArrayVariablesRawModeTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeBrowseStaticArrayVariablesRawModeTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeBrowseContinuationTest1Async() {
            // Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeBrowseContinuationTest1Async();
                });
            }
        }

        [SkippableFact]
        public async Task NodeBrowseContinuationTest2Async() {
            // Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeBrowseContinuationTest2Async();
                });
            }
        }

        [SkippableFact]
        public async Task NodeBrowseContinuationTest3Async() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeBrowseContinuationTest3Async();
                });
            }
        }

        [SkippableFact]
        public async Task NodeBrowsePathStaticScalarMethod3Test1Async() {
            // Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeBrowsePathStaticScalarMethod3Test1Async();
                });
            }
        }

        [SkippableFact]
        public async Task NodeBrowsePathStaticScalarMethod3Test2Async() {
            // Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeBrowsePathStaticScalarMethod3Test2Async();
                });
            }
        }

        [SkippableFact]
        public async Task NodeBrowsePathStaticScalarMethod3Test3Async() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeBrowsePathStaticScalarMethod3Test3Async();
                });
            }
        }

        [SkippableFact]
        public async Task NodeBrowsePathStaticScalarMethodsTestAsync() {
            // Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeBrowsePathStaticScalarMethodsTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeBrowseDiagnosticsNoneTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeBrowseDiagnosticsNoneTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeBrowseDiagnosticsStatusTestAsync() {
            // Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeBrowseDiagnosticsStatusTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeBrowseDiagnosticsOperationsTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeBrowseDiagnosticsOperationsTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeBrowseDiagnosticsVerboseTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeBrowseDiagnosticsVerboseTestAsync();
                });
            }
        }
    }
}
