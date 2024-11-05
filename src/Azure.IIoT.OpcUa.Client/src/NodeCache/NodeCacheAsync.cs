/* ========================================================================
 * Copyright (c) 2005-2023 The OPC Foundation, Inc. All rights reserved.
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
    using Opc.Ua.Redaction;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// An implementation of a client side nodecache.
    /// </summary>
    public partial class NodeCache : INodeCache
    {
        /// <inheritdoc/>
        public async Task<INode> FindAsync(ExpandedNodeId nodeId, CancellationToken ct = default)
        {
            // check for null.
            if (NodeId.IsNull(nodeId))
            {
                return null;
            }

            INode node;
            try
            {
                m_cacheLock.EnterReadLock();

                // check if node already exists.
                node = m_nodes.Find(nodeId);
            }
            finally
            {
                m_cacheLock.ExitReadLock();
            }

            if (node != null)
            {
                // do not return temporary nodes created after a Browse().
                if (node.GetType() != typeof(Node))
                {
                    return node;
                }
            }

            // fetch node from server.
            try
            {
                return await FetchNodeAsync(nodeId, ct).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Utils.LogError("Could not fetch node from server: NodeId={0}, Reason='{1}'.", nodeId, Redact.Create(e));
                // m_nodes[nodeId] = null;
                return null;
            }
        }

        /// <inheritdoc/>
        public async Task<IList<INode>> FindAsync(IList<ExpandedNodeId> nodeIds, CancellationToken ct = default)
        {
            // check for null.
            if (nodeIds == null || nodeIds.Count == 0)
            {
                return new List<INode>();
            }

            var count = nodeIds.Count;
            var nodes = new List<INode>(count);
            var fetchNodeIds = new ExpandedNodeIdCollection();

            int ii;
            for (ii = 0; ii < count; ii++)
            {
                INode node;
                try
                {
                    m_cacheLock.EnterReadLock();

                    // check if node already exists.
                    node = m_nodes.Find(nodeIds[ii]);
                }
                finally
                {
                    m_cacheLock.ExitReadLock();
                }

                // do not return temporary nodes created after a Browse().
                if (node != null && node.GetType() != typeof(Node))
                {
                    nodes.Add(node);
                }
                else
                {
                    nodes.Add(null);
                    fetchNodeIds.Add(nodeIds[ii]);
                }
            }

            if (fetchNodeIds.Count == 0)
            {
                return nodes;
            }

            // fetch missing nodes from server.
            IList<Node> fetchedNodes;
            try
            {
                fetchedNodes = await FetchNodesAsync(fetchNodeIds, ct).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Utils.LogError("Could not fetch nodes from server: Reason='{0}'.", e.Message);
                // m_nodes[nodeId] = null;
                return nodes;
            }

            ii = 0;
            foreach (var fetchedNode in fetchedNodes)
            {
                while (ii < count && nodes[ii] != null)
                {
                    ii++;
                }
                if (ii < count && nodes[ii] == null)
                {
                    nodes[ii++] = fetchedNode;
                }
                else
                {
                    Utils.LogError("Inconsistency fetching nodes from server. Not all nodes could be assigned.");
                    break;
                }
            }

            return nodes;
        }

        /// <inheritdoc/>
        public async Task<NodeId> FindSuperTypeAsync(ExpandedNodeId typeId, CancellationToken ct)
        {
            var type = await FindAsync(typeId, ct).ConfigureAwait(false);

            if (type == null)
            {
                return null;
            }

            try
            {
                m_cacheLock.EnterReadLock();

                return m_typeTree.FindSuperType(typeId);
            }
            finally
            {
                m_cacheLock.ExitReadLock();
            }
        }

        /// <inheritdoc/>
        public async Task<NodeId> FindSuperTypeAsync(NodeId typeId, CancellationToken ct = default)
        {
            var type = await FindAsync(typeId, ct).ConfigureAwait(false);

            if (type == null)
            {
                return null;
            }

            try
            {
                m_cacheLock.EnterReadLock();

                return m_typeTree.FindSuperType(typeId);
            }
            finally
            {
                m_cacheLock.ExitReadLock();
            }
        }

        /// <inheritdoc/>
        public async Task<Node> FetchNodeAsync(ExpandedNodeId nodeId, CancellationToken ct)
        {
            var localId = ExpandedNodeId.ToNodeId(nodeId, m_session.NamespaceUris);

            if (localId == null)
            {
                return null;
            }

            // fetch node from server.
            var source = await m_session.ReadNodeAsync(localId, ct).ConfigureAwait(false);

            try
            {
                // fetch references from server.
                var references = await m_session.FetchReferencesAsync(localId, ct).ConfigureAwait(false);

                try
                {
                    m_cacheLock.EnterUpgradeableReadLock();

                    foreach (var reference in references)
                    {
                        // create a placeholder for the node if it does not already exist.
                        if (!m_nodes.Exists(reference.NodeId))
                        {
                            // transform absolute identifiers.
                            if (reference.NodeId?.IsAbsolute == true)
                            {
                                reference.NodeId = ExpandedNodeId.ToNodeId(reference.NodeId, NamespaceUris);
                            }

                            var target = new Node(reference);

                            InternalWriteLockedAttach(target);
                        }

                        // add the reference.
                        source.ReferenceTable.Add(reference.ReferenceTypeId, !reference.IsForward, reference.NodeId);
                    }
                }
                finally
                {
                    m_cacheLock.ExitUpgradeableReadLock();
                }
            }
            catch (Exception e)
            {
                Utils.LogError("Could not fetch references for valid node with NodeId = {0}. Error = {1}", nodeId, Redact.Create(e));
            }

            InternalWriteLockedAttach(source);

            return source;
        }

        /// <inheritdoc/>
        public async Task<IList<Node>> FetchNodesAsync(IList<ExpandedNodeId> nodeIds, CancellationToken ct)
        {
            var count = nodeIds.Count;
            if (count == 0)
            {
                return new List<Node>();
            }

            var localIds = new NodeIdCollection(
                nodeIds.Select(nodeId => ExpandedNodeId.ToNodeId(nodeId, m_session.NamespaceUris)));

            // fetch nodes and references from server.
            (var sourceNodes, var readErrors) = await m_session.ReadNodesAsync(localIds, NodeClass.Unspecified, ct: ct).ConfigureAwait(false);
            (var referenceCollectionList, var fetchErrors) = await m_session.FetchReferencesAsync(localIds, ct).ConfigureAwait(false);

            var ii = 0;
            for (ii = 0; ii < count; ii++)
            {
                if (ServiceResult.IsBad(readErrors[ii]))
                {
                    continue;
                }

                if (!ServiceResult.IsBad(fetchErrors[ii]))
                {
                    // fetch references from server.
                    foreach (var reference in referenceCollectionList[ii])
                    {
                        try
                        {
                            m_cacheLock.EnterUpgradeableReadLock();

                            // create a placeholder for the node if it does not already exist.
                            if (!m_nodes.Exists(reference.NodeId))
                            {
                                // transform absolute identifiers.
                                if (reference.NodeId?.IsAbsolute == true)
                                {
                                    reference.NodeId = ExpandedNodeId.ToNodeId(reference.NodeId, NamespaceUris);
                                }

                                var target = new Node(reference);

                                InternalWriteLockedAttach(target);
                            }
                        }
                        finally
                        {
                            m_cacheLock.ExitUpgradeableReadLock();
                        }

                        // add the reference.
                        sourceNodes[ii].ReferenceTable.Add(reference.ReferenceTypeId, !reference.IsForward, reference.NodeId);
                    }
                }

                InternalWriteLockedAttach(sourceNodes[ii]);
            }

            return sourceNodes;
        }

        /// <inheritdoc/>
        public async Task<IList<INode>> FindReferencesAsync(
            ExpandedNodeId nodeId,
            NodeId referenceTypeId,
            bool isInverse,
            bool includeSubtypes,
            CancellationToken ct)
        {
            var targets = new List<INode>();

            if (await FindAsync(nodeId, ct).ConfigureAwait(false) is not Node source)
            {
                return targets;
            }

            IList<IReference> references;
            try
            {
                m_cacheLock.EnterReadLock();

                references = source.ReferenceTable.Find(referenceTypeId, isInverse, includeSubtypes, m_typeTree);
            }
            finally
            {
                m_cacheLock.ExitReadLock();
            }

            var targetIds = new ExpandedNodeIdCollection(
                references.Select(reference => reference.TargetId));

            var result = await FindAsync(targetIds, ct).ConfigureAwait(false);

            foreach (var target in result)
            {
                if (target != null)
                {
                    targets.Add(target);
                }
            }
            return targets;
        }

        /// <inheritdoc/>
        public async Task<IList<INode>> FindReferencesAsync(
            IList<ExpandedNodeId> nodeIds,
            IList<NodeId> referenceTypeIds,
            bool isInverse,
            bool includeSubtypes,
            CancellationToken ct)
        {
            var targets = new List<INode>();
            if (nodeIds.Count == 0 || referenceTypeIds.Count == 0)
            {
                return targets;
            }
            var targetIds = new ExpandedNodeIdCollection();
            var sources = await FindAsync(nodeIds, ct).ConfigureAwait(false);
            foreach (var source in sources)
            {
                if (!(source is Node node))
                {
                    continue;
                }

                foreach (var referenceTypeId in referenceTypeIds)
                {
                    IList<IReference> references;
                    try
                    {
                        m_cacheLock.EnterReadLock();

                        references = node.ReferenceTable.Find(referenceTypeId, isInverse, includeSubtypes, m_typeTree);
                    }
                    finally
                    {
                        m_cacheLock.ExitReadLock();
                    }

                    targetIds.AddRange(
                        references.Select(reference => reference.TargetId));
                }
            }

            var result = await FindAsync(targetIds, ct).ConfigureAwait(false);
            foreach (var target in result)
            {
                if (target != null)
                {
                    targets.Add(target);
                }
            }

            return targets;
        }
    }
}
