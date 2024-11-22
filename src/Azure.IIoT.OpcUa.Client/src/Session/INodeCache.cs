// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// A client side cache of the server's address space and type
    /// system to allow more efficient use of the server and connectivity
    /// resources.
    /// </summary>
    public interface INodeCache
    {
        /// <summary>
        /// Finds a node on the server. While the underlying object is
        /// of type Node it is not fetching the reference table.
        /// </summary>
        /// <param name="nodeId">The node identifier.</param>
        /// <param name="ct"></param>
        ValueTask<INode> FindAsync(NodeId nodeId,
            CancellationToken ct = default);

        /// <summary>
        /// Finds a set of nodes in the address space of the server.
        /// While the underlying objects returned are of type Node it
        /// is not fetching the reference table.
        /// </summary>
        /// <param name="nodeIds">The node identifier collection.</param>
        /// <param name="ct"></param>
        ValueTask<IReadOnlyList<INode>> FindAsync(
            IReadOnlyList<NodeId> nodeIds, CancellationToken ct = default);

        /// <summary>
        /// Get a node object from the cache or fetch it from the server.
        /// The node object contains references.
        /// </summary>
        /// <param name="nodeId">Node id to fetch.</param>
        /// <param name="ct"></param>
        ValueTask<Node> FetchNodeAsync(NodeId nodeId,
            CancellationToken ct = default);

        /// <summary>
        /// Find a node by traversing a provided browse path. The node does
        /// not container references. Call <see cref="FetchNodeAsync"/>
        /// to fetch a node with references
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="browsePath"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        ValueTask<INode?> FindNodeWithBrowsePathAsync(NodeId nodeId,
            QualifiedNameCollection browsePath, CancellationToken ct = default);

        /// <summary>
        /// Returns the references of the specified node that meet
        /// the criteria specified. The node might not contain references.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="referenceTypeId"></param>
        /// <param name="isInverse"></param>
        /// <param name="includeSubtypes"></param>
        /// <param name="ct"></param>
        ValueTask<IReadOnlyList<INode>> FindReferencesAsync(NodeId nodeId,
            NodeId referenceTypeId, bool isInverse, bool includeSubtypes,
            CancellationToken ct = default);

        /// <summary>
        /// Returns the references of the specified nodes that meet
        /// the criteria specified. The node might not contain references.
        /// </summary>
        /// <param name="nodeIds"></param>
        /// <param name="referenceTypeIds"></param>
        /// <param name="isInverse"></param>
        /// <param name="includeSubtypes"></param>
        /// <param name="ct"></param>
        ValueTask<IReadOnlyList<INode>> FindReferencesAsync(
            IReadOnlyList<NodeId> nodeIds,
            IReadOnlyList<NodeId> referenceTypeIds, bool isInverse,
            bool includeSubtypes, CancellationToken ct = default);

        /// <summary>
        /// Load the type hierarchy of this type into the cache for
        /// efficiently calling <see cref="IsTypeOf(NodeId, NodeId)"/>
        /// </summary>
        /// <param name="typeIds"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        ValueTask LoadTypeHierarchyAync(IReadOnlyList<NodeId> typeIds,
            CancellationToken ct = default);

        /// <summary>
        /// Determines whether a type is a subtype of another type.
        /// </summary>
        /// <param name="subTypeId">The subtype identifier.</param>
        /// <param name="superTypeId">The supertype identifier.</param>
        /// <returns><c>true</c> if <paramref name="superTypeId"/> is
        /// supertype of <paramref name="subTypeId"/>; otherwise
        /// <c>false</c>. </returns>
        bool IsTypeOf(NodeId subTypeId, NodeId superTypeId);

        /// <summary>
        /// Returns the immediate supertype for the type.
        /// </summary>
        /// <param name="typeId">The type identifier.</param>
        /// <param name="ct"></param>
        /// <returns>The immediate supertype idnetyfier for
        /// <paramref name="typeId"/></returns>
        ValueTask<NodeId> FindSuperTypeAsync(NodeId typeId,
            CancellationToken ct = default);

        /// <summary>
        /// Get built in type of the data type
        /// </summary>
        /// <param name="datatypeId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        ValueTask<BuiltInType> GetBuiltInTypeAsync(NodeId datatypeId,
            CancellationToken ct = default);

        /// <summary>
        /// Removes all nodes from the cache.
        /// </summary>
        void Clear();
    }
}
