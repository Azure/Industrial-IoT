// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Twin.v2.Twin.StartStop {
    using Microsoft.Azure.IIoT.Modules.OpcUa.Twin.Tests;
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
    public class TwinBrowseTests {

        public TwinBrowseTests(TestServerFixture server) {
            _server = server;
        }

        private EndpointModel Endpoint => new EndpointModel {
            Url = $"opc.tcp://{Dns.GetHostName()}:{_server.Port}/UA/SampleServer",
            Certificate = _server.Certificate?.RawData
        };

        private BrowseServicesTests<string> GetTests(EndpointRegistrationModel endpoint,
            IContainer services) {
            return new BrowseServicesTests<string>(
                () => services.Resolve<IBrowseServices<string>>(), endpoint.Id);
        }

        private readonly TestServerFixture _server;
        private readonly bool _runAll = Environment.GetEnvironmentVariable("TEST_ALL") != null;

        [SkippableFact]
        public async Task NodeBrowseInRootTest1Async() {
            // Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeBrowseInRootTest1Async();
                });
            }
        }

        [SkippableFact]
        public async Task NodeBrowseInRootTest2Async() {
            // Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeBrowseInRootTest2Async();
                });
            }
        }

        [SkippableFact]
        public async Task NodeBrowseFirstInRootTest1Async() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeBrowseFirstInRootTest1Async();
                });
            }
        }

        [SkippableFact]
        public async Task NodeBrowseFirstInRootTest2Async() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeBrowseFirstInRootTest2Async();
                });
            }
        }

        [SkippableFact]
        public async Task NodeBrowseBoilersObjectsTest1Async() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeBrowseBoilersObjectsTest1Async();
                });
            }
        }

        [SkippableFact]
        public async Task NodeBrowseBoilersObjectsTest2Async() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeBrowseBoilersObjectsTest2Async();
                });
            }
        }

        [SkippableFact]
        public async Task NodeBrowseDataAccessObjectsTest1Async() {
            // Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeBrowseDataAccessObjectsTest1Async();
                });
            }
        }

        [SkippableFact]
        public async Task NodeBrowseDataAccessObjectsTest2Async() {
            // Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeBrowseDataAccessObjectsTest2Async();
                });
            }
        }

        [SkippableFact]
        public async Task NodeBrowseDataAccessObjectsTest3Async() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeBrowseDataAccessObjectsTest3Async();
                });
            }
        }

        [SkippableFact]
        public async Task NodeBrowseDataAccessObjectsTest4Async() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeBrowseDataAccessObjectsTest4Async();
                });
            }
        }

        [SkippableFact]
        public async Task NodeBrowseDataAccessFC1001Test1Async() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeBrowseDataAccessFC1001Test1Async();
                });
            }
        }

        [SkippableFact]
        public async Task NodeBrowseDataAccessFC1001Test2Async() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeBrowseDataAccessFC1001Test2Async();
                });
            }
        }

        [SkippableFact]
        public async Task NodeBrowseStaticScalarVariablesTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeBrowseStaticScalarVariablesTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeBrowseStaticArrayVariablesTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeBrowseStaticArrayVariablesTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeBrowseStaticArrayVariablesWithValuesTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeBrowseStaticArrayVariablesWithValuesTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeBrowseStaticArrayVariablesRawModeTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeBrowseStaticArrayVariablesRawModeTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeBrowsePathStaticScalarMethod3Test1Async() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeBrowsePathStaticScalarMethod3Test1Async();
                });
            }
        }

        [SkippableFact]
        public async Task NodeBrowsePathStaticScalarMethod3Test2Async() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeBrowsePathStaticScalarMethod3Test2Async();
                });
            }
        }

        [SkippableFact]
        public async Task NodeBrowsePathStaticScalarMethod3Test3Async() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeBrowsePathStaticScalarMethod3Test3Async();
                });
            }
        }

        [SkippableFact]
        public async Task NodeBrowsePathStaticScalarMethodsTestAsync() {
            // Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeBrowsePathStaticScalarMethodsTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeBrowseDiagnosticsNoneTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeBrowseDiagnosticsNoneTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeBrowseDiagnosticsStatusTestAsync() {
            // Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeBrowseDiagnosticsStatusTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeBrowseDiagnosticsOperationsTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeBrowseDiagnosticsOperationsTestAsync();
                });
            }
        }

        [SkippableFact]
        public async Task NodeBrowseDiagnosticsVerboseTestAsync() {
            Skip.IfNot(_runAll);
            using (var harness = new TwinModuleFixture()) {
                await harness.RunTestAsync(Endpoint, async (endpoint, services) => {
                    await GetTests(endpoint, services).NodeBrowseDiagnosticsVerboseTestAsync();
                });
            }
        }
    }
}
