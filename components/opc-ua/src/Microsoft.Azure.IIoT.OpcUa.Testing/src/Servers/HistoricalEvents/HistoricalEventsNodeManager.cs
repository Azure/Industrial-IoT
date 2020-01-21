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

namespace HistoricalEvents {
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Reflection;
    using System.Data;
    using Opc.Ua;
    using Opc.Ua.Server;

    /// <summary>
    /// A node manager for a server that exposes several variables.
    /// </summary>
    public class HistoricalEventsNodeManager : CustomNodeManager2 {

        /// <summary>
        /// Initializes the node manager.
        /// </summary>
        public HistoricalEventsNodeManager(IServerInternal server,
            ApplicationConfiguration configuration) :
            base(server, configuration) {
            SystemContext.NodeIdFactory = this;

            // set one namespace for the type model and one names for dynamically created nodes.
            var namespaceUrls = new string[1];
            namespaceUrls[0] = Namespaces.HistoricalEvents;
            SetNamespaces(namespaceUrls);

            // get the configuration for the node manager.
            _configuration = configuration.ParseExtension<HistoricalEventsServerConfiguration>();

            // use suitable defaults if no configuration exists.
            if (_configuration == null) {
                _configuration = new HistoricalEventsServerConfiguration();
            }

            // initilize the report generator.
            _generator = new ReportGenerator();
            _generator.Initialize();
        }

        /// <summary>
        /// An overrideable version of the Dispose.
        /// </summary>
        protected override void Dispose(bool disposing) {
            if (disposing) {
                if (_simulationTimer != null) {
                    Utils.SilentDispose(_simulationTimer);
                    _simulationTimer = null;
                }
            }
        }

        /// <summary>
        /// Creates the NodeId for the specified node.
        /// </summary>
        public override NodeId New(ISystemContext context, NodeState node) {
            return node.NodeId;
        }

        /// <summary>
        /// Loads a node set from a file or resource and addes them to the set of predefined nodes.
        /// </summary>
        protected override NodeStateCollection LoadPredefinedNodes(ISystemContext context) {
            var type = GetType().GetTypeInfo();
            var predefinedNodes = new NodeStateCollection();
            predefinedNodes.LoadFromBinaryResource(context,
                $"{type.Assembly.GetName().Name}.Servers.{type.Namespace}.Design.{type.Namespace}.PredefinedNodes.uanodes",
                type.Assembly, true);
            return predefinedNodes;
        }

        /// <summary>
        /// Does any initialization required before the address space can be used.
        /// </summary>
        /// <remarks>
        /// The externalReferences is an out parameter that allows the node manager to link to nodes
        /// in other node managers. For example, the 'Objects' node is managed by the CoreNodeManager and
        /// should have a reference to the root folder node(s) exposed by this node manager.
        /// </remarks>
        public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences) {
            lock (Lock) {
                LoadPredefinedNodes(SystemContext, externalReferences);

                var platforms = (BaseObjectState)FindPredefinedNode(new NodeId(Objects.Plaforms, NamespaceIndex), typeof(BaseObjectState));
                platforms.EventNotifier = EventNotifiers.SubscribeToEvents | EventNotifiers.HistoryRead | EventNotifiers.HistoryWrite;
                base.AddRootNotifier(platforms);

                foreach (var areaName in _generator.GetAreas()) {
                    var area = CreateArea(SystemContext, platforms, areaName);

                    foreach (var well in _generator.GetWells(areaName)) {
                        CreateWell(SystemContext, area, well.Id, well.Name);
                    }
                }

                // start the simulation.
                _simulationTimer = new Timer(DoSimulation, null, 10000, 10000);
            }
        }

        /// <summary>
        /// Creates a new area.
        /// </summary>
        private BaseObjectState CreateArea(SystemContext context, BaseObjectState platforms, string areaName) {
            System.Diagnostics.Contracts.Contract.Assume(context != null);
            var area = new FolderState(null) {
                NodeId = new NodeId(areaName, NamespaceIndex),
                BrowseName = new QualifiedName(areaName, NamespaceIndex)
            };
            area.DisplayName = area.BrowseName.Name;
            area.EventNotifier = EventNotifiers.SubscribeToEvents | EventNotifiers.HistoryRead | EventNotifiers.HistoryWrite;
            area.TypeDefinitionId = Opc.Ua.ObjectTypeIds.FolderType;

            platforms.AddNotifier(SystemContext, ReferenceTypeIds.HasNotifier, false, area);
            area.AddNotifier(SystemContext, ReferenceTypeIds.HasNotifier, true, platforms);

            AddPredefinedNode(SystemContext, area);

            return area;
        }

        /// <summary>
        /// Creates a new well.
        /// </summary>
        private void CreateWell(SystemContext context, BaseObjectState area, string wellId, string wellName) {
            System.Diagnostics.Contracts.Contract.Assume(context != null);
            var well = new WellState(null) {
                NodeId = new NodeId(wellId, NamespaceIndex),
                BrowseName = new QualifiedName(wellName, NamespaceIndex),
                DisplayName = wellName,
                EventNotifier = EventNotifiers.SubscribeToEvents | EventNotifiers.HistoryRead | EventNotifiers.HistoryWrite,
                TypeDefinitionId = new NodeId(ObjectTypes.WellType, NamespaceIndex)
            };

            area.AddNotifier(SystemContext, ReferenceTypeIds.HasNotifier, false, well);
            well.AddNotifier(SystemContext, ReferenceTypeIds.HasNotifier, true, area);

            AddPredefinedNode(SystemContext, well);
        }

        /// <summary>
        /// Frees any resources allocated for the address space.
        /// </summary>
        public override void DeleteAddressSpace() {
            lock (Lock) {
                base.DeleteAddressSpace();
            }
        }

        /// <summary>
        /// Returns a unique handle for the node.
        /// </summary>
        protected override NodeHandle GetManagerHandle(ServerSystemContext context, NodeId nodeId, IDictionary<NodeId, NodeState> cache) {
            lock (Lock) {
                // quickly exclude nodes that are not in the namespace.
                if (!IsNodeIdInNamespace(nodeId)) {
                    return null;
                }

                // check for predefined nodes.
                if (PredefinedNodes != null) {

                    if (PredefinedNodes.TryGetValue(nodeId, out var node)) {
                        var handle = new NodeHandle {
                            NodeId = nodeId,
                            Validated = true,
                            Node = node
                        };

                        return handle;
                    }
                }

                return null;
            }
        }

        /// <summary>
        /// Verifies that the specified node exists.
        /// </summary>
        protected override NodeState ValidateNode(
            ServerSystemContext context,
            NodeHandle handle,
            IDictionary<NodeId, NodeState> cache) {
            // not valid if no root.
            if (handle == null) {
                return null;
            }

            // check if previously validated.
            if (handle.Validated) {
                return handle.Node;
            }

            // TBD

            return null;
        }



        /// <summary>
        /// Reads history events.
        /// </summary>
        protected override void HistoryReadEvents(
            ServerSystemContext context,
            ReadEventDetails details,
            TimestampsToReturn timestampsToReturn,
            IList<HistoryReadValueId> nodesToRead,
            IList<HistoryReadResult> results,
            IList<ServiceResult> errors,
            List<NodeHandle> nodesToProcess,
            IDictionary<NodeId, NodeState> cache) {
            for (var ii = 0; ii < nodesToProcess.Count; ii++) {
                var handle = nodesToProcess[ii];
                var nodeToRead = nodesToRead[handle.Index];
                var result = results[handle.Index];

                HistoryReadRequest request = null;

                // load an exising request.
                if (nodeToRead.ContinuationPoint != null) {
                    request = LoadContinuationPoint(context, nodeToRead.ContinuationPoint);

                    if (request == null) {
                        errors[handle.Index] = StatusCodes.BadContinuationPointInvalid;
                        continue;
                    }
                }

                // create a new request.
                else {
                    request = CreateHistoryReadRequest(
                        context,
                        details,
                        handle,
                        nodeToRead);
                }

                // process events until the max is reached.
                var events = new HistoryEvent();

                while (request.NumValuesPerNode == 0 || events.Events.Count < request.NumValuesPerNode) {
                    if (request.Events.Count == 0) {
                        break;
                    }

                    BaseEventState e = null;

                    if (request.TimeFlowsBackward) {
                        e = request.Events.Last.Value;
                        request.Events.RemoveLast();
                    }
                    else {
                        e = request.Events.First.Value;
                        request.Events.RemoveFirst();
                    }

                    events.Events.Add(GetEventFields(request, e));
                }

                errors[handle.Index] = ServiceResult.Good;

                // check if a continuation point is requred.
                if (request.Events.Count > 0) {
                    // only set if both end time and start time are specified.
                    if (details.StartTime != DateTime.MinValue && details.EndTime != DateTime.MinValue) {
                        result.ContinuationPoint = SaveContinuationPoint(context, request);
                    }
                }

                // check if no data returned.
                else {
                    errors[handle.Index] = StatusCodes.GoodNoData;
                }

                // return the data.
                result.HistoryData = new ExtensionObject(events);
            }
        }

        /// <summary>
        /// Updates or inserts events.
        /// </summary>
        protected override void HistoryUpdateEvents(
            ServerSystemContext context,
            IList<UpdateEventDetails> nodesToUpdate,
            IList<HistoryUpdateResult> results,
            IList<ServiceResult> errors,
            List<NodeHandle> nodesToProcess,
            IDictionary<NodeId, NodeState> cache) {
            for (var ii = 0; ii < nodesToProcess.Count; ii++) {
                var handle = nodesToProcess[ii];
                var nodeToUpdate = nodesToUpdate[handle.Index];
                var result = results[handle.Index];

                // validate the event filter.
                var filterContext = new FilterContext(context.NamespaceUris, context.TypeTable, context);
                var filterResult = nodeToUpdate.Filter.Validate(filterContext);

                if (ServiceResult.IsBad(filterResult.Status)) {
                    errors[handle.Index] = filterResult.Status;
                    continue;
                }

                // all done.
                errors[handle.Index] = StatusCodes.BadNotImplemented;
            }
        }

        /// <summary>
        /// Deletes history events.
        /// </summary>
        protected override void HistoryDeleteEvents(
            ServerSystemContext context,
            IList<DeleteEventDetails> nodesToUpdate,
            IList<HistoryUpdateResult> results,
            IList<ServiceResult> errors,
            List<NodeHandle> nodesToProcess,
            IDictionary<NodeId, NodeState> cache) {
            for (var ii = 0; ii < nodesToProcess.Count; ii++) {
                var handle = nodesToProcess[ii];
                var nodeToUpdate = nodesToUpdate[handle.Index];
                var result = results[handle.Index];

                // delete events.
                var failed = false;

                for (var jj = 0; jj < nodeToUpdate.EventIds.Count; jj++) {
                    try {
                        var eventId = new Guid(nodeToUpdate.EventIds[jj]).ToString();

                        if (!_generator.DeleteEvent(eventId)) {
                            result.OperationResults.Add(StatusCodes.BadEventIdUnknown);
                            failed = true;
                            continue;
                        }

                        result.OperationResults.Add(StatusCodes.Good);
                    }
                    catch {
                        result.OperationResults.Add(StatusCodes.BadEventIdUnknown);
                        failed = true;
                    }
                }

                // check if diagnostics are required.
                if (failed) {
                    if ((context.DiagnosticsMask & DiagnosticsMasks.OperationAll) != 0) {
                        for (var jj = 0; jj < nodeToUpdate.EventIds.Count; jj++) {
                            if (StatusCode.IsBad(result.OperationResults[jj])) {
                                result.DiagnosticInfos.Add(ServerUtils.CreateDiagnosticInfo(Server, context.OperationContext, result.OperationResults[jj]));
                            }
                        }
                    }
                }

                // clear operation results if all good.
                else {
                    result.OperationResults.Clear();
                }

                // all done.
                errors[handle.Index] = ServiceResult.Good;
            }
        }


        /// <summary>
        /// Fetches the requested event fields from the event.
        /// </summary>
        private HistoryEventFieldList GetEventFields(HistoryReadRequest request, IFilterTarget instance) {
            // fetch the event fields.
            var fields = new HistoryEventFieldList();

            foreach (var clause in request.Filter.SelectClauses) {
                // get the value of the attribute (apply localization).
                var value = instance.GetAttributeValue(
                    request.FilterContext,
                    clause.TypeDefinitionId,
                    clause.BrowsePath,
                    clause.AttributeId,
                    clause.ParsedIndexRange);

                // add the value to the list of event fields.
                if (value != null) {
                    // translate any localized text.
                    var text = value as LocalizedText;

                    if (text != null) {
                        value = Server.ResourceManager.Translate(request.FilterContext.PreferredLocales, text);
                    }

                    // add value.
                    fields.EventFields.Add(new Variant(value));
                }

                // add a dummy entry for missing values.
                else {
                    fields.EventFields.Add(Variant.Null);
                }
            }

            return fields;
        }

        /// <summary>
        /// Creates a new history request.
        /// </summary>
        private HistoryReadRequest CreateHistoryReadRequest(
            ServerSystemContext context,
            ReadEventDetails details,
            NodeHandle handle,
            HistoryReadValueId nodeToRead) {
            System.Diagnostics.Contracts.Contract.Assume(nodeToRead != null);
            var filterContext = new FilterContext(context.NamespaceUris, context.TypeTable, context.PreferredLocales);
            var events = new LinkedList<BaseEventState>();

            for (var ii = ReportType.FluidLevelTest; ii <= ReportType.InjectionTest; ii++) {
                DataView view = null;

                if (handle.Node is WellState) {
                    view = _generator.ReadHistoryForWellId(
                        ii,
                        (string)handle.Node.NodeId.Identifier,
                        details.StartTime,
                        details.EndTime);
                }
                else {
                    view = _generator.ReadHistoryForArea(
                        ii,
                        handle.Node.NodeId.Identifier as string,
                        details.StartTime,
                        details.EndTime);
                }

                var pos = events.First;
                var sizeLimited = details.StartTime == DateTime.MinValue || details.EndTime == DateTime.MinValue;

                foreach (DataRowView row in view) {
                    // check if reached max results.
                    if (sizeLimited) {
                        if (events.Count >= details.NumValuesPerNode) {
                            break;
                        }
                    }

                    var e = _generator.GetReport(context, NamespaceIndex, ii, row.Row);

                    if (details.Filter.WhereClause?.Elements.Count > 0) {
                        if (!details.Filter.WhereClause.Evaluate(filterContext, e)) {
                            continue;
                        }
                    }

                    var inserted = false;

                    for (var jj = pos; jj != null; jj = jj.Next) {
                        if (jj.Value.Time.Value > e.Time.Value) {
                            events.AddBefore(jj, e);
                            pos = jj;
                            inserted = true;
                            break;
                        }
                    }

                    if (!inserted) {
                        events.AddLast(e);
                        pos = null;
                    }
                }
            }

            var request = new HistoryReadRequest {
                Events = events,
                TimeFlowsBackward = details.StartTime == DateTime.MinValue || (details.EndTime != DateTime.MinValue && details.EndTime < details.StartTime),
                NumValuesPerNode = details.NumValuesPerNode,
                Filter = details.Filter,
                FilterContext = filterContext
            };
            return request;
        }

        /// <summary>
        /// Stores a read history request.
        /// </summary>
        private class HistoryReadRequest {
            public byte[] ContinuationPoint { get; set; }
            public LinkedList<BaseEventState> Events { get; set; }
            public bool TimeFlowsBackward { get; set; }
            public uint NumValuesPerNode { get; set; }
            public EventFilter Filter { get; set; }
            public FilterContext FilterContext { get; set; }
        }

        /// <summary>
        /// Releases the history continuation point.
        /// </summary>
        protected override void HistoryReleaseContinuationPoints(
            ServerSystemContext context,
            IList<HistoryReadValueId> nodesToRead,
            IList<ServiceResult> errors,
            List<NodeHandle> nodesToProcess,
            IDictionary<NodeId, NodeState> cache) {
            for (var ii = 0; ii < nodesToProcess.Count; ii++) {
                var handle = nodesToProcess[ii];
                var nodeToRead = nodesToRead[handle.Index];

                // find the continuation point.
                var request = LoadContinuationPoint(context, nodeToRead.ContinuationPoint);

                if (request == null) {
                    errors[handle.Index] = StatusCodes.BadContinuationPointInvalid;
                    continue;
                }

                // all done.
                errors[handle.Index] = StatusCodes.Good;
            }
        }

        /// <summary>
        /// Loads a history continuation point.
        /// </summary>
        private HistoryReadRequest LoadContinuationPoint(
            ServerSystemContext context,
            byte[] continuationPoint) {
            var session = context.OperationContext.Session;

            if (session == null) {
                return null;
            }


            if (!(session.RestoreHistoryContinuationPoint(continuationPoint) is HistoryReadRequest request)) {
                return null;
            }

            return request;
        }

        /// <summary>
        /// Saves a history continuation point.
        /// </summary>
        private byte[] SaveContinuationPoint(
            ServerSystemContext context,
            HistoryReadRequest request) {
            var session = context.OperationContext.Session;

            if (session == null) {
                return null;
            }

            var id = Guid.NewGuid();
            session.SaveHistoryContinuationPoint(id, request);
            request.ContinuationPoint = id.ToByteArray();
            return request.ContinuationPoint;
        }




        /// <summary>
        /// Does the simulation.
        /// </summary>
        /// <param name="state">The state.</param>
        private void DoSimulation(object state) {
            try {
                {
                    var row = _generator.GenerateFluidLevelTestReport();
                    var well = (BaseObjectState)FindPredefinedNode(new NodeId((string)row[BrowseNames.UidWell], NamespaceIndex), typeof(BaseObjectState));

                    if (well?.AreEventsMonitored == true) {
                        var e = _generator.GetFluidLevelTestReport(SystemContext, NamespaceIndex, row);
                        well.ReportEvent(SystemContext, e);
                    }
                }

                {
                    var row = _generator.GenerateInjectionTestReport();
                    var well = (BaseObjectState)FindPredefinedNode(new NodeId((string)row[BrowseNames.UidWell], NamespaceIndex), typeof(BaseObjectState));

                    if (well?.AreEventsMonitored == true) {
                        var e = _generator.GetInjectionTestReport(SystemContext, NamespaceIndex, row);
                        well.ReportEvent(SystemContext, e);
                    }
                }
            }
            catch (Exception e) {
                Utils.Trace(e, "Unexpected error during simulation.");
            }
        }

        private readonly HistoricalEventsServerConfiguration _configuration;
#pragma warning disable IDE0069 // Disposable fields should be disposed
        private Timer _simulationTimer;
#pragma warning restore IDE0069 // Disposable fields should be disposed
        private readonly ReportGenerator _generator;
    }
}
