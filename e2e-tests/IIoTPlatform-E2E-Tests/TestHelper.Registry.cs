// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatformE2ETests
{
    using IIoTPlatformE2ETests.TestExtensions;
    using Furly.Exceptions;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using RestSharp;
    using System;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.Globalization;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    internal static partial class TestHelper
    {
        /// <summary>
        /// Registry related helper methods
        /// </summary>
        public static class Registry
        {
            /// <summary>
            /// Registers a server, the discovery url will be saved in the <paramref name="context"/>
            /// </summary>
            /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
            /// <param name="discoveryUrl">Discovery URL to register</param>
            /// <param name="ct">Cancellation token</param>
            /// <exception cref="ArgumentNullException"></exception>
            public static async Task RegisterServerAsync(
                    IIoTPlatformTestContext context,
                    string discoveryUrl,
                    CancellationToken ct = default)
            {
                if (string.IsNullOrEmpty(discoveryUrl))
                {
                    context.OutputHelper.WriteLine($"{nameof(discoveryUrl)} is null or empty");
                    throw new ArgumentNullException(nameof(discoveryUrl));
                }

                var accessToken = await GetTokenAsync(context, ct).ConfigureAwait(false);

                using var client = new RestClient(context.IIoTPlatformConfigHubConfig.BaseUrl);

                var request = new RestRequest(TestConstants.APIRoutes.RegistryApplications, Method.Post)
                {
                    Timeout = TimeSpan.FromMilliseconds(TestConstants.DefaultTimeoutInMilliseconds)
                };
                request.AddHeader(TestConstants.HttpHeaderNames.Authorization, accessToken);

                var body = new { discoveryUrl };

                request.AddJsonBody(body);

                var response = await client.ExecuteAsync(request, ct).ConfigureAwait(false);
                Assert.NotNull(response);
                Assert.True(response.IsSuccessful, $"POST /registry/v2/applications failed ({response.StatusCode}, {response.ErrorMessage})!");
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
                    CancellationToken ct = default)
            {
                var accessToken = await GetTokenAsync(context, ct).ConfigureAwait(false);

                using var client = new RestClient(context.IIoTPlatformConfigHubConfig.BaseUrl);

                var request = new RestRequest(TestConstants.APIRoutes.RegistryApplications, Method.Get)
                {
                    Timeout = TimeSpan.FromMilliseconds(TestConstants.DefaultTimeoutInMilliseconds)
                };
                request.AddHeader(TestConstants.HttpHeaderNames.Authorization, accessToken);

                var response = await client.ExecuteAsync(request, ct).ConfigureAwait(false);
                Assert.NotNull(response);

                Assert.True(response.IsSuccessful, $"GET /registry/v2/application failed ({response.StatusCode}, {response.ErrorMessage})!");

                dynamic result = JsonConvert.DeserializeObject<ExpandoObject>(response.Content, new ExpandoObjectConverter());
                var json = (IDictionary<string, object>)result;

                Assert.True(HasProperty(result, "items"), "GET /registry/v2/application response did not contain items");
                Assert.False(result.items == null, "GET /registry/v2/application response items property is null");

                foreach (var item in result.items)
                {
                    var itemDictionary = (IDictionary<string, object>)item;

                    if (!itemDictionary.ContainsKey("discoveryUrls")
                        || !itemDictionary.ContainsKey("applicationId"))
                    {
                        continue;
                    }

                    var discoveryUrls = (List<object>)item.discoveryUrls;
                    var itemUrl = (string)discoveryUrls?.FirstOrDefault(url => IsUrlStringsEqual(url as string, discoveryUrl));

                    if (itemUrl != null)
                    {
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
                    CancellationToken ct = default)
            {
                var applicationId = context.ApplicationId;

                // Validate if an aplicationId was ever added to the context. If not try to get it from discovery Url
                if (string.IsNullOrEmpty(applicationId))
                {
                    applicationId = await GetApplicationIdAsync(context, discoveryUrl, ct).ConfigureAwait(false);
                }

                var accessToken = await GetTokenAsync(context, ct).ConfigureAwait(false);

                using var client = new RestClient(context.IIoTPlatformConfigHubConfig.BaseUrl);

                var resource = string.Format(CultureInfo.InvariantCulture, TestConstants.APIRoutes.RegistryApplicationsWithApplicationIdFormat, applicationId);
                var request = new RestRequest(resource, Method.Delete)
                {
                    Timeout = TimeSpan.FromMilliseconds(TestConstants.DefaultTimeoutInMilliseconds)
                };
                request.AddHeader(TestConstants.HttpHeaderNames.Authorization, accessToken);

                var response = await client.ExecuteAsync(request, ct).ConfigureAwait(false);
                Assert.NotNull(response);
                Assert.True(response.IsSuccessful, $"DELETE /registry/v2/application/{applicationId} failed ({response.StatusCode}, {response.ErrorMessage})!");
            }

            /// <summary>
            /// Remove everything from registry
            /// </summary>
            /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
            /// <param name="ct">Cancellation token</param>
            public static async Task RemoveAllApplicationsAsync(IIoTPlatformTestContext context,
                    CancellationToken ct = default)
            {
                while (true)
                {
                    dynamic json = await GetApplicationsInternalAsync(context, ct).ConfigureAwait(false);
                    Assert.True(HasProperty(json, "items"), "GET /registry/v2/applications response has no items property");
                    if (json.items == null || json.items.Count == 0)
                    {
                        return;
                    }
                    foreach (var item in json.items)
                    {
                        var id = item.applicationId?.ToString();
                        try
                        {
                            await RemoveApplicationAsync(context, id, ct);
                            context.OutputHelper.WriteLine($"Removed application {id}.");
                        }
                        catch (Exception ex)
                        {
                            context.OutputHelper.WriteLine($"Failed to remove application {id} -> {ex.Message}");
                        }
                    }
                }
            }

            /// <summary>
            /// Remove appolication
            /// </summary>
            /// <param name="context"></param>
            /// <param name="applicationId"></param>
            /// <param name="ct"></param>
            /// <exception cref="Exception"></exception>
            /// <exception cref="ResourceNotFoundException"></exception>
            public static async Task RemoveApplicationAsync(IIoTPlatformTestContext context, string applicationId, CancellationToken ct)
            {
                var route = $"{TestConstants.APIRoutes.RegistryApplications}/{applicationId}";
                var response = await CallRestApi(context, Method.Delete, route, ct: ct).ConfigureAwait(false);
                if (response.ErrorException != null)
                {
                    throw response.ErrorException;
                }
                if (!response.IsSuccessStatusCode)
                {
                    throw new ResourceNotFoundException(response.ToString());
                }
            }
        }
    }
}
