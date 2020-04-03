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
    public class TwinValueReadScalarTests {

        public TwinValueReadScalarTests(TestServerFixture server) {
            _server = server;
        }


        private EndpointModel Endpoint => new EndpointModel {
            Url = $"opc.tcp://{Dns.GetHostName()}:{_server.Port}/UA/SampleServer",
            Certificate = _server.Certificate?.RawData?.ToThumbprint()
        };

        private ReadScalarValueTests<string> GetTests(EndpointRegistrationModel endpoint, IContainer services) {
            return new ReadScalarValueTests<string>(
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
        public async Task NodeReadAllStaticScalarVariableNodeClassTest1Async() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadAllStaticScalarVariableNodeClassTest1Async();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadAllStaticScalarVariableAccessLevelTest1Async() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadAllStaticScalarVariableAccessLevelTest1Async();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadAllStaticScalarVariableWriteMaskTest1Async() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadAllStaticScalarVariableWriteMaskTest1Async();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadAllStaticScalarVariableWriteMaskTest2Async() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadAllStaticScalarVariableWriteMaskTest2Async();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticScalarBooleanValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadStaticScalarBooleanValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticScalarBooleanValueVariableWithBrowsePathTest1Async() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadStaticScalarBooleanValueVariableWithBrowsePathTest1Async();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticScalarBooleanValueVariableWithBrowsePathTest2Async() {
            // Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadStaticScalarBooleanValueVariableWithBrowsePathTest2Async();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticScalarBooleanValueVariableWithBrowsePathTest3Async() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadStaticScalarBooleanValueVariableWithBrowsePathTest3Async();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticScalarSByteValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadStaticScalarSByteValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticScalarByteValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadStaticScalarByteValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticScalarInt16ValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadStaticScalarInt16ValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticScalarUInt16ValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadStaticScalarUInt16ValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticScalarInt32ValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadStaticScalarInt32ValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticScalarUInt32ValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadStaticScalarUInt32ValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticScalarInt64ValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadStaticScalarInt64ValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticScalarUInt64ValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadStaticScalarUInt64ValueVariableTestAsync();
                });
            }
        }


        [SkippableFact]
        public async Task NodeReadStaticScalarFloatValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadStaticScalarFloatValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticScalarDoubleValueVariableTestAsync() {
            // Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadStaticScalarDoubleValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticScalarStringValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadStaticScalarStringValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticScalarDateTimeValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadStaticScalarDateTimeValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticScalarGuidValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadStaticScalarGuidValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticScalarByteStringValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadStaticScalarByteStringValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticScalarXmlElementValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadStaticScalarXmlElementValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticScalarNodeIdValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadStaticScalarNodeIdValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticScalarExpandedNodeIdValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadStaticScalarExpandedNodeIdValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticScalarQualifiedNameValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadStaticScalarQualifiedNameValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticScalarLocalizedTextValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadStaticScalarLocalizedTextValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticScalarStatusCodeValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadStaticScalarStatusCodeValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticScalarVariantValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadStaticScalarVariantValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticScalarEnumerationValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadStaticScalarEnumerationValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticScalarStructuredValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadStaticScalarStructuredValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticScalarNumberValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadStaticScalarNumberValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticScalarIntegerValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadStaticScalarIntegerValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadStaticScalarUIntegerValueVariableTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadStaticScalarUIntegerValueVariableTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadDataAccessMeasurementFloatValueTestAsync() {
            // Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadDataAccessMeasurementFloatValueTestAsync();
                });
            }
        }


        [SkippableFact]
        public async Task NodeReadDiagnosticsNoneTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadDiagnosticsNoneTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadDiagnosticsStatusTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadDiagnosticsStatusTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadDiagnosticsOperationsTestAsync() {
            // Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadDiagnosticsOperationsTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeReadDiagnosticsVerboseTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeReadDiagnosticsVerboseTestAsync();
                });
            }
        }
    }
}
