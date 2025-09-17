/* ========================================================================
 * Copyright (c) 2005-2017 The OPC Foundation, Inc. All rights reserved.
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

namespace DataAccess
{
    using Opc.Ua;
    using Opc.Ua.Server;
    using System.Collections.Generic;

    /// <summary>
    /// A node manager for a server that exposes several variables.
    /// </summary>
    public class DataAccessNodeManager : CustomNodeManager2
    {
        /// <summary>
        /// Initializes the node manager.
        /// </summary>
        /// <param name="server"></param>
        /// <param name="configuration"></param>
        public DataAccessNodeManager(IServerInternal server, ApplicationConfiguration configuration) :
            base(server, configuration, Namespaces.DataAccess)
        {
            AliasRoot = "DA";

            SystemContext.SystemHandle = _system = new UnderlyingSystem();
            SystemContext.NodeIdFactory = this;

            // get the configuration for the node manager.
            // use suitable defaults if no configuration exists.
            _configuration = configuration.ParseExtension<DataAccessServerConfiguration>() ??
                new DataAccessServerConfiguration();

            // create the table to store the cached blocks.
            _blocks = [];
        }

        /// <summary>
        /// An overrideable version of the Dispose.
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _system.Dispose();
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// Creates the NodeId for the specified node.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="node">The node.</param>
        /// <returns>The new NodeId.</returns>
        /// <remarks>
        /// This method is called by the NodeState.Create() method which initializes a Node from
        /// the type model. During initialization a number of child nodes are created and need to
        /// have NodeIds assigned to them. This implementation constructs NodeIds by constructing
        /// strings. Other implementations could assign unique integers or Guids and save the new
        /// Node in a dictionary for later lookup.
        /// </remarks>
        public override NodeId New(ISystemContext context, NodeState node)
        {
            return ModelUtils.ConstructIdForComponent(node, NamespaceIndex);
        }

        /// <summary>
        /// Does any initialization required before the address space can be used.
        /// </summary>
        /// <param name="externalReferences"></param>
        /// <remarks>
        /// The externalReferences is an out parameter that allows the node manager to link to nodes
        /// in other node managers. For example, the 'Objects' node is managed by the CoreNodeManager and
        /// should have a reference to the root folder node(s) exposed by this node manager.
        /// </remarks>
        public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences)
        {
            lock (Lock)
            {
                // find the top level segments and link them to the ObjectsFolder.
                var segments = _system.FindSegments(null);

                for (var ii = 0; ii < segments.Count; ii++)
                {
                    // Top level areas need a reference from the Server object.
                    // These references are added to a list that is returned to the caller.
                    // The caller will update the Objects folder node.

                    if (!externalReferences.TryGetValue(ObjectIds.ObjectsFolder, out var references))
                    {
                        externalReferences[ObjectIds.ObjectsFolder] = references = [];
                    }

                    // construct the NodeId of a segment.
                    var segmentId = ModelUtils.ConstructIdForSegment(segments[ii].Id, NamespaceIndex);

                    // add an organizes reference from the ObjectsFolder to the area.
                    references.Add(new NodeStateReference(ReferenceTypeIds.Organizes, false, segmentId));
                }

                // start the simulation.
                _system.StartSimulation();
            }
        }

        /// <summary>
        /// Frees any resources allocated for the address space.
        /// </summary>
        public override void DeleteAddressSpace()
        {
            lock (Lock)
            {
                _system.StopSimulation();
                _blocks.Clear();
            }
        }

        /// <summary>
        /// Returns a unique handle for the node.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="nodeId"></param>
        /// <param name="cache"></param>
        protected override NodeHandle GetManagerHandle(ServerSystemContext context, NodeId nodeId, IDictionary<NodeId, NodeState> cache)
        {
            lock (Lock)
            {
                // quickly exclude nodes that are not in the namespace.
                if (!IsNodeIdInNamespace(nodeId))
                {
                    return null;
                }

                // check for check for nodes that are being currently monitored.

                if (MonitoredNodes.TryGetValue(nodeId, out var monitoredNode))
                {
                    return new NodeHandle
                    {
                        NodeId = nodeId,
                        Validated = true,
                        Node = monitoredNode.Node
                    };
                }

                if (nodeId.IdType != IdType.String && PredefinedNodes.TryGetValue(nodeId, out var node))
                {
                    return new NodeHandle
                    {
                        NodeId = nodeId,
                        Node = node,
                        Validated = true
                    };
                }

                // parse the identifier.
                var parsedNodeId = ParsedNodeId.Parse(nodeId);

                if (parsedNodeId != null)
                {
                    return new NodeHandle
                    {
                        NodeId = nodeId,
                        Validated = false,
                        Node = null,
                        ParsedNodeId = parsedNodeId
                    };
                }

                return null;
            }
        }

        /// <summary>
        /// Verifies that the specified node exists.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="handle"></param>
        /// <param name="cache"></param>
        protected override NodeState ValidateNode(
            ServerSystemContext context,
            NodeHandle handle,
            IDictionary<NodeId, NodeState> cache)
        {
            // not valid if no root.
            if (handle == null)
            {
                return null;
            }

            // check if previously validated.
            if (handle.Validated)
            {
                return handle.Node;
            }

            NodeState target = null;

            // check if already in the cache.
            if (cache != null)
            {
                if (cache.TryGetValue(handle.NodeId, out target))
                {
                    // nulls mean a NodeId which was previously found to be invalid has been referenced again.
                    if (target == null)
                    {
                        return null;
                    }

                    handle.Node = target;
                    handle.Validated = true;
                    return handle.Node;
                }

                target = null;
            }

            try
            {
                // check if the node id has been parsed.
                if (handle.ParsedNodeId is not ParsedNodeId parsedNodeId)
                {
                    return null;
                }

                NodeState root = null;

                // validate a segment.
                if (parsedNodeId.RootType == ModelUtils.Segment)
                {
                    var segment = _system.FindSegment(parsedNodeId.RootId);

                    // segment does not exist.
                    if (segment == null)
                    {
                        return null;
                    }

                    var rootId = ModelUtils.ConstructIdForSegment(segment.Id, NamespaceIndex);

                    // create a temporary object to use for the operation.
                    root = new SegmentState(context, rootId, segment);
                }

                // validate segment.
                else if (parsedNodeId.RootType == ModelUtils.Block)
                {
                    // validate the block.
                    var block = _system.FindBlock(parsedNodeId.RootId);

                    // block does not exist.
                    if (block == null)
                    {
                        return null;
                    }

                    var rootId = ModelUtils.ConstructIdForBlock(block.Id, NamespaceIndex);

                    // check for check for blocks that are being currently monitored.

                    if (_blocks.TryGetValue(rootId, out var node))
                    {
                        root = node;
                    }

                    // create a temporary object to use for the operation.
                    else
                    {
                        root = new BlockState(this, rootId, block);
                    }
                }

                // unknown root type.
                else
                {
                    return null;
                }

                // all done if no components to validate.
                if (string.IsNullOrEmpty(parsedNodeId.ComponentPath))
                {
                    handle.Validated = true;
                    handle.Node = target = root;
                    return handle.Node;
                }

                // validate component.
                NodeState component = root.FindChildBySymbolicName(context, parsedNodeId.ComponentPath);

                // component does not exist.
                if (component == null)
                {
                    return null;
                }

                // found a valid component.
                handle.Validated = true;
                handle.Node = target = component;
                return handle.Node;
            }
            finally
            {
                // store the node in the cache to optimize subsequent lookups.
                cache?.Add(handle.NodeId, target);
            }
        }

        /// <summary>
        /// Called after creating a MonitoredItem.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="handle">The handle for the node.</param>
        /// <param name="monitoredItem">The monitored item.</param>
        protected override void OnMonitoredItemCreated(ServerSystemContext context, NodeHandle handle, ISampledDataChangeMonitoredItem monitoredItem)
        {
            if (handle.Node.GetHierarchyRoot() is BlockState block)
            {
                block.StartMonitoring(context);

                // need to save the block to ensure that multiple monitored items use the same instance.
                _blocks[block.NodeId] = block;
            }
        }

        /// <summary>
        /// Called after deleting a MonitoredItem.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="handle">The handle for the node.</param>
        /// <param name="monitoredItem">The monitored item.</param>
        protected override void OnMonitoredItemDeleted(ServerSystemContext context, NodeHandle handle, ISampledDataChangeMonitoredItem monitoredItem)
        {
            if (handle.Node.GetHierarchyRoot() is BlockState block && !block.StopMonitoring(context))
            {
                // can remove the block since all monitored items for the block are gone.
                _blocks.Remove(block.NodeId);
            }
        }

        private readonly UnderlyingSystem _system;
#pragma warning disable IDE0052 // Remove unread private members
        private readonly DataAccessServerConfiguration _configuration;
#pragma warning restore IDE0052 // Remove unread private members
        private readonly Dictionary<NodeId, BlockState> _blocks;
    }
}
