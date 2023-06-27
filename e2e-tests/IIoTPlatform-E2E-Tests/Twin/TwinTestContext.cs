// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatformE2ETests.Twin
{
    using IIoTPlatformE2ETests.TestExtensions;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public class TwinTestContext : IIoTPlatformTestContext
    {
        private bool _testEnvironmentPrepared;

        /// <summary>
        /// Initializes a new instance of the <see cref="TwinTestContext"/> class.
        /// Used for preparation executed once before any tests of the collection are started.
        /// </summary>
        public TwinTestContext()
        {
        }

        /// <summary>
        /// Disposes resources.
        /// Used for cleanup executed once after all tests of the collection were executed.
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            // OutputHelper cannot be used outside of test calls, we get rid of it before a helper method would use it
            OutputHelper = null;

            if (_testEnvironmentPrepared && !string.IsNullOrEmpty(OpcServerUrl))
            {
                using var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);
                TestHelper.Registry.UnregisterServerAsync(this, OpcServerUrl, cts.Token).GetAwaiter().GetResult();
            }

            base.Dispose(true);
            _testEnvironmentPrepared = false;
        }

        public async Task AssertTestEnvironmentPreparedAsync()
        {
            if (_testEnvironmentPrepared)
            {
                return;
            }

            using var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);

            // We will wait for microservices of IIoT platform to be healthy and modules to be deployed.
            await TestHelper.WaitForServicesAsync(this, cts.Token).ConfigureAwait(false);
            await RegistryHelper.WaitForIIoTModulesConnectedAsync(DeviceConfig.DeviceId, cts.Token).ConfigureAwait(false);

            var endpointUrl = TestHelper.GetSimulatedOpcServerUrls(this).First();
            await TestHelper.Registry.RegisterServerAsync(this, endpointUrl, cts.Token).ConfigureAwait(false);

            dynamic json = await TestHelper.Discovery.WaitForDiscoveryToBeCompletedAsync(this, new HashSet<string> { endpointUrl }, cts.Token).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(OpcUaEndpointId))
            {
                OpcUaEndpointId = await TestHelper.Discovery.GetOpcUaEndpointIdAsync(this, endpointUrl, cts.Token).ConfigureAwait(false);
                Assert.False(string.IsNullOrWhiteSpace(OpcUaEndpointId), "The endpoint id was not set");
            }

            OpcServerUrl = endpointUrl;
            _testEnvironmentPrepared = true;
        }
    }
}
