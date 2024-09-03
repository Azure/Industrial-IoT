// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.Sdk
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Furly.Extensions.Utils;
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Twin service api extensions
    /// </summary>
    public static class TwinServiceApiEx
    {
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
        public static async Task<BrowseFirstResponseModel> NodeBrowseAsync(
            this ITwinServiceApi service, string endpoint, BrowseFirstRequestModel request,
            CancellationToken ct = default)
        {
            if (request.MaxReferencesToReturn != null)
            {
                return await service.NodeBrowseFirstAsync(endpoint, request, ct).ConfigureAwait(false);
            }
            while (true)
            {
                // Limit size of batches to a reasonable default to avoid communication timeouts.
                request.MaxReferencesToReturn = 500;
                var result = await service.NodeBrowseFirstAsync(endpoint, request, ct).ConfigureAwait(false);
                var references = result.References.ToList();
                while (result.ContinuationToken != null)
                {
                    Debug.Assert(references != null);
                    try
                    {
                        var next = await service.NodeBrowseNextAsync(endpoint,
                            new BrowseNextRequestModel
                            {
                                ContinuationToken = result.ContinuationToken,
                                Header = request.Header,
                                ReadVariableValues = request.ReadVariableValues,
                                TargetNodesOnly = request.TargetNodesOnly
                            }, ct).ConfigureAwait(false);
                        references.AddRange(next.References);
                        result.ContinuationToken = next.ContinuationToken;
                    }
                    catch (Exception) when (result.ContinuationToken != null)
                    {
                        await Try.Async(() => service.NodeBrowseNextAsync(endpoint,
                            new BrowseNextRequestModel
                            {
                                ContinuationToken = result.ContinuationToken,
                                Abort = true
                            })).ConfigureAwait(false);
                        throw;
                    }
                }
                result.References = references;
                return result;
            }
        }
    }
}
