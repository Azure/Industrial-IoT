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

using Opc.Ua.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Opc.Ua.Sample
{
    /// <summary>
    /// A node manager for a variety of test data.
    /// </summary>
    public class SampleNodeManager : INodeManager, INodeIdFactory, IDisposable
    {
        /// <summary>
        /// Initializes the node manager.
        /// </summary>
        /// <param name="server"></param>
        public SampleNodeManager(IServerInternal server)
        {
            // save a reference to the server that owns the node manager.
            Server = server;

            // create the default context.
            SystemContext = Server.DefaultSystemContext.Copy();

            SystemContext.SystemHandle = null;
            SystemContext.NodeIdFactory = this;

            // create the table of nodes.
            PredefinedNodes = [];
            RootNotifiers = [];
            _sampledItems = [];
            _minimumSamplingInterval = 100;
        }

        /// <summary>
        /// Frees any unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// An overrideable version of the Dispose.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (Lock)
                {
                    Utils.SilentDispose(_samplingTimer);
                    _samplingTimer = null;

                    foreach (var node in PredefinedNodes.Values)
                    {
                        Utils.SilentDispose(node);
                    }
                }
            }
        }

        /// <summary>
        /// Creates the NodeId for the specified node.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="node">The node.</param>
        /// <returns>The new NodeId.</returns>
        public virtual NodeId New(ISystemContext context, NodeState node)
        {
            return node.NodeId;
        }

        /// <summary>
        /// Acquires the lock on the node manager.
        /// </summary>
        public object Lock { get; } = new object();

        /// <summary>
        /// The server that the node manager belongs to.
        /// </summary>
        protected IServerInternal Server { get; }

        /// <summary>
        /// The default context to use.
        /// </summary>
        protected ServerSystemContext SystemContext { get; }

        /// <summary>
        /// The predefined nodes managed by the node manager.
        /// </summary>
        protected NodeIdDictionary<NodeState> PredefinedNodes { get; }

        /// <summary>
        /// The root notifiers for the node manager.
        /// </summary>
        protected List<NodeState> RootNotifiers { get; }

        /// <summary>
        /// Returns true if the namespace for the node id is one of the namespaces managed by the node manager.
        /// </summary>
        /// <param name="nodeId">The node id to check.</param>
        /// <returns>True if the namespace is one of the nodes.</returns>
        protected virtual bool IsNodeIdInNamespace(NodeId nodeId)
        {
            if (NodeId.IsNull(nodeId))
            {
                return false;
            }

            // quickly exclude nodes that not in the namespace.
            for (var ii = 0; ii < _namespaceIndexes.Length; ii++)
            {
                if (nodeId.NamespaceIndex == _namespaceIndexes[ii])
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns the node if the handle refers to a node managed by this manager.
        /// </summary>
        /// <param name="managerHandle">The handle to check.</param>
        /// <returns>Non-null if the handle belongs to the node manager.</returns>
        protected virtual NodeState IsHandleInNamespace(object managerHandle)
        {
            if (managerHandle is not NodeState source)
            {
                return null;
            }

            if (!IsNodeIdInNamespace(source.NodeId))
            {
                return null;
            }

            return source;
        }

        /// <summary>
        /// Returns the state object for the specified node if it exists.
        /// </summary>
        /// <param name="nodeId"></param>
        public NodeState Find(NodeId nodeId)
        {
            lock (Lock)
            {
                NodeState node = null;

                if (!PredefinedNodes.TryGetValue(nodeId, out node))
                {
                    return null;
                }

                return node;
            }
        }

        /// <summary>
        /// Creates a new instance and assigns unique identifiers to all children.
        /// </summary>
        /// <param name="context">The operation context.</param>
        /// <param name="parentId">An optional parent identifier.</param>
        /// <param name="referenceTypeId">The reference type from the parent.</param>
        /// <param name="browseName">The browse name.</param>
        /// <param name="instance">The instance to create.</param>
        /// <returns>The new node id.</returns>
        /// <exception cref="ServiceResultException"></exception>
        public NodeId CreateNode(
            ServerSystemContext context,
            NodeId parentId,
            NodeId referenceTypeId,
            QualifiedName browseName,
            BaseInstanceState instance)
        {
            var contextToUse = (ServerSystemContext)SystemContext.Copy(context);

            lock (Lock)
            {
                instance.ReferenceTypeId = referenceTypeId;

                NodeState parent = null;

                if (parentId != null)
                {
                    if (!PredefinedNodes.TryGetValue(parentId, out parent))
                    {
                        throw ServiceResultException.Create(
                            StatusCodes.BadNodeIdUnknown,
                            "Cannot find parent with id: {0}",
                            parentId);
                    }

                    parent.AddChild(instance);
                }

                instance.Create(contextToUse, null, browseName, null, true);
                AddPredefinedNode(contextToUse, instance);

                return instance.NodeId;
            }
        }

        /// <summary>
        /// Deletes a node and all of its children.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="nodeId"></param>
        public bool DeleteNode(
            ServerSystemContext context,
            NodeId nodeId)
        {
            var contextToUse = SystemContext.Copy(context);

            var found = false;
            var referencesToRemove = new List<LocalReference>();

            lock (Lock)
            {
                NodeState node = null;

                if (PredefinedNodes.TryGetValue(nodeId, out node))
                {
                    RemovePredefinedNode(contextToUse, node, referencesToRemove);
                    found = true;
                }

                RemoveRootNotifier(node);
            }

            // must release the lock before removing cross references to other node managers.
            if (referencesToRemove.Count > 0)
            {
                Server.NodeManager.RemoveReferences(referencesToRemove);
            }

            return found;
        }

        /// <summary>
        /// Adds all encodeable types defined in a node manager to the server factory.
        /// </summary>
        /// <param name="assembly">The assembly which contains the encodeable types.</param>
        /// <param name="filter">A filter with which the FullName of the type must start.</param>
        protected void AddEncodeableNodeManagerTypes(Assembly assembly, string filter)
        {
            Server.Factory.AddEncodeableTypes(assembly.GetExportedTypes().Where(t => t.FullName.StartsWith(filter, StringComparison.Ordinal)));
        }

        /// <summary>
        /// Returns the namespaces used by the node manager.
        /// </summary>
        /// <remarks>
        /// All NodeIds exposed by the node manager must be qualified by a namespace URI. This property
        /// returns the URIs used by the node manager. In this example all NodeIds use a single URI.
        /// </remarks>
        public virtual IEnumerable<string> NamespaceUris
        {
            get
            {
                return _namespaceUris;
            }

            protected set
            {
                if (value != null)
                {
                    _namespaceUris = [.. value];
                }
                else
                {
                    _namespaceUris = [];
                }

                _namespaceIndexes = new ushort[_namespaceUris.Count];
            }
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
        public virtual void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences)
        {
            lock (Lock)
            {
                // add the uris to the server's namespace table and cache the indexes.
                for (var ii = 0; ii < _namespaceUris.Count; ii++)
                {
                    _namespaceIndexes[ii] = Server.NamespaceUris.GetIndexOrAppend(_namespaceUris[ii]);
                }

                LoadPredefinedNodes(SystemContext, externalReferences);
            }
        }

        /// <summary>
        /// Loads a node set from a file or resource and adds them to the set of predefined nodes.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="assembly"></param>
        /// <param name="resourcePath"></param>
        /// <param name="externalReferences"></param>
        public virtual void LoadPredefinedNodes(
            ISystemContext context,
            Assembly assembly,
            string resourcePath,
            IDictionary<NodeId, IList<IReference>> externalReferences)
        {
            // load the predefined nodes from an XML document.
            var predefinedNodes = new NodeStateCollection();
            predefinedNodes.LoadFromResource(context, resourcePath, assembly, true);

            // add the predefined nodes to the node manager.
            for (var ii = 0; ii < predefinedNodes.Count; ii++)
            {
                AddPredefinedNode(context, predefinedNodes[ii]);
            }

            // ensure the reverse references exist.
            AddReverseReferences(externalReferences);
        }

        /// <summary>
        /// Loads a node set from a file or resource and adds them to the set of predefined nodes.
        /// </summary>
        /// <param name="context"></param>
        protected virtual NodeStateCollection LoadPredefinedNodes(ISystemContext context)
        {
            return [];
        }

        /// <summary>
        /// Loads a node set from a file or resource and adds them to the set of predefined nodes.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="externalReferences"></param>
        protected virtual void LoadPredefinedNodes(
            ISystemContext context,
            IDictionary<NodeId, IList<IReference>> externalReferences)
        {
            // load the predefined nodes from an XML document.
            var predefinedNodes = LoadPredefinedNodes(context);

            // add the predefined nodes to the node manager.
            for (var ii = 0; ii < predefinedNodes.Count; ii++)
            {
                AddPredefinedNode(context, predefinedNodes[ii]);
            }

            // ensure the reverse references exist.
            AddReverseReferences(externalReferences);
        }

        /// <summary>
        /// Replaces the generic node with a node specific to the model.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="predefinedNode"></param>
        protected virtual NodeState AddBehaviourToPredefinedNode(ISystemContext context, NodeState predefinedNode)
        {
            if (predefinedNode is not BaseObjectState)
            {
                return predefinedNode;
            }

            return predefinedNode;
        }

        /// <summary>
        /// Recursively indexes the node and its children.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="node"></param>
        protected virtual void AddPredefinedNode(ISystemContext context, NodeState node)
        {
            var activeNode = AddBehaviourToPredefinedNode(context, node);
            PredefinedNodes[activeNode.NodeId] = activeNode;

            if (activeNode is BaseTypeState type)
            {
                AddTypesToTypeTree(type);
            }

            var children = new List<BaseInstanceState>();
            activeNode.GetChildren(context, children);

            for (var ii = 0; ii < children.Count; ii++)
            {
                AddPredefinedNode(context, children[ii]);
            }
        }

        /// <summary>
        /// Recursively indexes the node and its children.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="node"></param>
        /// <param name="referencesToRemove"></param>
        protected virtual void RemovePredefinedNode(
            ISystemContext context,
            NodeState node,
            List<LocalReference> referencesToRemove)
        {
            PredefinedNodes.Remove(node.NodeId);
            node.UpdateChangeMasks(NodeStateChangeMasks.Deleted);
            node.ClearChangeMasks(context, false);
            OnNodeRemoved(node);

            // remove from the parent.

            if (node is BaseInstanceState instance && instance.Parent != null)
            {
                instance.Parent.RemoveChild(instance);
            }

            // remove children.
            var children = new List<BaseInstanceState>();
            node.GetChildren(context, children);

            for (var ii = 0; ii < children.Count; ii++)
            {
                node.RemoveChild(children[ii]);
            }

            for (var ii = 0; ii < children.Count; ii++)
            {
                RemovePredefinedNode(context, children[ii], referencesToRemove);
            }

            // remove from type table.

            if (node is BaseTypeState type)
            {
                Server.TypeTree.Remove(type.NodeId);
            }

            // remove inverse references.
            var references = new List<IReference>();
            node.GetReferences(context, references);

            for (var ii = 0; ii < references.Count; ii++)
            {
                var reference = references[ii];

                if (reference.TargetId.IsAbsolute)
                {
                    continue;
                }

                var referenceToRemove = new LocalReference(
                    (NodeId)reference.TargetId,
                    reference.ReferenceTypeId,
                    !reference.IsInverse,
                    node.NodeId);

                referencesToRemove.Add(referenceToRemove);
            }
        }

        /// <summary>
        /// Called after a node has been deleted.
        /// </summary>
        /// <param name="node"></param>
        protected virtual void OnNodeRemoved(NodeState node)
        {
            // overridden by the sub-class.
        }

        /// <summary>
        /// Add the node to the set of root notifiers.
        /// </summary>
        /// <param name="notifier"></param>
        protected virtual void AddRootNotifier(NodeState notifier)
        {
            for (var ii = 0; ii < RootNotifiers.Count; ii++)
            {
                if (ReferenceEquals(notifier, RootNotifiers[ii]))
                {
                    return;
                }
            }

            RootNotifiers.Add(notifier);

            // subscribe to existing events.
            if (Server.EventManager != null)
            {
                var monitoredItems = Server.EventManager.GetMonitoredItems();

                for (var ii = 0; ii < monitoredItems.Count; ii++)
                {
                    if (monitoredItems[ii].MonitoringAllEvents)
                    {
                        SubscribeToAllEvents(
                            SystemContext,
                            monitoredItems[ii],
                            true,
                            notifier);
                    }
                }
            }
        }

        /// <summary>
        /// Remove the node from the set of root notifiers.
        /// </summary>
        /// <param name="notifier"></param>
        protected virtual void RemoveRootNotifier(NodeState notifier)
        {
            for (var ii = 0; ii < RootNotifiers.Count; ii++)
            {
                if (ReferenceEquals(notifier, RootNotifiers[ii]))
                {
                    RootNotifiers.RemoveAt(ii);
                    break;
                }
            }
        }

        /// <summary>
        /// Ensures that all reverse references exist.
        /// </summary>
        /// <param name="externalReferences">A list of references to add to external targets.</param>
        protected virtual void AddReverseReferences(IDictionary<NodeId, IList<IReference>> externalReferences)
        {
            foreach (var source in PredefinedNodes.Values)
            {
                // assign a default value to any variable value.

                if (source is BaseVariableState variable && variable.Value == null)
                {
                    variable.Value = TypeInfo.GetDefaultValue(variable.DataType, variable.ValueRank, Server.TypeTree);
                }

                // add reference from supertype for type nodes.
                /*
                BaseTypeState type = source as BaseTypeState;

                if (type != null && !NodeId.IsNull(type.SuperTypeId))
                {
                    if (!IsNodeIdInNamespace(type.SuperTypeId))
                    {
                        AddExternalReference(
                            type.SuperTypeId,
                            ReferenceTypeIds.HasSubtype,
                            false,
                            type.NodeId,
                            externalReferences);
                    }
                }
                */

                var references = new List<IReference>();
                source.GetReferences(SystemContext, references);

                for (var ii = 0; ii < references.Count; ii++)
                {
                    var reference = references[ii];

                    // nothing to do with external nodes.
                    if (reference.TargetId?.IsAbsolute != false)
                    {
                        continue;
                    }

                    var targetId = (NodeId)reference.TargetId;

                    // add inverse reference to internal targets.
                    NodeState target = null;

                    if (PredefinedNodes.TryGetValue(targetId, out target))
                    {
                        if (!target.ReferenceExists(reference.ReferenceTypeId, !reference.IsInverse, source.NodeId))
                        {
                            target.AddReference(reference.ReferenceTypeId, !reference.IsInverse, source.NodeId);
                        }

                        continue;
                    }

                    // check for inverse references to external notifiers.
                    if (reference.IsInverse && reference.ReferenceTypeId == ReferenceTypeIds.HasNotifier)
                    {
                        AddRootNotifier(source);
                    }

                    // nothing more to do for references to nodes managed by this manager.
                    if (IsNodeIdInNamespace(targetId))
                    {
                        continue;
                    }

                    // add external reference.
                    AddExternalReference(
                        targetId,
                        reference.ReferenceTypeId,
                        !reference.IsInverse,
                        source.NodeId,
                        externalReferences);
                }
            }
        }

        /// <summary>
        /// Adds an external reference to the dictionary.
        /// </summary>
        /// <param name="sourceId"></param>
        /// <param name="referenceTypeId"></param>
        /// <param name="isInverse"></param>
        /// <param name="targetId"></param>
        /// <param name="externalReferences"></param>
        protected void AddExternalReference(
            NodeId sourceId,
            NodeId referenceTypeId,
            bool isInverse,
            NodeId targetId,
            IDictionary<NodeId, IList<IReference>> externalReferences)
        {
            // get list of references to external nodes.
            IList<IReference> referencesToAdd = null;

            if (!externalReferences.TryGetValue(sourceId, out referencesToAdd))
            {
                externalReferences[sourceId] = referencesToAdd = [];
            }

            // add reserve reference from external node.
            var referenceToAdd = new ReferenceNode
            {
                ReferenceTypeId = referenceTypeId,
                IsInverse = isInverse,
                TargetId = targetId
            };

            referencesToAdd.Add(referenceToAdd);
        }

        /// <summary>
        /// Recursively adds the types to the type tree.
        /// </summary>
        /// <param name="type"></param>
        protected void AddTypesToTypeTree(BaseTypeState type)
        {
            if (!NodeId.IsNull(type.SuperTypeId) && !Server.TypeTree.IsKnown(type.SuperTypeId))
            {
                AddTypesToTypeTree(type.SuperTypeId);
            }

            if (type.NodeClass != NodeClass.ReferenceType)
            {
                Server.TypeTree.AddSubtype(type.NodeId, type.SuperTypeId);
            }
            else
            {
                Server.TypeTree.AddReferenceSubtype(type.NodeId, type.SuperTypeId, type.BrowseName);
            }
        }

        /// <summary>
        /// Recursively adds the types to the type tree.
        /// </summary>
        /// <param name="typeId"></param>
        protected void AddTypesToTypeTree(NodeId typeId)
        {
            if (Find(typeId) is not BaseTypeState type)
            {
                return;
            }

            AddTypesToTypeTree(type);
        }

        /// <summary>
        /// Finds the specified node and checks if it is of the expected type.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="expectedType"></param>
        /// <returns>Returns null if not found or not of the correct type.</returns>
        public NodeState FindPredefinedNode(NodeId nodeId, Type expectedType)
        {
            if (nodeId == null)
            {
                return null;
            }

            NodeState node = null;

            if (!PredefinedNodes.TryGetValue(nodeId, out node))
            {
                return null;
            }

            if (expectedType?.IsInstanceOfType(node) == false)
            {
                return null;
            }

            return node;
        }

        /// <summary>
        /// Frees any resources allocated for the address space.
        /// </summary>
        public virtual void DeleteAddressSpace()
        {
            lock (Lock)
            {
                PredefinedNodes.Clear();
            }
        }

        /// <summary>
        /// Returns a unique handle for the node.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <remarks>
        /// This must efficiently determine whether the node belongs to the node manager. If it does belong to
        /// NodeManager it should return a handle that does not require the NodeId to be validated again when
        /// the handle is passed into other methods such as 'Read' or 'Write'.
        /// </remarks>
        public virtual object GetManagerHandle(NodeId nodeId)
        {
            lock (Lock)
            {
                return GetManagerHandle(SystemContext, nodeId, null);
            }
        }

        /// <summary>
        /// Returns a unique handle for the node.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="nodeId"></param>
        /// <param name="cache"></param>
        /// <remarks>
        /// This must efficiently determine whether the node belongs to the node manager. If it does belong to
        /// NodeManager it should return a handle that does not require the NodeId to be validated again when
        /// the handle is passed into other methods such as 'Read' or 'Write'.
        /// </remarks>
        protected virtual object GetManagerHandle(ISystemContext context, NodeId nodeId, IDictionary<NodeId, NodeState> cache)
        {
            lock (Lock)
            {
                // quickly exclude nodes that not in the namespace.
                if (!IsNodeIdInNamespace(nodeId))
                {
                    return null;
                }

                // lookup the node.
                NodeState node = null;

                if (!PredefinedNodes.TryGetValue(nodeId, out node))
                {
                    return null;
                }

                return node;
            }
        }

        /// <summary>
        /// This method is used to add bi-directional references to nodes from other node managers.
        /// </summary>
        /// <param name="references"></param>
        /// <remarks>
        /// The additional references are optional, however, the NodeManager should support them.
        /// </remarks>
        public virtual void AddReferences(IDictionary<NodeId, IList<IReference>> references)
        {
            lock (Lock)
            {
                foreach (var current in references)
                {
                    // check for valid handle.

                    if (GetManagerHandle(SystemContext, current.Key, null) is not NodeState source)
                    {
                        continue;
                    }

                    // add reference to external target.
                    foreach (var reference in current.Value)
                    {
                        source.AddReference(reference.ReferenceTypeId, reference.IsInverse, reference.TargetId);
                    }
                }
            }
        }

        /// <summary>
        /// This method is used to delete bi-directional references to nodes from other node managers.
        /// </summary>
        /// <param name="sourceHandle"></param>
        /// <param name="referenceTypeId"></param>
        /// <param name="isInverse"></param>
        /// <param name="targetId"></param>
        /// <param name="deleteBidirectional"></param>
        public virtual ServiceResult DeleteReference(
            object sourceHandle,
            NodeId referenceTypeId,
            bool isInverse,
            ExpandedNodeId targetId,
            bool deleteBidirectional)
        {
            lock (Lock)
            {
                // check for valid handle.
                var source = IsHandleInNamespace(sourceHandle);

                if (source == null)
                {
                    return StatusCodes.BadNodeIdUnknown;
                }

                source.RemoveReference(referenceTypeId, isInverse, targetId);

                if (deleteBidirectional)
                {
                    // check if the target is also managed by the node manager.
                    if (!targetId.IsAbsolute && GetManagerHandle(SystemContext, (NodeId)targetId, null) is NodeState target)
                    {
                        target.RemoveReference(referenceTypeId, !isInverse, source.NodeId);
                    }
                }

                return ServiceResult.Good;
            }
        }

        /// <summary>
        /// Returns the basic metadata for the node. Returns null if the node does not exist.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="targetHandle"></param>
        /// <param name="resultMask"></param>
        /// <remarks>
        /// This method validates any placeholder handle.
        /// </remarks>
        public virtual NodeMetadata GetNodeMetadata(
            OperationContext context,
            object targetHandle,
            BrowseResultMask resultMask)
        {
            var systemContext = SystemContext.Copy(context);

            lock (Lock)
            {
                // check for valid handle.
                var target = IsHandleInNamespace(targetHandle);

                if (target == null)
                {
                    return null;
                }

                // validate node.
                if (!ValidateNode(systemContext, target))
                {
                    return null;
                }

                // read the attributes.
                var values = target.ReadAttributes(
                    systemContext,
                    Attributes.WriteMask,
                    Attributes.UserWriteMask,
                    Attributes.DataType,
                    Attributes.ValueRank,
                    Attributes.ArrayDimensions,
                    Attributes.AccessLevel,
                    Attributes.UserAccessLevel,
                    Attributes.EventNotifier,
                    Attributes.Executable,
                    Attributes.UserExecutable);

                // construct the metadata object.

                var metadata = new NodeMetadata(target, target.NodeId)
                {
                    NodeClass = target.NodeClass,
                    BrowseName = target.BrowseName,
                    DisplayName = target.DisplayName
                };

                if (values[0] != null && values[1] != null)
                {
                    metadata.WriteMask = (AttributeWriteMask)(((uint)values[0]) & ((uint)values[1]));
                }

                metadata.DataType = (NodeId)values[2];

                if (values[3] != null)
                {
                    metadata.ValueRank = (int)values[3];
                }

                metadata.ArrayDimensions = (IList<uint>)values[4];

                if (values[5] != null && values[6] != null)
                {
                    metadata.AccessLevel = (byte)(((byte)values[5]) & ((byte)values[6]));
                }

                if (values[7] != null)
                {
                    metadata.EventNotifier = (byte)values[7];
                }

                if (values[8] != null && values[9] != null)
                {
                    metadata.Executable = (((bool)values[8]) && ((bool)values[9]));
                }

                // get instance references.

                if (target is BaseInstanceState instance)
                {
                    metadata.TypeDefinition = instance.TypeDefinitionId;
                    metadata.ModellingRule = instance.ModellingRuleId;
                }

                // fill in the common attributes.
                return metadata;
            }
        }

        /// <summary>
        /// Browses the references from a node managed by the node manager.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="continuationPoint"></param>
        /// <param name="references"></param>
        /// <remarks>
        /// The continuation point is created for every browse operation and contains the browse parameters.
        /// The node manager can store its state information in the Data and Index properties.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="continuationPoint"/> is <c>null</c>.</exception>
        /// <exception cref="ServiceResultException"></exception>
        public virtual void Browse(
            OperationContext context,
            ref ContinuationPoint continuationPoint,
            IList<ReferenceDescription> references)
        {
            ArgumentNullException.ThrowIfNull(continuationPoint);
            ArgumentNullException.ThrowIfNull(references);

            // check for view.
            if (!ViewDescription.IsDefault(continuationPoint.View))
            {
                throw new ServiceResultException(StatusCodes.BadViewIdUnknown);
            }

            var systemContext = SystemContext.Copy(context);

            lock (Lock)
            {
                // verify that the node exists.
                var source = IsHandleInNamespace(continuationPoint.NodeToBrowse);

                if (source == null)
                {
                    throw new ServiceResultException(StatusCodes.BadNodeIdUnknown);
                }

                // validate node.
                if (!ValidateNode(systemContext, source))
                {
                    throw new ServiceResultException(StatusCodes.BadNodeIdUnknown);
                }

                // check for previous continuation point.

                // fetch list of references.
                if (continuationPoint.Data is not INodeBrowser browser)
                {
                    // create a new browser.
                    browser = source.CreateBrowser(
                        systemContext,
                        continuationPoint.View,
                        continuationPoint.ReferenceTypeId,
                        continuationPoint.IncludeSubtypes,
                        continuationPoint.BrowseDirection,
                        null,
                        null,
                        false);
                }

                // apply filters to references.
                for (var reference = browser.Next(); reference != null; reference = browser.Next())
                {
                    // create the type definition reference.
                    var description = GetReferenceDescription(context, reference, continuationPoint);

                    if (description == null)
                    {
                        continue;
                    }

                    // check if limit reached.
                    if (continuationPoint.MaxResultsToReturn != 0 && references.Count >= continuationPoint.MaxResultsToReturn)
                    {
                        browser.Push(reference);
                        continuationPoint.Data = browser;
                        return;
                    }

                    references.Add(description);
                }

                // release the continuation point if all done.
                continuationPoint.Dispose();
                continuationPoint = null;
            }
        }

        /// <summary>
        /// Returns the references for the node that meets the criteria specified.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="reference"></param>
        /// <param name="continuationPoint"></param>
        private ReferenceDescription GetReferenceDescription(
            OperationContext context,
            IReference reference,
            ContinuationPoint continuationPoint)
        {
            // create the type definition reference.
            var description = new ReferenceDescription
            {
                NodeId = reference.TargetId
            };
            description.SetReferenceType(continuationPoint.ResultMask, reference.ReferenceTypeId, !reference.IsInverse);

            // do not cache target parameters for remote nodes.
            if (reference.TargetId.IsAbsolute)
            {
                // only return remote references if no node class filter is specified.
                if (continuationPoint.NodeClassMask != 0)
                {
                    return null;
                }

                return description;
            }

            NodeState target = null;

            // check for local reference.

            if (reference is NodeStateReference referenceInfo)
            {
                target = referenceInfo.Target;
            }

            // check for internal reference.
            if (target == null)
            {
                var targetId = (NodeId)reference.TargetId;

                if (IsNodeIdInNamespace(targetId) && !PredefinedNodes.TryGetValue(targetId, out target))
                {
                    target = null;
                }
            }

            // the target may be a reference to a node in another node manager. In these cases
            // the target attributes must be fetched by the caller. The Unfiltered flag tells the
            // caller to do that.
            if (target == null)
            {
                description.Unfiltered = true;
                return description;
            }

            // apply node class filter.
            if (continuationPoint.NodeClassMask != 0 && ((continuationPoint.NodeClassMask & (uint)target.NodeClass) == 0))
            {
                return null;
            }

            NodeId typeDefinition = null;

            if (target is BaseInstanceState instance)
            {
                typeDefinition = instance.TypeDefinitionId;
            }

            // set target attributes.
            description.SetTargetAttributes(
                continuationPoint.ResultMask,
                target.NodeClass,
                target.BrowseName,
                target.DisplayName,
                typeDefinition);

            return description;
        }

        /// <summary>
        /// Returns the target of the specified browse path fragment(s).
        /// </summary>
        /// <param name="context"></param>
        /// <param name="sourceHandle"></param>
        /// <param name="relativePath"></param>
        /// <param name="targetIds"></param>
        /// <param name="unresolvedTargetIds"></param>
        /// <remarks>
        /// If reference exists but the node manager does not know the browse name it must
        /// return the NodeId as an unresolvedTargetIds. The caller will try to check the
        /// browse name.
        /// </remarks>
        public virtual void TranslateBrowsePath(
            OperationContext context,
            object sourceHandle,
            RelativePathElement relativePath,
            IList<ExpandedNodeId> targetIds,
            IList<NodeId> unresolvedTargetIds)
        {
            var systemContext = SystemContext.Copy(context);
            IDictionary<NodeId, NodeState> operationCache = new NodeIdDictionary<NodeState>();

            lock (Lock)
            {
                // verify that the node exists.
                var source = IsHandleInNamespace(sourceHandle);

                if (source == null)
                {
                    return;
                }

                // validate node.
                if (!ValidateNode(systemContext, source))
                {
                    return;
                }

                // get list of references that relative path.
                var browser = source.CreateBrowser(
                    systemContext,
                    null,
                    relativePath.ReferenceTypeId,
                    relativePath.IncludeSubtypes,
                    (relativePath.IsInverse) ? BrowseDirection.Inverse : BrowseDirection.Forward,
                    relativePath.TargetName,
                    null,
                    false);

                // check the browse names.
                try
                {
                    for (var reference = browser.Next(); reference != null; reference = browser.Next())
                    {
                        // ignore unknown external references.
                        if (reference.TargetId.IsAbsolute)
                        {
                            continue;
                        }

                        NodeState target = null;

                        // check for local reference.

                        if (reference is NodeStateReference referenceInfo)
                        {
                            target = referenceInfo.Target;
                        }

                        if (target == null)
                        {
                            var targetId = (NodeId)reference.TargetId;

                            // the target may be a reference to a node in another node manager.
                            if (!IsNodeIdInNamespace(targetId))
                            {
                                unresolvedTargetIds.Add((NodeId)reference.TargetId);
                                continue;
                            }

                            // look up the target manually.
                            target = GetManagerHandle(systemContext, targetId, operationCache) as NodeState;

                            if (target == null)
                            {
                                continue;
                            }
                        }

                        // check browse name.
                        if (target.BrowseName == relativePath.TargetName)
                        {
                            // ensure duplicate node ids are not added.
                            if (!targetIds.Contains(reference.TargetId))
                            {
                                targetIds.Add(reference.TargetId);
                            }
                        }
                    }
                }
                finally
                {
                    browser.Dispose();
                }
            }
        }

        /// <summary>
        /// Reads the value for the specified attribute.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="maxAge"></param>
        /// <param name="nodesToRead"></param>
        /// <param name="values"></param>
        /// <param name="errors"></param>
        public virtual void Read(
            OperationContext context,
            double maxAge,
            IList<ReadValueId> nodesToRead,
            IList<DataValue> values,
            IList<ServiceResult> errors)
        {
            var systemContext = SystemContext.Copy(context);
            IDictionary<NodeId, NodeState> operationCache = new NodeIdDictionary<NodeState>();
            var nodesToValidate = new List<ReadWriteOperationState>();

            lock (Lock)
            {
                for (var ii = 0; ii < nodesToRead.Count; ii++)
                {
                    var nodeToRead = nodesToRead[ii];

                    // skip items that have already been processed.
                    if (nodeToRead.Processed)
                    {
                        continue;
                    }

                    // check for valid handle.

                    if (GetManagerHandle(systemContext, nodeToRead.NodeId, operationCache) is not NodeState source)
                    {
                        continue;
                    }

                    // owned by this node manager.
                    nodeToRead.Processed = true;

                    // create an initial value.
                    var value = values[ii] = new DataValue();

                    value.Value = null;
                    value.ServerTimestamp = DateTime.UtcNow;
                    value.SourceTimestamp = DateTime.MinValue;
                    value.StatusCode = StatusCodes.Good;

                    // check if the node is ready for reading.
                    if (source.ValidationRequired)
                    {
                        errors[ii] = StatusCodes.BadNodeIdUnknown;

                        // must validate node in a separate operation.
                        var operation = new ReadWriteOperationState
                        {
                            Source = source,
                            Index = ii
                        };

                        nodesToValidate.Add(operation);

                        continue;
                    }

                    // read the attribute value.
                    errors[ii] = source.ReadAttribute(
                        systemContext,
                        nodeToRead.AttributeId,
                        nodeToRead.ParsedIndexRange,
                        nodeToRead.DataEncoding,
                        value);
                }

                // check for nothing to do.
                if (nodesToValidate.Count == 0)
                {
                    return;
                }

                // validates the nodes (reads values from the underlying data source if required).
                for (var ii = 0; ii < nodesToValidate.Count; ii++)
                {
                    var operation = nodesToValidate[ii];

                    if (!ValidateNode(systemContext, operation.Source))
                    {
                        continue;
                    }

                    var nodeToRead = nodesToRead[operation.Index];
                    var value = values[operation.Index];

                    // update the attribute value.
                    errors[operation.Index] = operation.Source.ReadAttribute(
                        systemContext,
                        nodeToRead.AttributeId,
                        nodeToRead.ParsedIndexRange,
                        nodeToRead.DataEncoding,
                        value);
                }
            }
        }

        /// <summary>
        /// Stores the state of a call method operation.
        /// </summary>
        private struct ReadWriteOperationState
        {
            public NodeState Source;
            public int Index;
        }

        /// <summary>
        /// Verifies that the specified node exists.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="node"></param>
        protected virtual bool ValidateNode(ServerSystemContext context, NodeState node)
        {
            // validate node only if required.
            if (node.ValidationRequired)
            {
                return node.Validate(context);
            }

            return true;
        }

        /// <summary>
        /// Reads the history for the specified nodes.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="details"></param>
        /// <param name="timestampsToReturn"></param>
        /// <param name="releaseContinuationPoints"></param>
        /// <param name="nodesToRead"></param>
        /// <param name="results"></param>
        /// <param name="errors"></param>
        public virtual void HistoryRead(
            OperationContext context,
            HistoryReadDetails details,
            TimestampsToReturn timestampsToReturn,
            bool releaseContinuationPoints,
            IList<HistoryReadValueId> nodesToRead,
            IList<HistoryReadResult> results,
            IList<ServiceResult> errors)
        {
            var systemContext = SystemContext.Copy(context);
            IDictionary<NodeId, NodeState> operationCache = new NodeIdDictionary<NodeState>();
            var nodesToValidate = new List<ReadWriteOperationState>();
            var readsToComplete = new List<ReadWriteOperationState>();

            lock (Lock)
            {
                for (var ii = 0; ii < nodesToRead.Count; ii++)
                {
                    var nodeToRead = nodesToRead[ii];

                    // skip items that have already been processed.
                    if (nodeToRead.Processed)
                    {
                        continue;
                    }

                    // check for valid handle.

                    if (GetManagerHandle(systemContext, nodeToRead.NodeId, operationCache) is not NodeState source)
                    {
                        continue;
                    }

                    // owned by this node manager.
                    nodeToRead.Processed = true;

                    // only variables supported.

                    if (source is not BaseVariableState variable)
                    {
                        errors[ii] = StatusCodes.BadHistoryOperationUnsupported;
                        continue;
                    }

                    results[ii] = new HistoryReadResult();

                    var operation = new ReadWriteOperationState
                    {
                        Source = source,
                        Index = ii
                    };

                    // check if the node is ready for reading.
                    if (source.ValidationRequired)
                    {
                        // must validate node in a separate operation.
                        errors[ii] = StatusCodes.BadNodeIdUnknown;
                        nodesToValidate.Add(operation);
                        continue;
                    }

                    // read the data.
                    readsToComplete.Add(operation);
                }

                // validates the nodes (reads values from the underlying data source if required).
                for (var ii = 0; ii < nodesToValidate.Count; ii++)
                {
                    var operation = nodesToValidate[ii];

                    if (!ValidateNode(systemContext, operation.Source))
                    {
                        continue;
                    }

                    readsToComplete.Add(operation);
                }
            }

            // reads the data without holding onto the lock.
            for (var ii = 0; ii < readsToComplete.Count; ii++)
            {
                var operation = readsToComplete[ii];

                errors[operation.Index] = HistoryRead(
                    systemContext,
                    operation.Source,
                    details,
                    timestampsToReturn,
                    releaseContinuationPoints,
                    nodesToRead[operation.Index],
                    results[operation.Index]);
            }
        }

        /// <summary>
        /// Reads the history for a single node which has already been validated.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="source"></param>
        /// <param name="details"></param>
        /// <param name="timestampsToReturn"></param>
        /// <param name="releaseContinuationPoints"></param>
        /// <param name="nodesToRead"></param>
        /// <param name="result"></param>
        protected virtual ServiceResult HistoryRead(
            ISystemContext context,
            NodeState source,
            HistoryReadDetails details,
            TimestampsToReturn timestampsToReturn,
            bool releaseContinuationPoints,
            HistoryReadValueId nodesToRead,
            HistoryReadResult result)
        {
            // check for variable.

            if (source is not BaseVariableState variable)
            {
                return StatusCodes.BadHistoryOperationUnsupported;
            }

            // check for access.
            lock (Lock)
            {
                if ((variable.AccessLevel & AccessLevels.HistoryRead) == 0)
                {
                    return StatusCodes.BadNotReadable;
                }
            }

            // handle read raw.

            if (details is ReadRawModifiedDetails readRawDetails)
            {
                return HistoryReadRaw(
                    context,
                    variable,
                    readRawDetails,
                    timestampsToReturn,
                    releaseContinuationPoints,
                    nodesToRead,
                    result);
            }

            // handle read processed.

            if (details is ReadProcessedDetails readProcessedDetails)
            {
                return HistoryReadProcessed(
                    context,
                    variable,
                    readProcessedDetails,
                    timestampsToReturn,
                    releaseContinuationPoints,
                    nodesToRead,
                    result);
            }

            // handle read processed.

            if (details is ReadAtTimeDetails readAtTimeDetails)
            {
                return HistoryReadAtTime(
                    context,
                    variable,
                    readAtTimeDetails,
                    timestampsToReturn,
                    releaseContinuationPoints,
                    nodesToRead,
                    result);
            }

            return StatusCodes.BadHistoryOperationUnsupported;
        }

        /// <summary>
        /// Reads the raw history for the variable value.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="source"></param>
        /// <param name="details"></param>
        /// <param name="timestampsToReturn"></param>
        /// <param name="releaseContinuationPoints"></param>
        /// <param name="nodeToRead"></param>
        /// <param name="result"></param>
        protected virtual ServiceResult HistoryReadRaw(
            ISystemContext context,
            BaseVariableState source,
            ReadRawModifiedDetails details,
            TimestampsToReturn timestampsToReturn,
            bool releaseContinuationPoints,
            HistoryReadValueId nodeToRead,
            HistoryReadResult result)
        {
            return StatusCodes.BadHistoryOperationUnsupported;
        }

        /// <summary>
        /// Reads the processed history for the variable value.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="source"></param>
        /// <param name="details"></param>
        /// <param name="timestampsToReturn"></param>
        /// <param name="releaseContinuationPoints"></param>
        /// <param name="nodeToRead"></param>
        /// <param name="result"></param>
        protected virtual ServiceResult HistoryReadProcessed(
            ISystemContext context,
            BaseVariableState source,
            ReadProcessedDetails details,
            TimestampsToReturn timestampsToReturn,
            bool releaseContinuationPoints,
            HistoryReadValueId nodeToRead,
            HistoryReadResult result)
        {
            return StatusCodes.BadHistoryOperationUnsupported;
        }

        /// <summary>
        /// Reads the history for the variable value.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="source"></param>
        /// <param name="details"></param>
        /// <param name="timestampsToReturn"></param>
        /// <param name="releaseContinuationPoints"></param>
        /// <param name="nodeToRead"></param>
        /// <param name="result"></param>
        protected virtual ServiceResult HistoryReadAtTime(
            ISystemContext context,
            BaseVariableState source,
            ReadAtTimeDetails details,
            TimestampsToReturn timestampsToReturn,
            bool releaseContinuationPoints,
            HistoryReadValueId nodeToRead,
            HistoryReadResult result)
        {
            return StatusCodes.BadHistoryOperationUnsupported;
        }

        /// <summary>
        /// Writes the value for the specified attributes.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="nodesToWrite"></param>
        /// <param name="errors"></param>
        public virtual void Write(
            OperationContext context,
            IList<WriteValue> nodesToWrite,
            IList<ServiceResult> errors)
        {
            var systemContext = SystemContext.Copy(context);
            IDictionary<NodeId, NodeState> operationCache = new NodeIdDictionary<NodeState>();
            var nodesToValidate = new List<ReadWriteOperationState>();

            lock (Lock)
            {
                for (var ii = 0; ii < nodesToWrite.Count; ii++)
                {
                    var nodeToWrite = nodesToWrite[ii];

                    // skip items that have already been processed.
                    if (nodeToWrite.Processed)
                    {
                        continue;
                    }

                    // check for valid handle.

                    if (GetManagerHandle(systemContext, nodeToWrite.NodeId, operationCache) is not NodeState source)
                    {
                        continue;
                    }

                    // owned by this node manager.
                    nodeToWrite.Processed = true;

                    // index range is not supported.
                    if (!string.IsNullOrEmpty(nodeToWrite.IndexRange))
                    {
                        errors[ii] = StatusCodes.BadWriteNotSupported;
                        continue;
                    }

                    // check if the node is ready for reading.
                    if (source.ValidationRequired)
                    {
                        errors[ii] = StatusCodes.BadNodeIdUnknown;

                        // must validate node in a separate operation.
                        var operation = new ReadWriteOperationState
                        {
                            Source = source,
                            Index = ii
                        };

                        nodesToValidate.Add(operation);

                        continue;
                    }

                    // write the attribute value.
                    errors[ii] = source.WriteAttribute(
                        systemContext,
                        nodeToWrite.AttributeId,
                        nodeToWrite.ParsedIndexRange,
                        nodeToWrite.Value);

                    // updates to source finished - report changes to monitored items.
                    source.ClearChangeMasks(systemContext, false);
                }

                // check for nothing to do.
                if (nodesToValidate.Count == 0)
                {
                    return;
                }

                // validates the nodes (reads values from the underlying data source if required).
                for (var ii = 0; ii < nodesToValidate.Count; ii++)
                {
                    var operation = nodesToValidate[ii];

                    if (!ValidateNode(systemContext, operation.Source))
                    {
                        continue;
                    }

                    var nodeToWrite = nodesToWrite[operation.Index];

                    // write the attribute value.
                    errors[operation.Index] = operation.Source.WriteAttribute(
                        systemContext,
                        nodeToWrite.AttributeId,
                        nodeToWrite.ParsedIndexRange,
                        nodeToWrite.Value);

                    // updates to source finished - report changes to monitored items.
                    operation.Source.ClearChangeMasks(systemContext, false);
                }
            }
        }

        /// <summary>
        /// Updates the history for the specified nodes.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="detailsType"></param>
        /// <param name="nodesToUpdate"></param>
        /// <param name="results"></param>
        /// <param name="errors"></param>
        public virtual void HistoryUpdate(
            OperationContext context,
            Type detailsType,
            IList<HistoryUpdateDetails> nodesToUpdate,
            IList<HistoryUpdateResult> results,
            IList<ServiceResult> errors)
        {
            var systemContext = SystemContext.Copy(context);
            IDictionary<NodeId, NodeState> operationCache = new NodeIdDictionary<NodeState>();
            var nodesToValidate = new List<ReadWriteOperationState>();

            lock (Lock)
            {
                for (var ii = 0; ii < nodesToUpdate.Count; ii++)
                {
                    var nodeToUpdate = nodesToUpdate[ii];

                    // skip items that have already been processed.
                    if (nodeToUpdate.Processed)
                    {
                        continue;
                    }

                    // check for valid handle.

                    if (GetManagerHandle(systemContext, nodeToUpdate.NodeId, operationCache) is not NodeState source)
                    {
                        continue;
                    }

                    // owned by this node manager.
                    nodeToUpdate.Processed = true;

                    // check if the node is ready for reading.
                    if (source.ValidationRequired)
                    {
                        errors[ii] = StatusCodes.BadNodeIdUnknown;

                        // must validate node in a separate operation.
                        var operation = new ReadWriteOperationState
                        {
                            Source = source,
                            Index = ii
                        };

                        nodesToValidate.Add(operation);

                        continue;
                    }

                    // historical data not available.
                    errors[ii] = StatusCodes.BadHistoryOperationUnsupported;
                }

                // check for nothing to do.
                if (nodesToValidate.Count == 0)
                {
                    return;
                }

                // validates the nodes (reads values from the underlying data source if required).
                for (var ii = 0; ii < nodesToValidate.Count; ii++)
                {
                    var operation = nodesToValidate[ii];

                    if (!ValidateNode(systemContext, operation.Source))
                    {
                        continue;
                    }

                    // historical data not available.
                    errors[ii] = StatusCodes.BadHistoryOperationUnsupported;
                }
            }
        }

        /// <summary>
        /// Calls a method on the specified nodes.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="methodsToCall"></param>
        /// <param name="results"></param>
        /// <param name="errors"></param>
        public virtual void Call(
            OperationContext context,
            IList<CallMethodRequest> methodsToCall,
            IList<CallMethodResult> results,
            IList<ServiceResult> errors)
        {
            var systemContext = SystemContext.Copy(context);
            IDictionary<NodeId, NodeState> operationCache = new NodeIdDictionary<NodeState>();
            var nodesToValidate = new List<CallOperationState>();

            lock (Lock)
            {
                for (var ii = 0; ii < methodsToCall.Count; ii++)
                {
                    var methodToCall = methodsToCall[ii];

                    // skip items that have already been processed.
                    if (methodToCall.Processed)
                    {
                        continue;
                    }

                    // check for valid handle.

                    if (GetManagerHandle(systemContext, methodToCall.ObjectId, operationCache) is not NodeState source)
                    {
                        continue;
                    }

                    // owned by this node manager.
                    methodToCall.Processed = true;

                    // find the method.
                    var method = source.FindMethod(systemContext, methodToCall.MethodId);

                    if (method == null)
                    {
                        // check for loose coupling.
                        if (source.ReferenceExists(ReferenceTypeIds.HasComponent, false, methodToCall.MethodId))
                        {
                            method = (MethodState)FindPredefinedNode(methodToCall.MethodId, typeof(MethodState));
                        }

                        if (method == null)
                        {
                            errors[ii] = StatusCodes.BadMethodInvalid;
                            continue;
                        }
                    }

                    var result = results[ii] = new CallMethodResult();

                    // check if the node is ready for reading.
                    if (source.ValidationRequired)
                    {
                        errors[ii] = StatusCodes.BadNodeIdUnknown;

                        // must validate node in a separate operation.
                        var operation = new CallOperationState
                        {
                            Source = source,
                            Method = method,
                            Index = ii
                        };

                        nodesToValidate.Add(operation);

                        continue;
                    }

                    // call the method.
                    errors[ii] = Call(
                        systemContext,
                        methodToCall,
                        source,
                        method,
                        result);
                }

                // check for nothing to do.
                if (nodesToValidate.Count == 0)
                {
                    return;
                }

                // validates the nodes (reads values from the underlying data source if required).
                for (var ii = 0; ii < nodesToValidate.Count; ii++)
                {
                    var operation = nodesToValidate[ii];

                    // validate the object.
                    if (!ValidateNode(systemContext, operation.Source))
                    {
                        continue;
                    }

                    // call the method.
                    var result = results[operation.Index];

                    errors[operation.Index] = Call(
                        systemContext,
                        methodsToCall[operation.Index],
                        operation.Source,
                        operation.Method,
                        result);
                }
            }
        }

        /// <summary>
        /// Stores the state of a call method operation.
        /// </summary>
        private struct CallOperationState
        {
            public NodeState Source;
            public MethodState Method;
            public int Index;
        }

        /// <summary>
        /// Calls a method on an object.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="methodToCall"></param>
        /// <param name="source"></param>
        /// <param name="method"></param>
        /// <param name="result"></param>
        protected virtual ServiceResult Call(
            ISystemContext context,
            CallMethodRequest methodToCall,
            NodeState source,
            MethodState method,
            CallMethodResult result)
        {
            var systemContext = context as ServerSystemContext;
            var argumentErrors = new List<ServiceResult>();
            var outputArguments = new VariantCollection();

            var error = method.Call(
                context,
                methodToCall.ObjectId,
                methodToCall.InputArguments,
                argumentErrors,
                outputArguments);

            if (ServiceResult.IsBad(error))
            {
                return error;
            }

            // check for argument errors.
            var argumentsValid = true;

            for (var jj = 0; jj < argumentErrors.Count; jj++)
            {
                var argumentError = argumentErrors[jj];

                if (argumentError != null)
                {
                    result.InputArgumentResults.Add(argumentError.StatusCode);

                    if (ServiceResult.IsBad(argumentError))
                    {
                        argumentsValid = false;
                    }
                }
                else
                {
                    result.InputArgumentResults.Add(StatusCodes.Good);
                }

                // only fill in diagnostic info if it is requested.
                if ((systemContext.OperationContext.DiagnosticsMask & DiagnosticsMasks.OperationAll) != 0)
                {
                    if (ServiceResult.IsBad(argumentError))
                    {
                        argumentsValid = false;
                        result.InputArgumentDiagnosticInfos.Add(new DiagnosticInfo(argumentError, systemContext.OperationContext.DiagnosticsMask, false, systemContext.OperationContext.StringTable));
                    }
                    else
                    {
                        result.InputArgumentDiagnosticInfos.Add(null);
                    }
                }
            }

            // check for validation errors.
            if (!argumentsValid)
            {
                result.StatusCode = StatusCodes.BadInvalidArgument;
                return result.StatusCode;
            }

            // do not return diagnostics if there are no errors.
            result.InputArgumentDiagnosticInfos.Clear();

            // return output arguments.
            result.OutputArguments = outputArguments;

            return ServiceResult.Good;
        }

        /// <summary>
        /// Subscribes or unsubscribes to events produced by the specified source.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="sourceId"></param>
        /// <param name="subscriptionId"></param>
        /// <param name="monitoredItem"></param>
        /// <param name="unsubscribe"></param>
        /// <remarks>
        /// This method is called when a event subscription is created or deletes. The node manager
        /// must  start/stop reporting events for the specified object and all objects below it in
        /// the notifier hierarchy.
        /// </remarks>
        public virtual ServiceResult SubscribeToEvents(
            OperationContext context,
            object sourceId,
            uint subscriptionId,
            IEventMonitoredItem monitoredItem,
            bool unsubscribe)
        {
            var systemContext = SystemContext.Copy(context);

            lock (Lock)
            {
                // check for valid handle.
                var source = IsHandleInNamespace(sourceId);

                if (source == null)
                {
                    return StatusCodes.BadNodeIdInvalid;
                }

                // check if the object supports subscritions.

                if (sourceId is not BaseObjectState instance || instance.EventNotifier != EventNotifiers.SubscribeToEvents)
                {
                    return StatusCodes.BadNotSupported;
                }

                var monitoredNode = instance.Handle as MonitoredNode;

                // handle unsubscribe.
                if (unsubscribe)
                {
                    if (monitoredNode != null)
                    {
                        monitoredNode.UnsubscribeToEvents(systemContext, monitoredItem);

                        // do any post processing.
                        OnUnsubscribeToEvents(systemContext, monitoredNode, monitoredItem);
                    }

                    return ServiceResult.Good;
                }

                // subscribe to events.
                if (monitoredNode == null)
                {
                    instance.Handle = monitoredNode = new MonitoredNode(Server, this, source);
                }

                monitoredNode.SubscribeToEvents(systemContext, monitoredItem);

                // do any post processing.
                OnSubscribeToEvents(systemContext, monitoredNode, monitoredItem);

                return ServiceResult.Good;
            }
        }

        /// <summary>
        /// Subscribes or unsubscribes to events produced by all event sources.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="subscriptionId"></param>
        /// <param name="monitoredItem"></param>
        /// <param name="unsubscribe"></param>
        /// <remarks>
        /// This method is called when a event subscription is created or deleted. The node
        /// manager must start/stop reporting events for all objects that it manages.
        /// </remarks>
        public virtual ServiceResult SubscribeToAllEvents(
            OperationContext context,
            uint subscriptionId,
            IEventMonitoredItem monitoredItem,
            bool unsubscribe)
        {
            var systemContext = SystemContext.Copy(context);

            lock (Lock)
            {
                // update root notifiers.
                for (var ii = 0; ii < RootNotifiers.Count; ii++)
                {
                    SubscribeToAllEvents(
                        systemContext,
                        monitoredItem,
                        unsubscribe,
                        RootNotifiers[ii]);
                }

                return ServiceResult.Good;
            }
        }

        /// <summary>
        /// Subscribes/unsubscribes to all events produced by the specified node.
        /// </summary>
        /// <param name="systemContext"></param>
        /// <param name="monitoredItem"></param>
        /// <param name="unsubscribe"></param>
        /// <param name="source"></param>
        protected void SubscribeToAllEvents(
            ISystemContext systemContext,
            IEventMonitoredItem monitoredItem,
            bool unsubscribe,
            NodeState source)
        {
            var monitoredNode = source.Handle as MonitoredNode;

            // handle unsubscribe.
            if (unsubscribe)
            {
                if (monitoredNode != null)
                {
                    monitoredNode.UnsubscribeToEvents(systemContext, monitoredItem);

                    // do any post processing.
                    OnUnsubscribeToEvents(systemContext, monitoredNode, monitoredItem);
                }

                return;
            }

            // subscribe to events.
            if (monitoredNode == null)
            {
                source.Handle = monitoredNode = new MonitoredNode(Server, this, source);
            }

            monitoredNode.SubscribeToEvents(systemContext, monitoredItem);

            // do any post processing.
            OnSubscribeToEvents(systemContext, monitoredNode, monitoredItem);
        }

        /// <summary>
        /// Does any processing after a monitored item is subscribed to.
        /// </summary>
        /// <param name="systemContext"></param>
        /// <param name="monitoredNode"></param>
        /// <param name="monitoredItem"></param>
        protected virtual void OnSubscribeToEvents(
            ISystemContext systemContext,
            MonitoredNode monitoredNode,
            IEventMonitoredItem monitoredItem)
        {
            // does nothing.
        }

        /// <summary>
        /// Does any processing after a monitored item is subscribed to.
        /// </summary>
        /// <param name="systemContext"></param>
        /// <param name="monitoredNode"></param>
        /// <param name="monitoredItem"></param>
        protected virtual void OnUnsubscribeToEvents(
            ISystemContext systemContext,
            MonitoredNode monitoredNode,
            IEventMonitoredItem monitoredItem)
        {
            // does nothing.
        }

        /// <summary>
        /// Tells the node manager to refresh any conditions associated with the specified monitored items.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="monitoredItems"></param>
        /// <remarks>
        /// This method is called when the condition refresh method is called for a subscription.
        /// The node manager must create a refresh event for each condition monitored by the subscription.
        /// </remarks>
        public virtual ServiceResult ConditionRefresh(
            OperationContext context,
            IList<IEventMonitoredItem> monitoredItems)
        {
            var systemContext = SystemContext.Copy(context);

            lock (Lock)
            {
                for (var ii = 0; ii < monitoredItems.Count; ii++)
                {
                    var monitoredItem = monitoredItems[ii];

                    if (monitoredItem == null)
                    {
                        continue;
                    }

                    // check for global subscription.
                    if (monitoredItem.MonitoringAllEvents)
                    {
                        for (var jj = 0; jj < RootNotifiers.Count; jj++)
                        {
                            if (RootNotifiers[jj].Handle is not MonitoredNode monitoredNode)
                            {
                                continue;
                            }

                            monitoredNode.ConditionRefresh(systemContext, monitoredItem);
                        }
                    }

                    // check for subscription to local node.
                    else
                    {
                        var source = IsHandleInNamespace(monitoredItem.ManagerHandle);

                        if (source == null)
                        {
                            continue;
                        }

                        if (source.Handle is not MonitoredNode monitoredNode)
                        {
                            continue;
                        }

                        monitoredNode.ConditionRefresh(systemContext, monitoredItem);
                    }
                }
            }

            return ServiceResult.Good;
        }

        /// <summary>
        /// Creates a new set of monitored items for a set of variables.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="subscriptionId"></param>
        /// <param name="publishingInterval"></param>
        /// <param name="timestampsToReturn"></param>
        /// <param name="itemsToCreate"></param>
        /// <param name="errors"></param>
        /// <param name="filterErrors"></param>
        /// <param name="monitoredItems"></param>
        /// <param name="createDurable"></param>
        /// <param name="globalIdCounter"></param>
        /// <remarks>
        /// This method only handles data change subscriptions. Event subscriptions are created by the SDK.
        /// </remarks>
        public virtual void CreateMonitoredItems(
            OperationContext context,
            uint subscriptionId,
            double publishingInterval,
            TimestampsToReturn timestampsToReturn,
            IList<MonitoredItemCreateRequest> itemsToCreate,
            IList<ServiceResult> errors,
            IList<MonitoringFilterResult> filterErrors,
            IList<IMonitoredItem> monitoredItems,
            bool createDurable,
            ref long globalIdCounter)
        {
            var systemContext = SystemContext.Copy(context);
            IDictionary<NodeId, NodeState> operationCache = new NodeIdDictionary<NodeState>();
            var nodesToValidate = new List<ReadWriteOperationState>();

            lock (Lock)
            {
                for (var ii = 0; ii < itemsToCreate.Count; ii++)
                {
                    var itemToCreate = itemsToCreate[ii];

                    // skip items that have already been processed.
                    if (itemToCreate.Processed)
                    {
                        continue;
                    }

                    var itemToMonitor = itemToCreate.ItemToMonitor;

                    // check for valid handle.

                    if (GetManagerHandle(systemContext, itemToMonitor.NodeId, operationCache) is not NodeState source)
                    {
                        continue;
                    }

                    // owned by this node manager.
                    itemToCreate.Processed = true;

                    // check if the node is ready for reading.
                    if (source.ValidationRequired)
                    {
                        errors[ii] = StatusCodes.BadNodeIdUnknown;

                        // must validate node in a separate operation.
                        var operation = new ReadWriteOperationState
                        {
                            Source = source,
                            Index = ii
                        };

                        nodesToValidate.Add(operation);

                        continue;
                    }

                    MonitoringFilterResult filterError = null;
                    IMonitoredItem monitoredItem = null;

                    errors[ii] = CreateMonitoredItem(
                        systemContext,
                        source,
                        subscriptionId,
                        publishingInterval,
                        context.DiagnosticsMask,
                        timestampsToReturn,
                        itemToCreate,
                        createDurable,
                        ref globalIdCounter,
                        out filterError,
                        out monitoredItem);

                    // save any filter error details.
                    filterErrors[ii] = filterError;

                    if (ServiceResult.IsBad(errors[ii]))
                    {
                        continue;
                    }

                    // save the monitored item.
                    monitoredItems[ii] = monitoredItem;
                }

                // check for nothing to do.
                if (nodesToValidate.Count == 0)
                {
                    return;
                }

                // validates the nodes (reads values from the underlying data source if required).
                for (var ii = 0; ii < nodesToValidate.Count; ii++)
                {
                    var operation = nodesToValidate[ii];

                    // validate the object.
                    if (!ValidateNode(systemContext, operation.Source))
                    {
                        continue;
                    }

                    var itemToCreate = itemsToCreate[operation.Index];

                    MonitoringFilterResult filterError = null;
                    IMonitoredItem monitoredItem = null;

                    errors[operation.Index] = CreateMonitoredItem(
                        systemContext,
                        operation.Source,
                        subscriptionId,
                        publishingInterval,
                        context.DiagnosticsMask,
                        timestampsToReturn,
                        itemToCreate,
                        createDurable,
                        ref globalIdCounter,
                        out filterError,
                        out monitoredItem);

                    // save any filter error details.
                    filterErrors[operation.Index] = filterError;

                    if (ServiceResult.IsBad(errors[operation.Index]))
                    {
                        continue;
                    }

                    // save the monitored item.
                    monitoredItems[operation.Index] = monitoredItem;
                }
            }
        }

        /// <summary>
        /// Restore a set of monitored items after a restart.
        /// </summary>
        /// <param name="itemsToRestore"></param>
        /// <param name="monitoredItems"></param>
        /// <param name="savedOwnerIdentity"></param>
        /// <exception cref="ArgumentNullException"><paramref name="itemsToRestore"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException"></exception>
        public virtual void RestoreMonitoredItems(
            IList<IStoredMonitoredItem> itemsToRestore,
            IList<IMonitoredItem> monitoredItems,
            IUserIdentity savedOwnerIdentity)
        {
            ArgumentNullException.ThrowIfNull(itemsToRestore);
            ArgumentNullException.ThrowIfNull(monitoredItems);

            if (Server.IsRunning)
            {
                throw new InvalidOperationException("Subscription restore can only occur on startup");
            }

            var systemContext = SystemContext.Copy();
            IDictionary<NodeId, NodeState> operationCache = new NodeIdDictionary<NodeState>();
            var nodesToValidate = new List<ReadWriteOperationState>();

            lock (Lock)
            {
                for (var ii = 0; ii < itemsToRestore.Count; ii++)
                {
                    var itemToCreate = itemsToRestore[ii];

                    // skip items that have already been processed.
                    if (itemToCreate.IsRestored)
                    {
                        continue;
                    }

                    // check for valid handle.

                    if (GetManagerHandle(systemContext, itemToCreate.NodeId, operationCache) is not NodeState source)
                    {
                        continue;
                    }

                    // owned by this node manager.
                    itemToCreate.IsRestored = true;

                    // check if the node is ready for reading.
                    if (source.ValidationRequired)
                    {
                        // must validate node in a separate operation.
                        var operation = new ReadWriteOperationState
                        {
                            Source = source,
                            Index = ii
                        };

                        nodesToValidate.Add(operation);

                        continue;
                    }

                    IMonitoredItem monitoredItem = null;

                    var success = RestoreMonitoredItem(systemContext, source, itemToCreate, out monitoredItem);

                    if (!success)
                    {
                        continue;
                    }

                    // save the monitored item.
                    monitoredItems[ii] = monitoredItem;
                }

                // check for nothing to do.
                if (nodesToValidate.Count == 0)
                {
                    return;
                }

                // validates the nodes (reads values from the underlying data source if required).
                for (var ii = 0; ii < nodesToValidate.Count; ii++)
                {
                    var operation = nodesToValidate[ii];

                    // validate the object.
                    if (!ValidateNode(systemContext, operation.Source))
                    {
                        continue;
                    }

                    var itemToCreate = itemsToRestore[operation.Index];

                    IMonitoredItem monitoredItem = null;

                    var success = RestoreMonitoredItem(systemContext, operation.Source, itemToCreate, out monitoredItem);

                    if (!success)
                    {
                        continue;
                    }

                    // save the monitored item.
                    monitoredItems[operation.Index] = monitoredItem;
                }
            }
        }

        /// <summary>
        /// Reads the initial value for a monitored item.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="node">The monitored node.</param>
        /// <param name="monitoredItem">The monitored item.</param>
        /// <param name="ignoreFilters">If the filters should be ignored.</param>
        protected virtual ServiceResult ReadInitialValue(
            ISystemContext context,
            MonitoredNode node,
            IDataChangeMonitoredItem2 monitoredItem,
            bool ignoreFilters)
        {
            var initialValue = new DataValue
            {
                Value = null,
                ServerTimestamp = DateTime.UtcNow,
                SourceTimestamp = DateTime.MinValue,
                StatusCode = StatusCodes.BadWaitingForInitialData
            };

            var error = node.Node.ReadAttribute(
                context,
                monitoredItem.AttributeId,
                monitoredItem.IndexRange,
                monitoredItem.DataEncoding,
                initialValue);

            monitoredItem.QueueValue(initialValue, error, ignoreFilters);

            return error;
        }

        /// <summary>
        /// Validates a data change filter provided by the client.
        /// </summary>
        /// <param name="context">The system context.</param>
        /// <param name="source">The node being monitored.</param>
        /// <param name="attributeId">The attribute being monitored.</param>
        /// <param name="requestedFilter">The requested monitoring filter.</param>
        /// <param name="filter">The validated data change filter.</param>
        /// <param name="range">The EU range associated with the value if required by the filter.</param>
        /// <returns>Any error condition. Good if no errors occurred.</returns>
        protected ServiceResult ValidateDataChangeFilter(
            ISystemContext context,
            NodeState source,
            uint attributeId,
            ExtensionObject requestedFilter,
            out DataChangeFilter filter,
            out Range range)
        {
            filter = null;
            range = null;

            // check for valid filter type.
            filter = requestedFilter.Body as DataChangeFilter;

            if (filter == null)
            {
                return StatusCodes.BadMonitoredItemFilterUnsupported;
            }

            // only supported for value attributes.
            if (attributeId != Attributes.Value)
            {
                return StatusCodes.BadMonitoredItemFilterUnsupported;
            }

            // only supported for variables.

            if (source is not BaseVariableState variable)
            {
                return StatusCodes.BadMonitoredItemFilterUnsupported;
            }

            // check the datatype.
            if (filter.DeadbandType != (uint)DeadbandType.None)
            {
                var builtInType = TypeInfo.GetBuiltInType(variable.DataType, Server.TypeTree);

                if (!TypeInfo.IsNumericType(builtInType))
                {
                    return StatusCodes.BadMonitoredItemFilterUnsupported;
                }
            }

            // validate filter.
            var error = filter.Validate();

            if (ServiceResult.IsBad(error))
            {
                return error;
            }

            if (filter.DeadbandType == (uint)DeadbandType.Percent)
            {
                if (variable.FindChild(context, BrowseNames.EURange) is not BaseVariableState euRange)
                {
                    return StatusCodes.BadMonitoredItemFilterUnsupported;
                }

                range = euRange.Value as Range;

                if (range == null)
                {
                    return StatusCodes.BadMonitoredItemFilterUnsupported;
                }
            }

            // all good.
            return ServiceResult.Good;
        }

        /// <summary>
        /// Restore a single monitored Item after a restart
        /// </summary>
        /// <param name="context"></param>
        /// <param name="source"></param>
        /// <param name="storedMonitoredItem"></param>
        /// <param name="monitoredItem"></param>
        /// <returns>true if sucesfully restored</returns>
        protected virtual bool RestoreMonitoredItem(
            ServerSystemContext context,
            NodeState source,
            IStoredMonitoredItem storedMonitoredItem,
            out IMonitoredItem monitoredItem)
        {
            // create monitored node.

            if (source.Handle is not MonitoredNode monitoredNode)
            {
                source.Handle = monitoredNode = new MonitoredNode(Server, this, source);
            }

            // check if the variable needs to be sampled.
            var samplingRequired = false;

            if (storedMonitoredItem.AttributeId == Attributes.Value)
            {
                var variable = source as BaseVariableState;

                if (variable.MinimumSamplingInterval > 0)
                {
                    storedMonitoredItem.SamplingInterval = CalculateSamplingInterval(variable, storedMonitoredItem.SamplingInterval);
                    samplingRequired = true;
                }
            }

            // create the item.
            var datachangeItem = monitoredNode.RestoreDataChangeItem(storedMonitoredItem);

            if (samplingRequired)
            {
                CreateSampledItem(storedMonitoredItem.SamplingInterval, datachangeItem);
            }

            // update monitored item list.
            monitoredItem = datachangeItem;

            return true;
        }

        /// <summary>
        /// Creates a new set of monitored items for a set of variables.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="source"></param>
        /// <param name="subscriptionId"></param>
        /// <param name="publishingInterval"></param>
        /// <param name="diagnosticsMasks"></param>
        /// <param name="timestampsToReturn"></param>
        /// <param name="itemToCreate"></param>
        /// <param name="createDurable"></param>
        /// <param name="globalIdCounter"></param>
        /// <param name="filterError"></param>
        /// <param name="monitoredItem"></param>
        /// <remarks>
        /// This method only handles data change subscriptions. Event subscriptions are created by the SDK.
        /// </remarks>
        protected virtual ServiceResult CreateMonitoredItem(
            ISystemContext context,
            NodeState source,
            uint subscriptionId,
            double publishingInterval,
            DiagnosticsMasks diagnosticsMasks,
            TimestampsToReturn timestampsToReturn,
            MonitoredItemCreateRequest itemToCreate,
            bool createDurable,
            ref long globalIdCounter,
            out MonitoringFilterResult filterError,
            out IMonitoredItem monitoredItem)
        {
            filterError = null;
            monitoredItem = null;
            ServiceResult error = null;

            // read initial value.
            var initialValue = new DataValue
            {
                Value = null,
                ServerTimestamp = DateTime.UtcNow,
                SourceTimestamp = DateTime.MinValue,
                StatusCode = StatusCodes.BadWaitingForInitialData
            };

            error = source.ReadAttribute(
                context,
                itemToCreate.ItemToMonitor.AttributeId,
                itemToCreate.ItemToMonitor.ParsedIndexRange,
                itemToCreate.ItemToMonitor.DataEncoding,
                initialValue);

            if (ServiceResult.IsBad(error))
            {
                if (error.StatusCode == StatusCodes.BadAttributeIdInvalid ||
                    error.StatusCode == StatusCodes.BadDataEncodingInvalid ||
                    error.StatusCode == StatusCodes.BadDataEncodingUnsupported)
                {
                    return error;
                }

                initialValue.StatusCode = error.StatusCode;
                _ = ServiceResult.Good;
            }

            // validate parameters.
            var parameters = itemToCreate.RequestedParameters;

            // validate the data change filter.
            DataChangeFilter filter = null;
            Range range = null;

            if (!ExtensionObject.IsNull(parameters.Filter))
            {
                error = ValidateDataChangeFilter(
                    context,
                    source,
                    itemToCreate.ItemToMonitor.AttributeId,
                    parameters.Filter,
                    out filter,
                    out range);

                if (ServiceResult.IsBad(error))
                {
                    return error;
                }
            }

            // create monitored node.

            if (source.Handle is not MonitoredNode monitoredNode)
            {
                source.Handle = monitoredNode = new MonitoredNode(Server, this, source);
            }

            // create a globally unique identifier.
            var monitoredItemId = Utils.IncrementIdentifier(ref globalIdCounter);

            // determine the sampling interval.
            var samplingInterval = itemToCreate.RequestedParameters.SamplingInterval;

            if (samplingInterval < 0)
            {
                samplingInterval = publishingInterval;
            }

            // check if the variable needs to be sampled.
            var samplingRequired = false;

            if (itemToCreate.ItemToMonitor.AttributeId == Attributes.Value)
            {
                var variable = source as BaseVariableState;

                if (variable.MinimumSamplingInterval > 0)
                {
                    samplingInterval = CalculateSamplingInterval(variable, samplingInterval);
                    samplingRequired = true;
                }
            }

            // create the item.
            var datachangeItem = monitoredNode.CreateDataChangeItem(
                context,
                monitoredItemId,
                itemToCreate.ItemToMonitor.AttributeId,
                itemToCreate.ItemToMonitor.ParsedIndexRange,
                itemToCreate.ItemToMonitor.DataEncoding,
                diagnosticsMasks,
                timestampsToReturn,
                itemToCreate.MonitoringMode,
                itemToCreate.RequestedParameters.ClientHandle,
                samplingInterval,
                itemToCreate.RequestedParameters.QueueSize,
                itemToCreate.RequestedParameters.DiscardOldest,
                filter,
                range,
                false);

            if (samplingRequired)
            {
                CreateSampledItem(samplingInterval, datachangeItem);
            }

            // report the initial value.
            datachangeItem.QueueValue(initialValue, null, true);

            // do any post processing.
            OnCreateMonitoredItem(context, itemToCreate, monitoredNode, datachangeItem);

            // update monitored item list.
            monitoredItem = datachangeItem;

            return ServiceResult.Good;
        }

        /// <summary>
        /// Calculates the sampling interval.
        /// </summary>
        /// <param name="variable"></param>
        /// <param name="samplingInterval"></param>
        private double CalculateSamplingInterval(BaseVariableState variable, double samplingInterval)
        {
            if (samplingInterval < variable.MinimumSamplingInterval)
            {
                samplingInterval = variable.MinimumSamplingInterval;
            }

            if ((samplingInterval % _minimumSamplingInterval) != 0)
            {
                samplingInterval = Math.Truncate(samplingInterval / _minimumSamplingInterval);
                samplingInterval++;
                samplingInterval *= _minimumSamplingInterval;
            }

            return samplingInterval;
        }

        /// <summary>
        /// Creates a new sampled item.
        /// </summary>
        /// <param name="samplingInterval"></param>
        /// <param name="monitoredItem"></param>
        private void CreateSampledItem(double samplingInterval, DataChangeMonitoredItem monitoredItem)
        {
            _sampledItems.Add(monitoredItem);

            _samplingTimer ??= new Timer(DoSample, null, (int)_minimumSamplingInterval, (int)_minimumSamplingInterval);
        }

        /// <summary>
        /// Deletes a sampled item.
        /// </summary>
        /// <param name="monitoredItem"></param>
        private void DeleteSampledItem(DataChangeMonitoredItem monitoredItem)
        {
            for (var ii = 0; ii < _sampledItems.Count; ii++)
            {
                if (ReferenceEquals(monitoredItem, _sampledItems[ii]))
                {
                    _sampledItems.RemoveAt(ii);
                    break;
                }
            }

            if (_sampledItems.Count == 0 && _samplingTimer != null)
            {
                _samplingTimer.Dispose();
                _samplingTimer = null;
            }
        }

        /// <summary>
        /// Polls each monitored item which requires sample.
        /// </summary>
        /// <param name="state"></param>
        private void DoSample(object state)
        {
            try
            {
                lock (Lock)
                {
                    for (var ii = 0; ii < _sampledItems.Count; ii++)
                    {
                        var monitoredItem = _sampledItems[ii];

                        if (monitoredItem.TimeToNextSample < _minimumSamplingInterval)
                        {
                            monitoredItem.ValueChanged(SystemContext);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Utils.LogError(e, "Unexpected error during diagnostics scan.");
            }
        }

        /// <summary>
        /// Does any processing after a monitored item is created.
        /// </summary>
        /// <param name="systemContext"></param>
        /// <param name="itemToCreate"></param>
        /// <param name="monitoredNode"></param>
        /// <param name="monitoredItem"></param>
        protected virtual void OnCreateMonitoredItem(
            ISystemContext systemContext,
            MonitoredItemCreateRequest itemToCreate,
            MonitoredNode monitoredNode,
            DataChangeMonitoredItem monitoredItem)
        {
            // does nothing.
        }

        /// <summary>
        /// Modifies the parameters for a set of monitored items.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="timestampsToReturn"></param>
        /// <param name="monitoredItems"></param>
        /// <param name="itemsToModify"></param>
        /// <param name="errors"></param>
        /// <param name="filterErrors"></param>
        public virtual void ModifyMonitoredItems(
            OperationContext context,
            TimestampsToReturn timestampsToReturn,
            IList<IMonitoredItem> monitoredItems,
            IList<MonitoredItemModifyRequest> itemsToModify,
            IList<ServiceResult> errors,
            IList<MonitoringFilterResult> filterErrors)
        {
            var systemContext = SystemContext.Copy(context);

            lock (Lock)
            {
                for (var ii = 0; ii < monitoredItems.Count; ii++)
                {
                    var itemToModify = itemsToModify[ii];

                    // skip items that have already been processed.
                    if (itemToModify.Processed)
                    {
                        continue;
                    }

                    // modify the monitored item.
                    MonitoringFilterResult filterError = null;

                    errors[ii] = ModifyMonitoredItem(
                        systemContext,
                        context.DiagnosticsMask,
                        timestampsToReturn,
                        monitoredItems[ii],
                        itemToModify,
                        out filterError);

                    // save any filter error details.
                    filterErrors[ii] = filterError;
                }
            }
        }

        /// <summary>
        /// Modifies the parameters for a monitored item.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="diagnosticsMasks"></param>
        /// <param name="timestampsToReturn"></param>
        /// <param name="monitoredItem"></param>
        /// <param name="itemToModify"></param>
        /// <param name="filterError"></param>
        protected virtual ServiceResult ModifyMonitoredItem(
            ISystemContext context,
            DiagnosticsMasks diagnosticsMasks,
            TimestampsToReturn timestampsToReturn,
            IMonitoredItem monitoredItem,
            MonitoredItemModifyRequest itemToModify,
            out MonitoringFilterResult filterError)
        {
            filterError = null;
            ServiceResult error = null;

            // check for valid handle.

            if (monitoredItem.ManagerHandle is not MonitoredNode monitoredNode)
            {
                return ServiceResult.Good;
            }

            if (IsHandleInNamespace(monitoredNode.Node) == null)
            {
                return ServiceResult.Good;
            }

            // owned by this node manager.
            itemToModify.Processed = true;

            // check for valid monitored item.
            var datachangeItem = monitoredItem as DataChangeMonitoredItem;

            // validate parameters.
            var parameters = itemToModify.RequestedParameters;

            // validate the data change filter.
            DataChangeFilter filter = null;
            Range range = null;

            if (!ExtensionObject.IsNull(parameters.Filter))
            {
                error = ValidateDataChangeFilter(
                    context,
                    monitoredNode.Node,
                    datachangeItem.AttributeId,
                    parameters.Filter,
                    out filter,
                    out range);

                if (ServiceResult.IsBad(error))
                {
                    return error;
                }
            }

            var previousSamplingInterval = datachangeItem.SamplingInterval;

            // check if the variable needs to be sampled.
            var samplingInterval = itemToModify.RequestedParameters.SamplingInterval;

            if (datachangeItem.AttributeId == Attributes.Value)
            {
                var variable = monitoredNode.Node as BaseVariableState;

                if (variable.MinimumSamplingInterval > 0)
                {
                    samplingInterval = CalculateSamplingInterval(variable, samplingInterval);
                }
            }

            // modify the monitored item parameters.
            _ = datachangeItem.Modify(
                diagnosticsMasks,
                timestampsToReturn,
                itemToModify.RequestedParameters.ClientHandle,
                samplingInterval,
                itemToModify.RequestedParameters.QueueSize,
                itemToModify.RequestedParameters.DiscardOldest,
                filter,
                range);

            // do any post processing.
            OnModifyMonitoredItem(
                context,
                itemToModify,
                monitoredNode,
                datachangeItem,
                previousSamplingInterval);

            return ServiceResult.Good;
        }

        /// <summary>
        /// Does any processing after a monitored item is created.
        /// </summary>
        /// <param name="systemContext"></param>
        /// <param name="itemToModify"></param>
        /// <param name="monitoredNode"></param>
        /// <param name="monitoredItem"></param>
        /// <param name="previousSamplingInterval"></param>
        protected virtual void OnModifyMonitoredItem(
            ISystemContext systemContext,
            MonitoredItemModifyRequest itemToModify,
            MonitoredNode monitoredNode,
            DataChangeMonitoredItem monitoredItem,
            double previousSamplingInterval)
        {
            // does nothing.
        }

        /// <summary>
        /// Deletes a set of monitored items.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="monitoredItems"></param>
        /// <param name="processedItems"></param>
        /// <param name="errors"></param>
        public virtual void DeleteMonitoredItems(
            OperationContext context,
            IList<IMonitoredItem> monitoredItems,
            IList<bool> processedItems,
            IList<ServiceResult> errors)
        {
            var systemContext = SystemContext.Copy(context);

            lock (Lock)
            {
                for (var ii = 0; ii < monitoredItems.Count; ii++)
                {
                    // skip items that have already been processed.
                    if (processedItems[ii])
                    {
                        continue;
                    }

                    // delete the monitored item.
                    var processed = false;

                    errors[ii] = DeleteMonitoredItem(
                        systemContext,
                        monitoredItems[ii],
                        out processed);

                    // indicate whether it was processed or not.
                    processedItems[ii] = processed;
                }
            }
        }

        /// <summary>
        /// Deletes a monitored item.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="monitoredItem"></param>
        /// <param name="processed"></param>
        protected virtual ServiceResult DeleteMonitoredItem(
            ISystemContext context,
            IMonitoredItem monitoredItem,
            out bool processed)
        {
            processed = false;

            // check for valid handle.

            if (monitoredItem.ManagerHandle is not MonitoredNode monitoredNode)
            {
                return ServiceResult.Good;
            }

            if (IsHandleInNamespace(monitoredNode.Node) == null)
            {
                return ServiceResult.Good;
            }

            // owned by this node manager.
            processed = true;

            // get the  source.
            _ = monitoredNode.Node;

            // check for valid monitored item.
            var datachangeItem = monitoredItem as DataChangeMonitoredItem;

            // check if the variable needs to be sampled.
            if (datachangeItem.AttributeId == Attributes.Value)
            {
                var variable = monitoredNode.Node as BaseVariableState;

                if (variable.MinimumSamplingInterval > 0)
                {
                    DeleteSampledItem(datachangeItem);
                }
            }

            // remove item.
            monitoredNode.DeleteItem(datachangeItem);

            // do any post processing.
            OnDeleteMonitoredItem(context, monitoredNode, datachangeItem);

            return ServiceResult.Good;
        }

        /// <summary>
        /// Does any processing after a monitored item is deleted.
        /// </summary>
        /// <param name="systemContext"></param>
        /// <param name="monitoredNode"></param>
        /// <param name="monitoredItem"></param>
        protected virtual void OnDeleteMonitoredItem(
            ISystemContext systemContext,
            MonitoredNode monitoredNode,
            DataChangeMonitoredItem monitoredItem)
        {
            // does nothing.
        }

        /// <summary>
        /// Transfers a set of monitored items.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="sendInitialValues">Whether the subscription should send initial values after transfer.</param>
        /// <param name="monitoredItems">The set of monitoring items to update.</param>
        /// <param name="processedItems">The list of bool with items that were already processed.</param>
        /// <param name="errors">Any errors.</param>
        public virtual void TransferMonitoredItems(
            OperationContext context,
            bool sendInitialValues,
            IList<IMonitoredItem> monitoredItems,
            IList<bool> processedItems,
            IList<ServiceResult> errors)
        {
            var systemContext = SystemContext.Copy(context);
            var transferredItems = new List<IMonitoredItem>();
            lock (Lock)
            {
                for (var ii = 0; ii < monitoredItems.Count; ii++)
                {
                    // skip items that have already been processed.
                    if (processedItems[ii] || monitoredItems[ii] == null)
                    {
                        continue;
                    }

                    // check handle.
                    // check for valid handle.

                    if (monitoredItems[ii].ManagerHandle is not MonitoredNode monitoredNode)
                    {
                        continue;
                    }

                    // owned by this node manager.
                    processedItems[ii] = true;
                    transferredItems.Add(monitoredItems[ii]);

                    if (sendInitialValues)
                    {
                        monitoredItems[ii].SetupResendDataTrigger();
                    }
                    errors[ii] = StatusCodes.Good;
                }
            }

            // do any post processing.
            OnMonitoredItemsTransferred(systemContext, transferredItems);
        }

        /// <summary>
        /// Called after transfer of MonitoredItems.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="monitoredItems">The transferred monitored items.</param>
        protected virtual void OnMonitoredItemsTransferred(
            ServerSystemContext context,
            IList<IMonitoredItem> monitoredItems
            )
        {
            // overridden by the sub-class.
        }

        /// <summary>
        /// Changes the monitoring mode for a set of monitored items.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="monitoringMode"></param>
        /// <param name="monitoredItems"></param>
        /// <param name="processedItems"></param>
        /// <param name="errors"></param>
        public virtual void SetMonitoringMode(
            OperationContext context,
            MonitoringMode monitoringMode,
            IList<IMonitoredItem> monitoredItems,
            IList<bool> processedItems,
            IList<ServiceResult> errors)
        {
            var systemContext = SystemContext.Copy(context);

            lock (Lock)
            {
                for (var ii = 0; ii < monitoredItems.Count; ii++)
                {
                    // skip items that have already been processed.
                    if (processedItems[ii])
                    {
                        continue;
                    }

                    // update monitoring mode.
                    var processed = false;

                    errors[ii] = SetMonitoringMode(
                        systemContext,
                        monitoredItems[ii],
                        monitoringMode,
                        out processed);

                    // indicate whether it was processed or not.
                    processedItems[ii] = processed;
                }
            }
        }

        /// <summary>
        /// Changes the monitoring mode for an item.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="monitoredItem"></param>
        /// <param name="monitoringMode"></param>
        /// <param name="processed"></param>
        protected virtual ServiceResult SetMonitoringMode(
            ISystemContext context,
            IMonitoredItem monitoredItem,
            MonitoringMode monitoringMode,
            out bool processed)
        {
            processed = false;

            // check for valid handle.

            if (monitoredItem.ManagerHandle is not MonitoredNode monitoredNode)
            {
                return ServiceResult.Good;
            }

            if (IsHandleInNamespace(monitoredNode.Node) == null)
            {
                return ServiceResult.Good;
            }

            // owned by this node manager.
            processed = true;

            // check for valid monitored item.
            var datachangeItem = monitoredItem as DataChangeMonitoredItem;

            // update monitoring mode.
            var previousMode = datachangeItem.SetMonitoringMode(monitoringMode);

            // need to provide an immediate update after enabling.
            if (previousMode == MonitoringMode.Disabled && monitoringMode != MonitoringMode.Disabled)
            {
                ReadInitialValue(context, monitoredNode, datachangeItem, false);
            }

            // do any post processing.
            OnSetMonitoringMode(context, monitoredNode, datachangeItem, previousMode, monitoringMode);

            return ServiceResult.Good;
        }

        /// <summary>
        /// Does any processing after a monitored item is created.
        /// </summary>
        /// <param name="systemContext"></param>
        /// <param name="monitoredNode"></param>
        /// <param name="monitoredItem"></param>
        /// <param name="previousMode"></param>
        /// <param name="currentMode"></param>
        protected virtual void OnSetMonitoringMode(
            ISystemContext systemContext,
            MonitoredNode monitoredNode,
            DataChangeMonitoredItem monitoredItem,
            MonitoringMode previousMode,
            MonitoringMode currentMode)
        {
            // does nothing.
        }

        private List<string> _namespaceUris;
        private ushort[] _namespaceIndexes;

        private Timer _samplingTimer;
        private readonly List<DataChangeMonitoredItem> _sampledItems;
        private readonly double _minimumSamplingInterval;
    }
}
