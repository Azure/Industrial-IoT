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
        public async Task NodeWriteStaticScalarBooleanValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticScalarBooleanValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticScalarBooleanValueVariableWithBrowsePathTest1Async() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticScalarBooleanValueVariableWithBrowsePathTest1Async();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticScalarBooleanValueVariableWithBrowsePathTest2Async() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticScalarBooleanValueVariableWithBrowsePathTest2Async();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticScalarBooleanValueVariableWithBrowsePathTest3Async() {
            // Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticScalarBooleanValueVariableWithBrowsePathTest3Async();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticScalarSByteValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticScalarSByteValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticScalarByteValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticScalarByteValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticScalarInt16ValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticScalarInt16ValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticScalarUInt16ValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticScalarUInt16ValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticScalarInt32ValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticScalarInt32ValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticScalarUInt32ValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticScalarUInt32ValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticScalarInt64ValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticScalarInt64ValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticScalarUInt64ValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticScalarUInt64ValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticScalarFloatValueVariableTestAsync() {
            // Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticScalarFloatValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticScalarDoubleValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticScalarDoubleValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticScalarStringValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticScalarStringValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticScalarDateTimeValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticScalarDateTimeValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticScalarGuidValueVariableTestAsync() {
            // Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticScalarGuidValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticScalarByteStringValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticScalarByteStringValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticScalarXmlElementValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticScalarXmlElementValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticScalarNodeIdValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticScalarNodeIdValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticScalarExpandedNodeIdValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticScalarExpandedNodeIdValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticScalarQualifiedNameValueVariableTestAsync() {
            // Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticScalarQualifiedNameValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticScalarLocalizedTextValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticScalarLocalizedTextValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticScalarStatusCodeValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticScalarStatusCodeValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticScalarVariantValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticScalarVariantValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticScalarEnumerationValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticScalarEnumerationValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticScalarStructuredValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticScalarStructuredValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticScalarNumberValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticScalarNumberValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticScalarIntegerValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticScalarIntegerValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticScalarUIntegerValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(async (device, module, services) => {
                    await GetTests(device, module, services).NodeWriteStaticScalarUIntegerValueVariableTestAsync();
                });
            }
        }
    }
}
