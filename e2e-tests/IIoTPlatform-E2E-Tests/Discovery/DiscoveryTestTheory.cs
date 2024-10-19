// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatformE2ETests.Discovery
{
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
    public sealed class DiscoveryTestTheory : IDisposable
    {
        private readonly DiscoveryTestContext _context;
        private readonly CancellationTokenSource _cancellationTokenSource;

        public DiscoveryTestTheory(DiscoveryTestContext context, ITestOutputHelper output)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _context.SetOutputHelper(output);

            _cancellationTokenSource = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);
        }

        [Fact, PriorityOrder(0)]
        public async Task TestPrepareAsync()
        {
            // Get OAuth token
            var token = await TestHelper.GetTokenAsync(_context, _cancellationTokenSource.Token);
        }

        [Fact, PriorityOrder(1)]
        public async Task TestDiscoverAllOpcUaEndpoints()
        {
            await TestHelper.Registry.RemoveAllApplicationsAsync(_context, _cancellationTokenSource.Token);

            // Add 5 servers
            var urls = TestHelper.GetSimulatedOpcServerUrls(_context).Take(5).ToHashSet();
            await AddTestOpcServersAsync(urls, _cancellationTokenSource.Token);

            // Discover all servers
            await TestHelper.Discovery.WaitForDiscoveryToBeCompletedAsync(
                _context, urls, _cancellationTokenSource.Token);
        }

        [Fact, PriorityOrder(2)]
        public async Task TestDiscoverOpcUaEndpointsIpAddress()
        {
            await TestHelper.Registry.RemoveAllApplicationsAsync(_context, _cancellationTokenSource.Token);

            // Add 1 server
            var ipAddress = _context.OpcPlcConfig.Ips.Split(TestConstants.SimulationUrlsSeparator).First();
            var url = $"opc.tcp://{ipAddress}:50000";
            var urls = new HashSet<string> { url };
            await AddTestOpcServersAsync(urls, _cancellationTokenSource.Token);

            // Discover all servers
            await TestHelper.Discovery.WaitForDiscoveryToBeCompletedAsync(
                _context, urls, _cancellationTokenSource.Token);
        }

        [Fact, PriorityOrder(3)]
        public async Task TestDiscoverOpcUaEndpointsPortRange()
        {
            await TestHelper.Registry.RemoveAllApplicationsAsync(_context, _cancellationTokenSource.Token);

            // Add 2 servers
            var urls = TestHelper.GetSimulatedOpcServerUrls(_context).Take(2).ToHashSet();
            await AddTestOpcServersAsync(urls, _cancellationTokenSource.Token);

            // Discover all servers
            await TestHelper.Discovery.WaitForDiscoveryToBeCompletedAsync(
                _context, urls, _cancellationTokenSource.Token);
        }

        [Fact, PriorityOrder(4)]
        public async Task TestDiscoverCleanupRegistry()
        {
            await TestHelper.Registry.RemoveAllApplicationsAsync(_context, _cancellationTokenSource.Token);
        }

        private async Task AddTestOpcServersAsync(HashSet<string> endpointUrls, CancellationToken ct)
        {
            foreach (var endpointUrl in endpointUrls)
            {
                var body = new
                {
                    discoveryUrl = endpointUrl
                };
                var response = await TestHelper.CallRestApi(_context, Method.Post, TestConstants.APIRoutes.RegistryApplications, body, ct: ct).ConfigureAwait(false);
                Assert.True(response.IsSuccessful, $"Got {response.StatusCode} registering {endpointUrl} discovery url");
            }
        }

        public void Dispose()
        {
            _context.Dispose();
            _cancellationTokenSource.Dispose();
        }
    }
}
