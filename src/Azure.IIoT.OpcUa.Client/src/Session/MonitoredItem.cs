// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation, The OPC Foundation, Inc.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client
{
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using System;

    /// <summary>
    /// A monitored item that can be extended to add extra
    /// information as context in the subscription.
    /// </summary>
    public abstract class MonitoredItem : IDisposable
    {
        /// <summary>
        /// A display name for the monitored item.
        /// </summary>
        public string? DisplayName { get; set; }

        /// <summary>
        /// The start node for the browse path that
        /// identifies the node to monitor.
        /// </summary>
        public NodeId StartNodeId { get; set; }

        /// <summary>
        /// The node class of the node being monitored
        /// (affects the type of filter available).
        /// </summary>
        public NodeClass NodeClass { get; set; }

        /// <summary>
        /// The attribute to monitor.
        /// </summary>
        public uint AttributeId { get; set; }

        /// <summary>
        /// The range of array indexes to monitor.
        /// </summary>
        public string? IndexRange { get; set; }

        /// <summary>
        /// The encoding to use when returning notifications.
        /// </summary>
        public QualifiedName? Encoding { get; set; }

        /// <summary>
        /// The monitoring mode.
        /// </summary>
        public MonitoringMode MonitoringMode { get; set; }

        /// <summary>
        /// The sampling interval.
        /// </summary>
        public TimeSpan SamplingInterval
        {
            get => _samplingInterval;
            set
            {
                if (_samplingInterval != value)
                {
                    AttributesModified = true;
                }
                _samplingInterval = value;
            }
        }

        /// <summary>
        /// The filter to use to select values to return.
        /// </summary>
        public MonitoringFilter? Filter
        {
            get => _filter;
            set
            {
                // validate filter against node class.
                ValidateFilter(value);

                AttributesModified = true;
                _filter = value;
            }
        }

        /// <summary>
        /// The length of the queue used to buffer values.
        /// </summary>
        public uint QueueSize
        {
            get => _queueSize;
            set
            {
                if (_queueSize != value)
                {
                    AttributesModified = true;
                }
                _queueSize = value;
            }
        }

        /// <summary>
        /// Whether to discard the oldest entries in the queue when it is full.
        /// </summary>
        public bool DiscardOldest
        {
            get => _discardOldest;
            set
            {
                if (_discardOldest != value)
                {
                    AttributesModified = true;
                }
                _discardOldest = value;
            }
        }

        /// <summary>
        /// The identifier assigned by the server.
        /// </summary>
        public uint ServerId { get; set; }

        /// <summary>
        /// Whether the item has been created on the server.
        /// </summary>
        public bool Created => ServerId != 0;

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
        public MonitoringMode CurrentMonitoringMode { get; internal set; }

        /// <summary>
        /// The sampling interval.
        /// </summary>
        public TimeSpan CurrentSamplingInterval { get; private set; }

        /// <summary>
        /// The length of the queue used to buffer values.
        /// </summary>
        public uint CurrentQueueSize { get; private set; }

        /// <summary>
        /// The identifier assigned by the client.
        /// </summary>
        public uint ClientHandle { get; private set; }

        /// <summary>
        /// The node id to monitor after applying any relative path.
        /// </summary>
        public NodeId? ResolvedNodeId => StartNodeId;

        /// <summary>
        /// Whether the monitoring attributes have been modified
        /// since the item was created.
        /// </summary>
        internal bool AttributesModified { get; private set; }

        /// <summary>
        /// The subscription that owns the monitored item.
        /// </summary>
        protected IManagedSubscription Subscription { get; }

        /// <summary>
        /// Create monitored item
        /// </summary>
        /// <param name="subscription"></param>
        /// <param name="options"></param>
        /// <param name="logger"></param>
        protected MonitoredItem(IManagedSubscription subscription,
            IOptionsMonitor<MonitoredItemOptions> options, ILogger logger)
        {
            Subscription = subscription;
            StartNodeId = NodeId.Null;
            AttributeId = Attributes.Value;
            MonitoringMode = MonitoringMode.Reporting;
            AttributesModified = true;
            _samplingInterval = TimeSpan.MinValue;
            _discardOldest = true;

            Error = ServiceResult.Good;
            ClientHandle = Utils.IncrementIdentifier(ref _globalClientHandle);

            _logger = logger;
            _options = options.CurrentValue;
            _monitor = options.OnChange(OnOptionsChanged);
            OnOptionsChanged(_options, null);
            _logger.LogDebug("{Item} CREATED.", this);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc/>
        public override string? ToString()
        {
            return
$"{Subscription}#{ClientHandle}|{ServerId} ({DisplayName ?? StartNodeId.ToString()})";
        }

        /// <summary>
        /// Dispose monitored item
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            Subscription.RemoveItem(this);
            _logger.LogDebug("{Item} REMOVED.", this);
            ServerId = 0;
            _monitor?.Dispose();
            _disposedValue = true;
        }

        /// <summary>
        /// Updates the object with the results of a create monitored item request.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="result"></param>
        /// <param name="index"></param>
        /// <param name="diagnosticInfos"></param>
        /// <param name="responseHeader"></param>
        internal void SetCreateResult(MonitoredItemCreateRequest request,
            MonitoredItemCreateResult result, int index,
            DiagnosticInfoCollection diagnosticInfos, ResponseHeader responseHeader)
        {
            ObjectDisposedException.ThrowIf(_disposedValue, this);
            var error = ServiceResult.Good;

            if (StatusCode.IsBad(result.StatusCode))
            {
                error = ClientBase.GetResult(result.StatusCode, index,
                    diagnosticInfos, responseHeader);
            }

            CurrentMonitoringMode = request.MonitoringMode;
            CurrentSamplingInterval = TimeSpan.FromMilliseconds(
                request.RequestedParameters.SamplingInterval);
            ClientHandle = request.RequestedParameters.ClientHandle;
            CurrentQueueSize = request.RequestedParameters.QueueSize;
            Error = error;

            if (ServiceResult.IsGood(error))
            {
                ServerId = result.MonitoredItemId;
                CurrentSamplingInterval =
                    TimeSpan.FromMilliseconds(result.RevisedSamplingInterval);
                CurrentQueueSize = result.RevisedQueueSize;

                if (result.FilterResult != null)
                {
                    FilterResult = Utils.Clone(result.FilterResult.Body)
                        as MonitoringFilterResult;
                }
            }
            LogRevisedSamplingRateAndQueueSize(true);
            AttributesModified = false;
        }

        /// <summary>
        /// Updates the object with the results of a modify monitored item request.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="result"></param>
        /// <param name="index"></param>
        /// <param name="diagnosticInfos"></param>
        /// <param name="responseHeader"></param>
        internal void SetModifyResult(MonitoredItemModifyRequest request,
            MonitoredItemModifyResult result, int index,
            DiagnosticInfoCollection diagnosticInfos, ResponseHeader responseHeader)
        {
            ObjectDisposedException.ThrowIf(_disposedValue, this);
            var error = ServiceResult.Good;
            if (StatusCode.IsBad(result.StatusCode))
            {
                error = ClientBase.GetResult(result.StatusCode, index,
                    diagnosticInfos, responseHeader);
            }

            Error = error;

            if (ServiceResult.IsGood(error))
            {
                ClientHandle = request.RequestedParameters.ClientHandle;
                CurrentSamplingInterval = TimeSpan.FromMilliseconds(
                    request.RequestedParameters.SamplingInterval);
                CurrentQueueSize = request.RequestedParameters.QueueSize;

                CurrentSamplingInterval = TimeSpan.FromMilliseconds(
                    result.RevisedSamplingInterval);
                CurrentQueueSize = result.RevisedQueueSize;

                if (result.FilterResult != null)
                {
                    FilterResult = Utils.Clone(result.FilterResult.Body)
                        as MonitoringFilterResult;
                }
            }
            LogRevisedSamplingRateAndQueueSize(false);
            AttributesModified = false;
        }

        /// <summary>
        /// Log revised sampling rate and queue size
        /// </summary>
        /// <param name="created"></param>
        public void LogRevisedSamplingRateAndQueueSize(bool created)
        {
            if (SamplingInterval != CurrentSamplingInterval &&
                QueueSize != CurrentQueueSize && CurrentQueueSize != 0)
            {
                _logger.LogInformation(
                    "{Item}: {Action} SamplingInterval was " +
                    "revised from {SamplingInterval} to {CurrentSamplingInterval} " +
                    "and QueueSize from {QueueSize} to {CurrentQueueSize}.",
                    this, created ? "CREATED" : "UPDATED",
                    SamplingInterval, CurrentSamplingInterval, QueueSize, CurrentQueueSize);
            }
            else if (SamplingInterval != CurrentSamplingInterval)
            {
                _logger.LogInformation(
                    "{Item}: {Action} SamplingInterval was " +
                    "revised from {SamplingInterval} to {CurrentSamplingInterval}.",
                    this, created ? "CREATED" : "UPDATED",
                    SamplingInterval, CurrentSamplingInterval);
            }
            else if (QueueSize != CurrentQueueSize && CurrentQueueSize != 0)
            {
                _logger.LogInformation(
                    "{Item}: {Action} QueueSize was " +
                    "revised from {QueueSize} to {CurrentQueueSize}.",
                    this, created ? "CREATED" : "UPDATED",
                    QueueSize, CurrentQueueSize);
            }
            else
            {
                _logger.LogDebug(
                    "{Item}: {Action} with desired configuration.",
                    this, created ? "CREATED" : "UPDATED");
            }
        }

        /// <summary>
        /// Updates the object with the results of a transfer subscription request.
        /// </summary>
        /// <param name="clientHandle"></param>
        internal void SetTransferResult(uint clientHandle)
        {
            ObjectDisposedException.ThrowIf(_disposedValue, this);
            _logger.LogDebug("{Item}: TRANSFERED.", this);

            // ensure the global counter is not duplicating future handle ids
            Utils.LowerLimitIdentifier(ref _globalClientHandle, clientHandle);
        }

        /// <summary>
        /// Updates the object with the results of a delete monitored item request.
        /// </summary>
        internal void Reset()
        {
            ObjectDisposedException.ThrowIf(_disposedValue, this);
            _logger.LogDebug("{Item}: RESET.",this);
            ServerId = 0;
            Error = ServiceResult.Good;
        }

        /// <summary>
        /// Called when the options change
        /// </summary>
        /// <param name="options"></param>
        /// <param name="name"></param>
        protected virtual void OnOptionsChanged(MonitoredItemOptions options, string? name)
        {
            _options = options;
            AttributesModified = true;
        }

        /// <summary>
        /// Throws an exception if the flter cannot be used with the node class.
        /// </summary>
        /// <param name="filter"></param>
        /// <exception cref="ServiceResultException"></exception>
        private void ValidateFilter(MonitoringFilter? filter)
        {
            if (filter == null)
            {
                return;
            }
            switch (filter)
            {
                case AggregateFilter:
                    if (NodeClass == NodeClass.Unspecified)
                    {
                        NodeClass = NodeClass.Variable;
                    }
                    else if (NodeClass != NodeClass.Variable)
                    {
                        throw ServiceResultException.Create(StatusCodes.BadFilterNotAllowed,
                            "AggregateFilter may not be specified for nodes of class '{0}'.",
                            NodeClass);
                    }
                    break;
                case DataChangeFilter:
                    if (NodeClass == NodeClass.Unspecified)
                    {
                        NodeClass = NodeClass.Variable;
                    }
                    else if (NodeClass is not NodeClass.Variable and not NodeClass.VariableType)
                    {
                        throw ServiceResultException.Create(StatusCodes.BadFilterNotAllowed,
                            "DataChangeFilter may not be specified for nodes of class '{0}'.",
                            NodeClass);
                    }
                    break;
                case EventFilter:
                    if (NodeClass == NodeClass.Unspecified)
                    {
                        NodeClass = NodeClass.Object;
                    }
                    else if (NodeClass is not NodeClass.Object and not NodeClass.View)
                    {
                        throw ServiceResultException.Create(StatusCodes.BadFilterNotAllowed,
                            "EventFilter may not be specified for nodes of class '{0}'.",
                            NodeClass);
                    }
                    break;
                case null:
                    break;
                default:
                    throw ServiceResultException.Create(StatusCodes.BadFilterNotAllowed,
                        "Unsupported filter '{0}'.",
                        filter.GetType().Name);
            }
        }

        private readonly IDisposable? _monitor;
        private MonitoredItemOptions _options;
        private TimeSpan _samplingInterval;
        private MonitoringFilter? _filter;
        private uint _queueSize;
        private bool _discardOldest;
        private static long _globalClientHandle;
        private bool _disposedValue;
        private readonly ILogger _logger;
    }
}
