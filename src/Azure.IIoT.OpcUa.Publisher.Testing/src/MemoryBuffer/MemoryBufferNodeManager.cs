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

namespace MemoryBuffer
{
    using Opc.Ua;
    using Opc.Ua.Sample;
    using Opc.Ua.Server;
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    /// <summary>
    /// A node manager for a variety of test data.
    /// </summary>
    public class MemoryBufferNodeManager : SampleNodeManager
    {
        /// <summary>
        /// Initializes the node manager.
        /// </summary>
        /// <param name="server"></param>
        /// <param name="configuration"></param>
        public MemoryBufferNodeManager(IServerInternal server,
            ApplicationConfiguration configuration) :
            base(server)
        {
            NamespaceUris = new List<string> {
                Namespaces.MemoryBuffer,
                Namespaces.MemoryBuffer + "/Instance"
            };

            // get the configuration for the node manager.
            // use suitable defaults if no configuration exists.
            _configuration = configuration.ParseExtension<MemoryBufferConfiguration>()
                ?? new MemoryBufferConfiguration();

            _buffers = [];
        }

        /// <summary>
        /// Dispose
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing && _buffers != null)
            {
                foreach (var buffer in _buffers.Values)
                {
                    buffer.Dispose();
                }
                _buffers.Clear();
                _buffers = null;
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
        public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences)
        {
            lock (Lock)
            {
                base.CreateAddressSpace(externalReferences);

                // create the nodes from configuration.
                var namespaceIndex = Server.NamespaceUris.GetIndexOrAppend(Namespaces.MemoryBuffer);

                var root = (BaseInstanceState)FindPredefinedNode(
                    new NodeId(Objects.MemoryBuffers, namespaceIndex),
                    typeof(BaseInstanceState));

                // create the nodes from configuration.
                namespaceIndex = Server.NamespaceUris.GetIndexOrAppend(Namespaces.MemoryBuffer + "/Instance");

                if (_configuration?.Buffers != null)
                {
                    for (var ii = 0; ii < _configuration.Buffers.Count; ii++)
                    {
                        var instance = _configuration.Buffers[ii];

                        // create a new buffer.
                        var bufferNode = new MemoryBufferState(SystemContext, instance);

                        // assign node ids.
                        bufferNode.Create(
                            SystemContext,
                            new NodeId(bufferNode.SymbolicName, namespaceIndex),
                            new QualifiedName(bufferNode.SymbolicName, namespaceIndex),
                            null,
                            true);

                        bufferNode.CreateBuffer(instance.DataType, instance.TagCount);
                        bufferNode.InitializeMonitoring(Server, this);

                        // save the buffers for easy look up later.
                        _buffers[bufferNode.SymbolicName] = bufferNode;

                        // link to root.
                        root.AddChild(bufferNode);
                    }
                }
            }
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
        /// <remarks>
        /// This must efficiently determine whether the node belongs to the node manager. If it does belong to
        /// NodeManager it should return a handle that does not require the NodeId to be validated again when
        /// the handle is passed into other methods such as 'Read' or 'Write'.
        /// </remarks>
        protected override object GetManagerHandle(ISystemContext context, NodeId nodeId, IDictionary<NodeId, NodeState> cache)
        {
            lock (Lock)
            {
                if (!IsNodeIdInNamespace(nodeId))
                {
                    return null;
                }

                if (nodeId.Identifier is string id)
                {
                    // check for a reference to the buffer.

                    if (_buffers.TryGetValue(id, out var buffer))
                    {
                        return buffer;
                    }

                    // tag ids have the syntax <bufferName>[<address>]
                    if (id[^1] != ']')
                    {
                        return null;
                    }

                    var index = id.IndexOf('[');

                    if (index == -1)
                    {
                        return null;
                    }

                    var bufferName = id[..index];

                    // verify the buffer.
                    if (!_buffers.TryGetValue(bufferName, out buffer))
                    {
                        return null;
                    }

                    // validate the address.
                    var offsetText = id.Substring(index + 1, id.Length - index - 2);

                    for (var ii = 0; ii < offsetText.Length; ii++)
                    {
                        if (!char.IsDigit(offsetText[ii]))
                        {
                            return null;
                        }
                    }

                    // check range on offset.
                    var offset = Convert.ToUInt32(offsetText);

                    if (offset >= buffer.SizeInBytes.Value)
                    {
                        return null;
                    }

                    // the tags contain all of the metadata required to support the UA
                    // operations and pointers to functions in the buffer object that
                    // allow the value to be accessed. These tags are ephemeral and are
                    // discarded after the operation completes. This design pattern allows
                    // the server to expose potentially millions of UA nodes without
                    // creating millions of objects that reside in memory.
                    return new MemoryTagState(buffer, offset);
                }

                return base.GetManagerHandle(context, nodeId, cache);
            }
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
        /// <param name="globalIdCounter"></param>
        /// <param name="filterError"></param>
        /// <param name="monitoredItem"></param>
        /// <remarks>
        /// This method only handles data change subscriptions. Event subscriptions are created by the SDK.
        /// </remarks>
        protected override ServiceResult CreateMonitoredItem(
            ISystemContext context,
            NodeState source,
            uint subscriptionId,
            double publishingInterval,
            DiagnosticsMasks diagnosticsMasks,
            TimestampsToReturn timestampsToReturn,
            MonitoredItemCreateRequest itemToCreate,
            ref long globalIdCounter,
            out MonitoringFilterResult filterError,
            out IMonitoredItem monitoredItem)
        {
            filterError = null;
            monitoredItem = null;

            // use default behavoir for non-tag sources.
            if (source is not MemoryTagState tag)
            {
                return base.CreateMonitoredItem(
                    context,
                    source,
                    subscriptionId,
                    publishingInterval,
                    diagnosticsMasks,
                    timestampsToReturn,
                    itemToCreate,
                    ref globalIdCounter,
                    out filterError,
                    out monitoredItem);
            }

            // validate parameters.
            var parameters = itemToCreate.RequestedParameters;

            // no filters supported at this time.
            var filter = (MonitoringFilter)ExtensionObject.ToEncodeable(parameters.Filter);

            if (filter != null)
            {
                return StatusCodes.BadFilterNotAllowed;
            }

            // index range not supported.
            if (itemToCreate.ItemToMonitor.ParsedIndexRange != NumericRange.Empty)
            {
                return StatusCodes.BadIndexRangeInvalid;
            }

            // data encoding not supported.
            if (!QualifiedName.IsNull(itemToCreate.ItemToMonitor.DataEncoding))
            {
                return StatusCodes.BadDataEncodingInvalid;
            }

            // read initial value.
            var initialValue = new DataValue
            {
                Value = null,
                ServerTimestamp = DateTime.UtcNow,
                SourceTimestamp = DateTime.MinValue,
                StatusCode = StatusCodes.Good
            };

            var error = source.ReadAttribute(
                context,
                itemToCreate.ItemToMonitor.AttributeId,
                itemToCreate.ItemToMonitor.ParsedIndexRange,
                itemToCreate.ItemToMonitor.DataEncoding,
                initialValue);

            if (ServiceResult.IsBad(error))
            {
                return error;
            }

            // get the monitored node for the containing buffer.

            if (tag.Parent is not MemoryBufferState buffer)
            {
                return StatusCodes.BadInternalError;
            }

            // create a globally unique identifier.
            var monitoredItemId = Utils.IncrementIdentifier(ref globalIdCounter);

            // determine the sampling interval.
            var samplingInterval = itemToCreate.RequestedParameters.SamplingInterval;

            if (samplingInterval < 0)
            {
                samplingInterval = publishingInterval;
            }

            // create the item.
            var datachangeItem = buffer.CreateDataChangeItem(
                tag,
                monitoredItemId,
                itemToCreate.ItemToMonitor,
                diagnosticsMasks,
                timestampsToReturn,
                itemToCreate.MonitoringMode,
                itemToCreate.RequestedParameters.ClientHandle,
                samplingInterval);

            /*
            // create the item.
            MemoryBufferMonitoredItem datachangeItem = buffer.CreateDataChangeItem(
                context,
                tag,
                monitoredItemId,
                itemToCreate.ItemToMonitor.AttributeId,
                diagnosticsMasks,
                timestampsToReturn,
                itemToCreate.MonitoringMode,
                itemToCreate.RequestedParameters.ClientHandle,
                samplingInterval);
            */

            // report the initial value.
            datachangeItem.QueueValue(initialValue, null);

            // update monitored item list.
            monitoredItem = datachangeItem;

            return ServiceResult.Good;
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
        protected override ServiceResult ModifyMonitoredItem(
            ISystemContext context,
            DiagnosticsMasks diagnosticsMasks,
            TimestampsToReturn timestampsToReturn,
            IMonitoredItem monitoredItem,
            MonitoredItemModifyRequest itemToModify,
            out MonitoringFilterResult filterError)
        {
            filterError = null;

            // check for valid handle.

            if (monitoredItem.ManagerHandle is not MemoryBufferState)
            {
                return base.ModifyMonitoredItem(
                    context,
                    diagnosticsMasks,
                    timestampsToReturn,
                    monitoredItem,
                    itemToModify,
                    out filterError);
            }

            // owned by this node manager.
            itemToModify.Processed = true;

            // get the monitored item.

            if (monitoredItem is not MemoryBufferMonitoredItem datachangeItem)
            {
                return StatusCodes.BadMonitoredItemIdInvalid;
            }

            // validate parameters.
            var parameters = itemToModify.RequestedParameters;

            // no filters supported at this time.
            var filter = (MonitoringFilter)ExtensionObject.ToEncodeable(parameters.Filter);

            if (filter != null)
            {
                return StatusCodes.BadFilterNotAllowed;
            }

            // modify the monitored item parameters.
            _ = datachangeItem.Modify(
                diagnosticsMasks,
                timestampsToReturn,
                itemToModify.RequestedParameters.ClientHandle,
                itemToModify.RequestedParameters.SamplingInterval);

            return ServiceResult.Good;
        }

        /// <summary>
        /// Deletes a monitored item.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="monitoredItem"></param>
        /// <param name="processed"></param>
        protected override ServiceResult DeleteMonitoredItem(
            ISystemContext context,
            IMonitoredItem monitoredItem,
            out bool processed)
        {
            // check for valid handle.
            if (monitoredItem.ManagerHandle is not MemoryBufferState buffer)
            {
                return base.DeleteMonitoredItem(
                    context,
                    monitoredItem,
                    out processed);
            }

            // owned by this node manager.
            processed = true;

            // get the monitored item.

            if (monitoredItem is not MemoryBufferMonitoredItem datachangeItem)
            {
                return StatusCodes.BadMonitoredItemIdInvalid;
            }

            // delete the item.
            buffer.DeleteItem(datachangeItem);

            return ServiceResult.Good;
        }

        /// <summary>
        /// Changes the monitoring mode for an item.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="monitoredItem"></param>
        /// <param name="monitoringMode"></param>
        /// <param name="processed"></param>
        protected override ServiceResult SetMonitoringMode(
            ISystemContext context,
            IMonitoredItem monitoredItem,
            MonitoringMode monitoringMode,
            out bool processed)
        {
            // check for valid handle.

            if (monitoredItem.ManagerHandle is not MemoryBufferState buffer)
            {
                return base.SetMonitoringMode(
                    context,
                    monitoredItem,
                    monitoringMode,
                    out processed);
            }

            // owned by this node manager.
            processed = true;

            // get the monitored item.

            if (monitoredItem is not MemoryBufferMonitoredItem datachangeItem)
            {
                return StatusCodes.BadMonitoredItemIdInvalid;
            }

            // delete the item.
            var previousMode = datachangeItem.SetMonitoringMode(monitoringMode);

            // need to provide an immediate update after enabling.
            if (previousMode == MonitoringMode.Disabled && monitoringMode != MonitoringMode.Disabled)
            {
                var initialValue = new DataValue
                {
                    Value = null,
                    ServerTimestamp = DateTime.UtcNow,
                    SourceTimestamp = DateTime.MinValue,
                    StatusCode = StatusCodes.Good
                };

                var tag = new MemoryTagState(buffer, datachangeItem.Offset);

                var error = tag.ReadAttribute(
                    context,
                    datachangeItem.AttributeId,
                    NumericRange.Empty,
                    null,
                    initialValue);

                datachangeItem.QueueValue(initialValue, error);
            }

            return ServiceResult.Good;
        }

        private readonly MemoryBufferConfiguration _configuration;
        private Dictionary<string, MemoryBufferState> _buffers;
    }
}
