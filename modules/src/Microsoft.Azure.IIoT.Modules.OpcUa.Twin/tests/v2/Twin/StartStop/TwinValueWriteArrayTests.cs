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
    using Microsoft.Azure.IIoT.Utils;
    using Opc.Ua;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading.Tasks;
    using Xunit;
    using Autofac;

    [Collection(WriteCollection.Name)]
    public class TwinValueWriteArrayTests {

        public TwinValueWriteArrayTests(TestServerFixture server) {
            _server = server;
            _hostEntry = Try.Op(() => Dns.GetHostEntry(Utils.GetHostName()))
                ?? Try.Op(() => Dns.GetHostEntry("localhost"));
        }

        private EndpointModel Endpoint => new EndpointModel {
            Url = $"opc.tcp://{_hostEntry?.HostName ?? "localhost"}:{_server.Port}/UA/SampleServer",
            AlternativeUrls = _hostEntry?.AddressList
                .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork)
                .Select(ip => $"opc.tcp://{ip}:{_server.Port}/UA/SampleServer").ToHashSet(),
            Certificate = _server.Certificate?.RawData?.ToThumbprint()
        };

        private WriteArrayValueTests<string> GetTests(EndpointRegistrationModel endpoint, IContainer services) {
            return new WriteArrayValueTests<string>(
                () => services.Resolve<INodeServices<string>>(), endpoint.Id,
                (ep, n) => _server.Client.ReadValueAsync(endpoint.Endpoint, n));
        }

        private readonly TestServerFixture _server;
        private readonly IPHostEntry _hostEntry;
#if TEST_ALL
        private readonly bool _runAll = true;
#else
        private readonly bool _runAll = System.Environment.GetEnvironmentVariable("TEST_ALL") != null;
#endif

        [SkippableFact]
        public async Task NodeWriteStaticArrayBooleanValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeWriteStaticArrayBooleanValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticArraySByteValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeWriteStaticArraySByteValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticArrayByteValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeWriteStaticArrayByteValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticArrayInt16ValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeWriteStaticArrayInt16ValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticArrayUInt16ValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeWriteStaticArrayUInt16ValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticArrayInt32ValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeWriteStaticArrayInt32ValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticArrayUInt32ValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeWriteStaticArrayUInt32ValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticArrayInt64ValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeWriteStaticArrayInt64ValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticArrayUInt64ValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeWriteStaticArrayUInt64ValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticArrayFloatValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeWriteStaticArrayFloatValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticArrayDoubleValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeWriteStaticArrayDoubleValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticArrayStringValueVariableTest1Async() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeWriteStaticArrayStringValueVariableTest1Async();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticArrayStringValueVariableTest2Async() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeWriteStaticArrayStringValueVariableTest2Async();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticArrayDateTimeValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeWriteStaticArrayDateTimeValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticArrayGuidValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeWriteStaticArrayGuidValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticArrayByteStringValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeWriteStaticArrayByteStringValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticArrayXmlElementValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeWriteStaticArrayXmlElementValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticArrayNodeIdValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeWriteStaticArrayNodeIdValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticArrayExpandedNodeIdValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeWriteStaticArrayExpandedNodeIdValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticArrayQualifiedNameValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeWriteStaticArrayQualifiedNameValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticArrayLocalizedTextValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeWriteStaticArrayLocalizedTextValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticArrayStatusCodeValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeWriteStaticArrayStatusCodeValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticArrayVariantValueVariableTest1Async() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeWriteStaticArrayVariantValueVariableTest1Async();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticArrayEnumerationValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeWriteStaticArrayEnumerationValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticArrayStructureValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeWriteStaticArrayStructureValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticArrayNumberValueVariableTest1Async() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeWriteStaticArrayNumberValueVariableTest1Async();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticArrayNumberValueVariableTest2Async() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeWriteStaticArrayNumberValueVariableTest2Async();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticArrayIntegerValueVariableTest1Async() {
            // Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeWriteStaticArrayIntegerValueVariableTest1Async();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticArrayIntegerValueVariableTest2Async() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeWriteStaticArrayIntegerValueVariableTest2Async();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticArrayUIntegerValueVariableTest1Async() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeWriteStaticArrayUIntegerValueVariableTest1Async();
                });
            }
        }

        [SkippableFact]
        public async Task NodeWriteStaticArrayUIntegerValueVariableTest2Async() {
            // Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeWriteStaticArrayUIntegerValueVariableTest2Async();
                });
            }
        }
    }
}
