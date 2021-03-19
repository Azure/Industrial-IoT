// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatform_E2E_Tests.Discovery {
    using IIoTPlatform_E2E_Tests.TestExtensions;
    using RestSharp;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;

    public class DiscoveryTestContext : IIoTPlatformTestContext {
        /// <summary>
        /// Initializes a new instance of the <see cref="DiscoveryTestContext"/> class.
        /// Used for preparation executed once before any tests of the collection are started.
        /// </summary>
        public DiscoveryTestContext() : base() {
            GetServersInformation();
        }

        /// <summary>
        /// Gets or sets the servers info
        /// </summary>
        public List<dynamic> ServersInfo;

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
            var applicationIds = ServersInfo.Select(s => s.applicationId?.ToString());
            RemoveAllApplications(applicationIds.OfType<string>().ToList());

            base.Dispose(true);
        }

        private void GetServersInformation() {
            // Add servers
            var simulatedOpcServers = TestHelper.GetSimulatedPublishedNodesConfigurationAsync(this).GetAwaiter().GetResult();
            var urls = simulatedOpcServers.Values.ToList().Select(s => s.EndpointUrl).ToList();
            AddTestOpcServers(urls);

            // Get info about servers
            dynamic result = TestHelper.Discovery.WaitForDiscoveryToBeCompletedAsync(this, requestedEndpointUrls: urls).GetAwaiter().GetResult();
            ServersInfo = result.items;

            // Remove servers
            var applicationIds = ServersInfo.Select(s => s.applicationId?.ToString());
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
