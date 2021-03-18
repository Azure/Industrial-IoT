// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatform_E2E_Tests.Discovery {
    using IIoTPlatform_E2E_Tests.TestExtensions;
    using RestSharp;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;

    public class DiscoveryTestContext : IIoTPlatformTestContext {
        /// <summary>
        /// Initializes a new instance of the <see cref="DiscoveryTestContext"/> class.
        /// Used for preparation executed once before any tests of the collection are started.
        /// </summary>
        public DiscoveryTestContext() : base() {
            PrepareTestEnvironment();
        }

        public List<dynamic> _servers;

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

            // Remove servers
            var applicationIds = _servers.Select(s => s.applicationId?.ToString());
            RemoveAllApplications(applicationIds.OfType<string>().ToList());        
        }

        private void PrepareTestEnvironment() {
            // Get info about servers
            var cancellationTokenSource = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);
            var simulatedOpcServers = TestHelper.GetSimulatedPublishedNodesConfigurationAsync(this, cancellationTokenSource.Token).GetAwaiter().GetResult();
            var urls = simulatedOpcServers.Values.ToList().Select(s => s.EndpointUrl).ToList();
            AddTestOpcServers(urls);
            dynamic result = TestHelper.WaitForDiscoveryToBeCompletedAsync(this, cancellationTokenSource.Token, requestedEndpointUrls: urls).GetAwaiter().GetResult();
            _servers = result.items;

            // Remove servers
            var applicationIds = _servers.Select(s => s.applicationId?.ToString());
            RemoveAllApplications(applicationIds.OfType<string>().ToList());
        }

        private void AddTestOpcServers(List<string> endpointUrls) {
            foreach (var endpointUrl in endpointUrls) {
                var body = new {
                    discoveryUrl = endpointUrl
                };
                TestHelper.CallRestApi(this, Method.POST, TestConstants.APIRoutes.RegistryApplications, body);
            }
        }

        private void RemoveApplication(string applicationId) {
            var route = $"{TestConstants.APIRoutes.RegistryApplications}/{applicationId}";
            TestHelper.CallRestApi(this, Method.DELETE, route);        
        }

        private void RemoveAllApplications(List<string> applicationIds) {
            foreach (var appId in applicationIds) {
                RemoveApplication(appId);
            }
        }
    }
}
