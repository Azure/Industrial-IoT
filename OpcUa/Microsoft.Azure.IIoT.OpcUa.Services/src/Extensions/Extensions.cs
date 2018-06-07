// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Services {
    using Microsoft.Azure.IIoT.OpcUa.Services.Models;
    using System;
    using System.Threading.Tasks;

    public static class Extensions {

        /// <summary>
        /// Browse all items regardless of continuation.
        /// </summary>
        /// <param name="service"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public static async Task<BrowseResultModel> NodeBrowseAsync<T>(
            this IOpcUaBrowseServices<T> service, T endpoint, BrowseRequestModel request) {
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
