// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation, The OPC Foundation, Inc.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client
{
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// A managed subscription inside a subscription manager. Can be
    /// extended to provide custom subscription implementations on
    /// top to route the received data appropriately per application.
    /// The subscription itself is based on top of the message processor
    /// implementation that routes messages to subscribers, but adds
    /// state management of the subscription on the server using the
    /// subscription and monitored item service set provided as context.
    /// </summary>
    public abstract class SubscriptionBase : MessageProcessor,
        IManagedSubscription
    {
        /// <summary>
        /// The minimum lifetime for subscriptions
        /// </summary>
        public TimeSpan MinLifetimeInterval { get; set; }

        /// <inheritdoc/>
        public TimeSpan PublishingInterval { get; set; }

        /// <inheritdoc/>
        public uint KeepAliveCount { get; set; }

        /// <inheritdoc/>
        public uint LifetimeCount { get; set; }

        /// <inheritdoc/>
        public uint MaxNotificationsPerPublish { get; set; }

        /// <inheritdoc/>
        public bool PublishingEnabled { get; set; }

        /// <inheritdoc/>
        public byte Priority { get; set; }

        /// <inheritdoc/>
        public byte CurrentPriority { get; private set; }

        /// <inheritdoc/>
        public TimeSpan CurrentPublishingInterval { get; private set; }

        /// <inheritdoc/>
        public uint CurrentKeepAliveCount { get; private set; }

        /// <inheritdoc/>
        public uint CurrentLifetimeCount { get; private set; }

        /// <inheritdoc/>
        public bool CurrentPublishingEnabled { get; private set; }

        /// <inheritdoc/>
        public uint CurrentMaxNotificationsPerPublish { get; private set; }

        /// <inheritdoc/>
        public IEnumerable<MonitoredItem> MonitoredItems
        {
            get
            {
                lock (_monitoredItems)
                {
                    return new List<MonitoredItem>(_monitoredItems.Values);
                }
            }
        }

        /// <inheritdoc/>
        public uint MonitoredItemCount
        {
            get
            {
                lock (_cache)
                {
                    return (uint)_monitoredItems.Count;
                }
            }
        }

        /// <inheritdoc/>
        public bool Created => Id != 0;

        /// <summary>
        /// Returns true if the subscription is not receiving publishes.
        /// </summary>
        internal bool PublishingStopped
        {
            get
            {
                var lastNotificationTimestamp = _lastNotificationTimestamp;
                if (lastNotificationTimestamp == 0)
                {
                    return false;
                }
                var timeSinceLastNotification = Observability.TimeProvider
                    .GetElapsedTime(lastNotificationTimestamp);
                return timeSinceLastNotification >
                    _keepAliveInterval + kKeepAliveTimerMargin;
            }
        }

        /// <summary>
        /// Current subscription options
        /// </summary>
        protected internal SubscriptionOptions Options { get; protected set; }

        /// <summary>
        /// Create subscription
        /// </summary>
        /// <param name="session"></param>
        /// <param name="completion"></param>
        /// <param name="options"></param>
        /// <param name="observability"></param>
        protected SubscriptionBase(ISubscriptionContext session,
            IMessageAckQueue completion, IOptionsMonitor<SubscriptionOptions> options,
            IObservability observability) : base(session, completion, observability)
        {
            Options = options.CurrentValue;
            _changeTracking = options.OnChange(OnOptionsChanged);
            _publishTimer = Observability.TimeProvider.CreateTimer(OnKeepAlive,
                null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            // OnOptionsChanged(Options, null);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{_session}:{Id}";
        }

        /// <summary>
        /// Add an item to the subscription
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        public MonitoredItem AddMonitoredItem(IOptionsMonitor<MonitoredItemOptions> options)
        {
            var monitoredItem = CreateMonitoredItem(options);
            lock (_cache)
            {
                _monitoredItems.Add(monitoredItem.ClientHandle, monitoredItem);
            }
            return monitoredItem;
        }

        /// <summary>
        /// Applies any changes to the subscription items.
        /// </summary>
        /// <param name="ct"></param>
        public async ValueTask ApplyMonitoredItemChangesAsync(CancellationToken ct)
        {
            await DeleteItemsAsync(ct).ConfigureAwait(false);
            await ModifyItemsAsync(ct).ConfigureAwait(false);
            await CreateItemsAsync(ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Tells the server to refresh all conditions being monitored
        /// by the subscription.
        /// </summary>
        /// <param name="ct"></param>
        public async ValueTask ConditionRefreshAsync(CancellationToken ct)
        {
            VerifySubscriptionState(true);
            var methodsToCall = new CallMethodRequestCollection
            {
                new CallMethodRequest()
                {
                    MethodId = MethodIds.ConditionType_ConditionRefresh,
                    InputArguments = new VariantCollection() { new Variant(Id) }
                }
            };
            await _session.CallAsync(null, methodsToCall, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Creates a subscription on the server and adds all monitored items.
        /// </summary>
        /// <param name="ct"></param>
        public async ValueTask ApplyChangesAsync(CancellationToken ct)
        {
            if (Created)
            {
                await ModifyAsync(ct).ConfigureAwait(false);
                return;
            }

            await CreateAsync(ct).ConfigureAwait(false);
            await CreateItemsAsync(ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async ValueTask RecreateAsync(CancellationToken ct)
        {
            _lastSequenceNumberProcessed = 0;
            _lastNotificationTimestamp = 0;

            Id = 0;
            CurrentPublishingInterval = TimeSpan.Zero;
            CurrentKeepAliveCount = 0;
            CurrentPublishingEnabled = false;
            CurrentPriority = 0;

            // Recreate subscription
            await CreateAsync(ct).ConfigureAwait(false);

            // Synchronize items with server
            await TrySynchronizeServerAndClientHandlesAsync(ct).ConfigureAwait(false);

            await ApplyMonitoredItemChangesAsync(ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Deletes a subscription on the server, but keeps the subscription in the session.
        /// </summary>
        /// <param name="silent"></param>
        /// <param name="ct"></param>
        /// <exception cref="ServiceResultException"></exception>
        public async ValueTask DeleteAsync(bool silent, CancellationToken ct)
        {
            if (!silent)
            {
                VerifySubscriptionState(true);
            }
            // nothing to do if not created.
            if (!Created)
            {
                return;
            }
            try
            {
                // delete the subscription.
                var subscriptionIds = new UInt32Collection { Id };
                var response = await _session.DeleteSubscriptionsAsync(null,
                    subscriptionIds, ct).ConfigureAwait(false);

                // validate response.
                ClientBase.ValidateResponse(response.Results, subscriptionIds);
                ClientBase.ValidateDiagnosticInfos(response.DiagnosticInfos, subscriptionIds);

                if (StatusCode.IsBad(response.Results[0]))
                {
                    throw new ServiceResultException(ClientBase.GetResult(
                        response.Results[0], 0, response.DiagnosticInfos,
                        response.ResponseHeader));
                }
            }

            // suppress exception if silent flag is set.
            catch (Exception e)
            {
                if (!silent)
                {
                    throw new ServiceResultException(e, StatusCodes.BadUnexpectedError);
                }
            }
            finally
            {
                OnSubscriptionDeleteCompleted();
            }
        }

        /// <inheritdoc/>
        public async ValueTask<bool> TryCompleteTransferAsync(
            IReadOnlyList<uint> availableSequenceNumbers, CancellationToken ct)
        {
            StopKeepAliveTimer();
            if (!await TrySynchronizeServerAndClientHandlesAsync(ct).ConfigureAwait(false))
            {
                return false;
            }

            // save available sequence numbers
            _availableInRetransmissionQueue = availableSequenceNumbers;
            await ApplyMonitoredItemChangesAsync(ct).ConfigureAwait(false);
            StartKeepAliveTimer();
            return true;
        }

        /// <inheritdoc/>
        public override ValueTask OnPublishReceivedAsync(NotificationMessage message,
            IReadOnlyList<uint>? availableSequenceNumbers,
            IReadOnlyList<string> stringTable)
        {
            // Reset the keep alive timer
            _publishTimer.Change(_keepAliveInterval, _keepAliveInterval);

            // send notification that publishing received a keep alive
            // or has to republish.
            if (PublishingStopped)
            {
                OnPublishStateChanged(PublishState.Recovered);
            }
            return base.OnPublishReceivedAsync(message, availableSequenceNumbers,
                stringTable);
        }

        /// <inheritdoc/>
        public void RemoveItem(MonitoredItem monitoredItem)
        {
            Debug.Assert(monitoredItem != null);
            lock (_cache)
            {
                if (!_monitoredItems.Remove(monitoredItem.ClientHandle))
                {
                    return;
                }
            }
            if (monitoredItem.Created)
            {
                _deletedItems.Add(monitoredItem.ServerId);
            }
        }

        /// <summary>
        /// Dispose subscription
        /// </summary>
        /// <param name="disposing"></param>
        protected override ValueTask DisposeAsync(bool disposing)
        {
            if (disposing)
            {
                _publishTimer.Dispose();
                _changeTracking?.Dispose();
            }
            return base.DisposeAsync(disposing);
        }

        /// <summary>
        /// Create monitored item
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        protected abstract MonitoredItem CreateMonitoredItem(
            IOptionsMonitor<MonitoredItemOptions> options);

        /// <summary>
        /// Called when the options changed
        /// </summary>
        /// <param name="options"></param>
        /// <param name="name"></param>
        protected virtual void OnOptionsChanged(SubscriptionOptions options, string? name)
        {
            Options = options;
        }

        /// <summary>
        /// Set monitoring mode of items.
        /// </summary>
        /// <param name="monitoringMode"></param>
        /// <param name="monitoredItems"></param>
        /// <param name="ct"></param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="monitoredItems"/> is <c>null</c>.</exception>
        protected internal async ValueTask<IReadOnlyList<ServiceResult>> SetMonitoringModeAsync(
            MonitoringMode monitoringMode, IReadOnlyList<MonitoredItem> monitoredItems,
            CancellationToken ct)
        {
            VerifySubscriptionState(true);

            if (monitoredItems.Count == 0)
            {
                return Array.Empty<ServiceResult>();
            }

            // get list of items to update.
            var monitoredItemIds = new UInt32Collection(monitoredItems
                .Select(monitoredItem => monitoredItem.ServerId));
            var response = await _session.SetMonitoringModeAsync(null, Id,
                monitoringMode, monitoredItemIds, ct).ConfigureAwait(false);
            var results = response.Results;
            ClientBase.ValidateResponse(results, monitoredItemIds);
            ClientBase.ValidateDiagnosticInfos(response.DiagnosticInfos,
                monitoredItemIds);

            // update results.
            var errors = new List<ServiceResult>(); // TODO: Remove
            for (var index = 0; index < results.Count; index++)
            {
                var error = monitoredItems[index].SetMonitoringModeResult(
                    monitoringMode, results[index], index, response.DiagnosticInfos,
                    response.ResponseHeader);
                errors.Add(error);
            }
            return errors; // TODO: Remove
        }

        /// <summary>
        /// Returns the monitored item identified by the client handle.
        /// </summary>
        /// <param name="clientHandle"></param>
        protected internal MonitoredItem? FindItemByClientHandle(uint clientHandle)
        {
            lock (_cache)
            {
                if (_monitoredItems.TryGetValue(clientHandle, out var monitoredItem))
                {
                    return monitoredItem;
                }
                return null;
            }
        }

        /// <summary>
        /// Changes the publishing enabled state for the subscription.
        /// </summary>
        /// <param name="enabled"></param>
        /// <param name="ct"></param>
        /// <exception cref="ServiceResultException"></exception>
        internal async ValueTask SetPublishingModeAsync(bool enabled, CancellationToken ct)
        {
            VerifySubscriptionState(true);

            // modify the subscription.
            var subscriptionIds = new UInt32Collection { Id };
            var response = await _session.SetPublishingModeAsync(
                null, enabled, subscriptionIds, ct).ConfigureAwait(false);

            // validate response.
            ClientBase.ValidateResponse(response.Results, subscriptionIds);
            ClientBase.ValidateDiagnosticInfos(response.DiagnosticInfos, subscriptionIds);

            if (StatusCode.IsBad(response.Results[0]))
            {
                throw new ServiceResultException(
                    ClientBase.GetResult(response.Results[0], 0,
                    response.DiagnosticInfos, response.ResponseHeader));
            }
        }

        /// <summary>
        /// Creates a subscription on the server and adds all monitored items.
        /// </summary>
        /// <param name="ct"></param>
        internal async ValueTask CreateAsync(CancellationToken ct)
        {
            // create the subscription.
            var revisedMaxKeepAliveCount = KeepAliveCount;
            var revisedLifetimeCount = LifetimeCount;

            AdjustCounts(ref revisedMaxKeepAliveCount, ref revisedLifetimeCount);

            var response = await _session.CreateSubscriptionAsync(null,
                PublishingInterval.TotalMilliseconds, revisedLifetimeCount,
                revisedMaxKeepAliveCount, MaxNotificationsPerPublish, PublishingEnabled,
                Priority, ct).ConfigureAwait(false);

            OnSubscriptionUpdateComplete(true, response.SubscriptionId,
                TimeSpan.FromMilliseconds(response.RevisedPublishingInterval),
                response.RevisedMaxKeepAliveCount, response.RevisedLifetimeCount,
                MaxNotificationsPerPublish);
        }

        /// <summary>
        /// Modifies a subscription on the server.
        /// </summary>
        /// <param name="ct"></param>
        internal async ValueTask ModifyAsync(CancellationToken ct)
        {
            // modify the subscription.
            var revisedKeepAliveCount = KeepAliveCount;
            var revisedLifetimeCounter = LifetimeCount;

            AdjustCounts(ref revisedKeepAliveCount, ref revisedLifetimeCounter);

            if (revisedKeepAliveCount != CurrentKeepAliveCount ||
                revisedLifetimeCounter != CurrentLifetimeCount ||
                Priority != CurrentPriority ||
                MaxNotificationsPerPublish != CurrentMaxNotificationsPerPublish ||
                PublishingInterval != CurrentPublishingInterval)
            {
                var response = await _session.ModifySubscriptionAsync(null, Id,
                    PublishingInterval.TotalMilliseconds, revisedLifetimeCounter,
                    revisedKeepAliveCount, MaxNotificationsPerPublish, Priority,
                    ct).ConfigureAwait(false);

                OnSubscriptionUpdateComplete(false, 0,
                    TimeSpan.FromMilliseconds(response.RevisedPublishingInterval),
                    response.RevisedMaxKeepAliveCount, response.RevisedLifetimeCount,
                    MaxNotificationsPerPublish);
            }

            if (PublishingEnabled != CurrentPublishingEnabled)
            {
                await SetPublishingModeAsync(PublishingEnabled, ct).ConfigureAwait(false);

                _logger.LogInformation(
                    "{Subscription}: Modified - Publishing is now {New}.",
                    this, PublishingEnabled ? "Enabled" : "Disabled");

                // update current state.
                CurrentPublishingEnabled = PublishingEnabled;
            }
        }

        /// <summary>
        /// Creates all items on the server that have not already been created.
        /// </summary>
        /// <param name="ct"></param>
        internal async ValueTask<IReadOnlyList<MonitoredItem>> CreateItemsAsync(CancellationToken ct)
        {
            VerifySubscriptionState(true);

            var requestItems = new MonitoredItemCreateRequestCollection();
            var itemsToCreate = new List<MonitoredItem>();
            lock (_cache)
            {
                foreach (var monitoredItem in _monitoredItems.Values)
                {
                    // ignore items that have been created.
                    if (monitoredItem.Created)
                    {
                        continue;
                    }

                    // build item request.
                    var request = new MonitoredItemCreateRequest
                    {
                        ItemToMonitor = new ReadValueId
                        {
                            NodeId = monitoredItem.ResolvedNodeId,
                            AttributeId = monitoredItem.AttributeId,
                            IndexRange = monitoredItem.IndexRange,
                            DataEncoding = monitoredItem.Encoding
                        },
                        MonitoringMode = monitoredItem.MonitoringMode,
                        RequestedParameters = new MonitoringParameters
                        {
                            ClientHandle = monitoredItem.ClientHandle,
                            SamplingInterval =
                                monitoredItem.SamplingInterval.TotalMilliseconds,
                            QueueSize = monitoredItem.QueueSize,
                            DiscardOldest = monitoredItem.DiscardOldest,
                            Filter = monitoredItem.Filter == null ? null :
                                new ExtensionObject(monitoredItem.Filter)
                        }
                    };
                    requestItems.Add(request);
                    itemsToCreate.Add(monitoredItem);
                }
            }

            if (requestItems.Count == 0)
            {
                return itemsToCreate;
            }

            // create monitored items.
            var response = await _session.CreateMonitoredItemsAsync(null, Id,
                TimestampsToReturn.Both, requestItems, ct).ConfigureAwait(false);
            var results = response.Results;
            ClientBase.ValidateResponse(results, itemsToCreate);
            ClientBase.ValidateDiagnosticInfos(response.DiagnosticInfos, itemsToCreate);

            // update results.
            for (var index = 0; index < results.Count; index++)
            {
                itemsToCreate[index].SetCreateResult(requestItems[index],
                    results[index], index, response.DiagnosticInfos,
                    response.ResponseHeader);
            }

            // return the list of items affected by the change.
            return itemsToCreate;
        }

        /// <summary>
        /// Modifies all items that have been changed.
        /// </summary>
        /// <param name="ct"></param>
        internal async ValueTask<IReadOnlyList<MonitoredItem>> ModifyItemsAsync(CancellationToken ct)
        {
            VerifySubscriptionState(true);

            var requestItems = new MonitoredItemModifyRequestCollection();
            var itemsToModify = new List<MonitoredItem>();

            lock (_cache)
            {
                foreach (var monitoredItem in _monitoredItems.Values)
                {
                    // ignore items that have been created or modified.
                    if (!monitoredItem.Created || !monitoredItem.OptionsChanged)
                    {
                        continue;
                    }

                    // build item request.
                    var request = new MonitoredItemModifyRequest
                    {
                        MonitoredItemId = monitoredItem.ServerId,
                        RequestedParameters = new MonitoringParameters
                        {
                            ClientHandle = monitoredItem.ClientHandle,
                            SamplingInterval =
                                monitoredItem.SamplingInterval.TotalMilliseconds,
                            QueueSize = monitoredItem.QueueSize,
                            DiscardOldest = monitoredItem.DiscardOldest,
                            Filter = monitoredItem.Filter == null ? null :
                                new ExtensionObject(monitoredItem.Filter)
                        }
                    };
                    requestItems.Add(request);
                    itemsToModify.Add(monitoredItem);
                }
            }
            if (requestItems.Count == 0)
            {
                return itemsToModify;
            }

            // modify the subscription.
            var response = await _session.ModifyMonitoredItemsAsync(null, Id,
                TimestampsToReturn.Both, requestItems, ct).ConfigureAwait(false);

            var results = response.Results;
            ClientBase.ValidateResponse(results, itemsToModify);
            ClientBase.ValidateDiagnosticInfos(response.DiagnosticInfos, itemsToModify);

            // update results.
            for (var index = 0; index < results.Count; index++)
            {
                itemsToModify[index].SetModifyResult(requestItems[index], results[index],
                    index, response.DiagnosticInfos, response.ResponseHeader);
            }

            // return the list of items affected by the change.
            return itemsToModify;
        }

        /// <summary>
        /// Deletes all items that have been marked for deletion.
        /// </summary>
        /// <param name="ct"></param>
        internal async ValueTask DeleteItemsAsync(CancellationToken ct)
        {
            VerifySubscriptionState(true);
            if (_deletedItems.Count == 0)
            {
                return;
            }
            var itemsToDelete = _deletedItems.ToList();
            _deletedItems.Clear();
            try
            {
                var monitoredItemIds = new UInt32Collection(itemsToDelete);
                var response = await _session.DeleteMonitoredItemsAsync(null, Id,
                    monitoredItemIds, ct).ConfigureAwait(false);
                var results = response.Results;
                ClientBase.ValidateResponse(results, monitoredItemIds);
                ClientBase.ValidateDiagnosticInfos(response.DiagnosticInfos,
                    monitoredItemIds);
            }
            catch
            {
                _deletedItems.AddRange(itemsToDelete);
                throw;
            }
        }

        /// <summary>
        /// <para>
        /// If a Client called CreateMonitoredItems during the network interruption
        /// and the call succeeded in the Server but did not return to the Client,
        /// then the Client does not know if the call succeeded. The Client may
        /// receive data changes for these monitored items but is not able to remove
        /// them since it does not know the Server handle.
        /// </para>
        /// <para>
        /// There is also no way for the Client to detect if the create succeeded.
        /// To delete and recreate the Subscription is also not an option since
        /// there may be several monitored items operating normally that should
        /// not be interrupted.
        /// </para>
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async ValueTask<bool> TrySynchronizeServerAndClientHandlesAsync(
            CancellationToken ct)
        {
            var (success, serverHandleStateMap) = await GetMonitoredItemsAsync(
                ct).ConfigureAwait(false);

            lock (_cache)
            {
                if (!success)
                {
                    // Reset all items
                    foreach (var monitoredItem in _monitoredItems.Values)
                    {
                        monitoredItem.Reset();
                    }
                    return false;
                }

                var monitoredItems = _monitoredItems.ToDictionary();
                _monitoredItems.Clear();

                //
                // Assign the server id to the item with the matching client handle. This
                // handles the case where the CreateMonitoredItems call succeeded on the
                // server side, but the response was not provided back.
                //
                var clientServerHandleMap = serverHandleStateMap
                    .ToDictionary(m => m.clientHandle, m => m.serverHandle);
                foreach (var monitoredItem in monitoredItems.ToList())
                {
                    //
                    // Adjust any items where the server handles does not map to the
                    // handle the server has assigned.
                    //
                    var clientHandle = monitoredItem.Value.ClientHandle;
                    if (clientServerHandleMap.Remove(clientHandle, out var serverHandle))
                    {
                        monitoredItem.Value.SetTransferResult(clientHandle, serverHandle);
                        monitoredItems.Remove(monitoredItem.Key);
                        _monitoredItems.Add(clientHandle, monitoredItem.Value);
                    }
                }

                //
                // Assign the server side client handle to the item with the same server id
                // This handles the case where we are recreating the subscription from a
                // previously stored state.
                //
                var serverClientHandleMap = clientServerHandleMap
                    .ToDictionary(m => m.Value, m => m.Key);
                foreach (var monitoredItem in monitoredItems.ToList())
                {
                    var serverHandle = monitoredItem.Value.ServerId;
                    if (serverClientHandleMap.Remove(serverHandle, out var clientHandle))
                    {
                        //
                        // There should not be any more item with the same client handle
                        // in this subscription, they were updated before. TODO: Assert
                        //
                        monitoredItem.Value.SetTransferResult(clientHandle, serverHandle);
                        monitoredItems.Remove(monitoredItem.Key);
                        _monitoredItems.Add(clientHandle, monitoredItem.Value);
                    }
                }

                // Ensure all items on the server that are not in the subscription are deleted
                _deletedItems.Clear();
                _deletedItems.AddRange(serverClientHandleMap.Keys);

                // Remaining items do not exist anymore on the server and need to be recreated
                foreach (var missingOnServer in monitoredItems.Values)
                {
                    if (missingOnServer.Created)
                    {
                        _logger.LogDebug("{Subscription}: Recreate client item {Item}.", this,
                            missingOnServer);
                        missingOnServer.Reset();
                    }
                    _monitoredItems.Add(missingOnServer.ClientHandle, missingOnServer);
                }
            }
            return true;
        }

        private record struct MonitoredItemsHandles(bool Success,
            IReadOnlyList<(uint serverHandle, uint clientHandle)> Handles);

        /// <summary>
        /// Call the GetMonitoredItems method on the server.
        /// </summary>
        /// <param name="ct"></param>
        private async ValueTask<MonitoredItemsHandles> GetMonitoredItemsAsync(
            CancellationToken ct)
        {
            try
            {
                var requests = new CallMethodRequestCollection
                {
                    new CallMethodRequest
                    {
                        ObjectId = ObjectIds.Server,
                        MethodId = MethodIds.Server_GetMonitoredItems,
                        InputArguments = new VariantCollection { new Variant(Id) }
                    }
                };

                var response = await _session.CallAsync(null, requests, ct).ConfigureAwait(false);
                var results = response.Results;
                var diagnosticInfos = response.DiagnosticInfos;
                ClientBase.ValidateResponse(results, requests);
                ClientBase.ValidateDiagnosticInfos(diagnosticInfos, requests);
                if (StatusCode.IsBad(results[0].StatusCode))
                {
                    throw ServiceResultException.Create(results[0].StatusCode,
                        0, diagnosticInfos, response.ResponseHeader.StringTable);
                }

                var outputArguments = results[0].OutputArguments;
                if (outputArguments.Count != 2 ||
                    outputArguments[0].Value is not uint[] serverHandles ||
                    outputArguments[1].Value is not uint[] clientHandles ||
                    clientHandles.Length != serverHandles.Length)
                {
                    throw ServiceResultException.Create(StatusCodes.BadUnexpectedError,
                        "Output arguments incorrect");
                }
                return new MonitoredItemsHandles(true, serverHandles.Zip(clientHandles).ToList());
            }
            catch (ServiceResultException sre)
            {
                _logger.LogError(sre,
                    "{Subscription}: Failed to call GetMonitoredItems on server", this);
                return new MonitoredItemsHandles(false, Array.Empty<(uint, uint)>());
            }
        }

        /// <summary>
        /// Stop the keep alive timer for the subscription.
        /// </summary>
        private void StopKeepAliveTimer()
        {
            _publishTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        }

        /// <summary>
        /// Starts a timer to ensure publish requests are sent frequently
        /// enough to detect network interruptions.
        /// </summary>
        private void StartKeepAliveTimer()
        {
            _lastNotificationTimestamp = Observability.TimeProvider.GetTimestamp();
            _keepAliveInterval = CurrentPublishingInterval * (CurrentKeepAliveCount + 1);
            if (_keepAliveInterval < kMinKeepAliveTimerInterval)
            {
                _keepAliveInterval = PublishingInterval * (KeepAliveCount + 1);
            }
            if (_keepAliveInterval > Timeout.InfiniteTimeSpan)
            {
                _keepAliveInterval = Timeout.InfiniteTimeSpan;
            }
            if (_keepAliveInterval < kMinKeepAliveTimerInterval)
            {
                _keepAliveInterval = kMinKeepAliveTimerInterval;
            }
            _publishTimer.Change(_keepAliveInterval, _keepAliveInterval);
        }

        /// <summary>
        /// Checks if a notification has arrived in time.
        /// </summary>
        /// <param name="state"></param>
        private void OnKeepAlive(object? state)
        {
            // check if a publish has arrived.
            if (PublishingStopped)
            {
                Interlocked.Increment(ref _publishLateCount);
                OnPublishStateChanged(PublishState.Stopped);
            }
        }

        /// <summary>
        /// Update the subscription with the given revised settings.
        /// </summary>
        /// <param name="created"></param>
        /// <param name="subscriptionId"></param>
        /// <param name="revisedPublishingInterval"></param>
        /// <param name="revisedKeepAliveCount"></param>
        /// <param name="revisedLifetimeCount"></param>
        /// <param name="maxNotificationsPerPublish"></param>
        private void OnSubscriptionUpdateComplete(bool created,
            uint subscriptionId, TimeSpan revisedPublishingInterval,
            uint revisedKeepAliveCount, uint revisedLifetimeCount,
            uint maxNotificationsPerPublish)
        {
            if (created &&
                PublishingEnabled != CurrentPublishingEnabled)
            {
                _logger.LogInformation(
                    "{Subscription}: Created - Publishing is {New}.",
                    this, PublishingEnabled ? "Enabled" : "Disabled");
                CurrentPublishingEnabled = PublishingEnabled;
            }

            if (CurrentKeepAliveCount != revisedKeepAliveCount)
            {
                _logger.LogInformation(
                    "{Subscription}: Changed KeepAliveCount to {New}.",
                    this, revisedKeepAliveCount);

                CurrentKeepAliveCount = revisedKeepAliveCount;
            }

            if (CurrentPublishingInterval != revisedPublishingInterval)
            {
                _logger.LogInformation(
                    "{Subscription}: Changed PublishingInterval to {New}.",
                    this, revisedPublishingInterval);
                CurrentPublishingInterval = revisedPublishingInterval;
            }

            if (CurrentMaxNotificationsPerPublish != maxNotificationsPerPublish)
            {
                _logger.LogInformation(
                    "{Subscription}: Change MaxNotificationsPerPublish to {New}.",
                    this, maxNotificationsPerPublish);
                CurrentMaxNotificationsPerPublish = maxNotificationsPerPublish;
            }

            if (CurrentLifetimeCount != revisedLifetimeCount)
            {
                _logger.LogInformation(
                    "{Subscription}: Changed LifetimeCount to {New}.",
                    this, revisedLifetimeCount);
                CurrentLifetimeCount = revisedLifetimeCount;
            }

            if (CurrentPriority != Priority)
            {
                _logger.LogInformation(
                    "{Subscription}: Changed Priority to {New}.",
                    this, Priority);
                CurrentPriority = Priority;
            }

            if (created)
            {
                Id = subscriptionId;
                StartKeepAliveTimer();
            }
        }

        /// <summary>
        /// Delete the subscription.
        /// Ignore errors, always reset all parameter.
        /// </summary>
        private void OnSubscriptionDeleteCompleted()
        {
            _lastSequenceNumberProcessed = 0;
            _lastNotificationTimestamp = 0;

            Id = 0;
            CurrentPublishingInterval = TimeSpan.Zero;
            CurrentKeepAliveCount = 0;
            CurrentPublishingEnabled = false;
            CurrentPriority = 0;

            foreach (var monitoredItem in MonitoredItems)
            {
                monitoredItem.Dispose();
            }
            _deletedItems.Clear();
        }

        /// <summary>
        /// Ensures sensible values for the counts.
        /// </summary>
        /// <param name="keepAliveCount"></param>
        /// <param name="lifetimeCount"></param>
        private void AdjustCounts(ref uint keepAliveCount, ref uint lifetimeCount)
        {
            const uint kDefaultKeepAlive = 10;
            const uint kDefaultLifeTime = 1000;

            // keep alive count must be at least 1, 10 is a good default.
            if (keepAliveCount == 0)
            {
                _logger.LogInformation("{Subscription}: Adjusted KeepAliveCount " +
                    "from {Old} to {New}.", this, keepAliveCount, kDefaultKeepAlive);
                keepAliveCount = kDefaultKeepAlive;
            }

            // ensure the lifetime is sensible given the sampling interval.
            if (PublishingInterval > TimeSpan.Zero)
            {
                if (MinLifetimeInterval > TimeSpan.Zero &&
                    MinLifetimeInterval < _session.SessionTimeout)
                {
                    _logger.LogWarning(
                        "{Subscription}: A smaller minimum LifetimeInterval " +
                        "{Counter}ms than session timeout {Timeout}ms configured.",
                        this, MinLifetimeInterval, _session.SessionTimeout);
                }

                var minLifetimeInterval = (uint)MinLifetimeInterval.TotalMilliseconds;
                var publishingInterval = (uint)PublishingInterval.TotalMilliseconds;
                var minLifetimeCount = minLifetimeInterval / publishingInterval;
                if (lifetimeCount < minLifetimeCount)
                {
                    lifetimeCount = minLifetimeCount;

                    if (minLifetimeInterval % publishingInterval != 0)
                    {
                        lifetimeCount++;
                    }
                    _logger.LogInformation(
                        "{Subscription}: Adjusted LifetimeCount to value={New}.",
                        this, lifetimeCount);
                }

                if (lifetimeCount * PublishingInterval < _session.SessionTimeout)
                {
                    _logger.LogWarning(
                        "{Subscription}: Lifetime {LifeTime}ms configured is less " +
                        "than session timeout {Timeout}ms.",
                        this, lifetimeCount * PublishingInterval, _session.SessionTimeout);
                }
            }
            else if (lifetimeCount == 0)
            {
                // don't know what the sampling interval will be - use something large
                // enough to ensure the user does not experience unexpected drop outs.
                _logger.LogInformation(
                    "{Subscription}: Adjusted LifetimeCount from {Old} to {New}. ",
                    this, lifetimeCount, kDefaultLifeTime);
                lifetimeCount = kDefaultLifeTime;
            }

            // validate spec: lifetimecount shall be at least 3*keepAliveCount
            var minLifeTimeCount = 3 * keepAliveCount;
            if (lifetimeCount < minLifeTimeCount)
            {
                _logger.LogInformation(
                    "{Subscription}: Adjusted LifetimeCount from {Old} to {New}.",
                    this, lifetimeCount, minLifeTimeCount);
                lifetimeCount = minLifeTimeCount;
            }
        }

        /// <summary>
        /// Throws an exception if the subscription is not in the correct state.
        /// </summary>
        /// <param name="created"></param>
        /// <exception cref="ServiceResultException"></exception>
        private void VerifySubscriptionState(bool created)
        {
            if (created && Id == 0)
            {
                throw new ServiceResultException(StatusCodes.BadInvalidState,
                    "Subscription has not been created.");
            }

            if (!created && Id != 0)
            {
                throw new ServiceResultException(StatusCodes.BadInvalidState,
                    "Subscription has already been created.");
            }
        }

        private static readonly TimeSpan kMinKeepAliveTimerInterval = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan kKeepAliveTimerMargin = TimeSpan.FromSeconds(1);
        private TimeSpan _keepAliveInterval;
        private int _publishLateCount;
        private readonly List<uint> _deletedItems = new();
        private readonly ITimer _publishTimer;
        private readonly object _cache = new();
        private readonly Dictionary<uint, MonitoredItem> _monitoredItems = new();
        private readonly IDisposable? _changeTracking;
    }
}
