// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Services.Sdk {
    using Azure.IIoT.OpcUa.Shared.Models;
    using Microsoft.Azure.IIoT.Utils;
    using System;
    using System.Threading;
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
        /// <param name="service"></param>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<BrowseResponseModel> NodeBrowseAsync(
            this ITwinServiceApi service, string endpoint, BrowseRequestModel request,
            CancellationToken ct = default) {
            if (request.MaxReferencesToReturn != null) {
                return await service.NodeBrowseFirstAsync(endpoint, request, ct);
            }
            while (true) {
                // Limit size of batches to a reasonable default to avoid communication timeouts.
                request.MaxReferencesToReturn = 500;
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
    }
}
