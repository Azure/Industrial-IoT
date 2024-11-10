/* ========================================================================
 * Copyright (c) 2005-2022 The OPC Foundation, Inc. All rights reserved.
 *
 * OPC Foundation MIT License 1.00
 *
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 *
 * The complete license agreement can be found here:
 * http://opcfoundation.org/License/MIT/1.00/
 * ======================================================================*/

namespace Opc.Ua.Client
{
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.IO;
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
