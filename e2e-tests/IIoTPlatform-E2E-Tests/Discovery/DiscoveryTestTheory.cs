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
    using System.Threading.Tasks;
    using TestExtensions;
    using Xunit;
    using Xunit.Abstractions;

    [TestCaseOrderer(TestCaseOrderer.FullName, TestConstants.TestAssemblyName)]
    [Collection(DiscoveryTestCollection.CollectionName)]
    [Trait(TestConstants.TraitConstants.DiscoveryModeTraitName, TestConstants.TraitConstants.DefaultTraitValue)]
    [Trait(TestConstants.TraitConstants.TestModeTraitName, TestConstants.TraitConstants.DefaultTraitValue)]
    public class DiscoveryTestTheory {
        private readonly DiscoveryTestContext _context;
        private readonly CancellationTokenSource _cancellationTokenSource;

        public DiscoveryTestTheory(DiscoveryTestContext context, ITestOutputHelper output) {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _context.OutputHelper = output ?? throw new ArgumentNullException(nameof(output));

            _cancellationTokenSource = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);

            // Get OAuth token
            var token = TestHelper.GetTokenAsync(_context, _cancellationTokenSource.Token).GetAwaiter().GetResult();
            Assert.NotEmpty(token);
        }

        [Fact, PriorityOrder(0)]
        public async Task TestDiscoverAllOpcUaEndpoints() {
            // Add 5 servers
            var endpointUrls = TestHelper.GetSimulatedOpcServerUrls(_context).Take(5).ToList();
            AddTestOpcServers(endpointUrls, _cancellationTokenSource.Token);

            // Discover all servers
            var result = await TestHelper.Discovery.WaitForDiscoveryToBeCompletedAsync(
                _context, _cancellationTokenSource.Token, endpointUrls).ConfigureAwait(false);

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
                RemoveApplication(applicationId, _cancellationTokenSource.Token);
            }
        }

        [Fact, PriorityOrder(1)]
        public async Task TestDiscoverOpcUaEndpointsIpAddress() {
            // Add 1 server
            var ipAddress = _context.OpcPlcConfig.Urls.Split(TestConstants.SimulationUrlsSeparator).First();
            var url = $"opc.tcp://{ipAddress}:50000";
            var urls = new List<string> { url };
            AddTestOpcServers(urls, _cancellationTokenSource.Token);

            // Registers servers by running a discovery scan
            var cidr = ipAddress + "/32";
            var body = new {
                configuration = new {
                    addressRangesToScan = cidr
                }
            };
            TestHelper.CallRestApi(
                _context,
                Method.Post,
                TestConstants.APIRoutes.RegistryDiscover,
                body,
                ct: _cancellationTokenSource.Token
            );

            // Validate that the endpoint can be found
            var result = await TestHelper.Discovery.WaitForEndpointDiscoveryToBeCompleted(
                _context, _cancellationTokenSource.Token, urls).ConfigureAwait(false);
            Assert.Equal(url, ((string)result.items[0].registration.endpoint.url).TrimEnd('/'));

            // Clean up
            RemoveApplication((string)result.items[0].applicationId, _cancellationTokenSource.Token);
        }

        [Fact, PriorityOrder(2)]
        public async Task TestDiscoverOpcUaEndpointsPortRange() {
            // Add 5 servers
            var urls = TestHelper.GetSimulatedOpcServerUrls(_context).Take(5).ToList();
            AddTestOpcServers(urls, _cancellationTokenSource.Token);

            // Registers servers by running a discovery scan
            var body = new {
                configuration = new {
                    portRangesToScan = "50000:51000"
                }
            };
            TestHelper.CallRestApi(
                _context,
                Method.Post,
                TestConstants.APIRoutes.RegistryDiscover,
                body,
                ct: _cancellationTokenSource.Token
            );

            // Validate that all endpoints are found
            var result = await TestHelper.Discovery.WaitForEndpointDiscoveryToBeCompleted(
                _context, _cancellationTokenSource.Token, urls).ConfigureAwait(false);
            var applicationIds = new List<string>(urls.Count);
            foreach (var item in result.items) {
                Assert.Contains(((string)item.registration.endpoint.url).TrimEnd('/'), urls);
                applicationIds.Add((string)item.applicationId);
            }

            // Clean up
            foreach (var applicationId in applicationIds) {
                RemoveApplication(applicationId, _cancellationTokenSource.Token);
            }
        }

        private void AddTestOpcServers(List<string> endpointUrls, CancellationToken ct) {
            foreach (var endpointUrl in endpointUrls) {
                var body = new {
                    discoveryUrl = endpointUrl
                };
                TestHelper.CallRestApi(_context, Method.Post, TestConstants.APIRoutes.RegistryApplications, body, ct: ct);
            }
        }

        private void RemoveApplication(string applicationId, CancellationToken ct) {
            var route = $"{TestConstants.APIRoutes.RegistryApplications}/{applicationId}";
            TestHelper.CallRestApi(_context, Method.Delete, route, ct: ct);
        }
    }
}
