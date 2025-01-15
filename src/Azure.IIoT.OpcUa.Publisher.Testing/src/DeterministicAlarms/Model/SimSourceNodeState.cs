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

namespace DeterministicAlarms.Model
{
    using DeterministicAlarms.Configuration;
    using DeterministicAlarms.SimBackend;
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using System.Text;

    public class SimSourceNodeState : BaseObjectState
    {
        private readonly DeterministicAlarmsNodeManager _nodeManager;
        private readonly SimSourceNodeBackend _simSourceNodeBackend;
        private readonly Dictionary<string, ConditionState> _alarmNodes = [];
        private readonly Dictionary<string, ConditionState> _events = [];

        public SimSourceNodeState(DeterministicAlarmsNodeManager nodeManager, NodeId nodeId, string name, IList<Alarm> alarms) : base(null)
        {
            _nodeManager = nodeManager;

            Initialize(_nodeManager.SystemContext);

            // Creates the whole backend object model for one source
            _simSourceNodeBackend = ((SimBackendService)_nodeManager.SystemContext.SystemHandle)
                .CreateSourceNodeBackend(name, alarms, OnAlarmChanged);

            // initialize the area with the fixed metadata.
            SymbolicName = name;
            NodeId = nodeId;
            BrowseName = new QualifiedName(name, nodeId.NamespaceIndex);
            DisplayName = BrowseName.Name;
            Description = null;
            ReferenceTypeId = null;
            TypeDefinitionId = ObjectTypeIds.BaseObjectType;
            EventNotifier = EventNotifiers.None;

            // This is to create all alarms
            _simSourceNodeBackend.Refresh();
        }

        public override void ConditionRefresh(ISystemContext context, List<IFilterTarget> events, bool includeChildren)
        {
            foreach (var @event in events)
            {
                if (@event is InstanceStateSnapshot instanceSnapShotForExistingEvent &&
                    ReferenceEquals(instanceSnapShotForExistingEvent.Handle, this))
                {
                    return;
                }
            }

            foreach (var alarm in _alarmNodes.Values)
            {
                if (!alarm.Retain.Value)
                {
                    continue;
                }

                var instanceStateSnapshotNewAlarm = new InstanceStateSnapshot();
                instanceStateSnapshotNewAlarm.Initialize(context, alarm);
                instanceStateSnapshotNewAlarm.Handle = this;
                events.Add(instanceStateSnapshotNewAlarm);
            }
        }

        private void OnAlarmChanged(object sender, SimAlarmStateBackendEventArgs alarm)
        {
            UpdateAlarmInSource(alarm.SnapshotAlarm);
        }

        public void UpdateAlarmInSource(SimAlarmStateBackend alarm, string eventId = null)
        {
            lock (_nodeManager.Lock)
            {
                if (!_alarmNodes.TryGetValue(alarm.Name, out var node))
                {
                    _alarmNodes[alarm.Name] = node = CreateAlarmOrCondition(alarm, null);
                }

                UpdateAlarm(node, alarm, eventId);
                ReportChanges(node);
            }
        }

        private ConditionState CreateAlarmOrCondition(SimAlarmStateBackend alarm, NodeId branchId)
        {
            ISystemContext context = _nodeManager.SystemContext;

            ConditionState node;

            // Condition
            if (alarm.AlarmType == AlarmObjectStates.ConditionType)
            {
                node = new ConditionState(this);
            }
            // All alarms inherent from AlarmConditionState
            else
            {
                switch (alarm.AlarmType)
                {
                    case AlarmObjectStates.TripAlarmType:
                        node = new TripAlarmState(this);
                        break;
                    case AlarmObjectStates.LimitAlarmType:
                        node = new LimitAlarmState(this);
                        break;
                    case AlarmObjectStates.OffNormalAlarmType:
                        node = new OffNormalAlarmState(this);
                        break;
                    default:
                        node = new AlarmConditionState(this);
                        break;
                }

                // create elements that conditiontype doesn't have
                CreateAlarmSpecificElements(context, (AlarmConditionState)node, branchId);
            }

            CreateCommonFieldsForAlarmAndCondition(context, node, alarm);

            // This call initializes the condition from the type model (i.e. creates all of the objects
            // and variables requried to store its state). The information about the type model was
            // incorporated into the class when the class was created.
            //
            // This method also assigns new NodeIds to all of the components by calling the INodeIdFactory.New
            // method on the INodeIdFactory object which is part of the system context. The NodeManager provides
            // the INodeIdFactory implementation used here.
            node.Create(
                context,
                null,
                new QualifiedName(alarm.Name, BrowseName.NamespaceIndex),
                null,
                true);

            // initialize event information.node
            node.EventType.Value = node.TypeDefinitionId;
            node.SourceNode.Value = NodeId;
            node.SourceName.Value = SymbolicName;
            node.ConditionName.Value = node.SymbolicName;
            node.Time.Value = DateTime.UtcNow;
            node.ReceiveTime.Value = node.Time.Value;
            node.BranchId.Value = branchId;

            // don't add branches to the address space.
            if (NodeId.IsNull(branchId))
            {
                AddChild(node);
            }

            return node;
        }

        private static void CreateAlarmSpecificElements(ISystemContext context, AlarmConditionState node, NodeId branchId)
        {
            node.ConfirmedState = new TwoStateVariableState(node);
            node.Confirm = new AddCommentMethodState(node);

            if (NodeId.IsNull(branchId))
            {
                node.SuppressedState = new TwoStateVariableState(node);
                node.ShelvingState = new ShelvedStateMachineState(node);
            }

            node.ActiveState = new TwoStateVariableState(node);
            node.ActiveState.TransitionTime = new PropertyState<DateTime>(node.ActiveState);
            node.ActiveState.EffectiveDisplayName = new PropertyState<LocalizedText>(node.ActiveState);
            node.ActiveState.Create(context, null, BrowseNames.ActiveState, null, false);
        }

        private static void CreateCommonFieldsForAlarmAndCondition(ISystemContext context,
            ConditionState node, SimAlarmStateBackend alarm)
        {
            node.SymbolicName = alarm.Name;

            // add optional components.
            node.Comment = new ConditionVariableState<LocalizedText>(node);
            node.ClientUserId = new PropertyState<string>(node);
            node.AddComment = new AddCommentMethodState(node);

            // adding optional components to children is a little more complicated since the
            // necessary initilization strings defined by the class that represents the child.
            // in this case we pre-create the child, add the optional components
            // and call create without assigning NodeIds. The NodeIds will be assigned when the
            // parent object is created.
            node.EnabledState = new TwoStateVariableState(node);
            node.EnabledState.TransitionTime = new PropertyState<DateTime>(node.EnabledState);
            node.EnabledState.EffectiveDisplayName = new PropertyState<LocalizedText>(node.EnabledState);
            node.EnabledState.Create(context, null, BrowseNames.EnabledState, null, false);

            // specify reference type between the source and the alarm.
            node.ReferenceTypeId = ReferenceTypeIds.HasComponent;
        }

        private void UpdateAlarm(ConditionState node, SimAlarmStateBackend alarm, string eventId = null)
        {
            ISystemContext context = _nodeManager.SystemContext;

            // remove old event.
            if (node.EventId.Value != null)
            {
                _events.Remove(Utils.ToHexString(node.EventId.Value));
            }

            node.EventId.Value = eventId != null ? Encoding.UTF8.GetBytes(eventId) : Guid.NewGuid().ToByteArray();
            node.Time.Value = DateTime.UtcNow;
            node.ReceiveTime.Value = node.Time.Value;

            // save the event for later lookup.
            _events[Utils.ToHexString(node.EventId.Value)] = node;

            // determine the retain state.
            node.Retain.Value = true;

            if (alarm != null)
            {
                node.Time.Value = alarm.Time;
                node.Message.Value = new LocalizedText(alarm.Reason);
                node.SetComment(context, alarm.Comment, alarm.UserName);
                node.SetSeverity(context, alarm.Severity);
                node.EnabledState.TransitionTime.Value = alarm.EnableTime;
                node.SetEnableState(context, (alarm.State & SimConditionStatesEnum.Enabled) != 0);

                if (node is AlarmConditionState nodeAlarm)
                {
                    nodeAlarm.SetAcknowledgedState(context, (alarm.State & SimConditionStatesEnum.Acknowledged) != 0);
                    nodeAlarm.SetConfirmedState(context, (alarm.State & SimConditionStatesEnum.Confirmed) != 0);
                    nodeAlarm.SetActiveState(context, (alarm.State & SimConditionStatesEnum.Active) != 0);
                    nodeAlarm.SetSuppressedState(context, (alarm.State & SimConditionStatesEnum.Suppressed) != 0);
                    nodeAlarm.ActiveState.TransitionTime.Value = alarm.ActiveTime;
                    // not interested in inactive alarms
                    if (!nodeAlarm.ActiveState.Id.Value)
                    {
                        nodeAlarm.Retain.Value = false;
                    }
                }
            }

            // check for deleted items.
            if ((alarm.State & SimConditionStatesEnum.Deleted) != 0)
            {
                node.Retain.Value = false;
            }

            // not interested in disabled alarms.
            if (!node.EnabledState.Id.Value)
            {
                node.Retain.Value = false;
            }
        }

        private void ReportChanges(ConditionState alarm)
        {
            // report changes to node attributes.
            alarm.ClearChangeMasks(_nodeManager.SystemContext, true);

            // check if events are being monitored for the source.
            if (AreEventsMonitored)
            {
                // create a snapshot.
                var e = new InstanceStateSnapshot();
                e.Initialize(_nodeManager.SystemContext, alarm);

                // report the event.
                alarm.ReportEvent(_nodeManager.SystemContext, e);
            }
        }
    }
}
