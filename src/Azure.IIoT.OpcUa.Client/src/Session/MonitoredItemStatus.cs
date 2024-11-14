// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation, The OPC Foundation, Inc.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client
{
    using System;

    /// <summary>
    /// The current status of monitored item.
    /// </summary>
    public sealed class MonitoredItemStatus
    {
        /// <summary>
        /// The identifier assigned by the server.
        /// </summary>
        public uint Id { get; set; }

        /// <summary>
        /// Whether the item has been created on the server.
        /// </summary>
        public bool Created => Id != 0;

        /// <summary>
        /// Any error condition associated with the monitored item.
        /// </summary>
        public ServiceResult Error { get; private set; }

        /// <summary>
        /// Filter result
        /// </summary>
        public MonitoringFilterResult? FilterResult { get; private set; }

        /// <summary>
        /// The monitoring mode.
        /// </summary>
        public MonitoringMode MonitoringMode { get; private set; }

        /// <summary>
        /// The sampling interval.
        /// </summary>
        public TimeSpan SamplingInterval { get; private set; }

        /// <summary>
        /// The length of the queue used to buffer values.
        /// </summary>
        public uint QueueSize { get; private set; }
#if ZOMBIE

        /// <summary>
        /// The length of the queue used to buffer values.
        /// </summary>
        public uint ClientHandle => _clientHandle;
#endif

        /// <summary>
        /// Creates a empty object.
        /// </summary>
        internal MonitoredItemStatus()
        {
            MonitoringMode = MonitoringMode.Disabled;
            Error = ServiceResult.Good;
        }

        /// <summary>
        /// Updates the monitoring mode.
        /// </summary>
        /// <param name="monitoringMode"></param>
        internal void SetMonitoringMode(MonitoringMode monitoringMode)
        {
            MonitoringMode = monitoringMode;
        }

        /// <summary>
        /// Updates the object with the results of a create monitored
        /// item request.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="result"></param>
        /// <param name="error"></param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="request"/> is <c>null</c>.</exception>
        internal void SetCreateResult(MonitoredItemCreateRequest request,
            MonitoredItemCreateResult result, ServiceResult error)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(result);

            MonitoringMode = request.MonitoringMode;
            SamplingInterval = TimeSpan.FromMilliseconds(
                request.RequestedParameters.SamplingInterval);
            _clientHandle = request.RequestedParameters.ClientHandle;
            QueueSize = request.RequestedParameters.QueueSize;
            Error = error;

            if (ServiceResult.IsGood(error))
            {
                Id = result.MonitoredItemId;
                SamplingInterval =
                    TimeSpan.FromMilliseconds(result.RevisedSamplingInterval);
                QueueSize = result.RevisedQueueSize;

                if (result.FilterResult != null)
                {
                    FilterResult = Utils.Clone(result.FilterResult.Body)
                        as MonitoringFilterResult;
                }
            }
        }

        /// <summary>
        /// Updates the object with the results of a transfer monitored
        /// item request.
        /// </summary>
        /// <param name="monitoredItem"></param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="monitoredItem"/> is <c>null</c>.</exception>
        internal void SetTransferResult(MonitoredItem monitoredItem)
        {
            ArgumentNullException.ThrowIfNull(monitoredItem);

            MonitoringMode = monitoredItem.MonitoringMode;
            _clientHandle = monitoredItem.ClientHandle;
            SamplingInterval = monitoredItem.SamplingInterval;
            QueueSize = monitoredItem.QueueSize;
            FilterResult = null;
        }

        /// <summary>
        /// Updates the object with the results of a modify monitored
        /// item request.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="result"></param>
        /// <param name="error"></param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="request"/> is <c>null</c>.</exception>
        internal void SetModifyResult(MonitoredItemModifyRequest request,
            MonitoredItemModifyResult result, ServiceResult error)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(result);

            Error = error;

            if (ServiceResult.IsGood(error))
            {
                _clientHandle = request.RequestedParameters.ClientHandle;
                SamplingInterval = TimeSpan.FromMilliseconds(
                    request.RequestedParameters.SamplingInterval);
                QueueSize = request.RequestedParameters.QueueSize;

                SamplingInterval = TimeSpan.FromMilliseconds(
                    result.RevisedSamplingInterval);
                QueueSize = result.RevisedQueueSize;

                if (result.FilterResult != null)
                {
                    FilterResult = Utils.Clone(result.FilterResult.Body)
                        as MonitoringFilterResult;
                }
            }
        }

        /// <summary>
        /// Updates the object with the results of a delete item request.
        /// </summary>
        /// <param name="error"></param>
        internal void SetDeleteResult(ServiceResult error)
        {
            Id = 0;
            Error = error;
        }

        private uint _clientHandle;
    }
}
