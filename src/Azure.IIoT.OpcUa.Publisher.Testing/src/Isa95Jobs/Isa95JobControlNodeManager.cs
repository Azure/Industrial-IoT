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

namespace Isa95Jobs
{
    using Opc.Ua;
    using Opc.Ua.Server;
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Threading;
    using UAModel.ISA95_JOBCONTROL_V2;

    /// <summary>
    /// A node manager for a server that exposes several variables.
    /// </summary>
    public class Isa95JobControlNodeManager : CustomNodeManager2
    {
        /// <summary>
        /// Initializes the node manager.
        /// </summary>
        /// <param name="server"></param>
        /// <param name="configuration"></param>
        public Isa95JobControlNodeManager(IServerInternal server, ApplicationConfiguration configuration) :
            base(server, configuration)
        {
            SystemContext.NodeIdFactory = this;

            // set one namespace for the type model and one names for dynamically created nodes.
            var namespaceUrls = new string[1];
            namespaceUrls[0] = UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2;
            SetNamespaces(namespaceUrls);

            // get the configuration for the node manager.
            // use suitable defaults if no configuration exists.
            _configuration = configuration.ParseExtension<Isa95JobControlServerConfiguration>()
                ?? new Isa95JobControlServerConfiguration();
        }

        /// <summary>
        /// An overrideable version of the Dispose.
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && _simulationTimer != null)
            {
                Utils.SilentDispose(_simulationTimer);
                _simulationTimer = null;
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
        /// Loads a node set from a file or resource and addes them to the set of predefined nodes.
        /// </summary>
        /// <param name="context"></param>
        protected override NodeStateCollection LoadPredefinedNodes(ISystemContext context)
        {
            var type = GetType().GetTypeInfo();
            var predefinedNodes = new NodeStateCollection();
            predefinedNodes.LoadFromBinaryResource(context,
                $"{type.Assembly.GetName().Name}.Generated.{type.Namespace}.Design.UAModel.ISA95_JOBCONTROL_V2.PredefinedNodes.uanodes",
                type.Assembly, true);
            return predefinedNodes;
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
                LoadPredefinedNodes(SystemContext, externalReferences);

                // start a simulation that changes the values of the nodes.
                _simulationTimer = new Timer(DoSimulation, null, kEventInterval, kEventInterval);
            }
        }

        /// <summary>
        /// Frees any resources allocated for the address space.
        /// </summary>
        public override void DeleteAddressSpace()
        {
            lock (Lock)
            {
                base.DeleteAddressSpace();
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

                // check for predefined nodes.
                if (PredefinedNodes != null && PredefinedNodes.TryGetValue(nodeId, out var node))
                {
                    return new NodeHandle
                    {
                        NodeId = nodeId,
                        Validated = true,
                        Node = node
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

            // TBD

            return null;
        }

        /// <summary>
        /// Does the simulation.
        /// </summary>
        /// <param name="state">The state.</param>
        private void DoSimulation(object state)
        {
            try
            {
                // construct translation object with default text.
                var info = new TranslationInfo(
                    "ISA95JobResponse",
                    "en-US",
                    "The job '{0}' has completed.",
                    ++_jobId);

                // construct the event.
                var e = new ISA95JobOrderStatusEventTypeState(null);

                e.Initialize(
                    SystemContext,
                    null,
                    (EventSeverity)0,
                    new LocalizedText(info));

                e.SetChildValue(SystemContext, Opc.Ua.BrowseNames.SourceName, "GB05_ServerTEST", false);
                e.SetChildValue(SystemContext, Opc.Ua.BrowseNames.SourceNode, Opc.Ua.ObjectIds.Server, false);

                var startTime = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(3));
                var endTime = DateTime.UtcNow;
                var response = new ISA95JobResponseDataType
                {
                    EncodingMask = ISA95JobResponseDataTypeFields.None | ISA95JobResponseDataTypeFields.StartTime | ISA95JobResponseDataTypeFields.EndTime | ISA95JobResponseDataTypeFields.EquipmentActuals | ISA95JobResponseDataTypeFields.MaterialActuals,
                    JobOrderID = _jobId.ToString(),
                    JobResponseID = Guid.NewGuid().ToString(),
                    StartTime = startTime,
                    EndTime = endTime,
                    EquipmentActuals =
                        [
                            new ISA95EquipmentDataType
                            {
                                EncodingMask = ISA95EquipmentDataTypeFields.EquipmentUse | ISA95EquipmentDataTypeFields.EngineeringUnits| ISA95EquipmentDataTypeFields.Quantity,
                                EngineeringUnits = new EUInformation("rpm", "RPM"),
                                EquipmentUse = "consumable",
                                Quantity = "500"
                            },
                            new ISA95EquipmentDataType
                            {
                                EncodingMask = ISA95EquipmentDataTypeFields.EquipmentUse | ISA95EquipmentDataTypeFields.EngineeringUnits| ISA95EquipmentDataTypeFields.Quantity,
                                EngineeringUnits = new EUInformation("C", "Celsius"),
                                EquipmentUse = "consumable",
                                Quantity = "3"
                            }
                        ],
                    MaterialActuals =
                        [
                            new ISA95MaterialDataType
                            {
                                EncodingMask = ISA95MaterialDataTypeFields.MaterialClassID | ISA95MaterialDataTypeFields.MaterialUse | ISA95MaterialDataTypeFields.Quantity,
                                MaterialClassID = Guid.NewGuid().ToString(),
                                MaterialUse = "consumable",
                                Quantity = "1"
                            },
                            new ISA95MaterialDataType
                            {
                                EncodingMask = ISA95MaterialDataTypeFields.MaterialClassID | ISA95MaterialDataTypeFields.MaterialUse | ISA95MaterialDataTypeFields.Quantity,
                                MaterialClassID = Guid.NewGuid().ToString(),
                                MaterialUse = "consumable",
                                Quantity = "2"
                            }
                        ]
                };
                e.SetChildValue(SystemContext, new QualifiedName(UAModel.ISA95_JOBCONTROL_V2.BrowseNames.JobResponse, NamespaceIndex), response, false);

                var jobOrderState = new ISA95JobOrderAndStateDataType
                {
                    JobOrder = new ISA95JobOrderDataType
                    {
                        EncodingMask = ISA95JobOrderDataTypeFields.None,
                        JobOrderID = _jobId.ToString(),
                        EquipmentRequirements =
                            [
                                new ISA95EquipmentDataType
                                {
                                    EncodingMask = ISA95EquipmentDataTypeFields.EquipmentUse | ISA95EquipmentDataTypeFields.EngineeringUnits | ISA95EquipmentDataTypeFields.Quantity,
                                    EngineeringUnits = new EUInformation("rpm", "RPM"),
                                    EquipmentUse = "free",
                                    Quantity = "1000"
                                }
                            ]
                    },
                    State =
                    [
                        new ISA95StateDataType
                        {
                            StateNumber = ++_state,
                            BrowsePath = new RelativePath(new QualifiedName("State " + _state, NamespaceIndex)),
                            StateText = new LocalizedText("en-US", "State " + _state),
                        },
                        new ISA95StateDataType
                        {
                            StateNumber = ++_state,
                            BrowsePath = new RelativePath(new QualifiedName("State " + _state, NamespaceIndex)),
                            StateText = new LocalizedText("en-US", "State " + _state),
                        },
                        new ISA95StateDataType
                        {
                            StateNumber = ++_state,
                            BrowsePath = new RelativePath(new QualifiedName("State " + _state, NamespaceIndex)),
                            StateText = new LocalizedText("en-US", "State " + _state),
                        }
                    ]
                };
                e.SetChildValue(SystemContext, new QualifiedName(UAModel.ISA95_JOBCONTROL_V2.BrowseNames.JobState, NamespaceIndex), jobOrderState, false);
                // e.SetChildValue(SystemContext, new QualifiedName(UAModel.ISA95_JOBCONTROL_V2.BrowseNames.JobOrder, NamespaceIndex), state, false);

                Server.ReportEvent(e);
            }
            catch (NullReferenceException)
            {
                // Stop simulation because the subscription is closed. This should be fixed in the server library.
                _simulationTimer.Change(Timeout.Infinite, Timeout.Infinite);
            }
            catch (Exception e)
            {
                Utils.Trace(e, "Unexpected error during simulation.");
            }
        }

#pragma warning disable IDE0052 // Remove unread private members
        private readonly Isa95JobControlServerConfiguration _configuration;
#pragma warning restore IDE0052 // Remove unread private members
        private Timer _simulationTimer;
        private int _jobId;
        private uint _state;
        private const int kEventInterval = 1000;
    }
}
