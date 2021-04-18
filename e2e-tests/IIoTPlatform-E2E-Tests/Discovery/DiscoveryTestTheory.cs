// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatform_E2E_Tests.Discovery {
    using RestSharp;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using TestExtensions;
    using Xunit;
    using Xunit.Abstractions;

    [TestCaseOrderer(TestCaseOrderer.FullName, TestConstants.TestAssemblyName)]
    [Collection(DiscoveryTestCollection.CollectionName)]
    [Trait(TestConstants.TraitConstants.DiscoveryModeTraitName, TestConstants.TraitConstants.DefaultTraitValue)]
    public class DiscoveryTestTheory {
        private readonly DiscoveryTestContext _context;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly RestClient _restClient;      

        public DiscoveryTestTheory(DiscoveryTestContext context, ITestOutputHelper output) {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _context.OutputHelper = output ?? throw new ArgumentNullException(nameof(output));
            
            _cancellationTokenSource = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);
            _restClient = new RestClient(_context.IIoTPlatformConfigHubConfig.BaseUrl) { Timeout = TestConstants.DefaultTimeoutInMilliseconds };

            // Get OAuth token
            var token = TestHelper.GetTokenAsync(_context, _cancellationTokenSource.Token).GetAwaiter().GetResult();
            Assert.NotEmpty(token);
        }

        [Fact, PriorityOrder(0)]
        public void Test_Discover_All_OPC_UA_Endpoints() {
            // Add 5 servers
            var endpointUrls = TestHelper.GetSimulatedOpcServerUrls(_context).Take(5).ToList();
            AddTestOpcServers(endpointUrls);

            // Discover all servers
            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);
            dynamic result = TestHelper.Discovery.WaitForDiscoveryToBeCompletedAsync(_context, cts.Token, requestedEndpointUrls: endpointUrls).GetAwaiter().GetResult();

            // Validate that all servers are discovered
            var applicationIds = new List<string>(endpointUrls.Count);
            Assert.Equal(endpointUrls.Count, result.items.Count);
            for (int i = 0; i < result.items.Count; i++) {
                Assert.Equal("Server", result.items[i].applicationType);
                var endpoint = "opc.tcp://" + result.items[i].hostAddresses[0].TrimEnd('/');
                Assert.True((bool)endpointUrls.Contains(endpoint));
                applicationIds.Add((string)result.items[i].applicationId);
            }

            // Clean up
            foreach (var applicationId in applicationIds) {
                RemoveApplication(applicationId);
            }
        }

        [Fact, PriorityOrder(1)]
        public void Test_Discover_OPC_UA_Endpoints_IpAddress() {
            // Add 1 server
            var ipAddress = _context.OpcPlcConfig.Urls.Split(TestConstants.SimulationUrlsSeparator).First();
            var url = $"opc.tcp://{ipAddress}:50000";
            var urls = new List<string> { url };
            AddTestOpcServers(urls);

            // Registers servers by running a discovery scan 
            var cidr = ipAddress + "/16";
            var body = new {
                configuration = new {
                    addressRangesToScan = cidr
                }
            };
            TestHelper.CallRestApi(_context, Method.POST, TestConstants.APIRoutes.RegistryDiscover, body);

            // Validate that the endpoint can be found
            var result = TestHelper.Discovery.WaitForEndpointDiscoveryToBeCompleted(_context, _cancellationTokenSource.Token, requestedEndpointUrls: urls).GetAwaiter().GetResult();
            Assert.Equal(url, ((string)result.items[0].registration.endpoint.url).TrimEnd('/'));

            // Clean up
            RemoveApplication((string)result.items[0].applicationId);
        }

        [Fact, PriorityOrder(2)]
        public void Test_Discover_OPC_UA_Endpoints_PortRange() {
            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);

            // Add 5 servers
            var urls = TestHelper.GetSimulatedOpcServerUrls(_context).Take(5).ToList();
            AddTestOpcServers(urls);

            // Registers servers by running a discovery scan
            var body = new {
                configuration = new {
                    portRangesToScan = "50000:51000"
                }
            };
            TestHelper.CallRestApi(_context, Method.POST, TestConstants.APIRoutes.RegistryDiscover, body);

            // Validate that all endpoints are found
            var result = TestHelper.Discovery.WaitForEndpointDiscoveryToBeCompleted(_context, cts.Token, requestedEndpointUrls: urls).GetAwaiter().GetResult();
            var applicationIds = new List<string>(urls.Count);
            foreach (var item in result.items) {
                Assert.Contains(((string)item.registration.endpoint.url).TrimEnd('/'), urls);
                applicationIds.Add((string)result.items[0].applicationId);
            }

            // Clean up
            foreach (var applicationId in applicationIds) {
                RemoveApplication(applicationId);
            }
        }
      
        private void AddTestOpcServers(List<string> endpointUrls) {
            foreach (var endpointUrl in endpointUrls) {
                var body = new {
                    discoveryUrl = endpointUrl
                };
                TestHelper.CallRestApi(_context, Method.POST, TestConstants.APIRoutes.RegistryApplications, body);
            }
        }

        private void RemoveApplication(string applicationId) {
            var route = $"{TestConstants.APIRoutes.RegistryApplications}/{applicationId}";
            TestHelper.CallRestApi(_context, Method.DELETE, route);
        }
    }
}
