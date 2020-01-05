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
    using System;
    using System.Collections.Generic;
    using Opc.Ua.Server;

    /// <summary>
    /// Provides a basic monitored item implementation which does not support queuing.
    /// </summary>
    public class DataChangeMonitoredItem : IDataChangeMonitoredItem {

        /// <summary>
        /// Constructs a new instance.
        /// </summary>
        public DataChangeMonitoredItem(
            MonitoredNode source,
            uint id,
            uint attributeId,
            NumericRange indexRange,
            QualifiedName dataEncoding,
            DiagnosticsMasks diagnosticsMasks,
            TimestampsToReturn timestampsToReturn,
            MonitoringMode monitoringMode,
            uint clientHandle,
            double samplingInterval,
            bool alwaysReportUpdates) {
            _source = source;
            Id = id;
            AttributeId = attributeId;
            IndexRange = indexRange;
            DataEncoding = dataEncoding;
            _timestampsToReturn = timestampsToReturn;
            _diagnosticsMasks = diagnosticsMasks;
            MonitoringMode = monitoringMode;
            ClientHandle = clientHandle;
            _samplingInterval = samplingInterval;
            _nextSampleTime = DateTime.UtcNow.Ticks;
            _readyToPublish = false;
            _readyToTrigger = false;
            AlwaysReportUpdates = alwaysReportUpdates;
        }

        /// <summary>
        /// Constructs a new instance.
        /// </summary>
        public DataChangeMonitoredItem(
            MonitoredNode source,
            uint id,
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
            Opc.Ua.Range range,
            bool alwaysReportUpdates) {
            _source = source;
            Id = id;
            AttributeId = attributeId;
            IndexRange = indexRange;
            DataEncoding = dataEncoding;
            _timestampsToReturn = timestampsToReturn;
            _diagnosticsMasks = diagnosticsMasks;
            MonitoringMode = monitoringMode;
            ClientHandle = clientHandle;
            _samplingInterval = samplingInterval;
            _nextSampleTime = DateTime.UtcNow.Ticks;
            _readyToPublish = false;
            _readyToTrigger = false;
            _queue = null;
            DataChangeFilter = filter;
            _range = 0;
            AlwaysReportUpdates = alwaysReportUpdates;

            if (range != null) {
                _range = range.High - range.Low;
            }

            if (queueSize > 1) {
                _queue = new MonitoredItemQueue();
                _queue.SetQueueSize(queueSize, discardOldest, diagnosticsMasks);
                _queue.SetSamplingInterval(samplingInterval);
            }
        }

        /// <summary>
        /// Gets the id for the attribute being monitored.
        /// </summary>
        public uint AttributeId { get; }

        /// <summary>
        /// Gets the index range used to selected a subset of the value.
        /// </summary>
        public NumericRange IndexRange { get; }

        /// <summary>
        /// Gets the data encoding to use when returning the value.
        /// </summary>
        public QualifiedName DataEncoding { get; }

        /// <summary>
        /// Whether the monitored item should report a value without checking if it was changed.
        /// </summary>
        public bool AlwaysReportUpdates { get; set; }

        /// <summary>
        /// The number of milliseconds until the next sample.
        /// </summary>
        public int TimeToNextSample {
            get {
                lock (_lock) {
                    if (MonitoringMode == MonitoringMode.Disabled) {
                        return int.MaxValue;
                    }

                    var now = DateTime.UtcNow;

                    if (_nextSampleTime <= now.Ticks) {
                        return 0;
                    }

                    return (int)((_nextSampleTime - now.Ticks) / TimeSpan.TicksPerMillisecond);
                }
            }
        }

        /// <summary>
        /// The monitoring mode.
        /// </summary>
        public MonitoringMode MonitoringMode { get; private set; }

        /// <summary>
        /// The sampling interval.
        /// </summary>
        public double SamplingInterval {
            get {
                lock (_lock) {
                    return _samplingInterval;
                }
            }
        }

        /// <summary>
        /// Modifies the monitored item parameters,
        /// </summary>
        public ServiceResult Modify(
            DiagnosticsMasks diagnosticsMasks,
            TimestampsToReturn timestampsToReturn,
            uint clientHandle,
            double samplingInterval) {
            return Modify(diagnosticsMasks, timestampsToReturn, clientHandle, samplingInterval, 0, false, null, null);
        }

        /// <summary>
        /// Modifies the monitored item parameters,
        /// </summary>
        public ServiceResult Modify(
            DiagnosticsMasks diagnosticsMasks,
            TimestampsToReturn timestampsToReturn,
            uint clientHandle,
            double samplingInterval,
            uint queueSize,
            bool discardOldest,
            DataChangeFilter filter,
            Opc.Ua.Range range) {
            lock (_lock) {
                _diagnosticsMasks = diagnosticsMasks;
                _timestampsToReturn = timestampsToReturn;
                ClientHandle = clientHandle;

                // subtract the previous sampling interval.
                var oldSamplingInterval = (long)(_samplingInterval * TimeSpan.TicksPerMillisecond);

                if (oldSamplingInterval < _nextSampleTime) {
                    _nextSampleTime -= oldSamplingInterval;
                }

                _samplingInterval = samplingInterval;

                // calculate the next sampling interval.
                var newSamplingInterval = (long)(_samplingInterval * TimeSpan.TicksPerMillisecond);

                if (_samplingInterval > 0) {
                    _nextSampleTime += newSamplingInterval;
                }
                else {
                    _nextSampleTime = 0;
                }

                // update the filter and the range.
                DataChangeFilter = filter;
                _range = 0;

                if (range != null) {
                    _range = range.High - range.Low;
                }

                // update the queue size.
                if (queueSize > 1) {
                    if (_queue == null) {
                        _queue = new MonitoredItemQueue();
                    }

                    _queue.SetQueueSize(queueSize, discardOldest, diagnosticsMasks);
                    _queue.SetSamplingInterval(samplingInterval);
                }
                else {
                    _queue = null;
                }

                return ServiceResult.Good;
            }
        }

        /// <summary>
        /// Called when the attribute being monitored changed. Reads and queues the value.
        /// </summary>
        public void ValueChanged(ISystemContext context) {
            var value = new DataValue();

            var error = _source.Node.ReadAttribute(context, AttributeId, NumericRange.Empty, null, value);

            if (ServiceResult.IsBad(error)) {
                value = new DataValue(error.StatusCode);
            }

            value.ServerTimestamp = DateTime.UtcNow;

            QueueValue(value, error);
        }



        /// <summary>
        /// The node manager for the monitored item.
        /// </summary>
        public INodeManager NodeManager => _source.NodeManager;

        /// <summary>
        /// The session for the monitored item.
        /// </summary>
        public Session Session {
            get {
                var subscription = SubscriptionCallback;

                if (subscription != null) {
                    return subscription.Session;
                }

                return null;
            }
        }

        /// <summary>
        /// The identifier for the subscription that the monitored item belongs to.
        /// </summary>
        public uint SubscriptionId {
            get {
                var subscription = SubscriptionCallback;

                if (subscription != null) {
                    return subscription.Id;
                }

                return 0;
            }
        }

        /// <summary>
        /// The unique identifier for the monitored item.
        /// </summary>
        public uint Id { get; }

        /// <summary>
        /// The client handle.
        /// </summary>
        public uint ClientHandle { get; private set; }

        /// <summary>
        /// The callback to use to notify the subscription when values are ready to publish.
        /// </summary>
        public ISubscription SubscriptionCallback { get; set; }

        /// <summary>
        /// The handle assigned to the monitored item by the node manager.
        /// </summary>
        public object ManagerHandle => _source;

        /// <summary>
        /// The type of monitor item.
        /// </summary>
        public int MonitoredItemType => MonitoredItemTypeMask.DataChange;

        /// <summary>
        /// Returns true if the item is ready to publish.
        /// </summary>
        public bool IsReadyToPublish {
            get {
                lock (_lock) {
                    // check if not ready to publish.
                    if (!_readyToPublish) {
                        return false;
                    }

                    // check if monitoring was turned off.
                    if (MonitoringMode != MonitoringMode.Reporting) {
                        return false;
                    }

                    // re-queue if too little time has passed since the last publish.
                    var now = DateTime.UtcNow.Ticks;

                    if (_nextSampleTime > now) {
                        return false;
                    }

                    return true;
                }
            }
        }

        /// <summary>
        /// Gets or Sets a value indicating whether the item is ready to trigger in case it has some linked items.
        /// </summary>
        public bool IsReadyToTrigger {
            get {
                lock (_lock) {
                    // only allow to trigger if sampling or reporting.
                    if (MonitoringMode == MonitoringMode.Disabled) {
                        return false;
                    }

                    return _readyToTrigger;
                }
            }

            set {
                lock (_lock) {
                    _readyToTrigger = value;
                }
            }
        }

        /// <summary>
        /// Returns the results for the create request.
        /// </summary>
        public ServiceResult GetCreateResult(out MonitoredItemCreateResult result) {
            lock (_lock) {
                result = new MonitoredItemCreateResult {
                    MonitoredItemId = Id,
                    StatusCode = StatusCodes.Good,
                    RevisedSamplingInterval = _samplingInterval,
                    RevisedQueueSize = 0,
                    FilterResult = null
                };

                if (_queue != null) {
                    result.RevisedQueueSize = _queue.QueueSize;
                }

                return ServiceResult.Good;
            }
        }

        /// <summary>
        /// Returns the results for the modify request.
        /// </summary>
        public ServiceResult GetModifyResult(out MonitoredItemModifyResult result) {
            lock (_lock) {
                result = new MonitoredItemModifyResult {
                    StatusCode = StatusCodes.Good,
                    RevisedSamplingInterval = _samplingInterval,
                    RevisedQueueSize = 0,
                    FilterResult = null
                };

                if (_queue != null) {
                    result.RevisedQueueSize = _queue.QueueSize;
                }

                return ServiceResult.Good;
            }
        }

        /// <summary>
        /// Queues a new data change.
        /// </summary>
        public void QueueValue(DataValue value, ServiceResult error) {
            lock (_lock) {
                // check if value has changed.
                if (!AlwaysReportUpdates) {
                    if (!MonitoredItem.ValueChanged(value, error, _lastValue, _lastError, DataChangeFilter, _range)) {
                        return;
                    }
                }

                // make a shallow copy of the value.
                if (value != null) {
                    var copy = new DataValue {
                        WrappedValue = value.WrappedValue,
                        StatusCode = value.StatusCode,
                        SourceTimestamp = value.SourceTimestamp,
                        SourcePicoseconds = value.SourcePicoseconds,
                        ServerTimestamp = value.ServerTimestamp,
                        ServerPicoseconds = value.ServerPicoseconds
                    };

                    value = copy;

                    // ensure the data value matches the error status code.
                    if (error != null && error.StatusCode.Code != 0) {
                        value.StatusCode = error.StatusCode;
                    }
                }

                _lastValue = value;
                _lastError = error;

                // queue value.
                _queue?.QueueValue(value, error);

                // flag the item as ready to publish.
                _readyToPublish = true;
                _readyToTrigger = true;
            }
        }

        /// <summary>
        /// Sets a flag indicating that the semantics for the monitored node have changed.
        /// </summary>
        /// <remarks>
        /// The StatusCode for next value reported by the monitored item will have the SemanticsChanged bit set.
        /// </remarks>
        public void SetSemanticsChanged() {
            lock (_lock) {
                _semanticsChanged = true;
            }
        }

        /// <summary>
        /// Sets a flag indicating that the structure of the monitored node has changed.
        /// </summary>
        /// <remarks>
        /// The StatusCode for next value reported by the monitored item will have the StructureChanged bit set.
        /// </remarks>
        public void SetStructureChanged() {
            lock (_lock) {
                _structureChanged = true;
            }
        }

        /// <summary>
        /// Changes the monitoring mode.
        /// </summary>
        public MonitoringMode SetMonitoringMode(MonitoringMode monitoringMode) {
            lock (_lock) {
                var previousMode = MonitoringMode;

                if (previousMode == monitoringMode) {
                    return previousMode;
                }

                if (previousMode == MonitoringMode.Disabled) {
                    _nextSampleTime = DateTime.UtcNow.Ticks;
                    _lastError = null;
                    _lastValue = null;
                }

                MonitoringMode = monitoringMode;

                if (monitoringMode == MonitoringMode.Disabled) {
                    _readyToPublish = false;
                    _readyToTrigger = false;
                }

                return previousMode;
            }
        }

        /// <summary>
        /// No filters supported.
        /// </summary>
        public DataChangeFilter DataChangeFilter { get; private set; }

        /// <summary>
        /// Increments the sample time to the next interval.
        /// </summary>
        private void IncrementSampleTime() {
            // update next sample time.
            var now = DateTime.UtcNow.Ticks;
            var samplingInterval = (long)(_samplingInterval * TimeSpan.TicksPerMillisecond);

            if (_nextSampleTime > 0) {
                var delta = now - _nextSampleTime;

                if (samplingInterval > 0 && delta >= 0) {
                    _nextSampleTime += ((delta / samplingInterval) + 1) * samplingInterval;
                }
            }

            // set sampling time based on current time.
            else {
                _nextSampleTime = now + samplingInterval;
            }
        }

        /// <summary>
        /// Called by the subscription to publish any notification.
        /// </summary>
        public bool Publish(OperationContext context, Queue<MonitoredItemNotification> notifications, Queue<DiagnosticInfo> diagnostics) {
            lock (_lock) {
                // check if not ready to publish.
                if (!IsReadyToPublish) {
                    return false;
                }

                // update sample time.
                IncrementSampleTime();

                // update publish flag.
                _readyToPublish = false;
                _readyToTrigger = false;

                // check if queuing is enabled.
                if (_queue == null) {
                    Publish(context, _lastValue, _lastError, notifications, diagnostics);
                }
                else {
                    DataValue value = null;
                    ServiceResult error = null;

                    while (_queue.Publish(out value, out error)) {
                        Publish(context, value, error, notifications, diagnostics);
                    }
                }

                return true;
            }
        }

        /// <summary>
        /// Publishes a value.
        /// </summary>
        private void Publish(
            OperationContext context,
            DataValue value,
            ServiceResult error,
            Queue<MonitoredItemNotification> notifications,
            Queue<DiagnosticInfo> diagnostics) {
            // set semantics changed bit.
            if (_semanticsChanged) {
                if (value != null) {
                    value.StatusCode = value.StatusCode.SetSemanticsChanged(true);
                }

                if (error != null) {
                    error = new ServiceResult(
                        error.StatusCode.SetSemanticsChanged(true),
                        error.SymbolicId,
                        error.NamespaceUri,
                        error.LocalizedText,
                        error.AdditionalInfo,
                        error.InnerResult);
                }

                _semanticsChanged = false;
            }

            // set structure changed bit.
            if (_structureChanged) {
                if (value != null) {
                    value.StatusCode = value.StatusCode.SetStructureChanged(true);
                }

                if (error != null) {
                    error = new ServiceResult(
                        error.StatusCode.SetStructureChanged(true),
                        error.SymbolicId,
                        error.NamespaceUri,
                        error.LocalizedText,
                        error.AdditionalInfo,
                        error.InnerResult);
                }

                _structureChanged = false;
            }

            // copy data value.
            var item = new MonitoredItemNotification {
                ClientHandle = ClientHandle,
                Value = value
            };

            // apply timestamp filter.
            if (_timestampsToReturn != TimestampsToReturn.Server && _timestampsToReturn != TimestampsToReturn.Both) {
                item.Value.ServerTimestamp = DateTime.MinValue;
            }

            if (_timestampsToReturn != TimestampsToReturn.Source && _timestampsToReturn != TimestampsToReturn.Both) {
                item.Value.SourceTimestamp = DateTime.MinValue;
            }

            notifications.Enqueue(item);

            // update diagnostic info.
            DiagnosticInfo diagnosticInfo = null;

            if (_lastError != null) {
                if ((_diagnosticsMasks & DiagnosticsMasks.OperationAll) != 0) {
                    diagnosticInfo = ServerUtils.CreateDiagnosticInfo(_source.Server, context, _lastError);
                }
            }

            diagnostics.Enqueue(diagnosticInfo);
        }

        private readonly object _lock = new object();
        private readonly MonitoredNode _source;
        private DataValue _lastValue;
        private ServiceResult _lastError;
        private TimestampsToReturn _timestampsToReturn;
        private DiagnosticsMasks _diagnosticsMasks;
        private double _samplingInterval;
        private MonitoredItemQueue _queue;
        private double _range;
        private long _nextSampleTime;
        private bool _readyToPublish;
        private bool _readyToTrigger;
        private bool _semanticsChanged;
        private bool _structureChanged;
    }
}
