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

namespace PerfTest
{
    using Opc.Ua;
    using Opc.Ua.Server;
    using System.Collections.Generic;

    /// <summary>
    /// A node manager for a server that exposes several variables.
    /// </summary>
    public class PerfTestNodeManager : CustomNodeManager2
    {
        /// <summary>
        /// Initializes the node manager.
        /// </summary>
        /// <param name="server"></param>
        /// <param name="configuration"></param>
        public PerfTestNodeManager(IServerInternal server, ApplicationConfiguration configuration)
        : base(server, configuration, Namespaces.PerfTest)
        {
            SystemContext.NodeIdFactory = this;
            SystemContext.SystemHandle = _system = new UnderlyingSystem();

            // get the configuration for the node manager.
            // use suitable defaults if no configuration exists.
            _configuration = configuration.ParseExtension<PerfTestServerConfiguration>() ??
                new PerfTestServerConfiguration();
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
                _system.Initialize();

                var registers = _system.Registers;

                for (var ii = 0; ii < registers.Count; ii++)
                {
                    var targetId = ModelUtils.GetRegisterId(registers[ii], NamespaceIndex);

                    if (!externalReferences.TryGetValue(ObjectIds.ObjectsFolder, out var references))
                    {
                        externalReferences[ObjectIds.ObjectsFolder] = references = [];
                    }

                    references.Add(new NodeStateReference(ReferenceTypeIds.Organizes, false, targetId));
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

                var handle = new NodeHandle
                {
                    NodeId = nodeId,
                    Validated = true
                };

                var id = (uint)nodeId.Identifier;

                // find register
                var registerId = (int)((id & 0xFF000000) >> 24);
                var index = (int)(id & 0x00FFFFFF);

                if (registerId == 0)
                {
                    var register = _system.GetRegister(index);

                    if (register == null)
                    {
                        return null;
                    }

                    handle.Node = ModelUtils.GetRegister(register, NamespaceIndex);
                }

                // find register variable.
                else
                {
                    var register = _system.GetRegister(registerId);

                    if (register == null)
                    {
                        return null;
                    }

                    // find register variable.
                    var variable = ModelUtils.GetRegisterVariable(register, index, NamespaceIndex);

                    if (variable == null)
                    {
                        return null;
                    }

                    handle.Node = variable;
                }

                return handle;
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

            // TBD

            return null;
        }

        protected override void OnCreateMonitoredItemsComplete(ServerSystemContext context, IList<IMonitoredItem> monitoredItems)
        {
            for (var ii = 0; ii < monitoredItems.Count; ii++)
            {
                var handle = IsHandleInNamespace(monitoredItems[ii].ManagerHandle);

                if (handle == null)
                {
                    continue;
                }

                var variable = handle.Node as BaseVariableState;

                if (handle.Node.Handle is MemoryRegister register)
                {
                    register.Subscribe((int)variable.NumericId, (IDataChangeMonitoredItem2)monitoredItems[ii]);
                }
            }
        }

        protected override void OnDeleteMonitoredItemsComplete(ServerSystemContext context, IList<IMonitoredItem> monitoredItems)
        {
            for (var ii = 0; ii < monitoredItems.Count; ii++)
            {
                var handle = IsHandleInNamespace(monitoredItems[ii].ManagerHandle);

                if (handle == null)
                {
                    continue;
                }

                var variable = handle.Node as BaseVariableState;

                if (handle.Node.Handle is MemoryRegister register)
                {
                    register.Unsubscribe((int)variable.NumericId, (IDataChangeMonitoredItem2)monitoredItems[ii]);
                }
            }
        }

#pragma warning disable IDE0052 // Remove unread private members
        private readonly PerfTestServerConfiguration _configuration;
#pragma warning restore IDE0052 // Remove unread private members
        private readonly UnderlyingSystem _system;
    }
}
