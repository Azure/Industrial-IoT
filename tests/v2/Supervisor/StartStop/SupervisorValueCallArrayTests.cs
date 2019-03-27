// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Twin.v2.Supervisor.StartStop {
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

    [Collection(WriteCollection.Name)]
    public class SupervisorValueCallArrayTests {

        public SupervisorValueCallArrayTests(TestServerFixture server) {
            _server = server;
        }

        private CallArrayMethodTests<EndpointRegistrationModel> GetTests(
            string deviceId, string moduleId, IContainer services) {
            return new CallArrayMethodTests<EndpointRegistrationModel>(
                () => services.Resolve<INodeServices<EndpointRegistrationModel>>(),
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
        public async Task NodeMethodMetadataStaticArrayMethod1Test() {
            // Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeMethodMetadataStaticArrayMethod1Test();
                });
            }
        }

        [SkippableFact]
        public async Task NodeMethodMetadataStaticArrayMethod2Test() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeMethodMetadataStaticArrayMethod2Test();
                });
            }
        }

        [SkippableFact]
        public async Task NodeMethodMetadataStaticArrayMethod3Test() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeMethodMetadataStaticArrayMethod3Test();
                });
            }
        }

        [SkippableFact]
        public async Task NodeMethodCallStaticArrayMethod1Test1() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeMethodCallStaticArrayMethod1Test1();
                });
            }
        }

        [SkippableFact]
        public async Task NodeMethodCallStaticArrayMethod1Test2() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeMethodCallStaticArrayMethod1Test2();
                });
            }
        }

        [SkippableFact]
        public async Task NodeMethodCallStaticArrayMethod1Test3() {
            //  Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeMethodCallStaticArrayMethod1Test3();
                });
            }
        }

        [SkippableFact]
        public async Task NodeMethodCallStaticArrayMethod1Test4() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeMethodCallStaticArrayMethod1Test4();
                });
            }
        }

        [SkippableFact]
        public async Task NodeMethodCallStaticArrayMethod1Test5() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeMethodCallStaticArrayMethod1Test5();
                });
            }
        }

        [SkippableFact]
        public async Task NodeMethodCallStaticArrayMethod2Test1() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeMethodCallStaticArrayMethod2Test1();
                });
            }
        }

        [SkippableFact]
        public async Task NodeMethodCallStaticArrayMethod2Test2() {
            // Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeMethodCallStaticArrayMethod2Test2();
                });
            }
        }

        [SkippableFact]
        public async Task NodeMethodCallStaticArrayMethod2Test3() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeMethodCallStaticArrayMethod2Test3();
                });
            }
        }

        [SkippableFact]
        public async Task NodeMethodCallStaticArrayMethod2Test4() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeMethodCallStaticArrayMethod2Test4();
                });
            }
        }

        [SkippableFact]
        public async Task NodeMethodCallStaticArrayMethod3Test1() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeMethodCallStaticArrayMethod3Test1();
                });
            }
        }

        [SkippableFact]
        public async Task NodeMethodCallStaticArrayMethod3Test2() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeMethodCallStaticArrayMethod3Test2();
                });
            }
        }

        [SkippableFact]
        public async Task NodeMethodCallStaticArrayMethod3Test3() {
            // Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeMethodCallStaticArrayMethod3Test3();
                });
            }
        }

    }
}
