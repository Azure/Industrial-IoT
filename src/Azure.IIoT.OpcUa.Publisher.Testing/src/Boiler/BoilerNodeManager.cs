/* ========================================================================
 * Copyright (c) 2005-2016 The OPC Foundation, Inc. All rights reserved.
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

namespace Boiler
{
    using Opc.Ua;
    using Opc.Ua.Sample;
    using System.Collections.Generic;
    using System.Reflection;

    /// <summary>
    /// A node manager the diagnostic information exposed by the server.
    /// </summary>
    public class BoilerNodeManager : SampleNodeManager
    {
        /// <summary>
        /// Initializes the node manager.
        /// </summary>
        /// <param name="server"></param>
        /// <param name="configuration"></param>
        public BoilerNodeManager(
            Opc.Ua.Server.IServerInternal server,
            ApplicationConfiguration configuration)
        :
            base(server)
        {
            System.Diagnostics.Contracts.Contract.Assume(configuration != null);
            var namespaceUris = new List<string> {
                Namespaces.Boiler,
                Namespaces.Boiler + "/Instance"
            };
            NamespaceUris = namespaceUris;

            _namespaceIndex = Server.NamespaceUris.GetIndexOrAppend(namespaceUris[1]);

            //  AddEncodeableNodeManagerTypes(typeof(BoilerNodeManager).Assembly, typeof(BoilerNodeManager).Namespace);

            _lastUsedId = 0;
            _boilers = [];
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _boilers != null)
            {
                foreach (var boiler in _boilers)
                {
                    boiler.Dispose();
                }
                _boilers = null;
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// Creates the NodeId for the specified node.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="node">The node.</param>
        /// <returns>The new NodeId.</returns>
        public override NodeId New(ISystemContext context, NodeState node)
        {
            var id = Utils.IncrementIdentifier(ref _lastUsedId);
            return new NodeId(id, _namespaceIndex);
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
                CreateBoiler(SystemContext, 2);
            }
        }

        /// <summary>
        /// Creates a boiler and adds it to the address space.
        /// </summary>
        /// <param name="context">The context to use.</param>
        /// <param name="unitNumber">The unit number for the boiler.</param>
        private void CreateBoiler(SystemContext context, int unitNumber)
        {
            var boiler = new BoilerState(null);

            var name = Utils.Format("Boiler #{0}", unitNumber);

            boiler.Create(
                context,
                null,
                new QualifiedName(name, _namespaceIndex),
                null,
                true);

            var folder = FindPredefinedNode(
                ExpandedNodeId.ToNodeId(ObjectIds.Boilers, Server.NamespaceUris),
                typeof(NodeState));

            folder.AddReference(Opc.Ua.ReferenceTypeIds.Organizes, false, boiler.NodeId);
            boiler.AddReference(Opc.Ua.ReferenceTypeIds.Organizes, true, folder.NodeId);

            var unitLabel = Utils.Format("{0}0", unitNumber);

            UpdateDisplayName(boiler.InputPipe, unitLabel);
            UpdateDisplayName(boiler.Drum, unitLabel);
            UpdateDisplayName(boiler.OutputPipe, unitLabel);
            UpdateDisplayName(boiler.LevelController, unitLabel);
            UpdateDisplayName(boiler.FlowController, unitLabel);
            UpdateDisplayName(boiler.CustomController, unitLabel);

            _boilers.Add(boiler);

            AddPredefinedNode(context, boiler);

            // Autostart boiler simulation state machine
            var start = boiler.Simulation.Start;
            IList<Variant> inputArguments = [];
            IList<Variant> outputArguments = [];
            var errors = new List<ServiceResult>();
            start.Call(context, boiler.NodeId, inputArguments, errors, outputArguments);
        }

        /// <summary>
        /// Updates the display name for an instance with the unit label name.
        /// </summary>
        /// <param name="instance">The instance to update.</param>
        /// <param name="unitLabel">The label to apply.</param>
        /// <remarks>This method assumes the DisplayName has the form NameX001 where X0 is the unit label placeholder.</remarks>
        private void UpdateDisplayName(BaseInstanceState instance, string unitLabel)
        {
            var displayName = instance.DisplayName;

            if (displayName != null)
            {
                var text = displayName.Text;

                if (text != null)
                {
                    text = text.Replace("X0", unitLabel);
                }

                displayName = new LocalizedText(displayName.Locale, text);
            }

            instance.DisplayName = displayName;
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
                $"{type.Assembly.GetName().Name}.Generated.{type.Namespace}.Design.{type.Namespace}.PredefinedNodes.uanodes",
                type.Assembly, true);
            return predefinedNodes;
        }

        /// <summary>
        /// Replaces the generic node with a node specific to the model.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="predefinedNode"></param>
        protected override NodeState AddBehaviourToPredefinedNode(ISystemContext context, NodeState predefinedNode)
        {
            if (predefinedNode is not BaseObjectState passiveNode)
            {
                return predefinedNode;
            }

            var typeId = passiveNode.TypeDefinitionId;

            if (!IsNodeIdInNamespace(typeId) || typeId.IdType != IdType.Numeric)
            {
                return predefinedNode;
            }

            switch ((uint)typeId.Identifier)
            {
                case ObjectTypes.BoilerType:
                    {
                        if (passiveNode is BoilerState)
                        {
                            break;
                        }

                        var activeNode = new BoilerState(passiveNode.Parent);
                        activeNode.Create(context, passiveNode);

                        // replace the node in the parent.
                        passiveNode.Parent?.ReplaceChild(context, activeNode);

                        // Autostart boiler simulation state machine
                        var start = activeNode.Simulation.Start;
                        IList<Variant> inputArguments = [];
                        IList<Variant> outputArguments = [];
                        var errors = new List<ServiceResult>();
                        start.Call(context, activeNode.NodeId, inputArguments, errors, outputArguments);

                        _boilers.Add(activeNode);
                        return activeNode;
                    }
            }

            return predefinedNode;
        }

        /// <summary>
        /// Does any processing after a monitored item is created.
        /// </summary>
        /// <param name="systemContext"></param>
        /// <param name="itemToCreate"></param>
        /// <param name="monitoredNode"></param>
        /// <param name="monitoredItem"></param>
        protected override void OnCreateMonitoredItem(
            ISystemContext systemContext,
            MonitoredItemCreateRequest itemToCreate,
            MonitoredNode monitoredNode,
            DataChangeMonitoredItem monitoredItem)
        {
            // TBD
        }

        /// <summary>
        /// Does any processing after a monitored item is created.
        /// </summary>
        /// <param name="systemContext"></param>
        /// <param name="itemToModify"></param>
        /// <param name="monitoredNode"></param>
        /// <param name="monitoredItem"></param>
        /// <param name="previousSamplingInterval"></param>
        protected override void OnModifyMonitoredItem(
            ISystemContext systemContext,
            MonitoredItemModifyRequest itemToModify,
            MonitoredNode monitoredNode,
            DataChangeMonitoredItem monitoredItem,
            double previousSamplingInterval)
        {
            // TBD
        }

        /// <summary>
        /// Does any processing after a monitored item is deleted.
        /// </summary>
        /// <param name="systemContext"></param>
        /// <param name="monitoredNode"></param>
        /// <param name="monitoredItem"></param>
        protected override void OnDeleteMonitoredItem(
            ISystemContext systemContext,
            MonitoredNode monitoredNode,
            DataChangeMonitoredItem monitoredItem)
        {
            // TBD
        }

        /// <summary>
        /// Does any processing after a monitored item is created.
        /// </summary>
        /// <param name="systemContext"></param>
        /// <param name="monitoredNode"></param>
        /// <param name="monitoredItem"></param>
        /// <param name="previousMode"></param>
        /// <param name="currentMode"></param>
        protected override void OnSetMonitoringMode(
            ISystemContext systemContext,
            MonitoredNode monitoredNode,
            DataChangeMonitoredItem monitoredItem,
            MonitoringMode previousMode,
            MonitoringMode currentMode)
        {
            // TBD
        }

        private readonly ushort _namespaceIndex;
        private long _lastUsedId;
        private List<BoilerState> _boilers;
    }
}
