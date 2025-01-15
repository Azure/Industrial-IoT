// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Services
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Parser;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Furly.Exceptions;
    using Opc.Ua;
    using Opc.Ua.Extensions;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Service Extensions
    /// </summary>
    internal static class Extensions
    {
        /// <summary>
        /// Resolve node id
        /// </summary>
        /// <param name="session"></param>
        /// <param name="header"></param>
        /// <param name="rootId"></param>
        /// <param name="browsePath"></param>
        /// <param name="paramName"></param>
        /// <param name="timeProvider"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<NodeId> ResolveNodeIdAsync(this IOpcUaSession session,
            RequestHeaderModel? header, string? rootId, IReadOnlyList<string>? browsePath,
            string paramName, TimeProvider timeProvider, CancellationToken ct = default)
        {
            var resolvedNodeId = rootId.ToNodeId(session.MessageContext);
            if (browsePath?.Count > 0)
            {
                resolvedNodeId = await session.ResolveBrowsePathToNodeAsync(header,
                    resolvedNodeId, [.. browsePath], paramName,
                    timeProvider, ct).ConfigureAwait(false);
            }
            return resolvedNodeId;
        }

        /// <summary>
        /// Resolve provided path to node.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="header"></param>
        /// <param name="rootId"></param>
        /// <param name="paths"></param>
        /// <param name="paramName"></param>
        /// <param name="timeProvider"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="ResourceNotFoundException"></exception>
        /// <exception cref="ResourceConflictException"></exception>
        public static async Task<NodeId> ResolveBrowsePathToNodeAsync(
            this IOpcUaSession session, RequestHeaderModel? header, NodeId rootId,
            string[] paths, string paramName, TimeProvider timeProvider,
            CancellationToken ct = default)
        {
            if (paths == null || paths.Length == 0)
            {
                return rootId;
            }
            if (NodeId.IsNull(rootId))
            {
                rootId = ObjectIds.RootFolder;
            }
            var browsepaths = new BrowsePathCollection
            {
                new BrowsePath
                {
                    StartingNode = rootId,
                    RelativePath = paths.ToRelativePath(session.MessageContext)
                }
            };
            var response = await session.Services.TranslateBrowsePathsToNodeIdsAsync(
                header.ToRequestHeader(timeProvider), browsepaths,
                ct).ConfigureAwait(false);
            Debug.Assert(response != null);
            var results = response.Validate(response.Results, r => r.StatusCode,
                response.DiagnosticInfos, browsepaths);
            var count = results[0].Result.Targets?.Count ?? 0;
            if (count == 0)
            {
                throw new ResourceNotFoundException(
                    $"{paramName} did not resolve to any node.");
            }
            if (count != 1)
            {
                throw new ResourceConflictException(
                    $"{paramName} resolved to {count} nodes.");
            }
            return results[0].Result.Targets[0].TargetId
                .ToNodeId(session.MessageContext.NamespaceUris);
        }
    }
}
