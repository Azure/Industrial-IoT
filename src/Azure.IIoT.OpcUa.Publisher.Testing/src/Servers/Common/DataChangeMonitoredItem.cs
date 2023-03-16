/* ========================================================================
 * Copyright (c) 2005-2019 The OPC Foundation, Inc. All rights reserved.
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

using Opc.Ua.Server;
using System;
using System.Collections.Generic;

namespace Opc.Ua.Sample
{
    /// <summary>
    /// Provides a basic monitored item implementation which does not support queuing.
    /// </summary>
    public class DataChangeMonitoredItem : IDataChangeMonitoredItem2
    {
        #region Constructors
        /// <summary>
        /// Constructs a new instance.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="id"></param>
        /// <param name="attributeId"></param>
        /// <param name="indexRange"></param>
        /// <param name="dataEncoding"></param>
        /// <param name="diagnosticsMasks"></param>
        /// <param name="timestampsToReturn"></param>
        /// <param name="monitoringMode"></param>
        /// <param name="clientHandle"></param>
        /// <param name="samplingInterval"></param>
        /// <param name="alwaysReportUpdates"></param>
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
            bool alwaysReportUpdates)
        {
            m_source = source;
            Id = id;
            AttributeId = attributeId;
            m_indexRange = indexRange;
            DataEncoding = dataEncoding;
            m_timestampsToReturn = timestampsToReturn;
            m_diagnosticsMasks = diagnosticsMasks;
            MonitoringMode = monitoringMode;
            ClientHandle = clientHandle;
            m_samplingInterval = samplingInterval;
            m_nextSampleTime = DateTime.UtcNow.Ticks;
            m_readyToPublish = false;
            m_readyToTrigger = false;
            m_resendData = false;
            AlwaysReportUpdates = alwaysReportUpdates;
        }

        /// <summary>
        /// Constructs a new instance.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="id"></param>
        /// <param name="attributeId"></param>
        /// <param name="indexRange"></param>
        /// <param name="dataEncoding"></param>
        /// <param name="diagnosticsMasks"></param>
        /// <param name="timestampsToReturn"></param>
        /// <param name="monitoringMode"></param>
        /// <param name="clientHandle"></param>
        /// <param name="samplingInterval"></param>
        /// <param name="queueSize"></param>
        /// <param name="discardOldest"></param>
        /// <param name="filter"></param>
        /// <param name="range"></param>
        /// <param name="alwaysReportUpdates"></param>
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
            Range range,
            bool alwaysReportUpdates)
        {
            m_source = source;
            Id = id;
            AttributeId = attributeId;
            m_indexRange = indexRange;
            DataEncoding = dataEncoding;
            m_timestampsToReturn = timestampsToReturn;
            m_diagnosticsMasks = diagnosticsMasks;
            MonitoringMode = monitoringMode;
            ClientHandle = clientHandle;
            m_samplingInterval = samplingInterval;
            m_nextSampleTime = DateTime.UtcNow.Ticks;
            m_readyToPublish = false;
            m_readyToTrigger = false;
            m_resendData = false;
            m_queue = null;
            DataChangeFilter = filter;
            m_range = 0;
            AlwaysReportUpdates = alwaysReportUpdates;

            if (range != null)
            {
                m_range = range.High - range.Low;
            }

            if (queueSize > 1)
            {
                m_queue = new MonitoredItemQueue(id);
                m_queue.SetQueueSize(queueSize, discardOldest, diagnosticsMasks);
                m_queue.SetSamplingInterval(samplingInterval);
            }
        }
        #endregion

        #region Public Members
        /// <summary>
        /// Gets the id for the attribute being monitored.
        /// </summary>
        public uint AttributeId { get; }

        /// <summary>
        /// Gets the index range used to selected a subset of the value.
        /// </summary>
        public NumericRange IndexRange => m_indexRange;

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
        public int TimeToNextSample
        {
            get
            {
                lock (m_lock)
                {
                    if (MonitoringMode == MonitoringMode.Disabled)
                    {
                        return Int32.MaxValue;
                    }

                    var now = DateTime.UtcNow;

                    if (m_nextSampleTime <= now.Ticks)
                    {
                        return 0;
                    }

                    return (int)((m_nextSampleTime - now.Ticks) / TimeSpan.TicksPerMillisecond);
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
        public double SamplingInterval
        {
            get
            {
                lock (m_lock)
                {
                    return m_samplingInterval;
                }
            }
        }

        /// <summary>
        /// Modifies the monitored item parameters,
        /// </summary>
        /// <param name="diagnosticsMasks"></param>
        /// <param name="timestampsToReturn"></param>
        /// <param name="clientHandle"></param>
        /// <param name="samplingInterval"></param>
        public ServiceResult Modify(
            DiagnosticsMasks diagnosticsMasks,
            TimestampsToReturn timestampsToReturn,
            uint clientHandle,
            double samplingInterval)
        {
            return Modify(diagnosticsMasks, timestampsToReturn, clientHandle, samplingInterval, 0, false, null, null);
        }

        /// <summary>
        /// Modifies the monitored item parameters,
        /// </summary>
        /// <param name="diagnosticsMasks"></param>
        /// <param name="timestampsToReturn"></param>
        /// <param name="clientHandle"></param>
        /// <param name="samplingInterval"></param>
        /// <param name="queueSize"></param>
        /// <param name="discardOldest"></param>
        /// <param name="filter"></param>
        /// <param name="range"></param>
        public ServiceResult Modify(
            DiagnosticsMasks diagnosticsMasks,
            TimestampsToReturn timestampsToReturn,
            uint clientHandle,
            double samplingInterval,
            uint queueSize,
            bool discardOldest,
            DataChangeFilter filter,
            Range range)
        {
            lock (m_lock)
            {
                m_diagnosticsMasks = diagnosticsMasks;
                m_timestampsToReturn = timestampsToReturn;
                ClientHandle = clientHandle;

                // subtract the previous sampling interval.
                var oldSamplingInterval = (long)(m_samplingInterval * TimeSpan.TicksPerMillisecond);

                if (oldSamplingInterval < m_nextSampleTime)
                {
                    m_nextSampleTime -= oldSamplingInterval;
                }

                m_samplingInterval = samplingInterval;

                // calculate the next sampling interval.                
                var newSamplingInterval = (long)(m_samplingInterval * TimeSpan.TicksPerMillisecond);

                if (m_samplingInterval > 0)
                {
                    m_nextSampleTime += newSamplingInterval;
                }
                else
                {
                    m_nextSampleTime = 0;
                }

                // update the filter and the range.
                DataChangeFilter = filter;
                m_range = 0;

                if (range != null)
                {
                    m_range = range.High - range.Low;
                }

                // update the queue size.
                if (queueSize > 1)
                {
                    m_queue ??= new MonitoredItemQueue(Id);

                    m_queue.SetQueueSize(queueSize, discardOldest, diagnosticsMasks);
                    m_queue.SetSamplingInterval(samplingInterval);
                }
                else
                {
                    m_queue = null;
                }

                return ServiceResult.Good;
            }
        }

        /// <summary>
        /// Called when the attribute being monitored changed. Reads and queues the value.
        /// </summary>
        /// <param name="context"></param>
        public void ValueChanged(ISystemContext context)
        {
            var value = new DataValue();

            var error = m_source.Node.ReadAttribute(context, AttributeId, NumericRange.Empty, null, value);

            if (ServiceResult.IsBad(error))
            {
                value = new DataValue(error.StatusCode);
            }

            value.ServerTimestamp = DateTime.UtcNow;

            QueueValue(value, error, false);
        }
        #endregion

        #region IMonitoredItem Members
        /// <summary>
        /// The node manager for the monitored item.
        /// </summary>
        public INodeManager NodeManager => m_source.NodeManager;

        /// <summary>
        /// The session for the monitored item.
        /// </summary>
        public Session Session
        {
            get
            {
                var subscription = SubscriptionCallback;

                if (subscription != null)
                {
                    return subscription.Session;
                }

                return null;
            }
        }

        /// <summary>
        /// The identifier for the subscription that the monitored item belongs to.
        /// </summary>
        public uint SubscriptionId
        {
            get
            {
                var subscription = SubscriptionCallback;

                if (subscription != null)
                {
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
        public object ManagerHandle => m_source;

        /// <summary>
        /// The type of monitor item.
        /// </summary>
        public int MonitoredItemType => MonitoredItemTypeMask.DataChange;

        /// <summary>
        /// Returns true if the item is ready to publish.
        /// </summary>
        public bool IsReadyToPublish
        {
            get
            {
                lock (m_lock)
                {
                    // check if not ready to publish.
                    if (!m_readyToPublish)
                    {
                        return false;
                    }

                    // check if monitoring was turned off.
                    if (MonitoringMode != MonitoringMode.Reporting)
                    {
                        return false;
                    }

                    // re-queue if too little time has passed since the last publish.
                    var now = DateTime.UtcNow.Ticks;

                    if (m_nextSampleTime > now)
                    {
                        return false;
                    }

                    return true;
                }
            }
        }

        /// <summary>
        /// Gets or Sets a value indicating whether the item is ready to trigger in case it has some linked items.
        /// </summary>
        public bool IsReadyToTrigger
        {
            get
            {
                lock (m_lock)
                {
                    // only allow to trigger if sampling or reporting.
                    if (MonitoringMode == MonitoringMode.Disabled)
                    {
                        return false;
                    }

                    return m_readyToTrigger;
                }
            }

            set
            {
                lock (m_lock)
                {
                    m_readyToTrigger = value;
                }
            }
        }

        /// <inheritdoc/>
        public bool IsResendData
        {
            get
            {
                lock (m_lock)
                {
                    return m_resendData;
                }
            }
        }

        /// <summary>
        /// Returns the results for the create request.
        /// </summary>
        /// <param name="result"></param>
        public ServiceResult GetCreateResult(out MonitoredItemCreateResult result)
        {
            lock (m_lock)
            {
                result = new MonitoredItemCreateResult();

                result.MonitoredItemId = Id;
                result.StatusCode = StatusCodes.Good;
                result.RevisedSamplingInterval = m_samplingInterval;
                result.RevisedQueueSize = 0;
                result.FilterResult = null;

                if (m_queue != null)
                {
                    result.RevisedQueueSize = m_queue.QueueSize;
                }

                return ServiceResult.Good;
            }
        }

        /// <summary>
        /// Returns the results for the modify request.
        /// </summary>
        /// <param name="result"></param>
        public ServiceResult GetModifyResult(out MonitoredItemModifyResult result)
        {
            lock (m_lock)
            {
                result = new MonitoredItemModifyResult();

                result.StatusCode = StatusCodes.Good;
                result.RevisedSamplingInterval = m_samplingInterval;
                result.RevisedQueueSize = 0;
                result.FilterResult = null;

                if (m_queue != null)
                {
                    result.RevisedQueueSize = m_queue.QueueSize;
                }

                return ServiceResult.Good;
            }
        }

        /// <inheritdoc/>
        public void SetupResendDataTrigger()
        {
            lock (m_lock)
            {
                if (MonitoringMode == MonitoringMode.Reporting)
                {
                    m_resendData = true;
                }
            }
        }
        #endregion

        #region IDataChangeMonitoredItem Members
        /// <summary>
        /// Queues a new data change.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="error"></param>
        public void QueueValue(DataValue value, ServiceResult error)
        {
            QueueValue(value, error, false);
        }
        #endregion

        #region IDataChangeMonitoredItem2 Members
        /// <summary>
        /// Queues a new data change.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="error"></param>
        /// <param name="ignoreFilters"></param>
        public void QueueValue(DataValue value, ServiceResult error, bool ignoreFilters)
        {
            lock (m_lock)
            {
                // check if value has changed.
                if (!AlwaysReportUpdates && !Opc.Ua.Server.MonitoredItem.ValueChanged(value, error, m_lastValue, m_lastError, DataChangeFilter, m_range))
                {
                    return;
                }

                // make a shallow copy of the value.
                if (value != null)
                {
                    var copy = new DataValue();

                    copy.WrappedValue = value.WrappedValue;
                    copy.StatusCode = value.StatusCode;
                    copy.SourceTimestamp = value.SourceTimestamp;
                    copy.SourcePicoseconds = value.SourcePicoseconds;
                    copy.ServerTimestamp = value.ServerTimestamp;
                    copy.ServerPicoseconds = value.ServerPicoseconds;

                    value = copy;

                    // ensure the data value matches the error status code.
                    if (error != null && error.StatusCode.Code != 0)
                    {
                        value.StatusCode = error.StatusCode;
                    }
                }

                m_lastValue = value;
                m_lastError = error;

                // queue value.
                m_queue?.QueueValue(value, error);

                // flag the item as ready to publish.
                m_readyToPublish = true;
                m_readyToTrigger = true;
            }
        }

        /// <summary>
        /// Sets a flag indicating that the semantics for the monitored node have changed.
        /// </summary>
        /// <remarks>
        /// The StatusCode for next value reported by the monitored item will have the SemanticsChanged bit set.
        /// </remarks>
        public void SetSemanticsChanged()
        {
            lock (m_lock)
            {
                m_semanticsChanged = true;
            }
        }

        /// <summary>
        /// Sets a flag indicating that the structure of the monitored node has changed.
        /// </summary>
        /// <remarks>
        /// The StatusCode for next value reported by the monitored item will have the StructureChanged bit set.
        /// </remarks>
        public void SetStructureChanged()
        {
            lock (m_lock)
            {
                m_structureChanged = true;
            }
        }

        /// <summary>
        /// Changes the monitoring mode.
        /// </summary>
        /// <param name="monitoringMode"></param>
        public MonitoringMode SetMonitoringMode(MonitoringMode monitoringMode)
        {
            lock (m_lock)
            {
                var previousMode = MonitoringMode;

                if (previousMode == monitoringMode)
                {
                    return previousMode;
                }

                if (previousMode == MonitoringMode.Disabled)
                {
                    m_nextSampleTime = DateTime.UtcNow.Ticks;
                    m_lastError = null;
                    m_lastValue = null;
                }

                MonitoringMode = monitoringMode;

                if (monitoringMode == MonitoringMode.Disabled)
                {
                    m_readyToPublish = false;
                    m_readyToTrigger = false;
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
        private void IncrementSampleTime()
        {
            // update next sample time.
            var now = DateTime.UtcNow.Ticks;
            var samplingInterval = (long)(m_samplingInterval * TimeSpan.TicksPerMillisecond);

            if (m_nextSampleTime > 0)
            {
                var delta = now - m_nextSampleTime;

                if (samplingInterval > 0 && delta >= 0)
                {
                    m_nextSampleTime += ((delta / samplingInterval) + 1) * samplingInterval;
                }
            }

            // set sampling time based on current time.
            else
            {
                m_nextSampleTime = now + samplingInterval;
            }
        }

        /// <summary>
        /// Called by the subscription to publish any notification.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="notifications"></param>
        /// <param name="diagnostics"></param>
        public bool Publish(OperationContext context, Queue<MonitoredItemNotification> notifications, Queue<DiagnosticInfo> diagnostics)
        {
            lock (m_lock)
            {
                // check if not ready to publish.
                if (!IsReadyToPublish)
                {
                    return false;
                }

                // update sample time.
                IncrementSampleTime();

                // update publish flag.
                m_readyToPublish = false;
                m_readyToTrigger = false;

                // check if queuing is enabled.
                if (m_queue != null && (!m_resendData || m_queue.ItemsInQueue != 0))
                {
                    DataValue value = null;
                    ServiceResult error = null;

                    while (m_queue.Publish(out value, out error))
                    {
                        Publish(context, value, error, notifications, diagnostics);

                        if (m_resendData)
                        {
                            m_readyToPublish = m_queue.ItemsInQueue > 0;
                            break;
                        }
                    }
                }
                else
                {
                    Publish(context, m_lastValue, m_lastError, notifications, diagnostics);
                }

                // update flags
                m_resendData = false;

                return true;
            }
        }

        /// <summary>
        /// Publishes a value.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="value"></param>
        /// <param name="error"></param>
        /// <param name="notifications"></param>
        /// <param name="diagnostics"></param>
        private void Publish(
            OperationContext context,
            DataValue value,
            ServiceResult error,
            Queue<MonitoredItemNotification> notifications,
            Queue<DiagnosticInfo> diagnostics)
        {
            // set semantics changed bit.
            if (m_semanticsChanged)
            {
                if (value != null)
                {
                    value.StatusCode = value.StatusCode.SetSemanticsChanged(true);
                }

                if (error != null)
                {
                    error = new ServiceResult(
                        error.StatusCode.SetSemanticsChanged(true),
                        error.SymbolicId,
                        error.NamespaceUri,
                        error.LocalizedText,
                        error.AdditionalInfo,
                        error.InnerResult);
                }

                m_semanticsChanged = false;
            }

            // set structure changed bit.
            if (m_structureChanged)
            {
                if (value != null)
                {
                    value.StatusCode = value.StatusCode.SetStructureChanged(true);
                }

                if (error != null)
                {
                    error = new ServiceResult(
                        error.StatusCode.SetStructureChanged(true),
                        error.SymbolicId,
                        error.NamespaceUri,
                        error.LocalizedText,
                        error.AdditionalInfo,
                        error.InnerResult);
                }

                m_structureChanged = false;
            }

            // copy data value.
            var item = new MonitoredItemNotification();

            item.ClientHandle = ClientHandle;
            item.Value = value;

            // apply timestamp filter.
            if (m_timestampsToReturn != TimestampsToReturn.Server && m_timestampsToReturn != TimestampsToReturn.Both)
            {
                item.Value.ServerTimestamp = DateTime.MinValue;
            }

            if (m_timestampsToReturn != TimestampsToReturn.Source && m_timestampsToReturn != TimestampsToReturn.Both)
            {
                item.Value.SourceTimestamp = DateTime.MinValue;
            }

            notifications.Enqueue(item);

            // update diagnostic info.
            DiagnosticInfo diagnosticInfo = null;

            if (m_lastError != null && (m_diagnosticsMasks & DiagnosticsMasks.OperationAll) != 0)
            {
                diagnosticInfo = ServerUtils.CreateDiagnosticInfo(m_source.Server, context, m_lastError);
            }

            diagnostics.Enqueue(diagnosticInfo);
        }
        #endregion

        #region Private Fields
        private readonly object m_lock = new();
        private readonly MonitoredNode m_source;
        private DataValue m_lastValue;
        private ServiceResult m_lastError;
        private NumericRange m_indexRange;
        private TimestampsToReturn m_timestampsToReturn;
        private DiagnosticsMasks m_diagnosticsMasks;
        private double m_samplingInterval;
        private MonitoredItemQueue m_queue;
        private double m_range;
        private long m_nextSampleTime;
        private bool m_readyToPublish;
        private bool m_readyToTrigger;
        private bool m_semanticsChanged;
        private bool m_structureChanged;
        private bool m_resendData;
        #endregion
    }
}
