// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Twin.v2.Twin.StartStop {
    using Microsoft.Azure.IIoT.Modules.OpcUa.Twin.Tests;
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
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
            Certificate = _server.Certificate?.RawData?.ToThumbprint()
        };

        private ReadArrayValueTests<string> GetTests(EndpointRegistrationModel endpoint, IContainer services) {
            return new ReadArrayValueTests<string>(
                () => services.Resolve<INodeServices<string>>(), endpoint.Id,
                (ep, n) => _server.Client.ReadValueAsync(endpoint.Endpoint, n));
        }

        private readonly TestServerFixture _server;
#if DEBUG
        private readonly bool _runAll = true;
#else
        private readonly bool _runAll = Environment.GetEnvironmentVariable("TEST_ALL") != null;
#endif

        [SkippableFact]
        public async Task NodeReadAllStaticArrayVariableNodeClassTest1Async() {
            // Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadAllStaticArrayVariableNodeClassTest1Async();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadAllStaticArrayVariableAccessLevelTest1Async() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadAllStaticArrayVariableAccessLevelTest1Async();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadAllStaticArrayVariableWriteMaskTest1Async() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadAllStaticArrayVariableWriteMaskTest1Async();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadAllStaticArrayVariableWriteMaskTest2Async() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadAllStaticArrayVariableWriteMaskTest2Async();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticArrayBooleanValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadStaticArrayBooleanValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticArraySByteValueVariableTestAsync() {
            // Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadStaticArraySByteValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticArrayByteValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadStaticArrayByteValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticArrayInt16ValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadStaticArrayInt16ValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticArrayUInt16ValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadStaticArrayUInt16ValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticArrayInt32ValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadStaticArrayInt32ValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticArrayUInt32ValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadStaticArrayUInt32ValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticArrayInt64ValueVariableTestAsync() {
            // Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadStaticArrayInt64ValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticArrayUInt64ValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadStaticArrayUInt64ValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticArrayFloatValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadStaticArrayFloatValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticArrayDoubleValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadStaticArrayDoubleValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticArrayStringValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadStaticArrayStringValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticArrayDateTimeValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadStaticArrayDateTimeValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticArrayGuidValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadStaticArrayGuidValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticArrayByteStringValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadStaticArrayByteStringValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticArrayXmlElementValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadStaticArrayXmlElementValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticArrayNodeIdValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadStaticArrayNodeIdValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticArrayExpandedNodeIdValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadStaticArrayExpandedNodeIdValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticArrayQualifiedNameValueVariableTestAsync() {
            // Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadStaticArrayQualifiedNameValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticArrayLocalizedTextValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadStaticArrayLocalizedTextValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticArrayStatusCodeValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadStaticArrayStatusCodeValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticArrayVariantValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadStaticArrayVariantValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticArrayEnumerationValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadStaticArrayEnumerationValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticArrayStructureValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadStaticArrayStructureValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticArrayNumberValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadStaticArrayNumberValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticArrayIntegerValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadStaticArrayIntegerValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticArrayUIntegerValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadStaticArrayUIntegerValueVariableTestAsync();
                });
            }
        }
    }
}
