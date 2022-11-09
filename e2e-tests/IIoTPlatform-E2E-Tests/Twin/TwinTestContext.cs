// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatform_E2E_Tests.Twin {
    using IIoTPlatform_E2E_Tests.TestExtensions;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using Xunit;

    public class TwinTestContext : IIoTPlatformTestContext {

        /// <summary>
        /// Initializes a new instance of the <see cref="TwinTestContext"/> class.
        /// Used for preparation executed once before any tests of the collection are started.
        /// </summary>
        public TwinTestContext() : base() {
            PrepareTestEnvironment();
        }

        /// <summary>
        /// Disposes resources.
        /// Used for cleanup executed once after all tests of the collection were executed.
        /// </summary>
        protected override void Dispose(bool disposing) {
            if (!disposing) {
                return;
            }

            // OutputHelper cannot be used outside of test calls, we get rid of it before a helper method would use it
            OutputHelper = null;

            using var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);

            if (!string.IsNullOrWhiteSpace(OpcUaEndpointId)) {
                TestHelper.Registry.DeactivateEndpointAsync(this, OpcUaEndpointId).GetAwaiter().GetResult();
                TestHelper.Registry.UnregisterServerAsync(this, OpcServerUrl, cts.Token).GetAwaiter().GetResult();
            }

            base.Dispose(true);
        }

        private void PrepareTestEnvironment() {
            RegisterOPCServerAndActivateEndpoint();
            CheckEndpointActivation();
        }

        private void RegisterOPCServerAndActivateEndpoint() {
            using var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);

            // We will wait for microservices of IIoT platform to be healthy and modules to be deployed.
            TestHelper.WaitForServicesAsync(this, cts.Token).GetAwaiter().GetResult();
            RegistryHelper.WaitForIIoTModulesConnectedAsync(DeviceConfig.DeviceId, cts.Token).GetAwaiter().GetResult();

            var endpointUrl = TestHelper.GetSimulatedOpcServerUrls(this).First();
            TestHelper.Registry.RegisterServerAsync(this, endpointUrl, cts.Token).GetAwaiter().GetResult();

            dynamic json = TestHelper.Discovery.WaitForDiscoveryToBeCompletedAsync(this, cts.Token, new HashSet<string> { endpointUrl }).GetAwaiter().GetResult();

            if (string.IsNullOrWhiteSpace(OpcUaEndpointId)) {
                OpcUaEndpointId = TestHelper.Discovery.GetOpcUaEndpointId(this, endpointUrl, cts.Token).GetAwaiter().GetResult();
                Assert.False(string.IsNullOrWhiteSpace(OpcUaEndpointId), "The endpoint id was not set");
            }

            OpcServerUrl = endpointUrl;
            TestHelper.Registry.ActivateEndpointAsync(this, OpcUaEndpointId, cts.Token).GetAwaiter().GetResult();
        }

        private void CheckEndpointActivation() {
            using var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);

            var endpoints = TestHelper.Registry.GetEndpointsAsync(this, cts.Token).GetAwaiter().GetResult();

            Assert.NotEmpty(endpoints);

            var (id, _, activationState, endpointState) = endpoints.SingleOrDefault(e => string.Equals(OpcUaEndpointId, e.Id));

            Assert.False(id == null, "The endpoint was not found");
            Assert.Equal(TestConstants.StateConstants.ActivatedAndConnected, activationState);
            Assert.Equal(TestConstants.StateConstants.Ready, endpointState);
        }
    }
}
