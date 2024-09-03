// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Furly.Extensions.Utils;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Node services extensions
    /// </summary>
    public static class NodeServicesEx
    {
        /// <summary>
        /// Browse all references if max references is null and user
        /// wants all. If user has requested maximum to return use
        /// <see cref="INodeServices{T}.BrowseFirstAsync"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="service"></param>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static async Task<BrowseFirstResponseModel> BrowseAsync<T>(
            this INodeServices<T> service, T connection, BrowseFirstRequestModel request,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(service);
            ArgumentNullException.ThrowIfNull(request);

            if (request.MaxReferencesToReturn != null)
            {
                return await service.BrowseFirstAsync(connection,
                    request, ct).ConfigureAwait(false);
            }
            var result = await service.BrowseFirstAsync(connection,
                request, ct).ConfigureAwait(false);
            var references = new List<NodeReferenceModel>();
            if (result.References != null)
            {
                references.AddRange(result.References);
            }
            var continuationToken = result.ContinuationToken;
            while (continuationToken != null)
            {
                try
                {
                    var next = await service.BrowseNextAsync(connection,
                        new BrowseNextRequestModel
                        {
                            ContinuationToken = continuationToken,
                            Header = request.Header,
                            NodeIdsOnly = request.NodeIdsOnly,
                            ReadVariableValues = request.ReadVariableValues,
                            TargetNodesOnly = request.TargetNodesOnly
                        }, ct).ConfigureAwait(false);
                    if (next.References != null)
                    {
                        references.AddRange(next.References);
                    }
                    continuationToken = next.ContinuationToken;
                }
                catch (Exception) when (continuationToken != null)
                {
                    await Try.Async(() => service.BrowseNextAsync(connection,
                        new BrowseNextRequestModel
                        {
                            ContinuationToken = continuationToken,
                            Abort = true
                        })).ConfigureAwait(false);
                    throw;
                }
            }
            return result with
            {
                References = references,
                ContinuationToken = null
            };
        }
    }
}
