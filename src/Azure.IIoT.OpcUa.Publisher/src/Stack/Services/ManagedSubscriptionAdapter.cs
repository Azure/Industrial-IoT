// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License. See LICENSE in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Services
{
    using Azure.IIoT.OpcUa.Encoders;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;
    using Microsoft.Extensions.Options;
    using Opc.Ua;
    using Opc.Ua.Client.Subscriptions;
    using Opc.Ua.Client.Subscriptions.MonitoredItems;
    using Opc.Ua.Extensions;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using ManagedSubscriptionOptions = Opc.Ua.Client.Subscriptions.SubscriptionOptions;

    /// <summary>
    /// Publisher-owned adapter for the public V2 subscription APIs.
    /// </summary>
    /// <remarks>
    /// Uses only the public V2 contracts so it remains independent from the
    /// subscription implementation's internal types.
    /// </remarks>
    internal sealed class ManagedSubscriptionAdapter : ISubscriptionNotificationHandler,
        IAsyncDisposable, IKeyFrameSnapshotProvider
    {
        /// <summary>
        /// Creates and registers a Publisher adapter with a V2 subscription
        /// manager.
        /// </summary>
        /// <param name="manager">The V2 subscription manager.</param>
        /// <param name="template">The Publisher subscription template.</param>
        /// <param name="options">The Publisher subscription options.</param>
        /// <param name="codec">The Publisher codec for node ids and filters.</param>
        /// <param name="modelChangeBrowserFactory">Creates Publisher address-space browsers.</param>
        /// <param name="logger">Logger used to contain subscriber failures.</param>
        /// <param name="timeProvider">The time provider for notification creation.</param>
        /// <param name="periodicKeyFrameInterval">Optional Publisher key-frame period.</param>
        /// <param name="watchdogAction">Runs non-diagnostic watchdog actions.</param>
        /// <param name="cyclicReadClient">Runs Publisher-owned cyclic reads.</param>
        /// <param name="endpointUrlProvider">Provides the connected endpoint URL.</param>
        /// <param name="applicationUriProvider">Provides the server application URI.</param>
        /// <param name="monitoredItemPreparation">Prepares one monitored-item template.</param>
        /// <param name="browsePathFromRootResolver">Resolves a node path from Objects.</param>
        public ManagedSubscriptionAdapter(ISubscriptionManager manager,
            SubscriptionModel template, OpcUaSubscriptionOptions options,
            IVariantEncoder codec,
            Func<TimeSpan, string, IOpcUaBrowser>? modelChangeBrowserFactory = null,
            ILogger<ManagedSubscriptionAdapter>? logger = null, TimeProvider? timeProvider = null,
            TimeSpan? periodicKeyFrameInterval = null,
            Action<ManagedSubscriptionAdapter, SubscriptionWatchdogBehavior, string>?
                watchdogAction = null,
            IManagedCyclicReadClient? cyclicReadClient = null,
            Func<string?>? endpointUrlProvider = null,
            Func<string?>? applicationUriProvider = null,
            Func<BaseMonitoredItemModel, CancellationToken,
                ValueTask<BaseMonitoredItemModel>>? monitoredItemPreparation = null,
            Func<string, CancellationToken, ValueTask<RelativePath?>>?
                browsePathFromRootResolver = null)
        {
            ArgumentNullException.ThrowIfNull(manager);
            ArgumentNullException.ThrowIfNull(template);
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(codec);
            if (periodicKeyFrameInterval.HasValue &&
                periodicKeyFrameInterval.Value <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(periodicKeyFrameInterval));
            }

            _codec = codec;
            _logger = logger ?? NullLogger<ManagedSubscriptionAdapter>.Instance;
            _modelChangeBrowserFactory = modelChangeBrowserFactory;
            _options = options;
            _periodicKeyFrameInterval = periodicKeyFrameInterval;
            _timeProvider = timeProvider ?? TimeProvider.System;
            _watchdogTimeout = template.MonitoredItemWatchdogTimeout ??
                options.DefaultMonitoredItemWatchdogTimeout ?? TimeSpan.Zero;
            _publishingInterval = template.PublishingInterval
                ?? options.DefaultPublishingInterval ?? TimeSpan.Zero;
            _watchdogCondition = template.WatchdogCondition ??
                options.DefaultMonitoredItemWatchdogCondition ??
                MonitoredItemWatchdogCondition.WhenAnyIsLate;
            _configuredWatchdogBehavior = template.WatchdogBehavior ??
                options.DefaultWatchdogBehavior;
            _watchdogBehavior = _configuredWatchdogBehavior ??
                SubscriptionWatchdogBehavior.Diagnostic;
            _watchdogAction = watchdogAction;
            _cyclicReadClient = cyclicReadClient;
            _endpointUrlProvider = endpointUrlProvider;
            _applicationUriProvider = applicationUriProvider;
            _monitoredItemPreparation = monitoredItemPreparation;
            _browsePathFromRootResolver = browsePathFromRootResolver;
            _resolveBrowsePathFromRoot = template.ResolveBrowsePathFromRoot ??
                options.FetchOpcBrowsePathFromRoot ?? false;
            var subscriptionOptions =
                ManagedSubscriptionOptionsAdapter.ToManagedOptions(template, options);
            _subscriptionOptions =
                new MutableOptionsMonitor<ManagedSubscriptionOptions>(subscriptionOptions);
            _subscription = manager.Add(this, _subscriptionOptions);
            _retryScheduler = new ManagedSubscriptionRetryScheduler(
                options, _timeProvider, RetryAsync, _logger);
            if (periodicKeyFrameInterval.HasValue)
            {
                _keyFrameTimer = _timeProvider.CreateTimer(OnKeyFrameTimer, null,
                    periodicKeyFrameInterval.Value, periodicKeyFrameInterval.Value);
            }
            _conditionTimer = _timeProvider.CreateTimer(OnConditionTimer, null,
                TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
            _preparationRetryTimer = _timeProvider.CreateTimer(
                OnPreparationRetryTimer, null,
                Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            _watchdogTimer = _timeProvider.CreateTimer(OnWatchdogTimer, null,
                Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        }

        /// <summary>
        /// The V2 logical subscription owned by this adapter.
        /// </summary>
        internal ISubscription Subscription => _subscription;

        /// <summary>
        /// The number of Publisher bindings currently registered.
        /// </summary>
        internal int BindingCount
        {
            get
            {
                lock (_bindingsLock)
                {
                    return _bindingsByHandle.Count;
                }
            }
        }

        /// <summary>
        /// The number of owners that retain Publisher key-frame state.
        /// </summary>
        internal int OwnerStateCount
        {
            get
            {
                lock (_bindingsLock)
                {
                    return _ownerStates.Count;
                }
            }
        }

        /// <summary>
        /// The number of item or subscription retries currently tracked.
        /// </summary>
        internal int RetryCount => _retryScheduler.Count;

        /// <summary>
        /// Captures the current managed subscription diagnostics for an owner.
        /// </summary>
        internal ManagedSubscriptionDiagnosticsSnapshot GetDiagnostics(
            ISubscriber owner)
        {
            ArgumentNullException.ThrowIfNull(owner);
            var monitoredItems = 0;
            var applied = 0;
            var pending = 0;
            var retrying = 0;
            var terminal = 0;
            var cyclic = 0;
            var heartbeats = 0;
            var conditions = 0;
            var late = 0;
            foreach (var binding in GetBindings().Where(binding =>
                binding.Owner.Equals(owner)))
            {
                var state = binding.CaptureApplyState(
                    TryGetMonitoredItem(binding, out var monitoredItem)
                        ? monitoredItem
                        : null);
                if (!state.Registered)
                {
                    continue;
                }
                monitoredItems++;
                if (state.Applied)
                {
                    applied++;
                }
                else if (state.Pending)
                {
                    pending++;
                }
                else if (_retryScheduler.IsRetrying(binding.Name))
                {
                    retrying++;
                }
                else
                {
                    terminal++;
                }
                if (binding.IsCyclicRead)
                {
                    cyclic++;
                }
                if (binding.HeartbeatEnabled)
                {
                    heartbeats++;
                }
                if (state.Applied && binding.Condition != null)
                {
                    conditions++;
                }
                if (binding.WatchdogEligible && binding.IsLate)
                {
                    late++;
                }
            }
            bool watchdogEnabled;
            lock (_watchdogLock)
            {
                watchdogEnabled = _watchdogEnabled;
            }
            var groups = Volatile.Read(ref _cyclicReadGroups).Count;
            var partitionCount = _subscription is IPartitionedSubscription partitioned
                ? partitioned.PartitionCount
                : 1;
            return new ManagedSubscriptionDiagnosticsSnapshot(
                monitoredItems, applied, pending, retrying, terminal,
                cyclic, groups, heartbeats, conditions, late,
                partitionCount, RetryCount,
                _subscriptionOptions.CurrentValue.PublishingEnabled,
                watchdogEnabled,
                Volatile.Read(ref _watchdogResetInProgress) != 0);
        }

        internal IReadOnlyList<Exception> GetBackgroundErrors()
        {
            var errors = new List<Exception>(2);
            var backgroundError = Volatile.Read(ref _backgroundError);
            if (backgroundError != null)
            {
                errors.Add(backgroundError);
            }
            var retryError = _retryScheduler.LastError;
            if (retryError != null && !ReferenceEquals(retryError, backgroundError))
            {
                errors.Add(retryError);
            }
            return errors;
        }

        /// <summary>
        /// Gets the number of applied monitored items for an owner.
        /// </summary>
        internal int GetGoodMonitoredItems(ISubscriber owner)
        {
            return GetDiagnostics(owner).AppliedMonitoredItems;
        }

        /// <summary>
        /// Gets the number of unapplied monitored items for an owner.
        /// </summary>
        internal int GetBadMonitoredItems(ISubscriber owner)
        {
            var diagnostics = GetDiagnostics(owner);
            return diagnostics.MonitoredItems - diagnostics.AppliedMonitoredItems;
        }

        /// <summary>
        /// Gets the effective condition timing state for an item binding.
        /// </summary>
        internal bool TryGetConditionIntervals(uint clientHandle,
            out int snapshotInterval, out int updateInterval)
        {
            snapshotInterval = 0;
            updateInterval = 0;
            if (!TryGetBinding(clientHandle, out var binding) || binding.Condition == null)
            {
                return false;
            }
            lock (binding.Condition._lock)
            {
                snapshotInterval = binding.Condition.SnapshotInterval;
                updateInterval = binding.Condition.UpdateInterval;
                return true;
            }
        }

        /// <summary>
        /// Gets whether the binding has a condition refresh request in flight.
        /// </summary>
        internal bool IsConditionRefreshRequested(uint clientHandle)
        {
            return TryGetBinding(clientHandle, out var binding) &&
                binding.Condition is { RefreshRequested: true };
        }

        /// <summary>
        /// Gets the number of active heartbeat items for an owner.
        /// </summary>
        internal int GetHeartbeatsEnabled(ISubscriber owner)
        {
            return GetDiagnostics(owner).HeartbeatsEnabled;
        }

        /// <summary>
        /// Gets the number of late monitored items for an owner.
        /// </summary>
        internal int GetLateMonitoredItems(ISubscriber owner)
        {
            return GetDiagnostics(owner).LateMonitoredItems;
        }

        /// <summary>
        /// Gets the number of active condition items for an owner.
        /// </summary>
        internal int GetConditionsEnabled(ISubscriber owner)
        {
            return GetDiagnostics(owner).ConditionsEnabled;
        }

        /// <summary>
        /// Updates cached heartbeat values for connection loss or recovery.
        /// </summary>
        internal void NotifyConnectionState(bool disconnected)
        {
            foreach (var binding in GetBindings())
            {
                binding.NotifyConnectionState(disconnected);
            }
            lock (_watchdogLock)
            {
                _watchdogConnected = !disconnected;
                UpdateWatchdogTimer();
            }
        }

        /// <summary>
        /// Adds a non-triggered Publisher monitored item to the V2 logical
        /// subscription.
        /// </summary>
        /// <param name="owner">The Publisher subscriber that owns the item.</param>
        /// <param name="template">The Publisher item template.</param>
        /// <returns><c>true</c> if V2 accepted the item.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when an asynchronous triggered-item tree is supplied.
        /// </exception>
        internal bool TryAdd(ISubscriber owner, BaseMonitoredItemModel template)
        {
            ArgumentNullException.ThrowIfNull(owner);
            ArgumentNullException.ThrowIfNull(template);
            EnterSynchronousMutation();
            try
            {
                if (ContainsCyclicReadTemplate(template))
                {
                    throw new InvalidOperationException(
                        "Cyclic-read items require TryAddAsync for worker lifecycle synchronization.");
                }
                if (template.TriggeredItems is { Count: > 0 })
                {
                    throw new InvalidOperationException(
                        "Triggered items require TryAddAsync so V2 SetTriggeringAsync can complete.");
                }
                if (template is MonitoredAddressSpaceModel)
                {
                    throw new InvalidOperationException(
                        "Address-space monitoring requires TryAddAsync for browser lifecycle synchronization.");
                }
                var added = TryAddBinding(
                    CreateBinding(CreateName(template), owner, template, null, []), out _);
                if (added)
                {
                    CommitBindingMetadata();
                    ApplyPublishingState();
                }
                return added;
            }
            finally
            {
                ExitMutation();
            }
        }

        /// <summary>
        /// Adds a Publisher monitored-item tree and applies V2 triggering
        /// links after every item is registered.
        /// </summary>
        /// <param name="owner">The Publisher subscriber that owns the tree.</param>
        /// <param name="template">The root Publisher item template.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns><c>true</c> if the complete tree was registered.</returns>
        internal async ValueTask<bool> TryAddAsync(ISubscriber owner,
            BaseMonitoredItemModel template, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(owner);
            ArgumentNullException.ThrowIfNull(template);
            ThrowIfDisposed();
            if (ContainsAddressSpaceTemplate(template))
            {
                throw new InvalidOperationException(
                    "Address-space monitoring requires UpdateAsync for browser lifecycle synchronization.");
            }

            using var operation = CancellationTokenSource.CreateLinkedTokenSource(
                ct, _disposeCts.Token);
            await _updateGate.WaitAsync(operation.Token).ConfigureAwait(false);
            BeginMutation();
            try
            {
                ThrowIfDisposed();
                var added = new List<ManagedSubscriptionItemBinding>();
                var completed = false;
                PendingInitialApply? pendingApply = null;
                try
                {
                    if (ContainsCyclicReadTemplate(template))
                    {
                        pendingApply = BeginPendingInitialApply();
                    }
                    var root = CreateBinding(CreateName(template), owner, template, null, []);
                    if (!TryAddBinding(root, out var rootItem))
                    {
                        return false;
                    }
                    added.Add(root);
                    var itemsAdded = await AddTriggeredItemsAsync(root, rootItem!, root.Name, added,
                        operation.Token).ConfigureAwait(false);
                    if (itemsAdded)
                    {
                        if (pendingApply != null)
                        {
                            var monitoredItems = added
                                .Select(binding => TryGetMonitoredItem(binding,
                                    out var monitoredItem) ? monitoredItem : null)
                                .Where(monitoredItem => monitoredItem != null)
                                .Cast<IMonitoredItem>()
                                .ToArray();
                            await WaitForInitialApplyAsync(pendingApply, monitoredItems,
                                operation.Token).ConfigureAwait(false);
                        }
                        await SynchronizeCyclicReadGroupsAsync(operation.Token)
                            .ConfigureAwait(false);
                        completed = true;
                        CommitBindingMetadata();
                        ApplyPublishingState();
                    }
                    return completed;
                }
                finally
                {
                    ClearPendingInitialApply(pendingApply);
                    if (!completed)
                    {
                        RemoveBindings(added);
                        await SynchronizeCyclicReadGroupsAsync(CancellationToken.None)
                            .ConfigureAwait(false);
                        ApplyPublishingState();
                    }
                }
            }
            finally
            {
                ExitMutation();
            }
        }

        /// <summary>
        /// Replaces the V2 collection's desired item state. Existing names keep
        /// their mutable V2 options monitor, so V2 receives an OnChange update
        /// without losing the server item or its client handle.
        /// </summary>
        /// <param name="items">The desired Publisher items and owners.</param>
        internal void Update(IEnumerable<(ISubscriber Owner, BaseMonitoredItemModel Template)> items)
        {
            ArgumentNullException.ThrowIfNull(items);
            EnterSynchronousMutation();
            try
            {
                var desiredItems = items.ToArray();
                if (GetBindings().Any(binding => binding.IsCyclicRead) ||
                    desiredItems.Any(item => ContainsCyclicReadTemplate(item.Template)))
                {
                    throw new InvalidOperationException(
                        "Cyclic-read item updates require UpdateAsync for worker lifecycle synchronization.");
                }
                if (GetBindings().Any(binding =>
                    binding.Template is MonitoredAddressSpaceModel))
                {
                    throw new InvalidOperationException(
                        "Address-space monitoring updates require UpdateAsync for browser lifecycle synchronization.");
                }
                var desired = CreateDesiredBindings(desiredItems);
                if (desired.Any(binding => binding.Template.TriggeredItems is { Count: > 0 }))
                {
                    throw new InvalidOperationException(
                        "Triggered item updates require UpdateAsync so V2 SetTriggeringAsync can complete.");
                }
                if (desired.Any(binding => binding.Template is MonitoredAddressSpaceModel))
                {
                    throw new InvalidOperationException(
                        "Address-space monitoring updates require UpdateAsync for browser lifecycle synchronization.");
                }
                UpdateBindings(desired, requestConditionRefresh: true);
                CommitBindingMetadata();
                ApplyPublishingState();
            }
            finally
            {
                ExitMutation();
            }
        }

        /// <summary>
        /// Replaces the V2 collection's desired item state including recursive
        /// triggered items and their V2 triggering relationships.
        /// </summary>
        /// <param name="items">The desired Publisher items and owners.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <param name="lifetimeCt">Runtime lifetime cancellation token.</param>
        internal async ValueTask UpdateAsync(
            IEnumerable<(ISubscriber Owner, BaseMonitoredItemModel Template)> items,
            CancellationToken ct = default, CancellationToken lifetimeCt = default)
        {
            ArgumentNullException.ThrowIfNull(items);
            var requestedItems = items.ToArray();
            var requestVersion = Interlocked.Increment(ref _preparationRequestVersion);
            try
            {
                await UpdateAsync(requestedItems, requestVersion, ct, lifetimeCt)
                    .ConfigureAwait(false);
            }
            catch
            {
                RestorePreparationRetryAfterFailedUpdate(requestVersion);
                throw;
            }
        }

        private async ValueTask UpdateAsync(
            (ISubscriber Owner, BaseMonitoredItemModel Template)[] requestedItems,
            long requestVersion, CancellationToken ct, CancellationToken lifetimeCt)
        {
            ThrowIfDisposed();
            using var operation = CancellationTokenSource.CreateLinkedTokenSource(
                ct, lifetimeCt, _disposeCts.Token);
            await _updateGate.WaitAsync(operation.Token).ConfigureAwait(false);
            if (requestVersion != Volatile.Read(ref _preparationRequestVersion))
            {
                _updateGate.Release();
                return;
            }
            BeginMutation();
            try
            {
                ThrowIfDisposed();
                var notifySemantics = Volatile.Read(ref _hasSynchronized) != 0;
                var previousPublishingEnabled = _subscriptionOptions.CurrentValue.PublishingEnabled;
                var previousItems = SnapshotDesiredItems();
                var previousBindingMetadata = SnapshotBindingMetadata();
                var dataSetClassFieldIds = new ConcurrentDictionary<string, Guid>(
                    _dataSetClassFieldIds, StringComparer.Ordinal);
                PendingInitialApply? pendingApply = null;
                try
                {
                    var (desiredItems, preparationFailed) =
                        await PrepareMonitoredItemsAsync(requestedItems,
                            dataSetClassFieldIds, operation.Token)
                        .ConfigureAwait(false);
                    var desired = CreatePreparedBindings(desiredItems,
                        dataSetClassFieldIds);
                    if (desired.Any(binding =>
                        binding.Monitor.CurrentValue.MonitoringMode !=
                            Opc.Ua.MonitoringMode.Disabled ||
                        binding.IsCyclicRead))
                    {
                        pendingApply = BeginPendingInitialApply();
                    }
                    var monitoredItems = UpdateBindings(desired, requestConditionRefresh: false);
                    await ResolveBrowsePathsFromRootAsync(desired, operation.Token)
                        .ConfigureAwait(false);
                    if (pendingApply != null)
                    {
                        await WaitForInitialApplyAsync(pendingApply, monitoredItems,
                            operation.Token)
                            .ConfigureAwait(false);
                    }
                    await RequestPendingConditionRefreshAsync(operation.Token)
                        .ConfigureAwait(false);
                    await ApplyTriggeringAsync(operation.Token).ConfigureAwait(false);
                    await SynchronizeModelChangeBrowsersAsync(operation.Token)
                        .ConfigureAwait(false);
                    await SynchronizeCyclicReadGroupsAsync(operation.Token)
                        .ConfigureAwait(false);
                    CommitBindingMetadata();
                    CommitDataSetClassFieldIds(dataSetClassFieldIds,
                        prune: !preparationFailed);
                    ApplyPublishingState();
                    Volatile.Write(ref _hasSynchronized, 1);
                    Volatile.Write(ref _preparationCommittedVersion, requestVersion);
                    UpdatePreparationRetry(requestedItems, preparationFailed,
                        requestVersion);
                    if (notifySemantics)
                    {
                        foreach (var owner in previousItems.Select(item => item.Owner)
                            .Concat(requestedItems.Select(item => item.Owner))
                            .Distinct())
                        {
                            await InvokeSubscriberAsync(owner, subscriber =>
                                subscriber.OnMonitoredItemSemanticsChangedAsync(operation.Token))
                                .ConfigureAwait(false);
                        }
                    }
                }
                catch (Exception updateException)
                {
                    ClearPendingInitialApply(pendingApply);
                    try
                    {
                        RestoreBindings(previousItems, previousBindingMetadata);
                        await SynchronizeCyclicReadGroupsAsync(CancellationToken.None)
                            .ConfigureAwait(false);
                        if (!lifetimeCt.IsCancellationRequested &&
                            !_disposeCts.IsCancellationRequested)
                        {
                            using var rollback =
                                CancellationTokenSource.CreateLinkedTokenSource(
                                    lifetimeCt, _disposeCts.Token);
                            await SynchronizeModelChangeBrowsersAsync(rollback.Token)
                                .ConfigureAwait(false);
                            await ApplyTriggeringAsync(rollback.Token).ConfigureAwait(false);
                        }
                        SetPublishingEnabled(previousPublishingEnabled);
                    }
                    catch (Exception rollbackException)
                    {
                        throw new AggregateException(
                            "Managed subscription synchronization and rollback failed.",
                            updateException, rollbackException);
                    }
                    throw;
                }
                finally
                {
                    ClearPendingInitialApply(pendingApply);
                }
            }

            finally
            {
                ExitMutation();
            }
        }

        /// <summary>
        /// Removes one bound item and its recursive triggered-item descendants.
        /// </summary>
        /// <param name="clientHandle">The V2 client handle.</param>
        /// <returns><c>true</c> if V2 removed the root item.</returns>
        internal bool TryRemove(uint clientHandle)
        {
            EnterSynchronousMutation();
            try
            {
                if (!TryGetBinding(clientHandle, out var binding))
                {
                    return false;
                }
                var bindings = GetDescendants(binding).ToArray();
                if (binding.IsCyclicRead ||
                    bindings.Any(candidate => candidate.IsCyclicRead))
                {
                    throw new InvalidOperationException(
                        "Cyclic-read item removal requires UpdateAsync for worker lifecycle synchronization.");
                }
                if (binding.Template is MonitoredAddressSpaceModel ||
                    bindings.Any(candidate =>
                        candidate.Template is MonitoredAddressSpaceModel))
                {
                    throw new InvalidOperationException(
                        "Address-space monitoring removal requires UpdateAsync for browser lifecycle synchronization.");
                }
                var removed = _subscription.MonitoredItems.TryRemove(clientHandle);
                RemoveBinding(binding);
                RemoveBindings(bindings.Where(candidate => candidate.ClientHandle != clientHandle));
                CommitBindingMetadata();
                ApplyPublishingState();
                return removed;
            }
            finally
            {
                ExitMutation();
            }
        }

        /// <summary>
        /// Marks an owner for a full Publisher key frame on its next delivery.
        /// </summary>
        /// <param name="owner">The Publisher subscriber.</param>
        internal void RequestKeyFrame(ISubscriber owner)
        {
            ArgumentNullException.ThrowIfNull(owner);
            lock (_bindingsLock)
            {
                if (_bindingsByHandle.Values.Any(binding => binding.Owner.Equals(owner)) &&
                    _ownerStates.TryGetValue(owner, out var state))
                {
                    state.KeyFrameRequired = true;
                }
            }
        }

        internal IReadOnlyList<ManagedEventMetadata> GetEventMetadata(ISubscriber owner)
        {
            ArgumentNullException.ThrowIfNull(owner);
            lock (_bindingsLock)
            {
                return _publishedEventMetadata.TryGetValue(owner, out var metadata)
                    ? metadata
                    : [];
            }
        }

        internal IReadOnlyList<DataMonitoredItemModel> GetDataMetadata(ISubscriber owner)
        {
            ArgumentNullException.ThrowIfNull(owner);
            lock (_bindingsLock)
            {
                return _publishedDataMetadata.TryGetValue(owner, out var metadata)
                    ? metadata
                    : [];
            }
        }

        /// <summary>
        /// Produces a full Publisher key frame from the owner value cache.
        /// </summary>
        /// <param name="owner">The Publisher subscriber.</param>
        /// <param name="notification">The resulting full frame.</param>
        /// <returns><c>true</c> when a frame was created.</returns>
        internal bool TryCreateKeyFrame(ISubscriber owner,
            out OpcUaSubscriptionNotification? notification)
        {
            ArgumentNullException.ThrowIfNull(owner);
            notification = CreateKeyFrame(owner, _timeProvider.GetUtcNow().UtcDateTime);
            return notification != null;
        }

        bool IKeyFrameSnapshotProvider.TryGetNotifications(ISubscriber owner,
            [NotNullWhen(true)] out IList<MonitoredItemNotificationModel>? notifications)
        {
            return TryGetKeyFrameNotifications(owner, out notifications);
        }

        /// <summary>
        /// Creates a Publisher keep-alive for one currently registered owner.
        /// </summary>
        internal OpcUaSubscriptionNotification? CreateKeepAlive(ISubscriber owner)
        {
            ArgumentNullException.ThrowIfNull(owner);
            return IsLiveOwner(owner) ? CreateNotification([],
                _timeProvider.GetUtcNow().UtcDateTime, MessageType.KeepAlive) : null;
        }

        /// <summary>
        /// Emits any pending condition snapshots. This is the timer's testable
        /// Publisher-owned seam.
        /// </summary>
        /// <param name="force">Whether to bypass configured intervals.</param>
        internal void FlushConditions(bool force = false)
        {
            foreach (var binding in GetBindings().Where(binding => binding.Condition != null))
            {
                FlushCondition(binding, force, endRefresh: false);
            }
        }

        /// <summary>
        /// Emits heartbeat notifications that are due.
        /// </summary>
        internal void FlushHeartbeats(bool force = false)
        {
            foreach (var binding in GetBindings().Where(binding => binding.HeartbeatEnabled))
            {
                EmitHeartbeat(binding, force);
            }
        }

        /// <summary>
        /// Runs the monitored-item watchdog immediately.
        /// </summary>
        internal void FlushWatchdog()
        {
            EvaluateWatchdog();
        }

        /// <summary>
        /// Runs all scheduled retries immediately.
        /// </summary>
        internal ValueTask FlushRetriesAsync(CancellationToken ct = default)
        {
            return _retryScheduler.ProcessAsync(force: true, ct);
        }

        /// <summary>
        /// Completes a previously started reset watchdog action.
        /// </summary>
        internal void CompleteWatchdogReset(bool succeeded)
        {
            Interlocked.Exchange(ref _watchdogResetInProgress, 0);
            if (!succeeded)
            {
                lock (_watchdogLock)
                {
                    if (_watchdogDisposing || Volatile.Read(ref _disposed) != 0)
                    {
                        return;
                    }
                    _watchdogPublishingEnabled =
                        !_watchdogPublishingStopped &&
                        _subscription.CurrentPublishingEnabled;
                    UpdateWatchdogTimer();
                }
            }
        }

        /// <inheritdoc/>
        public ValueTask OnDataChangeNotificationAsync(ISubscription subscription,
            uint sequenceNumber, DateTime publishTime,
            ReadOnlyMemory<DataValueChange> notification, PublishState publishStateMask,
            IReadOnlyList<string> stringTable)
        {
            var deliveries = new Dictionary<ISubscriber,
                List<(ManagedSubscriptionItemBinding Binding, DataValue Value,
                    uint ItemSequenceNumber)>>();
            foreach (var change in notification.Span)
            {
                if (!TryGetBinding(change.MonitoredItem, out var binding))
                {
                    continue;
                }
                if (binding.IsCyclicRead)
                {
                    continue;
                }
                var value = Clone(change.Value);
                var itemSequenceNumber = binding.RecordDataChange(value,
                    _timeProvider.GetUtcNow());
                if (binding.SkipFirstDataChange() || binding.DropDataChange)
                {
                    continue;
                }
                if (!deliveries.TryGetValue(binding.Owner, out var changes))
                {
                    changes = [];
                    deliveries.Add(binding.Owner, changes);
                }
                changes.Add((binding, change.Value, itemSequenceNumber));
            }

            foreach (var (owner, changes) in deliveries)
            {
                if (!IsLiveOwner(owner))
                {
                    continue;
                }
                var keyFrameRequired = RequiresKeyFrame(owner);
                var currentSequences = keyFrameRequired ? changes
                    .GroupBy(change => change.Binding.Name, StringComparer.Ordinal)
                    .ToDictionary(group => group.Key,
                        group => group.Last().ItemSequenceNumber,
                        StringComparer.Ordinal) : null;
                var notifications = keyFrameRequired ? null : changes
                    .Select(change => CreateDataNotification(change.Binding, change.Value,
                        change.ItemSequenceNumber))
                    .ToList();
                using var message = keyFrameRequired
                    ? CreateKeyFrame(owner, publishTime, sequenceNumber,
                        currentSequences)
                    : CreateNotification(notifications!, publishTime,
                        MessageType.DeltaFrame, sequenceNumber);
                if (message == null)
                {
                    continue;
                }
                MarkKeyFrameDelivered(owner, message.MessageType == MessageType.KeyFrame);
                lock (_deliveryGate)
                {
                    Deliver(owner, message,
                        static (subscriber, notification) =>
                            subscriber.OnSubscriptionDataChangeReceived(notification));
                }
                if (IsLiveOwner(owner))
                {
                    InvokeSubscriber(owner, subscriber =>
                        subscriber.OnSubscriptionDataDiagnosticsChange(true,
                            message.Notifications.Count,
                            message.Notifications.Sum(item => item.Overflow), 0));
                }
            }
            return ValueTask.CompletedTask;
        }

        /// <inheritdoc/>
        public async ValueTask OnEventDataNotificationAsync(ISubscription subscription,
            uint sequenceNumber, DateTime publishTime,
            ReadOnlyMemory<EventNotification> notification, PublishState publishStateMask,
            IReadOnlyList<string> stringTable)
        {
            var deliveries = new Dictionary<(ISubscriber Owner, string? EventTypeName),
                List<MonitoredItemNotificationModel>>();
            var eventNotifications = notification.ToArray();
            foreach (var item in eventNotifications)
            {
                if (!TryGetBinding(item.MonitoredItem, out var binding))
                {
                    continue;
                }
                binding.RecordActivity();
                if (binding.Template is MonitoredAddressSpaceModel)
                {
                    await ProcessModelChangeAsync(binding, item.Fields, ct: default)
                        .ConfigureAwait(false);
                    continue;
                }
                if (binding.Condition != null)
                {
                    if (binding.Condition.RefreshOnly)
                    {
                        // Refresh-only: suppress RefreshStart/End marker events and
                        // re-issue on RefreshRequired; let all other events fall through
                        // to normal event delivery so retained conditions surface as
                        // regular ua-event messages without any snapshot caching.
                        lock (binding.Condition._lock)
                        {
                            if (binding.Condition.Superseded)
                            {
                                continue;
                            }
                        }
                        if (binding.Condition.EventTypeIndex >= 0 &&
                            binding.Condition.EventTypeIndex < item.Fields.Count &&
                            item.Fields[binding.Condition.EventTypeIndex]
                                .TryGetValue(out NodeId refreshEventType))
                        {
                            if (refreshEventType == ObjectTypeIds.RefreshStartEventType ||
                                refreshEventType == ObjectTypeIds.RefreshEndEventType)
                            {
                                continue; // Suppress start/end markers
                            }
                            if (refreshEventType == ObjectTypeIds.RefreshRequiredEventType)
                            {
                                try
                                {
                                    await subscription.ConditionRefreshAsync()
                                        .ConfigureAwait(false);
                                }
                                catch (Exception ex) when (ex is not OperationCanceledException)
                                {
                                    RecordBackgroundError(ex);
                                    _logger.ConditionRefreshFailed(ex);
                                }
                                continue;
                            }
                        }
                        // Fall through: deliver as a regular event notification.
                    }
                    else
                    {
                        await ProcessConditionAsync(subscription, binding, sequenceNumber,
                            publishTime, item.Fields).ConfigureAwait(false);
                        continue;
                    }
                }
                var delivery = (binding.Owner, binding.Template.DisplayName);
                if (!deliveries.TryGetValue(delivery, out var notifications))
                {
                    notifications = [];
                    deliveries.Add(delivery, notifications);
                }
                AddEventNotifications(notifications, binding, item.Fields);
            }

            foreach (var ((owner, eventTypeName), notifications) in deliveries)
            {
                if (notifications.Count == 0 || !IsLiveOwner(owner))
                {
                    continue;
                }
                using var message = CreateNotification(notifications, publishTime,
                    MessageType.Event, sequenceNumber, eventTypeName);
                Deliver(owner, message,
                    static (subscriber, notification) =>
                        subscriber.OnSubscriptionEventReceived(notification));
                if (IsLiveOwner(owner))
                {
                    InvokeSubscriber(owner, subscriber => subscriber.OnSubscriptionEventDiagnosticsChange(
                        true, notifications.Count, notifications.Sum(item => item.Overflow), 0));
                }
            }
        }

        /// <inheritdoc/>
        public ValueTask OnKeepAliveNotificationAsync(ISubscription subscription,
            uint sequenceNumber, DateTime publishTime, PublishState publishStateMask)
        {
            if (publishStateMask.HasFlag(PublishState.Stopped) ||
                publishStateMask.HasFlag(PublishState.Timeout) ||
                publishStateMask.HasFlag(PublishState.Completed))
            {
                return ValueTask.CompletedTask;
            }
            foreach (var owner in GetBindings().Select(binding => binding.Owner).Distinct())
            {
                if (!IsLiveOwner(owner))
                {
                    continue;
                }
                if (RequiresKeyFrame(owner))
                {
                    var keyFrame = CreateKeyFrame(owner, publishTime);
                    if (keyFrame != null && IsLiveOwner(owner))
                    {
                        MarkKeyFrameDelivered(owner, true);
                        using (keyFrame)
                        {
                            lock (_deliveryGate)
                            {
                                Deliver(owner, keyFrame,
                                    static (subscriber, notification) =>
                                        subscriber.OnSubscriptionDataChangeReceived(notification));
                            }
                        }
                    }
                }
                if (!IsLiveOwner(owner))
                {
                    continue;
                }
                using var keepAlive = CreateNotification([], publishTime,
                    MessageType.KeepAlive);
                Deliver(owner, keepAlive,
                    static (subscriber, notification) =>
                        subscriber.OnSubscriptionKeepAlive(notification));
            }
            return ValueTask.CompletedTask;
        }

        /// <inheritdoc/>
        public async ValueTask OnSubscriptionStateChangedAsync(ISubscription subscription,
            SubscriptionState state, PublishState publishStateMask,
            CancellationToken ct = default)
        {
            HandleWatchdogPublishState(subscription, state, publishStateMask);
            Interlocked.Increment(ref _subscriptionStateVersion);
            if (state == SubscriptionState.Error)
            {
                _retryScheduler.UpdateSubscription(failed: true);
            }
            else if (state is SubscriptionState.Created or SubscriptionState.Modified)
            {
                _retryScheduler.UpdateSubscription(failed: false);
            }
            if (state is SubscriptionState.Created or SubscriptionState.Modified)
            {
                QueuePublishingStateEvaluation();
            }
            var bindings = GetBindings();
            var scheduleItemRetries = state != SubscriptionState.Error;
            foreach (var binding in bindings)
            {
                if (TryGetMonitoredItem(binding, out var monitoredItem))
                {
                    if (UpdateMonitoredItemStatus(binding, monitoredItem!,
                        out var serviceResult, scheduleItemRetries))
                    {
                        InvokeSubscriber(binding.Owner, subscriber =>
                            subscriber.OnMonitoredItemUpdate(binding.Template,
                                serviceResult));
                    }
                }
            }
            TryCompletePendingInitialApply();
            if (Volatile.Read(ref _disposed) == 0 && _updateGate.Wait(0))
            {
                BeginMutation();
                try
                {
                    await SynchronizeCyclicReadGroupsAsync(ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                    when (ct.IsCancellationRequested ||
                        _disposeCts.IsCancellationRequested)
                {
                }
                catch (Exception ex)
                {
                    RecordBackgroundError(ex);
                    _logger.CyclicReadSynchronizationFailed(ex);
                    QueueCyclicReadSynchronization();
                }
                finally
                {
                    ExitMutation();
                }
            }
            else if (Volatile.Read(ref _disposed) == 0)
            {
                QueueCyclicReadSynchronization();
            }

            var recovering = publishStateMask.HasFlag(PublishState.Recovered) ||
                publishStateMask.HasFlag(PublishState.Transferred) ||
                state == SubscriptionState.Created && _created;
            if (recovering)
            {
                foreach (var owner in bindings.Select(binding => binding.Owner).Distinct())
                {
                    RequestKeyFrame(owner);
                    await InvokeSubscriberAsync(owner, subscriber =>
                        subscriber.OnMonitoredItemSemanticsChangedAsync(ct)).ConfigureAwait(false);
                    var keyFrame = CreateKeyFrame(owner,
                        _timeProvider.GetUtcNow().UtcDateTime);
                    if (keyFrame != null)
                    {
                        MarkKeyFrameDelivered(owner, true);
                        using (keyFrame)
                        {
                            lock (_deliveryGate)
                            {
                                Deliver(owner, keyFrame,
                                    static (subscriber, notification) =>
                                        subscriber.OnSubscriptionDataChangeReceived(notification));
                            }
                        }
                    }
                }
            }
            _created |= state == SubscriptionState.Created;

            if (state is SubscriptionState.Created or SubscriptionState.Modified &&
                bindings.Any(binding => binding.Condition != null))
            {
                try
                {
                    await subscription.ConditionRefreshAsync(ct).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    RecordBackgroundError(ex);
                    _logger.ConditionRefreshFailed(ex);
                }
            }
        }

        private void HandleWatchdogPublishState(ISubscription subscription,
            SubscriptionState state, PublishState publishStateMask)
        {
            var logicalStopped = !subscription.CurrentPublishingEnabled;
            var publishingStopped = publishStateMask.HasFlag(PublishState.Stopped);
            if (publishingStopped)
            {
                lock (_watchdogLock)
                {
                    _watchdogPublishingStopped = true;
                    _watchdogPublishingEnabled = false;
                    UpdateWatchdogTimer();
                }
            }
            if (!publishingStopped &&
                (publishStateMask.HasFlag(PublishState.Completed) ||
                    state == SubscriptionState.Deleted))
            {
                lock (_watchdogLock)
                {
                    if (logicalStopped && !subscription.Created)
                    {
                        _watchdogPublishingStopped = true;
                        _watchdogPublishingEnabled = false;
                    }
                    else if (subscription.CurrentPublishingEnabled)
                    {
                        _watchdogPublishingStopped = false;
                        _watchdogPublishingEnabled = true;
                    }
                    UpdateWatchdogTimer();
                }
            }
            if (publishStateMask.HasFlag(PublishState.Recovered) ||
                state == SubscriptionState.Created && subscription.Created)
            {
                lock (_watchdogLock)
                {
                    _watchdogPublishingStopped = false;
                    _watchdogPublishingEnabled =
                        subscription.CurrentPublishingEnabled;
                    UpdateWatchdogTimer();
                }
            }
            else if (state == SubscriptionState.Modified)
            {
                lock (_watchdogLock)
                {
                    _watchdogPublishingEnabled =
                        !_watchdogPublishingStopped &&
                        subscription.CurrentPublishingEnabled;
                    UpdateWatchdogTimer();
                }
            }
            if (publishStateMask.HasFlag(PublishState.Timeout))
            {
                RunWatchdogAction(_configuredWatchdogBehavior ??
                    SubscriptionWatchdogBehavior.Reset,
                    "Managed subscription timed out on the server.",
                    requireEnabled: false);
            }
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }
            lock (_watchdogLock)
            {
                _watchdogDisposing = true;
                _watchdogPublishingEnabled = false;
                UpdateWatchdogTimer();
            }
            CancelPendingInitialApply();
            await _disposeCts.CancelAsync().ConfigureAwait(false);
            await _retryScheduler.DisposeAsync().ConfigureAwait(false);
            Task? preparationRetry;
            lock (_preparationRetryLock)
            {
                preparationRetry = _preparationRetryTask;
            }
            if (preparationRetry != null)
            {
                await preparationRetry.ConfigureAwait(false);
            }
            var cyclicReadSynchronization = GetCyclicReadSynchronizationTask();
            if (cyclicReadSynchronization != null)
            {
                await cyclicReadSynchronization.ConfigureAwait(false);
            }
            await _updateGate.WaitAsync().ConfigureAwait(false);
            List<Exception>? exceptions = null;
            try
            {
                try
                {
                    _conditionTimer.Dispose();
                    _keyFrameTimer?.Dispose();
                    _preparationRetryTimer.Dispose();
                }
                catch (Exception ex)
                {
                    exceptions ??= [];
                    exceptions.Add(ex);
                }
                try
                {
                    await DisposeModelChangeBrowsersAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    exceptions ??= [];
                    exceptions.Add(ex);
                }
                try
                {
                    await DisposeCyclicReadGroupsAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    exceptions ??= [];
                    exceptions.Add(ex);
                }
                if (_cyclicReadClient != null)
                {
                    try
                    {
                        await _cyclicReadClient.DisposeAsync().ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        exceptions ??= [];
                        exceptions.Add(ex);
                    }
                }
                ManagedSubscriptionItemBinding[] bindings;
                lock (_bindingsLock)
                {
                    bindings = [.. _bindingsByHandle.Values];
                    _bindingsByHandle.Clear();
                    _bindingsByName.Clear();
                    _ownerStates.Clear();
                }
                foreach (var binding in bindings)
                {
                    binding.Dispose();
                }
                try
                {
                    await _subscription.DisposeAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    exceptions ??= [];
                    exceptions.Add(ex);
                }
                try
                {
                    lock (_watchdogLock)
                    {
                        _watchdogTimerDisposed = true;
                    }
                    _watchdogTimer.Dispose();
                }
                catch (Exception ex)
                {
                    exceptions ??= [];
                    exceptions.Add(ex);
                }
            }
            finally
            {
                _updateGate.Release();
                _disposeCts.Dispose();
            }
            if (exceptions != null)
            {
                throw new AggregateException(
                    "Managed subscription disposal failed.", exceptions);
            }
        }

        private async ValueTask<bool> AddTriggeredItemsAsync(
            ManagedSubscriptionItemBinding parent, IMonitoredItem parentItem, string rootName,
            List<ManagedSubscriptionItemBinding> added, CancellationToken ct)
        {
            if (parent.Template.TriggeredItems == null)
            {
                return true;
            }
            for (var index = 0; index < parent.Template.TriggeredItems.Count; index++)
            {
                var template = parent.Template.TriggeredItems[index];
                var child = CreateBinding($"{parent.Name}/triggered/{index}:{GetNamePrefix(template)}",
                    parent.Owner, template, rootName, [parent.Name]);
                if (!TryAddBinding(child, out var childItem))
                {
                    return false;
                }
                added.Add(child);
                var result = await _subscription.SetTriggeringAsync(parentItem, [childItem!], null, ct)
                    .ConfigureAwait(false);
                if (!TryApplyTriggeringResult(parent, [child], result,
                    out _))
                {
                    return false;
                }
                if (!await AddTriggeredItemsAsync(child, childItem!, rootName, added, ct)
                    .ConfigureAwait(false))
                {
                    return false;
                }
            }
            return true;
        }

        private bool TryApplyTriggeringResult(ManagedSubscriptionItemBinding parent,
            IReadOnlyList<ManagedSubscriptionItemBinding> children, SetTriggeringResult result,
            out StatusCode failureStatus)
        {
            if (!StatusCode.IsGood(result.ServiceResult))
            {
                ReportTriggeringFailure(parent, result.ServiceResult);
                failureStatus = result.ServiceResult;
                return false;
            }
            if (result.AddResults.Count != children.Count)
            {
                ReportTriggeringFailure(parent, StatusCodes.BadUnexpectedError);
                failureStatus = StatusCodes.BadUnexpectedError;
                return false;
            }
            for (var index = 0; index < children.Count; index++)
            {
                var status = result.AddResults[index].Status;
                if (!StatusCode.IsGood(status))
                {
                    ReportTriggeringFailure(children[index], status);
                    failureStatus = status;
                    return false;
                }
            }
            foreach (var (_, status) in result.RemoveResults)
            {
                if (!StatusCode.IsGood(status))
                {
                    ReportTriggeringFailure(parent, status);
                    failureStatus = status;
                    return false;
                }
            }
            if (result.RemoveResults.Count != 0)
            {
                ReportTriggeringFailure(parent, StatusCodes.BadUnexpectedError);
                failureStatus = StatusCodes.BadUnexpectedError;
                return false;
            }
            failureStatus = StatusCodes.Good;
            return true;
        }

        private async ValueTask ApplyTriggeringAsync(CancellationToken ct)
        {
            foreach (var binding in GetBindings())
            {
                if (binding.ParentName == null ||
                    !TryGetBindingByName(binding.ParentName, out var parent) ||
                    !TryGetMonitoredItem(binding, out var childItem) ||
                    !TryGetMonitoredItem(parent, out var parentItem))
                {
                    continue;
                }
                var result = await _subscription.SetTriggeringAsync(parentItem!,
                    [childItem!], null, ct).ConfigureAwait(false);
                if (!TryApplyTriggeringResult(parent, [binding], result,
                    out var failureStatus))
                {
                    //
                    // Name both ends of the link. A bare status says a
                    // triggering link was refused but not which one, and a
                    // configuration can carry many.
                    //
                    throw new ServiceResultException(failureStatus,
                        $"Server refused to trigger '{binding.Name}' " +
                        $"({binding.Template.StartNodeId}) from '{parent.Name}' " +
                        $"({parent.Template.StartNodeId}).");
                }
            }
        }

        private void ReportTriggeringFailure(ManagedSubscriptionItemBinding binding,
            StatusCode statusCode)
        {
            InvokeSubscriber(binding.Owner, subscriber =>
                subscriber.OnMonitoredItemUpdate(binding.Template,
                    new ServiceResult(statusCode).ToServiceResultModel()));
        }

        private List<ManagedSubscriptionItemBinding> CreateDesiredBindings(
            IEnumerable<(ISubscriber Owner, BaseMonitoredItemModel Template)> items,
            bool includeTriggeredItems = false)
        {
            var desired = new List<ManagedSubscriptionItemBinding>();
            var names = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var (owner, template) in items)
            {
                ArgumentNullException.ThrowIfNull(owner);
                ArgumentNullException.ThrowIfNull(template);
                var prefix = GetNamePrefix(template);
                names.TryGetValue(prefix, out var ordinal);
                names[prefix] = ordinal + 1;
                var rootName = $"{prefix}:{ordinal}";
                AddDesiredBinding(owner, template, rootName, null, [], desired, includeTriggeredItems);
            }
            return desired;
        }

        private List<ManagedSubscriptionItemBinding> CreatePreparedBindings(
            IEnumerable<(string Name, ISubscriber Owner,
                BaseMonitoredItemModel Template)> items,
            IDictionary<string, Guid> dataSetClassFieldIds)
        {
            var desired = new List<ManagedSubscriptionItemBinding>();
            foreach (var (name, owner, template) in items)
            {
                AddDesiredBinding(owner, template, name, null, [], desired,
                    includeTriggeredItems: true,
                    dataSetClassFieldIds: dataSetClassFieldIds);
            }
            return desired;
        }

        private void AddDesiredBinding(ISubscriber owner, BaseMonitoredItemModel template,
            string name, string? rootName, IReadOnlyList<string> triggeredByNames,
            List<ManagedSubscriptionItemBinding> desired, bool includeTriggeredItems,
            IDictionary<string, Guid>? dataSetClassFieldIds = null)
        {
            var effective = template.SetDefaults(_options);
            ManagedSubscriptionItemBinding binding;
            if (TryGetBindingByName(name, out var current))
            {
                effective = EnsureDataSetClassFieldId(effective, name,
                    current.Template, dataSetClassFieldIds);
                binding = current;
            }
            else
            {
                effective = EnsureDataSetClassFieldId(effective, name,
                    dataSetClassFieldIds: dataSetClassFieldIds);
                binding = CreateBinding(name, owner, effective, rootName,
                    triggeredByNames, dataSetClassFieldIds);
            }
            binding.Update(owner, effective, ManagedSubscriptionOptionsAdapter.ToManagedOptions(
                effective, _options, _codec, rootName ?? name, triggeredByNames));
            desired.Add(binding);
            if (!includeTriggeredItems || effective.TriggeredItems == null)
            {
                return;
            }
            for (var index = 0; index < effective.TriggeredItems.Count; index++)
            {
                var child = effective.TriggeredItems[index];
                AddDesiredBinding(owner, child, $"{name}/triggered/{index}:{GetNamePrefix(child)}",
                    rootName ?? name, [name], desired, true, dataSetClassFieldIds);
            }
        }

        private IReadOnlyList<IMonitoredItem> UpdateBindings(
            List<ManagedSubscriptionItemBinding> desired,
            bool requestConditionRefresh)
        {
            var state = desired.Select(binding => (binding.Name,
                (IOptionsMonitor<MonitoredItemOptions>)binding.Monitor)).ToList();
            var monitoredItems = _subscription.MonitoredItems.Update(state);
            var desiredNames = desired
                .Select(binding => binding.Name)
                .ToHashSet(StringComparer.Ordinal);
            var monitoredItemNames = monitoredItems
                .Select(monitoredItem => monitoredItem.Name)
                .ToHashSet(StringComparer.Ordinal);
            if (monitoredItemNames.Count != monitoredItems.Count ||
                !desiredNames.SetEquals(monitoredItemNames))
            {
                var missing = desiredNames.Except(monitoredItemNames).Take(5);
                var unexpected = monitoredItemNames.Except(desiredNames).Take(5);
                throw new InvalidOperationException(
                    $"V2 monitored-item update was incomplete. Missing: " +
                    $"[{string.Join(", ", missing)}]; unexpected: " +
                    $"[{string.Join(", ", unexpected)}].");
            }
            var previousBindings = GetBindings();
            ManagedSubscriptionItemBinding[] removedBindings;
            lock (_bindingsLock)
            {
                _bindingsByHandle.Clear();
                _bindingsByName.Clear();
                foreach (var monitoredItem in monitoredItems)
                {
                    var binding = desired.FirstOrDefault(candidate =>
                        string.Equals(candidate.Name, monitoredItem.Name, StringComparison.Ordinal));
                    if (binding == null || _bindingsByHandle.ContainsKey(monitoredItem.ClientHandle))
                    {
                        continue;
                    }
                    binding.ClientHandle = monitoredItem.ClientHandle;
                    binding.Activate();
                    _bindingsByHandle.Add(binding.ClientHandle, binding);
                    _bindingsByName.Add(binding.Name, binding);
                    GetOwnerState(binding.Owner).KeyFrameRequired = true;
                }
                PruneOwnerStates();
                var activeBindings = _bindingsByHandle.Values.ToHashSet();
                removedBindings = previousBindings
                    .Where(binding => !activeBindings.Contains(binding))
                    .ToArray();
            }
            foreach (var binding in removedBindings)
            {
                _retryScheduler.Remove(binding.Name);
                binding.Dispose();
            }
            foreach (var binding in GetBindings())
            {
                if (TryGetMonitoredItem(binding, out var monitoredItem))
                {
                    if (UpdateMonitoredItemStatus(binding, monitoredItem!,
                        out var serviceResult))
                    {
                        InvokeSubscriber(binding.Owner, subscriber =>
                            subscriber.OnMonitoredItemUpdate(binding.Template,
                                serviceResult));
                    }
                }
            }
            if (requestConditionRefresh)
            {
                _ = RequestPendingConditionRefreshAsync(default).AsTask();
            }
            return monitoredItems;
        }

        private async ValueTask RequestPendingConditionRefreshAsync(CancellationToken ct)
        {
            var pending = GetBindings()
                .Select(TryCapturePendingConditionRefresh)
                .Where(pending => pending != null)
                .Cast<PendingConditionRefresh>()
                .ToArray();
            if (pending.Length == 0)
            {
                return;
            }
            try
            {
                await _subscription.ConditionRefreshAsync(ct).ConfigureAwait(false);
                foreach (var pendingCondition in pending)
                {
                    lock (pendingCondition.Condition!._lock)
                    {
                        if (ReferenceEquals(pendingCondition.Binding.Condition,
                            pendingCondition.Condition) &&
                            pendingCondition.Condition.Generation ==
                            pendingCondition.Generation)
                        {
                            pendingCondition.Condition.RefreshRequested = false;
                        }
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                RecordBackgroundError(ex);
                _logger.ConditionRefreshFailed(ex);
            }
        }

        private static PendingConditionRefresh? TryCapturePendingConditionRefresh(
            ManagedSubscriptionItemBinding binding)
        {
            var condition = binding.Condition;
            if (condition == null)
            {
                return null;
            }
            lock (condition._lock)
            {
                return condition.RefreshRequested ?
                    new PendingConditionRefresh(binding, condition) : null;
            }
        }

        private bool TryAddBinding(ManagedSubscriptionItemBinding binding,
            out IMonitoredItem? monitoredItem)
        {
            if (binding.Template is MonitoredAddressSpaceModel &&
                _modelChangeBrowserFactory == null)
            {
                throw new NotSupportedException(
                    "Address-space monitoring requires a managed browser factory.");
            }
            if (!_subscription.MonitoredItems.TryAdd(binding.Name, binding.Monitor,
                out monitoredItem) || monitoredItem == null)
            {
                binding.Dispose();
                return false;
            }
            lock (_bindingsLock)
            {
                if (_bindingsByHandle.ContainsKey(monitoredItem.ClientHandle) ||
                    _bindingsByName.ContainsKey(binding.Name))
                {
                    _subscription.MonitoredItems.TryRemove(monitoredItem.ClientHandle);
                    binding.Dispose();
                    monitoredItem = null;
                    return false;
                }
                binding.ClientHandle = monitoredItem.ClientHandle;
                binding.Activate();
                _bindingsByHandle.Add(binding.ClientHandle, binding);
                _bindingsByName.Add(binding.Name, binding);
                GetOwnerState(binding.Owner).KeyFrameRequired = true;
            }
            var createdMonitoredItem = monitoredItem;
            if (UpdateMonitoredItemStatus(binding, createdMonitoredItem,
                out var serviceResult))
            {
                InvokeSubscriber(binding.Owner, subscriber =>
                    subscriber.OnMonitoredItemUpdate(binding.Template,
                        serviceResult));
            }
            return true;
        }

        private void RemoveBindings(IEnumerable<ManagedSubscriptionItemBinding> bindings)
        {
            foreach (var binding in bindings)
            {
                _subscription.MonitoredItems.TryRemove(binding.ClientHandle);
                RemoveBinding(binding);
            }
        }

        private void RemoveBinding(ManagedSubscriptionItemBinding binding)
        {
            lock (_bindingsLock)
            {
                _bindingsByHandle.Remove(binding.ClientHandle);
                _bindingsByName.Remove(binding.Name);
                PruneOwnerStates();
            }
            _retryScheduler.Remove(binding.Name);
            binding.Dispose();
        }

        private IEnumerable<ManagedSubscriptionItemBinding> GetDescendants(
            ManagedSubscriptionItemBinding root)
        {
            var bindings = GetBindings();
            var byName = bindings.ToDictionary(binding => binding.Name, StringComparer.Ordinal);
            foreach (var candidate in bindings)
            {
                var parent = candidate.ParentName;
                while (parent != null)
                {
                    if (string.Equals(parent, root.Name, StringComparison.Ordinal))
                    {
                        yield return candidate;
                        break;
                    }
                    parent = byName.TryGetValue(parent, out var parentBinding) ?
                        parentBinding.ParentName : null;
                }
            }
        }

        private void ApplyPublishingState()
        {
            var hasActiveItems = GetBindings().Any(binding =>
                TryGetMonitoredItem(binding, out var monitoredItem) &&
                monitoredItem!.Created &&
                monitoredItem.CurrentMonitoringMode != Opc.Ua.MonitoringMode.Disabled);
            SetPublishingEnabled(hasActiveItems);
            SetWatchdogPublishingState(hasActiveItems &&
                _subscription.CurrentPublishingEnabled);
        }

        private void QueuePublishingStateEvaluation()
        {
            Interlocked.Exchange(ref _publishingStateDirty, 1);
            ApplyPendingPublishingState();
        }

        private void QueueCyclicReadSynchronization()
        {
            Interlocked.Exchange(ref _cyclicReadStateDirty, 1);
            lock (_cyclicReadSyncLock)
            {
                if (_cyclicReadSyncTask == null &&
                    Volatile.Read(ref _disposed) == 0)
                {
                    _cyclicReadSyncTask =
                        RunPendingCyclicReadSynchronizationAsync();
                }
            }
        }

        private async Task RunPendingCyclicReadSynchronizationAsync()
        {
            await Task.Yield();
            try
            {
                while (Volatile.Read(ref _disposed) == 0 &&
                    Interlocked.Exchange(ref _cyclicReadStateDirty, 0) != 0)
                {
                    await _updateGate.WaitAsync(_disposeCts.Token)
                        .ConfigureAwait(false);
                    BeginMutation();
                    try
                    {
                        if (Volatile.Read(ref _disposed) == 0)
                        {
                            await SynchronizeCyclicReadGroupsAsync(
                                _disposeCts.Token).ConfigureAwait(false);
                        }
                    }
                    finally
                    {
                        ExitMutation();
                    }
                }
            }
            catch (OperationCanceledException)
                when (_disposeCts.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                RecordBackgroundError(ex);
                _logger.CyclicReadSynchronizationFailed(ex);
            }
            finally
            {
                var restart = false;
                lock (_cyclicReadSyncLock)
                {
                    _cyclicReadSyncTask = null;
                    restart = Volatile.Read(ref _disposed) == 0 &&
                        Volatile.Read(ref _cyclicReadStateDirty) != 0;
                }
                if (restart)
                {
                    QueueCyclicReadSynchronization();
                }
            }
        }

        private Task? GetCyclicReadSynchronizationTask()
        {
            lock (_cyclicReadSyncLock)
            {
                return _cyclicReadSyncTask;
            }
        }

        private void ApplyPendingPublishingState()
        {
            while (Volatile.Read(ref _disposed) == 0)
            {
                if (!_updateGate.Wait(0))
                {
                    return;
                }
                try
                {
                    ApplyPendingPublishingStateUnderMutation();
                }
                finally
                {
                    _updateGate.Release();
                }
                if (Volatile.Read(ref _publishingStateDirty) == 0)
                {
                    return;
                }
            }
        }

        private void ExitMutation()
        {
            try
            {
                Interlocked.Increment(ref _mutationVersion);
                if (Volatile.Read(ref _publishingStateDirty) != 0)
                {
                    ApplyPendingPublishingStateUnderMutation();
                }
            }
            finally
            {
                _updateGate.Release();
            }
            if (Volatile.Read(ref _cyclicReadStateDirty) != 0)
            {
                QueueCyclicReadSynchronization();
            }
        }

        private void ApplyPendingPublishingStateUnderMutation()
        {
            do
            {
                Interlocked.Exchange(ref _publishingStateDirty, 0);
                if (Volatile.Read(ref _disposed) == 0 && BindingCount != 0)
                {
                    ApplyPublishingState();
                }
            }
            while (Volatile.Read(ref _publishingStateDirty) != 0);
        }

        private void BeginMutation()
        {
            Interlocked.Increment(ref _mutationVersion);
        }

        private void SetPublishingEnabled(bool enabled)
        {
            if (!enabled)
            {
                SetWatchdogPublishingState(false);
            }
            var current = _subscriptionOptions.CurrentValue;
            if (current.PublishingEnabled != enabled)
            {
                _subscriptionOptions.Update(current with
                {
                    PublishingEnabled = enabled
                });
            }
        }

        private void SetWatchdogPublishingState(bool enabled)
        {
            lock (_watchdogLock)
            {
                _watchdogPublishingEnabled = enabled &&
                    !_watchdogPublishingStopped;
                UpdateWatchdogTimer();
            }
        }

        private DesiredItemSnapshot[] SnapshotDesiredItems()
        {
            return GetBindings()
                .Where(binding => binding.ParentName == null)
                .Select(binding => new DesiredItemSnapshot(
                    binding.Name, binding.Owner,
                    binding.PublishedMetadata.Template))
                .ToArray();
        }

        private IReadOnlyDictionary<string, BindingMetadataSnapshot>
            SnapshotBindingMetadata()
        {
            return GetBindings().ToDictionary(binding => binding.Name,
                binding => binding.PublishedMetadata,
                StringComparer.Ordinal);
        }

        private void CommitBindingMetadata()
        {
            var bindings = GetBindings();
            foreach (var binding in bindings)
            {
                binding.CommitMetadata();
            }
            var data = bindings
                .Where(binding =>
                    binding.PublishedMetadata.Template is DataMonitoredItemModel)
                .GroupBy(binding => binding.Owner)
                .ToDictionary(group => group.Key,
                    group => (IReadOnlyList<DataMonitoredItemModel>)group
                        .Select(binding =>
                            (DataMonitoredItemModel)binding.PublishedMetadata.Template)
                        .ToList());
            var events = bindings
                .Select(binding => (binding.Owner, binding.PublishedMetadata))
                .Where(item => item.PublishedMetadata.Template is EventMonitoredItemModel &&
                    item.PublishedMetadata.EventFilter != null)
                .GroupBy(item => item.Owner)
                .ToDictionary(group => group.Key,
                    group => (IReadOnlyList<ManagedEventMetadata>)group
                        .Select(item => new ManagedEventMetadata(
                            (EventMonitoredItemModel)item.PublishedMetadata.Template,
                            item.PublishedMetadata.EventFilter!,
                            item.PublishedMetadata.EventFieldNames,
                            item.PublishedMetadata.EventFieldIds))
                        .ToList());
            lock (_bindingsLock)
            {
                _publishedDataMetadata = data;
                _publishedEventMetadata = events;
            }
        }

        private void RestoreBindings(IEnumerable<DesiredItemSnapshot> items,
            IReadOnlyDictionary<string, BindingMetadataSnapshot> metadata)
        {
            var desired = new List<ManagedSubscriptionItemBinding>();
            foreach (var item in items)
            {
                AddDesiredBinding(item.Owner, item.Template, item.Name, null, [],
                    desired, includeTriggeredItems: true);
            }
            UpdateBindings(desired, requestConditionRefresh: false);
            foreach (var binding in GetBindings())
            {
                if (metadata.TryGetValue(binding.Name, out var snapshot))
                {
                    binding.RestoreMetadata(snapshot);
                }
            }
        }

        private async ValueTask SynchronizeModelChangeBrowsersAsync(CancellationToken ct)
        {
            var desired = GetBindings()
                .Where(binding => binding.Template is MonitoredAddressSpaceModel)
                .ToDictionary(binding => binding.Name, StringComparer.Ordinal);
            if (desired.Count != 0 && _modelChangeBrowserFactory == null)
            {
                throw new NotSupportedException(
                    "Address-space monitoring requires a managed browser factory.");
            }

            Dictionary<string, ModelChangeBrowserRegistration> current;
            lock (_modelChangeBrowsersLock)
            {
                current = new Dictionary<string, ModelChangeBrowserRegistration>(
                    _modelChangeBrowsers, StringComparer.Ordinal);
            }

            List<ModelChangeBrowserRegistration> staged = [];
            try
            {
                foreach (var binding in desired.Values)
                {
                    ct.ThrowIfCancellationRequested();
                    var rebrowsePeriod = GetRebrowsePeriod(binding);
                    if (current.TryGetValue(binding.Name, out var existingRegistration) &&
                        existingRegistration.RebrowsePeriod == rebrowsePeriod)
                    {
                        continue;
                    }
                    var browser = _modelChangeBrowserFactory!(rebrowsePeriod,
                        _modelChangeSubscriptionName);
                    ModelChangeBrowserRegistration? registration = null;
                    registration = new ModelChangeBrowserRegistration(binding.Name,
                        rebrowsePeriod, browser,
                        (_, change) => PublishModelChange(registration!,
                            kNodeChangeType, change),
                        (_, change) => PublishModelChange(registration!,
                            kReferenceChangeType, change));
                    registration.Attach();
                    staged.Add(registration);
                }
            }
            catch (Exception creationException)
            {
                foreach (var registration in staged)
                {
                    registration.Detach();
                }
                try
                {
                    await CloseModelChangeBrowsersAsync(staged).ConfigureAwait(false);
                }
                catch (Exception cleanupException)
                {
                    throw new AggregateException(
                        "Managed model-change browser creation and cleanup failed.",
                        creationException, cleanupException);
                }
                throw;
            }

            List<ModelChangeBrowserRegistration> removed = [];
            lock (_modelChangeBrowsersLock)
            {
                foreach (var registration in _modelChangeBrowsers.Values.ToArray())
                {
                    if (!desired.TryGetValue(registration.Name, out var binding) ||
                        registration.RebrowsePeriod != GetRebrowsePeriod(binding))
                    {
                        _modelChangeBrowsers.Remove(registration.Name);
                        registration.Detach();
                        removed.Add(registration);
                    }
                }
                foreach (var registration in staged)
                {
                    _modelChangeBrowsers.Add(registration.Name, registration);
                }
            }
            await CloseModelChangeBrowsersAsync(removed).ConfigureAwait(false);
            foreach (var registration in staged)
            {
                registration.Start();
            }
        }

        private async ValueTask DisposeModelChangeBrowsersAsync()
        {
            ModelChangeBrowserRegistration[] registrations;
            lock (_modelChangeBrowsersLock)
            {
                registrations = [.. _modelChangeBrowsers.Values];
                _modelChangeBrowsers.Clear();
                foreach (var registration in registrations)
                {
                    registration.Detach();
                }
            }
            await CloseModelChangeBrowsersAsync(registrations).ConfigureAwait(false);
        }

        private static async ValueTask CloseModelChangeBrowsersAsync(
            IEnumerable<ModelChangeBrowserRegistration> registrations)
        {
            List<Exception>? exceptions = null;
            foreach (var registration in registrations)
            {
                try
                {
                    await registration.Browser.CloseAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    exceptions ??= [];
                    exceptions.Add(ex);
                }
            }
            if (exceptions != null)
            {
                throw new AggregateException(
                    "One or more managed model-change browsers failed to close.",
                    exceptions);
            }
        }

        private void PublishModelChange<T>(ModelChangeBrowserRegistration registration,
            ExpandedNodeId eventType,
            Change<T> change) where T : class, IEncodeable
        {
            lock (_modelChangeBrowsersLock)
            {
                if (!_modelChangeBrowsers.TryGetValue(registration.Name, out var current) ||
                    !ReferenceEquals(current, registration))
                {
                    return;
                }
            }
            if (Volatile.Read(ref _disposed) != 0 ||
                !TryGetBindingByName(registration.Name, out var binding) ||
                binding.Template is not MonitoredAddressSpaceModel template ||
                !IsLiveOwner(binding.Owner))
            {
                return;
            }

            var notifications = CreateModelChangeNotifications(template, eventType, change)
                .ToList();
            using var message = CreateNotification(notifications,
                change.Timestamp.UtcDateTime, MessageType.Event);
            Deliver(binding.Owner, message,
                static (subscriber, notification) =>
                    subscriber.OnSubscriptionEventReceived(notification));
            if (IsLiveOwner(binding.Owner))
            {
                InvokeSubscriber(binding.Owner, subscriber =>
                    subscriber.OnSubscriptionEventDiagnosticsChange(false,
                        notifications.Count,
                        notifications.Sum(notification => notification.Overflow), 1));
            }
        }

        private static IEnumerable<MonitoredItemNotificationModel>
            CreateModelChangeNotifications<T>(MonitoredAddressSpaceModel template,
                ExpandedNodeId eventType, Change<T> change) where T : class, IEncodeable
        {
            for (var index = 0; index < kModelChangeFields.Length; index++)
            {
                Variant value = index switch
                {
                    0 => new Variant((Uuid)Guid.NewGuid()),
                    1 => new Variant(eventType),
                    2 => new Variant(change.Source),
                    3 => new Variant(change.Timestamp.UtcDateTime),
                    4 => change.ChangedItem == null ?
                        Variant.Null : Variant.FromStructure(change.ChangedItem),
                    _ => Variant.Null
                };
                yield return new MonitoredItemNotificationModel
                {
                    Id = template.Id ?? string.Empty,
                    DataSetName = template.DisplayName,
                    DataSetFieldName = kModelChangeFields[index],
                    PathFromRoot = change.PathFromRoot,
                    NodeId = template.StartNodeId,
                    Value = new DataValue(value),
                    Flags = MonitoredItemSourceFlags.ModelChanges,
                    SequenceNumber = change.SequenceNumber
                };
            }
        }

        private TimeSpan GetRebrowsePeriod(
            ManagedSubscriptionItemBinding binding)
        {
            return ((MonitoredAddressSpaceModel)binding.Template).RebrowsePeriod ??
                _options.DefaultRebrowsePeriod ??
                TimeSpan.FromHours(12);
        }

        private static bool ContainsAddressSpaceTemplate(
            BaseMonitoredItemModel template)
        {
            return template is MonitoredAddressSpaceModel ||
                template.TriggeredItems?.Any(ContainsAddressSpaceTemplate) == true;
        }

        private static bool HasPendingChanges(IMonitoredItem monitoredItem)
        {
            return monitoredItem is IMonitoredItemApplyState
            {
                HasPendingChanges: true
            };
        }

        private bool ContainsCyclicReadTemplate(
            BaseMonitoredItemModel template)
        {
            var effective = template.SetDefaults(_options);
            return effective is DataMonitoredItemModel
            {
                SamplingUsingCyclicRead: true
            } || effective.TriggeredItems?.Any(ContainsCyclicReadTemplate) == true;
        }

        private PendingInitialApply BeginPendingInitialApply()
        {
            var pending = new PendingInitialApply(
                Volatile.Read(ref _subscriptionStateVersion));
            lock (_initialApplyLock)
            {
                if (_pendingInitialApply != null)
                {
                    throw new InvalidOperationException(
                        "An initial monitored-item synchronization is already pending.");
                }
                _pendingInitialApply = pending;
            }
            return pending;
        }

        private async ValueTask WaitForInitialApplyAsync(PendingInitialApply pending,
            IReadOnlyList<IMonitoredItem> monitoredItems, CancellationToken ct)
        {
            lock (_initialApplyLock)
            {
                if (!ReferenceEquals(_pendingInitialApply, pending))
                {
                    ThrowIfDisposed();
                    throw new InvalidOperationException(
                        "The initial monitored-item synchronization is no longer active.");
                }
                pending.MonitoredItems = monitoredItems.Select(monitoredItem =>
                {
                    if (!TryGetBindingByName(monitoredItem.Name, out var binding))
                    {
                        throw new InvalidOperationException(
                            $"Missing Publisher binding for V2 item '{monitoredItem.Name}'.");
                    }
                    return new PendingMonitoredItem(monitoredItem,
                        binding.Monitor.CurrentValue.MonitoringMode);
                }).ToArray();
            }
            TryCompletePendingInitialApply();
            try
            {
                await pending.Completion.Task.WaitAsync(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
                when (Volatile.Read(ref _disposed) != 0)
            {
                throw new ObjectDisposedException(nameof(ManagedSubscriptionAdapter));
            }
        }

        private void TryCompletePendingInitialApply()
        {
            PendingInitialApply? pending;
            IReadOnlyList<PendingMonitoredItem>? monitoredItems;
            lock (_initialApplyLock)
            {
                pending = _pendingInitialApply;
                monitoredItems = pending?.MonitoredItems;
            }
            if (pending == null || monitoredItems == null)
            {
                return;
            }
            var stateObserved =
                Volatile.Read(ref _subscriptionStateVersion) !=
                pending.StateVersion;
            if (monitoredItems.All(item =>
                !HasPendingChanges(item.Item) &&
                (!ServiceResult.IsGood(item.Item.Error) ||
                    (item.Item.Created &&
                        item.Item.CurrentMonitoringMode ==
                            item.DesiredMonitoringMode) ||
                    stateObserved)))
            {
                pending.Completion.TrySetResult();
            }
        }

        private void ClearPendingInitialApply(PendingInitialApply? pending)
        {
            if (pending == null)
            {
                return;
            }
            lock (_initialApplyLock)
            {
                if (ReferenceEquals(_pendingInitialApply, pending))
                {
                    _pendingInitialApply = null;
                }
            }
        }

        private void CancelPendingInitialApply()
        {
            PendingInitialApply? pending;
            lock (_initialApplyLock)
            {
                pending = _pendingInitialApply;
            }
            pending?.Completion.TrySetException(new ObjectDisposedException(
                nameof(ManagedSubscriptionAdapter)));
        }

        private async ValueTask SynchronizeCyclicReadGroupsAsync(
            CancellationToken ct)
        {
            await _cyclicReadGate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                var desired = new Dictionary<CyclicReadGroupKey, List<CyclicReadItem>>();
                foreach (ManagedSubscriptionItemBinding binding in GetBindings())
                {
                    if (binding.CyclicReadGroupKey is not { } key ||
                        !binding.TryCaptureCyclicRead(out CyclicReadItem item))
                    {
                        continue;
                    }
                    if (!desired.TryGetValue(key, out List<CyclicReadItem>? items))
                    {
                        items = [];
                        desired.Add(key, items);
                    }
                    items.Add(item);
                }
                foreach (List<CyclicReadItem> items in desired.Values)
                {
                    items.Sort(static (left, right) =>
                        StringComparer.Ordinal.Compare(
                            left.Binding.Name, right.Binding.Name));
                }
                if (desired.Count != 0 && _cyclicReadClient == null)
                {
                    throw new NotSupportedException(
                        "Managed cyclic reads require a managed session read client.");
                }

                var replacement = new Dictionary<CyclicReadGroupKey, CyclicReadGroup>();
                var created = new List<CyclicReadGroup>();
                foreach (var (key, items) in desired)
                {
                    if (_cyclicReadGroups.TryGetValue(key, out CyclicReadGroup? group))
                    {
                        group.Update(items);
                    }
                    else
                    {
                        group = new CyclicReadGroup(this, _cyclicReadClient!,
                            key, items, _timeProvider, _disposeCts.Token);
                        created.Add(group);
                    }
                    replacement.Add(key, group);
                }

                KeyValuePair<CyclicReadGroupKey, CyclicReadGroup>[] removed =
                    _cyclicReadGroups
                    .Where(entry => !replacement.ContainsKey(entry.Key))
                    .ToArray();
                try
                {
                    foreach (var (key, group) in removed)
                    {
                        _cyclicReadGroups.Remove(key);
                        await group.DisposeAsync().ConfigureAwait(false);
                    }
                }
                catch
                {
                    foreach (CyclicReadGroup group in created)
                    {
                        await group.DisposeAsync().ConfigureAwait(false);
                    }
                    throw;
                }
                _cyclicReadGroups = replacement;
                foreach (CyclicReadGroup group in created)
                {
                    group.Start();
                }
            }
            finally
            {
                _cyclicReadGate.Release();
            }
        }

        private async ValueTask DisposeCyclicReadGroupsAsync()
        {
            await _cyclicReadGate.WaitAsync().ConfigureAwait(false);
            try
            {
                CyclicReadGroup[] groups = [.. _cyclicReadGroups.Values];
                _cyclicReadGroups.Clear();
                List<Exception>? exceptions = null;
                foreach (CyclicReadGroup group in groups)
                {
                    try
                    {
                        await group.DisposeAsync().ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        exceptions ??= [];
                        exceptions.Add(ex);
                    }
                }
                if (exceptions != null)
                {
                    throw new AggregateException(
                        "One or more managed cyclic-read groups failed to stop.",
                        exceptions);
                }
            }
            finally
            {
                _cyclicReadGate.Release();
            }
        }

        private void DeliverCyclicRead(
            IReadOnlyList<CyclicReadItem> items,
            IReadOnlyList<DataValue> values,
            uint cycleSequenceNumber,
            DateTime publishTime,
            int missedCycles)
        {
            var mutationVersion = Volatile.Read(ref _mutationVersion);
            if (Volatile.Read(ref _disposed) != 0 ||
                (mutationVersion & 1) != 0)
            {
                return;
            }
            var deliveries = new Dictionary<ISubscriber,
                List<(MonitoredItemNotificationModel Notification,
                    DateTime SourceTimestamp, int Order)>>();
            if (Volatile.Read(ref _disposed) != 0)
            {
                return;
            }
            var receivedAt = _timeProvider.GetUtcNow();
            var count = Math.Min(items.Count, values.Count);
            for (var index = 0; index < count; index++)
            {
                CyclicReadItem item = items[index];
                DataValue value = Clone(values[index]);
                if (missedCycles > 0)
                {
                    value = HeartbeatState.WithStatusCode(value,
                        value.StatusCode.SetOverflow(true));
                }
                if (!item.Binding.TryRecordCyclicRead(item.Generation,
                    item.SkipFirst, value, receivedAt,
                    out uint itemSequenceNumber, out bool skip) ||
                    skip ||
                    !item.Binding.IsCurrentCyclicRead(item.Generation) ||
                    !IsLiveOwner(item.Owner))
                {
                    continue;
                }
                if (!deliveries.TryGetValue(item.Owner, out var notifications))
                {
                    notifications = [];
                    deliveries.Add(item.Owner, notifications);
                }
                notifications.Add((CreateCyclicReadNotification(item, value,
                    itemSequenceNumber, missedCycles),
                    (DateTime)value.SourceTimestamp, index));
            }
            if (mutationVersion != Volatile.Read(ref _mutationVersion) ||
                Volatile.Read(ref _disposed) != 0)
            {
                return;
            }

            foreach (var (owner, valuesForOwner) in deliveries)
            {
                if (Volatile.Read(ref _disposed) != 0 ||
                    !IsLiveOwner(owner))
                {
                    continue;
                }
                var notifications = valuesForOwner
                    .OrderBy(value => value.SourceTimestamp)
                    .ThenBy(value => value.Order)
                    .Select(value => value.Notification)
                    .ToList();
                using var message = CreateNotification(notifications, publishTime,
                    MessageType.DeltaFrame, cycleSequenceNumber);
                Deliver(owner, message,
                    static (subscriber, notification) =>
                        subscriber.OnSubscriptionCyclicReadCompleted(notification));
                if (IsLiveOwner(owner))
                {
                    InvokeSubscriber(owner, subscriber =>
                        subscriber.OnSubscriptionCyclicReadDiagnosticsChange(
                            notifications.Count,
                            notifications.Sum(notification => notification.Overflow)));
                }
            }
        }

        private bool UpdateMonitoredItemStatus(
            ManagedSubscriptionItemBinding binding,
            IMonitoredItem monitoredItem,
            out ServiceResultModel? serviceResult,
            bool scheduleRetry = true)
        {
            var status = binding.UpdateMonitoredItemStatus(monitoredItem);
            if (scheduleRetry)
            {
                _retryScheduler.Update(binding.CaptureRetryTarget());
            }
            else
            {
                _retryScheduler.Remove(binding.Name);
            }
            if (!binding.TryCaptureStatusReport(out status))
            {
                serviceResult = null;
                return false;
            }
            serviceResult = ToServiceResult(monitoredItem, status);
            return true;
        }

        private async ValueTask<ManagedRetryOutcome> RetryAsync(
            ManagedRetryRequest request, CancellationToken ct)
        {
            await _updateGate.WaitAsync(ct).ConfigureAwait(false);
            BeginMutation();
            try
            {
                if (Volatile.Read(ref _disposed) != 0)
                {
                    return ManagedRetryOutcome.Obsolete;
                }
                if (request.Name == null)
                {
                    if (BindingCount == 0)
                    {
                        return ManagedRetryOutcome.Obsolete;
                    }
                    await _subscription.RecreateAsync(ct).ConfigureAwait(false);
                    return ManagedRetryOutcome.Started;
                }
                if (!TryGetBindingByName(request.Name, out var binding) ||
                    !binding.IsRetryCurrent(request.Generation,
                        request.Kind, request.Status))
                {
                    return ManagedRetryOutcome.Obsolete;
                }
                if (_subscription.MonitoredItems is not
                    IMonitoredItemRetryCollection retry)
                {
                    _logger.ManagedItemRetryUnsupported(request.Name);
                    return ManagedRetryOutcome.Failed;
                }
                return retry.TryRequeue(request.ClientHandle)
                    ? ManagedRetryOutcome.Started
                    : ManagedRetryOutcome.Failed;
            }
            finally
            {
                ExitMutation();
            }
        }

        private void EnterSynchronousMutation()
        {
            ThrowIfDisposed();
            if (!_updateGate.Wait(0))
            {
                throw new InvalidOperationException(
                    "A managed subscription mutation is already in progress.");
            }
            BeginMutation();
            try
            {
                ThrowIfDisposed();
            }
            catch
            {
                ExitMutation();
                throw;
            }
        }

        private void PruneOwnerStates()
        {
            var activeOwners = _bindingsByHandle.Values
                .Select(binding => binding.Owner)
                .ToHashSet();
            foreach (var owner in _ownerStates.Keys
                .Where(owner => !activeOwners.Contains(owner))
                .ToArray())
            {
                _ownerStates.Remove(owner);
            }
        }

        private ManagedSubscriptionItemBinding CreateBinding(string name,
            ISubscriber owner, BaseMonitoredItemModel template, string? rootName,
            IReadOnlyList<string> triggeredByNames,
            IDictionary<string, Guid>? dataSetClassFieldIds = null)
        {
            var effective = EnsureDataSetClassFieldId(template.SetDefaults(_options),
                name, dataSetClassFieldIds: dataSetClassFieldIds);
            return new ManagedSubscriptionItemBinding(name, owner, effective,
                ManagedSubscriptionOptionsAdapter.ToManagedOptions(effective, _options, _codec,
                    rootName ?? name, triggeredByNames),
                rootName ?? name, triggeredByNames, _codec.Context, _timeProvider,
                OnHeartbeatTimer, _publishingInterval);
        }

        private async ValueTask<(List<(string Name, ISubscriber Owner,
            BaseMonitoredItemModel Template)> Items, bool HasFailures)>
            PrepareMonitoredItemsAsync(
            IEnumerable<(ISubscriber Owner, BaseMonitoredItemModel Template)> items,
            IDictionary<string, Guid> dataSetClassFieldIds, CancellationToken ct)
        {
            var prepared = new List<(string Name, ISubscriber Owner,
                BaseMonitoredItemModel Template)>();
            var preparationFailures = 0;
            var names = new Dictionary<string, int>(StringComparer.Ordinal);
            var pending = new List<(string Name, ISubscriber Owner,
                BaseMonitoredItemModel Template)>();
            foreach (var (owner, template) in items)
            {
                var prefix = GetNamePrefix(template);
                names.TryGetValue(prefix, out var ordinal);
                names[prefix] = ordinal + 1;
                var rootName = $"{prefix}:{ordinal}";
                pending.Add((rootName, owner, template));
            }
            for (var offset = 0; offset < pending.Count;
                offset += kPreparationBatchSize)
            {
                var count = Math.Min(kPreparationBatchSize,
                    pending.Count - offset);
                var tasks = new Task<(string Name, ISubscriber Owner,
                    BaseMonitoredItemModel? Template)>[count];
                for (var index = 0; index < count; index++)
                {
                    var item = pending[offset + index];
                    tasks[index] = PrepareRootAsync(item);
                }
                foreach (var result in await Task.WhenAll(tasks).ConfigureAwait(false))
                {
                    if (result.Template != null)
                    {
                        prepared.Add((result.Name, result.Owner, result.Template));
                    }
                }
            }
            return (prepared, Volatile.Read(ref preparationFailures) != 0);

            async Task<(string Name, ISubscriber Owner,
                BaseMonitoredItemModel? Template)> PrepareRootAsync(
                (string Name, ISubscriber Owner,
                    BaseMonitoredItemModel Template) item)
            {
                var result = await PrepareMonitoredItemAsync(item.Owner,
                    item.Template, item.Name, ct).ConfigureAwait(false);
                return (item.Name, item.Owner, result);
            }

            async ValueTask<BaseMonitoredItemModel?> PrepareMonitoredItemAsync(
                ISubscriber owner, BaseMonitoredItemModel template,
                string name, CancellationToken cancellationToken)
            {
                template = EnsureDataSetClassFieldId(template, name,
                    dataSetClassFieldIds: dataSetClassFieldIds);
                BaseMonitoredItemModel result;
                try
                {
                    result = _monitoredItemPreparation == null ? template :
                        await _monitoredItemPreparation(template, cancellationToken)
                            .ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Interlocked.Exchange(ref preparationFailures, 1);
                    InvokeSubscriber(owner, subscriber => subscriber.OnMonitoredItemUpdate(
                        template, new ServiceResult(ex).ToServiceResultModel()));
                    return null;
                }
                if (result.TriggeredItems == null)
                {
                    return result;
                }
                var triggeredItems = new List<BaseMonitoredItemModel>(
                    result.TriggeredItems.Count);
                for (var index = 0; index < result.TriggeredItems.Count; index++)
                {
                    var triggered = result.TriggeredItems[index];
                    var preparedTriggered = await PrepareMonitoredItemAsync(owner,
                        triggered, $"{name}/triggered/{index}:{GetNamePrefix(triggered)}",
                        cancellationToken).ConfigureAwait(false);
                    if (preparedTriggered != null)
                    {
                        triggeredItems.Add(preparedTriggered);
                    }
                }
                return result with
                {
                    TriggeredItems = triggeredItems
                };
            }
        }

        private void UpdatePreparationRetry(
            (ISubscriber Owner, BaseMonitoredItemModel Template)[] requestedItems,
            bool preparationFailed, long requestVersion)
        {
            lock (_preparationRetryLock)
            {
                if (requestVersion != Volatile.Read(ref _preparationRequestVersion) ||
                    requestVersion != Volatile.Read(ref _preparationCommittedVersion) ||
                    Volatile.Read(ref _disposed) != 0)
                {
                    return;
                }
                if (!preparationFailed)
                {
                    _preparationRetryPending = false;
                    _preparationRetryAttempt = 0;
                    _preparationRetryItems = [];
                    _preparationRetryTimer.Change(
                        Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
                    return;
                }
                var delay = ManagedSubscriptionRetryPolicy.GetDelay(
                    ManagedItemRetryKind.Invalid, _options,
                    _preparationRetryAttempt);
                if (_preparationRetryAttempt < int.MaxValue)
                {
                    _preparationRetryAttempt++;
                }
                _preparationRetryItems = requestedItems;
                _preparationRetryVersion = requestVersion;
                _preparationRetryPending = delay != TimeSpan.MaxValue;
                _preparationRetryTimer.Change(
                    _preparationRetryPending ? delay : Timeout.InfiniteTimeSpan,
                    Timeout.InfiniteTimeSpan);
            }
        }

        private void OnPreparationRetryTimer(object? state)
        {
            _ = state;
            lock (_preparationRetryLock)
            {
                if (!_preparationRetryPending ||
                    Volatile.Read(ref _disposed) != 0 ||
                    _preparationRetryTask is { IsCompleted: false })
                {
                    if (_preparationRetryPending &&
                        _preparationRetryTask is { IsCompleted: false })
                    {
                        _preparationRetryTimerMissed = true;
                    }
                    return;
                }
                _preparationRetryPending = false;
                var items = _preparationRetryItems;
                var version = _preparationRetryVersion;
                _preparationRetryTask = RunPreparationRetryAsync(items, version);
            }
        }

        private async Task RunPreparationRetryAsync(
            (ISubscriber Owner, BaseMonitoredItemModel Template)[] items,
            long requestVersion)
        {
            try
            {
                await UpdateAsync(items, requestVersion, _disposeCts.Token,
                    _disposeCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (
                _disposeCts.IsCancellationRequested ||
                requestVersion != Volatile.Read(ref _preparationRequestVersion))
            {
            }
            catch (ObjectDisposedException) when (Volatile.Read(ref _disposed) != 0)
            {
            }
            catch (Exception ex)
            {
                RecordBackgroundError(ex);
                _logger.PreparationRetryFailed(ex);
                UpdatePreparationRetry(items, preparationFailed: true,
                    requestVersion);
            }
            finally
            {
                lock (_preparationRetryLock)
                {
                    _preparationRetryTask = null;
                    if (_preparationRetryTimerMissed &&
                        _preparationRetryPending &&
                        Volatile.Read(ref _disposed) == 0)
                    {
                        _preparationRetryTimerMissed = false;
                        _preparationRetryTimer.Change(TimeSpan.Zero,
                            Timeout.InfiniteTimeSpan);
                    }
                }
            }
        }

        private void RestorePreparationRetryAfterFailedUpdate(long requestVersion)
        {
            var committedVersion = Volatile.Read(ref _preparationCommittedVersion);
            if (Interlocked.CompareExchange(ref _preparationRequestVersion,
                committedVersion, requestVersion) != requestVersion)
            {
                return;
            }
            lock (_preparationRetryLock)
            {
                if (_preparationRetryItems.Length == 0 ||
                    _preparationRetryVersion != committedVersion ||
                    Volatile.Read(ref _disposed) != 0)
                {
                    return;
                }
                var delay = ManagedSubscriptionRetryPolicy.GetDelay(
                    ManagedItemRetryKind.Invalid, _options,
                    _preparationRetryAttempt);
                if (_preparationRetryAttempt < int.MaxValue)
                {
                    _preparationRetryAttempt++;
                }
                _preparationRetryPending = delay != TimeSpan.MaxValue;
                _preparationRetryTimer.Change(
                    _preparationRetryPending ? delay : Timeout.InfiniteTimeSpan,
                    Timeout.InfiniteTimeSpan);
            }
        }

        private void CommitDataSetClassFieldIds(
            IDictionary<string, Guid> dataSetClassFieldIds, bool prune)
        {
            if (prune)
            {
                var active = GetBindings()
                    .Where(binding => binding.Template is DataMonitoredItemModel)
                    .Select(binding => binding.Name)
                    .ToHashSet(StringComparer.Ordinal);
                foreach (var name in dataSetClassFieldIds.Keys
                    .Where(name => !active.Contains(name))
                    .ToArray())
                {
                    dataSetClassFieldIds.Remove(name);
                }
            }
            _dataSetClassFieldIds.Clear();
            foreach (var (name, fieldId) in dataSetClassFieldIds)
            {
                _dataSetClassFieldIds.Add(name, fieldId);
            }
        }

        private BaseMonitoredItemModel EnsureDataSetClassFieldId(
            BaseMonitoredItemModel template, string name,
            BaseMonitoredItemModel? previous = null,
            IDictionary<string, Guid>? dataSetClassFieldIds = null)
        {
            dataSetClassFieldIds ??= _dataSetClassFieldIds;
            if (template is not DataMonitoredItemModel data)
            {
                return template;
            }
            if (data.DataSetClassFieldId != Guid.Empty)
            {
                if (!dataSetClassFieldIds.ContainsKey(name))
                {
                    dataSetClassFieldIds[name] = data.DataSetClassFieldId;
                }
                return template;
            }
            var fieldId = previous is DataMonitoredItemModel existing &&
                    existing.DataSetClassFieldId != Guid.Empty
                ? existing.DataSetClassFieldId
                : dataSetClassFieldIds.TryGetValue(name, out var cached)
                    ? cached
                    : Guid.NewGuid();
            dataSetClassFieldIds[name] = fieldId;
            return data with
            {
                DataSetClassFieldId = fieldId
            };
        }

        private async ValueTask ResolveBrowsePathsFromRootAsync(
            IEnumerable<ManagedSubscriptionItemBinding> bindings, CancellationToken ct)
        {
            if (!_resolveBrowsePathFromRoot || _browsePathFromRootResolver == null)
            {
                return;
            }
            var allBindings = bindings.ToArray();
            for (var offset = 0; offset < allBindings.Length;
                offset += kPreparationBatchSize)
            {
                var batch = allBindings
                    .Skip(offset)
                    .Take(kPreparationBatchSize)
                    .ToArray();
                var paths = await Task.WhenAll(batch.Select(async binding =>
                    (Binding: binding, Path: await _browsePathFromRootResolver(
                        binding.Template.StartNodeId, ct).ConfigureAwait(false))))
                    .ConfigureAwait(false);
                foreach (var (binding, path) in paths)
                {
                    binding.PathFromRoot = path;
                }
            }
        }

        private void OnHeartbeatTimer(ManagedSubscriptionItemBinding binding)
        {
            try
            {
                EmitHeartbeat(binding);
            }
            catch (Exception ex)
            {
                RecordBackgroundError(ex);
                _logger.HeartbeatFailed(ex, binding.Name);
            }
        }

        private async ValueTask ProcessModelChangeAsync(ManagedSubscriptionItemBinding binding,
            ArrayOf<Variant> fields, CancellationToken ct)
        {
            if (fields.Count < 2 ||
                !fields[0].TryGetValue(out NodeId eventType) ||
                eventType != ObjectTypeIds.BaseModelChangeEventType &&
                eventType != ObjectTypeIds.GeneralModelChangeEventType)
            {
                return;
            }
            await InvokeModelChangeBrowserAsync(binding, ct)
                .ConfigureAwait(false);
        }

        private async ValueTask ProcessConditionAsync(ISubscription subscription,
            ManagedSubscriptionItemBinding binding, uint sequenceNumber, DateTime publishTime,
            ArrayOf<Variant> fields)
        {
            var condition = binding.Condition!;
            lock (condition._lock)
            {
                if (condition.Superseded)
                {
                    return;
                }
            }
            if (condition.EventTypeIndex >= 0 && condition.EventTypeIndex < fields.Count &&
                fields[condition.EventTypeIndex].TryGetValue(out NodeId eventType))
            {
                if (eventType == ObjectTypeIds.RefreshStartEventType)
                {
                    lock (condition._lock)
                    {
                        condition.Active.Clear();
                        condition.Refreshing = true;
                    }
                    return;
                }
                if (eventType == ObjectTypeIds.RefreshEndEventType)
                {
                    FlushCondition(binding, force: true, endRefresh: true);
                    return;
                }
                if (eventType == ObjectTypeIds.RefreshRequiredEventType)
                {
                    try
                    {
                        await subscription.ConditionRefreshAsync().ConfigureAwait(false);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        RecordBackgroundError(ex);
                        _logger.ConditionRefreshFailed(ex);
                    }
                    return;
                }
            }
            lock (condition._lock)
            {
                if (condition.ConditionIdIndex < 0 || condition.RetainIndex < 0 ||
                    condition.ConditionIdIndex >= fields.Count || condition.RetainIndex >= fields.Count)
                {
                    return;
                }
                var id = fields[condition.ConditionIdIndex].ToString();
                if (string.IsNullOrEmpty(id))
                {
                    return;
                }
                var retain = fields[condition.RetainIndex].TryGetValue(out bool value) && value;
                if (!retain)
                {
                    condition.Active.Remove(id);
                    condition.Dirty = true;
                    return;
                }
                var notifications = new List<MonitoredItemNotificationModel>();
                AddEventNotifications(notifications, binding, fields);
                if (notifications.Count == 0)
                {
                    return;
                }
                foreach (var notification in notifications)
                {
                    if (notification.Value is { } dataValue)
                    {
                        notification.Value = new DataValue(dataValue.WrappedValue,
                            dataValue.StatusCode, publishTime, dataValue.ServerTimestamp,
                            dataValue.SourcePicoseconds, dataValue.ServerPicoseconds);
                    }
                }
                condition.Active[id] = notifications.Select(CloneNotification).ToList();
                condition.Dirty = true;
            }
        }

        private void FlushCondition(ManagedSubscriptionItemBinding binding,
            bool force, bool endRefresh)
        {
            var condition = binding.Condition!;
            var now = _timeProvider.GetUtcNow();
            List<MonitoredItemNotificationModel>? notifications = null;
            lock (condition._lock)
            {
                if (endRefresh)
                {
                    condition.Refreshing = false;
                    condition.Dirty = true;
                }
                if (condition.Superseded || condition.Refreshing || condition.Publishing ||
                    !force && now < condition.LastSent +
                    TimeSpan.FromSeconds(condition.SnapshotInterval) &&
                    (!condition.Dirty || now < condition.LastSent +
                        TimeSpan.FromSeconds(condition.UpdateInterval)))
                {
                    return;
                }
                condition.Publishing = true;
                notifications = condition.Active.Values
                    .SelectMany(value => value.Select(CloneNotification))
                    .ToList();
                condition.Dirty = false;
                condition.LastSent = now;
            }
            try
            {
                if (notifications.Count != 0 &&
                    ReferenceEquals(binding.Condition, condition) &&
                    IsLiveOwner(binding.Owner))
                {
                    using var message = CreateNotification(notifications,
                        now.UtcDateTime, MessageType.Condition,
                        eventTypeName: binding.Template.DisplayName);
                    Deliver(binding.Owner, message,
                        static (owner, notification) => owner.OnSubscriptionEventReceived(notification));
                    InvokeSubscriber(binding.Owner,
                        owner => owner.OnSubscriptionEventDiagnosticsChange(true,
                            notifications.Count, notifications.Sum(item => item.Overflow), 0));
                }
            }
            finally
            {
                lock (condition._lock)
                {
                    condition.Publishing = false;
                }
            }
        }

        private MonitoredItemNotificationModel CreateDataNotification(
            ManagedSubscriptionItemBinding binding, DataValue value,
            uint? sequenceNumber = null)
        {
            return new MonitoredItemNotificationModel
            {
                Id = binding.Template.DataSetFieldId ?? string.Empty,
                DataSetFieldName = binding.Template.DisplayName,
                DataSetName = binding.Template.DisplayName,
                NodeId = binding.Template.StartNodeId,
                PathFromRoot = binding.PathFromRoot,
                Value = Clone(value),
                Flags = 0,
                Overflow = value.StatusCode.Overflow ? 1 : 0,
                SequenceNumber = sequenceNumber ?? binding.NextSequenceNumber()
            };
        }

        private MonitoredItemNotificationModel CreateCyclicReadNotification(
            in CyclicReadItem item, in DataValue value,
            uint sequenceNumber, int overflow)
        {
            return new MonitoredItemNotificationModel
            {
                Id = item.Id,
                DataSetFieldName = item.DisplayName,
                DataSetName = item.DisplayName,
                NodeId = item.NodeId,
                PathFromRoot = item.Binding.PathFromRoot,
                Value = Clone(value),
                Flags = 0,
                Overflow = overflow > 0
                    ? overflow
                    : value.StatusCode.Overflow ? 1 : 0,
                SequenceNumber = sequenceNumber
            };
        }

        private void EmitHeartbeat(ManagedSubscriptionItemBinding binding, bool force = false)
        {
            if (Volatile.Read(ref _disposed) != 0 ||
                !TryGetBindingByName(binding.Name, out var current) ||
                !ReferenceEquals(current, binding) ||
                !IsLiveOwner(binding.Owner) ||
                !binding.TryCaptureHeartbeat(_timeProvider.GetUtcNow(), force,
                    out var heartbeat))
            {
                return;
            }

            var value = heartbeat.Value;
            if ((heartbeat.Behavior & HeartbeatBehavior.WatchdogLKG) ==
                HeartbeatBehavior.WatchdogLKG && !IsGoodDataValue(value))
            {
                return;
            }
            if (!value.HasValue && TryGetMonitoredItem(binding, out var monitoredItem) &&
                ServiceResult.IsNotGood(monitoredItem!.Error))
            {
                value = DataValue.FromStatusCode(monitoredItem.Error.StatusCode);
            }
            if (!value.HasValue)
            {
                return;
            }

            var heartbeatValue = value.Value;
            if ((heartbeat.Behavior &
                HeartbeatBehavior.WatchdogLKVWithUpdatedTimestamps) ==
                HeartbeatBehavior.WatchdogLKVWithUpdatedTimestamps)
            {
                var elapsed = heartbeat.ReceivedAt.HasValue ?
                    heartbeat.SignalTime - heartbeat.ReceivedAt.Value : TimeSpan.Zero;
                heartbeatValue = new DataValue(heartbeatValue.WrappedValue,
                    heartbeatValue.StatusCode,
                    heartbeatValue.SourceTimestamp == DateTimeUtc.MinValue ?
                        DateTimeUtc.MinValue :
                        heartbeatValue.SourceTimestamp.ToDateTime().Add(elapsed),
                    heartbeatValue.ServerTimestamp == DateTimeUtc.MinValue ?
                        DateTimeUtc.MinValue :
                        heartbeatValue.ServerTimestamp.ToDateTime().Add(elapsed),
                    heartbeatValue.SourcePicoseconds,
                    heartbeatValue.ServerPicoseconds);
            }

            if (!binding.IsHeartbeatCurrent(heartbeat.ItemSequenceNumber))
            {
                return;
            }
            var notification = new MonitoredItemNotificationModel
            {
                Id = binding.Template.DataSetFieldId ?? string.Empty,
                DataSetFieldName = binding.Template.DisplayName,
                DataSetName = binding.Template.DisplayName,
                NodeId = binding.Template.StartNodeId,
                PathFromRoot = binding.PathFromRoot,
                Value = Clone(heartbeatValue),
                Flags = MonitoredItemSourceFlags.Heartbeat,
                Overflow = 0,
                SequenceNumber = heartbeat.ItemSequenceNumber
            };
            using var message = new OpcUaSubscriptionNotification(
                heartbeat.SignalTime, _codec.Context as ServiceMessageContext,
                [notification], keyFrameSnapshotProvider: this)
            {
                ApplicationUri = _applicationUriProvider?.Invoke(),
                EndpointUrl = _endpointUrlProvider?.Invoke(),
                MessageType = MessageType.DeltaFrame,
                SequenceNumber = NextSequenceNumber()
            };
            var diagnosticsOnly = (heartbeat.Behavior &
                HeartbeatBehavior.WatchdogLKVDiagnosticsOnly) ==
                HeartbeatBehavior.WatchdogLKVDiagnosticsOnly;
            // Hold the delivery gate across the final staleness check and delivery so
            // that a heartbeat (timer thread) cannot slip between the value-change path's
            // RecordDataChange call and its own Deliver call.  All binding/heartbeat locks
            // are released before entering the gate; see _deliveryGate documentation for
            // the full lock-ordering argument.
            lock (_deliveryGate)
            {
                if (!TryGetBindingByName(binding.Name, out current) ||
                    !ReferenceEquals(current, binding) ||
                    !IsLiveOwner(binding.Owner) ||
                    !binding.IsHeartbeatCurrent(heartbeat.ItemSequenceNumber))
                {
                    return;
                }
                if (!diagnosticsOnly)
                {
                    Deliver(binding.Owner, message,
                        static (subscriber, heartbeatNotification) =>
                            subscriber.OnSubscriptionDataChangeReceived(heartbeatNotification));
                }
            }
            if (IsLiveOwner(binding.Owner))
            {
                InvokeSubscriber(binding.Owner, subscriber =>
                    subscriber.OnSubscriptionDataDiagnosticsChange(false, 1,
                        notification.Overflow, 1));
            }
        }

        private static bool IsGoodDataValue(DataValue? value)
        {
            if (!value.HasValue)
            {
                return false;
            }
            var dataValue = value.Value;
            return dataValue.StatusCode == StatusCodes.Good ||
                dataValue.WrappedValue != Variant.Null &&
                !StatusCode.IsBad(dataValue.StatusCode);
        }

        private void AddEventNotifications(List<MonitoredItemNotificationModel> notifications,
            ManagedSubscriptionItemBinding binding, ArrayOf<Variant> fields)
        {
            var names = binding.EventFieldNames;
            var count = Math.Min(names.Count, fields.Count);
            var itemSequenceNumber = binding.NextSequenceNumber();
            for (var index = 0; index < count; index++)
            {
                if (names[index] == null)
                {
                    continue;
                }
                notifications.Add(new MonitoredItemNotificationModel
                {
                    Id = binding.Template.Id ?? string.Empty,
                    DataSetName = binding.Template.DisplayName,
                    DataSetFieldName = names[index],
                    NodeId = binding.Template.StartNodeId,
                    PathFromRoot = binding.PathFromRoot,
                    Value = Clone(new DataValue(fields[index])),
                    SequenceNumber = itemSequenceNumber
                });
            }
        }

        private OpcUaSubscriptionNotification? CreateKeyFrame(ISubscriber owner,
            DateTime publishTime, uint? publishSequenceNumber = null,
            IReadOnlyDictionary<string, uint>? currentSequences = null)
        {
            if (!TryGetKeyFrameNotifications(owner, out var values,
                currentSequences))
            {
                return null;
            }
            return CreateNotification(values, publishTime, MessageType.KeyFrame,
                publishSequenceNumber);
        }

        private bool TryGetKeyFrameNotifications(ISubscriber owner,
            [NotNullWhen(true)] out IList<MonitoredItemNotificationModel>? notifications,
            IReadOnlyDictionary<string, uint>? currentSequences = null)
        {
            notifications = null;
            if (!IsLiveOwner(owner))
            {
                return false;
            }
            var values = GetBindings()
                .Where(binding => binding.Owner.Equals(owner) &&
                    binding.Template is DataMonitoredItemModel &&
                    !binding.IsCyclicRead &&
                    !binding.DropDataChange)
                .Select(binding => CreateDataNotification(binding,
                    binding.LastDataValue ?? DataValue.FromStatusCode(StatusCodes.BadNoData),
                    currentSequences?.TryGetValue(binding.Name, out var sequenceNumber) == true ?
                        sequenceNumber : null))
                .ToList();
            if (values.Count == 0)
            {
                return false;
            }
            notifications = values;
            return true;
        }

        private OpcUaSubscriptionNotification CreateNotification(
            IList<MonitoredItemNotificationModel> notifications,
            DateTime publishTime, MessageType messageType,
            uint? publishSequenceNumber = null, string? eventTypeName = null)
        {
            return new OpcUaSubscriptionNotification(_timeProvider.GetUtcNow(),
                _codec.Context as ServiceMessageContext, notifications,
                publishSequenceNumber, keyFrameSnapshotProvider: this)
            {
                ApplicationUri = _applicationUriProvider?.Invoke(),
                EndpointUrl = _endpointUrlProvider?.Invoke(),
                EventTypeName = eventTypeName,
                MessageType = messageType,
                PublishTimestamp = new DateTimeOffset(publishTime),
                SequenceNumber = NextSequenceNumber()
            };
        }

        private bool RequiresKeyFrame(ISubscriber owner)
        {
            lock (_bindingsLock)
            {
                if (!_bindingsByHandle.Values.Any(binding => binding.Owner.Equals(owner)) ||
                    !_ownerStates.TryGetValue(owner, out var state))
                {
                    return false;
                }

                return state.KeyFrameRequired || _periodicKeyFrameInterval.HasValue &&
                    _timeProvider.GetUtcNow() >= state.LastKeyFrame +
                    _periodicKeyFrameInterval.Value;
            }
        }

        private bool IsLiveOwner(ISubscriber owner)
        {
            lock (_bindingsLock)
            {
                return _bindingsByHandle.Values.Any(binding => binding.Owner.Equals(owner));
            }
        }

        private void MarkKeyFrameDelivered(ISubscriber owner, bool delivered)
        {
            if (!delivered)
            {
                return;
            }
            lock (_bindingsLock)
            {
                if (_bindingsByHandle.Values.Any(binding => binding.Owner.Equals(owner)) &&
                    _ownerStates.TryGetValue(owner, out var state))
                {
                    state.KeyFrameRequired = false;
                    state.LastKeyFrame = _timeProvider.GetUtcNow();
                }
            }
        }

        private bool TryGetBinding(IMonitoredItem? monitoredItem,
            out ManagedSubscriptionItemBinding binding)
        {
            binding = null!;
            return monitoredItem != null && TryGetBinding(monitoredItem.ClientHandle, out binding);
        }

        private bool TryGetBinding(uint clientHandle, out ManagedSubscriptionItemBinding binding)
        {
            lock (_bindingsLock)
            {
                return _bindingsByHandle.TryGetValue(clientHandle, out binding!);
            }
        }

        private bool TryGetBindingByName(string name, out ManagedSubscriptionItemBinding binding)
        {
            lock (_bindingsLock)
            {
                return _bindingsByName.TryGetValue(name, out binding!);
            }
        }

        private bool TryGetMonitoredItem(ManagedSubscriptionItemBinding binding,
            out IMonitoredItem? monitoredItem)
        {
            return _subscription.MonitoredItems.TryGetMonitoredItemByClientHandle(
                binding.ClientHandle, out monitoredItem);
        }

        private ManagedSubscriptionItemBinding[] GetBindings()
        {
            lock (_bindingsLock)
            {
                return [.. _bindingsByHandle.Values];
            }
        }

        private OwnerState GetOwnerState(ISubscriber owner)
        {
            if (!_ownerStates.TryGetValue(owner, out var state))
            {
                state = new OwnerState(_timeProvider.GetUtcNow());
                _ownerStates.Add(owner, state);
            }
            return state;
        }

        private string CreateName(BaseMonitoredItemModel template)
        {
            return $"{GetNamePrefix(template)}:{Interlocked.Increment(ref _nextItemName)}";
        }

        private static string GetNamePrefix(BaseMonitoredItemModel template)
        {
            return string.IsNullOrWhiteSpace(template.Id) ? "monitored-item" : template.Id;
        }

        private DataValue Clone(in DataValue value)
        {
            using var stream = new MemoryStream();
            using (var encoder = new BinaryEncoder(stream, _codec.Context, leaveOpen: true))
            {
                encoder.WriteDataValue(null, value);
            }
            stream.Position = 0;
            using var decoder = new BinaryDecoder(stream, _codec.Context, leaveOpen: true);
            return decoder.ReadDataValue(null);
        }

        private MonitoredItemNotificationModel CloneNotification(
            MonitoredItemNotificationModel notification)
        {
            return notification with
            {
                Value = notification.Value.HasValue ? Clone(notification.Value.Value) : null
            };
        }

        private static ServiceResultModel? ToServiceResult(
            IMonitoredItem monitoredItem, StatusCode status)
        {
            if (StatusCode.IsGood(status))
            {
                return null;
            }
            return monitoredItem.Error?.StatusCode == status
                ? monitoredItem.Error.ToServiceResultModel()
                : new ServiceResult(status).ToServiceResultModel();
        }

        private void OnKeyFrameTimer(object? state)
        {
            foreach (var owner in GetBindings().Select(binding => binding.Owner).Distinct())
            {
                RequestKeyFrame(owner);
                if (!RequiresKeyFrame(owner))
                {
                    continue;
                }
                var notification = CreateKeyFrame(owner,
                    _timeProvider.GetUtcNow().UtcDateTime);
                if (notification != null && IsLiveOwner(owner))
                {
                    MarkKeyFrameDelivered(owner, true);
                    using (notification)
                    {
                        lock (_deliveryGate)
                        {
                            Deliver(owner, notification,
                                static (subscriber, keyFrame) =>
                                    subscriber.OnSubscriptionDataChangeReceived(keyFrame));
                        }
                    }
                }
            }
        }

        private void OnConditionTimer(object? state)
        {
            FlushConditions();
        }

        private void OnWatchdogTimer(object? state)
        {
            try
            {
                EvaluateWatchdog();
            }
            catch (Exception ex)
            {
                RecordBackgroundError(ex);
                _logger.WatchdogFailed(ex);
            }
        }

        private void EvaluateWatchdog()
        {
            long lastCheck;
            lock (_watchdogLock)
            {
                if (_watchdogDisposing || Volatile.Read(ref _disposed) != 0 ||
                    !_watchdogEnabled || !_watchdogCheckInitialized)
                {
                    return;
                }
                lastCheck = _lastWatchdogCheckTimestamp;
                _lastWatchdogCheckTimestamp = _timeProvider.GetTimestamp();
            }

            var bindings = GetBindings()
                .Where(binding => binding.WatchdogEligible)
                .ToArray();
            if (bindings.Length == 0)
            {
                return;
            }
            var late = bindings.Count(binding => binding.WasLateSince(lastCheck));
            if (late == 0)
            {
                return;
            }
            _logger.WatchdogItemsLate(late, bindings.Length, _watchdogBehavior);
            if (_watchdogBehavior == SubscriptionWatchdogBehavior.Diagnostic ||
                _watchdogCondition == MonitoredItemWatchdogCondition.WhenAllAreLate &&
                late != bindings.Length)
            {
                return;
            }
            var message = $"Performed watchdog action {_watchdogBehavior} because " +
                $"{late} of {bindings.Length} managed monitored items are late.";
            RunWatchdogAction(_watchdogBehavior, message, requireEnabled: true);
        }

        private void RunWatchdogAction(SubscriptionWatchdogBehavior behavior,
            string message, bool requireEnabled)
        {
            lock (_watchdogLock)
            {
                if (_watchdogDisposing || Volatile.Read(ref _disposed) != 0)
                {
                    return;
                }
                if (requireEnabled && (!_watchdogEnabled ||
                    !_watchdogConnected || !_watchdogPublishingEnabled))
                {
                    return;
                }
                if (behavior == SubscriptionWatchdogBehavior.Reset)
                {
                    if (Interlocked.Exchange(ref _watchdogResetInProgress, 1) != 0)
                    {
                        return;
                    }
                    _watchdogPublishingEnabled = false;
                    UpdateWatchdogTimer();
                }
                if (_watchdogAction == null)
                {
                    if (behavior == SubscriptionWatchdogBehavior.Reset)
                    {
                        CompleteFailedWatchdogReset();
                    }
                    return;
                }
                try
                {
                    _watchdogAction(this, behavior, message);
                }
                catch
                {
                    if (behavior == SubscriptionWatchdogBehavior.Reset)
                    {
                        CompleteFailedWatchdogReset();
                    }
                    throw;
                }
            }
        }

        private void CompleteFailedWatchdogReset()
        {
            Interlocked.Exchange(ref _watchdogResetInProgress, 0);
            _watchdogPublishingEnabled =
                !_watchdogPublishingStopped &&
                _subscription.CurrentPublishingEnabled;
            UpdateWatchdogTimer();
        }

        private void UpdateWatchdogTimer()
        {
            var enabled = !_watchdogDisposing &&
                Volatile.Read(ref _disposed) == 0 &&
                _watchdogTimeout > TimeSpan.Zero &&
                _watchdogConnected &&
                _watchdogPublishingEnabled;
            if (!enabled)
            {
                _watchdogEnabled = false;
                _watchdogCheckInitialized = false;
                _lastWatchdogCheckTimestamp = 0;
                if (!_watchdogTimerDisposed)
                {
                    _watchdogTimer.Change(Timeout.InfiniteTimeSpan,
                        Timeout.InfiniteTimeSpan);
                }
                return;
            }
            if (_watchdogEnabled)
            {
                return;
            }
            _watchdogEnabled = true;
            _watchdogCheckInitialized = true;
            _lastWatchdogCheckTimestamp = _timeProvider.GetTimestamp();
            _watchdogTimer.Change(_watchdogTimeout, _watchdogTimeout);
        }

        private uint NextSequenceNumber()
        {
            return IncrementSequenceNumber(ref _sequenceNumber);
        }

        /// <summary>
        /// Advance a sequence number, skipping zero.
        /// </summary>
        /// <remarks>
        /// Part 6 reserves zero as "unknown", so a counter that wraps has to
        /// step over it rather than hand it out. This was the only surviving
        /// member of a sequence-number helper that also tracked gaps and
        /// republish; upstream
        /// <c>Opc.Ua.Client\Subscription\MessageProcessor</c> owns that now, so
        /// the rest went with it.
        /// </remarks>
        /// <param name="sequenceNumber">The counter to advance.</param>
        private static uint IncrementSequenceNumber(ref uint sequenceNumber)
        {
            while (true)
            {
                var result = Interlocked.Increment(ref sequenceNumber);
                if (result != 0)
                {
                    return result;
                }
            }
        }

        private void InvokeSubscriber(ISubscriber owner, Action<ISubscriber> callback)
        {
            try
            {
                callback(owner);
            }
            catch (Exception ex)
            {
                _logger.SubscriberCallbackFailed(ex, owner.GetType().Name);
            }
        }

        private void Deliver(ISubscriber owner, OpcUaSubscriptionNotification notification,
            Action<ISubscriber, OpcUaSubscriptionNotification> callback)
        {
            InvokeSubscriber(owner, subscriber => callback(subscriber, notification));
        }

        private async ValueTask InvokeSubscriberAsync(ISubscriber owner,
            Func<ISubscriber, Task> callback)
        {
            try
            {
                await callback(owner).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.SubscriberCallbackFailed(ex, owner.GetType().Name);
            }
        }

        private async ValueTask InvokeModelChangeBrowserAsync(
            ManagedSubscriptionItemBinding binding, CancellationToken ct)
        {
            try
            {
                ModelChangeBrowserRegistration? registration;
                lock (_modelChangeBrowsersLock)
                {
                    _modelChangeBrowsers.TryGetValue(binding.Name, out registration);
                }
                registration?.Browser.Rebrowse();
                await InvokeSubscriberAsync(binding.Owner, subscriber =>
                    subscriber.OnMonitoredItemSemanticsChangedAsync(ct)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                RecordBackgroundError(ex);
                _logger.ModelChangeBrowserFailed(ex, binding.Owner.GetType().Name);
            }
        }

        private void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        }

        private void RecordBackgroundError(Exception exception)
        {
            Volatile.Write(ref _backgroundError, exception);
        }

        private readonly record struct CyclicReadGroupKey(
            TimeSpan SamplingInterval,
            TimeSpan MaxAge);

        private readonly record struct CyclicReadItem(
            ManagedSubscriptionItemBinding Binding,
            long Generation,
            ManagedCyclicReadRequest Request,
            ISubscriber Owner,
            string Id,
            string DisplayName,
            string NodeId,
            bool SkipFirst);

        private sealed class CyclicReadGroup : IAsyncDisposable
        {
            public CyclicReadGroup(
                ManagedSubscriptionAdapter adapter,
                IManagedCyclicReadClient client,
                CyclicReadGroupKey key,
                IReadOnlyList<CyclicReadItem> items,
                TimeProvider timeProvider,
                CancellationToken lifetimeCt)
            {
                _adapter = adapter;
                _client = client;
                _key = key;
                _items = [.. items];
                _timeProvider = timeProvider;
                _cts = CancellationTokenSource.CreateLinkedTokenSource(lifetimeCt);
            }

            public void Start()
            {
                if (Interlocked.Exchange(ref _started, 1) == 0)
                {
                    _worker = RunAsync(_cts.Token);
                }
            }

            public void Update(IReadOnlyList<CyclicReadItem> items)
            {
                lock (_lock)
                {
                    _items = [.. items];
                }
            }

            public async ValueTask DisposeAsync()
            {
                if (Interlocked.Exchange(ref _disposed, 1) != 0)
                {
                    return;
                }
                try
                {
                    await _cts.CancelAsync().ConfigureAwait(false);
                    if (_worker != null)
                    {
                        await _worker.ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException) when (_cts.IsCancellationRequested)
                {
                }
                finally
                {
                    _cts.Dispose();
                }
            }

            private async Task RunAsync(CancellationToken ct)
            {
                var delayUntilNext = _key.SamplingInterval;
                var carriedMissedCycles = 0;
                while (!ct.IsCancellationRequested)
                {
                    var cycleStarted = _timeProvider.GetTimestamp();
                    await Task.Delay(delayUntilNext, _timeProvider, ct)
                        .ConfigureAwait(false);

                    CyclicReadItem[] items;
                    lock (_lock)
                    {
                        items = [.. _items];
                    }
                    if (items.Length == 0)
                    {
                        delayUntilNext = _key.SamplingInterval;
                        carriedMissedCycles = 0;
                        continue;
                    }

                    IReadOnlyList<DataValue> values;
                    try
                    {
                        values = await _client.ReadAsync(
                            items.Select(item => item.Request).ToArray(),
                            _key.SamplingInterval, _key.MaxAge, ct)
                            .ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (ct.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        _adapter.RecordBackgroundError(ex);
                        _adapter._logger.CyclicReadFailed(ex);
                        var status = new ServiceResult(ex).StatusCode;
                        values = Enumerable.Repeat(
                            DataValue.FromStatusCode(status), items.Length).ToArray();
                    }

                    var readCompleted = _timeProvider.GetTimestamp();
                    var elapsed = _timeProvider.GetElapsedTime(
                        cycleStarted, readCompleted);
                    var overrun = elapsed > delayUntilNext
                        ? elapsed - delayUntilNext
                        : TimeSpan.Zero;
                    var readMissedCycles = (int)Math.Min(
                        overrun.Ticks / _key.SamplingInterval.Ticks,
                        int.MaxValue);
                    var missedCycles = readMissedCycles >
                        int.MaxValue - carriedMissedCycles
                            ? int.MaxValue
                            : readMissedCycles + carriedMissedCycles;
                    var completedAt = _timeProvider.GetUtcNow();
                    var cycleSequenceNumber =
                        IncrementSequenceNumber(ref _cycleSequenceNumber);
                    try
                    {
                        _adapter.DeliverCyclicRead(items,
                            Normalize(values, items.Length),
                            cycleSequenceNumber, completedAt.UtcDateTime,
                            missedCycles);
                    }
                    catch (Exception ex)
                    {
                        _adapter.RecordBackgroundError(ex);
                        _adapter._logger.CyclicReadFailed(ex);
                    }

                    var cycleCompleted = _timeProvider.GetTimestamp();
                    var totalElapsed = _timeProvider.GetElapsedTime(
                        cycleStarted, cycleCompleted);
                    var totalOverrun = totalElapsed > delayUntilNext
                        ? totalElapsed - delayUntilNext
                        : TimeSpan.Zero;
                    var totalMissedCycles = Math.Min(
                        totalOverrun.Ticks / _key.SamplingInterval.Ticks,
                        int.MaxValue);
                    carriedMissedCycles = (int)Math.Max(0,
                        totalMissedCycles - readMissedCycles);
                    var remainder = totalOverrun.Ticks %
                        _key.SamplingInterval.Ticks;
                    delayUntilNext = remainder == 0
                        ? _key.SamplingInterval
                        : _key.SamplingInterval - TimeSpan.FromTicks(remainder);
                }
            }

            private static IReadOnlyList<DataValue> Normalize(
                IReadOnlyList<DataValue> values, int expectedCount)
            {
                if (values.Count == expectedCount)
                {
                    return values;
                }
                var normalized = new DataValue[expectedCount];
                var count = Math.Min(values.Count, expectedCount);
                for (var index = 0; index < count; index++)
                {
                    normalized[index] = values[index];
                }
                for (var index = count; index < normalized.Length; index++)
                {
                    normalized[index] =
                        DataValue.FromStatusCode(StatusCodes.BadUnexpectedError);
                }
                return normalized;
            }

            private readonly ManagedSubscriptionAdapter _adapter;
            private readonly IManagedCyclicReadClient _client;
            private readonly CancellationTokenSource _cts;
            private readonly CyclicReadGroupKey _key;
            private readonly Lock _lock = new();
            private readonly TimeProvider _timeProvider;
            private CyclicReadItem[] _items;
            private Task? _worker;
            private int _disposed;
            private int _started;
            private uint _cycleSequenceNumber;
        }

        private sealed class ManagedSubscriptionItemBinding
        {
            public uint ClientHandle { get; set; }
            public string Name { get; }
            public string RootName { get; }
            public string? ParentName { get; }
            public ISubscriber Owner { get; private set; }
            public BaseMonitoredItemModel Template { get; private set; }
            public MutableOptionsMonitor<MonitoredItemOptions> Monitor { get; }
            public IReadOnlyList<string?> EventFieldNames { get; private set; }
            public IReadOnlyList<Guid> EventFieldIds { get; private set; }
            public ConditionState? Condition { get; private set; }
            public RelativePath? PathFromRoot { get; set; }
            public BindingMetadataSnapshot PublishedMetadata =>
                Volatile.Read(ref _publishedMetadata);
            public DataValue? LastDataValue
            {
                get
                {
                    lock (_dataLock)
                    {
                        return _lastDataValue;
                    }
                }
            }
            public bool Registered { get; private set; }
            public bool HeartbeatEnabled => _heartbeat?.Enabled == true;
            public bool DropDataChange => _heartbeat?.DropDataChange == true;

            public ManagedSubscriptionItemBinding(string name, ISubscriber owner,
                BaseMonitoredItemModel template, MonitoredItemOptions options,
                string rootName, IReadOnlyList<string> triggeredByNames,
                IServiceMessageContext messageContext,
                TimeProvider timeProvider,
                Action<ManagedSubscriptionItemBinding> heartbeatCallback,
                TimeSpan publishingInterval = default)
            {
                Name = name;
                Owner = owner;
                Template = template;
                RootName = rootName;
                ParentName = triggeredByNames.Count == 0 ? null : triggeredByNames[0];
                Monitor = new MutableOptionsMonitor<MonitoredItemOptions>(options);
                _messageContext = messageContext;
                (EventFieldNames, Condition) = CreateEventLayout(template, options,
                    _messageContext);
                EventFieldIds = CreateEventFieldIds(template as EventMonitoredItemModel,
                    EventFieldNames);
                _publishedMetadata = CreateMetadataSnapshot();
                _timeProvider = timeProvider;
                _heartbeatCallback = heartbeatCallback;
                _publishingInterval = publishingInterval;
                UpdateHeartbeat(template);
            }

            public void Update(ISubscriber owner, BaseMonitoredItemModel template,
                MonitoredItemOptions options)
            {
                var ownerChanged = !ReferenceEquals(Owner, owner);
                var itemIdentityChanged = ownerChanged ||
                    Template.GetType() != template.GetType() ||
                    !string.Equals(Template.StartNodeId, template.StartNodeId,
                        StringComparison.Ordinal) ||
                    Template.AttributeId != template.AttributeId;
                var conditionChanged = Registered && Template is EventMonitoredItemModel &&
                    template is EventMonitoredItemModel &&
                    (!Equals(Template, template) ||
                        !ReferenceEquals(Monitor.CurrentValue.Filter, options.Filter));
                var monitoringModeChanged =
                    Monitor.CurrentValue.MonitoringMode != options.MonitoringMode;
                lock (_dataLock)
                {
                    if (itemIdentityChanged || monitoringModeChanged)
                    {
                        _lastDataValue = null;
                        _lastDataReceivedAt = null;
                        _lastDataSequenceNumber = 0;
                        _lastConnectedStatusCode = null;
                        _lastActivityTimestamp = 0;
                        _hasActivity = false;
                        _isLate = false;
                        _applied = false;
                        _appliedMonitoringMode = Opc.Ua.MonitoringMode.Disabled;
                        _heartbeat?.Reset();
                        _heartbeat?.SetApplied(false);
                        Interlocked.Exchange(ref _firstDataChange, 0);
                    }
                    Owner = owner;
                    Template = template;
                    _cyclicReadGeneration++;
                    _cyclicReadActivated = false;
                    _hasReportedApplyStatus = false;
                }
                Monitor.Update(options);
                UpdateHeartbeat(template);
                if (conditionChanged && Condition != null)
                {
                    lock (Condition._lock)
                    {
                        Condition.Superseded = true;
                        Condition.Refreshing = true;
                    }
                }

                var previousEventFieldNames = EventFieldNames;
                var previousEventFieldIds = EventFieldIds;
                (EventFieldNames, Condition) = CreateEventLayout(template, options,
                    _messageContext,
                    conditionChanged ? null : Condition);
                EventFieldIds = CreateEventFieldIds(
                    template as EventMonitoredItemModel, EventFieldNames,
                    previousEventFieldNames, previousEventFieldIds);
                if (conditionChanged && Condition != null)
                {
                    Condition.Refreshing = true;
                    Condition.RefreshRequested = true;
                }
            }

            public void RestoreMetadata(BindingMetadataSnapshot snapshot)
            {
                PathFromRoot = snapshot.PathFromRoot;
                EventFieldNames = snapshot.EventFieldNames;
                EventFieldIds = snapshot.EventFieldIds;
                Volatile.Write(ref _publishedMetadata, snapshot);
            }

            public void CommitMetadata()
            {
                Volatile.Write(ref _publishedMetadata, CreateMetadataSnapshot());
            }

            public void Activate()
            {
                lock (_dataLock)
                {
                    Registered = true;
                    _cyclicReadGeneration++;
                    _heartbeat?.SetActive(true);
                }
            }

            public void Dispose()
            {
                lock (_dataLock)
                {
                    Registered = false;
                    _isLate = false;
                    _cyclicReadGeneration++;
                    _cyclicReadActivated = false;
                    _heartbeat?.Dispose();
                    _heartbeat = null;
                }
            }

            public uint RecordDataChange(DataValue value, DateTimeOffset receivedAt)
            {
                var sequenceNumber = NextSequenceNumber();
                lock (_dataLock)
                {
                    _lastDataValue = value;
                    _lastDataReceivedAt = receivedAt;
                    _lastDataSequenceNumber = sequenceNumber;
                    _lastActivityTimestamp = _timeProvider.GetTimestamp();
                    _hasActivity = true;
                    _isLate = false;
                    _lastConnectedStatusCode = null;
                    _heartbeat?.Record(value, receivedAt, sequenceNumber);
                }
                return sequenceNumber;
            }

            public void NotifyConnectionState(bool disconnected)
            {
                lock (_dataLock)
                {
                    if (!_lastDataValue.HasValue)
                    {
                        return;
                    }
                    var value = _lastDataValue.Value;
                    if (disconnected)
                    {
                        _lastConnectedStatusCode ??= value.StatusCode;
                        value = HeartbeatState.WithStatusCode(value,
                            IsGoodDataValue(value) ?
                                StatusCodes.UncertainNoCommunicationLastUsableValue :
                                StatusCodes.BadNoCommunication);
                    }
                    else if (_lastConnectedStatusCode.HasValue)
                    {
                        value = HeartbeatState.WithStatusCode(value,
                            _lastConnectedStatusCode.Value);
                        _lastConnectedStatusCode = null;
                    }
                    _lastDataValue = value;
                    _heartbeat?.ReplaceValue(value);
                }
            }

            public StatusCode UpdateMonitoredItemStatus(
                IMonitoredItem monitoredItem)
            {
                var pending = HasPendingChanges(monitoredItem);
                var status = monitoredItem.Error?.StatusCode ?? StatusCodes.Good;
                var applied = !pending &&
                    monitoredItem.Created &&
                    ServiceResult.IsGood(monitoredItem.Error) &&
                    monitoredItem.CurrentMonitoringMode ==
                        Monitor.CurrentValue.MonitoringMode;
                if (!applied && !pending && StatusCode.IsGood(status))
                {
                    status = StatusCodes.BadMonitoredItemIdInvalid;
                }
                var retryKind = !applied && !pending
                    ? ManagedSubscriptionRetryPolicy.Classify(status,
                        ServiceResult.IsGood(monitoredItem.Error))
                    : ManagedItemRetryKind.None;
                lock (_dataLock)
                {
                    _applied = applied;
                    _applyPending = pending;
                    _applyRetryKind = retryKind;
                    _applyStatusCode = status;
                    _appliedMonitoringMode = monitoredItem.CurrentMonitoringMode;
                    if (!_cyclicReadActivated && applied && IsCyclicRead &&
                        _appliedMonitoringMode == Opc.Ua.MonitoringMode.Disabled)
                    {
                        _cyclicReadActivated = true;
                        _cyclicReadGeneration++;
                    }
                    if (!applied)
                    {
                        _isLate = false;
                    }
                    _heartbeat?.SetApplied(applied);
                }
                return status;
            }

            public bool TryCaptureStatusReport(out StatusCode status)
            {
                lock (_dataLock)
                {
                    status = _applyStatusCode;
                    if (_applyPending ||
                        (_hasReportedApplyStatus &&
                            _reportedApplyStatusCode == status))
                    {
                        return false;
                    }
                    _hasReportedApplyStatus = true;
                    _reportedApplyStatusCode = status;
                    return true;
                }
            }

            public (bool Registered, bool Applied, bool Pending) CaptureApplyState(
                IMonitoredItem? monitoredItem)
            {
                bool registered;
                lock (_dataLock)
                {
                    registered = Registered;
                    if (monitoredItem == null)
                    {
                        return (registered, _applied, _applyPending);
                    }
                }
                var pending = HasPendingChanges(monitoredItem);
                var applied = !pending &&
                    monitoredItem.Created &&
                    ServiceResult.IsGood(monitoredItem.Error) &&
                    monitoredItem.CurrentMonitoringMode ==
                        Monitor.CurrentValue.MonitoringMode;
                return (registered, applied, pending);
            }

            public ManagedItemRetryTarget CaptureRetryTarget()
            {
                lock (_dataLock)
                {
                    return new ManagedItemRetryTarget(Name, ClientHandle,
                        _cyclicReadGeneration,
                        _applyRetryKind,
                        Registered ? _applyStatusCode : StatusCodes.Good,
                        Registered && _applyPending,
                        !Registered || _applied);
                }
            }

            public bool IsRetryCurrent(long generation,
                ManagedItemRetryKind kind, StatusCode status)
            {
                lock (_dataLock)
                {
                    return generation == _cyclicReadGeneration &&
                        Registered && !_applyPending && !_applied &&
                        _applyRetryKind == kind &&
                        _applyStatusCode == status;
                }
            }

            public void RecordActivity()
            {
                lock (_dataLock)
                {
                    _lastActivityTimestamp = _timeProvider.GetTimestamp();
                    _hasActivity = true;
                    _isLate = false;
                }
            }

            public bool WasLateSince(long lastCheck)
            {
                lock (_dataLock)
                {
                    if (!Registered || !_applied ||
                        Monitor.CurrentValue.MonitoringMode ==
                            Opc.Ua.MonitoringMode.Disabled)
                    {
                        _isLate = false;
                        return false;
                    }
                    return _isLate = !_hasActivity ||
                        _lastActivityTimestamp < lastCheck;
                }
            }

            public bool TryCaptureHeartbeat(DateTimeOffset signalTime, bool force,
                out HeartbeatSnapshot heartbeat)
            {
                if (_heartbeat != null)
                {
                    return _heartbeat.TryCapture(signalTime, force, out heartbeat);
                }
                heartbeat = default;
                return false;
            }

            public bool IsHeartbeatCurrent(uint sequenceNumber)
            {
                return _heartbeat?.IsCurrent(sequenceNumber) == true;
            }

            public bool IsCyclicRead =>
                Template is DataMonitoredItemModel
                {
                    SamplingUsingCyclicRead: true
                };

            public CyclicReadGroupKey? CyclicReadGroupKey
            {
                get
                {
                    if (Template is not DataMonitoredItemModel
                        {
                            SamplingUsingCyclicRead: true
                        } data)
                    {
                        return null;
                    }
                    return new CyclicReadGroupKey(
                        data.SamplingInterval is { } interval &&
                            interval > TimeSpan.Zero
                                ? interval
                                : TimeSpan.FromSeconds(1),
                        data.CyclicReadMaxAge is { } maxAge &&
                            maxAge > TimeSpan.Zero
                                ? maxAge
                                : TimeSpan.Zero);
                }
            }

            public bool TryCaptureCyclicRead(out CyclicReadItem item)
            {
                lock (_dataLock)
                {
                    if (!Registered || !_cyclicReadActivated ||
                        !IsCyclicRead)
                    {
                        item = default;
                        return false;
                    }
                    MonitoredItemOptions options = Monitor.CurrentValue;
                    var template = Template;
                    item = new CyclicReadItem(this, _cyclicReadGeneration,
                        new ManagedCyclicReadRequest(
                            new ReadValueId
                            {
                                NodeId = options.StartNodeId,
                                AttributeId = options.AttributeId,
                                IndexRange = options.IndexRange,
                                DataEncoding = options.Encoding ?? QualifiedName.Null
                            },
                            template is DataMonitoredItemModel
                            {
                                RegisterRead: true
                            }),
                        Owner,
                        template.DataSetFieldId ?? string.Empty,
                        template.DisplayName,
                        template.StartNodeId,
                        template is DataMonitoredItemModel
                        {
                            SkipFirst: true
                        });
                    return true;
                }
            }

            public bool TryRecordCyclicRead(long generation,
                bool skipFirst, in DataValue value,
                DateTimeOffset receivedAt, out uint sequenceNumber,
                out bool skip)
            {
                lock (_dataLock)
                {
                    if (generation != _cyclicReadGeneration ||
                        !Registered || !_cyclicReadActivated || !IsCyclicRead)
                    {
                        sequenceNumber = 0;
                        skip = false;
                        return false;
                    }
                    sequenceNumber = NextSequenceNumber();
                    skip = skipFirst &&
                        Interlocked.Exchange(ref _firstDataChange, 1) == 0;
                    _lastDataValue = value;
                    _lastDataReceivedAt = receivedAt;
                    _lastDataSequenceNumber = sequenceNumber;
                    _lastActivityTimestamp = _timeProvider.GetTimestamp();
                    _hasActivity = true;
                    _isLate = false;
                    return true;
                }
            }

            public bool IsCurrentCyclicRead(long generation)
            {
                lock (_dataLock)
                {
                    return generation == _cyclicReadGeneration &&
                        Registered && _cyclicReadActivated && IsCyclicRead;
                }
            }

            public bool IsLate
            {
                get
                {
                    lock (_dataLock)
                    {
                        return _isLate;
                    }
                }
            }
            public bool WatchdogEligible
            {
                get
                {
                    lock (_dataLock)
                    {
                        return Registered && _applied &&
                            _appliedMonitoringMode != Opc.Ua.MonitoringMode.Disabled;
                    }
                }
            }

            public bool SkipFirstDataChange()
            {
                return Template is DataMonitoredItemModel { SkipFirst: true } &&
                    Interlocked.Exchange(ref _firstDataChange, 1) == 0;
            }

            public uint NextSequenceNumber()
            {
                return IncrementSequenceNumber(ref _sequenceNumber);
            }

            private void UpdateHeartbeat(BaseMonitoredItemModel template)
            {
                lock (_dataLock)
                {
                    if (template is not DataMonitoredItemModel data ||
                        data.SamplingUsingCyclicRead == true ||
                        data.HeartbeatInterval is not { } interval ||
                        interval <= TimeSpan.Zero)
                    {
                        _heartbeat?.Dispose();
                        _heartbeat = null;
                        return;
                    }
                    var created = _heartbeat == null;
                    _heartbeat ??= new HeartbeatState(_timeProvider,
                        () => _heartbeatCallback(this));
                    _heartbeat.Update(interval,
                        data.HeartbeatBehavior ?? HeartbeatBehavior.WatchdogLKV,
                        Registered, _publishingInterval);
                    if (created && _lastDataValue.HasValue &&
                        _lastDataReceivedAt.HasValue)
                    {
                        _heartbeat.Record(_lastDataValue.Value,
                            _lastDataReceivedAt.Value,
                            _lastDataSequenceNumber);
                    }
                }
            }

            private static (IReadOnlyList<string?> Fields, ConditionState? Condition) CreateEventLayout(
                BaseMonitoredItemModel template, MonitoredItemOptions options,
                IServiceMessageContext messageContext,
                ConditionState? existing = null)
            {
                if (options.Filter is not EventFilter filter)
                {
                    return ([], null);
                }
                var fields = new List<string?>();
                var eventTemplate = template as EventMonitoredItemModel;
                var condition = eventTemplate is
                    {
                        ConditionHandling: { SnapshotInterval: not null }
                    } ? existing is { } current && current.Matches(eventTemplate) ? current :
                        new ConditionState(eventTemplate)
                  : eventTemplate is
                    {
                        ConditionHandling.RefreshRetainedConditionsOnStart: true,
                        ConditionHandling.SnapshotInterval: null
                    } ? existing is { RefreshOnly: true } refreshCurrent &&
                            refreshCurrent.Matches(eventTemplate) ? refreshCurrent :
                        new ConditionState(eventTemplate, refreshOnly: true)
                  : null;
                for (var index = 0; index < filter.SelectClauses.Count; index++)
                {
                    var clause = filter.SelectClauses[index];
                    if (condition != null)
                    {
                        if (clause.TypeDefinitionId == ObjectTypeIds.BaseEventType &&
                            clause.BrowsePath.Count != 0 &&
                            clause.BrowsePath[0] == BrowseNames.EventType)
                        {
                            condition.EventTypeIndex = index;
                            if (!condition.RefreshOnly)
                            {
                                // For snapshot conditions, hide EventType from event
                                // notifications; it is consumed only for marker detection.
                                fields.Add(null);
                                continue;
                            }
                            // For refresh-only conditions the EventType field is preserved
                            // so that retained conditions appear in regular event messages
                            // with all their configured fields intact.
                        }
                        if (!condition.RefreshOnly)
                        {
                            if (clause.TypeDefinitionId == ObjectTypeIds.ConditionType &&
                                clause.AttributeId == Attributes.NodeId)
                            {
                                condition.ConditionIdIndex = index;
                                fields.Add(GetFieldName(eventTemplate, clause, index,
                                    messageContext) ?? "ConditionId");
                                continue;
                            }
                            if (ManagedSubscriptionOptionsAdapter.IsConditionRetainClause(clause))
                            {
                                condition.RetainIndex = index;
                                fields.Add(GetFieldName(eventTemplate, clause, index,
                                    messageContext) ?? BrowseNames.Retain);
                                continue;
                            }
                        }
                    }

                    fields.Add(GetFieldName(eventTemplate, clause, index, messageContext));
                }
                return (fields, condition);

                static string? GetFieldName(EventMonitoredItemModel? template,
                    SimpleAttributeOperand clause, int index,
                    IServiceMessageContext messageContext)
                {
                    SimpleAttributeOperandModel? configured = null;
                    if (template?.EventFilter.SelectClauses is { } configuredClauses &&
                        index < configuredClauses.Count)
                    {
                        configured = configuredClauses[index];
                    }
                    if (!string.IsNullOrEmpty(configured?.DisplayName))
                    {
                        return configured.DisplayName;
                    }
                    if (clause.BrowsePath.Count == 0)
                    {
                        if (clause.TypeDefinitionId == ObjectTypeIds.ConditionType &&
                            clause.AttributeId == Attributes.NodeId)
                        {
                            return "ConditionId";
                        }
                        return null;
                    }
                    var names = new string[clause.BrowsePath.Count];
                    for (var pathIndex = 0; pathIndex < clause.BrowsePath.Count; pathIndex++)
                    {
                        names[pathIndex] = clause.BrowsePath[pathIndex].AsString(messageContext,
                            template?.NamespaceFormat ?? NamespaceFormat.Uri);
                    }
                    return string.Join("/", names);
                }
            }

            private static IReadOnlyList<Guid> CreateEventFieldIds(
                EventMonitoredItemModel? template, IReadOnlyList<string?> fieldNames,
                IReadOnlyList<string?>? previousFieldNames = null,
                IReadOnlyList<Guid>? previousFieldIds = null)
            {
                var ids = new Guid[fieldNames.Count];
                for (var index = 0; index < fieldNames.Count; index++)
                {
                    var fieldName = fieldNames[index];
                    if (fieldName == null)
                    {
                        continue;
                    }

                    var configuredId = template?.EventFilter.SelectClauses?
                        .ElementAtOrDefault(index)?.DataSetClassFieldId ?? Guid.Empty;
                    if (configuredId != Guid.Empty)
                    {
                        ids[index] = configuredId;
                    }
                    else if (previousFieldNames != null && previousFieldIds != null &&
                        index < previousFieldNames.Count && index < previousFieldIds.Count &&
                        string.Equals(previousFieldNames[index], fieldName,
                            StringComparison.Ordinal) &&
                        previousFieldIds[index] != Guid.Empty)
                    {
                        ids[index] = previousFieldIds[index];
                    }
                    else
                    {
                        ids[index] = Guid.NewGuid();
                    }
                }
                return ids;
            }

            private BindingMetadataSnapshot CreateMetadataSnapshot()
            {
                return new BindingMetadataSnapshot(Template, PathFromRoot,
                    Monitor.CurrentValue.Filter as EventFilter,
                    [.. EventFieldNames], [.. EventFieldIds]);
            }

            private int _firstDataChange;
            private uint _sequenceNumber;
            private HeartbeatState? _heartbeat;
            private DataValue? _lastDataValue;
            private BindingMetadataSnapshot _publishedMetadata;
            private DateTimeOffset? _lastDataReceivedAt;
            private uint _lastDataSequenceNumber;
            private StatusCode? _lastConnectedStatusCode;
            private long _lastActivityTimestamp;
            private long _cyclicReadGeneration;
            private bool _cyclicReadActivated;
            private bool _hasActivity;
            private bool _hasReportedApplyStatus;
            private bool _applied;
            private bool _applyPending;
            private ManagedItemRetryKind _applyRetryKind;
            private StatusCode _reportedApplyStatusCode = StatusCodes.Good;
            private StatusCode _applyStatusCode = StatusCodes.Good;
            private Opc.Ua.MonitoringMode _appliedMonitoringMode;
            private volatile bool _isLate;
            private readonly Lock _dataLock = new();
            private readonly IServiceMessageContext _messageContext;
            private readonly TimeProvider _timeProvider;
            private readonly Action<ManagedSubscriptionItemBinding> _heartbeatCallback;
            private readonly TimeSpan _publishingInterval;
        }

        private readonly record struct HeartbeatSnapshot(
            DataValue? Value,
            DateTimeOffset? ReceivedAt,
            DateTimeOffset SignalTime,
            HeartbeatBehavior Behavior,
            uint ItemSequenceNumber);

        private sealed class HeartbeatState : IDisposable
        {
            public bool Enabled
            {
                get
                {
                    lock (_lock)
                    {
                        return _enabled;
                    }
                }
            }

            public bool DropDataChange
            {
                get
                {
                    lock (_lock)
                    {
                        return (_behavior & HeartbeatBehavior.Reserved) != 0;
                    }
                }
            }

            public HeartbeatState(TimeProvider timeProvider, Action callback)
            {
                _timeProvider = timeProvider;
                _callback = callback;
            }

            public void Update(TimeSpan interval, HeartbeatBehavior behavior, bool active,
                TimeSpan publishingInterval = default)
            {
                lock (_lock)
                {
                    if (_disposed)
                    {
                        return;
                    }
                    var changed = _interval != interval || _behavior != behavior
                        || _publishingInterval != publishingInterval;
                    _interval = interval;
                    _behavior = behavior;
                    _publishingInterval = publishingInterval;
                    _registered = active;
                    ReconcileTimer(changed);
                }
            }

            public void SetActive(bool active)
            {
                lock (_lock)
                {
                    if (_disposed || _registered == active)
                    {
                        return;
                    }
                    _registered = active;
                    ReconcileTimer(restart: true);
                }
            }

            public void SetApplied(bool applied)
            {
                lock (_lock)
                {
                    if (_disposed || _applied == applied)
                    {
                        return;
                    }
                    _applied = applied;
                    ReconcileTimer(restart: applied);
                }
            }

            public void Record(DataValue value, DateTimeOffset receivedAt,
                uint sequenceNumber)
            {
                lock (_lock)
                {
                    if (_disposed)
                    {
                        return;
                    }
                    _lastValue = value;
                    _lastReceivedAt = receivedAt;
                    _lastSequenceNumber = sequenceNumber;
                    if (_enabled &&
                        (_behavior & HeartbeatBehavior.PeriodicLKV) == 0)
                    {
                        ArmTimer();
                    }
                }
            }

            public void Reset()
            {
                lock (_lock)
                {
                    _lastValue = null;
                    _lastReceivedAt = null;
                    _lastSequenceNumber = 0;
                    if (_enabled)
                    {
                        ArmTimer();
                    }
                }
            }

            public void ReplaceValue(DataValue value)
            {
                lock (_lock)
                {
                    _lastValue = value;
                }
            }

            public bool TryCapture(DateTimeOffset signalTime, bool force,
                out HeartbeatSnapshot heartbeat)
            {
                lock (_lock)
                {
                    if (_disposed || !_enabled)
                    {
                        heartbeat = default;
                        return false;
                    }
                    if (!force)
                    {
                        var elapsed = _timeProvider.GetElapsedTime(_armedTimestamp);
                        var deadline = EffectiveDeadline;
                        if (elapsed < deadline)
                        {
                            _timer!.Change(deadline - elapsed,
                                Timeout.InfiniteTimeSpan);
                            heartbeat = default;
                            return false;
                        }
                    }
                    ArmTimer();
                    heartbeat = new HeartbeatSnapshot(_lastValue,
                        _lastReceivedAt, signalTime, _behavior,
                        _lastSequenceNumber);
                    return true;
                }
            }

            public bool IsCurrent(uint sequenceNumber)
            {
                lock (_lock)
                {
                    return !_disposed && _enabled &&
                        _lastSequenceNumber == sequenceNumber;
                }
            }

            public void Dispose()
            {
                lock (_lock)
                {
                    if (_disposed)
                    {
                        return;
                    }
                    _disposed = true;
                    DisableTimer();
                }
            }

            private void EnableTimer(bool restart)
            {
                if (_timer == null)
                {
                    _timer = _timeProvider.CreateTimer(_ => _callback(), null,
                        Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
                    restart = true;
                }
                _enabled = true;
                if (restart)
                {
                    ArmTimer();
                }
            }

            private void ArmTimer()
            {
                _armedTimestamp = _timeProvider.GetTimestamp();
                _timer!.Change(EffectiveDeadline, Timeout.InfiniteTimeSpan);
            }

            private TimeSpan EffectiveDeadline =>
                (_behavior & HeartbeatBehavior.PeriodicLKV) != 0
                    ? _interval
                    : _interval + TimeSpan.FromTicks(
                        Math.Min(_publishingInterval.Ticks, _interval.Ticks));

            private void DisableTimer()
            {
                _enabled = false;
                _timer?.Dispose();
                _timer = null;
            }

            private void ReconcileTimer(bool restart)
            {
                var shouldEnable = _registered &&
                    (!RequiresGoodValue(_behavior) || _applied);
                if (!shouldEnable)
                {
                    DisableTimer();
                }
                else
                {
                    EnableTimer(restart);
                }
            }

            private static bool RequiresGoodValue(HeartbeatBehavior behavior)
            {
                return (behavior & HeartbeatBehavior.WatchdogLKG) ==
                    HeartbeatBehavior.WatchdogLKG;
            }

            public static DataValue WithStatusCode(in DataValue value,
                StatusCode statusCode)
            {
                return new DataValue(value.WrappedValue, statusCode,
                    value.SourceTimestamp, value.ServerTimestamp,
                    value.SourcePicoseconds, value.ServerPicoseconds);
            }

            private readonly Lock _lock = new();
            private readonly TimeProvider _timeProvider;
            private readonly Action _callback;
            private ITimer? _timer;
            private DataValue? _lastValue;
            private DateTimeOffset? _lastReceivedAt;
            private long _armedTimestamp;
            private TimeSpan _interval;
            private TimeSpan _publishingInterval;
            private HeartbeatBehavior _behavior;
            private uint _lastSequenceNumber;
            private bool _registered;
            private bool _applied;
            private bool _enabled;
            private bool _disposed;
        }

        private sealed class ConditionState
        {
            public int EventTypeIndex { get; set; } = -1;
            public int ConditionIdIndex { get; set; } = -1;
            public int RetainIndex { get; set; } = -1;
            public int SnapshotInterval { get; }
            public int UpdateInterval { get; }
            public DateTimeOffset LastSent { get; set; }
            public bool Dirty { get; set; }
            public bool Publishing { get; set; }
            public bool RefreshRequested { get; set; }
            public bool Refreshing { get; set; }
            public bool Superseded { get; set; }
            public long Generation { get; }
            /// <summary>
            /// When <see langword="true"/> this is a refresh-only condition:
            /// <see cref="SnapshotInterval"/> and <see cref="UpdateInterval"/> are
            /// unused.  <see cref="RefreshStartEventType"/> and
            /// <see cref="RefreshEndEventType"/> are suppressed;
            /// <see cref="RefreshRequiredEventType"/> re-issues the refresh;
            /// all other events are forwarded as regular ua-event messages.
            /// </summary>
            public bool RefreshOnly { get; }
            public Dictionary<string, List<MonitoredItemNotificationModel>> Active { get; } = [];

            public ConditionState(EventMonitoredItemModel template, bool refreshOnly = false)
            {
                Generation = Interlocked.Increment(ref s_generation);
                RefreshOnly = refreshOnly;
                if (!refreshOnly)
                {
                    SnapshotInterval = template.ConditionHandling!.SnapshotInterval!.Value;
                    UpdateInterval = template.ConditionHandling.UpdateInterval ?? SnapshotInterval;
                }
            }

            public bool Matches(EventMonitoredItemModel template)
            {
                if (RefreshOnly)
                {
                    return template.ConditionHandling?.RefreshRetainedConditionsOnStart == true &&
                        template.ConditionHandling?.SnapshotInterval == null;
                }
                return SnapshotInterval == template.ConditionHandling!.SnapshotInterval!.Value &&
                    UpdateInterval == (template.ConditionHandling.UpdateInterval ?? SnapshotInterval);
            }

            internal readonly Lock _lock = new();
            private static long s_generation;
        }

        private sealed record class PendingConditionRefresh(
            ManagedSubscriptionItemBinding Binding, ConditionState? Condition)
        {
            public long Generation => Condition?.Generation ?? 0;
        }

        private sealed record class DesiredItemSnapshot(
            string Name,
            ISubscriber Owner,
            BaseMonitoredItemModel Template);

        private sealed record class BindingMetadataSnapshot(
            BaseMonitoredItemModel Template,
            RelativePath? PathFromRoot,
            EventFilter? EventFilter,
            IReadOnlyList<string?> EventFieldNames,
            IReadOnlyList<Guid> EventFieldIds);

        internal sealed record class ManagedEventMetadata(
            EventMonitoredItemModel Template,
            EventFilter Filter,
            IReadOnlyList<string?> FieldNames,
            IReadOnlyList<Guid> FieldIds);

        private sealed record class PendingMonitoredItem(
            IMonitoredItem Item,
            Opc.Ua.MonitoringMode DesiredMonitoringMode);

        private sealed class ModelChangeBrowserRegistration
        {
            public string Name { get; }
            public TimeSpan RebrowsePeriod { get; }
            public IOpcUaBrowser Browser { get; }

            public ModelChangeBrowserRegistration(string name, TimeSpan rebrowsePeriod,
                IOpcUaBrowser browser, EventHandler<Change<Node>> nodeChanged,
                EventHandler<Change<ReferenceDescription>> referenceChanged)
            {
                Name = name;
                RebrowsePeriod = rebrowsePeriod;
                Browser = browser;
                _nodeChanged = nodeChanged;
                _referenceChanged = referenceChanged;
            }

            public void Attach()
            {
                Browser.OnNodeChange += _nodeChanged;
                Browser.OnReferenceChange += _referenceChanged;
            }

            public void Detach()
            {
                Browser.OnNodeChange -= _nodeChanged;
                Browser.OnReferenceChange -= _referenceChanged;
            }

            public void Start()
            {
                if (Browser is IStartableOpcUaBrowser startable)
                {
                    startable.Start();
                }
            }

            private readonly EventHandler<Change<Node>> _nodeChanged;
            private readonly EventHandler<Change<ReferenceDescription>> _referenceChanged;
        }

        private sealed class OwnerState
        {
            public bool KeyFrameRequired { get; set; } = true;
            public DateTimeOffset LastKeyFrame { get; set; }

            public OwnerState(DateTimeOffset now)
            {
                LastKeyFrame = now;
            }
        }

        private sealed class PendingInitialApply
        {
            public PendingInitialApply(long stateVersion)
            {
                StateVersion = stateVersion;
            }

            public long StateVersion { get; }
            public TaskCompletionSource Completion { get; } = new(
                TaskCreationOptions.RunContinuationsAsynchronously);
            public IReadOnlyList<PendingMonitoredItem>? MonitoredItems { get; set; }
        }

        private sealed class MutableOptionsMonitor<
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
            T> : IOptionsMonitor<T> where T : class
        {
            public T CurrentValue
            {
                get
                {
                    lock (_lock)
                    {
                        return _value;
                    }
                }
            }

            public MutableOptionsMonitor(T value)
            {
                _value = value ?? throw new ArgumentNullException(nameof(value));
            }

            public T Get(string? name)
            {
                return CurrentValue;
            }

            public IDisposable OnChange(Action<T, string?> listener)
            {
                ArgumentNullException.ThrowIfNull(listener);
                lock (_lock)
                {
                    _listeners.Add(listener);
                }
                return new Registration(this, listener);
            }

            public void Update(T value)
            {
                ArgumentNullException.ThrowIfNull(value);
                Action<T, string?>[] listeners;
                lock (_lock)
                {
                    _value = value;
                    listeners = [.. _listeners];
                }
                foreach (var listener in listeners)
                {
                    listener(value, Options.DefaultName);
                }
            }

            private sealed class Registration : IDisposable
            {
                public Registration(MutableOptionsMonitor<T> owner, Action<T, string?> listener)
                {
                    _owner = owner;
                    _listener = listener;
                }

                public void Dispose()
                {
                    lock (_owner._lock)
                    {
                        _owner._listeners.Remove(_listener);
                    }
                }

                private readonly Action<T, string?> _listener;
                private readonly MutableOptionsMonitor<T> _owner;
            }

            private readonly Lock _lock = new();
            private readonly List<Action<T, string?>> _listeners = [];
            private T _value;
        }

        private readonly Lock _bindingsLock = new();
        /// <summary>
        /// Serializes heartbeat delivery against value-change and key-frame delivery
        /// on the same <see cref="OnSubscriptionDataChangeReceived"/> path.
        /// <para>
        /// Lock ordering rule: every other lock (<see cref="_bindingsLock"/>,
        /// <c>HeartbeatState._lock</c>, <c>binding._dataLock</c>) must be
        /// released before acquiring <see cref="_deliveryGate"/>.  Inside the
        /// gate, <see cref="_bindingsLock"/> and <c>HeartbeatState._lock</c> may
        /// be re-acquired (heartbeat staleness re-check); no path holds those
        /// locks when it tries to acquire the gate, so no cycle is possible.
        /// Subscriber callbacks are invoked while the gate is held; correctness
        /// (preventing stale heartbeats from arriving after fresher values) takes
        /// precedence over throughput on both the timer and publish paths.
        /// </para>
        /// </summary>
        private readonly Lock _deliveryGate = new();
        private readonly Dictionary<uint, ManagedSubscriptionItemBinding> _bindingsByHandle = [];
        private readonly Dictionary<string, ManagedSubscriptionItemBinding> _bindingsByName =
            new(StringComparer.Ordinal);
        private readonly Dictionary<string, Guid> _dataSetClassFieldIds =
            new(StringComparer.Ordinal);
        private Dictionary<ISubscriber, IReadOnlyList<DataMonitoredItemModel>>
            _publishedDataMetadata = [];
        private Dictionary<ISubscriber, IReadOnlyList<ManagedEventMetadata>>
            _publishedEventMetadata = [];
        private readonly IVariantEncoder _codec;
        private readonly Func<string?>? _endpointUrlProvider;
        private readonly Func<string?>? _applicationUriProvider;
        private readonly Func<BaseMonitoredItemModel, CancellationToken,
            ValueTask<BaseMonitoredItemModel>>? _monitoredItemPreparation;
        private readonly Func<string, CancellationToken, ValueTask<RelativePath?>>?
            _browsePathFromRootResolver;
        private readonly bool _resolveBrowsePathFromRoot;
        private readonly IManagedCyclicReadClient? _cyclicReadClient;
        private readonly Lock _cyclicReadSyncLock = new();
#pragma warning disable CA2213 // Retained so pre-disposal waiters can release safely.
        private readonly SemaphoreSlim _cyclicReadGate = new(1, 1);
#pragma warning restore CA2213 // Disposable fields should be disposed
        private Dictionary<CyclicReadGroupKey, CyclicReadGroup> _cyclicReadGroups = [];
        private readonly ITimer _conditionTimer;
        private readonly CancellationTokenSource _disposeCts = new();
        private readonly Lock _initialApplyLock = new();
        private readonly ILogger _logger;
        private readonly Func<TimeSpan, string, IOpcUaBrowser>? _modelChangeBrowserFactory;
        private readonly Lock _modelChangeBrowsersLock = new();
        private readonly Dictionary<string, ModelChangeBrowserRegistration>
            _modelChangeBrowsers = new(StringComparer.Ordinal);
        private readonly string _modelChangeSubscriptionName = Guid.NewGuid().ToString("N");
        private readonly OpcUaSubscriptionOptions _options;
        private readonly Dictionary<ISubscriber, OwnerState> _ownerStates = [];
        private readonly Lock _preparationRetryLock = new();
        private readonly ITimer _preparationRetryTimer;
        private readonly TimeSpan? _periodicKeyFrameInterval;
        private readonly ManagedSubscriptionRetryScheduler _retryScheduler;
        private readonly ISubscription _subscription;
        private readonly MutableOptionsMonitor<ManagedSubscriptionOptions> _subscriptionOptions;
        private readonly TimeProvider _timeProvider;
        private readonly ITimer? _keyFrameTimer;
        private readonly Action<ManagedSubscriptionAdapter,
            SubscriptionWatchdogBehavior, string>? _watchdogAction;
        private readonly SubscriptionWatchdogBehavior? _configuredWatchdogBehavior;
        private readonly SubscriptionWatchdogBehavior _watchdogBehavior;
        private readonly MonitoredItemWatchdogCondition _watchdogCondition;
        private readonly Lock _watchdogLock = new();
        private readonly ITimer _watchdogTimer;
        private readonly TimeSpan _watchdogTimeout;
        private readonly TimeSpan _publishingInterval;
#pragma warning disable CA2213 // Retained so pre-disposal waiters can release safely.
        private readonly SemaphoreSlim _updateGate = new(1, 1);
#pragma warning restore CA2213 // Disposable fields should be disposed
        private PendingInitialApply? _pendingInitialApply;
        private Task? _cyclicReadSyncTask;
        private Task? _preparationRetryTask;
        private (ISubscriber Owner, BaseMonitoredItemModel Template)[]
            _preparationRetryItems = [];
        private Exception? _backgroundError;
        private int _cyclicReadStateDirty;
        private int _disposed;
        private int _hasSynchronized;
        private int _nextItemName;
        private int _preparationRetryAttempt;
        private bool _preparationRetryPending;
        private bool _preparationRetryTimerMissed;
        private int _publishingStateDirty;
        private int _watchdogResetInProgress;
        private long _lastWatchdogCheckTimestamp;
        private long _mutationVersion;
        private long _preparationCommittedVersion;
        private long _preparationRequestVersion;
        private long _preparationRetryVersion;
        private long _subscriptionStateVersion;
        private uint _sequenceNumber;
        private bool _created;
        private bool _watchdogConnected = true;
        private bool _watchdogCheckInitialized;
        private bool _watchdogDisposing;
        private bool _watchdogEnabled;
        private bool _watchdogPublishingEnabled;
        private bool _watchdogPublishingStopped;
        private bool _watchdogTimerDisposed;
        private const int kPreparationBatchSize = 32;
        private static readonly ExpandedNodeId kReferenceChangeType
            = new("ReferenceChange", "http://www.microsoft.com/opc-publisher");
        private static readonly ExpandedNodeId kNodeChangeType
            = new("NodeChange", "http://www.microsoft.com/opc-publisher");
        private static readonly string[] kModelChangeFields =
        [
            BrowseNames.EventId,
            BrowseNames.EventType,
            BrowseNames.SourceNode,
            BrowseNames.Time,
            "Change"
        ];
    }

    internal readonly record struct ManagedSubscriptionDiagnosticsSnapshot(
        int MonitoredItems,
        int AppliedMonitoredItems,
        int PendingMonitoredItems,
        int RetryingMonitoredItems,
        int TerminalMonitoredItems,
        int CyclicMonitoredItems,
        int CyclicWorkerCount,
        int HeartbeatsEnabled,
        int ConditionsEnabled,
        int LateMonitoredItems,
        int PartitionCount,
        int RetryCount,
        bool PublishingEnabled,
        bool WatchdogEnabled,
        bool WatchdogResetInProgress);

    /// <summary>
    /// Source-generated logging for the managed subscription adapter.
    /// </summary>
    internal static partial class ManagedSubscriptionAdapterLogging
    {
        [LoggerMessage(EventId = 1120, Level = LogLevel.Error,
            Message = "A subscriber callback failed for {Subscriber}.")]
        public static partial void SubscriberCallbackFailed(this ILogger logger,
            Exception exception, string subscriber);

        [LoggerMessage(EventId = 1121, Level = LogLevel.Warning,
            Message = "Condition refresh failed.")]
        public static partial void ConditionRefreshFailed(this ILogger logger, Exception exception);

        [LoggerMessage(EventId = 1122, Level = LogLevel.Error,
            Message = "The model-change browser failed for {Subscriber}.")]
        public static partial void ModelChangeBrowserFailed(this ILogger logger,
            Exception exception, string subscriber);

        [LoggerMessage(EventId = 1123, Level = LogLevel.Error,
            Message = "Managed heartbeat failed for {Item}.")]
        public static partial void HeartbeatFailed(this ILogger logger,
            Exception exception, string item);

        [LoggerMessage(EventId = 1124, Level = LogLevel.Warning,
            Message = "{Late} of {Total} managed monitored items are late; watchdog behavior is {Behavior}.")]
        public static partial void WatchdogItemsLate(this ILogger logger,
            int late, int total, SubscriptionWatchdogBehavior behavior);

        [LoggerMessage(EventId = 1125, Level = LogLevel.Error,
            Message = "Managed monitored-item watchdog failed.")]
        public static partial void WatchdogFailed(this ILogger logger,
            Exception exception);

        [LoggerMessage(EventId = 1126, Level = LogLevel.Error,
            Message = "Managed cyclic read failed.")]
        public static partial void CyclicReadFailed(this ILogger logger,
            Exception exception);

        [LoggerMessage(EventId = 1127, Level = LogLevel.Warning,
            Message = "Managed cyclic read could not discover server operation limits; " +
                "the read will use one unbounded batch.")]
        public static partial void CyclicReadOperationLimitsUnavailable(
            this ILogger logger, Exception exception);

        [LoggerMessage(EventId = 1128, Level = LogLevel.Warning,
            Message = "Managed cyclic read node registration failed; " +
                "the original node id will be used.")]
        public static partial void CyclicReadNodeRegistrationUnavailable(
            this ILogger logger, Exception exception);

        [LoggerMessage(EventId = 1129, Level = LogLevel.Warning,
            Message = "Managed cyclic read node registration returned {Status}; " +
                "the original node id will be used.")]
        public static partial void CyclicReadNodeRegistrationRejected(
            this ILogger logger, StatusCode status);

        [LoggerMessage(EventId = 1130, Level = LogLevel.Error,
            Message = "Managed cyclic-read state synchronization failed.")]
        public static partial void CyclicReadSynchronizationFailed(
            this ILogger logger, Exception exception);

        [LoggerMessage(EventId = 1133, Level = LogLevel.Error,
            Message = "Managed monitored item {Item} cannot be retried because " +
                "the V2 collection has no retry capability.")]
        public static partial void ManagedItemRetryUnsupported(
            this ILogger logger, string item);

        [LoggerMessage(EventId = 1134, Level = LogLevel.Error,
            Message = "Managed monitored-item preparation retry failed.")]
        public static partial void PreparationRetryFailed(
            this ILogger logger, Exception exception);
    }
}
