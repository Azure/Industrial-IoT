// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation, The OPC Foundation, Inc.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client
{
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Threading.Channels;
    using System.Linq;
    using System.Collections.Immutable;
    using System.Diagnostics;

    /// <summary>
    /// A subscription.
    /// </summary>
    public abstract class Subscription : IAsyncDisposable
    {
        /// <summary>
        /// The publishing interval.
        /// </summary>
        public TimeSpan PublishingInterval { get; set; }

        /// <summary>
        /// The keep alive count.
        /// </summary>
        public uint KeepAliveCount { get; set; }

        /// <summary>
        /// The life time of the subscription in counts of
        /// publish interval.
        /// LifetimeCount shall be at least 3*KeepAliveCount.
        /// </summary>
        public uint LifetimeCount { get; set; }

        /// <summary>
        /// The maximum number of notifications per publish request.
        /// </summary>
        public uint MaxNotificationsPerPublish { get; set; }

        /// <summary>
        /// Whether publishing is enabled.
        /// </summary>
        public bool PublishingEnabled { get; set; }

        /// <summary>
        /// The priority assigned to subscription.
        /// </summary>
        public byte Priority { get; set; }

        /// <summary>
        /// The timestamps to return with the notification messages.
        /// </summary>
        public TimestampsToReturn TimestampsToReturn { get; set; }
            = TimestampsToReturn.Both;

        /// <summary>
        /// The minimum lifetime for subscriptions
        /// </summary>
        public TimeSpan MinLifetimeInterval { get; set; }

        /// <summary>
        /// If the available sequence numbers of a subscription
        /// are republished or acknowledged after a transfer.
        /// </summary>
        /// <remarks>
        /// Default <c>false</c>, set to <c>true</c> if no data
        /// loss is important and available publish requests
        /// (sequence numbers) that were never acknowledged should
        /// be recovered with a republish. The setting is used
        /// after a subscription transfer.
        /// </remarks>
        public bool RepublishAfterTransfer { get; set; }

        /// <summary>
        /// Current priority
        /// </summary>
        public byte CurrentPriority { get; private set; }

        /// <summary>
        /// The current publishing interval.
        /// </summary>
        public TimeSpan CurrentPublishingInterval { get; private set; }

        /// <summary>
        /// The current keep alive count.
        /// </summary>
        public uint CurrentKeepAliveCount { get; private set; }

        /// <summary>
        /// The current lifetime count.
        /// </summary>
        public uint CurrentLifetimeCount { get; private set; }

        /// <summary>
        /// Whether publishing is currently enabled.
        /// </summary>
        public bool CurrentPublishingEnabled { get; private set; }

        /// <summary>
        /// The items to monitor.
        /// </summary>
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

        /// <summary>
        /// Returns true if the subscription has changes that need to be applied.
        /// </summary>
        public bool ChangesPending
        {
            get
            {
                lock (_cache)
                {
                    if (_deletedItems.Count > 0)
                    {
                        return true;
                    }
                    foreach (var monitoredItem in _monitoredItems.Values)
                    {
                        if (Created && !monitoredItem.Created)
                        {
                            return true;
                        }

                        if (monitoredItem.AttributesModified)
                        {
                            return true;
                        }
                    }
                    return false;
                }
            }
        }

        /// <summary>
        /// Returns the number of monitored items.
        /// </summary>
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

        /// <summary>
        /// The session that owns the subscription item.
        /// </summary>
        public Session Session { get; }

        /// <summary>
        /// The unique identifier assigned by the server.
        /// </summary>
        protected internal uint Id { get; private set; }

        /// <summary>
        /// Whether the subscription has been created on the server.
        /// </summary>
        public bool Created => Id != 0;

        /// <summary>
        /// Returns true if the subscription is not receiving publishes.
        /// </summary>
        public bool PublishingStopped
        {
            get
            {
                var timeSinceLastNotification = HiResClock.TickCount - _lastNotificationTickCount;
                if (timeSinceLastNotification > _keepAliveInterval + kKeepAliveTimerMargin)
                {
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Create subscription
        /// </summary>
        /// <param name="session"></param>
        /// <param name="completion"></param>
        /// <param name="logger"></param>
        protected Subscription(Session session, IAckQueue completion,
            ILogger logger)
        {
            _logger = logger;
            Session = session;
            _completion = completion;
            _messages = Channel.CreateUnboundedPrioritized<IncomingMessage>(
                new UnboundedPrioritizedChannelOptions<IncomingMessage>
                {
                    SingleReader = true
                });
            _publishTimer = new Timer(OnKeepAlive);
            _messageWorkerTask = ProcessMessagesAsync(_cts.Token);

            TimestampsToReturn = TimestampsToReturn.Both;
            RepublishAfterTransfer = false;
        }

        /// <inheritdoc/>
        public ValueTask DisposeAsync()
        {
            GC.SuppressFinalize(this);
            return DisposeAsync(true);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{Session.SessionId}:{Id}";
        }

        /// <summary>
        /// Changes the publishing enabled state for the subscription.
        /// </summary>
        /// <param name="enabled"></param>
        /// <param name="ct"></param>
        /// <exception cref="ServiceResultException"></exception>
        public async Task SetPublishingModeAsync(bool enabled, CancellationToken ct)
        {
            VerifySubscriptionState(true);

            // modify the subscription.
            UInt32Collection subscriptionIds = new uint[] { Id };

            var response = await Session.SetPublishingModeAsync(
                null, enabled, new uint[] { Id }, ct).ConfigureAwait(false);

            // validate response.
            ClientBase.ValidateResponse(response.Results, subscriptionIds);
            ClientBase.ValidateDiagnosticInfos(response.DiagnosticInfos, subscriptionIds);

            if (StatusCode.IsBad(response.Results[0]))
            {
                throw new ServiceResultException(
                    ClientBase.GetResult(response.Results[0], 0,
                    response.DiagnosticInfos, response.ResponseHeader));
            }

            // update current state.
            CurrentPublishingEnabled = PublishingEnabled = enabled;
        }

        /// <summary>
        /// Add an item to the subscription
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        public MonitoredItem AddMonitoredItem(MonitoredItemOptions? options = null)
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
        public async Task ApplyChangesAsync(CancellationToken ct)
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
        public async Task ConditionRefreshAsync(CancellationToken ct)
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
            await Session.CallAsync(null, methodsToCall, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Creates a subscription on the server and adds all monitored items.
        /// </summary>
        /// <param name="forceCreate"></param>
        /// <param name="ct"></param>
        public async Task CreateAsync(bool forceCreate, CancellationToken ct)
        {
            if (!forceCreate)
            {
                VerifySubscriptionState(false);
            }
            else
            {
                Reset();
            }

            // create the subscription.
            var revisedMaxKeepAliveCount = KeepAliveCount;
            var revisedLifetimeCount = LifetimeCount;

            AdjustCounts(ref revisedMaxKeepAliveCount, ref revisedLifetimeCount);

            var response = await Session.CreateSubscriptionAsync(null,
                PublishingInterval.TotalMilliseconds, revisedLifetimeCount,
                revisedMaxKeepAliveCount, MaxNotificationsPerPublish, PublishingEnabled,
                Priority, ct).ConfigureAwait(false);

            OnSubscriptionUpdateComplete(true, response.SubscriptionId,
                TimeSpan.FromMilliseconds(response.RevisedPublishingInterval),
                response.RevisedMaxKeepAliveCount, response.RevisedLifetimeCount);

            await CreateItemsAsync(ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Deletes a subscription on the server, but keeps the subscription in the session.
        /// </summary>
        /// <param name="silent"></param>
        /// <param name="ct"></param>
        /// <exception cref="ServiceResultException"></exception>
        public async Task DeleteAsync(bool silent, CancellationToken ct)
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
                UInt32Collection subscriptionIds = new uint[] { Id };

                var response = await Session.DeleteSubscriptionsAsync(null, subscriptionIds,
                    ct).ConfigureAwait(false);

                // validate response.
                ClientBase.ValidateResponse(response.Results, subscriptionIds);
                ClientBase.ValidateDiagnosticInfos(response.DiagnosticInfos, subscriptionIds);

                if (StatusCode.IsBad(response.Results[0]))
                {
                    throw new ServiceResultException(
                        ClientBase.GetResult(response.Results[0], 0, response.DiagnosticInfos,
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

        /// <summary>
        /// Modifies a subscription on the server.
        /// </summary>
        /// <param name="ct"></param>
        public async Task ModifyAsync(CancellationToken ct)
        {
            VerifySubscriptionState(true);

            // modify the subscription.
            var revisedKeepAliveCount = KeepAliveCount;
            var revisedLifetimeCounter = LifetimeCount;

            AdjustCounts(ref revisedKeepAliveCount, ref revisedLifetimeCounter);
            var response = await Session.ModifySubscriptionAsync(null, Id,
                PublishingInterval.TotalMilliseconds, revisedLifetimeCounter,
                revisedKeepAliveCount, MaxNotificationsPerPublish, Priority,
                ct).ConfigureAwait(false);
            OnSubscriptionUpdateComplete(false, 0,
                TimeSpan.FromMilliseconds(response.RevisedPublishingInterval),
                response.RevisedMaxKeepAliveCount, response.RevisedLifetimeCount);
        }

        /// <summary>
        /// Creates all items on the server that have not already been created.
        /// </summary>
        /// <param name="ct"></param>
        internal async Task<IReadOnlyList<MonitoredItem>> CreateItemsAsync(CancellationToken ct)
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
            var response = await Session.CreateMonitoredItemsAsync(null, Id,
                TimestampsToReturn, requestItems, ct).ConfigureAwait(false);
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
        internal async Task<IReadOnlyList<MonitoredItem>> ModifyItemsAsync(CancellationToken ct)
        {
            VerifySubscriptionState(true);

            var requestItems = new MonitoredItemModifyRequestCollection();
            var itemsToModify = new List<MonitoredItem>();

            lock (_cache)
            {
                foreach (var monitoredItem in _monitoredItems.Values)
                {
                    // ignore items that have been created or modified.
                    if (!monitoredItem.Created || !monitoredItem.AttributesModified)
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
            var response = await Session.ModifyMonitoredItemsAsync(null, Id,
                TimestampsToReturn, requestItems, ct).ConfigureAwait(false);

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
        internal async Task DeleteItemsAsync(CancellationToken ct)
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
                var response = await Session.DeleteMonitoredItemsAsync(null, Id,
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
        /// Dispose subscription
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual async ValueTask DisposeAsync(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    _publishTimer.Dispose();
                    _messages.Writer.TryComplete();
                    _cts.Cancel();

                    await _messageWorkerTask.ConfigureAwait(false);
                }
                finally
                {
                    _cts.Dispose();
                }
            }
        }

        /// <summary>
        /// Create monitored item
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        protected abstract MonitoredItem CreateMonitoredItem(MonitoredItemOptions? options);

        /// <summary>
        /// Removes an item from the subscription.
        /// </summary>
        /// <param name="monitoredItem"></param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="monitoredItem"/> is <c>null</c>.</exception>
        internal void RemoveItem(MonitoredItem monitoredItem)
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

        /// <summary>S
        /// Process status change notification
        /// </summary>
        /// <param name="sequenceNumber"></param>
        /// <param name="publishTime"></param>
        /// <param name="notification"></param>
        /// <param name="publishStateMask"></param>
        /// <param name="stringTable"></param>
        /// <returns></returns>
        protected virtual ValueTask OnStatusChangeNotificationAsync(uint sequenceNumber,
            DateTime publishTime, StatusChangeNotification notification,
            PublishState publishStateMask, IReadOnlyList<string> stringTable)
        {
            // TODO
            return ValueTask.CompletedTask;
        }

        /// <summary>
        /// Process keep alive notification
        /// </summary>
        /// <param name="sequenceNumber"></param>
        /// <param name="publishTime"></param>
        /// <param name="publishStateMask"></param>
        /// <returns></returns>
        protected abstract ValueTask OnKeepAliveNotificationAsync(uint sequenceNumber,
            DateTime publishTime, PublishState publishStateMask);

        /// <summary>
        /// Process data change notification
        /// </summary>
        /// <param name="sequenceNumber"></param>
        /// <param name="publishTime"></param>
        /// <param name="notification"></param>
        /// <param name="publishStateMask"></param>
        /// <param name="stringTable"></param>
        /// <returns></returns>
        protected abstract ValueTask OnDataChangeNotificationAsync(uint sequenceNumber,
            DateTime publishTime, DataChangeNotification notification,
            PublishState publishStateMask, IReadOnlyList<string> stringTable);

        /// <summary>
        /// Process event notification
        /// </summary>
        /// <param name="sequenceNumber"></param>
        /// <param name="publishTime"></param>
        /// <param name="notification"></param>
        /// <param name="publishStateMask"></param>
        /// <param name="stringTable"></param>
        /// <returns></returns>
        protected abstract ValueTask OnEventDataNotificationAsync(uint sequenceNumber,
            DateTime publishTime, EventNotificationList notification,
            PublishState publishStateMask, IReadOnlyList<string> stringTable);

        /// <summary>
        /// On publish state changed
        /// </summary>
        /// <param name="stateMask"></param>
        /// <returns></returns>
        protected virtual void OnPublishStateChanged(PublishState stateMask)
        {
            if (stateMask.HasFlag(PublishState.Stopped))
            {
                _logger.LogInformation("{Subscription} STOPPED!", this);
            }
            if (stateMask.HasFlag(PublishState.Recovered))
            {
                _logger.LogInformation("{Subscription} RECOVERED!", this);
            }
        }

        /// <summary>
        /// Set monitoring mode of items.
        /// </summary>
        /// <param name="monitoringMode"></param>
        /// <param name="monitoredItems"></param>
        /// <param name="ct"></param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="monitoredItems"/> is <c>null</c>.</exception>
        protected async Task<IReadOnlyList<ServiceResult>> SetMonitoringModeAsync(
            MonitoringMode monitoringMode, IReadOnlyList<MonitoredItem> monitoredItems,
            CancellationToken ct)
        {
            VerifySubscriptionState(true);

            if (monitoredItems.Count == 0)
            {
                return Array.Empty<ServiceResult>();
            }

            // get list of items to update.
            var monitoredItemIds = new UInt32Collection();
            foreach (var monitoredItem in monitoredItems)
            {
                monitoredItemIds.Add(monitoredItem.ServerId);
            }

            var response = await Session.SetMonitoringModeAsync(null, Id,
                monitoringMode, monitoredItemIds, ct).ConfigureAwait(false);
            var results = response.Results;
            ClientBase.ValidateResponse(results, monitoredItemIds);
            ClientBase.ValidateDiagnosticInfos(response.DiagnosticInfos,
                monitoredItemIds);

            // update results.
            var errors = new List<ServiceResult>();

            // update results.
            var noErrors = true;
            for (var index = 0; index < results.Count; index++)
            {
                ServiceResult error;
                if (StatusCode.IsBad(results[index]))
                {
                    error = ClientBase.GetResult(results[index], index,
                        response.DiagnosticInfos, response.ResponseHeader);
                    noErrors = false;
                }
                else
                {
                    monitoredItems[index].CurrentMonitoringMode = monitoringMode;
                    error = ServiceResult.Good;
                }
                errors.Add(error);
            }

            // return empty list if no errors occurred.
            if (noErrors)
            {
                return Array.Empty<ServiceResult>();
            }
            return errors;
        }

        /// <summary>
        /// Returns the monitored item identified by the client handle.
        /// </summary>
        /// <param name="clientHandle"></param>
        protected MonitoredItem? FindItemByClientHandle(uint clientHandle)
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
        /// Called after the subscription was transferred.
        /// </summary>
        /// <param name="availableSequenceNumbers">A list of sequence number ranges
        /// that identify NotificationMessages that are in the Subscription’s
        /// retransmission queue. This parameter is null if the transfer of the
        /// Subscription failed.</param>
        /// <param name="subscriptionId">Id of the transferred subscription.</param>
        /// <param name="ct">The cancellation token.</param>
        internal async Task<bool> TransferAsync(IReadOnlyList<uint>? availableSequenceNumbers,
            uint? subscriptionId, CancellationToken ct)
        {
            StopKeepAliveTimer();
            if (subscriptionId.HasValue)
            {
                // sets state to 'Created'
                Id = subscriptionId.Value;
            }

            // ---- This could be done always
            var synchronizeHandles = subscriptionId.HasValue;
            if (synchronizeHandles)
            {
                // handle the case when the client restarts and loads the saved
                // subscriptions from storage
                var (success, handleMap) =
                    await GetMonitoredItemsAsync(ct).ConfigureAwait(false);
                if (!success)
                {
                    _logger.LogError("{Subscription}: The server failed to respond " +
                        "to GetMonitoredItems after transfer.", this);
                    return false;
                }

                var monitoredItemsCount = _monitoredItems.Count;
                if (handleMap.Count != monitoredItemsCount)
                {
                    // invalid state
                    _logger.LogError("{Subscription}: Number of Monitored Items " +
                        "on client and server do not match after transfer {Old}!={New}",
                        this, handleMap.Count, monitoredItemsCount);
                    return false;
                }

                lock (_cache)
                {
                    var currentItems = _monitoredItems.Values.ToList();
                    _monitoredItems.Clear();
                    var serverClientHandleMap = handleMap.ToDictionary();
                    foreach (var monitoredItem in _monitoredItems.Values)
                    {
                        if (serverClientHandleMap.TryGetValue(monitoredItem.ServerId,
                            out var clientHandle))
                        {
                            _monitoredItems[clientHandle] = monitoredItem;
                            monitoredItem.SetTransferResult(clientHandle);
                        }
                        else
                        {
                            // modify client handle on server
                            _monitoredItems[monitoredItem.ClientHandle] = monitoredItem;
                        }
                    }
                }
            }

            // save available sequence numbers
            if (availableSequenceNumbers != null)
            {
                _availableInRetransmissionQueue = availableSequenceNumbers;
            }

            if (subscriptionId.HasValue)
            {
                await ModifyItemsAsync(ct).ConfigureAwait(false);
            }
            StartKeepAliveTimer();
            return true;
        }

        /// <summary>
        /// Allows the session to add the notification message to the subscription for dispatch.
        /// </summary>
        /// <param name="availableSequenceNumbers"></param>
        /// <param name="message"></param>
        /// <param name="stringTable"></param>
        internal async ValueTask OnPublishReceivedAsync(IReadOnlyList<uint>? availableSequenceNumbers,
            NotificationMessage message, IReadOnlyList<string> stringTable)
        {
            // Reset the keep alive timer
            _publishTimer.Change(_keepAliveInterval, _keepAliveInterval);

            // send notification that publishing received a keep alive or has to republish.
            if (PublishingStopped)
            {
                OnPublishStateChanged(PublishState.Recovered);
            }

            if (availableSequenceNumbers != null)
            {
                _availableInRetransmissionQueue = availableSequenceNumbers;
            }
            _lastNotificationTickCount = HiResClock.TickCount;
            await _messages.Writer.WriteAsync(new IncomingMessage(
                message, stringTable, DateTime.UtcNow)).ConfigureAwait(false);
        }

        /// <summary>
        /// Call the GetMonitoredItems method on the server.
        /// </summary>
        /// <param name="ct"></param>
        private async Task<(bool, IReadOnlyList<(uint serverHandle, uint clientHandle)>)> GetMonitoredItemsAsync(
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

                var response = await Session.CallAsync(null, requests, ct).ConfigureAwait(false);
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
                return (true, serverHandles.Zip(clientHandles).ToList());
            }
            catch (ServiceResultException sre)
            {
                _logger.LogError(sre,
                    "{Subscription}: Failed to call GetMonitoredItems on server", this);
                return (false, Array.Empty<(uint, uint)>());
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
            _lastNotificationTickCount = HiResClock.TickCount;
            var currentPublishingInterval = CurrentPublishingInterval.TotalMilliseconds;
            _keepAliveInterval = (int)Math.Min(
                currentPublishingInterval * (CurrentKeepAliveCount + 1), int.MaxValue);
            if (_keepAliveInterval < kMinKeepAliveTimerInterval)
            {
                var publishingInterval = PublishingInterval.TotalMilliseconds;
                _keepAliveInterval = (int)Math.Min(
                    publishingInterval * (KeepAliveCount + 1), int.MaxValue);
                _keepAliveInterval = Math.Max(kMinKeepAliveTimerInterval, _keepAliveInterval);
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
        private void OnSubscriptionUpdateComplete(bool created,
            uint subscriptionId, TimeSpan revisedPublishingInterval,
            uint revisedKeepAliveCount, uint revisedLifetimeCount)
        {
            // update current state.
            CurrentPublishingInterval = revisedPublishingInterval;
            CurrentKeepAliveCount = revisedKeepAliveCount;
            CurrentLifetimeCount = revisedLifetimeCount;

            CurrentPriority = Priority;
            if (created)
            {
                CurrentPublishingEnabled = PublishingEnabled;
                Id = subscriptionId;
                StartKeepAliveTimer();
            }

            _logger.LogInformation(@"Successfully {Action} subscription {Subscription}'.
Actual (revised) state/desired state:
# PublishingEnabled {CurrentPublishingEnabled}/{PublishingEnabled}
# PublishingInterval {CurrentPublishingInterval}/{PublishingInterval}
# KeepAliveCount {CurrentKeepAliveCount}/{KeepAliveCount}
# LifetimeCount {CurrentLifetimeCount}/{LifetimeCount}", created ? "created" : "modified",
                this,
                CurrentPublishingEnabled, PublishingEnabled,
                CurrentPublishingInterval, PublishingInterval,
                CurrentKeepAliveCount, KeepAliveCount,
                CurrentLifetimeCount, LifetimeCount);
        }

        /// <summary>
        /// Delete the subscription.
        /// Ignore errors, always reset all parameter.
        /// </summary>
        private void OnSubscriptionDeleteCompleted()
        {
            _lastSequenceNumberProcessed = 0;
            _lastNotificationTickCount = 0;

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
        /// Reset the subscription
        /// </summary>
        private void Reset()
        {
            _lastSequenceNumberProcessed = 0;
            _lastNotificationTickCount = 0;

            Id = 0;
            CurrentPublishingInterval = TimeSpan.Zero;
            CurrentKeepAliveCount = 0;
            CurrentPublishingEnabled = false;
            CurrentPriority = 0;

            // update items.
            lock (_cache)
            {
                foreach (var monitoredItem in _monitoredItems.Values)
                {
                    monitoredItem.Reset();
                }
            }
            _deletedItems.Clear();
        }

        /// <summary>
        /// Processes all incoming messages and dispatch them.
        /// </summary>
        /// <param name="ct"></param>
        private async Task ProcessMessagesAsync(CancellationToken ct)
        {
            // This can be optimized to peek when missing sequence number and not ins
            // available sequence number als to support batching. TODO
            // This also needs to guard against overruns using some form of semaphore
            // To block the publisher. Unless we get https://github.com/dotnet/runtime/issues/101292
            try
            {
                await foreach (var incoming in _messages.Reader.ReadAllAsync(ct).ConfigureAwait(false))
                {
                    var prevSeqNum = _lastSequenceNumberProcessed;
                    var curSeqNum = incoming.Message.SequenceNumber;
                    if (prevSeqNum != 0)
                    {
                        for (var missing = prevSeqNum + 1; missing < curSeqNum; missing++)
                        {
                            // Try to republish missing messages from retransmission queue
                            await TryRepublishAsync(missing, curSeqNum, ct).ConfigureAwait(false);
                        }
                        if (prevSeqNum >= curSeqNum)
                        {
                            // Can occur if we republished a message
                            if (!_logger.IsEnabled(LogLevel.Debug))
                            {
                                continue;
                            }
                            if (curSeqNum == prevSeqNum)
                            {
                                _logger.LogDebug("{Subscription}: Received duplicate message " +
                                    "with sequence number #{SeqNumber}.", this, curSeqNum);
                            }
                            else
                            {
                                _logger.LogDebug("{Subscription}: Received older message with " +
                                    "sequence number #{SeqNumber} but already processed message with " +
                                    "sequence number #{Old}.", this, curSeqNum, prevSeqNum);
                            }
                            continue;
                        }
                    }
                    _lastSequenceNumberProcessed = curSeqNum;
                    await OnNotificationReceivedAsync(incoming.Message, PublishState.None,
                        ct).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger.LogCritical(ex,
                    "{Subscription}: Error processing messages. Processor is exiting!!!", this);
                OnPublishStateChanged(PublishState.Stopped);

                // Do not call complete here as we do not want the subscription to be removed
                throw;
            }
            await _completion.CompleteAsync(this, default).ConfigureAwait(false);

            async Task TryRepublishAsync(uint missing, uint curSeqNum, CancellationToken ct)
            {
                if (_availableInRetransmissionQueue.Contains(missing))
                {
                    _logger.LogWarning("{Subscription}: Message with sequence number " +
                        "#{SeqNumber} is not in server retransmission queue and was dropped.",
                        this, missing);
                    return;
                }
                _logger.LogInformation("{Subscription}: Republishing missing message " +
                    "with sequence number #{Missing} to catch up to message " +
                    "with sequence number #{SeqNumber}...", this, missing, curSeqNum);
                var republish = await Session.RepublishAsync(null, Id, missing,
                    ct).ConfigureAwait(false);

                if (ServiceResult.IsGood(republish.ResponseHeader.ServiceResult))
                {
                    await OnNotificationReceivedAsync(republish.NotificationMessage,
                        PublishState.Republish, ct).ConfigureAwait(false);
                }
                else
                {
                    _logger.LogWarning("{Subscription}: Republishing message with " +
                        "sequence number #{SeqNumber} failed.", this, missing);
                }
            }
        }

        /// <summary>
        /// Dispatch notification message
        /// </summary>
        /// <param name="message"></param>
        /// <param name="publishStateMask"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async ValueTask OnNotificationReceivedAsync(NotificationMessage message,
            PublishState publishStateMask, CancellationToken ct)
        {
            try
            {
                if (message.NotificationData.Count == 0)
                {
                    publishStateMask |= PublishState.KeepAlive;
                    await OnKeepAliveNotificationAsync(message.SequenceNumber,
                        message.PublishTime, publishStateMask).ConfigureAwait(false);
                }
                else
                {
#if PARALLEL_DISPATCH
                    await Task.WhenAll(message.NotificationData.Select(
                        notificationData => DispatchAsync(message, publishStateMask,
                            notificationData))).ConfigureAwait(false);
#else
                    foreach (var notificationData in message.NotificationData)
                    {
                        await DispatchAsync(message, publishStateMask,
                            notificationData).ConfigureAwait(false);
                    }
#endif
                }
                await _completion.QueueAsync(
                    new SubscriptionAcknowledgement
                    {
                        SequenceNumber = message.SequenceNumber,
                        SubscriptionId = Id
                    }, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "{Subscription}: Error dispatching notification data.", this);
            }

            async Task DispatchAsync(NotificationMessage message,
                PublishState publishStateMask, ExtensionObject? notificationData)
            {
                if (notificationData == null)
                {
                    return;
                }
                switch (notificationData.Body)
                {
                    case DataChangeNotification datachange:
                        await OnDataChangeNotificationAsync(message.SequenceNumber,
                            message.PublishTime, datachange, publishStateMask,
                            message.StringTable).ConfigureAwait(false);
                        break;
                    case EventNotificationList events:
                        await OnEventDataNotificationAsync(message.SequenceNumber,
                            message.PublishTime, events, publishStateMask,
                            message.StringTable).ConfigureAwait(false);
                        break;
                    case StatusChangeNotification statusChanged:
                        var mask = publishStateMask;
                        if (statusChanged.Status ==
                            StatusCodes.GoodSubscriptionTransferred)
                        {
                            mask |= PublishState.Transferred;
                        }
                        else if (statusChanged.Status == StatusCodes.BadTimeout)
                        {
                            mask |= PublishState.Timeout;
                        }
                        await OnStatusChangeNotificationAsync(message.SequenceNumber,
                            message.PublishTime, statusChanged, mask,
                            message.StringTable).ConfigureAwait(false);
                        break;
                }
            }
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
                    MinLifetimeInterval < Session.SessionTimeout)
                {
                    _logger.LogWarning(
                        "{Subscription}: A smaller minimum LifetimeInterval " +
                        "{Counter}ms than session timeout {Timeout}ms configured.",
                        this, MinLifetimeInterval, Session.SessionTimeout);
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

                if (lifetimeCount * PublishingInterval < Session.SessionTimeout)
                {
                    _logger.LogWarning(
                        "{Subscription}: Lifetime {LifeTime}ms configured is less " +
                        "than session timeout {Timeout}ms.",
                        this, lifetimeCount * PublishingInterval, Session.SessionTimeout);
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

        /// <summary>
        /// A message received from the server cached until is processed or discarded.
        /// </summary>
        /// <param name="Message"></param>
        /// <param name="StringTable"></param>
        /// <param name="Enqueued"></param>
        private sealed record class IncomingMessage(
            NotificationMessage Message, IReadOnlyList<string> StringTable, DateTime Enqueued)
            : IComparable<IncomingMessage>
        {
            /// <inheritdoc/>
            public int CompareTo(IncomingMessage? other)
            {
                // Greater than zero – This instance follows the next message in the sort order.
                return (int)(Message.SequenceNumber -
                    (other?.Message.SequenceNumber ?? uint.MinValue));
            }
        }

        private IReadOnlyList<uint> _availableInRetransmissionQueue = Array.Empty<uint>();
        private const int kMinKeepAliveTimerInterval = 1000;
        private const int kKeepAliveTimerMargin = 1000;
        private int _lastNotificationTickCount;
        private int _keepAliveInterval;
        private int _publishLateCount;
        private uint _lastSequenceNumberProcessed;
        private readonly ILogger _logger;
        private readonly IAckQueue _completion;
        private readonly List<uint> _deletedItems = new();
        private readonly Timer _publishTimer;
        private readonly object _cache = new();
        private readonly CancellationTokenSource _cts = new();
        private readonly Task _messageWorkerTask;
        private readonly Channel<IncomingMessage> _messages;
        private readonly Dictionary<uint, MonitoredItem> _monitoredItems = new();
    }
}
