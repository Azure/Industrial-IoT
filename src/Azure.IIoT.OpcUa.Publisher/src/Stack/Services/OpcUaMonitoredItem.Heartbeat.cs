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
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Threading;
    using System.Threading.Tasks;

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
            /// Whether timer is enabled
            /// </summary>
            public bool TimerEnabled { get; set; }

            /// <summary>
            /// Create data item with heartbeat
            /// </summary>
            /// <param name="subscription"></param>
            /// <param name="owner"></param>
            /// <param name="dataTemplate"></param>
            /// <param name="session"></param>
            /// <param name="logger"></param>
            /// <param name="timeProvider"></param>
            public Heartbeat(IMonitoredItemContext subscription, ISubscriber owner,
                DataMonitoredItemModel dataTemplate, IOpcUaSession session,
                ILogger<DataChange> logger, TimeProvider timeProvider) :
                base(subscription, owner, dataTemplate, session, logger, timeProvider)
            {
                _heartbeatInterval = dataTemplate.HeartbeatInterval
                    ?? dataTemplate.SamplingInterval ?? TimeSpan.FromSeconds(1);
                _heartbeatBehavior = dataTemplate.HeartbeatBehavior
                    ?? HeartbeatBehavior.WatchdogLKV;
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
                var str = $"Data Item '{Template.StartNodeId}' " +
                    $"(with {Template.HeartbeatBehavior ?? HeartbeatBehavior.WatchdogLKV} Heartbeat) ";
                if (RemoteId.HasValue)
                {
                    str += $" with server id {RemoteId} ({(Created ? "" : "not ")}created)";
                }
                return str;
            }

            /// <inheritdoc/>
            protected override ValueTask DisposeAsync(bool disposing)
            {
                if (disposing)
                {
                    lock (_timerLock)
                    {
                        _disposed = true;
                        if (_heartbeatTimer != null)
                        {
                            _heartbeatTimer.Elapsed -= SendHeartbeatNotifications;
                            _heartbeatTimer.Dispose();
                            _heartbeatTimer = null;
                        }
                    }
                }
                return base.DisposeAsync(disposing);
            }

            /// <inheritdoc/>
            protected override bool ProcessMonitoredItemNotification(DateTimeOffset publishTime,
                MonitoredItemNotification monitoredItemNotification,
                MonitoredItemNotifications notifications)
            {
                Debug.Assert(!Disposed);
                var result = base.ProcessMonitoredItemNotification(publishTime,
                    monitoredItemNotification, notifications);

                if (!_disposed && (_heartbeatBehavior & HeartbeatBehavior.PeriodicLKV) == 0)
                {
                    EnableHeartbeatTimer();
                }
                return result;
            }

            /// <inheritdoc/>
            public override bool MergeWith(OpcUaMonitoredItem item, out bool metadataChanged)
            {
                metadataChanged = false;
                if (item is not Heartbeat model || Disposed)
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

                itemChange |= base.MergeWith(model, out metadataChanged);
                return itemChange;
            }

            /// <inheritdoc/>
            public override bool TryCompleteChanges(ref bool applyChanges)
            {
                var result = base.TryCompleteChanges(ref applyChanges);
                var lkg = (_heartbeatBehavior & HeartbeatBehavior.WatchdogLKG)
                        == HeartbeatBehavior.WatchdogLKG;
                if (!result && lkg)
                {
                    // Stop heartbeat
                    DisableHeartbeatTimer();
                }
                else
                {
                    EnableHeartbeatTimer();
                }
                return result;
            }

            /// <inheritdoc/>
            public override bool TryGetMonitoredItemNotifications(DateTimeOffset publishTime,
                IEncodeable evt, MonitoredItemNotifications notifications)
            {
                _lastSequenceNumber = GetNextSequenceNumber();
                if (!_disposed && (_heartbeatBehavior & HeartbeatBehavior.PeriodicLKV) == 0)
                {
                    EnableHeartbeatTimer();
                }
                return base.TryGetMonitoredItemNotifications(publishTime, evt, notifications);
            }

            /// <inheritdoc/>
            public override bool SkipMonitoredItemNotification()
            {
                var dropValue = (_heartbeatBehavior & HeartbeatBehavior.Reserved) != 0;
                return dropValue || base.SkipMonitoredItemNotification();
            }

            /// <inheritdoc/>
            public override void NotifySessionConnectionState(bool disconnected)
            {
                //
                // We change the reference here - we cloned the value and if it has been
                // updated while we are doing this, a new value will be in in place and we
                // should be connected again or we would not have received it.
                //
                var lastValue = LastReceivedValue as MonitoredItemNotification;
                if (lastValue?.Value != null)
                {
                    if (disconnected)
                    {
                        _lastStatusCode = lastValue.Value.StatusCode;
                        if (IsGoodDataValue(lastValue.Value))
                        {
                            lastValue.Value.StatusCode =
                                StatusCodes.UncertainNoCommunicationLastUsableValue;
                        }
                        else
                        {
                            lastValue.Value.StatusCode =
                                StatusCodes.BadNoCommunication;
                        }
                    }
                    else if (_lastStatusCode.HasValue)
                    {
                        lastValue.Value.StatusCode = _lastStatusCode.Value;
                        _lastStatusCode = null; // This is safe as we are called from the client thread
                    }
                }
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
            private void SendHeartbeatNotifications(object? sender, ElapsedEventArgs e)
            {
                if (Disposed)
                {
                    return;
                }

                var lastSequenceNumber = _lastSequenceNumber;
                var lastNotification = LastReceivedValue as MonitoredItemNotification;
                if ((_heartbeatBehavior & HeartbeatBehavior.WatchdogLKG)
                        == HeartbeatBehavior.WatchdogLKG &&
                        !IsGoodDataValue(lastNotification?.Value))
                {
                    // Currently no last known good value (LKG) to send
                    _logger.LogInformation("{Item}: No last known good value to send.", this);
                    return;
                }

                var lastValue = lastNotification?.Value;
                if (lastValue == null && ServiceResult.IsNotGood(Error))
                {
                    lastValue = new DataValue(Error.StatusCode);
                }

                if (lastValue == null)
                {
                    // Currently no last known value (LKV) to send
                    _logger.LogInformation("{Item}: No last known value to send.", this);
                    return;
                }
                if ((_heartbeatBehavior & HeartbeatBehavior.WatchdogLKVWithUpdatedTimestamps)
                        == HeartbeatBehavior.WatchdogLKVWithUpdatedTimestamps)
                {
                    // Adjust to the diff between now and received if desired
                    // Should not be possible that last value received is null, nevertheless.
                    var diffTime = LastReceivedTime.HasValue ?
                        e.SignalTime - LastReceivedTime.Value : TimeSpan.Zero;

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
                    NodeId = TheResolvedNodeId,
                    PathFromRoot = TheResolvedRelativePath,
                    Value = lastValue,
                    Flags = MonitoredItemSourceFlags.Heartbeat,
                    SequenceNumber = lastSequenceNumber
                };
                if (lastSequenceNumber != _lastSequenceNumber)
                {
                    // New value came in while running the timer callback - no need to send heartbeat
                    return;
                }
                Publish(Owner, MessageType.DeltaFrame, heartbeat.YieldReturn().ToList(),
                    diagnosticsOnly: (_heartbeatBehavior & HeartbeatBehavior.WatchdogLKVDiagnosticsOnly)
                        == HeartbeatBehavior.WatchdogLKVDiagnosticsOnly, timestamp: e.SignalTime);
            }

            /// <summary>
            /// Enable timer
            /// </summary>
            private void EnableHeartbeatTimer()
            {
                lock (_timerLock)
                {
                    if (_disposed)
                    {
                        return;
                    }
                    if (_heartbeatTimer == null)
                    {
                        _heartbeatTimer = new(TimeProvider)
                        {
                            AutoReset = true
                        };
                        _heartbeatTimer.Elapsed += SendHeartbeatNotifications;
                        _logger.LogInformation("Re-enable heartbeat timer");
                    }
                    _heartbeatTimer.Interval = _heartbeatInterval;
                    _heartbeatTimer.Enabled = true;
                    TimerEnabled = true;
                }
            }

            /// <summary>
            /// Disable timer
            /// </summary>
            private void DisableHeartbeatTimer()
            {
                lock (_timerLock)
                {
                    if (_heartbeatTimer != null)
                    {
                        _heartbeatTimer.Elapsed -= SendHeartbeatNotifications;
                        _heartbeatTimer.Dispose();
                        _heartbeatTimer = null;
                        _logger.LogDebug("Disabled heartbeat timer");
                    }
                    TimerEnabled = false;
                }
            }

            private TimerEx? _heartbeatTimer;
            private HeartbeatBehavior _heartbeatBehavior;
            private TimeSpan _heartbeatInterval;
            private StatusCode? _lastStatusCode;
            private uint _lastSequenceNumber;
            private readonly Lock _timerLock = new();
            private bool _disposed;
        }
    }
}
