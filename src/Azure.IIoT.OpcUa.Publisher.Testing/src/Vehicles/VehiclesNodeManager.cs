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

namespace Vehicles
{
    using Opc.Ua;
    using Opc.Ua.Server;
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    /// <summary>
    /// A node manager for a server that exposes several variables.
    /// </summary>
    public class VehiclesNodeManager : CustomNodeManager2
    {
        /// <summary>
        /// Initializes the node manager.
        /// </summary>
        /// <param name="server"></param>
        /// <param name="configuration"></param>
        public VehiclesNodeManager(IServerInternal server, ApplicationConfiguration configuration)
        :
            base(server, configuration)
        {
            SystemContext.NodeIdFactory = this;

            SetNamespaces(
                Namespaces.Vehicles,
                Types.Namespaces.Vehicles,
                Instances.Namespaces.VehiclesInstances);

            // get the configuration for the node manager.
            // use suitable defaults if no configuration exists.
            _configuration = configuration.ParseExtension<VehiclesServerConfiguration>()
                ?? new VehiclesServerConfiguration();
        }

        /// <summary>
        /// An overrideable version of the Dispose.
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // TBD
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// Creates the NodeId for the specified node.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="node"></param>
        public override NodeId New(ISystemContext context, NodeState node)
        {
            // generate a numeric node id if the node has a parent and no node id assigned.

            if (node is BaseInstanceState instance && instance.Parent != null)
            {
                return GenerateNodeId();
            }

            return node.NodeId;
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
                base.CreateAddressSpace(externalReferences);

                var dictionary = (BaseDataVariableState)FindPredefinedNode(
                    ExpandedNodeId.ToNodeId(Types.VariableIds.Vehicles_BinarySchema, Server.NamespaceUris),
                    typeof(BaseDataVariableState));

                var type = GetType().GetTypeInfo();
                dictionary.Value = LoadSchemaFromResource(
                    $"{type.Assembly.GetName().Name}.Generated.{type.Namespace}.Design.{type.Namespace}.Types.Types.bsd",
                    typeof(Types.VehicleType).Assembly);

                dictionary = (BaseDataVariableState)FindPredefinedNode(
                    ExpandedNodeId.ToNodeId(Types.VariableIds.Vehicles_XmlSchema, Server.NamespaceUris),
                    typeof(BaseDataVariableState));

                dictionary.Value = LoadSchemaFromResource(
                    $"{type.Assembly.GetName().Name}.Generated.{type.Namespace}.Design.{type.Namespace}.Types.Types.xsd",
                    typeof(Types.VehicleType).Assembly);
            }
        }

        /// <summary>
        /// Loads the schema from an embedded resource.
        /// </summary>
        /// <param name="resourcePath"></param>
        /// <param name="assembly"></param>
        /// <exception cref="ArgumentNullException"><paramref name="resourcePath"/> is <c>null</c>.</exception>
        /// <exception cref="ServiceResultException"></exception>
        public byte[] LoadSchemaFromResource(string resourcePath, Assembly assembly)
        {
            ArgumentNullException.ThrowIfNull(resourcePath);
            if (assembly == null)
            {
                assembly = Assembly.GetCallingAssembly();
            }

            var istrm = assembly.GetManifestResourceStream(resourcePath);
            if (istrm == null)
            {
                throw ServiceResultException.Create(StatusCodes.BadDecodingError, "Could not load nodes from resource: {0}", resourcePath);
            }

            var buffer = new byte[istrm.Length];
            istrm.ReadExactly(buffer, 0, (int)istrm.Length);
            return buffer;
        }

        /// <summary>
        /// Loads a node set from a file or resource and addes them to the set of predefined nodes.
        /// </summary>
        /// <param name="context"></param>
        protected override NodeStateCollection LoadPredefinedNodes(ISystemContext context)
        {
            var type = GetType().GetTypeInfo();
            var predefinedNodes = new NodeStateCollection();
            predefinedNodes.LoadFromBinaryResource(context,
                $"{type.Assembly.GetName().Name}.Generated.{type.Namespace}.Design.{type.Namespace}.Types.PredefinedNodes.uanodes",
                type.Assembly, true);
            predefinedNodes.LoadFromBinaryResource(context,
                $"{type.Assembly.GetName().Name}.Generated.{type.Namespace}.Design.{type.Namespace}.Instances.PredefinedNodes.uanodes",
                type.Assembly, true);
            return predefinedNodes;
        }

        /// <summary>
        /// Frees any resources allocated for the address space.
        /// </summary>
        public override void DeleteAddressSpace()
        {
            lock (Lock)
            {
                // TBD
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

                // check cache (the cache is used because the same node id can appear many times in a single request).
                if (cache != null && cache.TryGetValue(nodeId, out var node))
                {
                    return new NodeHandle(nodeId, node);
                }

                // look up predefined node.
                if (PredefinedNodes.TryGetValue(nodeId, out node))
                {
                    var handle = new NodeHandle(nodeId, node);

                    cache?.Add(nodeId, node);

                    return handle;
                }

                // node not found.
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

            // lookup in operation cache.
            var target = FindNodeInCache(context, handle, cache);

            if (target != null)
            {
                handle.Node = target;
                handle.Validated = true;
                return handle.Node;
            }

            // put root into operation cache.
            if (cache != null)
            {
                cache[handle.NodeId] = target;
            }

            handle.Node = target;
            handle.Validated = true;
            return handle.Node;
        }

        /// <summary>
        /// Generates a new node id.
        /// </summary>
        private NodeId GenerateNodeId()
        {
            return new NodeId(++_nextNodeId, NamespaceIndex);
        }

        private uint _nextNodeId;
#pragma warning disable IDE0052 // Remove unread private members
        private readonly VehiclesServerConfiguration _configuration;
#pragma warning restore IDE0052 // Remove unread private members
    }
}
