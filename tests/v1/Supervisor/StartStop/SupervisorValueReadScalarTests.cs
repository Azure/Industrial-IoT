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
    public class SupervisorValueReadScalarTests {

        public SupervisorValueReadScalarTests(TestServerFixture server) {
            _server = server;
        }

        private ReadScalarValueTests<EndpointRegistrationModel> GetTests(
            string deviceId, string moduleId, IContainer services) {
            return new ReadScalarValueTests<EndpointRegistrationModel>(
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
        public async Task NodeReadAllStaticScalarVariableNodeClassTest1() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeReadAllStaticScalarVariableNodeClassTest1();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadAllStaticScalarVariableAccessLevelTest1() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeReadAllStaticScalarVariableAccessLevelTest1();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadAllStaticScalarVariableWriteMaskTest1() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeReadAllStaticScalarVariableWriteMaskTest1();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadAllStaticScalarVariableWriteMaskTest2() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeReadAllStaticScalarVariableWriteMaskTest2();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticScalarBooleanValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeReadStaticScalarBooleanValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticScalarBooleanValueVariableWithBrowsePathTest1() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeReadStaticScalarBooleanValueVariableWithBrowsePathTest1();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticScalarBooleanValueVariableWithBrowsePathTest2() {
            // Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeReadStaticScalarBooleanValueVariableWithBrowsePathTest2();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticScalarBooleanValueVariableWithBrowsePathTest3() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeReadStaticScalarBooleanValueVariableWithBrowsePathTest3();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticScalarSByteValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeReadStaticScalarSByteValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticScalarByteValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeReadStaticScalarByteValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticScalarInt16ValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeReadStaticScalarInt16ValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticScalarUInt16ValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeReadStaticScalarUInt16ValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticScalarInt32ValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeReadStaticScalarInt32ValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticScalarUInt32ValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeReadStaticScalarUInt32ValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticScalarInt64ValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeReadStaticScalarInt64ValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticScalarUInt64ValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeReadStaticScalarUInt64ValueVariableTest();
                });
            }
        }


        [SkippableFact]
        public async Task NodeReadStaticScalarFloatValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeReadStaticScalarFloatValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticScalarDoubleValueVariableTest() {
            // Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeReadStaticScalarDoubleValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticScalarStringValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeReadStaticScalarStringValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticScalarDateTimeValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeReadStaticScalarDateTimeValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticScalarGuidValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeReadStaticScalarGuidValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticScalarByteStringValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeReadStaticScalarByteStringValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticScalarXmlElementValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeReadStaticScalarXmlElementValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticScalarNodeIdValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeReadStaticScalarNodeIdValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticScalarExpandedNodeIdValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeReadStaticScalarExpandedNodeIdValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticScalarQualifiedNameValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeReadStaticScalarQualifiedNameValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticScalarLocalizedTextValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeReadStaticScalarLocalizedTextValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticScalarStatusCodeValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeReadStaticScalarStatusCodeValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticScalarVariantValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeReadStaticScalarVariantValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticScalarEnumerationValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeReadStaticScalarEnumerationValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticScalarStructuredValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeReadStaticScalarStructuredValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticScalarNumberValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeReadStaticScalarNumberValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticScalarIntegerValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeReadStaticScalarIntegerValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticScalarUIntegerValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeReadStaticScalarUIntegerValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadDiagnosticsNoneTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeReadDiagnosticsNoneTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadDiagnosticsStatusTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeReadDiagnosticsStatusTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadDiagnosticsOperationsTest() {
            // Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeReadDiagnosticsOperationsTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadDiagnosticsVerboseTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeReadDiagnosticsVerboseTest();
                });
            }
        }
    }
}
