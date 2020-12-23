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

namespace Opc.Ua.Sample {
    using System.Collections.Generic;
    using Opc.Ua.Server;

    /// <summary>
    /// Keeps track of the monitored items for a single node.
    /// </summary>
    public class MonitoredNode {

        /// <summary>
        /// Initializes the instance with the context for the node being monitored.
        /// </summary>
        public MonitoredNode(
            IServerInternal server,
            INodeManager nodeManager,
            NodeState node) {
            Server = server;
            NodeManager = nodeManager;
            Node = node;
        }

        /// <summary>
        /// The server that the node belongs to.
        /// </summary>
        public IServerInternal Server { get; }

        /// <summary>
        /// The node manager that the node belongs to.
        /// </summary>
        public INodeManager NodeManager { get; }

        /// <summary>
        /// The node being monitored.
        /// </summary>
        public NodeState Node { get; }

        /// <summary>
        /// Whether the node has any active monitored items for the specified attribute.
        /// </summary>
        public bool IsMonitoringRequired(uint attributeId) {
            if (_monitoredItems != null) {
                for (var ii = 0; ii < _monitoredItems.Count; ii++) {
                    var monitoredItem = _monitoredItems[ii];

                    if (monitoredItem.AttributeId == attributeId && monitoredItem.MonitoringMode != MonitoringMode.Disabled) {
                        return true;
                    }
                }
            }

            return false;
        }



        /// <summary>
        /// Creates a new data change monitored item.
        /// </summary>
        /// <param name="context">The system context.</param>
        /// <param name="monitoredItemId">The unique identifier for the monitiored item.</param>
        /// <param name="attributeId">The attribute to monitor.</param>
        /// <param name="indexRange">The index range to use for array values.</param>
        /// <param name="dataEncoding">The data encoding to return for structured values.</param>
        /// <param name="diagnosticsMasks">The diagnostics masks to use.</param>
        /// <param name="timestampsToReturn">The timestamps to return.</param>
        /// <param name="monitoringMode">The initial monitoring mode.</param>
        /// <param name="clientHandle">The handle assigned by the client.</param>
        /// <param name="samplingInterval">The sampling interval.</param>
        /// <param name="queueSize">The queue size.</param>
        /// <param name="discardOldest">Whether to discard the oldest values when the queue overflows.</param>
        /// <param name="filter">The data change filter to use.</param>
        /// <param name="range">The range to use when evaluating a percentage deadband filter.</param>
        /// <param name="alwaysReportUpdates">Whether the monitored item should skip the check for a change in value.</param>
        /// <returns>The new monitored item.</returns>
        public DataChangeMonitoredItem CreateDataChangeItem(
            ISystemContext context,
            uint monitoredItemId,
            uint attributeId,
            NumericRange indexRange,
            QualifiedName dataEncoding,
            DiagnosticsMasks diagnosticsMasks,
            TimestampsToReturn timestampsToReturn,
            MonitoringMode monitoringMode,
            uint clientHandle,
            double samplingInterval,
            uint queueSize,
            bool discardOldest,
            DataChangeFilter filter,
            Range range,
            bool alwaysReportUpdates) {
            System.Diagnostics.Contracts.Contract.Assume(context != null);
            var monitoredItem = new DataChangeMonitoredItem(
                this,
                monitoredItemId,
                attributeId,
                indexRange,
                dataEncoding,
                diagnosticsMasks,
                timestampsToReturn,
                monitoringMode,
                clientHandle,
                samplingInterval,
                queueSize,
                discardOldest,
                filter,
                range,
                alwaysReportUpdates);

            if (_monitoredItems == null) {
                _monitoredItems = new List<DataChangeMonitoredItem>();
                Node.OnStateChanged = OnNodeChange;
            }

            _monitoredItems.Add(monitoredItem);

            return monitoredItem;
        }

        /// <summary>
        /// Creates a new data change monitored item.
        /// </summary>
        /// <param name="context">The system context.</param>
        /// <param name="monitoredItemId">The unique identifier for the monitiored item.</param>
        /// <param name="attributeId">The attribute to monitor.</param>
        /// <param name="indexRange">The index range to use for array values.</param>
        /// <param name="dataEncoding">The data encoding to return for structured values.</param>
        /// <param name="diagnosticsMasks">The diagnostics masks to use.</param>
        /// <param name="timestampsToReturn">The timestamps to return.</param>
        /// <param name="monitoringMode">The initial monitoring mode.</param>
        /// <param name="clientHandle">The handle assigned by the client.</param>
        /// <param name="samplingInterval">The sampling interval.</param>
        /// <param name="alwaysReportUpdates">Whether the monitored item should skip the check for a change in value.</param>
        /// <returns>The new monitored item.</returns>
        public DataChangeMonitoredItem CreateDataChangeItem(
            ISystemContext context,
            uint monitoredItemId,
            uint attributeId,
            NumericRange indexRange,
            QualifiedName dataEncoding,
            DiagnosticsMasks diagnosticsMasks,
            TimestampsToReturn timestampsToReturn,
            MonitoringMode monitoringMode,
            uint clientHandle,
            double samplingInterval,
            bool alwaysReportUpdates) {
            return CreateDataChangeItem(
                context,
                monitoredItemId,
                attributeId,
                indexRange,
                dataEncoding,
                diagnosticsMasks,
                timestampsToReturn,
                monitoringMode,
                clientHandle,
                samplingInterval,
                0,
                false,
                null,
                null,
                alwaysReportUpdates);
        }

        /// <summary>
        /// Deletes the monitored item.
        /// </summary>
        public void DeleteItem(IMonitoredItem monitoredItem) {
            if (_monitoredItems != null) {
                for (var ii = 0; ii < _monitoredItems.Count; ii++) {
                    if (ReferenceEquals(monitoredItem, _monitoredItems[ii])) {
                        _monitoredItems.RemoveAt(ii);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Handles change events raised by the node.
        /// </summary>
        /// <param name="context">The system context.</param>
        /// <param name="state">The node that raised the event.</param>
        /// <param name="masks">What caused the event to be raised</param>
        public void OnNodeChange(ISystemContext context, NodeState state, NodeStateChangeMasks masks) {
            if (_monitoredItems != null) {
                for (var ii = 0; ii < _monitoredItems.Count; ii++) {
                    var monitoredItem = _monitoredItems[ii];

                    // check if the node has been deleted.
                    if (((int)masks & (int)NodeStateChangeMasks.Deleted) != 0) {
                        monitoredItem.QueueValue(null, StatusCodes.BadNodeIdUnknown);
                        continue;
                    }

                    if (monitoredItem.AttributeId == Attributes.Value) {
                        if (((int)masks & (int)NodeStateChangeMasks.Value) != 0) {
                            monitoredItem.ValueChanged(context);
                        }
                    }
                    else {
                        if (((int)masks & (int)NodeStateChangeMasks.NonValue) != 0) {
                            monitoredItem.ValueChanged(context);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Subscribes to events produced by the node.
        /// </summary>
        public void SubscribeToEvents(ISystemContext context, IEventMonitoredItem eventSubscription) {
            if (_eventSubscriptions == null) {
                _eventSubscriptions = new List<IEventMonitoredItem>();
            }

            if (_eventSubscriptions.Count == 0) {
                Node.OnReportEvent = OnReportEvent;
                Node.SetAreEventsMonitored(context, true, true);
            }

            for (var ii = 0; ii < _eventSubscriptions.Count; ii++) {
                if (ReferenceEquals(eventSubscription, _eventSubscriptions[ii])) {
                    return;
                }
            }

            _eventSubscriptions.Add(eventSubscription);
        }

        /// <summary>
        /// Unsubscribes to events produced by the node.
        /// </summary>
        public void UnsubscribeToEvents(ISystemContext context, IEventMonitoredItem eventSubscription) {
            if (_eventSubscriptions != null) {
                for (var ii = 0; ii < _eventSubscriptions.Count; ii++) {
                    if (ReferenceEquals(eventSubscription, _eventSubscriptions[ii])) {
                        _eventSubscriptions.RemoveAt(ii);

                        if (_eventSubscriptions.Count == 0) {
                            Node.SetAreEventsMonitored(context, false, true);
                            Node.OnReportEvent = null;
                        }

                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Handles events reported by the node.
        /// </summary>
        /// <param name="context">The system context.</param>
        /// <param name="state">The node that raised the event.</param>
        /// <param name="e">The event to report.</param>
        public void OnReportEvent(ISystemContext context, NodeState state, IFilterTarget e) {
            if (_eventSubscriptions != null) {
                for (var ii = 0; ii < _eventSubscriptions.Count; ii++) {
                    _eventSubscriptions[ii].QueueEvent(e);
                }
            }
        }

        /// <summary>
        /// Resends the events for any conditions belonging to the node or its children.
        /// </summary>
        /// <param name="context">The system context.</param>
        /// <param name="monitoredItem">The item to refresh.</param>
        public void ConditionRefresh(
            ISystemContext context,
            IEventMonitoredItem monitoredItem) {
            if (_eventSubscriptions != null) {
                for (var ii = 0; ii < _eventSubscriptions.Count; ii++) {
                    // only process items monitoring this node.
                    if (!ReferenceEquals(monitoredItem, _eventSubscriptions[ii])) {
                        continue;
                    }

                    // get the set of condition events for the node and its children.
                    var events = new List<IFilterTarget>();
                    Node.ConditionRefresh(context, events, true);

                    // report the events to the monitored item.
                    for (var jj = 0; jj < events.Count; jj++) {
                        monitoredItem.QueueEvent(events[jj]);
                    }
                }
            }
        }

        private List<IEventMonitoredItem> _eventSubscriptions;
        private List<DataChangeMonitoredItem> _monitoredItems;
    }
}
