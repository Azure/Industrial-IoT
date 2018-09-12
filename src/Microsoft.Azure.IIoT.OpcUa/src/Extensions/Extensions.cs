// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa {
    using Microsoft.Azure.IIoT.OpcUa.Models;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Browse services extensions
    /// </summary>
    public static class Extensions {

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
                                ContinuationToken = result.ContinuationToken
                            });
                        result.References.AddRange(next.References);
                        result.ContinuationToken = next.ContinuationToken;
                    }
                    catch (Exception) {
                        // TODO: Catch other exceptions and continue, e.g. in case of timeout
                        throw;
                    }
                }
                return result;
            }
        }
    }
}
