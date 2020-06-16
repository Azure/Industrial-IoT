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
    using Microsoft.Azure.IIoT.Utils;
    using Opc.Ua;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading.Tasks;
    using Xunit;
    using Autofac;

    [Collection(WriteCollection.Name)]
    public class SupervisorValueCallScalarTests {


        public SupervisorValueCallScalarTests(TestServerFixture server) {
            _server = server;
            _hostEntry = Try.Op(() => Dns.GetHostEntry(Utils.GetHostName()))
                ?? Try.Op(() => Dns.GetHostEntry("localhost"));
        }

        private CallScalarMethodTests<EndpointRegistrationModel> GetTests(
            string deviceId, string moduleId, IContainer services) {
            return new CallScalarMethodTests<EndpointRegistrationModel>(
                () => services.Resolve<INodeServices<EndpointRegistrationModel>>(),
                new EndpointRegistrationModel {
                    Endpoint = new EndpointModel {
                        Url = $"opc.tcp://{_hostEntry?.HostName ?? "localhost"}:{_server.Port}/UA/SampleServer",
                        AlternativeUrls = _hostEntry?.AddressList
                            .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork)
                            .Select(ip => $"opc.tcp://{ip}:{_server.Port}/UA/SampleServer").ToHashSet(),
                        Certificate = _server.Certificate?.RawData?.ToThumbprint()
                    },
                    Id = "testid",
                    SupervisorId = SupervisorModelEx.CreateSupervisorId(deviceId, moduleId)
                });
        }

        private readonly TestServerFixture _server;
        private readonly IPHostEntry _hostEntry;
#if TEST_ALL
        private readonly bool _runAll = true;
#else
        private readonly bool _runAll = System.Environment.GetEnvironmentVariable("TEST_ALL") != null;
#endif

        [SkippableFact]
        public async Task NodeMethodMetadataStaticScalarMethod1TestAsync() {
            // Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeMethodMetadataStaticScalarMethod1TestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeMethodMetadataStaticScalarMethod2TestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeMethodMetadataStaticScalarMethod2TestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeMethodMetadataStaticScalarMethod3TestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeMethodMetadataStaticScalarMethod3TestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeMethodMetadataStaticScalarMethod3WithBrowsePathTest1Async() {
            // Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeMethodMetadataStaticScalarMethod3WithBrowsePathTest1Async();
                });
            }
        }

        [SkippableFact]
        public async Task NodeMethodMetadataStaticScalarMethod3WithBrowsePathTest2Async() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeMethodMetadataStaticScalarMethod3WithBrowsePathTest2Async();
                });
            }
        }

        [SkippableFact]
        public async Task NodeMethodCallStaticScalarMethod1Test1Async() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeMethodCallStaticScalarMethod1Test1Async();
                });
            }
        }

        [SkippableFact]
        public async Task NodeMethodCallStaticScalarMethod1Test2Async() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeMethodCallStaticScalarMethod1Test2Async();
                });
            }
        }

        [SkippableFact]
        public async Task NodeMethodCallStaticScalarMethod1Test3Async() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeMethodCallStaticScalarMethod1Test3Async();
                });
            }
        }

        [SkippableFact]
        public async Task NodeMethodCallStaticScalarMethod1Test4Async() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeMethodCallStaticScalarMethod1Test4Async();
                });
            }
        }

        [SkippableFact]
        public async Task NodeMethodCallStaticScalarMethod1Test5Async() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeMethodCallStaticScalarMethod1Test5Async();
                });
            }
        }

        [SkippableFact]
        public async Task NodeMethodCallStaticScalarMethod2Test1Async() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeMethodCallStaticScalarMethod2Test1Async();
                });
            }
        }

        [SkippableFact]
        public async Task NodeMethodCallStaticScalarMethod2Test2Async() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeMethodCallStaticScalarMethod2Test2Async();
                });
            }
        }

        [SkippableFact]
        public async Task NodeMethodCallStaticScalarMethod3Test1Async() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeMethodCallStaticScalarMethod3Test1Async();
                });
            }
        }

        [SkippableFact]
        public async Task NodeMethodCallStaticScalarMethod3Test2Async() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeMethodCallStaticScalarMethod3Test2Async();
                });
            }
        }

        [SkippableFact]
        public async Task NodeMethodCallStaticScalarMethod3WithBrowsePathNoIdsTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeMethodCallStaticScalarMethod3WithBrowsePathNoIdsTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeMethodCallStaticScalarMethod3WithObjectIdAndBrowsePathTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeMethodCallStaticScalarMethod3WithObjectIdAndBrowsePathTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeMethodCallStaticScalarMethod3WithObjectIdAndMethodIdAndBrowsePathTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeMethodCallStaticScalarMethod3WithObjectIdAndMethodIdAndBrowsePathTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeMethodCallStaticScalarMethod3WithObjectPathAndMethodIdAndBrowsePathTestAsync() {
            // Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeMethodCallStaticScalarMethod3WithObjectPathAndMethodIdAndBrowsePathTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeMethodCallStaticScalarMethod3WithObjectIdAndPathAndMethodIdAndPathTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeMethodCallStaticScalarMethod3WithObjectIdAndPathAndMethodIdAndPathTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeMethodCallBoiler2ResetTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeMethodCallBoiler2ResetTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeMethodCallBoiler1ResetTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeMethodCallBoiler1ResetTestAsync();
                });
            }
        }
    }
}
