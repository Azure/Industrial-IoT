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
    using Azure.IIoT.OpcUa.Core.Exceptions;
    using Azure.IIoT.OpcUa.Encoders.Utils;
    using Opc.Ua;
    using Opc.Ua.Extensions;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
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
            if (NodeIdCompat.IsNull(rootId))
            {
                rootId = ObjectIds.RootFolder;
            }
            var browsepaths = new BrowsePathCollection
            {
                new BrowsePath
                {
                    StartingNode = rootId,
                    RelativePath = paths.ToServiceRelativePath(session.MessageContext)
                }
            };
            var response = await session.Services.TranslateBrowsePathsToNodeIdsAsync(
                header.ToRequestHeader(timeProvider), browsepaths,
                ct).ConfigureAwait(false);
            Debug.Assert(response != null);
            var results = response.Validate(response.Results, r => r.StatusCode,
                response.DiagnosticInfos, browsepaths);
            var count = results[0].Result.Targets.Count;
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
            return ExpandedNodeId.ToNodeId(results[0].Result.Targets[0].TargetId,
                session.MessageContext.NamespaceUris);
        }

        /// <summary>
        /// Convert service browse path elements to a relative path.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        internal static RelativePath ToServiceRelativePath(this IReadOnlyList<string>? path,
            IServiceMessageContext context)
        {
            if (path == null)
            {
                return new RelativePath();
            }
            return new RelativePath
            {
                Elements = path
                    .Where(p => !string.IsNullOrEmpty(p))
                    .Select(p => ParsePathElement(p, context))
                    .ToArray()
            };
        }

        /// <summary>
        /// Parse a service browse path element.
        /// </summary>
        private static RelativePathElement ParsePathElement(string element,
            IServiceMessageContext context)
        {
            var pathElement = new RelativePathElement
            {
                IncludeSubtypes = true,
                IsInverse = false
            };
            var index = 0;
            while (index < element.Length)
            {
                if (element[index] == '!')
                {
                    pathElement.IsInverse = true;
                    index++;
                    continue;
                }
                if (element[index] == '#')
                {
                    pathElement.IncludeSubtypes = false;
                    index++;
                    continue;
                }
                break;
            }
            if (index < element.Length && element[index] == '<')
            {
                index++;
                while (index < element.Length)
                {
                    if (element[index] == '!')
                    {
                        pathElement.IsInverse = true;
                        index++;
                        continue;
                    }
                    if (element[index] == '#')
                    {
                        pathElement.IncludeSubtypes = false;
                        index++;
                        continue;
                    }
                    break;
                }
                var end = element.IndexOf('>', index);
                if (end < 0)
                {
                    throw new FormatException(
                        "Reference path starts in < but does not end in >");
                }
                var reference = element[index..end];
                pathElement.ReferenceTypeId = reference.ToNodeId(context);
                if (NodeIdCompat.IsNull(pathElement.ReferenceTypeId) &&
                    TypeMaps.ReferenceTypes.Value.TryGetIdentifier(reference, out var id))
                {
                    pathElement.ReferenceTypeId = new NodeId(id);
                }
                index = end + 1;
            }
            else if (index < element.Length && element[index] == '/')
            {
                pathElement.ReferenceTypeId = ReferenceTypeIds.HierarchicalReferences;
                index++;
            }
            else if (index < element.Length && element[index] == '.')
            {
                pathElement.ReferenceTypeId = ReferenceTypeIds.Aggregates;
                index++;
            }
            else
            {
                pathElement.ReferenceTypeId = ReferenceTypeIds.References;
            }
            var target = element[index..];
            if (string.IsNullOrEmpty(target))
            {
                throw new FormatException("Bad target name is empty");
            }
            pathElement.TargetName = target.ToQualifiedName(context);
            return pathElement;
        }
    }
}
