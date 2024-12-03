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

    /// <summary>
    /// Maps an alarm source to a UA object node.
    /// </summary>
    public class SourceState : BaseObjectState
    {
        /// <summary>
        /// Initializes the area.
        /// </summary>
        /// <param name="nodeManager"></param>
        /// <param name="nodeId"></param>
        /// <param name="sourcePath"></param>
        /// <param name="timeService"></param>
        public SourceState(
            CustomNodeManager2 nodeManager,
            NodeId nodeId,
            string sourcePath,
            TimeService timeService)
        :
            base(null)
        {
            Initialize(nodeManager.SystemContext);

            // save the node manager that owns the source.
            _nodeManager = nodeManager;
            _timeService = timeService;

            // create the source with the underlying system.
            _source = ((UnderlyingSystem)nodeManager.SystemContext.SystemHandle).CreateSource(sourcePath, OnAlarmChanged);

            // initialize the area with the fixed metadata.
            SymbolicName = _source.Name;
            NodeId = nodeId;
            BrowseName = new QualifiedName(Utils.Format("{0}", _source.Name), nodeId.NamespaceIndex);
            DisplayName = BrowseName.Name;
            Description = null;
            ReferenceTypeId = null;
            TypeDefinitionId = ObjectTypeIds.BaseObjectType;
            EventNotifier = EventNotifiers.None;

            // create a dialog.
            _dialog = CreateDialog("OnlineState");

            // create the table of conditions.
            _alarms = [];
            _events = [];
            _branches = [];

            // request an updated for all alarms.
            _source.Refresh();
        }

        /// <summary>
        /// Returns the last event produced for any conditions belonging to the node or its chilren.
        /// </summary>
        /// <param name="context">The system context.</param>
        /// <param name="events">The list of condition events to return.</param>
        /// <param name="includeChildren">Whether to recursively report events for the children.</param>
        public override void ConditionRefresh(ISystemContext context, List<IFilterTarget> events, bool includeChildren)
        {
            // need to check if this source has already been processed during this refresh operation.
            for (var ii = 0; ii < events.Count; ii++)
            {
                if (events[ii] is InstanceStateSnapshot e && ReferenceEquals(e.Handle, this))
                {
                    return;
                }
            }

            // report the dialog.
            if (_dialog != null)
            {
                // do not refresh dialogs that are not active.
                if (_dialog.Retain.Value)
                {
                    // create a snapshot.
                    var e = new InstanceStateSnapshot();
                    e.Initialize(context, _dialog);

                    // set the handle of the snapshot to check for duplicates.
                    e.Handle = this;

                    events.Add(e);
                }
            }

            // the alarm objects act as a cache for the last known state and are used to generate refresh events.
            foreach (var alarm in _alarms.Values)
            {
                // do not refresh alarms that are not in an interesting state.
                if (!alarm.Retain.Value)
                {
                    continue;
                }

                // create a snapshot.
                var e = new InstanceStateSnapshot();
                e.Initialize(context, alarm);

                // set the handle of the snapshot to check for duplicates.
                e.Handle = this;

                events.Add(e);
            }

            // report any active branches.
            foreach (var alarm in _branches.Values)
            {
                // create a snapshot.
                var e = new InstanceStateSnapshot();
                e.Initialize(context, alarm);

                // set the handle of the snapshot to check for duplicates.
                e.Handle = this;

                events.Add(e);
            }
        }
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _dialog?.Dispose();
        }

        /// <summary>
        /// Called when the state of an alarm for the source has changed.
        /// </summary>
        /// <param name="alarm"></param>
        private void OnAlarmChanged(UnderlyingSystemAlarm alarm)
        {
            lock (_nodeManager.Lock)
            {
                // ignore archived alarms for now.
                if (alarm.RecordNumber != 0)
                {
                    var branchId = new NodeId(alarm.RecordNumber, NodeId.NamespaceIndex);

                    // find the alarm branch.

                    if (!_branches.TryGetValue(branchId, out var branch))
                    {
                        _branches[branchId] = branch = CreateAlarm(alarm, branchId);
                    }

                    // map the system information to the UA defined alarm.
                    UpdateAlarm(branch, alarm);
                    ReportChanges(branch);

                    // delete the branch.
                    if ((alarm.State & UnderlyingSystemAlarmStates.Deleted) != 0)
                    {
                        _branches.Remove(branchId);
                    }

                    return;
                }

                // find the alarm node.

                if (!_alarms.TryGetValue(alarm.Name, out var node))
                {
                    _alarms[alarm.Name] = node = CreateAlarm(alarm, null);
                }

                // map the system information to the UA defined alarm.
                UpdateAlarm(node, alarm);
                ReportChanges(node);
            }
        }

        /// <summary>
        /// Creates a new dialog condition
        /// </summary>
        /// <param name="dialogName"></param>
        private DialogConditionState CreateDialog(string dialogName)
        {
            ISystemContext context = _nodeManager.SystemContext;

            var node = new DialogConditionState(this)
            {
                SymbolicName = dialogName
            };

            // specify optional fields.
            node.EnabledState = new TwoStateVariableState(node);
            node.EnabledState.TransitionTime = new PropertyState<DateTime>(node.EnabledState);
            node.EnabledState.EffectiveDisplayName = new PropertyState<LocalizedText>(node.EnabledState);
            node.EnabledState.Create(context, null, BrowseNames.EnabledState, null, false);

            // specify reference type between the source and the alarm.
            node.ReferenceTypeId = ReferenceTypeIds.HasComponent;

            // This call initializes the condition from the type model (i.e. creates all of the objects
            // and variables requried to store its state). The information about the type model was
            // incorporated into the class when the class was created.
            node.Create(
                context,
                null,
                new QualifiedName(dialogName, BrowseName.NamespaceIndex),
                null,
                true);

            AddChild(node);

            // initialize event information.
            node.EventId.Value = Guid.NewGuid().ToByteArray();
            node.EventType.Value = node.TypeDefinitionId;
            node.SourceNode.Value = NodeId;
            node.SourceName.Value = SymbolicName;
            node.ConditionName.Value = node.SymbolicName;
            node.Time.Value = _timeService.UtcNow;
            node.ReceiveTime.Value = node.Time.Value;
            if (node.LocalTime != null)
            {
                node.LocalTime.Value = Utils.GetTimeZoneInfo();
            }

            node.Message.Value = "The dialog was activated";
            node.Retain.Value = true;

            node.SetEnableState(context, true);
            node.SetSeverity(context, EventSeverity.Low);

            // initialize the dialog information.
            node.Prompt.Value = "Please specify a new state for the source.";
            node.ResponseOptionSet.Value = _responseOptions;
            node.DefaultResponse.Value = 2;
            node.CancelResponse.Value = 2;
            node.OkResponse.Value = 0;

            // set up method handlers.
            node.OnRespond = OnRespond;

            // this flag needs to be set because the underlying system does not produce these events.
            node.AutoReportStateChanges = true;

            // activate the dialog.
            node.Activate(context);

            // return the new node.
            return node;
        }

        /// <summary>
        /// The responses used with the dialog condition.
        /// </summary>
        private readonly LocalizedText[] _responseOptions = [
            "Online",
            "Offline",
            "No Change"
        ];

        /// <summary>
        /// Creates a new alarm for the source.
        /// </summary>
        /// <param name="alarm">The alarm.</param>
        /// <param name="branchId">The branch id.</param>
        /// <returns>The new alarm.</returns>
        private AlarmConditionState CreateAlarm(UnderlyingSystemAlarm alarm, NodeId branchId)
        {
            ISystemContext context = _nodeManager.SystemContext;

            AlarmConditionState node = null;

            // need to map the alarm type to a UA defined alarm type.
            switch (alarm.AlarmType)
            {
                case "HighAlarm":
                    {
                        var node2 = new ExclusiveDeviationAlarmState(this);
                        node = node2;
                        node2.HighLimit = new PropertyState<double>(node2);
                        break;
                    }

                case "HighLowAlarm":
                    {
                        var node2 = new NonExclusiveLevelAlarmState(this);
                        node = node2;

                        node2.HighHighLimit = new PropertyState<double>(node2);
                        node2.HighLimit = new PropertyState<double>(node2);
                        node2.LowLimit = new PropertyState<double>(node2);
                        node2.LowLowLimit = new PropertyState<double>(node2);

                        node2.HighHighState = new TwoStateVariableState(node2);
                        node2.HighState = new TwoStateVariableState(node2);
                        node2.LowState = new TwoStateVariableState(node2);
                        node2.LowLowState = new TwoStateVariableState(node2);

                        break;
                    }

                case "TripAlarm":
                    {
                        node = new TripAlarmState(this);
                        break;
                    }

                default:
                    {
                        node = new AlarmConditionState(this);
                        break;
                    }
            }

            node.SymbolicName = alarm.Name;

            // add optional components.
            node.Comment = new ConditionVariableState<LocalizedText>(node);
            node.ClientUserId = new PropertyState<string>(node);
            node.AddComment = new AddCommentMethodState(node);
            node.ConfirmedState = new TwoStateVariableState(node);
            node.Confirm = new AddCommentMethodState(node);

            if (NodeId.IsNull(branchId))
            {
                node.SuppressedState = new TwoStateVariableState(node);
                node.ShelvingState = new ShelvedStateMachineState(node);
            }

            // adding optional components to children is a little more complicated since the
            // necessary initilization strings defined by the class that represents the child.
            // in this case we pre-create the child, add the optional components
            // and call create without assigning NodeIds. The NodeIds will be assigned when the
            // parent object is created.
            node.EnabledState = new TwoStateVariableState(node);
            node.EnabledState.TransitionTime = new PropertyState<DateTime>(node.EnabledState);
            node.EnabledState.EffectiveDisplayName = new PropertyState<LocalizedText>(node.EnabledState);
            node.EnabledState.Create(context, null, BrowseNames.EnabledState, null, false);

            // same procedure add optional components to the ActiveState component.
            node.ActiveState = new TwoStateVariableState(node);
            node.ActiveState.TransitionTime = new PropertyState<DateTime>(node.ActiveState);
            node.ActiveState.EffectiveDisplayName = new PropertyState<LocalizedText>(node.ActiveState);
            node.ActiveState.Create(context, null, BrowseNames.ActiveState, null, false);

            // specify reference type between the source and the alarm.
            node.ReferenceTypeId = ReferenceTypeIds.HasComponent;

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

            // don't add branches to the address space.
            if (NodeId.IsNull(branchId))
            {
                AddChild(node);
            }

            // initialize event information.node
            node.EventType.Value = node.TypeDefinitionId;
            node.SourceNode.Value = NodeId;
            node.SourceName.Value = SymbolicName;
            node.ConditionName.Value = node.SymbolicName;
            node.Time.Value = _timeService.UtcNow;
            node.ReceiveTime.Value = node.Time.Value;
            if (node.LocalTime != null)
            {
                node.LocalTime.Value = Utils.GetTimeZoneInfo();
            }
            node.BranchId.Value = branchId;

            // set up method handlers.
            node.OnEnableDisable = OnEnableDisableAlarm;
            node.OnAcknowledge = OnAcknowledge;
            node.OnAddComment = OnAddComment;
            node.OnConfirm = OnConfirm;
            node.OnShelve = OnShelve;
            node.OnTimedUnshelve = OnTimedUnshelve;

            // return the new node.
            return node;
        }

        /// <summary>
        /// Updates the alarm with a new state.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="alarm">The alarm.</param>
        private void UpdateAlarm(AlarmConditionState node, UnderlyingSystemAlarm alarm)
        {
            ISystemContext context = _nodeManager.SystemContext;

            // remove old event.
            if (node.EventId.Value != null)
            {
                _events.Remove(Utils.ToHexString(node.EventId.Value));
            }

            // update the basic event information (include generating a unique id for the event).
            node.EventId.Value = Guid.NewGuid().ToByteArray();
            node.Time.Value = _timeService.UtcNow;
            node.ReceiveTime.Value = node.Time.Value;

            // save the event for later lookup.
            _events[Utils.ToHexString(node.EventId.Value)] = node;

            // determine the retain state.
            node.Retain.Value = true;

            if (alarm != null)
            {
                node.Time.Value = alarm.Time;
                node.Message.Value = new LocalizedText(alarm.Reason);

                // update the states.
                node.SetEnableState(context, (alarm.State & UnderlyingSystemAlarmStates.Enabled) != 0);
                node.SetAcknowledgedState(context, (alarm.State & UnderlyingSystemAlarmStates.Acknowledged) != 0);
                node.SetConfirmedState(context, (alarm.State & UnderlyingSystemAlarmStates.Confirmed) != 0);
                node.SetActiveState(context, (alarm.State & UnderlyingSystemAlarmStates.Active) != 0);
                node.SetSuppressedState(context, (alarm.State & UnderlyingSystemAlarmStates.Suppressed) != 0);

                // update other information.
                node.SetComment(context, alarm.Comment, alarm.UserName);
                node.SetSeverity(context, alarm.Severity);

                node.EnabledState.TransitionTime.Value = alarm.EnableTime;
                node.ActiveState.TransitionTime.Value = alarm.ActiveTime;

                // check for deleted items.
                if ((alarm.State & UnderlyingSystemAlarmStates.Deleted) != 0)
                {
                    node.Retain.Value = false;
                }

                // handle high alarms.

                if (node is ExclusiveLimitAlarmState highAlarm)
                {
                    highAlarm.HighLimit.Value = alarm.Limits[0];

                    if ((alarm.State & UnderlyingSystemAlarmStates.High) != 0)
                    {
                        highAlarm.SetLimitState(context, LimitAlarmStates.High);
                    }
                }

                // handle high-low alarms.

                if (node is NonExclusiveLimitAlarmState highLowAlarm)
                {
                    highLowAlarm.HighHighLimit.Value = alarm.Limits[0];
                    highLowAlarm.HighLimit.Value = alarm.Limits[1];
                    highLowAlarm.LowLimit.Value = alarm.Limits[2];
                    highLowAlarm.LowLowLimit.Value = alarm.Limits[3];

                    var limit = LimitAlarmStates.Inactive;

                    if ((alarm.State & UnderlyingSystemAlarmStates.HighHigh) != 0)
                    {
                        limit |= LimitAlarmStates.HighHigh;
                    }

                    if ((alarm.State & UnderlyingSystemAlarmStates.High) != 0)
                    {
                        limit |= LimitAlarmStates.High;
                    }

                    if ((alarm.State & UnderlyingSystemAlarmStates.Low) != 0)
                    {
                        limit |= LimitAlarmStates.Low;
                    }

                    if ((alarm.State & UnderlyingSystemAlarmStates.LowLow) != 0)
                    {
                        limit |= LimitAlarmStates.LowLow;
                    }

                    highLowAlarm.SetLimitState(context, limit);
                }
            }

            // not interested in disabled or inactive alarms.
            if (!node.EnabledState.Id.Value || !node.ActiveState.Id.Value)
            {
                node.Retain.Value = false;
            }
        }

        /// <summary>
        /// Called when the alarm is enabled or disabled.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="condition"></param>
        /// <param name="enabling"></param>
        private ServiceResult OnEnableDisableAlarm(
            ISystemContext context,
            ConditionState condition,
            bool enabling)
        {
            _source.EnableAlarm(condition.SymbolicName, enabling);
            return ServiceResult.Good;
        }

        /// <summary>
        /// Called when the alarm has a comment added.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="condition"></param>
        /// <param name="eventId"></param>
        /// <param name="comment"></param>
        private ServiceResult OnAddComment(
            ISystemContext context,
            ConditionState condition,
            byte[] eventId,
            LocalizedText comment)
        {
            var alarm = FindAlarmByEventId(eventId);

            if (alarm == null)
            {
                return StatusCodes.BadEventIdUnknown;
            }

            _source.CommentAlarm(alarm.SymbolicName, GetRecordNumber(alarm), comment, GetUserName(context));

            return ServiceResult.Good;
        }

        /// <summary>
        /// Called when the alarm is acknowledged.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="condition"></param>
        /// <param name="eventId"></param>
        /// <param name="comment"></param>
        private ServiceResult OnAcknowledge(
            ISystemContext context,
            ConditionState condition,
            byte[] eventId,
            LocalizedText comment)
        {
            var alarm = FindAlarmByEventId(eventId);

            if (alarm == null)
            {
                return StatusCodes.BadEventIdUnknown;
            }

            _source.AcknowledgeAlarm(alarm.SymbolicName, GetRecordNumber(alarm), comment, GetUserName(context));

            return ServiceResult.Good;
        }

        /// <summary>
        /// Called when the alarm is confirmed.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="condition"></param>
        /// <param name="eventId"></param>
        /// <param name="comment"></param>
        private ServiceResult OnConfirm(
            ISystemContext context,
            ConditionState condition,
            byte[] eventId,
            LocalizedText comment)
        {
            var alarm = FindAlarmByEventId(eventId);

            if (alarm == null)
            {
                return StatusCodes.BadEventIdUnknown;
            }

            _source.ConfirmAlarm(alarm.SymbolicName, GetRecordNumber(alarm), comment, GetUserName(context));

            return ServiceResult.Good;
        }

        /// <summary>
        /// Called when the alarm is shelved.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="alarm"></param>
        /// <param name="shelving"></param>
        /// <param name="oneShot"></param>
        /// <param name="shelvingTime"></param>
        private ServiceResult OnShelve(
            ISystemContext context,
            AlarmConditionState alarm,
            bool shelving,
            bool oneShot,
            double shelvingTime)
        {
            alarm.SetShelvingState(context, shelving, oneShot, shelvingTime);
            alarm.Message.Value = "The alarm shelved.";

            UpdateAlarm(alarm, null);
            ReportChanges(alarm);

            return ServiceResult.Good;
        }

        /// <summary>
        /// Called when the alarm is shelved.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="alarm"></param>
        private ServiceResult OnTimedUnshelve(
            ISystemContext context,
            AlarmConditionState alarm)
        {
            // update the alarm state and produce and event.
            alarm.SetShelvingState(context, false, false, 0);
            alarm.Message.Value = "The timed shelving period expired.";

            UpdateAlarm(alarm, null);
            ReportChanges(alarm);

            return ServiceResult.Good;
        }

        /// <summary>
        /// Called when the dialog receives a response.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="dialog"></param>
        /// <param name="selectedResponse"></param>
        private ServiceResult OnRespond(
            ISystemContext context,
            DialogConditionState dialog,
            int selectedResponse)
        {
            // response 0 means set the source online.
            if (selectedResponse == 0)
            {
                _source.SetOfflineState(false);
            }

            // response 1 means set the source offine.
            if (selectedResponse == 1)
            {
                _source.SetOfflineState(true);
            }

            // other responses mean do nothing.
            dialog.SetResponse(context, selectedResponse);

            // dialog no longer interesting once it is deactivated.
            dialog.Message.Value = "The dialog was deactivated";
            dialog.Retain.Value = false;

            return ServiceResult.Good;
        }

        /// <summary>
        /// Reports the changes to the alarm.
        /// </summary>
        /// <param name="alarm"></param>
        private void ReportChanges(AlarmConditionState alarm)
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

        /// <summary>
        /// Finds the alarm by event id.
        /// </summary>
        /// <param name="eventId">The event id.</param>
        /// <returns>The alarm. Null if not found.</returns>
        private AlarmConditionState FindAlarmByEventId(byte[] eventId)
        {
            if (eventId == null)
            {
                return null;
            }

            if (!_events.TryGetValue(Utils.ToHexString(eventId), out var alarm))
            {
                return null;
            }

            return alarm;
        }

        /// <summary>
        /// Gets the record number associated with tge alarm.
        /// </summary>
        /// <param name="alarm">The alarm.</param>
        /// <returns>The record number; 0 if the alarm is not an archived alarm.</returns>
        private uint GetRecordNumber(AlarmConditionState alarm)
        {
            if (alarm == null)
            {
                return 0;
            }

            if (alarm.BranchId == null || alarm.BranchId.Value == null)
            {
                return 0;
            }

            var recordNumber = alarm.BranchId.Value.Identifier as uint?;

            return recordNumber ?? 0;
        }

        /// <summary>
        /// Gets the user name associated with the context.
        /// </summary>
        /// <param name="context"></param>
        private string GetUserName(ISystemContext context)
        {
            if (context.UserIdentity != null)
            {
                return context.UserIdentity.DisplayName;
            }

            return null;
        }

        private readonly CustomNodeManager2 _nodeManager;
        private readonly TimeService _timeService;
        private readonly UnderlyingSystemSource _source;
        private readonly Dictionary<string, AlarmConditionState> _alarms;
        private readonly Dictionary<string, AlarmConditionState> _events;
        private readonly Dictionary<NodeId, AlarmConditionState> _branches;
        private readonly DialogConditionState _dialog;
    }
}
