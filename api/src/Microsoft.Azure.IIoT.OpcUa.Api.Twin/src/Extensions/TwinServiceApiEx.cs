// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Twin {
    using Microsoft.Azure.IIoT.OpcUa.Api.Twin.Models;
    using Microsoft.Azure.IIoT.Utils;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Twin service api extensions
    /// </summary>
    public static class TwinServiceApiEx {

        /// <summary>
        /// Browse all references if max references is null and user
        /// wants all. If user has requested maximum to return uses
        /// <see cref="ITwinServiceApi.NodeBrowseFirstAsync"/>
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="service"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public static async Task<BrowseResponseApiModel> NodeBrowseAsync(
            this ITwinServiceApi service, string endpoint, BrowseRequestApiModel request) {
            if (request.MaxReferencesToReturn != null) {
                return await service.NodeBrowseFirstAsync(endpoint, request);
            }
            while (true) {
                var result = await service.NodeBrowseFirstAsync(endpoint, request);
                while (result.ContinuationToken != null) {
                    try {
                        var next = await service.NodeBrowseNextAsync(endpoint,
                            new BrowseNextRequestApiModel {
                                ContinuationToken = result.ContinuationToken,
                                Header = request.Header,
                                ReadVariableValues = request.ReadVariableValues,
                                TargetNodesOnly = request.TargetNodesOnly
                            });
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

        /// <summary>
        /// Get list of published nodes
        /// </summary>
        /// <param name="service"></param>
        /// <param name="endpointId"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<PublishedItemApiModel>> NodePublishListAllAsync(
            this ITwinServiceApi service, string endpointId) {
            var nodes = new List<PublishedItemApiModel>();
            var result = await service.NodePublishListAsync(endpointId);
            nodes.AddRange(result.Items);
            while (result.ContinuationToken != null) {
                result = await service.NodePublishListAsync(endpointId,
                    result.ContinuationToken);
                nodes.AddRange(result.Items);
            }
            return nodes;
        }

        /// <summary>
        /// Get list of published nodes
        /// </summary>
        /// <param name="service"></param>
        /// <param name="endpointId"></param>
        /// <param name="continuation"></param>
        /// <returns></returns>
        public static Task<PublishedItemListResponseApiModel> NodePublishListAsync(
            this ITwinServiceApi service, string endpointId, string continuation = null) {
            return service.NodePublishListAsync(endpointId, new PublishedItemListRequestApiModel {
                ContinuationToken = continuation
            });
        }
    }
}
