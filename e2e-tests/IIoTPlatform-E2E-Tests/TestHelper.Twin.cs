// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatformE2ETests
{
    using IIoTPlatformE2ETests.TestExtensions;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using RestSharp;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    internal static partial class TestHelper
    {
        /// <summary>
        /// Twin related helper methods
        /// </summary>
        public static class Twin
        {
            /// <summary>
            /// Equivalent to GetSetOfUniqueNodesAsync
            /// </summary>
            /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
            /// <param name="endpointId">Id of the endpoint as returned by <see cref="Registry_GetEndpoints(IIoTPlatformTestContext)"/></param>
            /// <param name="nodeId">Id of the parent node or null to browse the root node</param>
            /// <param name="ct">Cancellation token</param>
            /// <exception cref="ArgumentNullException"></exception>
            public static async Task<List<(string NodeId, string NodeClass, bool Children)>> GetBrowseEndpointAsync(
                    IIoTPlatformTestContext context,
                    string endpointId,
                    string nodeId = null,
                    CancellationToken ct = default)
            {
                if (string.IsNullOrEmpty(endpointId))
                {
                    context.OutputHelper.WriteLine($"{nameof(endpointId)} is null or empty");
                    throw new ArgumentNullException(nameof(endpointId));
                }

                var result = new List<(string NodeId, string NodeClass, bool Children)>();
                string continuationToken = null;

                do
                {
                    var browseResult = await GetBrowseEndpoint_InternalAsync(context, endpointId, nodeId, continuationToken, ct).ConfigureAwait(false);

                    if (browseResult.results.Count > 0)
                    {
                        result.AddRange(browseResult.results);
                    }

                    continuationToken = browseResult.continuationToken;
                } while (continuationToken != null);

                return result;
            }

            /// <summary>
            /// Calls a GET twin browse with the given <paramref name="endpointId"/>
            /// </summary>
            /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
            /// <param name="endpointId">Id of the endpoint as returned by <see cref="Registry_GetEndpoints(IIoTPlatformTestContext)"/></param>
            /// <param name="nodeId">Id of the parent node or null to browse the root node</param>
            /// <param name="continuationToken">Continuation token from the previous call, or null</param>
            /// <param name="ct">Cancellation token</param>
            /// <exception cref="Xunit.Sdk.XunitException"></exception>
            private static async Task<(List<(string NodeId, string NodeClass, bool Children)> results, string continuationToken)> GetBrowseEndpoint_InternalAsync(
                    IIoTPlatformTestContext context,
                    string endpointId,
                    string nodeId = null,
                    string continuationToken = null,
                    CancellationToken ct = default)
            {
                var accessToken = await GetTokenAsync(context, ct).ConfigureAwait(false);
                using var client = new RestClient(context.IIoTPlatformConfigHubConfig.BaseUrl);

                RestRequest request;
                if (continuationToken == null)
                {
                    request = new RestRequest($"twin/v2/browse/{endpointId}", Method.Get)
                    {
                        Timeout = TimeSpan.FromMilliseconds(TestConstants.DefaultTimeoutInMilliseconds)
                    };

                    if (!string.IsNullOrEmpty(nodeId))
                    {
                        request.AddQueryParameter("nodeId", nodeId);
                    }
                }
                else
                {
                    request = new RestRequest($"twin/v2/browse/{endpointId}/next", Method.Get)
                    {
                        Timeout = TimeSpan.FromMilliseconds(TestConstants.DefaultTimeoutInMilliseconds)
                    };
                    request.AddQueryParameter("continuationToken", continuationToken);
                }

                if (accessToken != null)
                {
                    request.AddHeader(TestConstants.HttpHeaderNames.Authorization, accessToken);
                }

                var response = await client.ExecuteAsync(request, ct).ConfigureAwait(false);

                Assert.NotNull(response);
                if (!response.IsSuccessful)
                {
                    context.OutputHelper.WriteLine($"StatusCode: {response.StatusCode}");
                    context.OutputHelper.WriteLine($"ErrorMessage: {response.ErrorMessage}");
                    throw new Xunit.Sdk.XunitException($"GET twin/v2/browse/{endpointId} of node {nodeId} failed (continuation: {continuationToken ?? "None"})!");
                }

                dynamic json = JsonConvert.DeserializeObject<ExpandoObject>(response.Content, new ExpandoObjectConverter());

                Assert.True(HasProperty(json, "references"), $"GET twin/v2/browse/{endpointId} of node {nodeId} response has no items");
                Assert.False(json.references == null, $"GET twin/v2/browse/{endpointId} of node {nodeId} response references property is null");

                var result = new List<(string NodeId, string NodeClass, bool Children)>();

                foreach (var node in json.references)
                {
                    try
                    {
                        var targetId = node.target.nodeId.ToString();
                        var nodeClass = node.target.nodeClass.ToString();
                        var children = node.target.children.ToString();
                        var hasChildren = (bool)string.Equals(children,
                            "true", StringComparison.OrdinalIgnoreCase);
                        result.Add((targetId, nodeClass, hasChildren));
                    }
                    catch
                    {
                        try
                        {
                            var errorInfo = node.target.errorInfo.symbolicId.ToString();
                            Assert.Equal("BadSecurityModeInsufficient", errorInfo);
                        }
                        catch
                        {
                            context.OutputHelper.WriteLine(
                                JsonConvert.SerializeObject(node, Formatting.Indented));
                            throw;
                        }
                    }
                }

                var responseContinuationToken = HasProperty(json, "continuationToken") ? json.continuationToken : null;

                return (results: result, continuationToken: responseContinuationToken);
            }

            /// <summary>
            /// Equivalent to recursive calling GetSetOfUniqueNodesAsync to get the whole hierarchy of nodes
            /// </summary>
            /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
            /// <param name="endpointId">Id of the endpoint as returned by <see cref="Registry_GetEndpoints(IIoTPlatformTestContext)"/></param>
            /// <param name="maxResult"></param>
            /// <param name="nodeClass">Class of the node to filter to or null for no filtering</param>
            /// <param name="nodeId">Id of the parent node or null to browse the root node</param>
            /// <param name="ct">Cancellation token</param>
            /// <exception cref="ArgumentNullException"></exception>
            public static async Task<List<(string NodeId, string NodeClass, bool Children)>> GetBrowseEndpointRecursiveAsync(
                IIoTPlatformTestContext context,
                string endpointId,
                int maxResult,
                string nodeClass = null,
                string nodeId = null,
                CancellationToken ct = default)
            {
                if (string.IsNullOrEmpty(endpointId))
                {
                    context.OutputHelper.WriteLine($"{nameof(endpointId)} is null or empty");
                    throw new ArgumentNullException(nameof(endpointId));
                }

                var nodes = new ConcurrentDictionary<string, (string NodeClass, bool Children)>();

                await GetBrowseEndpointRecursiveCollectResultsAsync(context, endpointId, nodes, maxResult, nodeId, ct).ConfigureAwait(false);

                return nodes
                    .Where(n => string.Equals(nodeClass, n.Value.NodeClass, StringComparison.OrdinalIgnoreCase))
                    .Select(n => (n.Key, n.Value.NodeClass, n.Value.Children))
                    .ToList();
            }

            /// <summary>
            /// Collects all nodes recursively avoiding circular references between nodes
            /// </summary>
            /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
            /// <param name="endpointId">Id of the endpoint as returned by <see cref="Registry_GetEndpoints(IIoTPlatformTestContext)"/></param>
            /// <param name="nodes">Collection of nodes found</param>
            /// <param name="maxResults"></param>
            /// <param name="nodeId">Id of the parent node or null to browse the root node</param>
            /// <param name="ct">Cancellation token</param>
            private static async Task GetBrowseEndpointRecursiveCollectResultsAsync(
                IIoTPlatformTestContext context,
                string endpointId,
                ConcurrentDictionary<string, (string NodeClass, bool Children)> nodes,
                int maxResults,
                string nodeId = null,
                CancellationToken ct = default)
            {
                var currentNodes = await GetBrowseEndpointAsync(context, endpointId, nodeId, ct).ConfigureAwait(false);

                foreach (var node in currentNodes)
                {
                    ct.ThrowIfCancellationRequested();
                    if (!nodes.TryAdd(node.NodeId, (node.NodeClass, node.Children)))
                    {
                        continue;
                    }
                    if (maxResults != -1 && nodes.Count >= maxResults)
                    {
                        return;
                    }
                    if (node.Children)
                    {
                        await GetBrowseEndpointRecursiveCollectResultsAsync(
                            context,
                            endpointId,
                            nodes,
                            maxResults,
                            node.NodeId,
                            ct).ConfigureAwait(false);
                    }
                }
            }

            /// <summary>
            /// Gets method metadata
            /// </summary>
            /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
            /// <param name="endpointId">Id of the endpoint as returned by <see cref="Registry_GetEndpoints(IIoTPlatformTestContext)"/></param>
            /// <param name="methodId">Id of the method to get the metadata of</param>
            /// <param name="ct">Cancellation token</param>
            public static async Task<dynamic> GetMethodMetadataAsync(
                    IIoTPlatformTestContext context,
                    string endpointId,
                    string methodId = null,
                    CancellationToken ct = default)
            {
                var accessToken = await GetTokenAsync(context, ct).ConfigureAwait(false);
                using var client = new RestClient(context.IIoTPlatformConfigHubConfig.BaseUrl);

                var request = new RestRequest($"twin/v2/call/{endpointId}/metadata", Method.Post)
                {
                    Timeout = TimeSpan.FromMilliseconds(TestConstants.DefaultTimeoutInMilliseconds)
                };
                if (accessToken != null)
                {
                    request.AddHeader(TestConstants.HttpHeaderNames.Authorization, accessToken);
                }

                var body = new
                {
                    methodId,
                    header = new
                    {
                        diagnostics = new
                        {
                            level = "Verbose"
                        }
                    }
                };

                request.AddJsonBody(body);

                var response = await client.ExecuteAsync(request, ct).ConfigureAwait(false);
                return JsonConvert.DeserializeObject<ExpandoObject>(response.Content, new ExpandoObjectConverter());
            }

            /// <summary>
            /// Reads node attributes
            /// </summary>
            /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
            /// <param name="endpointId">Id of the endpoint as returned by <see cref="Registry_GetEndpoints(IIoTPlatformTestContext)"/></param>
            /// <param name="attributes">Attributes to be read</param>
            /// <param name="ct">Cancellation token</param>
            /// <exception cref="Xunit.Sdk.XunitException"></exception>
            public static async Task<dynamic> ReadNodeAttributesAsync(
                    IIoTPlatformTestContext context,
                    string endpointId,
                    List<object> attributes,
                    CancellationToken ct = default)
            {
                var accessToken = await GetTokenAsync(context, ct).ConfigureAwait(false);
                using var client = new RestClient(context.IIoTPlatformConfigHubConfig.BaseUrl);

                var request = new RestRequest($"twin/v2/read/{endpointId}/attributes", Method.Post)
                {
                    Timeout = TimeSpan.FromMilliseconds(TestConstants.DefaultTimeoutInMilliseconds)
                };
                if (accessToken != null)
                {
                    request.AddHeader(TestConstants.HttpHeaderNames.Authorization, accessToken);
                }

                var body = new { attributes };

                request.AddJsonBody(body);

                var response = await client.ExecuteAsync(request, ct).ConfigureAwait(false);

                Assert.NotNull(response);
                if (!response.IsSuccessful)
                {
                    context.OutputHelper.WriteLine($"StatusCode: {response.StatusCode}");
                    context.OutputHelper.WriteLine($"ErrorMessage: {response.ErrorMessage}");
                    throw new Xunit.Sdk.XunitException("GET twin/v2/browse/{endpointId} failed!");
                }

                return JsonConvert.DeserializeObject<ExpandoObject>(response.Content, new ExpandoObjectConverter());
            }

            /// <summary>
            /// Writes node attributes
            /// </summary>
            /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
            /// <param name="endpointId">Id of the endpoint as returned by <see cref="Registry_GetEndpoints(IIoTPlatformTestContext)"/></param>
            /// <param name="attributes">Attributes to be written</param>
            /// <param name="ct">Cancellation token</param>
            /// <exception cref="Xunit.Sdk.XunitException"></exception>
            public static async Task<dynamic> WriteNodeAttributesAsync(
                    IIoTPlatformTestContext context,
                    string endpointId,
                    List<object> attributes,
                    CancellationToken ct = default)
            {
                var accessToken = await GetTokenAsync(context, ct).ConfigureAwait(false);
                using var client = new RestClient(context.IIoTPlatformConfigHubConfig.BaseUrl);

                var request = new RestRequest($"twin/v2/write/{endpointId}/attributes", Method.Post)
                {
                    Timeout = TimeSpan.FromMilliseconds(TestConstants.DefaultTimeoutInMilliseconds)
                };
                if (accessToken != null)
                {
                    request.AddHeader(TestConstants.HttpHeaderNames.Authorization, accessToken);
                }

                var body = new { attributes };

                request.AddJsonBody(body);

                var response = await client.ExecuteAsync(request, ct).ConfigureAwait(false);

                Assert.NotNull(response);
                if (!response.IsSuccessful)
                {
                    context.OutputHelper.WriteLine($"StatusCode: {response.StatusCode}");
                    context.OutputHelper.WriteLine($"ErrorMessage: {response.ErrorMessage}");
                    throw new Xunit.Sdk.XunitException("GET twin/v2/browse/{endpointId} failed!");
                }

                return JsonConvert.DeserializeObject<ExpandoObject>(response.Content, new ExpandoObjectConverter());
            }

            /// <summary>
            /// Calls a method
            /// </summary>
            /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
            /// <param name="endpointId">Id of the endpoint as returned by <see cref="Registry_GetEndpoints(IIoTPlatformTestContext)"/></param>
            /// <param name="methodId">Id of the method to call</param>
            /// <param name="objectId">Object ID</param>
            /// <param name="arguments">Method arguments</param>
            /// <param name="ct">Cancellation token</param>
            public static async Task<dynamic> CallMethodAsync(
                    IIoTPlatformTestContext context,
                    string endpointId,
                    string methodId,
                    string objectId,
                    List<object> arguments,
                    CancellationToken ct = default)
            {
                var accessToken = await GetTokenAsync(context, ct).ConfigureAwait(false);
                using var client = new RestClient(context.IIoTPlatformConfigHubConfig.BaseUrl);

                var request = new RestRequest($"twin/v2/call/{endpointId}", Method.Post)
                {
                    Timeout = TimeSpan.FromMilliseconds(TestConstants.DefaultTimeoutInMilliseconds)
                };
                if (accessToken != null)
                {
                    request.AddHeader(TestConstants.HttpHeaderNames.Authorization, accessToken);
                }

                var body = new
                {
                    methodId,
                    objectId,
                    arguments,
                    header = new
                    {
                        diagnostics = new
                        {
                            level = "Verbose"
                        }
                    }
                };

                request.AddJsonBody(body);

                var response = await client.ExecuteAsync(request, ct).ConfigureAwait(false);
                return JsonConvert.DeserializeObject<ExpandoObject>(response.Content, new ExpandoObjectConverter());
            }

            /// <summary>
            /// Browses nodes using a path from the specified node id
            /// </summary>
            /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
            /// <param name="endpointId">Id of the endpoint as returned by <see cref="Registry_GetEndpoints(IIoTPlatformTestContext)"/></param>
            /// <param name="nodeId">Node to browse from, if null defaults to root folder</param>
            /// <param name="browsePath">The paths to browse from node</param>
            /// <param name="ct">Cancellation token</param>
            /// <exception cref="Xunit.Sdk.XunitException"></exception>
            public static async Task<dynamic> GetBrowseNodePathAsync(
                    IIoTPlatformTestContext context,
                    string endpointId,
                    string nodeId,
                    List<string> browsePath,
                    CancellationToken ct = default)
            {
                var accessToken = await GetTokenAsync(context, ct).ConfigureAwait(false);
                using var client = new RestClient(context.IIoTPlatformConfigHubConfig.BaseUrl);

                var request = new RestRequest($"twin/v2/browse/{endpointId}/path", Method.Post)
                {
                    Timeout = TimeSpan.FromMilliseconds(TestConstants.DefaultTimeoutInMilliseconds)
                };
                if (accessToken != null)
                {
                    request.AddHeader(TestConstants.HttpHeaderNames.Authorization, accessToken);
                }

                var body = new
                {
                    nodeId,
                    browsePaths = new List<object> { browsePath }
                };

                request.AddJsonBody(body);

                var response = await client.ExecuteAsync(request, ct).ConfigureAwait(false);

                Assert.NotNull(response);
                if (!response.IsSuccessful)
                {
                    context.OutputHelper.WriteLine($"StatusCode: {response.StatusCode}");
                    context.OutputHelper.WriteLine($"ErrorMessage: {response.ErrorMessage}");
                    throw new Xunit.Sdk.XunitException("GET twin/v2/browse/{endpointId} failed!");
                }

                return JsonConvert.DeserializeObject<ExpandoObject>(response.Content, new ExpandoObjectConverter());
            }

            /// <summary>
            /// Reads the value of a node
            /// </summary>
            /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
            /// <param name="endpointId">Id of the endpoint as returned by <see cref="Registry_GetEndpoints(IIoTPlatformTestContext)"/></param>
            /// <param name="nodeId">Id of the node to read the value of</param>
            /// <param name="ct">Cancellation token</param>
            /// <exception cref="Xunit.Sdk.XunitException"></exception>
            public static async Task<(dynamic Value, string DataType)> ReadNodeValueAsync(
                    IIoTPlatformTestContext context,
                    string endpointId,
                    string nodeId,
                    CancellationToken ct = default)
            {
                var accessToken = await GetTokenAsync(context, ct).ConfigureAwait(false);

                using var client = new RestClient(context.IIoTPlatformConfigHubConfig.BaseUrl);

                var request = new RestRequest($"twin/v2/read/{endpointId}", Method.Post)
                {
                    Timeout = TimeSpan.FromMilliseconds(TestConstants.DefaultTimeoutInMilliseconds)
                };
                if (accessToken != null)
                {
                    request.AddHeader(TestConstants.HttpHeaderNames.Authorization, accessToken);
                }

                var body = new { nodeId };

                request.AddJsonBody(body);

                var response = await client.ExecuteAsync(request, ct).ConfigureAwait(false);
                Assert.NotNull(response);

                if (!response.IsSuccessful)
                {
                    context.OutputHelper.WriteLine($"StatusCode: {response.StatusCode}");
                    context.OutputHelper.WriteLine($"ErrorMessage: {response.ErrorMessage}");
                    throw new Xunit.Sdk.XunitException("POST twin/v2/read failed!");
                }

                dynamic json = JsonConvert.DeserializeObject<ExpandoObject>(response.Content, new ExpandoObjectConverter());

                Assert.True(TestHelper.HasProperty(json, "dataType"), "'dataType' was not found in the response body");

                if (TestHelper.HasProperty(json, "value"))
                {
                    return (Value: json.value, DataType: json.dataType);
                }

                return (Value: default, DataType: json.dataType);
            }

            /// <summary>
            /// Reads the value of a node
            /// </summary>
            /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
            /// <param name="endpointId">Id of the endpoint as returned by <see cref="Registry_GetEndpoints(IIoTPlatformTestContext)"/></param>
            /// <param name="nodeId">Id of the node to read the value of</param>
            /// <param name="value"></param>
            /// <param name="dataType"></param>
            /// <param name="ct">Cancellation token</param>
            /// <exception cref="Xunit.Sdk.XunitException"></exception>
            public static async Task WriteNodeValueAsync(
                    IIoTPlatformTestContext context,
                    string endpointId,
                    string nodeId,
                    dynamic value,
                    string dataType,
                    CancellationToken ct = default)
            {
                var accessToken = await GetTokenAsync(context, ct).ConfigureAwait(false);

                using var client = new RestClient(context.IIoTPlatformConfigHubConfig.BaseUrl);

                var request = new RestRequest($"twin/v2/write/{endpointId}", Method.Post)
                {
                    Timeout = TimeSpan.FromMilliseconds(TestConstants.DefaultTimeoutInMilliseconds)
                };
                if (accessToken != null)
                {
                    request.AddHeader(TestConstants.HttpHeaderNames.Authorization, accessToken);
                }

                var body = new { nodeId, value, dataType };

                request.AddJsonBody(body);

                var response = await client.ExecuteAsync(request, ct).ConfigureAwait(false);
                Assert.NotNull(response);

                if (!response.IsSuccessful)
                {
                    context.OutputHelper.WriteLine($"StatusCode: {response.StatusCode}");
                    context.OutputHelper.WriteLine($"ErrorMessage: {response.ErrorMessage}");
                    throw new Xunit.Sdk.XunitException("POST twin/v2/read failed!");
                }
            }
        }
    }
}
