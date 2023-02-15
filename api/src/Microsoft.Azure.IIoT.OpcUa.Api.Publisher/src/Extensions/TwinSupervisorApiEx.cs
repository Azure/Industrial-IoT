// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Extensions {
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher;
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models;
    using Microsoft.Azure.IIoT.Utils;
    using System;
    using System.Threading;
    using System.Threading.Tasks;
#if ZOMBIE

    /// <summary>
    /// Api extensions
    /// </summary>
    public static class TwinSupervisorApiEx {
#if ZOMBIE
#if ZOMBIE

        /// <summary>
        /// Browse node on endpoint
        /// </summary>
        /// <param name="api"></param>
        /// <param name="endpointUrl"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static Task<BrowseResponseApiModel> NodeBrowseAsync(
            this ITwinModuleApi api, string endpointUrl, BrowseRequestApiModel request,
            CancellationToken ct = default) {
            return api.NodeBrowseAsync(ConnectionTo(endpointUrl), request, ct);
        }
#endif
#if ZOMBIE

        /// <summary>
        /// Browse node on endpoint
        /// </summary>
        /// <param name="api"></param>
        /// <param name="endpointUrl"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static Task<BrowseResponseApiModel> NodeBrowseFirstAsync(
            this ITwinModuleApi api, string endpointUrl, BrowseRequestApiModel request,
            CancellationToken ct = default) {
            return api.NodeBrowseFirstAsync(new ConnectionApiModel {
                Endpoint = new EndpointApiModel {
                    Url = endpointUrl,
                    SecurityMode = SecurityMode.None
                }
            }, request, ct);
        }
#endif
#if ZOMBIE

        /// <summary>
        /// Browse next references on endpoint
        /// </summary>
        /// <param name="api"></param>
        /// <param name="endpointUrl"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static Task<BrowseNextResponseApiModel> NodeBrowseNextAsync(
            this ITwinModuleApi api, string endpointUrl, BrowseNextRequestApiModel request,
            CancellationToken ct = default) {
            return api.NodeBrowseNextAsync(new ConnectionApiModel {
                Endpoint = new EndpointApiModel {
                    Url = endpointUrl,
                    SecurityMode = SecurityMode.None
                }
            }, request, ct);
        }
#endif
#if ZOMBIE

        /// <summary>
        /// Browse by path on endpoint
        /// </summary>
        /// <param name="api"></param>
        /// <param name="endpointUrl"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static Task<BrowsePathResponseApiModel> NodeBrowsePathAsync(
            this ITwinModuleApi api, string endpointUrl, BrowsePathRequestApiModel request,
            CancellationToken ct = default) {
            return api.NodeBrowsePathAsync(ConnectionTo(endpointUrl), request, ct);
        }
#endif
#if ZOMBIE

        /// <summary>
        /// Call method on endpoint
        /// </summary>
        /// <param name="api"></param>
        /// <param name="endpointUrl"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static Task<MethodCallResponseApiModel> NodeMethodCallAsync(
            this ITwinModuleApi api, string endpointUrl, MethodCallRequestApiModel request,
            CancellationToken ct = default) {
            return api.NodeMethodCallAsync(ConnectionTo(endpointUrl), request, ct);
        }
#endif
#if ZOMBIE

        /// <summary>
        /// Get meta data for method call on endpoint
        /// </summary>
        /// <param name="api"></param>
        /// <param name="endpointUrl"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static Task<MethodMetadataResponseApiModel> NodeMethodGetMetadataAsync(
            this ITwinModuleApi api, string endpointUrl, MethodMetadataRequestApiModel request,
            CancellationToken ct = default) {
            return api.NodeMethodGetMetadataAsync(ConnectionTo(endpointUrl), request, ct);
        }
#endif
#if ZOMBIE

        /// <summary>
        /// Read node value on endpoint
        /// </summary>
        /// <param name="api"></param>
        /// <param name="endpointUrl"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static Task<ValueReadResponseApiModel> NodeValueReadAsync(
            this ITwinModuleApi api, string endpointUrl, ValueReadRequestApiModel request,
            CancellationToken ct = default) {
            return api.NodeValueReadAsync(ConnectionTo(endpointUrl), request, ct);
        }
#endif
#if ZOMBIE

        /// <summary>
        /// Write node value on endpoint
        /// </summary>
        /// <param name="api"></param>
        /// <param name="endpointUrl"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static Task<ValueWriteResponseApiModel> NodeValueWriteAsync(
            this ITwinModuleApi api, string endpointUrl, ValueWriteRequestApiModel request,
            CancellationToken ct = default) {
            return api.NodeValueWriteAsync(ConnectionTo(endpointUrl), request, ct);
        }
#endif
#if ZOMBIE

        /// <summary>
        /// Read node attributes on endpoint
        /// </summary>
        /// <param name="api"></param>
        /// <param name="endpointUrl"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static Task<ReadResponseApiModel> NodeReadAsync(
            this ITwinModuleApi api, string endpointUrl, ReadRequestApiModel request,
            CancellationToken ct = default) {
            return api.NodeReadAsync(ConnectionTo(endpointUrl), request, ct);
        }
#endif
#if ZOMBIE

        /// <summary>
        /// Write node attributes on endpoint
        /// </summary>
        /// <param name="api"></param>
        /// <param name="endpointUrl"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static Task<WriteResponseApiModel> NodeWriteAsync(
            this ITwinModuleApi api, string endpointUrl, WriteRequestApiModel request,
            CancellationToken ct = default) {
            return api.NodeWriteAsync(ConnectionTo(endpointUrl), request, ct);
        }
#endif

        /// <summary>
        /// Browse all references if max references == null and user
        /// wants all. If user has requested maximum to return uses
        /// <see cref="ITwinModuleApi.NodeBrowseFirstAsync"/>
        /// </summary>
        /// <param name="service"></param>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<BrowseResponseApiModel> NodeBrowseAsync(
            this ITwinModuleApi service, ConnectionApiModel endpoint,
            BrowseRequestApiModel request, CancellationToken ct = default) {
            if (request.MaxReferencesToReturn != null) {
                return await service.NodeBrowseFirstAsync(endpoint, request, ct);
            }
            while (true) {
                var result = await service.NodeBrowseFirstAsync(endpoint, request, ct);
                while (result.ContinuationToken != null) {
                    try {
                        var next = await service.NodeBrowseNextAsync(endpoint,
                            new BrowseNextRequestApiModel {
                                ContinuationToken = result.ContinuationToken,
                                Header = request.Header,
                                ReadVariableValues = request.ReadVariableValues,
                                TargetNodesOnly = request.TargetNodesOnly
                            }, ct);
                        result.References.AddRange(next.References);
                        result.ContinuationToken = next.ContinuationToken;
                    }
                    catch (Exception) {
                        await Try.Async(() => service.NodeBrowseNextAsync(endpoint,
                            new BrowseNextRequestApiModel {
                                ContinuationToken = result.ContinuationToken,
                                Abort = true
                            }));
                        throw;
                    }
                }
                return result;
            }
        }
#endif
#if ZOMBIE

        /// <summary>
        /// New endpoint
        /// </summary>
        /// <param name="endpointUrl"></param>
        /// <returns></returns>
        private static ConnectionApiModel ConnectionTo(string endpointUrl) {
            return new ConnectionApiModel {
                Endpoint = new EndpointApiModel {
                    Url = endpointUrl,
                    SecurityMode = SecurityMode.None,
                    SecurityPolicy = "None"
                }
            };
        }
#endif
    }
#endif
}
