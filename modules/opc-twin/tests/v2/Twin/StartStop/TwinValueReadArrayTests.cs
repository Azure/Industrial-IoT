// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Twin.v2.Twin.StartStop {
    using Microsoft.Azure.IIoT.Modules.OpcUa.Twin.Tests;
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Testing.Fixtures;
    using Microsoft.Azure.IIoT.OpcUa.Testing.Tests;
    using Microsoft.Azure.IIoT.OpcUa.Twin;
    using System;
    using System.Net;
    using System.Threading.Tasks;
    using Xunit;
    using Autofac;

    [Collection(ReadCollection.Name)]
    public class TwinValueReadArrayTests {

        public TwinValueReadArrayTests(TestServerFixture server) {
            _server = server;
        }

        private EndpointModel Endpoint => new EndpointModel {
            Url = $"opc.tcp://{Dns.GetHostName()}:{_server.Port}/UA/SampleServer",
            Certificate = _server.Certificate?.RawData
        };

        private ReadArrayValueTests<string> GetTests(EndpointRegistrationModel endpoint, IContainer services) {
            return new ReadArrayValueTests<string>(
                () => services.Resolve<INodeServices<string>>(), endpoint.Id,
                (ep, n) => _server.Client.ReadValueAsync(endpoint.Endpoint, n));
        }

        private readonly TestServerFixture _server;
        private static readonly bool _runAll = Environment.GetEnvironmentVariable("TEST_ALL") != null;

        [SkippableFact]
        public async Task NodeReadAllStaticArrayVariableNodeClassTest1() {
            // Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadAllStaticArrayVariableNodeClassTest1();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadAllStaticArrayVariableAccessLevelTest1() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadAllStaticArrayVariableAccessLevelTest1();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadAllStaticArrayVariableWriteMaskTest1() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadAllStaticArrayVariableWriteMaskTest1();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadAllStaticArrayVariableWriteMaskTest2() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadAllStaticArrayVariableWriteMaskTest2();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticArrayBooleanValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadStaticArrayBooleanValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticArraySByteValueVariableTest() {
            // Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadStaticArraySByteValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticArrayByteValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadStaticArrayByteValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticArrayInt16ValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadStaticArrayInt16ValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticArrayUInt16ValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadStaticArrayUInt16ValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticArrayInt32ValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadStaticArrayInt32ValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticArrayUInt32ValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadStaticArrayUInt32ValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticArrayInt64ValueVariableTest() {
            // Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadStaticArrayInt64ValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticArrayUInt64ValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadStaticArrayUInt64ValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticArrayFloatValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadStaticArrayFloatValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticArrayDoubleValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadStaticArrayDoubleValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticArrayStringValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadStaticArrayStringValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticArrayDateTimeValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadStaticArrayDateTimeValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticArrayGuidValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadStaticArrayGuidValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticArrayByteStringValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadStaticArrayByteStringValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticArrayXmlElementValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadStaticArrayXmlElementValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticArrayNodeIdValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadStaticArrayNodeIdValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticArrayExpandedNodeIdValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadStaticArrayExpandedNodeIdValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticArrayQualifiedNameValueVariableTest() {
            // Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadStaticArrayQualifiedNameValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticArrayLocalizedTextValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadStaticArrayLocalizedTextValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticArrayStatusCodeValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadStaticArrayStatusCodeValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticArrayVariantValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadStaticArrayVariantValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticArrayEnumerationValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadStaticArrayEnumerationValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticArrayStructureValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadStaticArrayStructureValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticArrayNumberValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadStaticArrayNumberValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticArrayIntegerValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadStaticArrayIntegerValueVariableTest();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticArrayUIntegerValueVariableTest() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadStaticArrayUIntegerValueVariableTest();
                });
            }
        }
    }
}
