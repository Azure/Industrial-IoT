// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation, The OPC Foundation, Inc.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client
{
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Text;

    /// <summary>
    /// A monitored item that can be extended to add extra
    /// information as context in the subscription.
    /// </summary>
    public abstract class MonitoredItem : IDisposable
    {
        /// <summary>
        /// The identifier assigned by the server.
        /// </summary>
        public uint ServerId { get; private set; }

        /// <summary>
        /// Whether the item has been created on the server.
        /// </summary>
        public bool Created => ServerId != 0;

        /// <summary>
        /// The last error result associated with the item
        /// </summary>
        public ServiceResult Error { get; private set; }

        /// <summary>
        /// The filter result of the last change applied.
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
        /// The subscription that owns the monitored item.
        /// </summary>
        protected IMonitoredItemContext Context { get; }

        /// <summary>
        /// Current monitored item options (TODO: REMOVE)
        /// </summary>
        protected MonitoredItemOptions Options
        {
            get => _currentOptions ?? new();
            set => _currentOptions = value;
        }

        /// <summary>
        /// Create monitored item
        /// </summary>
        /// <param name="subscription"></param>
        /// <param name="options"></param>
        /// <param name="logger"></param>
        protected MonitoredItem(IMonitoredItemContext subscription,
            IOptionsMonitor<MonitoredItemOptions> options, ILogger logger)
        {
            Context = subscription;
            ClientHandle = Utils.IncrementIdentifier(ref _globalClientHandle);
            Error = ServiceResult.Good;

            _logger = logger;

            QueuePendingChanges(options.CurrentValue, _currentOptions);
            _changeTracking = options.OnChange((o, _) => OnOptionsChanged(o));
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
            var sb = new StringBuilder()
              .Append(Context)
              .Append('#')
              .Append(ClientHandle)
              .Append('|')
              .Append(ServerId)
              .Append(" (")
              .Append(Options.DisplayName ?? Options.StartNodeId.ToString())
              .Append(')');
            return sb.ToString();
        }

        /// <summary>
        /// Dispose monitored item
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !_disposedValue)
            {
                while (TryGetPendingChange(out var change))
                {
                    change.Abandon();
                }

                Context.RemoveItem(this);
                _logger.LogDebug("{Item} REMOVED.", this);

                ServerId = 0;
                _changeTracking?.Dispose();
                _disposedValue = true;
            }
        }

        /// <summary>
        /// Called when the subscription state changed
        /// </summary>
        /// <param name="state"></param>
        /// <param name="publishingInterval"></param>
        protected internal virtual void OnSubscriptionStateChange(
            SubscriptionState state, TimeSpan publishingInterval)
        {
            var options = _currentOptions;
            if (options == null ||
                (state != SubscriptionState.Created &&
                 state != SubscriptionState.Modified))
            {
                return;
            }
            var queueSize = options.QueueSize;
            if (!options.AutoSetQueueSize)
            {
                return;
            }
            if (publishingInterval == TimeSpan.Zero)
            {
                return;
            }
            var samplingInterval = CurrentSamplingInterval;
            if (samplingInterval == TimeSpan.Zero)
            {
                samplingInterval = options.SamplingInterval;
            }
            if (samplingInterval <= TimeSpan.Zero)
            {
                return;
            }
            queueSize = Math.Max(queueSize, (uint)Math.Ceiling(
                publishingInterval.TotalMilliseconds / samplingInterval.TotalMilliseconds))
                + 1;
            if (queueSize == options.QueueSize)
            {
                return;
            }
            OnOptionsChanged(options with { QueueSize = queueSize });
        }

        /// <summary>
        /// Called when the options change
        /// </summary>
        /// <param name="options"></param>
        protected virtual void OnOptionsChanged(MonitoredItemOptions options)
        {
            QueuePendingChanges(options, _currentOptions);
            Context.Update();
        }

        /// <summary>
        /// Get the current pending change in the change list. The change list
        /// collects the changes to be made to the monitored item while the
        /// subscription is applying state changes.
        /// </summary>
        /// <param name="change"></param>
        /// <returns></returns>
        internal bool TryGetPendingChange([NotNullWhen(true)] out Change? change)
        {
            return _pendingChanges.TryPeek(out change);
        }

        /// <summary>
        /// Updates the object with the results of a transfer subscription request.
        /// </summary>
        /// <param name="clientHandle"></param>
        /// <param name="serverHandle"></param>
        internal void SetTransferResult(uint clientHandle, uint serverHandle)
        {
            ObjectDisposedException.ThrowIf(_disposedValue, this);

            // ensure the global counter is not duplicating future handle ids
            if (clientHandle != ClientHandle)
            {
                _logger.LogInformation("{Item}: UPDATE CLIENT ID from {Old} to {New}.",
                    this, ClientHandle, clientHandle);

                ClientHandle = clientHandle;

                Utils.LowerLimitIdentifier(ref _globalClientHandle, clientHandle);
            }
            if (serverHandle != ServerId)
            {
                _logger.LogInformation("{Item}: UPDATE SERVER ID from {Old} to {New}.",
                    this, ServerId, serverHandle);

                ServerId = serverHandle;
            }
        }

        /// <summary>
        /// Reset the monitored item to its initial state for recreation
        /// on server side.
        /// </summary>
        internal void Reset()
        {
            ObjectDisposedException.ThrowIf(_disposedValue, this);

            _logger.LogDebug("{Item}: RESET.", this);
            ServerId = 0;

            var options = _currentOptions;
            while (TryGetPendingChange(out var change))
            {
                change.Abandon();
                options = change.Options;
            };
            _currentOptions = null;
            if (options == null)
            {
                return;
            }
            QueuePendingChanges(options, null);
        }

        /// <summary>
        /// Queues changes into the change queue for the item
        /// </summary>
        /// <param name="options"></param>
        /// <param name="currentOptions"></param>
        private void QueuePendingChanges(MonitoredItemOptions options,
            MonitoredItemOptions? currentOptions)
        {
            if (currentOptions != null && currentOptions == options)
            {
                // No changes
                Context.NotifyItemChangeResult(this, 0, options, new ServiceResult(
                    StatusCodes.BadNothingToDo), true, null);
                return;
            }

            if (NodeId.IsNull(options.StartNodeId))
            {
                // Not valid
                Context.NotifyItemChangeResult(this, 0, options, new ServiceResult(
                    StatusCodes.BadNodeIdInvalid), true, null);
                return;
            }
            _currentOptions = options;
            _pendingChanges.Enqueue(new Change(this, options, currentOptions));
        }

        /// <summary>
        /// Complete a change
        /// </summary>
        /// <param name="change"></param>
        /// <returns></returns>
        internal bool CompleteChange(Change change)
        {
            return _pendingChanges.TryDequeue(out var completed)
                && change == completed;
        }

        /// <summary>
        /// Change steps to apply to the monitored items in the
        /// subscription context. The change list is a queue of
        /// steps to perform inside the subscription.
        /// see Subscription.ApplyMonitoredItemChangesAsync for
        /// more information
        /// </summary>
        internal sealed class Change
        {
            /// <summary>
            /// The item on which the changes are applied
            /// </summary>
            public MonitoredItem Item { get; }

            /// <summary>
            /// Timestamps to return
            /// </summary>
            public TimestampsToReturn Timestamps => Options.TimestampsToReturn;

            /// <summary>
            /// Create request if the item is not yet created
            /// </summary>
            public MonitoredItemCreateRequest? Create { get; }

            /// <summary>
            /// Modification request if the item is already created
            /// </summary>
            public MonitoredItemModifyRequest? Modify { get; private set; }

            /// <summary>
            /// Monitoring mode change pending
            /// </summary>
            public MonitoringMode? MonitoringModeChange { get; private set; }

            /// <summary>
            /// Force recreating the item due to changes in the
            /// read item information.
            /// </summary>
            public bool ForceRecreate { get; private set; }

            /// <summary>
            /// Current retry count of this change
            /// </summary>
            public int RetryCount { get; private set; }

            /// <summary>
            /// Options that are the source of the change
            /// </summary>
            public MonitoredItemOptions Options { get; }

            /// <summary>
            /// Create change
            /// </summary>
            /// <param name="item"></param>
            /// <param name="options"></param>
            /// <param name="currentOptions"></param>
            public Change(MonitoredItem item, MonitoredItemOptions options,
                MonitoredItemOptions? currentOptions)
            {
                Debug.Assert(!NodeId.IsNull(options.StartNodeId));
                Options = options;
                Item = item;

                var parameters = new MonitoringParameters
                {
                    ClientHandle = item.ClientHandle,
                    SamplingInterval = (int)options.SamplingInterval.TotalMilliseconds,
                    QueueSize = options.QueueSize,
                    DiscardOldest = options.DiscardOldest,
                    Filter = options.Filter != null ?
                        new ExtensionObject(options.Filter) : null
                };

                Create = new MonitoredItemCreateRequest
                {
                    ItemToMonitor = new ReadValueId
                    {
                        NodeId = options.StartNodeId,
                        AttributeId = options.AttributeId,
                        IndexRange = options.IndexRange,
                        DataEncoding = options.Encoding
                    },
                    MonitoringMode = options.MonitoringMode,
                    RequestedParameters = parameters
                };

                if (currentOptions == null ||
                    currentOptions.StartNodeId != options.StartNodeId ||
                    currentOptions.AttributeId != options.AttributeId ||
                    currentOptions.IndexRange != options.IndexRange ||
                    currentOptions.Encoding != options.Encoding)
                {
                    Modify = null;
                    MonitoringModeChange = null;
                    ForceRecreate = true;
                }
                else
                {
                    Modify = new MonitoredItemModifyRequest
                    {
                        MonitoredItemId = item.ServerId,
                        RequestedParameters = parameters
                    };

                    if (currentOptions.MonitoringMode != options.MonitoringMode)
                    {
                        MonitoringModeChange = options.MonitoringMode;

                        // If only monitoring mode changed, no need to modify
                        if (currentOptions with
                        {
                            MonitoringMode = MonitoringModeChange.Value
                        } == options)
                        {
                            Modify = null;
                        }
                    }
                    else
                    {
                        MonitoringModeChange = null;
                    }
                }
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
                Debug.Assert(request.RequestedParameters.ClientHandle == Item.ClientHandle);
                var error = ServiceResult.Good;

                if (StatusCode.IsBad(result.StatusCode))
                {
                    error = ClientBase.GetResult(result.StatusCode, index,
                        diagnosticInfos, responseHeader);
                }

                Item.CurrentMonitoringMode = request.MonitoringMode;
                Item.CurrentSamplingInterval = TimeSpan.FromMilliseconds(
                    request.RequestedParameters.SamplingInterval);
                Item.CurrentQueueSize = request.RequestedParameters.QueueSize;

                if (ServiceResult.IsGood(error))
                {
                    Item.ServerId = result.MonitoredItemId;
                    Item.CurrentSamplingInterval =
                        TimeSpan.FromMilliseconds(result.RevisedSamplingInterval);
                    Item.CurrentQueueSize = result.RevisedQueueSize;

                    Item.LogRevisedSamplingRateAndQueueSize(Options, true);
                    Notify(error, true, result.FilterResult);
                    return;
                }

                // Declare final if not communication error which includes success
                RetryCount++;
                Notify(error, false, result.FilterResult);
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
                Debug.Assert(request.RequestedParameters.ClientHandle == Item.ClientHandle);
                var error = ServiceResult.Good;
                if (StatusCode.IsBad(result.StatusCode))
                {
                    error = ClientBase.GetResult(result.StatusCode, index,
                        diagnosticInfos, responseHeader);
                }

                if (ServiceResult.IsGood(error))
                {
                    Item.CurrentSamplingInterval = TimeSpan.FromMilliseconds(
                        request.RequestedParameters.SamplingInterval);
                    Item.CurrentQueueSize = request.RequestedParameters.QueueSize;

                    Item.CurrentSamplingInterval = TimeSpan.FromMilliseconds(
                        result.RevisedSamplingInterval);
                    Item.CurrentQueueSize = result.RevisedQueueSize;

                    if (MonitoringModeChange == null)
                    {
                        Item.LogRevisedSamplingRateAndQueueSize(Options, false);
                    }

                    // Declare final
                    Notify(error, MonitoringModeChange == null, result.FilterResult);
                    return;
                }

                if (!IsCommunicationError(error))
                {
                    // Do not apply the mode change but force a recreate
                    MonitoringModeChange = null;
                    Modify = null;
                    ForceRecreate = true;
                }

                if (MonitoringModeChange == null)
                {
                    // Retry the modification request
                    RetryCount++;
                }
                Notify(error, false, result.FilterResult);
            }

            /// <summary>
            /// Set monitoring mode result
            /// </summary>
            /// <param name="monitoringMode"></param>
            /// <param name="statusCode"></param>
            /// <param name="index"></param>
            /// <param name="diagnosticInfos"></param>
            /// <param name="responseHeader"></param>
            /// <exception cref="NotImplementedException"></exception>
            internal void SetMonitoringModeResult(MonitoringMode monitoringMode,
                StatusCode statusCode, int index, DiagnosticInfoCollection diagnosticInfos,
                ResponseHeader responseHeader)
            {
                var error = ServiceResult.Good;
                if (StatusCode.IsBad(statusCode))
                {
                    error = ClientBase.GetResult(statusCode, index, diagnosticInfos,
                        responseHeader);
                }

                if (ServiceResult.IsGood(error))
                {
                    Item.CurrentMonitoringMode = monitoringMode;
                    Item.LogRevisedSamplingRateAndQueueSize(Options, false);

                    Notify(error, true);
                    return;
                }

                if (!IsCommunicationError(error))
                {
                    // Reapply the mode change
                    Modify = null;
                    ForceRecreate = true;
                }
                // Retry
                RetryCount++;
                Notify(error, false);
            }

            /// <summary>
            /// Set result of delete
            /// </summary>
            /// <param name="statusCode"></param>
            /// <param name="index"></param>
            /// <param name="diagnosticInfos"></param>
            /// <param name="responseHeader"></param>
            internal void SetDeleteResult(StatusCode statusCode, int index,
                DiagnosticInfoCollection diagnosticInfos, ResponseHeader responseHeader)
            {
                var error = ServiceResult.Good;
                if (StatusCode.IsBad(statusCode))
                {
                    error = ClientBase.GetResult(statusCode, index, diagnosticInfos,
                        responseHeader);
                }

                var final = Create == null && Modify == null;
                if (ServiceResult.IsGood(error) ||
                    error.StatusCode == StatusCodes.BadMonitoredItemIdInvalid ||
                    final)
                {
                    Item.ServerId = 0;
                    ForceRecreate = false;
                    // Now state is !Created so continue here to recreate
                }
                else
                {
                    // Retry
                    RetryCount++;
                }
                Notify(error, final);
            }

            /// <summary>
            /// Abandon the change
            /// </summary>
            internal void Abandon()
            {
                Notify(new ServiceResult(StatusCodes.BadOperationAbandoned), true);
            }

            /// <summary>
            /// Notify and handle retries
            /// </summary>
            /// <param name="error"></param>
            /// <param name="final"></param>
            /// <param name="filterResultExtensionObject"></param>
            private void Notify(ServiceResult error, bool final,
                ExtensionObject? filterResultExtensionObject = null)
            {
                var filterResult = filterResultExtensionObject?.Body
                    as MonitoringFilterResult;
                var stop = Item.Context.NotifyItemChangeResult(
                    Item, RetryCount, Options, error, final, filterResult);
                if (final || stop)
                {
                    Item.CompleteChange(this);
                }
                Item.Error = error;
                Item.FilterResult = filterResult == null ? Item.FilterResult :
                    (MonitoringFilterResult)Utils.Clone(filterResult);
            }

            /// <summary>
            /// Returns true if communication issue and not an error
            /// in subscription or even success or uncertain states
            /// </summary>
            /// <param name="error"></param>
            /// <returns></returns>
            private bool IsCommunicationError(ServiceResult error)
            {
                switch ((uint)error.StatusCode)
                {
                    case StatusCodes.BadCommunicationError:
                    case StatusCodes.BadNotConnected:
                    case StatusCodes.BadSecureChannelClosed:
                        return true;
                }
                return false; // includes not errors
            }
        }

        /// <summary>
        /// Log revised sampling rate and queue size
        /// </summary>
        /// <param name="options"></param>
        /// <param name="created"></param>
        public void LogRevisedSamplingRateAndQueueSize(MonitoredItemOptions options,
            bool created)
        {
            if (options.SamplingInterval != CurrentSamplingInterval &&
                options.QueueSize != CurrentQueueSize && CurrentQueueSize != 0)
            {
                _logger.LogInformation(
                    "{Item}: {Action} SamplingInterval was " +
                    "revised from {SamplingInterval} to {CurrentSamplingInterval} " +
                    "and QueueSize from {QueueSize} to {CurrentQueueSize}.",
                    this, created ? "CREATED" : "UPDATED",
                    options.SamplingInterval, CurrentSamplingInterval,
                    options.QueueSize, CurrentQueueSize);
            }
            else if (options.SamplingInterval != CurrentSamplingInterval)
            {
                _logger.LogInformation(
                    "{Item}: {Action} SamplingInterval was " +
                    "revised from {SamplingInterval} to {CurrentSamplingInterval}.",
                    this, created ? "CREATED" : "UPDATED",
                    options.SamplingInterval, CurrentSamplingInterval);
            }
            else if (options.QueueSize != CurrentQueueSize && CurrentQueueSize != 0)
            {
                _logger.LogInformation(
                    "{Item}: {Action} QueueSize was " +
                    "revised from {QueueSize} to {CurrentQueueSize}.",
                    this, created ? "CREATED" : "UPDATED",
                    options.QueueSize, CurrentQueueSize);
            }
            else
            {
                _logger.LogDebug(
                    "{Item}: {Action} with desired configuration.",
                    this, created ? "CREATED" : "UPDATED");
            }
        }

        private bool _disposedValue;
        private MonitoredItemOptions? _currentOptions;
        private readonly ConcurrentQueue<Change> _pendingChanges = new();
        private readonly IDisposable? _changeTracking;
        private readonly ILogger _logger;
        internal static long _globalClientHandle;
    }
}
