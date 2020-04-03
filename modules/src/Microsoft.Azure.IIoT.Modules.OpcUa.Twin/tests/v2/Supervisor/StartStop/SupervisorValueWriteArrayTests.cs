// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Twin.v2.Supervisor.StartStop {
    using Microsoft.Azure.IIoT.Modules.OpcUa.Twin.Tests;
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
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

    [Collection(WriteCollection.Name)]
    public class SupervisorValueWriteArrayTests {

        public SupervisorValueWriteArrayTests(TestServerFixture server) {
            _server = server;
        }

        private WriteArrayValueTests<EndpointRegistrationModel> GetTests(
            string deviceId, string moduleId, IContainer services) {
            return new WriteArrayValueTests<EndpointRegistrationModel>(
                () => services.Resolve<INodeServices<EndpointRegistrationModel>>(),
                new EndpointRegistrationModel {
                    Endpoint = new EndpointModel {
                        Url = $"opc.tcp://{Dns.GetHostName()}:{_server.Port}/UA/SampleServer",
                        Certificate = _server.Certificate?.RawData?.ToThumbprint()
                    },
                    Id = "testid",
                    SupervisorId = SupervisorModelEx.CreateSupervisorId(deviceId, moduleId)
                }, (ep, n) => _server.Client.ReadValueAsync(ep.Endpoint, n));
        }

        private readonly TestServerFixture _server;
#if DEBUG
        private readonly bool _runAll = true;
#else
        private readonly bool _runAll = Environment.GetEnvironmentVariable("TEST_ALL") != null;
#endif

        [SkippableFact]
        public async Task NodeWriteStaticArrayBooleanValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticArrayBooleanValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticArraySByteValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticArraySByteValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticArrayByteValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticArrayByteValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticArrayInt16ValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticArrayInt16ValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticArrayUInt16ValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticArrayUInt16ValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticArrayInt32ValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticArrayInt32ValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticArrayUInt32ValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticArrayUInt32ValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticArrayInt64ValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticArrayInt64ValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticArrayUInt64ValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticArrayUInt64ValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticArrayFloatValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticArrayFloatValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticArrayDoubleValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticArrayDoubleValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticArrayStringValueVariableTest1Async() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticArrayStringValueVariableTest1Async();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticArrayStringValueVariableTest2Async() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticArrayStringValueVariableTest2Async();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticArrayDateTimeValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticArrayDateTimeValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticArrayGuidValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticArrayGuidValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticArrayByteStringValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticArrayByteStringValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticArrayXmlElementValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticArrayXmlElementValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticArrayNodeIdValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticArrayNodeIdValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticArrayExpandedNodeIdValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticArrayExpandedNodeIdValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticArrayQualifiedNameValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticArrayQualifiedNameValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticArrayLocalizedTextValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticArrayLocalizedTextValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticArrayStatusCodeValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticArrayStatusCodeValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticArrayVariantValueVariableTest1Async() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticArrayVariantValueVariableTest1Async();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticArrayEnumerationValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticArrayEnumerationValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticArrayStructureValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticArrayStructureValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticArrayNumberValueVariableTest1Async() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticArrayNumberValueVariableTest1Async();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticArrayNumberValueVariableTest2Async() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticArrayNumberValueVariableTest2Async();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticArrayIntegerValueVariableTest1Async() {
            // Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticArrayIntegerValueVariableTest1Async();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticArrayIntegerValueVariableTest2Async() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticArrayIntegerValueVariableTest2Async();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticArrayUIntegerValueVariableTest1Async() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticArrayUIntegerValueVariableTest1Async();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticArrayUIntegerValueVariableTest2Async() {
            // Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticArrayUIntegerValueVariableTest2Async();
                });
            }
        }
    }
}
