// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatform_E2E_Tests {
    using IIoTPlatform_E2E_Tests.TestExtensions;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using RestSharp;
    using System;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    internal static partial class TestHelper {

        /// <summary>
        /// Discovery related helper methods
        /// </summary>
        public static class Discovery {
            /// <summary>
            /// Wait until the OPC UA server is discovered
            /// </summary>
            /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
            /// <param name="ct">Cancellation token</param>
            /// <param name="requestedEndpointUrls">List of OPC UA endpoint URLS that need to be activated and connected</param>
            /// <returns>content of GET /registry/v2/application request as dynamic object</returns>
            public static async Task<dynamic> WaitForDiscoveryToBeCompletedAsync(
                IIoTPlatformTestContext context,
                CancellationToken ct = default,
                HashSet<string> requestedEndpointUrls = null
            ) {
                ct.ThrowIfCancellationRequested();

                try {
                    dynamic json;
                    var foundEndpoints = new HashSet<string>();
                    int numberOfItems;
                    bool shouldExit = false;
                    do {
                        var route = TestConstants.APIRoutes.RegistryApplications;
                        var response = CallRestApi(context, Method.Get, route, ct: ct);
                        Assert.True(response.IsSuccessful, $"Got {response.StatusCode} calling {route}.");
                        Assert.NotEmpty(response.Content);
                        json = JsonConvert.DeserializeObject<ExpandoObject>(response.Content, new ExpandoObjectConverter());
                        Assert.NotNull(json);
                        numberOfItems = (int)json.items.Count;
                        if (numberOfItems <= 0) {
                            await Task.Delay(TestConstants.DefaultDelayMilliseconds);
                        }
                        else {
                            for (int i = 0; i < numberOfItems; i++) {
                                var endpoint = "opc.tcp://" +((string)json.items[i].hostAddresses[0]).TrimEnd('/');

                                if (requestedEndpointUrls == null || requestedEndpointUrls.Contains(endpoint)) {
                                    if (!foundEndpoints.Contains(endpoint)) {
                                        context.OutputHelper?.WriteLine($"Found {endpoint}...");
                                        foundEndpoints.Add(endpoint);
                                    }
                                }
                            }

                            var expectedNumberOfEndpoints = requestedEndpointUrls != null
                                                            ? requestedEndpointUrls.Count
                                                            : 1;

                            if (foundEndpoints.Count < expectedNumberOfEndpoints) {
                                await Task.Delay(TestConstants.DefaultDelayMilliseconds);
                            }
                            else {
                                context.OutputHelper?.WriteLine($"Found all endpoints!");
                                shouldExit = true;
                            }
                        }

                    } while (!shouldExit);

                    return json;
                }
                catch (Exception e) {
                    context.OutputHelper?.WriteLine("Error: discovery module didn't find OPC UA server in time");
                    PrettyPrintException(e, context.OutputHelper);
                    return null;
                }
            }

            /// <summary>
            /// Wait until the OPC UA endpoint is detected
            /// </summary>
            /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
            /// <param name="ct">Cancellation token</param>
            /// <param name="requestedEndpointUrls">List of OPC UA endpoint URLS that need to be activated and connected</param>
            /// <param name="securityMode"></param>
            /// <param name="securityPolicy"></param>
            /// <returns>content of GET /registry/v2/endpoints request as dynamic object</returns>
            public static async Task<dynamic> WaitForEndpointDiscoveryToBeCompleted(
                IIoTPlatformTestContext context,
                CancellationToken ct = default,
                HashSet<string> requestedEndpointUrls = null,
                string securityMode = null,
                string securityPolicy = null) {

                ct.ThrowIfCancellationRequested();
                if (requestedEndpointUrls != null && requestedEndpointUrls.Count > 0) {
                    context.OutputHelper?.WriteLine($"Waiting for endpoint {requestedEndpointUrls.Aggregate((a, b) => a + ", " + b)}");
                }
                try {
                    dynamic json;
                    var foundEndpoints = new HashSet<string>();
                    var totalEndpoints = new HashSet<string>();
                    int numberOfItems;
                    bool shouldExit = false;
                    do {
                        json = await GetEndpointsInternalAsync(context, ct).ConfigureAwait(false);

                        Assert.NotNull(json);
                        numberOfItems = (int)json.items.Count;
                        if (numberOfItems <= 0) {
                            await Task.Delay(TestConstants.DefaultDelayMilliseconds);
                        }
                        else {
                            for (int indexOfOpcUaEndpoint = 0; indexOfOpcUaEndpoint < numberOfItems; indexOfOpcUaEndpoint++) {
                                var endpoint = ((string)json.items[indexOfOpcUaEndpoint].registration.endpoint.url).TrimEnd('/');
                                if (!totalEndpoints.Contains(endpoint)) {
                                    context.OutputHelper?.WriteLine($"Found {endpoint}.");
                                    totalEndpoints.Add(endpoint);
                                }

                                if (requestedEndpointUrls == null || requestedEndpointUrls.Contains(endpoint)) {
                                    if ((securityMode == null ||
                                        securityMode == json.items[indexOfOpcUaEndpoint].registration.endpoint.securityMode) &&
                                        (securityPolicy == null ||
                                        securityPolicy == json.items[indexOfOpcUaEndpoint].registration.endpoint.securityPolicy)) {
                                        if (!foundEndpoints.Contains(endpoint)) {
                                            context.OutputHelper?.WriteLine($"Matched {endpoint}...");
                                            foundEndpoints.Add(endpoint);
                                        }
                                    }
                                }
                            }

                            var expectedNumberOfEndpoints = requestedEndpointUrls != null
                                                            ? requestedEndpointUrls.Count
                                                            : 1;

                            if (foundEndpoints.Count < expectedNumberOfEndpoints) {
                                await Task.Delay(TestConstants.DefaultDelayMilliseconds);
                            }
                            else {
                                context.OutputHelper?.WriteLine($"Found all endpoints!");
                                shouldExit = true;
                            }
                        }

                    } while (!shouldExit);

                    return json;
                }
                catch (Exception e) {
                    context.OutputHelper?.WriteLine("Error: OPC UA endpoint not found in time");
                    PrettyPrintException(e, context.OutputHelper);
                    throw;
                }
            }

            /// <summary>
            /// Waits for the discovery to be completed and then gets the Endpoint ID
            /// </summary>
            /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
            /// <param name="requestedEndpointUrl">Endpoint URL to get the ID for</param>
            /// <param name="ct">Cancellation token</param>
            /// <param name="securityMode"></param>
            /// <param name="securityPolicy"></param>
            /// <returns></returns>
            public static async Task<string> GetOpcUaEndpointId(
                    IIoTPlatformTestContext context,
                    string requestedEndpointUrl,
                    CancellationToken ct,
                    string securityMode = null,
                    string securityPolicy = null) {
                var json = await WaitForEndpointDiscoveryToBeCompleted(context, ct, new HashSet<string> { requestedEndpointUrl }, securityMode, securityPolicy);

                int numberOfItems = json.items.Count;

                for (var indexOfOpcUaEndpoint = 0; indexOfOpcUaEndpoint < numberOfItems; indexOfOpcUaEndpoint++) {

                    var endpoint = ((string)json.items[indexOfOpcUaEndpoint].registration.endpoint.url).TrimEnd('/');
                    if (endpoint == requestedEndpointUrl) {
                        context.ApplicationId = json.items[indexOfOpcUaEndpoint].applicationId;
                        return (string)json.items[indexOfOpcUaEndpoint].registration.id;
                    }
                }

                return null;
            }
        }
    }
}
