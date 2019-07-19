// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Twin.v2.Supervisor.StartStop {
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

    [Collection(WriteCollection.Name)]
    public class SupervisorValueWriteScalarTests {

        public SupervisorValueWriteScalarTests(TestServerFixture server) {
            _server = server;
        }

        private WriteScalarValueTests<EndpointRegistrationModel> GetTests(
            string deviceId, string moduleId, IContainer services) {
            return new WriteScalarValueTests<EndpointRegistrationModel>(
                () => services.Resolve<INodeServices<EndpointRegistrationModel>>(),
                new EndpointRegistrationModel {
                    Endpoint = new EndpointModel {
                        Url = $"opc.tcp://{Dns.GetHostName()}:{_server.Port}/UA/SampleServer",
                        Certificate = _server.Certificate?.RawData
                    },
                    Id = "testid",
                    SupervisorId = SupervisorModelEx.CreateSupervisorId(deviceId, moduleId)
                }, (ep, n) => _server.Client.ReadValueAsync(ep.Endpoint, n));
        }

        private readonly TestServerFixture _server;
        private static readonly bool _runAll = Environment.GetEnvironmentVariable("TEST_ALL") != null;

        [SkippableFact]
        public async Task NodeWriteStaticScalarBooleanValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticScalarBooleanValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticScalarBooleanValueVariableWithBrowsePathTest1() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticScalarBooleanValueVariableWithBrowsePathTest1();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticScalarBooleanValueVariableWithBrowsePathTest2() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticScalarBooleanValueVariableWithBrowsePathTest2();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticScalarBooleanValueVariableWithBrowsePathTest3() {
            // Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticScalarBooleanValueVariableWithBrowsePathTest3();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticScalarSByteValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticScalarSByteValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticScalarByteValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticScalarByteValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticScalarInt16ValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticScalarInt16ValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticScalarUInt16ValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticScalarUInt16ValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticScalarInt32ValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticScalarInt32ValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticScalarUInt32ValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticScalarUInt32ValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticScalarInt64ValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticScalarInt64ValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticScalarUInt64ValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticScalarUInt64ValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticScalarFloatValueVariableTest() {
            // Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticScalarFloatValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticScalarDoubleValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticScalarDoubleValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticScalarStringValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticScalarStringValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticScalarDateTimeValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticScalarDateTimeValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticScalarGuidValueVariableTest() {
            // Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticScalarGuidValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticScalarByteStringValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticScalarByteStringValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticScalarXmlElementValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticScalarXmlElementValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticScalarNodeIdValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticScalarNodeIdValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticScalarExpandedNodeIdValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticScalarExpandedNodeIdValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticScalarQualifiedNameValueVariableTest() {
            // Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticScalarQualifiedNameValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticScalarLocalizedTextValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticScalarLocalizedTextValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticScalarStatusCodeValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticScalarStatusCodeValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticScalarVariantValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticScalarVariantValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticScalarEnumerationValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticScalarEnumerationValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticScalarStructuredValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticScalarStructuredValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticScalarNumberValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticScalarNumberValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticScalarIntegerValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticScalarIntegerValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticScalarUIntegerValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticScalarUIntegerValueVariableTest();
                });
            }
        }
    }
}
