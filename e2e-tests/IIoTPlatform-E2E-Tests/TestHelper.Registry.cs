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
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    internal static partial class TestHelper {

        /// <summary>
        /// Registry related helper methods
        /// </summary>
        public static class Registry {

            /// <summary>
            /// Wait for first OPC UA endpoint to be activated
            /// </summary>
            /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
            /// <param name="ct">Cancellation token</param>
            /// <param name="requestedEndpointUrls">List of OPC UA endpoint URLS that need to be activated and connected</param>
            /// <returns>content of GET /registry/v2/endpoints request as dynamic object</returns>
            public static async Task<dynamic> WaitForEndpointToBeActivatedAsync(
                IIoTPlatformTestContext context,
                CancellationToken ct = default,
                IEnumerable<string> requestedEndpointUrls = null) {

                var accessToken = await GetTokenAsync(context, ct);
                var client = new RestClient(context.IIoTPlatformConfigHubConfig.BaseUrl) {
                    Timeout = TestConstants.DefaultTimeoutInMilliseconds
                };

                ct.ThrowIfCancellationRequested();
                try {
                    dynamic json;
                    var activationStates = new List<string>(10);
                    do {
                        activationStates.Clear();
                        var request = new RestRequest(Method.GET);
                        request.AddHeader(TestConstants.HttpHeaderNames.Authorization, accessToken);
                        request.Resource = TestConstants.APIRoutes.RegistryEndpoints;

                        var response = await client.ExecuteAsync(request, ct);
                        Assert.NotNull(response);
                        Assert.True(response.IsSuccessful, "GET /registry/v2/endpoints failed!");

                        if (!response.IsSuccessful) {
                            context.OutputHelper?.WriteLine($"StatusCode: {response.StatusCode}");
                            context.OutputHelper?.WriteLine($"ErrorMessage: {response.ErrorMessage}");
                        }

                        Assert.NotEmpty(response.Content);
                        json = JsonConvert.DeserializeObject<ExpandoObject>(response.Content, new ExpandoObjectConverter());
                        Assert.NotNull(json);

                        int count = (int)json.items.Count;
                        Assert.NotEqual(0, count);
                        if (requestedEndpointUrls == null) {
                            activationStates.Add((string)json.items[0].activationState);
                        }
                        else {
                            for (int indexOfRequestedOpcServer = 0;
                                indexOfRequestedOpcServer < count;
                                indexOfRequestedOpcServer++) {
                                var endpoint = ((string)json.items[indexOfRequestedOpcServer].registration.endpoint.url).TrimEnd('/');
                                if (requestedEndpointUrls.Contains(endpoint)) {
                                    activationStates.Add((string)json.items[indexOfRequestedOpcServer].activationState);
                                }
                            }
                        }

                        // wait the endpoint to be connected
                        if (activationStates.Any(s => s == "Activated")) {
                            await Task.Delay(TestConstants.DefaultTimeoutInMilliseconds, ct);
                        }

                    } while (activationStates.All(s => s != "ActivatedAndConnected"));

                    return json;
                }
                catch (Exception) {
                    context.OutputHelper?.WriteLine("Error: OPC UA endpoint couldn't be activated");
                    return null;
                }
            }

            /// <summary>
            /// Registers a server, the discovery url will be saved in the <paramref name="context"/>
            /// </summary>
            /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
            /// <param name="discoveryUrl">Discovery URL to register</param>
            /// <param name="ct">Cancellation token</param>
            public static async Task RegisterServerAsync(
                    IIoTPlatformTestContext context,
                    string discoveryUrl,
                    CancellationToken ct = default) {

                if (string.IsNullOrEmpty(discoveryUrl)) {
                    context.OutputHelper.WriteLine($"{nameof(discoveryUrl)} is null or empty");
                    throw new ArgumentNullException(nameof(discoveryUrl));
                }

                var accessToken = await GetTokenAsync(context, ct).ConfigureAwait(false);

                var client = new RestClient(context.IIoTPlatformConfigHubConfig.BaseUrl) { Timeout = TestConstants.DefaultTimeoutInMilliseconds };

                var request = new RestRequest(Method.POST);
                request.AddHeader(TestConstants.HttpHeaderNames.Authorization, accessToken);
                request.Resource = TestConstants.APIRoutes.RegistryApplications;

                var body = new { discoveryUrl };

                request.AddJsonBody(JsonConvert.SerializeObject(body));

                var response = await client.ExecuteAsync(request, ct).ConfigureAwait(false);
                Assert.NotNull(response);

                if (!response.IsSuccessful) {
                    context.OutputHelper.WriteLine($"StatusCode: {response.StatusCode}");
                    context.OutputHelper.WriteLine($"ErrorMessage: {response.ErrorMessage}");
                    Assert.True(response.IsSuccessful, "POST /registry/v2/applications failed!");
                }
            }

            /// <summary>
            /// Gets the application ID associated with the DiscoveryUrl property of <paramref name="context"/>
            /// </summary>
            /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
            /// <param name="discoveryUrl">Discovery URL the application is registered to</param>
            /// <param name="ct">Cancellation token</param>
            public static async Task<string> GetApplicationIdAsync(
                    IIoTPlatformTestContext context,
                    string discoveryUrl,
                    CancellationToken ct = default) {

                var accessToken = await GetTokenAsync(context, ct).ConfigureAwait(false);

                var client = new RestClient(context.IIoTPlatformConfigHubConfig.BaseUrl) { Timeout = TestConstants.DefaultTimeoutInMilliseconds };

                var request = new RestRequest(Method.GET);
                request.AddHeader(TestConstants.HttpHeaderNames.Authorization, accessToken);
                request.Resource = TestConstants.APIRoutes.RegistryApplications;

                var response = await client.ExecuteAsync(request, ct).ConfigureAwait(false);
                Assert.NotNull(response);

                if (!response.IsSuccessful) {
                    context.OutputHelper?.WriteLine($"StatusCode: {response.StatusCode}");
                    context.OutputHelper?.WriteLine($"ErrorMessage: {response.ErrorMessage}");
                    Assert.True(response.IsSuccessful, "GET /registry/v2/application failed!");
                }

                dynamic result = JsonConvert.DeserializeObject<ExpandoObject>(response.Content, new ExpandoObjectConverter());
                var json = (IDictionary<string, object>)result;

                Assert.True(HasProperty(result, "items"), "GET /registry/v2/application response did not contain items");
                Assert.False(result.items == null, "GET /registry/v2/application response items property is null");

                foreach (var item in result.items) {
                    var itemDictionary = (IDictionary<string, object>)item;

                    if (!itemDictionary.ContainsKey("discoveryUrls")
                        || !itemDictionary.ContainsKey("applicationId")) {
                        continue;
                    }

                    var discoveryUrls = (List<object>)item.discoveryUrls;
                    var itemUrl = (string)discoveryUrls?.FirstOrDefault(url => IsUrlStringsEqual(url as string, discoveryUrl));

                    if (itemUrl != null) {
                        return item.applicationId;
                    }
                }

                return null;
            }

            /// <summary>
            /// Unregisters a server identified by <paramref name="applicationId"/>
            /// </summary>
            /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
            /// <param name="discoveryUrl">Discovery URL the application is registered to</param>
            /// <param name="ct">Cancellation token</param>
            public static async Task UnregisterServerAsync(
                    IIoTPlatformTestContext context,
                    string discoveryUrl,
                    CancellationToken ct = default) {

                var applicationId = context.ApplicationId == null ? await GetApplicationIdAsync(context, discoveryUrl, ct) : context.ApplicationId;
                var accessToken = await GetTokenAsync(context, ct).ConfigureAwait(false);

                var client = new RestClient(context.IIoTPlatformConfigHubConfig.BaseUrl) { Timeout = TestConstants.DefaultTimeoutInMilliseconds };

                var request = new RestRequest(Method.DELETE);
                request.AddHeader(TestConstants.HttpHeaderNames.Authorization, accessToken);
                request.Resource = string.Format(TestConstants.APIRoutes.RegistryApplicationsWithApplicationIdFormat, applicationId);

                var response = await client.ExecuteAsync(request, ct).ConfigureAwait(false);
                Assert.NotNull(response);

                if (!response.IsSuccessful) {
                    context.OutputHelper?.WriteLine($"StatusCode: {response.StatusCode}");
                    context.OutputHelper?.WriteLine($"ErrorMessage: {response.ErrorMessage}");
                    Assert.True(response.IsSuccessful, "DELETE /registry/v2/application/{applicationId} failed!");
                }
            }

            /// <summary>
            /// Activates (and waits for activated and connected state) the endpoint from <paramref name="context"/>
            /// </summary>
            /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
            /// <param name="ct">Cancellation token</param>
            public static async Task ActivateEndpointAsync(IIoTPlatformTestContext context, string endpointId, CancellationToken ct = default) {
                var accessToken = await GetTokenAsync(context, ct).ConfigureAwait(false);

                Assert.False(string.IsNullOrWhiteSpace(endpointId), "Endpoint not set in the test context");

                var client = new RestClient(context.IIoTPlatformConfigHubConfig.BaseUrl) {
                    Timeout = TestConstants.DefaultTimeoutInMilliseconds
                };

                var request = new RestRequest(Method.POST);
                request.AddHeader(TestConstants.HttpHeaderNames.Authorization, accessToken);
                request.Resource = string.Format(TestConstants.APIRoutes.RegistryActivateEndpointsFormat, endpointId);

                var response = client.ExecuteAsync(request, ct).GetAwaiter().GetResult();

                Assert.True(response.IsSuccessful, "POST /registry/v2/endpoints/{endpointId}/activate failed!");

                while (true) {
                    Assert.False(ct.IsCancellationRequested, "Endpoint was not activated within the expected timeout");

                    var endpointList = await GetEndpointsAsync(context, ct).ConfigureAwait(false);
                    var endpoint = endpointList.FirstOrDefault(e => string.Equals(e.Id, endpointId));

                    if (string.Equals(endpoint.ActivationState, TestConstants.StateConstants.ActivatedAndConnected)
                            && string.Equals(endpoint.EndpointState, TestConstants.StateConstants.Ready)) {
                        return;
                    }

                    context.OutputHelper?.WriteLine(string.IsNullOrEmpty(endpoint.Url) ? "Endpoint not found" :
                        $"Endpoint state: {endpoint.EndpointState}, activation: {endpoint.ActivationState}");

                    await Task.Delay(TestConstants.DefaultDelayMilliseconds).ConfigureAwait(false);
                }
            }

            /// <summary>
            /// Deactivate the endpoint from <paramref name="context"/>
            /// </summary>
            /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
            /// <param name="ct">Cancellation token</param>
            public static async Task DeactivateEndpointAsync(IIoTPlatformTestContext context, string endpointId, CancellationToken ct = default) {
                var accessToken = await GetTokenAsync(context, ct).ConfigureAwait(false);

                Assert.False(string.IsNullOrWhiteSpace(endpointId), "Endpoint not set in the test context");

                var client = new RestClient(context.IIoTPlatformConfigHubConfig.BaseUrl) {
                    Timeout = TestConstants.DefaultTimeoutInMilliseconds
                };

                var request = new RestRequest(Method.POST);
                request.AddHeader(TestConstants.HttpHeaderNames.Authorization, accessToken);
                request.Resource = string.Format(TestConstants.APIRoutes.RegistryDeactivateEndpointsFormat, endpointId);

                var response = client.ExecuteAsync(request, ct).GetAwaiter().GetResult();

                Assert.True(response.IsSuccessful, "POST /registry/v2/endpoints/{endpointId}/deactivate failed!");

            }

            /// <summary>
            /// Gets endpoints from registry
            /// </summary>
            /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
            /// <param name="ct">Cancellation token</param>
            public static async Task<List<(string Id, string Url, string ActivationState, string EndpointState)>> GetEndpointsAsync(
                    IIoTPlatformTestContext context,
                    CancellationToken ct = default) {
                dynamic json = await GetEndpointInternalAsync(context, ct).ConfigureAwait(false);

                Assert.True(HasProperty(json, "items"), "GET /registry/v2/endpoints response has no items");
                Assert.False(json.items == null, "GET /registry/v2/endpoints response items property is null");
                Assert.NotEqual(0, json.items.Count);

                var result = new List<(string Id, string Url, string ActivationState, string EndpointState)>();

                foreach (var item in json.items) {
                    var id = item.registration.id?.ToString();
                    var endpointUrl = item.registration.endpointUrl?.ToString();
                    var activationState = item.activationState?.ToString();
                    var endpointState = item.endpointState?.ToString();
                    result.Add((id, endpointUrl, activationState, endpointState));
                }

                return result;
            }
        }
    }
}
