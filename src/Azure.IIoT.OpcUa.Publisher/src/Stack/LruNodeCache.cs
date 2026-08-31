// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

// This is not a compatibility shim any more, despite its shape. It is where
// the Publisher's node-lookup policy lives: GetNodeAsync returns null for a
// node the server does not have, rather than throwing, and a dozen call sites
// in the metadata and event-filter builders are written against that. Removing
// it would push a try/catch into every one of them - more code, not less - so
// it stays until those call sites can be made to want the 2.0 behaviour.
//
// The one thing that should still go is IsTypeOf, which blocks on an async
// call because AsyncEnumerableBrowser's callers are synchronous.

namespace Azure.IIoT.OpcUa.Publisher.Stack
{
    using Opc.Ua;
    using Opc.Ua.Client;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Node cache surface used by the Publisher stack layer, wrapping the
    /// <see cref="INodeCache"/> so that a node the server does not have reads
    /// back as null instead of raising.
    /// </summary>
    public interface ILruNodeCache
    {
        /// <summary>
        /// The underlying 2.0 node cache (for APIs that consume it directly,
        /// e.g. <c>NodeCacheResolver</c>).
        /// </summary>
        INodeCache Inner { get; }

        /// <summary>
        /// Returns true if the subtype is of the given supertype.
        /// </summary>
        bool IsTypeOf(NodeId subTypeId, NodeId superTypeId);

        /// <summary>
        /// Get a node from the cache. Returns null when the node cannot be
        /// resolved (preserving the legacy behavior).
        /// </summary>
        ValueTask<INode?> GetNodeAsync(NodeId nodeId, CancellationToken ct = default);

        /// <summary>
        /// Get the immediate super type of the given type.
        /// </summary>
        ValueTask<NodeId> GetSuperTypeAsync(NodeId typeId, CancellationToken ct = default);

        /// <summary>
        /// Get the built in type of the given data type.
        /// </summary>
        ValueTask<BuiltInType> GetBuiltInTypeAsync(NodeId dataTypeId, CancellationToken ct = default);

        /// <summary>
        /// Get the references of the given node.
        /// </summary>
        ValueTask<IReadOnlyList<INode>> GetReferencesAsync(NodeId nodeId, NodeId referenceTypeId,
            bool isInverse, bool includeSubtypes, CancellationToken ct = default);

        /// <summary>
        /// Clear the cache.
        /// </summary>
        void Clear();
    }

    /// <summary>
    /// <see cref="ILruNodeCache"/> implementation delegating to the 2.0
    /// <see cref="NodeCache"/>.
    /// </summary>
    internal sealed class LruNodeCache : ILruNodeCache
    {
        /// <inheritdoc/>
        public INodeCache Inner { get; }

        /// <summary>
        /// Create the cache
        /// </summary>
        public LruNodeCache(INodeCacheContext context, ITelemetryContext telemetry,
            TimeSpan? cacheExpiry, int capacity, bool unused = true)
        {
            _ = unused;
            Inner = new NodeCache(context, telemetry, cacheExpiry, capacity);
        }

        /// <inheritdoc/>
        public bool IsTypeOf(NodeId subTypeId, NodeId superTypeId)
            => Inner.IsTypeOfAsync(subTypeId, superTypeId).AsTask().GetAwaiter().GetResult();

        /// <inheritdoc/>
        public async ValueTask<INode?> GetNodeAsync(NodeId nodeId, CancellationToken ct = default)
        {
            try
            {
                return await Inner.GetNodeAsync(nodeId, ct).ConfigureAwait(false);
            }
            catch (ServiceResultException)
            {
                return null;
            }
        }

        /// <inheritdoc/>
        public ValueTask<NodeId> GetSuperTypeAsync(NodeId typeId, CancellationToken ct = default)
            => Inner.FindSuperTypeAsync(typeId, ct);

        /// <inheritdoc/>
        public ValueTask<BuiltInType> GetBuiltInTypeAsync(NodeId dataTypeId, CancellationToken ct = default)
            => Inner.GetBuiltInTypeAsync(dataTypeId, ct);

        /// <inheritdoc/>
        public async ValueTask<IReadOnlyList<INode>> GetReferencesAsync(NodeId nodeId,
            NodeId referenceTypeId, bool isInverse, bool includeSubtypes, CancellationToken ct = default)
        {
            var references = await Inner.GetReferencesAsync(nodeId, referenceTypeId,
                isInverse, includeSubtypes, ct).ConfigureAwait(false);
            return references.ToArray() ?? [];
        }

        /// <inheritdoc/>
        public void Clear() => Inner.Clear();
    }
}
