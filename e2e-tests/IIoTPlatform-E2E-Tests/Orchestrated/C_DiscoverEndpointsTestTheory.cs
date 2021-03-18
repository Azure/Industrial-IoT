// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatform_E2E_Tests.Orchestrated {
    using Newtonsoft.Json;
    using RestSharp;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using TestExtensions;
    using Xunit;
    using Xunit.Abstractions;

    [TestCaseOrderer(TestCaseOrderer.FullName, TestConstants.TestAssemblyName)]
    [Collection("IIoT Multiple Nodes Test Collection")]
    [Trait(TestConstants.TraitConstants.DiscovererModeTraitName, TestConstants.TraitConstants.DefaultTraitValue)]
    public class C_DiscoverEndpointsTestTheory {
        private readonly ITestOutputHelper _output;
        private readonly IIoTMultipleNodesTestContext _context;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly RestClient _restClient;
        private readonly List<dynamic> _servers;

        public C_DiscoverEndpointsTestTheory(IIoTMultipleNodesTestContext context, ITestOutputHelper output) {
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _context.OutputHelper = _output;
            _cancellationTokenSource = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);
            _restClient = new RestClient(_context.IIoTPlatformConfigHubConfig.BaseUrl) { Timeout = TestConstants.DefaultTimeoutInMilliseconds };

            // Switch to Orchestrated mode
            TestHelper.SwitchToOrchestratedModeAsync(_context).GetAwaiter().GetResult();

            // Get OAuth token
            var token = TestHelper.GetTokenAsync(_context, _cancellationTokenSource.Token).GetAwaiter().GetResult();
            Assert.NotEmpty(token);

            // Get info about servers
            var simulatedOpcServers = TestHelper.GetSimulatedPublishedNodesConfigurationAsync(_context, _cancellationTokenSource.Token).GetAwaiter().GetResult();
            var urls = simulatedOpcServers.Values.ToList().Select(s => s.EndpointUrl).ToList();
            AddTestOpcServers(urls);
            dynamic result = TestHelper.Discovery.WaitForDiscoveryToBeCompletedAsync(_context, _cancellationTokenSource.Token, requestedEndpointUrls: urls).GetAwaiter().GetResult();
            _servers = result.items;

            // Remove servers
            var applicationIds = _servers.Select(s => s.applicationId?.ToString());
            RemoveAllApplications(applicationIds.OfType<string>().ToList());
        }

        [Fact, PriorityOrder(0)]
        public void Test_Discover_OPC_UA_Endpoints_IpAddress() {
            // Add 1 server
            var server = _servers[0];
            string url = Convert.ToString(server.discoveryUrls[0]).TrimEnd('/');
            var urls = new List<string> { url };
            AddTestOpcServers(urls);

            // Registers servers by running a discovery scan 
            string ipAddress = Convert.ToString(server.hostAddresses[0]);
            var cidr = ipAddress.Replace(":50000/", "") + "/16";
            var body = new {
                configuration = new {
                    addressRangesToScan = cidr
                }
            };
            CallRestApi(Method.POST, TestConstants.APIRoutes.RegistryDiscover, body);

            // Validate that the endpoint can be found
            var result = TestHelper.Discovery.WaitForEndpointDiscoveryToBeCompleted(_context, _cancellationTokenSource.Token, requestedEndpointUrls: urls).GetAwaiter().GetResult();
            Assert.Equal(url, ((string)result.items[0].registration.endpointUrl).TrimEnd('/'));

            // Validate that the certificate can be returned
            var endpoint = result.items[0].registration.endpoint;
            Assert.Equal("SignAndEncrypt", endpoint.securityMode.ToString());
            Assert.Equal("http://opcfoundation.org/UA/SecurityPolicy#Basic256Sha256", endpoint.securityPolicy.ToString());
            Assert.Equal(40, endpoint.certificate.ToString().Length);
        }

        [Fact, PriorityOrder(1)]
        public void Test_Discover_OPC_UA_Endpoints_PortRange() {
            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);

            // Add 5 servers
            var urls = _servers.SelectMany(s => (List<object>)s.discoveryUrls).Take(5).OfType<string>().ToList();
            urls = urls.Select(u => u.TrimEnd('/')).ToList();
            AddTestOpcServers(urls);

            // Registers servers by running a discovery scan
            var body = new {
                configuration = new {
                    portRangesToScan = "50000:51000"
                }
            };
            var reponse = CallRestApi(Method.POST, TestConstants.APIRoutes.RegistryDiscover, body);

            // Validate that all endpoints are found
            var result = TestHelper.Discovery.WaitForEndpointDiscoveryToBeCompleted(_context, cts.Token, requestedEndpointUrls: urls).GetAwaiter().GetResult();
            foreach (var item in result.items) {
                Assert.Contains(((string)item.registration.endpointUrl).TrimEnd('/'), urls);
            }
        }

        [Fact, PriorityOrder(2)]
        public void Test_Discover_All_OPC_UA_Endpoints() {
            // Add 5 servers
            var endpointUrls = _servers.SelectMany(s => (List<object>)s.discoveryUrls).Take(5).OfType<string>().ToList();
            endpointUrls = endpointUrls.Select(u => u.TrimEnd('/')).ToList();
            AddTestOpcServers(endpointUrls);

            // Discover all servers
            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);
            dynamic result = TestHelper.Discovery.WaitForDiscoveryToBeCompletedAsync(_context, cts.Token, requestedEndpointUrls: endpointUrls).GetAwaiter().GetResult();

            // Validate that all servers are discovered
            var applicationIds = new List<string>(endpointUrls.Count);
            Assert.Equal(endpointUrls.Count, result.items.Count);
            for (int i = 0; i < result.items.Count; i++) {
                Assert.Equal("Server", result.items[i].applicationType);
                Assert.True((bool)endpointUrls.Contains(result.items[i].discoveryUrls[0].TrimEnd('/')));
                applicationIds.Add(result.items[i].applicationId);
            }

            // Remove all servers
            RemoveAllApplications(applicationIds);
        }

        private void AddTestOpcServers(List<string> endpointUrls) {
            foreach (var endpointUrl in endpointUrls) {
                var body = new {
                    discoveryUrl = endpointUrl
                };
                CallRestApi(Method.POST, TestConstants.APIRoutes.RegistryApplications, body);
            }
        }

        private void RemoveApplication(string applicationId) {
            var route = $"{TestConstants.APIRoutes.RegistryApplications}/{applicationId}";
            CallRestApi(Method.DELETE, route);
        }

        private void RemoveAllApplications(List<string> applicationIds) {
            foreach (var appId in applicationIds) {
                RemoveApplication(appId);
            }
        }

        private IRestResponse CallRestApi(Method method, string route, object body = null) {
            var accessToken = TestHelper.GetTokenAsync(_context, _cancellationTokenSource.Token).GetAwaiter().GetResult();

            var request = new RestRequest(method);
            request.Resource = route;
            request.AddHeader(TestConstants.HttpHeaderNames.Authorization, accessToken);

            if (body != null) {
                request.AddJsonBody(JsonConvert.SerializeObject(body));
            }

            var response = _restClient.ExecuteAsync(request, _cancellationTokenSource.Token).GetAwaiter().GetResult();
            Assert.True(response.IsSuccessful);
            return response;
        }
    }
}