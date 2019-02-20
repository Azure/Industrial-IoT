// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Twin.v1.Supervisor.StartStop {
    using Microsoft.Azure.IIoT.Modules.OpcUa.Twin.Tests;
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
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
    public class SupervisorValueReadArrayTests {

        public SupervisorValueReadArrayTests(TestServerFixture server) {
            _server = server;
        }

        private ReadArrayValueTests<EndpointRegistrationModel> GetTests(
            string deviceId, string moduleId, IContainer services) {
            return new ReadArrayValueTests<EndpointRegistrationModel>(
                () => services.Resolve<INodeServices<EndpointRegistrationModel>>(),
                new EndpointRegistrationModel {
                    Endpoint = new EndpointModel {
                        Url = $"opc.tcp://{Dns.GetHostName()}:{_server.Port}/UA/SampleServer"
                    },
                    Id = "testid",
                    SupervisorId = SupervisorModelEx.CreateSupervisorId(deviceId, moduleId)
                }, (ep, n) => _server.Client.ReadValueAsync(ep.Endpoint, n));
        }

        private readonly TestServerFixture _server;
        private static readonly bool _runAll = Environment.GetEnvironmentVariable("TEST_ALL") != null;

        [SkippableFact]
        public async Task NodeReadAllStaticArrayVariableNodeClassTest1() {
            // Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeReadAllStaticArrayVariableNodeClassTest1();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadAllStaticArrayVariableAccessLevelTest1() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeReadAllStaticArrayVariableAccessLevelTest1();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadAllStaticArrayVariableWriteMaskTest1() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeReadAllStaticArrayVariableWriteMaskTest1();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadAllStaticArrayVariableWriteMaskTest2() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeReadAllStaticArrayVariableWriteMaskTest2();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticArrayBooleanValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeReadStaticArrayBooleanValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticArraySByteValueVariableTest() {
            // Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
            await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeReadStaticArraySByteValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticArrayByteValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeReadStaticArrayByteValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticArrayInt16ValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeReadStaticArrayInt16ValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticArrayUInt16ValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeReadStaticArrayUInt16ValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticArrayInt32ValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeReadStaticArrayInt32ValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticArrayUInt32ValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeReadStaticArrayUInt32ValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticArrayInt64ValueVariableTest() {
            // Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeReadStaticArrayInt64ValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticArrayUInt64ValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeReadStaticArrayUInt64ValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticArrayFloatValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeReadStaticArrayFloatValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticArrayDoubleValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeReadStaticArrayDoubleValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticArrayStringValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeReadStaticArrayStringValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticArrayDateTimeValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeReadStaticArrayDateTimeValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticArrayGuidValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeReadStaticArrayGuidValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticArrayByteStringValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeReadStaticArrayByteStringValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticArrayXmlElementValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeReadStaticArrayXmlElementValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticArrayNodeIdValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeReadStaticArrayNodeIdValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticArrayExpandedNodeIdValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeReadStaticArrayExpandedNodeIdValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticArrayQualifiedNameValueVariableTest() {
            // Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeReadStaticArrayQualifiedNameValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticArrayLocalizedTextValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeReadStaticArrayLocalizedTextValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticArrayStatusCodeValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeReadStaticArrayStatusCodeValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticArrayVariantValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeReadStaticArrayVariantValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticArrayEnumerationValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeReadStaticArrayEnumerationValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticArrayStructureValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeReadStaticArrayStructureValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticArrayNumberValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeReadStaticArrayNumberValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticArrayIntegerValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeReadStaticArrayIntegerValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticArrayUIntegerValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeReadStaticArrayUIntegerValueVariableTest();
                });
            }
        }
    }
}
