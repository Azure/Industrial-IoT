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

namespace Alarms
{
    using Opc.Ua;
    using Opc.Ua.Server;
    using Opc.Ua.Test;
    using System;
    using System.Collections.Generic;
    using System.Threading;

    /// <summary>
    /// A node manager for a simple server that exposes several Areas, Sources and Conditions.
    /// </summary>
    /// <remarks>
    /// This node manager presumes that the information model consists of a hierachy of predefined
    /// Areas with a number of Sources contained within them. Each individual Source is
    /// identified by a fully qualified path. The underlying system knows how to access the source
    /// configuration when it is provided the fully qualified path.
    /// </remarks>
    public class AlarmConditionServerNodeManager : CustomNodeManager2
    {
        /// <summary>
        /// Initializes the node manager.
        /// </summary>
        /// <param name="server"></param>
        /// <param name="configuration"></param>
        /// <param name="timeservice"></param>
        public AlarmConditionServerNodeManager(IServerInternal server, ApplicationConfiguration configuration, TimeService timeservice)
        :
            base(server, configuration, Namespaces.AlarmCondition)
        {
            SystemContext.SystemHandle = _system = new UnderlyingSystem(timeservice);
            SystemContext.NodeIdFactory = this;

            // get the configuration for the node manager.
            _configuration = configuration.ParseExtension<AlarmConditionServerConfiguration>();

            // use suitable defaults if no configuration exists.
            _configuration ??= new AlarmConditionServerConfiguration
            {
                Areas = [
                        new AreaConfiguration {
                            Name = "Green",
                            SubAreas = [
                                new AreaConfiguration {
                                    Name = "East",
                                    SubAreas = [
                                        new AreaConfiguration {
                                            Name = "Red",
                                            SourcePaths = [
                                                "Colours/EastTank",
                                                "Colours/NorthMotor"
                                            ]
                                        },
                                        new AreaConfiguration {
                                            Name = "Blue",
                                            SourcePaths = [
                                                "Metals/WestTank",
                                                "Metals/SouthMotor"
                                            ]
                                        }
                                    ]
                                }
                            ]
                        },
                        new AreaConfiguration {
                            Name = "Yellow",
                            SubAreas = [
                                new AreaConfiguration {
                                    Name = "West",
                                    SubAreas = [
                                        new AreaConfiguration {
                                            Name = "Red",
                                            SourcePaths = [
                                                "Metals/SouthMotor",
                                                "Colours/NorthMotor"
                                            ]
                                        },
                                        new AreaConfiguration {
                                            Name = "Blue",
                                            SourcePaths = [
                                                "Colours/EastTank",
                                                "Metals/WestTank"
                                            ]
                                        }
                                    ]
                                }
                            ]
                        }
                    ]
            };

            // create the table to store the available areas.
            _areas = [];

            // create the table to store the available sources.
            _sources = [];
            _timeservice = timeservice;
        }

        /// <summary>
        /// An overrideable version of the Dispose.
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_system != null)
                {
                    _system.Dispose();
                    _system = null;
                }
                if (_simulationTimer != null)
                {
                    _simulationTimer.Dispose();
                    _simulationTimer = null;
                }
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
                if (_configuration.Areas != null)
                {
                    // Top level areas need a reference from the Server object.
                    // These references are added to a list that is returned to the caller.
                    // The caller will update the Objects folder node.

                    if (!externalReferences.TryGetValue(ObjectIds.Server, out var references))
                    {
                        externalReferences[ObjectIds.Server] = references = [];
                    }

                    for (var ii = 0; ii < _configuration.Areas.Count; ii++)
                    {
                        // recursively process each area.
                        var area = CreateAndIndexAreas(null, _configuration.Areas[ii]);
                        AddRootNotifier(area);

                        // add an organizes reference from the ObjectsFolder to the area.
                        references.Add(new NodeStateReference(ReferenceTypeIds.HasNotifier, false, area.NodeId));
                    }
                }

                // start the simulation.
                _system.StartSimulation();
                _simulationTimer = new Timer(OnRaiseSystemEvents, null, 1000, 1000);
            }
        }

        private void OnRaiseSystemEvents(object state)
        {
            try
            {
                var e = new SystemEventState(null);

                e.Initialize(
                    SystemContext,
                    null,
                    EventSeverity.Medium,
                    new LocalizedText("Raising Events"));

                e.SetChildValue(SystemContext, BrowseNames.SourceNode, ObjectIds.Server, false);
                e.SetChildValue(SystemContext, BrowseNames.SourceName, "Internal", false);

                Server.ReportEvent(e);

                var ae = new AuditEventState(null);

                ae.Initialize(
                    SystemContext,
                    null,
                    EventSeverity.Medium,
                    new LocalizedText("Events Raised"),
                    true,
                    _timeservice.UtcNow);

                ae.SetChildValue(SystemContext, BrowseNames.SourceNode, ObjectIds.Server, false);
                ae.SetChildValue(SystemContext, BrowseNames.SourceName, "Internal", false);

                Server.ReportEvent(ae);
            }
            catch (Exception e)
            {
                Utils.Trace(e, "Unexpected error in OnRaiseSystemEvents");
            }
        }

        /// <summary>
        /// Creates and indexes an area defined for the server.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="configuration"></param>
        private AreaState CreateAndIndexAreas(AreaState parent, AreaConfiguration configuration)
        {
            // create a unique path to the area.
            var areaPath = Utils.Format("{0}/{1}", (parent != null) ? parent.SymbolicName : string.Empty, configuration.Name);
            var areaId = ModelUtils.ConstructIdForArea(areaPath, NamespaceIndex);

            // create the object that will be used to access the area and any variables contained within it.
            var area = new AreaState(SystemContext, parent, areaId, configuration);
            _areas[areaPath] = area;

            parent?.AddChild(area);

            // create an index any sub-areas defined for the area.
            if (configuration.SubAreas != null)
            {
                for (var ii = 0; ii < configuration.SubAreas.Count; ii++)
                {
                    CreateAndIndexAreas(area, configuration.SubAreas[ii]);
                }
            }

            // add references to sources.
            if (configuration.SourcePaths != null)
            {
                for (var ii = 0; ii < configuration.SourcePaths.Count; ii++)
                {
                    var sourcePath = configuration.SourcePaths[ii];

                    // check if the source already exists because it is referenced by another area.

                    if (!_sources.TryGetValue(sourcePath, out var source))
                    {
                        var sourceId = ModelUtils.ConstructIdForSource(sourcePath, NamespaceIndex);
                        _sources[sourcePath] = source = new SourceState(this, sourceId, sourcePath, _timeservice);
                    }

                    // HasEventSource and HasNotifier control the propagation of event notifications so
                    // they are not like other references. These calls set up a link between the source
                    // and area that will cause events produced by the source to be automatically
                    // propagated to the area.
                    source.AddNotifier(SystemContext, ReferenceTypeIds.HasEventSource, true, area);
                    area.AddNotifier(SystemContext, ReferenceTypeIds.HasEventSource, false, source);
                }
            }

            return area;
        }

        /// <summary>
        /// Frees any resources allocated for the address space.
        /// </summary>
        public override void DeleteAddressSpace()
        {
            lock (Lock)
            {
                _system.StopSimulation();
                _areas.Clear();
                _sources.Clear();
            }
        }

        /// <summary>
        /// Returns a unique handle for the node.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="nodeId"></param>
        /// <param name="cache"></param>
        protected override NodeHandle GetManagerHandle(ServerSystemContext context,
            NodeId nodeId, IDictionary<NodeId, NodeState> cache)
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

                // validate area.
                if (parsedNodeId.RootType == ModelUtils.Area)
                {
                    if (!_areas.TryGetValue(parsedNodeId.RootId, out var area))
                    {
                        return null;
                    }

                    root = area;
                }

                // validate soucre.
                else if (parsedNodeId.RootType == ModelUtils.Source)
                {
                    if (!_sources.TryGetValue(parsedNodeId.RootId, out var source))
                    {
                        return null;
                    }

                    root = source;
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

        private UnderlyingSystem _system;
        private readonly AlarmConditionServerConfiguration _configuration;
        private readonly Dictionary<string, AreaState> _areas;
        private readonly Dictionary<string, SourceState> _sources;
        private readonly TimeService _timeservice;
        private Timer _simulationTimer;
    }
}
