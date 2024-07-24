// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Services
{
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Encoders.PubSub;
    using Microsoft.Extensions.Logging;
    using Opc.Ua;
    using Opc.Ua.Client;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Threading;
    using System.Threading.Tasks;

    internal abstract partial class OpcUaMonitoredItem
    {
        /// <summary>
        /// Condition item
        /// </summary>
        [DataContract(Namespace = Namespaces.OpcUaXsd)]
        [KnownType(typeof(DataChangeFilter))]
        [KnownType(typeof(EventFilter))]
        [KnownType(typeof(AggregateFilter))]
        internal class Condition : Event
        {
            /// <summary>
            /// Create condition item
            /// </summary>
            /// <param name="template"></param>
            /// <param name="logger"></param>
            /// <param name="timeProvider"></param>
            public Condition(EventMonitoredItemModel template,
                ILogger<Event> logger, TimeProvider timeProvider) :
                base(template, logger, timeProvider)
            {
                _snapshotInterval = template.ConditionHandling?.SnapshotInterval
                    ?? throw new ArgumentException("Invalid snapshot interval");
                _updateInterval = template.ConditionHandling?.UpdateInterval
                    ?? _snapshotInterval;
                _lastSentPendingConditions = timeProvider.GetUtcNow();
                _conditionHandlingState = new ConditionHandlingState();
            }

            /// <summary>
            /// Copy constructor
            /// </summary>
            /// <param name="item"></param>
            /// <param name="copyEventHandlers"></param>
            /// <param name="copyClientHandle"></param>
            private Condition(Condition item, bool copyEventHandlers,
                bool copyClientHandle)
                : base(item, copyEventHandlers, copyClientHandle)
            {
                _snapshotInterval = item._snapshotInterval;
                _updateInterval = item._updateInterval;
                _conditionHandlingState = item._conditionHandlingState;
                _lastSentPendingConditions = item._lastSentPendingConditions;
                _callback = item._callback;

                if (_callback != null)
                {
                    EnableConditionTimer();
                }
            }

            /// <inheritdoc/>
            public override MonitoredItem CloneMonitoredItem(
                bool copyEventHandlers, bool copyClientHandle)
            {
                return new Condition(this, copyEventHandlers, copyClientHandle);
            }

            /// <inheritdoc/>
            public override bool Equals(object? obj)
            {
                if (obj is not Condition item)
                {
                    return false;
                }
                return base.Equals(item);
            }

            /// <inheritdoc/>
            public override int GetHashCode()
            {
                return 38138123 + base.GetHashCode();
            }

            /// <inheritdoc/>
            public override string ToString()
            {
                return
                    $"Condition Item '{Template.StartNodeId}' with server id {RemoteId}" +
                    $" - {(Status?.Created == true ? "" : "not ")}created";
            }

            /// <inheritdoc/>
            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    lock (_timerLock)
                    {
                        _disposed = true;
                        if (_conditionTimer != null)
                        {
                            _conditionTimer.Elapsed -= OnConditionTimerElapsed;
                            _conditionTimer.Dispose();
                            _conditionTimer = null;
                        }
                    }
                }
                base.Dispose(disposing);
            }

            /// <inheritdoc/>
            protected override bool ProcessEventNotification(uint sequenceNumber, DateTimeOffset publishTime,
                EventFieldList eventFields, IList<MonitoredItemNotificationModel> notifications)
            {
                Debug.Assert(Valid);
                Debug.Assert(Template != null);

                if (_disposed)
                {
                    return false;
                }

                var evFilter = Filter as EventFilter;
                var eventTypeIndex = evFilter?.SelectClauses.IndexOf(
                    evFilter.SelectClauses
                        .Find(x => x.TypeDefinitionId == ObjectTypeIds.BaseEventType
                            && x.BrowsePath?.FirstOrDefault() == BrowseNames.EventType));

                var state = _conditionHandlingState;

                // now, is this a regular event or RefreshStartEventType/RefreshEndEventType?
                if (eventTypeIndex.HasValue && eventTypeIndex.Value != -1)
                {
                    var eventType = eventFields.EventFields[eventTypeIndex.Value].Value as NodeId;
                    if (eventType == ObjectTypeIds.RefreshStartEventType)
                    {
                        // stop the timers during condition refresh
                        DisableConditionTimer();
                        state.Active.Clear();
                        _logger.LogDebug("{Item}: Stopped pending alarm handling " +
                            "during condition refresh.", this);
                        return true;
                    }
                    else if (eventType == ObjectTypeIds.RefreshEndEventType)
                    {
                        // restart the timers once condition refresh is done.
                        EnableConditionTimer();
                        _logger.LogDebug("{Item}: Restarted pending alarm handling " +
                            "after condition refresh.", this);
                        return true;
                    }
                    else if (eventType == ObjectTypeIds.RefreshRequiredEventType)
                    {
                        var noErrorFound = true;

                        // issue a condition refresh to make sure we are in a correct state
                        _logger.LogInformation("{Item}: Issuing ConditionRefresh for " +
                            "item {Name} on subscription {Subscription} due to receiving " +
                            "a RefreshRequired event", this, Template.DisplayName,
                            Subscription.DisplayName);
                        try
                        {
                            Subscription.ConditionRefresh();
                        }
                        catch (Exception e)
                        {
                            _logger.LogInformation("{Item}: ConditionRefresh for item {Name} " +
                                "on subscription {Subscription} failed with error '{Message}'",
                                this, Template.DisplayName, Subscription.DisplayName, e.Message);
                            noErrorFound = false;
                        }
                        if (noErrorFound)
                        {
                            _logger.LogInformation("{Item}: ConditionRefresh for item {Name} " +
                                "on subscription {Subscription} has completed", this,
                                Template.DisplayName, Subscription.DisplayName);
                        }
                        return true;
                    }
                }

                var monitoredItemNotifications = ToMonitoredItemNotifications(
                    sequenceNumber, eventFields).ToList();
                var conditionIdIndex = state.ConditionIdIndex;
                var retainIndex = state.RetainIndex;
                if (conditionIdIndex < monitoredItemNotifications.Count &&
                    retainIndex < monitoredItemNotifications.Count)
                {
                    // Cache conditions
                    var conditionId = monitoredItemNotifications[conditionIdIndex].Value?
                        .Value?.ToString();
                    if (conditionId != null)
                    {
                        var retain = monitoredItemNotifications[retainIndex].Value?
                            .GetValue(false) ?? false;

                        if (state.Active.ContainsKey(conditionId) && !retain)
                        {
                            state.Active.Remove(conditionId, out _);
                            state.Dirty = true;
                        }
                        else if (retain && !monitoredItemNotifications
                            .All(m => m.Value?.Value == null))
                        {
                            state.Dirty = true;
                            monitoredItemNotifications.ForEach(n =>
                            {
                                n.Value ??= new DataValue(StatusCodes.GoodNoData);
                                // Set SourceTimestamp to publish time
                                n.Value.SourceTimestamp = publishTime.UtcDateTime;
                            });
                            state.Active.AddOrUpdate(conditionId, monitoredItemNotifications);
                        }
                    }
                }
                return true;
            }

            /// <inheritdoc/>
            public override bool MergeWith(OpcUaMonitoredItem item, IOpcUaSession session,
                 out bool metadataChanged)
            {
                metadataChanged = false;
                if (item is not Condition model || !Valid)
                {
                    return false;
                }

                var itemChange = false;
                if (_snapshotInterval != model._snapshotInterval)
                {
                    _logger.LogDebug("{Item}: Changing shptshot interval from {Old} to {New}",
                        this, TimeSpan.FromSeconds(_snapshotInterval).TotalMilliseconds,
                        TimeSpan.FromSeconds(model._snapshotInterval).TotalMilliseconds);

                    _snapshotInterval = model._snapshotInterval;
                    itemChange = true;
                }

                if (_updateInterval != model._updateInterval)
                {
                    _logger.LogDebug("{Item}: Changing update interval from {Old} to {New}",
                        this, TimeSpan.FromSeconds(_updateInterval).TotalMilliseconds,
                        TimeSpan.FromSeconds(model._updateInterval).TotalMilliseconds);

                    _updateInterval = model._updateInterval;
                    itemChange = true;
                }

                itemChange |= base.MergeWith(model, session, out metadataChanged);
                return itemChange;
            }

            /// <inheritdoc/>
            public override bool TryCompleteChanges(Subscription subscription,
                ref bool applyChanges, Callback cb)
            {
                var result = base.TryCompleteChanges(subscription, ref applyChanges, cb);
                if (!AttachedToSubscription || !result)
                {
                    DisableConditionTimer();
                    _callback = null;
                }
                else
                {
                    _callback = cb;
                    EnableConditionTimer();
                }
                return result;
            }

            /// <summary>
            /// Get event filter
            /// </summary>
            /// <param name="session"></param>
            /// <param name="ct"></param>
            /// <returns></returns>
            protected override async ValueTask<EventFilter> GetEventFilterAsync(IOpcUaSession session,
                CancellationToken ct)
            {
                var (eventFilter, internalSelectClauses) =
                    await BuildEventFilterAsync(session, ct).ConfigureAwait(false);

                var conditionHandlingState = InitializeConditionHandlingState(
                    eventFilter, internalSelectClauses);

                UpdateFieldNames(session, eventFilter, internalSelectClauses);

                _conditionHandlingState = conditionHandlingState;
                EnableConditionTimer();

                return eventFilter;
            }

            /// <summary>
            /// Initialize periodic pending condition handling state
            /// </summary>
            /// <param name="eventFilter"></param>
            /// <param name="internalSelectClauses"></param>
            /// <returns></returns>
            private static ConditionHandlingState InitializeConditionHandlingState(
                EventFilter eventFilter, List<SimpleAttributeOperand> internalSelectClauses)
            {
                var conditionHandlingState = new ConditionHandlingState();

                var conditionIdClause = eventFilter.SelectClauses
                    .Find(x => x.TypeDefinitionId == ObjectTypeIds.ConditionType
                        && x.AttributeId == Attributes.NodeId);
                if (conditionIdClause != null)
                {
                    conditionHandlingState.ConditionIdIndex =
                        eventFilter.SelectClauses.IndexOf(conditionIdClause);
                }
                else
                {
                    conditionHandlingState.ConditionIdIndex = eventFilter.SelectClauses.Count;
                    var selectClause = new SimpleAttributeOperand()
                    {
                        BrowsePath = new QualifiedNameCollection(),
                        TypeDefinitionId = ObjectTypeIds.ConditionType,
                        AttributeId = Attributes.NodeId
                    };
                    eventFilter.SelectClauses.Add(selectClause);
                    internalSelectClauses.Add(selectClause);
                }

                var retainClause = eventFilter.SelectClauses
                    .Find(x => x.TypeDefinitionId == ObjectTypeIds.ConditionType &&
                        x.BrowsePath?.FirstOrDefault() == BrowseNames.Retain);
                if (retainClause != null)
                {
                    conditionHandlingState.RetainIndex =
                        eventFilter.SelectClauses.IndexOf(retainClause);
                }
                else
                {
                    conditionHandlingState.RetainIndex = eventFilter.SelectClauses.Count;
                    var selectClause = new SimpleAttributeOperand(
                        ObjectTypeIds.ConditionType, BrowseNames.Retain);
                    eventFilter.SelectClauses.Add(selectClause);
                    internalSelectClauses.Add(selectClause);
                }
                return conditionHandlingState;
            }

            /// <summary>
            /// Called when the condition timer fires
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
            private void OnConditionTimerElapsed(object? sender, ElapsedEventArgs e)
            {
                Debug.Assert(Template != null);
                var now = TimeProvider.GetUtcNow();
                var state = _conditionHandlingState;
                try
                {
                    if (!Created)
                    {
                        return;
                    }

                    // is it time to send anything?
                    var sendPendingConditions = now >
                        _lastSentPendingConditions + TimeSpan.FromSeconds(_snapshotInterval);
                    if (!sendPendingConditions && state.Dirty)
                    {
                        sendPendingConditions = now >
                            _lastSentPendingConditions + TimeSpan.FromSeconds(_updateInterval);
                    }
                    if (sendPendingConditions)
                    {
                        SendPendingConditions();
                        _lastSentPendingConditions = now;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "{Item}: SendPendingConditions failed.", this);
                }
                finally
                {
                    EnableConditionTimer();
                }
            }

            /// <summary>
            /// Send pending conditions
            /// </summary>
            private void SendPendingConditions()
            {
                var state = _conditionHandlingState;
                var callback = _callback;
                if (callback == null)
                {
                    return;
                }

                var notifications = state.Active
                    .Select(entry => entry.Value
                        .Where(n => n.DataSetFieldName != null)
                        .Select(n => n with { })
                        .ToList())
                    .ToList();
                state.Dirty = false;

                foreach (var conditionNotification in notifications)
                {
                    callback(MessageType.Condition, conditionNotification,
                        dataSetName: DataSetName);
                }
            }

            /// <summary>
            /// Enable timer
            /// </summary>
            private void EnableConditionTimer()
            {
                lock (_timerLock)
                {
                    if (_disposed)
                    {
                        return;
                    }
                    if (_conditionTimer == null)
                    {
                        _conditionTimer = new TimerEx(TimeProvider);
                        _conditionTimer.AutoReset = false;
                        _conditionTimer.Elapsed += OnConditionTimerElapsed;
                        _logger.LogDebug("Re-enabled condition timer.");
                    }
                    _conditionTimer.Interval = TimeSpan.FromSeconds(1);
                    _conditionTimer.Enabled = true;
                }
            }

            /// <summary>
            /// Disable timer
            /// </summary>
            private void DisableConditionTimer()
            {
                lock (_timerLock)
                {
                    if (_conditionTimer != null)
                    {
                        _conditionTimer.Elapsed -= OnConditionTimerElapsed;
                        _conditionTimer.Dispose();
                        _conditionTimer = null;
                        _logger.LogDebug("Disabled condition timer.");
                    }
                }
            }

            private sealed class ConditionHandlingState
            {
                /// <summary>
                /// Index in the SelectClause array for Condition id field
                /// </summary>
                public int ConditionIdIndex { get; set; }

                /// <summary>
                /// Index in the SelectClause array for Retain field
                /// </summary>
                public int RetainIndex { get; set; }

                /// <summary>
                /// Has the pending alarms events been updated since las update message?
                /// </summary>
                public bool Dirty { get; set; }

                /// <summary>
                /// Cache of the latest events for the pending alarms optionally monitored
                /// </summary>
                public Dictionary<string, List<MonitoredItemNotificationModel>> Active { get; }
                    = new Dictionary<string, List<MonitoredItemNotificationModel>>();
            }

            private Callback? _callback;
            private ConditionHandlingState _conditionHandlingState;
            private DateTimeOffset _lastSentPendingConditions;
            private int _snapshotInterval;
            private int _updateInterval;
            private TimerEx? _conditionTimer;
            private object _timerLock = new();
            private bool _disposed;
        }
    }
}
