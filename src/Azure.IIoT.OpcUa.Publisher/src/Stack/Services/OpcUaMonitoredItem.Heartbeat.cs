﻿// ------------------------------------------------------------
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
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Threading;
    using Timer = System.Timers.Timer;

    internal abstract partial class OpcUaMonitoredItem
    {
        /// <summary>
        /// <para>
        /// Data monitored item with heartbeat handling. If no data is sent
        /// within the heartbeat interval the previously received data is
        /// sent instead. Heartbeat is not a cyclic read.
        /// </para>
        /// <para>
        /// Heartbeat is now implemented as a watchdog of the notification
        /// which is reset every time a new value arrives. This is different
        /// from past implementation where the keep alive was driving heartbeats.
        /// This caused issues and was ineffective due to the use of the value
        /// cache on the subscription object caching _all_ values received.
        /// </para>
        /// </summary>
        [DataContract(Namespace = Namespaces.OpcUaXsd)]
        [KnownType(typeof(DataChangeFilter))]
        [KnownType(typeof(EventFilter))]
        [KnownType(typeof(AggregateFilter))]
        internal sealed class Heartbeat : DataChange
        {
            /// <summary>
            /// Create data item with heartbeat
            /// </summary>
            /// <param name="dataTemplate"></param>
            /// <param name="logger"></param>
            public Heartbeat(DataMonitoredItemModel dataTemplate,
                ILogger<DataChange> logger) : base(dataTemplate, logger)
            {
                _heartbeatInterval = dataTemplate.HeartbeatInterval
                    ?? dataTemplate.SamplingInterval ?? TimeSpan.FromSeconds(1);
                _timerInterval = Timeout.InfiniteTimeSpan;
                _heartbeatBehavior = dataTemplate.HeartbeatBehavior
                    ?? HeartbeatBehavior.WatchdogLKV;
                _heartbeatTimer = new Timer();
                _heartbeatTimer.Elapsed += SendHeartbeatNotifications;
                _heartbeatTimer.AutoReset = true;
                _heartbeatTimer.Enabled = true;
            }

            /// <summary>
            /// Copy constructor
            /// </summary>
            /// <param name="item"></param>
            /// <param name="copyEventHandlers"></param>
            /// <param name="copyClientHandle"></param>
            private Heartbeat(Heartbeat item, bool copyEventHandlers,
                bool copyClientHandle)
                : base(item, copyEventHandlers, copyClientHandle)
            {
                _heartbeatInterval = item._heartbeatInterval;
                _timerInterval = item._timerInterval;
                _heartbeatBehavior = item._heartbeatBehavior;
                _lastValueReceived = item._lastValueReceived;
                _callback = item._callback;
                _heartbeatTimer = item.CloneTimer();
                if (_heartbeatTimer != null)
                {
                    _heartbeatTimer.Elapsed += SendHeartbeatNotifications;
                }
            }

            /// <inheritdoc/>
            public override MonitoredItem CloneMonitoredItem(
                bool copyEventHandlers, bool copyClientHandle)
            {
                return new Heartbeat(this, copyEventHandlers, copyClientHandle);
            }

            /// <inheritdoc/>
            public override bool Equals(object? obj)
            {
                if (obj is not Heartbeat)
                {
                    return false;
                }
                return base.Equals(obj);
            }

            /// <inheritdoc/>
            public override int GetHashCode()
            {
                return HashCode.Combine(base.GetHashCode(), nameof(Heartbeat));
            }

            /// <inheritdoc/>
            public override string ToString()
            {
                return $"Data Item '{Template.StartNodeId}' " +
                    $"(with {Template.HeartbeatBehavior ?? HeartbeatBehavior.WatchdogLKV} Heartbeat) " +
                    $"with server id {RemoteId} - {(Status?.Created == true ? "" :
                        "not ")}created";
            }

            /// <inheritdoc/>
            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    var timer = CloneTimer();
                    timer?.Dispose();
                }
                base.Dispose(disposing);
            }

            /// <inheritdoc/>
            protected override bool ProcessMonitoredItemNotification(uint sequenceNumber,
                DateTime timestamp, MonitoredItemNotification monitoredItemNotification,
                IList<MonitoredItemNotificationModel> notifications)
            {
                Debug.Assert(Valid);
                var result = base.ProcessMonitoredItemNotification(sequenceNumber, timestamp,
                    monitoredItemNotification, notifications);

                if (_heartbeatTimer != null && (_heartbeatBehavior & HeartbeatBehavior.PeriodicLKV) == 0)
                {
                    _heartbeatTimer.Interval = _timerInterval.TotalMilliseconds;
                    _heartbeatTimer.Enabled = true;
                }
                return result;
            }

            /// <inheritdoc/>
            public override bool MergeWith(OpcUaMonitoredItem item, IOpcUaSession session,
                 out bool metadataChanged)
            {
                metadataChanged = false;
                if (item is not Heartbeat model || !Valid)
                {
                    return false;
                }

                var itemChange = false;

                if (_heartbeatInterval != model._heartbeatInterval)
                {
                    _logger.LogDebug("{Item}: Changing heartbeat from {Old} to {New}",
                        this, _heartbeatInterval, model._heartbeatInterval);

                    _heartbeatInterval = model._heartbeatInterval;
                    itemChange = true;
                }

                if (_heartbeatBehavior != model._heartbeatBehavior)
                {
                    _logger.LogDebug("{Item}: Changing heartbeat behavior from {Old} to {New}",
                        this, _heartbeatBehavior, model._heartbeatBehavior);

                    _heartbeatBehavior = model._heartbeatBehavior;
                    itemChange = true;
                }

                itemChange |= base.MergeWith(model, session, out metadataChanged);
                return itemChange;
            }

            /// <inheritdoc/>
            public override bool TryCompleteChanges(Subscription subscription,
                ref bool applyChanges,
                Callback cb)
            {
                if (_heartbeatTimer == null)
                {
                    return false;
                }
                var result = base.TryCompleteChanges(subscription, ref applyChanges, cb);
                {
                    var lkg = (_heartbeatBehavior & HeartbeatBehavior.WatchdogLKG)
                            == HeartbeatBehavior.WatchdogLKG;
                    if (!AttachedToSubscription || (!result && lkg))
                    {
                        _callback = null;
                        // Stop heartbeat
                        _heartbeatTimer.Enabled = false;
                        _timerInterval = Timeout.InfiniteTimeSpan;
                    }
                    else
                    {
                        Debug.Assert(AttachedToSubscription);
                        _callback = cb;
                        if (_timerInterval != _heartbeatInterval)
                        {
                            // Start heartbeat after completion
                            _heartbeatTimer.Interval = _heartbeatInterval.TotalMilliseconds;
                            _timerInterval = _heartbeatInterval;
                        }
                        _heartbeatTimer.Enabled = true;
                    }
                }
                return result;
            }

            /// <inheritdoc/>
            public override bool TryGetMonitoredItemNotifications(uint sequenceNumber, DateTime timestamp,
                IEncodeable evt, IList<MonitoredItemNotificationModel> notifications)
            {
                _lastValueReceived = DateTime.UtcNow;
                if (_heartbeatTimer != null && (_heartbeatBehavior & HeartbeatBehavior.PeriodicLKV) == 0)
                {
                    _heartbeatTimer.Enabled = false;
                }
                return base.TryGetMonitoredItemNotifications(sequenceNumber, timestamp, evt, notifications);
            }

            /// <summary>
            /// TODO: What is a Good value? Right now we say that it must either be full good or
            /// have a value and not a bad status code (to cover Good_, and Uncertain_ as well)
            /// </summary>
            /// <param name="value"></param>
            /// <returns></returns>
            private static bool IsGoodDataValue(DataValue? value)
            {
                if (value == null)
                {
                    return false;
                }
                return value.StatusCode == StatusCodes.Good ||
                    (value.WrappedValue != Variant.Null && !StatusCode.IsBad(value.StatusCode));
            }

            /// <summary>
            /// Send heartbeat
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
            private void SendHeartbeatNotifications(object? sender, System.Timers.ElapsedEventArgs e)
            {
                var callback = _callback;
                if (callback == null || !Valid)
                {
                    return;
                }

                var lastNotification = LastReceivedValue as MonitoredItemNotification;
                if ((_heartbeatBehavior & HeartbeatBehavior.WatchdogLKG)
                        == HeartbeatBehavior.WatchdogLKG &&
                        !IsGoodDataValue(lastNotification?.Value))
                {
                    // Currently no last known good value (LKG) to send
                    _logger.LogDebug("{Item}: No last known good value to send.", this);
                    return;
                }

                var lastValue = lastNotification?.Value;
                if (lastValue == null && Status?.Error?.StatusCode != null)
                {
                    lastValue = new DataValue(Status.Error.StatusCode);
                }

                if (lastValue == null)
                {
                    // Currently no last known value (LKV) to send
                    _logger.LogDebug("{Item}: No last known value to send.", this);
                    return;
                }
                if ((_heartbeatBehavior & HeartbeatBehavior.WatchdogLKVWithUpdatedTimestamps)
                        == HeartbeatBehavior.WatchdogLKVWithUpdatedTimestamps)
                {
                    // Adjust to the diff between now and received if desired
                    // Should not be possible that last value received is null, nevertheless.
                    var diffTime = _lastValueReceived.HasValue ?
                        DateTime.UtcNow - _lastValueReceived.Value : TimeSpan.Zero;

                    lastValue = new DataValue(lastValue)
                    {
                        SourceTimestamp = lastValue.SourceTimestamp == DateTime.MinValue ?
                            DateTime.MinValue : lastValue.SourceTimestamp.Add(diffTime),
                        ServerTimestamp = lastValue.ServerTimestamp == DateTime.MinValue ?
                            DateTime.MinValue : lastValue.ServerTimestamp.Add(diffTime)
                    };
                }

                // If last value is null create a error value.
                var heartbeat = new MonitoredItemNotificationModel
                {
                    Id = Template.Id,
                    DataSetFieldName = Template.DisplayName,
                    DataSetName = Template.DisplayName,
                    Context = Template.Context,
                    NodeId = TheResolvedNodeId,
                    PathFromRoot = TheResolvedRelativePath,
                    Value = lastValue,
                    Flags = MonitoredItemSourceFlags.Heartbeat,
                    SequenceNumber = 0
                };
                callback(MessageType.DeltaFrame, heartbeat.YieldReturn(),
                    diagnosticsOnly: (_heartbeatBehavior & HeartbeatBehavior.WatchdogLKVDiagnosticsOnly)
                        == HeartbeatBehavior.WatchdogLKVDiagnosticsOnly);
            }

            /// <summary>
            /// Clone the timer
            /// </summary>
            /// <returns></returns>
            private Timer? CloneTimer()
            {
                var timer = _heartbeatTimer;
                _heartbeatTimer = null;
                if (timer != null)
                {
                    timer.Elapsed -= SendHeartbeatNotifications;
                }
                return timer;
            }

            private Timer? _heartbeatTimer;
            private TimeSpan _timerInterval;
            private HeartbeatBehavior _heartbeatBehavior;
            private TimeSpan _heartbeatInterval;
            private Callback? _callback;
            private DateTime? _lastValueReceived;
        }
    }
}
