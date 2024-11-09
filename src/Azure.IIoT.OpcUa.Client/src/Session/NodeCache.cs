// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client
{
    using Opc.Ua;
    using Opc.Ua.Export;
    using BitFaster.Caching;
    using BitFaster.Caching.Lfu;
    using BitFaster.Caching.Lru;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Node cache inside the session object. The node cache consists of two LRU caches.
    /// These caches contain nodes and references (Read and browse). Cache entries expire
    /// after not being accessed for a certain time. The cache is thread-safe and can be
    /// accessed by multiple readers. It is also limited to a capacity at which point
    /// least recently used entries will be evicted.
    /// </summary>
    internal sealed class NodeCache : INodeCache
    {
        /// <summary>
        /// Create cache
        /// </summary>
        /// <param name="session"></param>
        /// <param name="cacheExpiry"></param>
        /// <param name="capacity"></param>
        public NodeCache(INodeCacheContext session, TimeSpan? cacheExpiry = null,
            int capacity = 4096)
        {
            cacheExpiry ??= TimeSpan.FromMinutes(5);

            _session = session;
            _nodes = new ConcurrentLruBuilder<NodeId, Node>()
                   .WithAtomicGetOrAdd()
                   .AsAsyncCache()
                   .WithCapacity(capacity)
                   .WithKeyComparer(Comparers.Instance)
                   .WithExpireAfterAccess(cacheExpiry.Value)
                   .Build();
            _refs = new ConcurrentLruBuilder<NodeId, List<ReferenceDescription>>()
                   .WithAtomicGetOrAdd()
                   .AsAsyncCache()
                   .WithCapacity(capacity)
                   .WithKeyComparer(Comparers.Instance)
                   .WithExpireAfterAccess(cacheExpiry.Value)
                   .Build();
        }

        /// <inheritdoc/>
        public ValueTask<INode> FindAsync(NodeId nodeId, CancellationToken ct)
        {
            if (_nodes.TryGet(nodeId, out var node))
            {
                return ValueTask.FromResult<INode>(node);
            }
            return FindAsyncCore(nodeId, ct);

            async ValueTask<INode> FindAsyncCore(NodeId nodeId, CancellationToken ct)
                => await GetOrAddNodeAsync(nodeId, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public ValueTask<IReadOnlyList<INode>> FindAsync(IReadOnlyList<NodeId> nodeIds,
            CancellationToken ct)
        {
            var count = nodeIds.Count;
            var result = new List<Node?>(nodeIds.Count);
            if (count != 0)
            {
                var notFound = new List<NodeId>();
                foreach (var nodeId in nodeIds)
                {
                    if (_nodes.TryGet(nodeId, out var node))
                    {
                        result.Add(node);
                        continue;
                    }
                    notFound.Add(nodeId);
                    result.Add(null);
                }
                if (notFound.Count != 0)
                {
                    return FetchRemainingNodesAsyncCore(notFound, result, ct);
                    async ValueTask<IReadOnlyList<INode>> FetchRemainingNodesAsyncCore(
                        IReadOnlyList<NodeId> remainingIds, List<Node?> sparse,
                        CancellationToken ct)
                    {
                        var nodes = (List<Node>)await FetchRemainingNodesAsync(
                            remainingIds, sparse, ct).ConfigureAwait(false);
                        return nodes.ConvertAll(c => (INode)c);
                    }
                }
            }
            return ValueTask.FromResult<IReadOnlyList<INode>>(result
                .ConvertAll(c => (INode)c!));
        }

        /// <inheritdoc/>
        public ValueTask<IReadOnlyList<INode>> FindReferencesAsync(NodeId nodeId,
            NodeId referenceTypeId, bool isInverse, bool includeSubtypes,
            CancellationToken ct)
        {
            if ((!includeSubtypes || IsTypeHierarchyLoaded(new[] { referenceTypeId })) &&
                _refs.TryGet(nodeId, out var references))
            {
                return FindAsync(FilterNodes(references, isInverse, referenceTypeId,
                    includeSubtypes), ct);
            }
            return FindReferencesAsyncCore(nodeId, referenceTypeId, isInverse,
                includeSubtypes, ct);

            async ValueTask<IReadOnlyList<INode>> FindReferencesAsyncCore(NodeId nodeId,
                NodeId referenceTypeId, bool isInverse, bool includeSubtypes,
                CancellationToken ct)
            {
                if (includeSubtypes)
                {
                    await LoadTypeHierarchyAync(new[] { referenceTypeId }, ct).ConfigureAwait(false);
                }
                var references = await GetOrAddReferencesAsync(nodeId,
                    ct).ConfigureAwait(false);
                return await FindAsync(FilterNodes(references, isInverse, referenceTypeId,
                    includeSubtypes), ct).ConfigureAwait(false);
            }

            List<NodeId> FilterNodes(IEnumerable<ReferenceDescription> references,
                bool isInverse, NodeId refTypeId, bool includeSubtypes)
            {
                return references
                    .Where(r => r.IsForward == !isInverse &&
                        (r.ReferenceTypeId == refTypeId ||
                            (includeSubtypes && IsTypeOf(r.ReferenceTypeId, refTypeId))))
                    .Select(r => ToNodeId(r.NodeId))
                    .Where(n => !NodeId.IsNull(n))
                    .ToList();
            }
        }

        /// <inheritdoc/>
        public ValueTask<IReadOnlyList<INode>> FindReferencesAsync(
            IReadOnlyList<NodeId> nodeIds, IReadOnlyList<NodeId> referenceTypeIds,
            bool isInverse, bool includeSubtypes, CancellationToken ct)
        {
            var targetIds = new List<NodeId>();
            var notFound = new List<NodeId>();
            if (includeSubtypes && !IsTypeHierarchyLoaded(referenceTypeIds))
            {
                return FindReferencesAsyncCore(notFound, referenceTypeIds, isInverse,
                    includeSubtypes, targetIds, ct);
            }
            foreach (var nodeId in nodeIds)
            {
                if (NodeId.IsNull(nodeId))
                {
                    continue;
                }
                if (_refs.TryGet(nodeId, out var references))
                {
                    targetIds.AddRange(FilterNodes(references, isInverse, referenceTypeIds,
                        includeSubtypes));
                }
                else
                {
                    notFound.Add(nodeId);
                }
            }
            if (notFound.Count != 0)
            {
                return FindReferencesAsyncCore(notFound, referenceTypeIds, isInverse,
                    includeSubtypes, targetIds, ct);
            }
            return FindAsync(targetIds, ct);

            async ValueTask<IReadOnlyList<INode>> FindReferencesAsyncCore(
                IReadOnlyList<NodeId> nodeIds, IReadOnlyList<NodeId> referenceTypeIds,
                bool isInverse, bool includeSubtypes, List<NodeId> targetIds,
                CancellationToken ct)
            {
                if (includeSubtypes)
                {
                    await LoadTypeHierarchyAync(referenceTypeIds, ct).ConfigureAwait(false);
                }
                foreach (var nodeId in nodeIds)
                {
                    var references = await GetOrAddReferencesAsync(nodeId,
                        ct).ConfigureAwait(false);
                    targetIds.AddRange(FilterNodes(references, isInverse, referenceTypeIds,
                        includeSubtypes));
                }
                return await FindAsync(targetIds, ct).ConfigureAwait(false);
            }
            List<NodeId> FilterNodes(IEnumerable<ReferenceDescription> references,
                bool isInverse, IReadOnlyList<NodeId> referenceTypeIds, bool includeSubtypes)
            {
                return references
                    .Where(r => r.IsForward == !isInverse &&
                        referenceTypeIds.Any(refTypeId => r.ReferenceTypeId == refTypeId ||
                            (includeSubtypes && IsTypeOf(r.ReferenceTypeId, refTypeId))))
                    .Select(r => ToNodeId(r.NodeId))
                    .Where(n => !NodeId.IsNull(n))
                    .ToList();
            }
        }

        /// <inheritdoc/>
        public ValueTask<Node> FetchNodeAsync(NodeId nodeId, CancellationToken ct)
        {
            if (_nodes.TryGet(nodeId, out var node))
            {
                if (node.ReferenceTable.Count == 0)
                {
                    return ValueTask.FromResult(node);
                }
                if (_refs.TryGet(nodeId, out var references))
                {
                    AddReferencesToNode(node, references);
                    return ValueTask.FromResult(node);
                }
            }
            return FetchNodeAsyncCore(nodeId, ct);

            async ValueTask<Node> FetchNodeAsyncCore(NodeId nodeId, CancellationToken ct)
            {
                var node = await GetOrAddNodeAsync(nodeId, ct).ConfigureAwait(false);
                if (node.ReferenceTable.Count == 0)
                {
                    var references = await GetOrAddReferencesAsync(nodeId,
                        ct).ConfigureAwait(false);
                    AddReferencesToNode(node, references);
                }
                return node;
            }
        }
#if ZOMBIE

        /// <inheritdoc/>
        public ValueTask<IReadOnlyList<Node>> FetchNodesAsync(
            IReadOnlyList<NodeId> nodeIds, CancellationToken ct)
        {
            var count = nodeIds.Count;
            var result = new List<Node?>(nodeIds.Count);
            if (count != 0)
            {
                var notFound = new List<NodeId>();
                foreach (var nodeId in nodeIds)
                {
                    if (_nodes.TryGet(nodeId, out var node))
                    {
                        if (node.ReferenceTable.Count != 0)
                        {
                            result.Add(node);
                            continue;
                        }

                        if (_refs.TryGet(nodeId, out var references))
                        {
                            AddReferencesToNode(node, references);
                            result.Add(node);
                            continue;
                        }
                    }
                    notFound.Add(nodeId);
                    result.Add(null);
                }
                if (notFound.Count != 0)
                {
                    return FetchRemainingNodesAsync(notFound, result, ct);
                }
            }
            return ValueTask.FromResult<IReadOnlyList<Node>>(result!);
        }
#endif

        /// <inheritdoc/>
        public async ValueTask LoadTypeHierarchyAync(IReadOnlyList<NodeId> typeIds,
            CancellationToken ct)
        {
            var nodes = await FindReferencesAsync(typeIds, new []
            {
                ReferenceTypeIds.HasSubtype
            },false, false, ct).ConfigureAwait(false);
            if (nodes.Count > 0)
            {
                if (nodes is not List<INode> subTypes)
                {
                    subTypes = nodes.ToList();
                }
                await LoadTypeHierarchyAync(subTypes.ConvertAll(n => ToNodeId(n.NodeId)),
                    ct).ConfigureAwait(false);
            }
        }

        /// <inheritdoc/>
        public bool IsTypeOf(NodeId subTypeId, NodeId superTypeId)
        {
            if (subTypeId == superTypeId)
            {
                return true;
            }
            if (!_refs.TryGet(subTypeId, out var references))
            {
                // block - we can throw here but user should load
                references = GetOrAddReferencesAsync(subTypeId, default)
                    .AsTask().GetAwaiter().GetResult();
            }
            subTypeId = GetSuperTypeFromReferences(references);
            if (!NodeId.IsNull(subTypeId))
            {
                return IsTypeOf(subTypeId, superTypeId);
            }
            return false;
        }

        /// <inheritdoc/>
        public ValueTask<NodeId> FindSuperTypeAsync(NodeId typeId,
            CancellationToken ct)
        {
            if (_refs.TryGet(typeId, out var references))
            {
                return ValueTask.FromResult(GetSuperTypeFromReferences(references));
            }
            return FindSuperTypeAsyncCore(typeId, ct);
            async ValueTask<NodeId> FindSuperTypeAsyncCore(NodeId typeId,
                CancellationToken ct)
            {
                var references = await GetOrAddReferencesAsync(typeId,
                    ct).ConfigureAwait(false);
                return GetSuperTypeFromReferences(references);
            }
        }

        /// <inheritdoc/>
        public async ValueTask<BuiltInType> GetBuiltInTypeAsync(NodeId datatypeId,
            CancellationToken ct)
        {
            var typeId = datatypeId;
            while (!Opc.Ua.NodeId.IsNull(typeId))
            {
                if (typeId.NamespaceIndex == 0 && typeId.IdType == Opc.Ua.IdType.Numeric)
                {
                    BuiltInType id = (BuiltInType)(int)(uint)typeId.Identifier;
                    if (id > BuiltInType.Null &&
                        id <= BuiltInType.Enumeration &&
                        id != BuiltInType.DiagnosticInfo)
                    {
                        return id;
                    }
                }
                typeId = await FindSuperTypeAsync(typeId, ct).ConfigureAwait(false);
            }
            return BuiltInType.Null;
        }

        /// <inheritdoc/>
        public async ValueTask<INode?> FindNodeWithBrowsePathAsync(NodeId nodeId,
            QualifiedNameCollection browsePath, CancellationToken ct)
        {
            INode? found = null;
            foreach (var browseName in browsePath)
            {
                found = null;
                while (true)
                {
                    if (Opc.Ua.NodeId.IsNull(nodeId))
                    {
                        // Nothing can be found since there is no
                        return null;
                    }

                    //
                    // Get all hierarchical references of the node and
                    // match browse name
                    //
                    var references = await FindReferencesAsync(nodeId,
                        ReferenceTypeIds.HierarchicalReferences,
                        false, true, ct).ConfigureAwait(false);
                    foreach (var target in references)
                    {
                        if (target.BrowseName == browseName)
                        {
                            nodeId = ToNodeId(target.NodeId);
                            if (!NodeId.IsNull(nodeId))
                            {
                                found = target;
                            }
                            break;
                        }
                    }

                    if (found != null)
                    {
                        break;
                    }
                    // Try find name in super type
                    nodeId = await FindSuperTypeAsync(nodeId,
                        ct).ConfigureAwait(false);
                }
                nodeId = ToNodeId(found.NodeId);
            }
            return found;
        }

        /// <inheritdoc/>
        public void Clear()
        {
            _nodes.Clear();
            _refs.Clear();
        }

        /// <summary>
        /// Get or add node to the cache
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private ValueTask<Node> GetOrAddNodeAsync(NodeId nodeId, CancellationToken ct)
        {
            Debug.Assert(!NodeId.IsNull(nodeId));
            return _nodes.GetOrAddAsync(nodeId, async (nodeId, context) =>
                await context.session.ReadNodeAsync(nodeId, context.ct).ConfigureAwait(false),
                (session: _session, ct));
        }

        /// <summary>
        /// Get or add references to cache
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private ValueTask<List<ReferenceDescription>> GetOrAddReferencesAsync(
            NodeId nodeId, CancellationToken ct)
        {
            Debug.Assert(!NodeId.IsNull(nodeId));
            return _refs.GetOrAddAsync(nodeId, async (nodeId, context) =>
            {
                var references = await context.session.FetchReferencesAsync(nodeId,
                    context.ct).ConfigureAwait(false);
                foreach (var reference in references)
                {
                    // transform absolute identifiers.
                    if (reference.NodeId?.IsAbsolute == true)
                    {
                        reference.NodeId = ExpandedNodeId.ToNodeId(
                            reference.NodeId, context.session.NamespaceUris);
                    }
                }
                return references;
            }, (session: _session, ct));
        }

        /// <summary>
        /// Fetch remaining nodes not yet in the result list
        /// </summary>
        /// <param name="remainingIds"></param>
        /// <param name="result"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async ValueTask<IReadOnlyList<Node>> FetchRemainingNodesAsync(
            IReadOnlyList<NodeId> remainingIds, List<Node?> result, CancellationToken ct)
        {
            Debug.Assert(result.Count(r => r == null) == remainingIds.Count);

            // fetch nodes and references from server.
            var localIds = new NodeIdCollection(remainingIds);
            (var sourceNodes, var readErrors) = await _session.ReadNodesAsync(
                localIds, ct).ConfigureAwait(false);
            (var referencesList, var fetchErrors) = await _session.FetchReferencesAsync(
                localIds, ct).ConfigureAwait(false);

            Debug.Assert(sourceNodes.Count == localIds.Count);
            Debug.Assert(sourceNodes.Count == referencesList.Count);
            var resultMissingIndex = 0;
            for (var index = 0; index < localIds.Count; index++)
            {
                if (!ServiceResult.IsBad(readErrors[index]))
                {
                    if (!ServiceResult.IsBad(fetchErrors[index]))
                    {
                        _refs.AddOrUpdate(remainingIds[index], referencesList[index]);
                        AddReferencesToNode(sourceNodes[index], referencesList[index]);
                    }
                    _nodes.AddOrUpdate(remainingIds[index], sourceNodes[index]);
                }
                while (result[resultMissingIndex] != null)
                {
                    resultMissingIndex++;
                    Debug.Assert(resultMissingIndex < result.Count);
                }
                result[resultMissingIndex] = sourceNodes[index];
            }
            Debug.Assert(!result.Any(r => r == null)); // None now should be null
            return result!;
        }

        /// <summary>
        /// Check whether type hierarchy is loaded
        /// </summary>
        /// <param name="typeIds"></param>
        /// <returns></returns>
        private bool IsTypeHierarchyLoaded(IEnumerable<NodeId> typeIds)
        {
            var types = new Queue<NodeId>(typeIds.Where(nodeId => !NodeId.IsNull(nodeId)));
            while (types.TryDequeue(out var typeId))
            {
                if (!_refs.TryGet(typeId, out var references))
                {
                    return false;
                }
                foreach (var reference in references)
                {
                    if (reference.ReferenceTypeId == ReferenceTypeIds.HasSubtype &&
                        reference.IsForward &&
                        !reference.NodeId.IsAbsolute)
                    {
                        types.Enqueue(ToNodeId(reference.NodeId));
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Get supertype from references
        /// </summary>
        /// <param name="references"></param>
        /// <returns></returns>
        private NodeId GetSuperTypeFromReferences(List<ReferenceDescription> references)
        {
            return references
                .Where(r => !r.IsForward &&
                    r.ReferenceTypeId == ReferenceTypeIds.HasSubtype)
                .Select(r => ExpandedNodeId.ToNodeId(r.NodeId, _session.NamespaceUris))
                .DefaultIfEmpty(NodeId.Null)
                .First();
        }

        /// <summary>
        /// Connect references to node atomically
        /// </summary>
        /// <param name="node"></param>
        /// <param name="references"></param>
        private static void AddReferencesToNode(Node node, List<ReferenceDescription> references)
        {
            lock (node.ReferenceTable)
            {
                if (node.ReferenceTable.Count != 0)
                {
                    // Competing threads already added references
                    return;
                }
                foreach (var reference in references)
                {
                    node.ReferenceTable.Add(reference.ReferenceTypeId,
                        !reference.IsForward, reference.NodeId);
                }
            }
        }

        /// <summary>
        /// Convert to node id from expanded node id if the expanded node id
        /// is not absolute. Otherwise return a null node id
        /// </summary>
        /// <param name="expandedNodeId"></param>
        /// <returns></returns>
        private NodeId ToNodeId(ExpandedNodeId expandedNodeId)
        {
            if (expandedNodeId.IsAbsolute)
            {
                return NodeId.Null;
            }
            return ExpandedNodeId.ToNodeId(expandedNodeId, _session.NamespaceUris);
        }

        private readonly INodeCacheContext _session;
        private readonly IAsyncCache<NodeId, Node> _nodes;
        private readonly IAsyncCache<NodeId, List<ReferenceDescription>> _refs;
    }
}
