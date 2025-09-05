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

namespace HistoricalAccess
{
    using Opc.Ua;
    using Opc.Ua.Server;
    using Opc.Ua.Test;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Reflection;
    using System.Threading;

    /// <summary>
    /// A node manager for a server that exposes several variables.
    /// </summary>
    public class HistoricalAccessServerNodeManager : CustomNodeManager2
    {
        /// <summary>
        /// Initializes the node manager.
        /// </summary>
        /// <param name="server"></param>
        /// <param name="configuration"></param>
        /// <param name="timeService"></param>
        public HistoricalAccessServerNodeManager(IServerInternal server, ApplicationConfiguration configuration, TimeService timeService)
        :
            base(server, configuration, Namespaces.HistoricalAccess)
        {
            AliasRoot = "HDA";

            // get the configuration for the node manager.
            _configuration = configuration.ParseExtension<HistoricalAccessServerConfiguration>();

            // use suitable defaults if no configuration exists.
            _configuration ??= new HistoricalAccessServerConfiguration
            {
                ArchiveRoot = "Historian"
            };

            _timeService = timeService;
            SystemContext.SystemHandle = _system = new UnderlyingSystem(_configuration, NamespaceIndex, timeService);
            SystemContext.NodeIdFactory = this;
        }

        /// <summary>
        /// An overrideable version of the Dispose.
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && _simulationTimer != null)
            {
                _simulationTimer.Dispose();
                _simulationTimer = null;
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
            if (node is BaseInstanceState instance && instance.Parent != null)
            {
                return NodeTypes.ConstructIdForComponent(instance, instance.Parent.NodeId.NamespaceIndex);
            }

            return node.NodeId;
        }

        /// <summary>
        /// Does any initialization required before the address space can be used.
        /// </summary>
        /// <param name="externalReferences"></param>
        public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences)
        {
            lock (Server.DiagnosticsLock)
            {
                var capabilities = Server.DiagnosticsNodeManager.GetDefaultHistoryCapabilities();
                capabilities.AccessHistoryDataCapability.Value = true;
                capabilities.InsertDataCapability.Value = true;
                capabilities.ReplaceDataCapability.Value = true;
                capabilities.UpdateDataCapability.Value = true;
                capabilities.DeleteRawCapability.Value = true;
                capabilities.DeleteAtTimeCapability.Value = true;
                capabilities.InsertAnnotationCapability.Value = true;
            }

            lock (Lock)
            {
                if (!externalReferences.TryGetValue(ObjectIds.ObjectsFolder, out var references))
                {
                    externalReferences[ObjectIds.ObjectsFolder] = references = [];
                }

                var root = _system.GetFolderState(string.Empty);
                references.Add(new NodeStateReference(ReferenceTypeIds.Organizes, false, root.NodeId));
                root.AddReference(ReferenceTypeIds.Organizes, true, ObjectIds.ObjectsFolder);

                CreateFolderFromResources(root, "Sample");
                // CreateFolderFromResources(root, "Dynamic");
            }
        }

        /// <summary>
        /// Creates items from embedded resources.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="folderName"></param>
        private void CreateFolderFromResources(NodeState root, string folderName)
        {
            var dataFolder = new FolderState(root)
            {
                ReferenceTypeId = ReferenceTypeIds.Organizes,
                TypeDefinitionId = ObjectTypeIds.FolderType,
                NodeId = new NodeId(folderName, NamespaceIndex),
                BrowseName = new QualifiedName(folderName, NamespaceIndex)
            };
            dataFolder.DisplayName = dataFolder.BrowseName.Name;
            dataFolder.WriteMask = AttributeWriteMask.None;
            dataFolder.UserWriteMask = AttributeWriteMask.None;
            dataFolder.EventNotifier = EventNotifiers.None;
            root.AddChild(dataFolder);
            AddPredefinedNode(SystemContext, root);

            var type = GetType().GetTypeInfo();
            foreach (var resourcePath in Assembly.GetExecutingAssembly().GetManifestResourceNames())
            {
                if (!resourcePath.StartsWith($"{type.Assembly.GetName().Name}.HistoricalAccess.Data.{folderName}.", StringComparison.Ordinal))
                {
                    continue;
                }

                var item = new ArchiveItem(resourcePath, Assembly.GetExecutingAssembly(), resourcePath);
                var node = new ArchiveItemState(SystemContext, item, NamespaceIndex, _timeService);
                node.ReloadFromSource(SystemContext);

                dataFolder.AddReference(ReferenceTypeIds.Organizes, false, node.NodeId);
                node.AddReference(ReferenceTypeIds.Organizes, true, dataFolder.NodeId);

                AddPredefinedNode(SystemContext, node);
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

                // check for predefined nodes,

                if (PredefinedNodes.TryGetValue(nodeId, out var node))
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
            // lookup in cache.
            var target = FindNodeInCache(context, handle, cache);

            if (target != null)
            {
                handle.Node = target;
                handle.Validated = true;
                return handle.Node;
            }

            var pnd = (ParsedNodeId)handle.ParsedNodeId;

            // check for a new node.
            switch (pnd.RootType)
            {
                case NodeTypes.Folder:
                    {
                        target = _system.GetFolderState(pnd.RootId);
                        break;
                    }

                case NodeTypes.Item:
                    {
                        var item = _system.GetItemState(SystemContext, pnd);
                        item.LoadConfiguration(context);
                        target = item;
                        break;
                    }
            }

            // root is not valid.
            if (target == null)
            {
                return null;
            }

            // validate component.
            if (!string.IsNullOrEmpty(pnd.ComponentPath))
            {
                NodeState component = target.FindChildBySymbolicName(context, pnd.ComponentPath);

                // component does not exist.
                if (component == null)
                {
                    return null;
                }

                target = component;
            }

            // put root into cache.
            if (cache != null)
            {
                cache[handle.NodeId] = target;
            }

            handle.Node = target;
            handle.Validated = true;
            return handle.Node;
        }

        /// <summary>
        /// Validates the nodes and reads the values from the underlying source.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="nodesToRead"></param>
        /// <param name="values"></param>
        /// <param name="errors"></param>
        /// <param name="nodesToValidate"></param>
        /// <param name="cache"></param>
        protected override void Read(
            ServerSystemContext context,
            IList<ReadValueId> nodesToRead,
            IList<DataValue> values,
            IList<ServiceResult> errors,
            List<NodeHandle> nodesToValidate,
            IDictionary<NodeId, NodeState> cache)
        {
            for (var ii = 0; ii < nodesToValidate.Count; ii++)
            {
                var handle = nodesToValidate[ii];

                lock (Lock)
                {
                    // validate node.
                    var source = ValidateNode(context, handle, cache);

                    if (source == null)
                    {
                        continue;
                    }

                    // check if the node needs to be initialized from disk.

                    if (source.GetHierarchyRoot() is ArchiveItemState item
                        && item.ArchiveItem.LastLoadTime.AddDays(1) < _timeService.UtcNow)
                    {
                        item.LoadConfiguration(context);
                    }

                    var nodeToRead = nodesToRead[handle.Index];
                    var value = values[handle.Index];

                    // update the attribute value.
                    errors[handle.Index] = source.ReadAttribute(
                        context,
                        nodeToRead.AttributeId,
                        nodeToRead.ParsedIndexRange,
                        nodeToRead.DataEncoding,
                        value);
                }
            }
        }

        /// <summary>
        /// Reads the initial value for a monitored item.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="handle">The item handle.</param>
        /// <param name="monitoredItem">The monitored item.</param>
        protected override ServiceResult ReadInitialValue(
            ISystemContext context,
            NodeHandle handle,
            IDataChangeMonitoredItem2 monitoredItem)
        {
            var monitoredItemObj = monitoredItem as MonitoredItem;
            if (handle.Node is not ArchiveItemState || monitoredItemObj?.AttributeId != Attributes.Value)
            {
                return base.ReadInitialValue(context, handle, monitoredItem);
            }

            if (monitoredItemObj?.Filter is not AggregateFilter filter ||
                filter.StartTime >= _timeService.UtcNow.AddMilliseconds(-filter.ProcessingInterval))
            {
                return base.ReadInitialValue(context, handle, monitoredItem);
            }

            var details = new ReadRawModifiedDetails
            {
                StartTime = filter.StartTime,
                EndTime = _timeService.UtcNow,
                ReturnBounds = true,
                IsReadModified = false,
                NumValuesPerNode = 0
            };

            var nodeToRead = new HistoryReadValueId
            {
                NodeId = handle.NodeId,
                ParsedIndexRange = NumericRange.Empty
            };

            try
            {
                var request = CreateHistoryReadRequest(
                    context as ServerSystemContext,
                    details,
                    handle,
                    nodeToRead);

                while (request.Values.Count > 0)
                {
                    if (request.Values.Count == 0)
                    {
                        break;
                    }

                    var value = request.Values.First.Value;
                    request.Values.RemoveFirst();
                    monitoredItemObj.QueueValue(value, null, true);
                }
                return StatusCodes.Good;
            }
            catch (Exception e)
            {
                var error = ServiceResult.Create(e, StatusCodes.BadUnexpectedError, "Unexpected error fetching initial values.");
                monitoredItemObj.QueueValue(null, error, true);
                return error;
            }
        }

        /// <summary>
        /// Called after creating a MonitoredItem.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="handle"></param>
        /// <param name="monitoredItem"></param>
        protected override void OnMonitoredItemCreated(ServerSystemContext context, NodeHandle handle, ISampledDataChangeMonitoredItem monitoredItem)
        {
            lock (Lock)
            {
                var root = handle.Node.GetHierarchyRoot();

                if (root != null && root is ArchiveItemState item)
                {
                    _monitoredItems ??= [];

                    _monitoredItems.TryAdd(item.ArchiveItem.UniquePath, item);
                    item.SubscribeCount++;

                    _simulationTimer ??= new Timer(DoSimulation, null, 500, 500);
                }
            }
        }

        /// <summary>
        /// Revises an aggregate filter (may require knowledge of the variable being used).
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="handle">The handle.</param>
        /// <param name="samplingInterval">The sampling interval for the monitored item.</param>
        /// <param name="queueSize">The queue size for the monitored item.</param>
        /// <param name="filterToUse">The filter to revise.</param>
        /// <returns>Good if the filter is acceptable.</returns>
        protected override StatusCode ReviseAggregateFilter(
            ServerSystemContext context,
            NodeHandle handle,
            double samplingInterval,
            uint queueSize,
            ServerAggregateFilter filterToUse)
        {
            // use the sampling interval to limit the processing interval.
            if (filterToUse.ProcessingInterval < samplingInterval)
            {
                filterToUse.ProcessingInterval = samplingInterval;
            }

            // check if an archive item.

            if (handle.Node is not ArchiveItemState item)
            {
                // no historial data so must start in the future.
                while (filterToUse.StartTime < _timeService.UtcNow)
                {
                    filterToUse.StartTime = filterToUse.StartTime.AddMilliseconds(filterToUse.ProcessingInterval);
                }

                // use suitable defaults for values which are are not archived items.
                filterToUse.AggregateConfiguration.UseServerCapabilitiesDefaults = false;
                filterToUse.AggregateConfiguration.UseSlopedExtrapolation = false;
                filterToUse.AggregateConfiguration.TreatUncertainAsBad = false;
                filterToUse.AggregateConfiguration.PercentDataBad = 100;
                filterToUse.AggregateConfiguration.PercentDataGood = 100;
                filterToUse.Stepped = true;
            }
            else
            {
                // use the archive acquisition sampling interval to limit the processing interval.
                if (filterToUse.ProcessingInterval < item.ArchiveItem.SamplingInterval)
                {
                    filterToUse.ProcessingInterval = item.ArchiveItem.SamplingInterval;
                }

                // ensure the buffer does not get overfilled.
                while (filterToUse.StartTime.AddMilliseconds(queueSize * filterToUse.ProcessingInterval) < _timeService.UtcNow)
                {
                    filterToUse.StartTime = filterToUse.StartTime.AddMilliseconds(filterToUse.ProcessingInterval);
                }

                filterToUse.Stepped = item.ArchiveItem.Stepped;

                // revise the configration.
                ReviseAggregateConfiguration(context, item, filterToUse.AggregateConfiguration);
            }

            return StatusCodes.Good;
        }

        /// <summary>
        /// Revises the aggregate configuration.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="item"></param>
        /// <param name="configurationToUse"></param>
        private void ReviseAggregateConfiguration(
            ServerSystemContext context,
            ArchiveItemState item,
            AggregateConfiguration configurationToUse)
        {
            System.Diagnostics.Contracts.Contract.Assume(context != null);
            // set configuration from defaults.
            if (configurationToUse.UseServerCapabilitiesDefaults)
            {
                var configuration = item.ArchiveItem.AggregateConfiguration;

                if (configuration?.UseServerCapabilitiesDefaults != false)
                {
                    configuration = Server.AggregateManager.GetDefaultConfiguration(null);
                }

                configurationToUse.UseSlopedExtrapolation = configuration.UseSlopedExtrapolation;
                configurationToUse.TreatUncertainAsBad = configuration.TreatUncertainAsBad;
                configurationToUse.PercentDataBad = configuration.PercentDataBad;
                configurationToUse.PercentDataGood = configuration.PercentDataGood;
            }

            // override configuration when it does not make sense for the item.
            configurationToUse.UseServerCapabilitiesDefaults = false;

            if (item.ArchiveItem.Stepped)
            {
                configurationToUse.UseSlopedExtrapolation = false;
            }
        }

        /// <summary>
        /// Called after deleting a MonitoredItem.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="handle"></param>
        /// <param name="monitoredItem"></param>
        protected override void OnMonitoredItemDeleted(ServerSystemContext context, NodeHandle handle, ISampledDataChangeMonitoredItem monitoredItem)
        {
            lock (Lock)
            {
                var root = handle.Node.GetHierarchyRoot();

                if (root != null && root is ArchiveItemState item &&
                    _monitoredItems.TryGetValue(item.ArchiveItem.UniquePath, out var item2))
                {
                    item2.SubscribeCount--;

                    if (item2.SubscribeCount == 0)
                    {
                        _monitoredItems.Remove(item.ArchiveItem.UniquePath);
                    }

                    if (_monitoredItems.Count == 0 && _simulationTimer != null)
                    {
                        _simulationTimer.Dispose();
                        _simulationTimer = null;
                    }
                }
            }
        }

        /// <summary>
        /// Reads the raw data for an item.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="details"></param>
        /// <param name="timestampsToReturn"></param>
        /// <param name="nodesToRead"></param>
        /// <param name="results"></param>
        /// <param name="errors"></param>
        /// <param name="nodesToProcess"></param>
        /// <param name="cache"></param>
        protected override void HistoryReadRawModified(
            ServerSystemContext context,
            ReadRawModifiedDetails details,
            TimestampsToReturn timestampsToReturn,
            IList<HistoryReadValueId> nodesToRead,
            IList<HistoryReadResult> results,
            IList<ServiceResult> errors,
            List<NodeHandle> nodesToProcess,
            IDictionary<NodeId, NodeState> cache)
        {
            for (var ii = 0; ii < nodesToRead.Count; ii++)
            {
                var handle = nodesToProcess[ii];
                var nodeToRead = nodesToRead[handle.Index];
                var result = results[handle.Index];
                try
                {
                    // validate node.
                    var source = ValidateNode(context, handle, cache);

                    if (source == null)
                    {
                        continue;
                    }

                    HistoryReadRequest request;
                    // load an exising request.
                    if (nodeToRead.ContinuationPoint != null)
                    {
                        request = LoadContinuationPoint(context, nodeToRead.ContinuationPoint);

                        if (request == null)
                        {
                            errors[handle.Index] = StatusCodes.BadContinuationPointInvalid;
                            continue;
                        }
                    }

                    // create a new request.
                    else
                    {
                        request = CreateHistoryReadRequest(
                            context,
                            details,
                            handle,
                            nodeToRead);
                    }

                    // process values until the max is reached.
                    var data = details.IsReadModified ? new HistoryModifiedData() : new HistoryData();

                    while (request.NumValuesPerNode == 0 || data.DataValues.Count < request.NumValuesPerNode)
                    {
                        if (request.Values.Count == 0)
                        {
                            break;
                        }

                        var value = request.Values.First.Value;
                        request.Values.RemoveFirst();
                        data.DataValues.Add(value);

                        if (data is HistoryModifiedData modifiedData)
                        {
                            ModificationInfo modificationInfo = null;

                            if (request.ModificationInfos?.Count > 0)
                            {
                                modificationInfo = request.ModificationInfos.First.Value;
                                request.ModificationInfos.RemoveFirst();
                            }

                            modifiedData.ModificationInfos.Add(modificationInfo);
                        }
                    }

                    errors[handle.Index] = ServiceResult.Good;

                    // check if a continuation point is requred.
                    if (request.Values.Count > 0)
                    {
                        // only set if both end time and start time are specified.
                        if (details.StartTime != DateTime.MinValue && details.EndTime != DateTime.MinValue)
                        {
                            result.ContinuationPoint = SaveContinuationPoint(context, request);
                        }
                    }

                    // check if no data returned.
                    else
                    {
                        errors[handle.Index] = StatusCodes.GoodNoData;
                    }

                    // return the data.
                    result.HistoryData = new ExtensionObject(data);
                }
                catch (Exception e)
                {
                    errors[handle.Index] = ServiceResult.Create(e, StatusCodes.BadUnexpectedError, "Unexpected error processing request.");
                }
            }
        }

        /// <summary>
        /// Reads the processed data for an item.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="details"></param>
        /// <param name="timestampsToReturn"></param>
        /// <param name="nodesToRead"></param>
        /// <param name="results"></param>
        /// <param name="errors"></param>
        /// <param name="nodesToProcess"></param>
        /// <param name="cache"></param>
        protected override void HistoryReadProcessed(
            ServerSystemContext context,
            ReadProcessedDetails details,
            TimestampsToReturn timestampsToReturn,
            IList<HistoryReadValueId> nodesToRead,
            IList<HistoryReadResult> results,
            IList<ServiceResult> errors,
            List<NodeHandle> nodesToProcess,
            IDictionary<NodeId, NodeState> cache)
        {
            for (var ii = 0; ii < nodesToRead.Count; ii++)
            {
                var handle = nodesToProcess[ii];
                var nodeToRead = nodesToRead[handle.Index];
                var result = results[handle.Index];
                try
                {
                    // validate node.
                    var source = ValidateNode(context, handle, cache);

                    if (source == null)
                    {
                        continue;
                    }

                    HistoryReadRequest request;
                    // load an exising request.
                    if (nodeToRead.ContinuationPoint != null)
                    {
                        request = LoadContinuationPoint(context, nodeToRead.ContinuationPoint);

                        if (request == null)
                        {
                            errors[handle.Index] = StatusCodes.BadContinuationPointInvalid;
                            continue;
                        }
                    }

                    // create a new request.
                    else
                    {
                        // validate aggregate type.
                        if (details.AggregateType.Count <= ii || !Server.AggregateManager.IsSupported(details.AggregateType[ii]))
                        {
                            errors[handle.Index] = StatusCodes.BadAggregateNotSupported;
                            continue;
                        }

                        request = CreateHistoryReadRequest(
                            context,
                            details,
                            handle,
                            nodeToRead,
                            details.AggregateType[ii]);
                    }

                    // process values until the max is reached.
                    var data = new HistoryData();

                    while (request.NumValuesPerNode == 0 || data.DataValues.Count < request.NumValuesPerNode)
                    {
                        if (request.Values.Count == 0)
                        {
                            break;
                        }

                        var value = request.Values.First.Value;
                        request.Values.RemoveFirst();
                        data.DataValues.Add(value);
                    }

                    errors[handle.Index] = ServiceResult.Good;

                    // check if a continuation point is requred.
                    if (request.Values.Count > 0)
                    {
                        result.ContinuationPoint = SaveContinuationPoint(context, request);
                    }

                    // check if no data returned.
                    else
                    {
                        errors[handle.Index] = StatusCodes.GoodNoData;
                    }

                    // return the data.
                    result.HistoryData = new ExtensionObject(data);
                }
                catch (Exception e)
                {
                    errors[handle.Index] = ServiceResult.Create(e, StatusCodes.BadUnexpectedError, "Unexpected error processing request.");
                }
            }
        }

        /// <summary>
        /// Reads the data at the specified time for an item.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="details"></param>
        /// <param name="timestampsToReturn"></param>
        /// <param name="nodesToRead"></param>
        /// <param name="results"></param>
        /// <param name="errors"></param>
        /// <param name="nodesToProcess"></param>
        /// <param name="cache"></param>
        protected override void HistoryReadAtTime(
            ServerSystemContext context,
            ReadAtTimeDetails details,
            TimestampsToReturn timestampsToReturn,
            IList<HistoryReadValueId> nodesToRead,
            IList<HistoryReadResult> results,
            IList<ServiceResult> errors,
            List<NodeHandle> nodesToProcess,
            IDictionary<NodeId, NodeState> cache)
        {
            for (var ii = 0; ii < nodesToRead.Count; ii++)
            {
                var handle = nodesToProcess[ii];
                var nodeToRead = nodesToRead[handle.Index];
                var result = results[handle.Index];
                try
                {
                    // validate node.
                    var source = ValidateNode(context, handle, cache);

                    if (source == null)
                    {
                        continue;
                    }

                    HistoryReadRequest request;
                    // load an exising request.
                    if (nodeToRead.ContinuationPoint != null)
                    {
                        request = LoadContinuationPoint(context, nodeToRead.ContinuationPoint);

                        if (request == null)
                        {
                            errors[handle.Index] = StatusCodes.BadContinuationPointInvalid;
                            continue;
                        }
                    }

                    // create a new request.
                    else
                    {
                        request = CreateHistoryReadRequest(
                            context,
                            details,
                            handle,
                            nodeToRead);
                    }

                    // process values until the max is reached.
                    var data = new HistoryData();

                    while (request.NumValuesPerNode == 0 || data.DataValues.Count < request.NumValuesPerNode)
                    {
                        if (request.Values.Count == 0)
                        {
                            break;
                        }

                        var value = request.Values.First.Value;
                        request.Values.RemoveFirst();
                        data.DataValues.Add(value);
                    }

                    errors[handle.Index] = ServiceResult.Good;

                    // check if a continuation point is requred.
                    if (request.Values.Count > 0)
                    {
                        result.ContinuationPoint = SaveContinuationPoint(context, request);
                    }

                    // check if no data returned.
                    else
                    {
                        errors[handle.Index] = StatusCodes.GoodNoData;
                    }

                    // return the data.
                    result.HistoryData = new ExtensionObject(data);
                }
                catch (Exception e)
                {
                    errors[handle.Index] = ServiceResult.Create(e, StatusCodes.BadUnexpectedError, "Unexpected error processing request.");
                }
            }
        }

        /// <summary>
        /// Updates the data history for one or more nodes.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="nodesToUpdate"></param>
        /// <param name="results"></param>
        /// <param name="errors"></param>
        /// <param name="nodesToProcess"></param>
        /// <param name="cache"></param>
        protected override void HistoryUpdateData(
            ServerSystemContext context,
            IList<UpdateDataDetails> nodesToUpdate,
            IList<HistoryUpdateResult> results,
            IList<ServiceResult> errors,
            List<NodeHandle> nodesToProcess,
            IDictionary<NodeId, NodeState> cache)
        {
            for (var ii = 0; ii < nodesToProcess.Count; ii++)
            {
                var handle = nodesToProcess[ii];
                var nodeToUpdate = nodesToUpdate[handle.Index];
                var result = results[handle.Index];

                try
                {
                    // remove not supported.
                    if (nodeToUpdate.PerformInsertReplace == PerformUpdateType.Remove)
                    {
                        continue;
                    }

                    // validate node.
                    var source = ValidateNode(context, handle, cache);

                    if (source == null)
                    {
                        continue;
                    }

                    // load the archive.

                    if (handle.Node is not ArchiveItemState item)
                    {
                        continue;
                    }

                    item.ReloadFromSource(context);

                    // process each item.
                    for (var jj = 0; jj < nodeToUpdate.UpdateValues.Count; jj++)
                    {
                        StatusCode error = item.UpdateHistory(context, nodeToUpdate.UpdateValues[jj], nodeToUpdate.PerformInsertReplace);
                        result.OperationResults.Add(error);
                    }

                    errors[handle.Index] = ServiceResult.Good;
                }
                catch (Exception e)
                {
                    errors[handle.Index] = ServiceResult.Create(e, StatusCodes.BadUnexpectedError, "Unexpected error processing request.");
                }
            }
        }

        /// <summary>
        /// Updates the data history for one or more nodes.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="nodesToUpdate"></param>
        /// <param name="results"></param>
        /// <param name="errors"></param>
        /// <param name="nodesToProcess"></param>
        /// <param name="cache"></param>
        protected override void HistoryUpdateStructureData(
            ServerSystemContext context,
            IList<UpdateStructureDataDetails> nodesToUpdate,
            IList<HistoryUpdateResult> results,
            IList<ServiceResult> errors,
            List<NodeHandle> nodesToProcess,
            IDictionary<NodeId, NodeState> cache)
        {
            for (var ii = 0; ii < nodesToProcess.Count; ii++)
            {
                var handle = nodesToProcess[ii];
                var nodeToUpdate = nodesToUpdate[handle.Index];
                var result = results[handle.Index];

                try
                {
                    // validate node.
                    var source = ValidateNode(context, handle, cache);

                    if (source == null)
                    {
                        continue;
                    }

                    // only support annotations.
                    if (handle.Node.BrowseName != BrowseNames.Annotations)
                    {
                        continue;
                    }

                    // load the archive.
                    var item = Reload(context, handle);

                    if (item == null)
                    {
                        continue;
                    }

                    // process each item.
                    for (var jj = 0; jj < nodeToUpdate.UpdateValues.Count; jj++)
                    {
                        if (ExtensionObject.ToEncodeable(nodeToUpdate.UpdateValues[jj].Value as ExtensionObject) is not Annotation annotation)
                        {
                            result.OperationResults.Add(StatusCodes.BadTypeMismatch);
                            continue;
                        }

                        StatusCode error = item.UpdateAnnotations(
                            context,
                            annotation,
                            nodeToUpdate.UpdateValues[jj],
                            nodeToUpdate.PerformInsertReplace);

                        result.OperationResults.Add(error);
                    }

                    errors[handle.Index] = ServiceResult.Good;
                }
                catch (Exception e)
                {
                    errors[handle.Index] = ServiceResult.Create(e, StatusCodes.BadUnexpectedError, "Unexpected error processing request.");
                }
            }
        }

        /// <summary>
        /// Deletes the data history for one or more nodes.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="nodesToUpdate"></param>
        /// <param name="results"></param>
        /// <param name="errors"></param>
        /// <param name="nodesToProcess"></param>
        /// <param name="cache"></param>
        protected override void HistoryDeleteRawModified(
            ServerSystemContext context,
            IList<DeleteRawModifiedDetails> nodesToUpdate,
            IList<HistoryUpdateResult> results,
            IList<ServiceResult> errors,
            List<NodeHandle> nodesToProcess,
            IDictionary<NodeId, NodeState> cache)
        {
            for (var ii = 0; ii < nodesToProcess.Count; ii++)
            {
                var handle = nodesToProcess[ii];
                var nodeToUpdate = nodesToUpdate[handle.Index];

                try
                {
                    // validate node.
                    var source = ValidateNode(context, handle, cache);

                    if (source == null)
                    {
                        continue;
                    }

                    // load the archive.

                    if (handle.Node is not ArchiveItemState item)
                    {
                        continue;
                    }

                    item.ReloadFromSource(context);

                    // delete the history.
                    item.DeleteHistory(context, nodeToUpdate.StartTime, nodeToUpdate.EndTime, nodeToUpdate.IsDeleteModified);
                    errors[handle.Index] = ServiceResult.Good;
                }
                catch (Exception e)
                {
                    errors[handle.Index] = ServiceResult.Create(e, StatusCodes.BadUnexpectedError, "Error deleting data from archive.");
                }
            }
        }

        /// <summary>
        /// Deletes the data history for one or more nodes.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="nodesToUpdate"></param>
        /// <param name="results"></param>
        /// <param name="errors"></param>
        /// <param name="nodesToProcess"></param>
        /// <param name="cache"></param>
        protected override void HistoryDeleteAtTime(
            ServerSystemContext context,
            IList<DeleteAtTimeDetails> nodesToUpdate,
            IList<HistoryUpdateResult> results,
            IList<ServiceResult> errors,
            List<NodeHandle> nodesToProcess,
            IDictionary<NodeId, NodeState> cache)
        {
            for (var ii = 0; ii < nodesToProcess.Count; ii++)
            {
                var handle = nodesToProcess[ii];
                var nodeToUpdate = nodesToUpdate[handle.Index];
                var result = results[handle.Index];

                try
                {
                    // validate node.
                    var source = ValidateNode(context, handle, cache);

                    if (source == null)
                    {
                        continue;
                    }

                    // load the archive.

                    if (handle.Node is not ArchiveItemState item)
                    {
                        continue;
                    }

                    item.ReloadFromSource(context);

                    // process each item.
                    for (var jj = 0; jj < nodeToUpdate.ReqTimes.Count; jj++)
                    {
                        StatusCode error = item.DeleteHistory(context, nodeToUpdate.ReqTimes[jj]);
                        result.OperationResults.Add(error);
                    }

                    errors[handle.Index] = ServiceResult.Good;
                }
                catch (Exception e)
                {
                    errors[handle.Index] = ServiceResult.Create(e, StatusCodes.BadUnexpectedError, "Unexpected error processing request.");
                }
            }
        }

        /// <summary>
        /// Loads the archive item state from the underlying source.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="handle"></param>
        private ArchiveItemState Reload(ServerSystemContext context, NodeHandle handle)
        {
            var item = handle.Node as ArchiveItemState;

            if (item == null && handle.Node is BaseInstanceState property)
            {
                item = property.Parent as ArchiveItemState;
            }

            item?.ReloadFromSource(context);

            return item;
        }

        /// <summary>
        /// Creates a new history request.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="details"></param>
        /// <param name="handle"></param>
        /// <param name="nodeToRead"></param>
        /// <exception cref="ServiceResultException"></exception>
        private HistoryReadRequest CreateHistoryReadRequest(
            ServerSystemContext context,
            ReadRawModifiedDetails details,
            NodeHandle handle,
            HistoryReadValueId nodeToRead)
        {
            var sizeLimited = details.StartTime == DateTime.MinValue || details.EndTime == DateTime.MinValue;
            var applyIndexRangeOrEncoding = nodeToRead.ParsedIndexRange != NumericRange.Empty || !QualifiedName.IsNull(nodeToRead.DataEncoding);
            var returnBounds = !details.IsReadModified && details.ReturnBounds;
            var timeFlowsBackward = (details.StartTime == DateTime.MinValue) || (details.EndTime != DateTime.MinValue && details.EndTime < details.StartTime);

            // find the archive item.
            var item = Reload(context, handle);

            if (item == null)
            {
                throw new ServiceResultException(StatusCodes.BadNotSupported);
            }

            var values = new LinkedList<DataValue>();
            LinkedList<ModificationInfo> modificationInfos = null;

            if (details.IsReadModified)
            {
                modificationInfos = new LinkedList<ModificationInfo>();
            }

            // read history.
            var view = item.ReadHistory(details.StartTime, details.EndTime, details.IsReadModified, handle.Node.BrowseName);

            var startBound = -1;
            var endBound = -1;
            var ii = timeFlowsBackward ? view.Count - 1 : 0;

            while (ii >= 0 && ii < view.Count)
            {
                try
                {
                    var timestamp = (DateTime)view[ii].Row[0];

                    // check if looking for start of data.
                    if (values.Count == 0)
                    {
                        if (timeFlowsBackward)
                        {
                            if ((details.StartTime != DateTime.MinValue && timestamp >= details.StartTime) || (details.StartTime == DateTime.MinValue && timestamp >= details.EndTime))
                            {
                                startBound = ii;

                                if (timestamp > details.StartTime)
                                {
                                    continue;
                                }
                            }
                        }
                        else
                        {
                            if (timestamp <= details.StartTime)
                            {
                                startBound = ii;

                                if (timestamp < details.StartTime)
                                {
                                    continue;
                                }
                            }
                        }
                    }

                    // check if absolute max values specified.
                    if (sizeLimited && details.NumValuesPerNode > 0 && details.NumValuesPerNode < values.Count)
                    {
                        break;
                    }

                    // check for end bound.
                    if (details.EndTime != DateTime.MinValue && timestamp >= details.EndTime)
                    {
                        if (timeFlowsBackward)
                        {
                            if (timestamp <= details.EndTime)
                            {
                                endBound = ii;
                                break;
                            }
                        }
                        else
                        {
                            if (timestamp >= details.EndTime)
                            {
                                endBound = ii;
                                break;
                            }
                        }
                    }

                    // check if the start bound needs to be returned.
                    if (returnBounds && values.Count == 0 && startBound != ii && details.StartTime != DateTime.MinValue)
                    {
                        // add start bound.
                        if (startBound == -1)
                        {
                            values.AddLast(new DataValue(Variant.Null, StatusCodes.BadBoundNotFound, details.StartTime, details.StartTime));
                        }
                        else
                        {
                            values.AddLast(RowToDataValue(context, nodeToRead, view[startBound], applyIndexRangeOrEncoding));
                        }

                        // check if absolute max values specified.
                        if (sizeLimited && details.NumValuesPerNode > 0 && details.NumValuesPerNode < values.Count)
                        {
                            break;
                        }
                    }

                    // add value.
                    values.AddLast(RowToDataValue(context, nodeToRead, view[ii], applyIndexRangeOrEncoding));

                    modificationInfos?.AddLast((ModificationInfo)view[ii].Row[6]);
                }
                finally
                {
                    if (timeFlowsBackward)
                    {
                        ii--;
                    }
                    else
                    {
                        ii++;
                    }
                }
            }

            // add late bound.
            while (returnBounds && details.EndTime != DateTime.MinValue)
            {
                // add start bound.
                if (values.Count == 0)
                {
                    if (startBound == -1)
                    {
                        values.AddLast(new DataValue(Variant.Null, StatusCodes.BadBoundNotFound, details.StartTime, details.StartTime));
                    }
                    else
                    {
                        values.AddLast(RowToDataValue(context, nodeToRead, view[startBound], applyIndexRangeOrEncoding));
                    }
                }

                // check if absolute max values specified.
                if (sizeLimited && details.NumValuesPerNode > 0 && details.NumValuesPerNode < values.Count)
                {
                    break;
                }

                // add end bound.
                if (endBound == -1)
                {
                    values.AddLast(new DataValue(Variant.Null, StatusCodes.BadBoundNotFound, details.EndTime, details.EndTime));
                }
                else
                {
                    values.AddLast(RowToDataValue(context, nodeToRead, view[endBound], applyIndexRangeOrEncoding));
                }

                break;
            }

            return new HistoryReadRequest
            {
                Values = values,
                ModificationInfos = modificationInfos,
                NumValuesPerNode = details.NumValuesPerNode,
                Filter = null
            };
        }

        /// <summary>
        /// Creates a new history request.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="details"></param>
        /// <param name="handle"></param>
        /// <param name="nodeToRead"></param>
        /// <param name="aggregateId"></param>
        /// <exception cref="ServiceResultException"></exception>
        private HistoryReadRequest CreateHistoryReadRequest(
            ServerSystemContext context,
            ReadProcessedDetails details,
            NodeHandle handle,
            HistoryReadValueId nodeToRead,
            NodeId aggregateId)
        {
            var applyIndexRangeOrEncoding = nodeToRead.ParsedIndexRange != NumericRange.Empty || !QualifiedName.IsNull(nodeToRead.DataEncoding);
            var timeFlowsBackward = details.EndTime < details.StartTime;

            if (handle.Node is not ArchiveItemState item)
            {
                throw new ServiceResultException(StatusCodes.BadNotSupported);
            }

            item.ReloadFromSource(context);

            var values = new LinkedList<DataValue>();

            // read history.
            var view = item.ReadHistory(details.StartTime, details.EndTime, false);

            var ii = timeFlowsBackward ? view.Count - 1 : 0;

            // choose the aggregate configuration.
            var configuration = (AggregateConfiguration)details.AggregateConfiguration.MemberwiseClone();
            ReviseAggregateConfiguration(context, item, configuration);

            // create the aggregate calculator.
            var calculator = Server.AggregateManager.CreateCalculator(
                aggregateId,
                details.StartTime,
                details.EndTime,
                details.ProcessingInterval,
                item.ArchiveItem.Stepped,
                configuration);

            while (ii >= 0 && ii < view.Count)
            {
                try
                {
                    var value = (DataValue)view[ii].Row[2];
                    calculator.QueueRawValue(value);

                    // queue any processed values.
                    QueueProcessedValues(
                        context,
                        calculator,
                        nodeToRead.ParsedIndexRange,
                        nodeToRead.DataEncoding,
                        applyIndexRangeOrEncoding,
                        false,
                        values);
                }
                finally
                {
                    if (timeFlowsBackward)
                    {
                        ii--;
                    }
                    else
                    {
                        ii++;
                    }
                }
            }

            // queue any processed values beyond the end of the data.
            QueueProcessedValues(
                context,
                calculator,
                nodeToRead.ParsedIndexRange,
                nodeToRead.DataEncoding,
                applyIndexRangeOrEncoding,
                true,
                values);

            return new HistoryReadRequest
            {
                Values = values,
                NumValuesPerNode = 0,
                Filter = null
            };
        }

        /// <summary>
        /// Creates a new history request.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="details"></param>
        /// <param name="handle"></param>
        /// <param name="nodeToRead"></param>
        /// <exception cref="ServiceResultException"></exception>
        private static HistoryReadRequest CreateHistoryReadRequest(
            ServerSystemContext context,
            ReadAtTimeDetails details,
            NodeHandle handle,
            HistoryReadValueId nodeToRead)
        {
            System.Diagnostics.Contracts.Contract.Assume(nodeToRead is not null);

            if (handle.Node is not ArchiveItemState item)
            {
                throw new ServiceResultException(StatusCodes.BadNotSupported);
            }

            item.ReloadFromSource(context);

            // find the start and end times.
            var startTime = DateTime.MaxValue;
            var endTime = DateTime.MinValue;

            for (var ii = 0; ii < details.ReqTimes.Count; ii++)
            {
                if (startTime > details.ReqTimes[ii])
                {
                    startTime = details.ReqTimes[ii];
                }

                if (endTime < details.ReqTimes[ii])
                {
                    endTime = details.ReqTimes[ii];
                }
            }

            var view = item.ReadHistory(startTime, endTime, false);

            var values = new LinkedList<DataValue>();

            for (var ii = 0; ii < details.ReqTimes.Count; ii++)
            {
                // find the value at the time.
                var index = item.FindValueAtOrBefore(view, details.ReqTimes[ii], !details.UseSimpleBounds, out var dataBeforeIgnored);

                if (index < 0)
                {
                    values.AddLast(new DataValue(StatusCodes.BadNoData, details.ReqTimes[ii]));
                    continue;
                }

                // nothing more to do if a raw value exists.
                if ((DateTime)view[index].Row[0] == details.ReqTimes[ii])
                {
                    values.AddLast((DataValue)view[index].Row[2]);
                    continue;
                }

                var before = (DataValue)view[index].Row[2];
                DataValue value;

                // find the value after the time.
                var afterIndex = item.FindValueAfter(view, index, !details.UseSimpleBounds, out var dataAfterIgnored);

                if (afterIndex < 0)
                {
                    // use stepped interpolation if no end bound exists.
                    value = AggregateCalculator.SteppedInterpolate(details.ReqTimes[ii], before);

                    if (StatusCode.IsNotBad(value.StatusCode) && dataBeforeIgnored)
                    {
                        value.StatusCode = value.StatusCode.SetCodeBits(StatusCodes.UncertainDataSubNormal);
                    }

                    values.AddLast(value);
                    continue;
                }

                // use stepped or slopped interpolation depending on the value.
                if (item.ArchiveItem.Stepped)
                {
                    value = AggregateCalculator.SteppedInterpolate(details.ReqTimes[ii], before);

                    if (StatusCode.IsNotBad(value.StatusCode) && dataBeforeIgnored)
                    {
                        value.StatusCode = value.StatusCode.SetCodeBits(StatusCodes.UncertainDataSubNormal);
                    }
                }
                else
                {
                    value = AggregateCalculator.SlopedInterpolate(details.ReqTimes[ii], before, (DataValue)view[afterIndex].Row[2]);

                    if (StatusCode.IsNotBad(value.StatusCode) && (dataBeforeIgnored || dataAfterIgnored))
                    {
                        value.StatusCode = value.StatusCode.SetCodeBits(StatusCodes.UncertainDataSubNormal);
                    }
                }

                values.AddLast(value);
            }

            return new HistoryReadRequest
            {
                Values = values,
                NumValuesPerNode = 0,
                Filter = null
            };
        }

        /// <summary>
        /// Extracts and queues any processed values.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="calculator"></param>
        /// <param name="indexRange"></param>
        /// <param name="dataEncoding"></param>
        /// <param name="applyIndexRangeOrEncoding"></param>
        /// <param name="returnPartial"></param>
        /// <param name="values"></param>
        private void QueueProcessedValues(
            ServerSystemContext context,
            IAggregateCalculator calculator,
            NumericRange indexRange,
            QualifiedName dataEncoding,
            bool applyIndexRangeOrEncoding,
            bool returnPartial,
            LinkedList<DataValue> values)
        {
            var proccessedValue = calculator.GetProcessedValue(returnPartial);

            while (proccessedValue != null)
            {
                // apply any index range or encoding.
                if (applyIndexRangeOrEncoding)
                {
                    var rawValue = proccessedValue.Value;
                    var result = BaseVariableState.ApplyIndexRangeAndDataEncoding(context, indexRange, dataEncoding, ref rawValue);

                    if (ServiceResult.IsBad(result))
                    {
                        proccessedValue.Value = rawValue;
                    }
                    else
                    {
                        proccessedValue.Value = null;
                        proccessedValue.StatusCode = result.StatusCode;
                    }
                }

                // queue the result.
                values.AddLast(proccessedValue);
                proccessedValue = calculator.GetProcessedValue(returnPartial);
            }
        }

        /// <summary>
        /// Creates a new history request.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="nodeToRead"></param>
        /// <param name="row"></param>
        /// <param name="applyIndexRangeOrEncoding"></param>
        private DataValue RowToDataValue(
            ServerSystemContext context,
            HistoryReadValueId nodeToRead,
            DataRowView row,
            bool applyIndexRangeOrEncoding)
        {
            var value = (DataValue)row[2];

            // apply any index range or encoding.
            if (applyIndexRangeOrEncoding)
            {
                var rawValue = value.Value;
                var result = BaseVariableState.ApplyIndexRangeAndDataEncoding(context, nodeToRead.ParsedIndexRange, nodeToRead.DataEncoding, ref rawValue);

                if (ServiceResult.IsBad(result))
                {
                    value.Value = rawValue;
                }
                else
                {
                    value.Value = null;
                    value.StatusCode = result.StatusCode;
                }
            }

            return value;
        }

        /// <summary>
        /// Stores a read history request.
        /// </summary>
        private sealed class HistoryReadRequest
        {
            public byte[] ContinuationPoint { get; set; }
            public LinkedList<DataValue> Values { get; set; }
            public LinkedList<ModificationInfo> ModificationInfos { get; set; }
            public uint NumValuesPerNode { get; set; }
            public AggregateFilter Filter { get; set; }
        }

        /// <summary>
        /// Releases the history continuation point.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="nodesToRead"></param>
        /// <param name="errors"></param>
        /// <param name="nodesToProcess"></param>
        /// <param name="cache"></param>
        protected override void HistoryReleaseContinuationPoints(
            ServerSystemContext context,
            IList<HistoryReadValueId> nodesToRead,
            IList<ServiceResult> errors,
            List<NodeHandle> nodesToProcess,
            IDictionary<NodeId, NodeState> cache)
        {
            for (var ii = 0; ii < nodesToProcess.Count; ii++)
            {
                var handle = nodesToProcess[ii];
                var nodeToRead = nodesToRead[handle.Index];

                // find the continuation point.
                var request = LoadContinuationPoint(context, nodeToRead.ContinuationPoint);

                if (request == null)
                {
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
        /// <param name="context"></param>
        /// <param name="continuationPoint"></param>
        private static HistoryReadRequest LoadContinuationPoint(
            ServerSystemContext context,
            byte[] continuationPoint)
        {
            var session = context.OperationContext.Session;

            if (session == null)
            {
                return null;
            }

            if (session.RestoreHistoryContinuationPoint(continuationPoint)
                is not HistoryReadRequest request)
            {
                return null;
            }

            return request;
        }

        /// <summary>
        /// Saves a history continuation point.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="request"></param>
        private static byte[] SaveContinuationPoint(
            ServerSystemContext context,
            HistoryReadRequest request)
        {
            var session = context.OperationContext.Session;

            if (session == null)
            {
                return null;
            }

            var id = Guid.NewGuid();
            session.SaveHistoryContinuationPoint(id, request);
            request.ContinuationPoint = id.ToByteArray();
            return request.ContinuationPoint;
        }

        /// <summary>
        /// Runs the simulation.
        /// </summary>
        /// <param name="state"></param>
        private void DoSimulation(object state)
        {
            try
            {
                lock (Lock)
                {
                    foreach (var item in _monitoredItems.Values)
                    {
                        if (item.ArchiveItem.LastLoadTime.AddDays(1) < _timeService.UtcNow)
                        {
                            item.LoadConfiguration(SystemContext);
                        }

                        foreach (var value in item.NewSamples(SystemContext))
                        {
                            item.WrappedValue = value.WrappedValue;
                            item.Timestamp = value.SourceTimestamp;
                            item.StatusCode = value.StatusCode;
                            item.ClearChangeMasks(SystemContext, true);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Utils.Trace("Unexpected error during simulation: {0}", e.Message);
            }
        }

        private readonly UnderlyingSystem _system;
        private readonly HistoricalAccessServerConfiguration _configuration;
        private readonly TimeService _timeService;
        private Timer _simulationTimer;
        private Dictionary<string, ArchiveItemState> _monitoredItems;
    }
}
