// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace FileSystem
{
    using Opc.Ua;
    using Opc.Ua.Server;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// A node manager for a server that exposes several variables.
    /// </summary>
    public class FileSystemNodeManager : CustomNodeManager2
    {
        /// <summary>
        /// Initializes the node manager.
        /// </summary>
        /// <param name="server"></param>
        /// <param name="configuration"></param>
        public FileSystemNodeManager(IServerInternal server, ApplicationConfiguration configuration) :
            base(server, configuration, Namespaces.FileSystem)
        {
           // SystemContext.SystemHandle = _system = new UnderlyingSystem();
            SystemContext.NodeIdFactory = this;

            // get the configuration for the node manager.
            // use suitable defaults if no configuration exists.
            _configuration = configuration.ParseExtension<FileSystemServerConfiguration>() ??
                new FileSystemServerConfiguration();
        }

        /// <summary>
        /// An overrideable version of the Dispose.
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
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
                foreach (var fs in DriveInfo.GetDrives())
                {
                    // Top level areas need a reference from the Server object.
                    // These references are added to a list that is returned to the caller.
                    // The caller will update the Objects folder node.

                    if (!externalReferences.TryGetValue(ObjectIds.ObjectsFolder, out var references))
                    {
                        externalReferences[ObjectIds.ObjectsFolder] = references = new List<IReference>();
                    }

                    // construct the NodeId of a segment.
                    var fsId = ModelUtils.ConstructIdForVolume(fs.RootDirectory.FullName, NamespaceIndex);

                    // add an organizes reference from the ObjectsFolder to the volume.
                    references.Add(new NodeStateReference(ReferenceTypeIds.Organizes, false, fsId));
                }
            }
        }

        /// <summary>
        /// Frees any resources allocated for the address space.
        /// </summary>
        public override void DeleteAddressSpace()
        {
            lock (Lock)
            {
            }
        }

        /// <summary>
        /// Returns a unique handle for the node.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="nodeId"></param>
        /// <param name="cache"></param>
        protected override NodeHandle GetManagerHandle(ServerSystemContext context, NodeId nodeId,
            IDictionary<NodeId, NodeState> cache)
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
        protected override NodeState ValidateNode(ServerSystemContext context,
            NodeHandle handle, IDictionary<NodeId, NodeState> cache)
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

                // Validate drive
                if (parsedNodeId.RootType == ModelUtils.Volume)
                {
                    var volume = DriveInfo.GetDrives().FirstOrDefault(d => d.RootDirectory.FullName == parsedNodeId.RootId);

                    // volume does not exist.
                    if (volume == null)
                    {
                        return null;
                    }

                    var rootId = ModelUtils.ConstructIdForVolume(volume.RootDirectory.FullName, NamespaceIndex);

                    // create a temporary object to use for the operation.
#pragma warning disable CA2000 // Dispose objects before losing scope
                    root = new DirectoryObjectState(context, rootId, volume.RootDirectory.FullName, true);
#pragma warning restore CA2000 // Dispose objects before losing scope
                }

                // Validate directory
                else if (parsedNodeId.RootType == ModelUtils.Directory)
                {
                    // block does not exist.
                    if (!Path.Exists(parsedNodeId.RootId))
                    {
                        return null;
                    }

                    var rootId = ModelUtils.ConstructIdForDirectory(parsedNodeId.RootId, NamespaceIndex);

#pragma warning disable CA2000 // Dispose objects before losing scope
                    root = new DirectoryObjectState(context, rootId, parsedNodeId.RootId);
#pragma warning restore CA2000 // Dispose objects before losing scope
                }

                // Validate file
                else if (parsedNodeId.RootType == ModelUtils.File)
                {
                    // block does not exist.
                    if (!Path.Exists(parsedNodeId.RootId))
                    {
                        return null;
                    }

                    var rootId = ModelUtils.ConstructIdForFile(parsedNodeId.RootId, NamespaceIndex);

#pragma warning disable CA2000 // Dispose objects before losing scope
                    root = new FileObjectState(context, rootId, parsedNodeId.RootId);
#pragma warning restore CA2000 // Dispose objects before losing scope
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

#pragma warning disable IDE0052 // Remove unread private members
        private readonly FileSystemServerConfiguration _configuration;
#pragma warning restore IDE0052 // Remove unread private members
    }
}
