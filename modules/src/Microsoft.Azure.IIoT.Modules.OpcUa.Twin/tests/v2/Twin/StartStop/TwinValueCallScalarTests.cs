// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Twin.v2.Twin.StartStop {
    using Microsoft.Azure.IIoT.Modules.OpcUa.Twin.Tests;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Testing.Fixtures;
    using Microsoft.Azure.IIoT.OpcUa.Testing.Tests;
    using Microsoft.Azure.IIoT.OpcUa.Twin;
    using System;
    using System.Net;
    using System.Threading.Tasks;
    using Xunit;
    using Autofac;

    [Collection(WriteCollection.Name)]
    public class TwinValueCallScalarTests {

        public TwinValueCallScalarTests(TestServerFixture server) {
            _server = server;
        }

        private EndpointModel Endpoint => new EndpointModel {
            Url = $"opc.tcp://{Dns.GetHostName()}:{_server.Port}/UA/SampleServer",
            Certificate = _server.Certificate?.RawData
        };

        private CallScalarMethodTests<string> GetTests(EndpointRegistrationModel endpoint, IContainer services) {
            return new CallScalarMethodTests<string>(
                () => services.Resolve<INodeServices<string>>(), endpoint.Id);
        }

        private readonly TestServerFixture _server;
        private static readonly bool _runAll = Environment.GetEnvironmentVariable("TEST_ALL") != null;

        [SkippableFact]
        public async Task NodeMethodMetadataStaticScalarMethod1Test() {
            // Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeMethodMetadataStaticScalarMethod1Test();
                });
            }
        }

        [SkippableFact]
        public async Task NodeMethodMetadataStaticScalarMethod2Test() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeMethodMetadataStaticScalarMethod2Test();
                });
            }
        }

        [SkippableFact]
        public async Task NodeMethodMetadataStaticScalarMethod3Test() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeMethodMetadataStaticScalarMethod3Test();
                });
            }
        }

        [SkippableFact]
        public async Task NodeMethodMetadataStaticScalarMethod3WithBrowsePathTest1() {
            // Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeMethodMetadataStaticScalarMethod3WithBrowsePathTest1();
                });
            }
        }

        [SkippableFact]
        public async Task NodeMethodMetadataStaticScalarMethod3WithBrowsePathTest2() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeMethodMetadataStaticScalarMethod3WithBrowsePathTest2();
                });
            }
        }

        [SkippableFact]
        public async Task NodeMethodCallStaticScalarMethod1Test1() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeMethodCallStaticScalarMethod1Test1();
                });
            }
        }

        [SkippableFact]
        public async Task NodeMethodCallStaticScalarMethod1Test2() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeMethodCallStaticScalarMethod1Test2();
                });
            }
        }

        [SkippableFact]
        public async Task NodeMethodCallStaticScalarMethod1Test3() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeMethodCallStaticScalarMethod1Test3();
                });
            }
        }

        [SkippableFact]
        public async Task NodeMethodCallStaticScalarMethod1Test4() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeMethodCallStaticScalarMethod1Test4();
                });
            }
        }

        [SkippableFact]
        public async Task NodeMethodCallStaticScalarMethod1Test5() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeMethodCallStaticScalarMethod1Test5();
                });
            }
        }

        [SkippableFact]
        public async Task NodeMethodCallStaticScalarMethod2Test1() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeMethodCallStaticScalarMethod2Test1();
                });
            }
        }

        [SkippableFact]
        public async Task NodeMethodCallStaticScalarMethod2Test2() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeMethodCallStaticScalarMethod2Test2();
                });
            }
        }

        [SkippableFact]
        public async Task NodeMethodCallStaticScalarMethod3Test1() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeMethodCallStaticScalarMethod3Test1();
                });
            }
        }

        [SkippableFact]
        public async Task NodeMethodCallStaticScalarMethod3Test2() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeMethodCallStaticScalarMethod3Test2();
                });
            }
        }

        [SkippableFact]
        public async Task NodeMethodCallStaticScalarMethod3WithBrowsePathNoIdsTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeMethodCallStaticScalarMethod3WithBrowsePathNoIdsTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeMethodCallStaticScalarMethod3WithObjectIdAndBrowsePathTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeMethodCallStaticScalarMethod3WithObjectIdAndBrowsePathTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeMethodCallStaticScalarMethod3WithObjectIdAndMethodIdAndBrowsePathTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeMethodCallStaticScalarMethod3WithObjectIdAndMethodIdAndBrowsePathTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeMethodCallStaticScalarMethod3WithObjectPathAndMethodIdAndBrowsePathTest() {
            // Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeMethodCallStaticScalarMethod3WithObjectPathAndMethodIdAndBrowsePathTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeMethodCallStaticScalarMethod3WithObjectIdAndPathAndMethodIdAndPathTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeMethodCallStaticScalarMethod3WithObjectIdAndPathAndMethodIdAndPathTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeMethodCallBoiler2ResetTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeMethodCallBoiler2ResetTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeMethodCallBoiler1ResetTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeMethodCallBoiler1ResetTest();
                });
            }
        }
    }
}
