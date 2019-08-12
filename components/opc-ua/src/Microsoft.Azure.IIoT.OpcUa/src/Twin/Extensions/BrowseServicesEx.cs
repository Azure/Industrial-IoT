// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Twin {
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Microsoft.Azure.IIoT.Utils;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Browse services extensions
    /// </summary>
    public static class BrowseServicesEx {

        /// <summary>
        /// Browse all references if max references is null and user
        /// wants all. If user has requested maximum to return use
        /// <see cref="IBrowseServices{T}.NodeBrowseFirstAsync"/>
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="service"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public static async Task<BrowseResultModel> NodeBrowseAsync<T>(
            this IBrowseServices<T> service, T endpoint, BrowseRequestModel request) {
            if (request.MaxReferencesToReturn != null) {
                return await service.NodeBrowseFirstAsync(endpoint, request);
            }
            while (true) {
                var result = await service.NodeBrowseFirstAsync(endpoint, request);
                while (result.ContinuationToken != null) {
                    try {
                        var next = await service.NodeBrowseNextAsync(endpoint,
                            new BrowseNextRequestModel {
                                ContinuationToken = result.ContinuationToken,
                                Header = request.Header,
                                NodeIdsOnly = request.NodeIdsOnly,
                                ReadVariableValues = request.ReadVariableValues,
                                TargetNodesOnly = request.TargetNodesOnly
                            });
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
    }
}
