// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Api {
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
        public static Task<BrowseResponseModel> NodeBrowseAsync(
            this ITwinModuleApi api, string endpointUrl, BrowseRequestModel request,
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
        public static Task<BrowseResponseModel> NodeBrowseFirstAsync(
            this ITwinModuleApi api, string endpointUrl, BrowseRequestModel request,
            CancellationToken ct = default) {
            return api.NodeBrowseFirstAsync(new ConnectionModel {
                Endpoint = new EndpointModel {
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
        public static Task<BrowseNextResponseModel> NodeBrowseNextAsync(
            this ITwinModuleApi api, string endpointUrl, BrowseNextRequestModel request,
            CancellationToken ct = default) {
            return api.NodeBrowseNextAsync(new ConnectionModel {
                Endpoint = new EndpointModel {
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
        public static Task<BrowsePathResponseModel> NodeBrowsePathAsync(
            this ITwinModuleApi api, string endpointUrl, BrowsePathRequestModel request,
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
        public static Task<MethodCallResponseModel> NodeMethodCallAsync(
            this ITwinModuleApi api, string endpointUrl, MethodCallRequestModel request,
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
        public static Task<MethodMetadataResponseModel> NodeMethodGetMetadataAsync(
            this ITwinModuleApi api, string endpointUrl, MethodMetadataRequestModel request,
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
        public static Task<ValueReadResponseModel> NodeValueReadAsync(
            this ITwinModuleApi api, string endpointUrl, ValueReadRequestModel request,
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
        public static Task<ValueWriteResponseModel> NodeValueWriteAsync(
            this ITwinModuleApi api, string endpointUrl, ValueWriteRequestModel request,
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
        public static Task<ReadResponseModel> NodeReadAsync(
            this ITwinModuleApi api, string endpointUrl, ReadRequestModel request,
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
        public static Task<WriteResponseModel> NodeWriteAsync(
            this ITwinModuleApi api, string endpointUrl, WriteRequestModel request,
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
        public static async Task<BrowseResponseModel> NodeBrowseAsync(
            this ITwinModuleApi service, ConnectionModel endpoint,
            BrowseRequestModel request, CancellationToken ct = default) {
            if (request.MaxReferencesToReturn != null) {
                return await service.NodeBrowseFirstAsync(endpoint, request, ct);
            }
            while (true) {
                var result = await service.NodeBrowseFirstAsync(endpoint, request, ct);
                while (result.ContinuationToken != null) {
                    try {
                        var next = await service.NodeBrowseNextAsync(endpoint,
                            new BrowseNextRequestModel {
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
                            new BrowseNextRequestModel {
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
        private static ConnectionModel ConnectionTo(string endpointUrl) {
            return new ConnectionModel {
                Endpoint = new EndpointModel {
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
