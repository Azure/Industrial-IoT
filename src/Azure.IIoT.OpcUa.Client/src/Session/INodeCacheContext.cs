// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation, The OPC Foundation, Inc.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Interface between node cache and session
    /// </summary>
    internal interface INodeCacheContext
    {
        /// <summary>
        /// Gets the table of namespace uris known to the server.
        /// </summary>
        NamespaceTable NamespaceUris { get; }

        /// <summary>
        /// Reads the values for the node attributes and returns a node object
        /// collection.
        /// </summary>
        /// <remarks>
        /// If the nodeclass for the nodes in nodeIdCollection is already known
        /// and passed as nodeClass, reads only values of required attributes.
        /// Otherwise NodeClass.Unspecified should be used.
        /// </remarks>
        /// <param name="nodeIds">The nodeId collection to read.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>The node collection and associated errors.</returns>
        Task<ResultSet<Node>> ReadNodesAsync(IReadOnlyList<NodeId> nodeIds,
            CancellationToken ct = default);

        /// <summary>
        /// Reads the values for the node attributes and returns a node object.
        /// </summary>
        /// <remarks>
        /// If the nodeclass is known, only the supported attribute values are
        /// read.
        /// </remarks>
        /// <param name="nodeId">The nodeId.</param>
        /// <param name="ct">The cancellation token for the request.</param>
        Task<Node> ReadNodeAsync(NodeId nodeId, CancellationToken ct = default);

        /// <summary>
        /// Fetches all references for the specified node.
        /// </summary>
        /// <param name="nodeId">The node id.</param>
        /// <param name="ct"></param>
        Task<ReferenceDescriptionCollection> FetchReferencesAsync(NodeId nodeId,
            CancellationToken ct = default);

        /// <summary>
        /// Fetches all references for the specified nodes.
        /// </summary>
        /// <param name="nodeIds">The node id collection.</param>
        /// <param name="ct"></param>
        /// <returns>A list of reference collections and the errors reported by the
        /// server.</returns>
        Task<ResultSet<ReferenceDescriptionCollection>> FetchReferencesAsync(
            IReadOnlyList<NodeId> nodeIds, CancellationToken ct = default);
    }
}
