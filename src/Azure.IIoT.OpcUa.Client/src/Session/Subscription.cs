/* ========================================================================
 * Copyright (c) 2005-2021 The OPC Foundation, Inc. All rights reserved.
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

namespace Opc.Ua.Client
{
    using Opc.Ua.Types.Utils;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// The delegate used to receive data change notifications via a direct function call instead of a .NET Event.
    /// </summary>
    /// <param name="subscription"></param>
    /// <param name="notification"></param>
    /// <param name="stringTable"></param>
    public delegate void FastDataChangeNotificationEventHandler(
        Subscription subscription, DataChangeNotification notification,
        IList<string> stringTable);

    /// <summary>
    /// The delegate used to receive event notifications via a
    /// direct function call instead of a .NET Event.
    /// </summary>
    /// <param name="subscription"></param>
    /// <param name="notification"></param>
    /// <param name="stringTable"></param>
    public delegate void FastEventNotificationEventHandler(
        Subscription subscription, EventNotificationList notification,
        IList<string> stringTable);

    /// <summary>
    /// The delegate used to receive keep alive notifications via
    /// a direct function call instead of a .NET Event.
    /// </summary>
    /// <param name="subscription"></param>
    /// <param name="notification"></param>
    public delegate void FastKeepAliveNotificationEventHandler(
        Subscription subscription, NotificationData notification);

    /// <summary>
    /// The delegate used to receive subscription state change notifications.
    /// </summary>
    /// <param name="subscription"></param>
    /// <param name="e"></param>
    public delegate void SubscriptionStateChangedEventHandler(
        Subscription subscription, SubscriptionStateChangedEventArgs e);

    /// <summary>
    /// The delegate used to receive publish state change notifications.
    /// </summary>
    /// <param name="subscription"></param>
    /// <param name="e"></param>
    public delegate void PublishStateChangedEventHandler(
        Subscription subscription, PublishStateChangedEventArgs e);

    /// <summary>
    /// A subscription.
    /// </summary>
    public abstract class Subscription : IDisposable, ICloneable
    {
        /// <summary>
        /// A display name for the subscription.
        /// </summary>
        [DataMember(Order = 1)]
        public string DisplayName { get; set; }

        /// <summary>
        /// The publishing interval.
        /// </summary>
        [DataMember(Order = 2)]
        public int PublishingInterval { get; set; }

        /// <summary>
        /// The keep alive count.
        /// </summary>
        [DataMember(Order = 3)]
        public uint KeepAliveCount { get; set; }

        /// <summary>
        /// The life time of the subscription in counts of
        /// publish interval.
        /// LifetimeCount shall be at least 3*KeepAliveCount.
        /// </summary>
        [DataMember(Order = 4)]
        public uint LifetimeCount { get; set; }

        /// <summary>
        /// The maximum number of notifications per publish request.
        /// </summary>
        [DataMember(Order = 5)]
        public uint MaxNotificationsPerPublish { get; set; }

        /// <summary>
        /// Whether publishing is enabled.
        /// </summary>
        [DataMember(Order = 6)]
        public bool PublishingEnabled { get; set; }

        /// <summary>
        /// The priority assigned to subscription.
        /// </summary>
        [DataMember(Order = 7)]
        public byte Priority { get; set; }

        /// <summary>
        /// The timestamps to return with the notification messages.
        /// </summary>
        [DataMember(Order = 8)]
        public TimestampsToReturn TimestampsToReturn { get; set; }

        /// <summary>
        /// The default monitored item.
        /// </summary>
        [DataMember(Order = 10)]
        public MonitoredItem DefaultItem { get; set; }

        /// <summary>
        /// The minimum lifetime for subscriptions in milliseconds.
        /// </summary>
        [DataMember(Order = 12)]
        public uint MinLifetimeInterval { get; set; }

        /// <summary>
        /// Gets or sets the behavior of waiting for sequential
        /// order in handling incoming messages.
        /// </summary>
        /// <value>
        /// <c>true</c> if incoming messages are handled
        /// sequentially; <c>false</c> otherwise.
        /// </value>
        /// <remarks>
        /// Setting <see cref="SequentialPublishing"/> to <c>true</c>
        /// means incoming messages are processed in a "single-threaded"
        /// manner and callbacks will not be invoked in parallel.
        /// </remarks>
        [DataMember(Order = 14)]
        public bool SequentialPublishing
        {
            get => _sequentialPublishing;
            set
            {
                // synchronize with message list processing
                lock (_cache)
                {
                    _sequentialPublishing = value;
                }
            }
        }

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
        [DataMember(Name = "RepublishAfterTransfer", Order = 15)]
        public bool RepublishAfterTransfer { get; set; }

        /// <summary>
        /// The unique identifier assigned by the server which can be used to transfer a session.
        /// </summary>
        [DataMember(Name = "TransferId", Order = 16)]
        public uint TransferId { get; set; }

        /// <summary>
        /// The current publishing interval.
        /// </summary>
        [DataMember(Name = "CurrentPublishInterval", Order = 20)]
        public double CurrentPublishingInterval { get; set; }

        /// <summary>
        /// The current keep alive count.
        /// </summary>
        [DataMember(Name = "CurrentKeepAliveCount", Order = 21)]
        public uint CurrentKeepAliveCount { get; set; }

        /// <summary>
        /// The current lifetime count.
        /// </summary>
        [DataMember(Name = "CurrentLifetimeCount", Order = 22)]
        public uint CurrentLifetimeCount { get; set; }

        /// <summary>
        /// Raised to indicate that the state of the subscription has changed.
        /// </summary>
        public event SubscriptionStateChangedEventHandler StateChanged
        {
            add => _StateChanged += value;
            remove => _StateChanged -= value;
        }

        /// <summary>
        /// Raised to indicate the publishing state for the subscription
        /// has stopped or resumed (see PublishingStopped property).
        /// </summary>
        public event PublishStateChangedEventHandler PublishStatusChanged
        {
            add => _publishStatusChanged += value;
            remove => _publishStatusChanged -= value;
        }

        /// <summary>
        /// Gets or sets the fast data change callback.
        /// </summary>
        /// <value>The fast data change callback.</value>
        /// <remarks>
        /// Only one callback is allowed at a time but it is more
        /// efficient to call than an event.
        /// </remarks>
        public FastDataChangeNotificationEventHandler? FastDataChangeCallback { get; set; }

        /// <summary>
        /// Gets or sets the fast event callback.
        /// </summary>
        /// <value>The fast event callback.</value>
        /// <remarks>
        /// Only one callback is allowed at a time but it is more
        /// efficient to call than an event.
        /// </remarks>
        public FastEventNotificationEventHandler? FastEventCallback { get; set; }

        /// <summary>
        /// Gets or sets the fast keep alive callback.
        /// </summary>
        /// <value>The keep alive change callback.</value>
        /// <remarks>
        /// Only one callback is allowed at a time but it is more
        /// efficient to call than an event.
        /// </remarks>
        public FastKeepAliveNotificationEventHandler? FastKeepAliveCallback { get; set; }

        /// <summary>
        /// Back compat
        /// </summary>
        public static bool DisableMonitoredItemCache { get => true; set { } }

        /// <summary>
        /// The items to monitor.
        /// </summary>
        public IEnumerable<MonitoredItem> MonitoredItems
        {
            get
            {
                lock (_cache)
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
                        if (Created && !monitoredItem.Status.Created)
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
        public ISessionInternal? Session { get; protected internal set; }

        /// <summary>
        /// A local handle assigned to the subscription
        /// </summary>
        public object? Handle { get; set; }

        /// <summary>
        /// The unique identifier assigned by the server.
        /// </summary>
        public uint Id { get; private set; }

        /// <summary>
        /// Whether the subscription has been created on the server.
        /// </summary>
        public bool Created => Id != 0;

        /// <summary>
        /// Whether publishing is currently enabled.
        /// </summary>
        public bool CurrentPublishingEnabled { get; private set; }

        /// <summary>
        /// Create subscription
        /// </summary>
        /// <param name="logger"></param>
        protected Subscription(ILogger logger)
        {
            _logger = logger;
            DisplayName = "Subscription";
            TimestampsToReturn = TimestampsToReturn.Both;
            _maxMessageCount = 10;
            RepublishAfterTransfer = false;
            _outstandingMessageWorkers = 0;
            _sequentialPublishing = false;
            _lastSequenceNumberProcessed = 0;
            _messageCache = new LinkedList<NotificationMessage>();
            _monitoredItems = new SortedDictionary<uint, MonitoredItem>();
            _deletedItems = new List<MonitoredItem>();
            _messageWorkerEvent = new AsyncAutoResetEvent();
            _resyncLastSequenceNumberProcessed = false;

            DefaultItem = new MonitoredItem
            {
                DisplayName = "MonitoredItem",
                SamplingInterval = -1,
                MonitoringMode = MonitoringMode.Reporting,
                QueueSize = 0,
                DiscardOldest = true
            };
        }

        /// <summary>
        /// Initializes the subscription from a template.
        /// </summary>
        /// <param name="template">The template.</param>
        /// <param name="copyEventHandlers">if set to <c>true</c>
        /// the event handlers are copied.</param>
        protected Subscription(Subscription template, bool copyEventHandlers)
        {
            TimestampsToReturn = TimestampsToReturn.Both;
            _maxMessageCount = 10;
            _messageCache = new LinkedList<NotificationMessage>();
            _monitoredItems = new SortedDictionary<uint, MonitoredItem>();
            _deletedItems = new List<MonitoredItem>();
            _messageWorkerEvent = new AsyncAutoResetEvent();

            DefaultItem = new MonitoredItem
            {
                DisplayName = "MonitoredItem",
                SamplingInterval = -1,
                MonitoringMode = MonitoringMode.Reporting,
                QueueSize = 0,
                DiscardOldest = true
            };

            Debug.Assert(template != null);

            _logger = template._logger;
            _maxMessageCount = template._maxMessageCount;
            _sequentialPublishing = template._sequentialPublishing;

            DisplayName = template.DisplayName;
            PublishingInterval = template.PublishingInterval;
            KeepAliveCount = template.KeepAliveCount;
            LifetimeCount = template.LifetimeCount;
            MinLifetimeInterval = template.MinLifetimeInterval;
            MaxNotificationsPerPublish = template.MaxNotificationsPerPublish;
            PublishingEnabled = template.PublishingEnabled;
            Priority = template.Priority;
            TimestampsToReturn = template.TimestampsToReturn;
            RepublishAfterTransfer = template.RepublishAfterTransfer;
            DefaultItem = (MonitoredItem)template.DefaultItem.Clone();
            Handle = template.Handle;
            TransferId = template.TransferId;

            if (copyEventHandlers)
            {
                _StateChanged = template._StateChanged;
                _publishStatusChanged = template._publishStatusChanged;
                FastDataChangeCallback = template.FastDataChangeCallback;
                FastEventCallback = template.FastEventCallback;
                FastKeepAliveCallback = template.FastKeepAliveCallback;
            }

            // copy the list of monitored items.
            var clonedMonitoredItems = new List<MonitoredItem>();
            foreach (var monitoredItem in template.MonitoredItems)
            {
                var clone = monitoredItem.CloneMonitoredItem(copyEventHandlers, true);
                clone.DisplayName = monitoredItem.DisplayName;
                clonedMonitoredItems.Add(clone);
            }
            if (clonedMonitoredItems.Count > 0)
            {
                AddItems(clonedMonitoredItems);
            }
        }

        /// <summary>
        /// Resets the state of the publish timer and associated message worker.
        /// </summary>
        private void ResetPublishTimerAndWorkerState()
        {
            // stop the publish timer.
            _publishTimer?.Dispose();
            _publishTimer = null;
            _messageWorkerCts?.Dispose();
            _messageWorkerEvent.Set();
            _messageWorkerCts = null;
            _messageWorkerTask = null;
        }

        /// <summary>
        /// Frees any unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// An overrideable version of the Dispose.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _publishTimer?.Dispose();
                _publishTimer = null;
                _messageWorkerCts?.Dispose();
                _messageWorkerCts = null;
                _messageWorkerEvent.Set();
                _messageWorkerTask = null;
            }
        }

        /// <inheritdoc/>
        public virtual object Clone()
        {
            return MemberwiseClone();
        }

        /// <summary>
        /// Clones a subscription or a subclass with an option to copy event handlers.
        /// </summary>
        /// <param name="copyEventHandlers"></param>
        /// <returns>A cloned instance of the subscription or its subclass.</returns>
        public abstract Subscription CloneSubscription(bool copyEventHandlers);

        /// <summary>
        /// Sends a notification that the state of the subscription has changed.
        /// </summary>
        public void ChangesCompleted()
        {
            _StateChanged?.Invoke(this, new SubscriptionStateChangedEventArgs(_changeMask));
            _changeMask = SubscriptionChangeMask.None;
        }

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
        /// Creates a subscription on the server and adds all monitored items.
        /// </summary>
        /// <param name="ct"></param>
        public async Task CreateAsync(CancellationToken ct)
        {
            VerifySubscriptionState(false);

            // create the subscription.
            var revisedMaxKeepAliveCount = KeepAliveCount;
            var revisedLifetimeCount = LifetimeCount;

            AdjustCounts(ref revisedMaxKeepAliveCount, ref revisedLifetimeCount);

            Debug.Assert(Session != null);
            var response = await Session.CreateSubscriptionAsync(
                null,
                PublishingInterval,
                revisedLifetimeCount,
                revisedMaxKeepAliveCount,
                MaxNotificationsPerPublish,
                PublishingEnabled,
                Priority,
                ct).ConfigureAwait(false);

            CreateSubscription(
                response.SubscriptionId,
                response.RevisedPublishingInterval,
                response.RevisedMaxKeepAliveCount,
                response.RevisedLifetimeCount);

            await CreateItemsAsync(ct).ConfigureAwait(false);

            ChangesCompleted();
        }

        /// <summary>
        /// Deletes a subscription on the server.
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
            Debug.Assert(Session != null);
            try
            {
                lock (_cache)
                {
                    ResetPublishTimerAndWorkerState();
                }

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
            // always put object in disconnected state even if an error occurs.
            finally
            {
                DeleteSubscription();
            }

            ChangesCompleted();
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

            Debug.Assert(Session != null);
            var response = await Session.ModifySubscriptionAsync(null, Id,
                PublishingInterval, revisedLifetimeCounter, revisedKeepAliveCount,
                MaxNotificationsPerPublish, Priority, ct).ConfigureAwait(false);

            // update current state.
            ModifySubscription(
                response.RevisedPublishingInterval,
                response.RevisedMaxKeepAliveCount,
                response.RevisedLifetimeCount);

            ChangesCompleted();
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

            Debug.Assert(Session != null);
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
            _changeMask |= SubscriptionChangeMask.Modified;

            ChangesCompleted();
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
        /// Creates all items on the server that have not already been created.
        /// </summary>
        /// <param name="ct"></param>
        public async Task<IList<MonitoredItem>> CreateItemsAsync(CancellationToken ct)
        {
            var requestItems = PrepareItemsToCreate(out var itemsToCreate);
            if (requestItems.Count == 0)
            {
                return itemsToCreate;
            }

            // create monitored items.
            Debug.Assert(Session != null);
            var response = await Session.CreateMonitoredItemsAsync(
                null,
                Id,
                TimestampsToReturn,
                requestItems,
                ct).ConfigureAwait(false);

            var results = response.Results;
            ClientBase.ValidateResponse(results, itemsToCreate);
            ClientBase.ValidateDiagnosticInfos(response.DiagnosticInfos, itemsToCreate);

            // update results.
            for (var index = 0; index < results.Count; index++)
            {
                itemsToCreate[index].SetCreateResult(requestItems[index],
                    results[index], index, response.DiagnosticInfos, response.ResponseHeader);
            }

            _changeMask |= SubscriptionChangeMask.ItemsCreated;
            ChangesCompleted();

            // return the list of items affected by the change.
            return itemsToCreate;
        }

        /// <summary>
        /// Modifies all items that have been changed.
        /// </summary>
        /// <param name="ct"></param>
        public async Task<IList<MonitoredItem>> ModifyItemsAsync(CancellationToken ct)
        {
            VerifySubscriptionState(true);

            var requestItems = new MonitoredItemModifyRequestCollection();
            var itemsToModify = new List<MonitoredItem>();

            PrepareItemsToModify(requestItems, itemsToModify);

            if (requestItems.Count == 0)
            {
                return itemsToModify;
            }

            // modify the subscription.
            Debug.Assert(Session != null);
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

            _changeMask |= SubscriptionChangeMask.ItemsModified;
            ChangesCompleted();

            // return the list of items affected by the change.
            return itemsToModify;
        }

        /// <summary>
        /// Deletes all items that have been marked for deletion.
        /// </summary>
        /// <param name="ct"></param>
        public async Task<IList<MonitoredItem>> DeleteItemsAsync(CancellationToken ct)
        {
            VerifySubscriptionState(true);

            if (_deletedItems.Count == 0)
            {
                return new List<MonitoredItem>();
            }

            var itemsToDelete = _deletedItems;
            _deletedItems = new List<MonitoredItem>();

            var monitoredItemIds = new UInt32Collection();

            foreach (var monitoredItem in itemsToDelete)
            {
                monitoredItemIds.Add(monitoredItem.Status.Id);
            }

            Debug.Assert(Session != null);
            var response = await Session.DeleteMonitoredItemsAsync(null, Id,
                monitoredItemIds, ct).ConfigureAwait(false);

            var results = response.Results;
            ClientBase.ValidateResponse(results, monitoredItemIds);
            ClientBase.ValidateDiagnosticInfos(response.DiagnosticInfos, monitoredItemIds);

            // update results.
            for (var index = 0; index < results.Count; index++)
            {
                itemsToDelete[index].SetDeleteResult(results[index], index,
                    response.DiagnosticInfos, response.ResponseHeader);
            }

            _changeMask |= SubscriptionChangeMask.ItemsDeleted;
            ChangesCompleted();

            // return the list of items affected by the change.
            return itemsToDelete;
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
                monitoredItemIds.Add(monitoredItem.Status.Id);
            }

            Debug.Assert(Session != null);
            var response = await Session.SetMonitoringModeAsync(null, Id,
                monitoringMode, monitoredItemIds, ct).ConfigureAwait(false);
            var results = response.Results;
            ClientBase.ValidateResponse(results, monitoredItemIds);
            ClientBase.ValidateDiagnosticInfos(response.DiagnosticInfos, monitoredItemIds);

            // update results.
            var errors = new List<ServiceResult>();
            var noErrors = UpdateMonitoringMode(monitoredItems, errors, results,
                response.DiagnosticInfos, response.ResponseHeader, monitoringMode);

            // raise state changed event.
            _changeMask |= SubscriptionChangeMask.ItemsModified;
            ChangesCompleted();

            // return empty list if no errors occurred.
            if (noErrors)
            {
                return Array.Empty<ServiceResult>();
            }
            return errors;
        }

        /// <summary>
        /// Tells the server to refresh all conditions being monitored by the subscription.
        /// </summary>
        /// <param name="ct"></param>
        public async Task ConditionRefreshAsync(CancellationToken ct)
        {
            VerifySubscriptionState(true);
            var methodsToCall = new CallMethodRequestCollection();
            methodsToCall.Add(new CallMethodRequest()
            {
                MethodId = MethodIds.ConditionType_ConditionRefresh,
                InputArguments = new VariantCollection() { new Variant(Id) }
            });

            Debug.Assert(Session != null);
            var response = await Session.CallAsync(null, methodsToCall,
                ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Called after the subscription was transferred.
        /// </summary>
        /// <param name="session">The session to which the subscription is
        /// transferred.</param>
        /// <param name="id">Id of the transferred subscription.</param>
        /// <param name="availableSequenceNumbers">The available sequence
        /// numbers on the server.</param>
        /// <param name="ct">The cancellation token.</param>
        internal async Task<bool> TransferAsync(ISessionInternal session, uint id,
            UInt32Collection availableSequenceNumbers, CancellationToken ct)
        {
            if (Created)
            {
                // handle the case when the client has the subscription
                // template and reconnects
                if (id != Id)
                {
                    return false;
                }

                // remove the subscription from disconnected session
                if (Session?.RemoveTransferredSubscription(this) != true)
                {
                    _logger.LogError("SubscriptionId {Id}: Failed to remove transferred " +
                        "subscription from owner SessionId={SessionId}.", Id,
                        Session?.SessionId);
                    return false;
                }

                // remove default subscription template which was copied in Session.Create()
                var subscriptionsToRemove = session.Subscriptions
                    .Where(s => !s.Created && s.TransferId == Id)
                    .ToList();
                await session.RemoveSubscriptionsAsync(subscriptionsToRemove,
                    ct).ConfigureAwait(false);

                // add transferred subscription to session
                if (!session.AddSubscription(this))
                {
                    _logger.LogError("SubscriptionId {Id}: Failed to add transferred " +
                        "subscription to SessionId={SessionId}.", Id, session.SessionId);
                    return false;
                }
            }
            else
            {
                // handle the case when the client restarts and loads the saved
                // subscriptions from storage
                var (success, serverHandles, clientHandles) =
                    await GetMonitoredItemsAsync(ct).ConfigureAwait(false);
                if (!success)
                {
                    _logger.LogError("SubscriptionId {Id}: The server failed to respond " +
                        "to GetMonitoredItems after transfer.", Id);
                    return false;
                }

                var monitoredItemsCount = _monitoredItems.Count;
                if (serverHandles.Count != monitoredItemsCount ||
                    clientHandles.Count != monitoredItemsCount)
                {
                    // invalid state
                    _logger.LogError("SubscriptionId {Id}: Number of Monitored Items" +
                        " on client and server do not match after transfer {Old}!={New}",
                        Id, serverHandles.Count, monitoredItemsCount);
                    return false;
                }

                // sets state to 'Created'
                Id = id;
                TransferItems(serverHandles, clientHandles, out var itemsToModify);

                await ModifyItemsAsync(ct).ConfigureAwait(false);
            }

            // add available sequence numbers to incoming
            ProcessTransferredSequenceNumbers(availableSequenceNumbers);

            _changeMask |= SubscriptionChangeMask.Transferred;
            ChangesCompleted();
            StartKeepAliveTimer();
            return true;
        }

        /// <summary>
        /// Adds the notification message to internal cache.
        /// </summary>
        /// <param name="availableSequenceNumbers"></param>
        /// <param name="message"></param>
        /// <param name="stringTable"></param>
        public void SaveMessageInCache(IList<uint> availableSequenceNumbers,
            NotificationMessage message, IList<string> stringTable)
        {
            PublishStateChangedEventHandler? callback = null;
            lock (_cache)
            {
                if (availableSequenceNumbers != null)
                {
                    _availableSequenceNumbers = availableSequenceNumbers;
                }

                if (message == null)
                {
                    return;
                }

                // check if a publish error was previously reported.
                if (PublishingStopped)
                {
                    callback = _publishStatusChanged;
                }

                var now = DateTime.UtcNow;
                Interlocked.Exchange(ref _lastNotificationTime, now.Ticks);
                var tickCount = HiResClock.TickCount;
                _lastNotificationTickCount = tickCount;

                // save the string table that came with notification.
                message.StringTable = new List<string>(stringTable);

                // find or create an entry for the incoming sequence number.
                var entry = FindOrCreateEntry(now, tickCount, message.SequenceNumber);

                // check for keep alive.
                if (message.NotificationData.Count > 0)
                {
                    entry.Message = message;
                    entry.Processed = false;
                }

                // fill in any gaps in the queue
                var node = _incomingMessages.First;

                while (node != null)
                {
                    entry = node.Value;
                    var next = node.Next;

                    if (next != null &&
                        next.Value.SequenceNumber > entry.SequenceNumber + 1)
                    {
                        var placeholder = new IncomingMessage();
                        placeholder.SequenceNumber = entry.SequenceNumber + 1;
                        placeholder.Timestamp = now;
                        placeholder.TickCount = tickCount;
                        node = _incomingMessages.AddAfter(node, placeholder);
                        continue;
                    }

                    node = next;
                }

                // clean out processed values.
                node = _incomingMessages.First;

                while (node != null)
                {
                    entry = node.Value;
                    var next = node.Next;

                    // can only pull off processed or expired or missing messages.
                    if (!entry.Processed && !(entry.Republished &&
                       (entry.RepublishStatus != StatusCodes.Good ||
                            (tickCount - entry.TickCount) > kRepublishMessageExpiredTimeout)))
                    {
                        break;
                    }

                    if (next != null)
                    {
                        //If the message being removed is supposed to be the next message,
                        //advance it to release anything waiting on it to be processed
                        if (entry.SequenceNumber == _lastSequenceNumberProcessed + 1)
                        {
                            if (!entry.Processed)
                            {
                                _logger.LogWarning("SubscriptionId {Id} skipping PublishResponse " +
                                    "Sequence Number {SeqNumber}", Id, entry.SequenceNumber);
                            }

                            _lastSequenceNumberProcessed = entry.SequenceNumber;
                        }

                        _incomingMessages.Remove(node);
                    }

                    node = next;
                }
            }

            // send notification that publishing received a keep alive or has to republish.
            if (callback != null)
            {
                try
                {
                    callback(this,
                        new PublishStateChangedEventArgs(PublishStateChangedMask.Recovered));
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error while raising PublishStateChanged event.");
                }
            }

            // process messages.
            _messageWorkerEvent.Set();
        }

        /// <summary>
        /// Adds an item to the subscription.
        /// </summary>
        /// <param name="monitoredItem"></param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="monitoredItem"/> is <c>null</c>.</exception>
        public void AddItem(MonitoredItem monitoredItem)
        {
            ArgumentNullException.ThrowIfNull(monitoredItem);

            lock (_cache)
            {
                if (_monitoredItems.ContainsKey(monitoredItem.ClientHandle))
                {
                    return;
                }

                _monitoredItems.Add(monitoredItem.ClientHandle, monitoredItem);
                monitoredItem.Subscription = this;
            }

            _changeMask |= SubscriptionChangeMask.ItemsAdded;
            ChangesCompleted();
        }

        /// <summary>
        /// Adds items to the subscription.
        /// </summary>
        /// <param name="monitoredItems"></param>
        /// <exception cref="ArgumentNullException"><paramref name="monitoredItems"/> is <c>null</c>.</exception>
        public void AddItems(IEnumerable<MonitoredItem> monitoredItems)
        {
            ArgumentNullException.ThrowIfNull(monitoredItems);

            var added = false;

            lock (_cache)
            {
                foreach (var monitoredItem in monitoredItems)
                {
                    if (!_monitoredItems.ContainsKey(monitoredItem.ClientHandle))
                    {
                        _monitoredItems.Add(monitoredItem.ClientHandle, monitoredItem);
                        monitoredItem.Subscription = this;
                        added = true;
                    }
                }
            }

            if (added)
            {
                _changeMask |= SubscriptionChangeMask.ItemsAdded;
                ChangesCompleted();
            }
        }

        /// <summary>
        /// Removes an item from the subscription.
        /// </summary>
        /// <param name="monitoredItem"></param>
        /// <exception cref="ArgumentNullException"><paramref name="monitoredItem"/> is <c>null</c>.</exception>
        public void RemoveItem(MonitoredItem monitoredItem)
        {
            ArgumentNullException.ThrowIfNull(monitoredItem);

            lock (_cache)
            {
                if (!_monitoredItems.Remove(monitoredItem.ClientHandle))
                {
                    return;
                }

                monitoredItem.Subscription = null;
            }

            if (monitoredItem.Status.Created)
            {
                _deletedItems.Add(monitoredItem);
            }

            _changeMask |= SubscriptionChangeMask.ItemsRemoved;
            ChangesCompleted();
        }

        /// <summary>
        /// Removes items from the subscription.
        /// </summary>
        /// <param name="monitoredItems"></param>
        /// <exception cref="ArgumentNullException"><paramref name="monitoredItems"/> is <c>null</c>.</exception>
        public void RemoveItems(IEnumerable<MonitoredItem> monitoredItems)
        {
            ArgumentNullException.ThrowIfNull(monitoredItems);

            var changed = false;

            lock (_cache)
            {
                foreach (var monitoredItem in monitoredItems)
                {
                    if (_monitoredItems.Remove(monitoredItem.ClientHandle))
                    {
                        monitoredItem.Subscription = null;

                        if (monitoredItem.Status.Created)
                        {
                            _deletedItems.Add(monitoredItem);
                        }

                        changed = true;
                    }
                }
            }

            if (changed)
            {
                _changeMask |= SubscriptionChangeMask.ItemsRemoved;
                ChangesCompleted();
            }
        }

        /// <summary>
        /// Returns the monitored item identified by the client handle.
        /// </summary>
        /// <param name="clientHandle"></param>
        public MonitoredItem? FindItemByClientHandle(uint clientHandle)
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
        /// Updates the available sequence numbers and queues after transfer.
        /// </summary>
        /// <remarks>
        /// If <see cref="RepublishAfterTransfer"/> is set to <c>true</c>, sequence numbers
        /// are queued for republish, otherwise ack may be sent.
        /// </remarks>
        /// <param name="availableSequenceNumbers">The list of available sequence numbers on the server.</param>
        private void ProcessTransferredSequenceNumbers(UInt32Collection availableSequenceNumbers)
        {
            lock (_cache)
            {
                // reset incoming state machine and clear cache
                _lastSequenceNumberProcessed = 0;
                _resyncLastSequenceNumberProcessed = true;
                _incomingMessages.Clear();

                // save available sequence numbers
                _availableSequenceNumbers = (UInt32Collection)availableSequenceNumbers.MemberwiseClone();

                if (availableSequenceNumbers.Count != 0 && RepublishAfterTransfer)
                {
                    // update last sequence number processed
                    // available seq numbers may not be in order
                    foreach (var sequenceNumber in availableSequenceNumbers)
                    {
                        if (sequenceNumber >= _lastSequenceNumberProcessed)
                        {
                            _lastSequenceNumberProcessed = sequenceNumber + 1;
                        }
                    }

                    // only republish consecutive sequence numbers
                    // triggers the republish mechanism immediately,
                    // if event is in the past
                    var now = DateTime.UtcNow.AddMilliseconds(-kRepublishMessageTimeout * 2);
                    var tickCount = HiResClock.TickCount - (kRepublishMessageTimeout * 2);
                    var lastSequenceNumberToRepublish = _lastSequenceNumberProcessed - 1;
                    var availableNumbers = availableSequenceNumbers.Count;
                    var republishMessages = 0;
                    for (var i = 0; i < availableNumbers; i++)
                    {
                        var found = false;
                        foreach (var sequenceNumber in availableSequenceNumbers)
                        {
                            if (lastSequenceNumberToRepublish == sequenceNumber)
                            {
                                FindOrCreateEntry(now, tickCount, sequenceNumber);
                                found = true;
                                break;
                            }
                        }

                        if (found)
                        {
                            // remove sequence number handled for republish
                            availableSequenceNumbers.Remove(lastSequenceNumberToRepublish);
                            lastSequenceNumberToRepublish--;
                            republishMessages++;
                        }
                        else
                        {
                            break;
                        }
                    }

                    _logger.LogInformation("SubscriptionId {Id}: Republishing {Messages} messages, next sequencenumber {SequenceNumber} after transfer.",
                        Id, republishMessages, _lastSequenceNumberProcessed);

                    availableSequenceNumbers.Clear();
                }
            }
        }

        /// <summary>
        /// Call the GetMonitoredItems method on the server.
        /// </summary>
        /// <param name="ct"></param>
        private async Task<(bool, UInt32Collection, UInt32Collection)> GetMonitoredItemsAsync(CancellationToken ct)
        {
            var serverHandles = new UInt32Collection();
            var clientHandles = new UInt32Collection();
            try
            {
                Debug.Assert(Session != null);
                var outputArguments = await Session.CallAsync(ObjectIds.Server,
                    MethodIds.Server_GetMonitoredItems, ct, TransferId).ConfigureAwait(false);
                if (outputArguments?.Count == 2)
                {
                    serverHandles.AddRange((uint[])outputArguments[0]);
                    clientHandles.AddRange((uint[])outputArguments[1]);
                    return (true, serverHandles, clientHandles);
                }
            }
            catch (ServiceResultException sre)
            {
                _logger.LogError(sre, "SubscriptionId {Id}: Failed to call GetMonitoredItems on server", Id);
            }
            return (false, serverHandles, clientHandles);
        }

        /// <summary>
        /// Starts a timer to ensure publish requests are sent frequently enough to detect
        /// network interruptions.
        /// </summary>
        private void StartKeepAliveTimer()
        {
            // stop the publish timer.
            lock (_cache)
            {
                _publishTimer?.Dispose();
                _publishTimer = null;

                Interlocked.Exchange(ref _lastNotificationTime, DateTime.UtcNow.Ticks);
                _lastNotificationTickCount = HiResClock.TickCount;
                _keepAliveInterval = (int)Math.Min(
                    CurrentPublishingInterval * (CurrentKeepAliveCount + 1), int.MaxValue);
                if (_keepAliveInterval < kMinKeepAliveTimerInterval)
                {
                    _keepAliveInterval = (int)Math.Min(
                        PublishingInterval * (KeepAliveCount + 1), int.MaxValue);
                    _keepAliveInterval = Math.Max(kMinKeepAliveTimerInterval, _keepAliveInterval);
                }
#if NET6_0_OR_GREATER
                var publishTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(_keepAliveInterval));
                _ = Task.Run(() => OnKeepAliveAsync(publishTimer));
                _publishTimer = publishTimer;
#else
                _publishTimer = new Timer(OnKeepAlive, _keepAliveInterval, _keepAliveInterval, _keepAliveInterval);
#endif

                if (_messageWorkerTask?.IsCompleted != false)
                {
                    _messageWorkerCts?.Dispose();
                    _messageWorkerCts = new CancellationTokenSource();
                    var ct = _messageWorkerCts.Token;
                    _messageWorkerTask = Task.Run(() => PublishResponseMessageWorkerAsync(ct));
                }
            }

            // start publishing. Fill the queue.
            Session?.StartPublishing(BeginPublishTimeout(), false);
        }

#if NET6_0_OR_GREATER
        /// <summary>
        /// Checks if a notification has arrived. Sends a publish if it has not.
        /// </summary>
        /// <param name="publishTimer"></param>
        private async Task OnKeepAliveAsync(PeriodicTimer publishTimer)
        {
            while (await publishTimer.WaitForNextTickAsync().ConfigureAwait(false))
            {
                if (!PublishingStopped)
                {
                    continue;
                }

                HandleOnKeepAliveStopped();
            }
        }
#else
        /// <summary>
        /// Checks if a notification has arrived. Sends a publish if it has not.
        /// </summary>
        private void OnKeepAlive(object state)
        {
            if (!PublishingStopped)
            {
                return;
            }

            HandleOnKeepAliveStopped();
        }
#endif

        /// <summary>
        /// Handles callback if publishing stopped. Sends a publish.
        /// </summary>
        private void HandleOnKeepAliveStopped()
        {
            // check if a publish has arrived.
            var callback = _publishStatusChanged;

            Interlocked.Increment(ref _publishLateCount);

            if (callback != null)
            {
                try
                {
                    callback(this, new PublishStateChangedEventArgs(PublishStateChangedMask.Stopped));
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error while raising PublishStateChanged event.");
                }
            }

            // try to send a publish to recover stopped publishing.
            Session?.BeginPublish(BeginPublishTimeout());
        }

        /// <summary>
        /// Publish response worker task for the subscriptions.
        /// </summary>
        /// <param name="ct"></param>
        private async Task PublishResponseMessageWorkerAsync(CancellationToken ct)
        {
            _logger.LogTrace("SubscriptionId {Id} - Publish Thread {Thread:X8} Started.", Id, Environment.CurrentManagedThreadId);

            bool cancelled;
            try
            {
                do
                {
                    await _messageWorkerEvent.WaitAsync().ConfigureAwait(false);

                    cancelled = ct.IsCancellationRequested;
                    if (!cancelled)
                    {
                        await OnMessageReceivedAsync(ct).ConfigureAwait(false);
                        cancelled = ct.IsCancellationRequested;
                    }
                }
                while (!cancelled);
            }
            catch (OperationCanceledException)
            {
                // intentionally fall through
            }
            catch (Exception e)
            {
                _logger.LogError(e, "SubscriptionId {Id} - Publish Worker Thread {Thread:X8} Exited Unexpectedly.", Id, Environment.CurrentManagedThreadId);
                return;
            }

            _logger.LogTrace("SubscriptionId {Id} - Publish Thread {Thread:X8} Exited Normally.", Id, Environment.CurrentManagedThreadId);
        }

        /// <summary>
        /// Calculate the timeout of a publish request.
        /// </summary>
        private int BeginPublishTimeout()
        {
            return Math.Max(Math.Min(_keepAliveInterval * 3, int.MaxValue), kMinKeepAliveTimerInterval);
        }

        /// <summary>
        /// Update the subscription with the given revised settings.
        /// </summary>
        /// <param name="revisedPublishingInterval"></param>
        /// <param name="revisedKeepAliveCount"></param>
        /// <param name="revisedLifetimeCounter"></param>
        private void ModifySubscription(
            double revisedPublishingInterval,
            uint revisedKeepAliveCount,
            uint revisedLifetimeCounter
            )
        {
            CreateOrModifySubscription(false, 0,
                revisedPublishingInterval, revisedKeepAliveCount, revisedLifetimeCounter);
        }

        /// <summary>
        /// Update the subscription with the given revised settings.
        /// </summary>
        /// <param name="subscriptionId"></param>
        /// <param name="revisedPublishingInterval"></param>
        /// <param name="revisedKeepAliveCount"></param>
        /// <param name="revisedLifetimeCounter"></param>
        private void CreateSubscription(
            uint subscriptionId,
            double revisedPublishingInterval,
            uint revisedKeepAliveCount,
            uint revisedLifetimeCounter
            )
        {
            CreateOrModifySubscription(true, subscriptionId,
                revisedPublishingInterval, revisedKeepAliveCount, revisedLifetimeCounter);
        }

        /// <summary>
        /// Update the subscription with the given revised settings.
        /// </summary>
        /// <param name="created"></param>
        /// <param name="subscriptionId"></param>
        /// <param name="revisedPublishingInterval"></param>
        /// <param name="revisedKeepAliveCount"></param>
        /// <param name="revisedLifetimeCounter"></param>
        private void CreateOrModifySubscription(
            bool created,
            uint subscriptionId,
            double revisedPublishingInterval,
            uint revisedKeepAliveCount,
            uint revisedLifetimeCounter
            )
        {
            // update current state.
            CurrentPublishingInterval = revisedPublishingInterval;
            CurrentKeepAliveCount = revisedKeepAliveCount;
            CurrentLifetimeCount = revisedLifetimeCounter;
            _currentPriority = Priority;

            if (!created)
            {
                _changeMask |= SubscriptionChangeMask.Modified;
            }
            else
            {
                CurrentPublishingEnabled = PublishingEnabled;
                TransferId = Id = subscriptionId;
                StartKeepAliveTimer();
                _changeMask |= SubscriptionChangeMask.Created;
            }

            if (KeepAliveCount != revisedKeepAliveCount)
            {
                _logger.LogInformation("For subscriptionId {Id}, Keep alive count was revised from {Old} to {New}",
                    Id, KeepAliveCount, revisedKeepAliveCount);
            }

            if (LifetimeCount != revisedLifetimeCounter)
            {
                _logger.LogInformation("For subscriptionId {Id}, Lifetime count was revised from {Old} to {New}",
                    Id, LifetimeCount, revisedLifetimeCounter);
            }

            if (PublishingInterval != revisedPublishingInterval)
            {
                _logger.LogInformation("For subscriptionId {Id}, Publishing interval was revised from {Old} to {New}",
                    Id, PublishingInterval, revisedPublishingInterval);
            }

            if (revisedLifetimeCounter < revisedKeepAliveCount * 3)
            {
                _logger.LogInformation("For subscriptionId {Id}, Revised lifetime counter (value={Lifetime}) is less than three times the keep alive count (value={KeepAlive})", Id, revisedLifetimeCounter, revisedKeepAliveCount);
            }

            if (_currentPriority == 0)
            {
                _logger.LogInformation("For subscriptionId {Id}, the priority was set to 0.", Id);
            }
        }

        /// <summary>
        /// Delete the subscription.
        /// Ignore errors, always reset all parameter.
        /// </summary>
        private void DeleteSubscription()
        {
            TransferId = Id = 0;
            CurrentPublishingInterval = 0;
            CurrentKeepAliveCount = 0;
            CurrentPublishingEnabled = false;
            _currentPriority = 0;

            // update items.
            lock (_cache)
            {
                var responseHeader = new ResponseHeader();
                var diagnosticInfo = new DiagnosticInfoCollection();
                foreach (var monitoredItem in _monitoredItems.Values)
                {
                    monitoredItem.SetDeleteResult(StatusCodes.Good, 0, diagnosticInfo, responseHeader);
                }
            }
            _deletedItems.Clear();
            _changeMask |= SubscriptionChangeMask.Deleted;
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
                _logger.LogInformation(
                    "Adjusted KeepAliveCount from value={Old}, to value={New}, for subscriptionId {Id}.",
                    keepAliveCount, kDefaultKeepAlive, Id);
                keepAliveCount = kDefaultKeepAlive;
            }

            // ensure the lifetime is sensible given the sampling interval.
            if (PublishingInterval > 0)
            {
                var session = Session;
                Debug.Assert(session != null);

                if (MinLifetimeInterval > 0 && MinLifetimeInterval < session.SessionTimeout)
                {
                    _logger.LogWarning("A smaller minLifetimeInterval {Counter}ms than session " +
                        "timeout {Timeout}ms configured for subscriptionId {Id}.",
                        MinLifetimeInterval, session.SessionTimeout, Id);
                }

                var minLifetimeCount = (uint)(MinLifetimeInterval / PublishingInterval);
                if (lifetimeCount < minLifetimeCount)
                {
                    lifetimeCount = minLifetimeCount;

                    if (MinLifetimeInterval % PublishingInterval != 0)
                    {
                        lifetimeCount++;
                    }

                    _logger.LogInformation(
                        "Adjusted LifetimeCount to value={New}, for subscriptionId {Id}. ",
                        lifetimeCount, Id);
                }

                if (lifetimeCount * PublishingInterval < session.SessionTimeout)
                {
                    _logger.LogWarning("Lifetime {LifeTime}ms configured for " +
                        "subscriptionId {Id} is less than session timeout {Timeout}ms.",
                        lifetimeCount * PublishingInterval, Id, session.SessionTimeout);
                }
            }
            else if (lifetimeCount == 0)
            {
                // don't know what the sampling interval will be - use something large enough
                // to ensure the user does not experience unexpected drop outs.
                _logger.LogInformation(
                    "Adjusted LifetimeCount from value={Old}, to value={New}, for subscriptionId {Id}. ",
                    lifetimeCount, kDefaultLifeTime, Id);
                lifetimeCount = kDefaultLifeTime;
            }

            // validate spec: lifetimecount shall be at least 3*keepAliveCount
            var minLifeTimeCount = 3 * keepAliveCount;
            if (lifetimeCount < minLifeTimeCount)
            {
                _logger.LogInformation(
                    "Adjusted LifetimeCount from value={Old}, to value={New}, for subscriptionId {Id}. ",
                    lifetimeCount, minLifeTimeCount, Id);
                lifetimeCount = minLifeTimeCount;
            }
        }

        /// <summary>
        /// Processes the incoming messages.
        /// </summary>
        /// <param name="ct"></param>
        private async Task OnMessageReceivedAsync(CancellationToken ct)
        {
            try
            {
                Interlocked.Increment(ref _outstandingMessageWorkers);

                ISessionInternal? session = null;
                uint subscriptionId = 0;
                PublishStateChangedEventHandler? callback = null;

                // list of new messages to process.
                List<NotificationMessage>? messagesToProcess = null;

                // list of keep alive messages to process.
                List<IncomingMessage>? keepAliveToProcess = null;

                // list of new messages to republish.
                List<IncomingMessage>? messagesToRepublish = null;

                var publishStateChangedMask = PublishStateChangedMask.None;

                lock (_cache)
                {
                    for (var llNode = _incomingMessages.First; llNode != null; llNode = llNode.Next)
                    {
                        // update monitored items with unprocessed messages.
                        if (llNode.Value.Message != null && !llNode.Value.Processed &&
                            (!_sequentialPublishing || ValidSequentialPublishMessage(llNode.Value)))
                        {
                            messagesToProcess ??= new List<NotificationMessage>();

                            messagesToProcess.Add(llNode.Value.Message);

                            // remove the oldest items.
                            while (_messageCache.Count > _maxMessageCount)
                            {
                                _messageCache.RemoveFirst();
                            }

                            _messageCache.AddLast(llNode.Value.Message);
                            llNode.Value.Processed = true;

                            // Keep the last sequence number processed going up
                            if (llNode.Value.SequenceNumber > _lastSequenceNumberProcessed ||
                               (llNode.Value.SequenceNumber == 1 && _lastSequenceNumberProcessed == uint.MaxValue))
                            {
                                _lastSequenceNumberProcessed = llNode.Value.SequenceNumber;
                                if (_resyncLastSequenceNumberProcessed)
                                {
                                    _logger.LogInformation("SubscriptionId {Id}: Resynced last sequence number processed to {LastSeqNumber}.",
                                        Id, _lastSequenceNumberProcessed);
                                    _resyncLastSequenceNumberProcessed = false;
                                }
                            }
                        }

                        // process keep alive messages
                        else if (llNode.Next == null && llNode.Value.Message == null && !llNode.Value.Processed)
                        {
                            keepAliveToProcess ??= new List<IncomingMessage>();
                            keepAliveToProcess.Add(llNode.Value);
                            publishStateChangedMask |= PublishStateChangedMask.KeepAlive;
                        }

                        // check for missing messages.
                        else if (llNode.Next != null && llNode.Value.Message == null && !llNode.Value.Processed && !llNode.Value.Republished)
                        {
                            // tolerate if a single request was received out of order
                            if (llNode.Next.Next != null &&
                                (HiResClock.TickCount - llNode.Value.TickCount) > kRepublishMessageTimeout)
                            {
                                llNode.Value.Republished = true;
                                publishStateChangedMask |= PublishStateChangedMask.Republish;

                                // only call republish if the sequence number is available
                                if (_availableSequenceNumbers?.Contains(llNode.Value.SequenceNumber) == true)
                                {
                                    messagesToRepublish ??= new List<IncomingMessage>();

                                    messagesToRepublish.Add(llNode.Value);
                                }
                                else
                                {
                                    _logger.LogInformation("Skipped to receive RepublishAsync for subscription {Id}-{SeqNumber}-BadMessageNotAvailable", subscriptionId, llNode.Value.SequenceNumber);
                                    llNode.Value.RepublishStatus = StatusCodes.BadMessageNotAvailable;
                                }
                            }
                        }
#if DEBUG
                        // a message that is deferred because of a missing sequence number
                        else if (llNode.Value.Message != null && !llNode.Value.Processed)
                        {
                            _logger.LogDebug("subscriptionId {Id}: Delayed message with sequence number {SeqNumber}, expected sequence number is {Expected}.",
                                Id, llNode.Value.SequenceNumber, _lastSequenceNumberProcessed + 1);
                        }
#endif
                    }

                    session = Session;
                    subscriptionId = Id;
                    callback = _publishStatusChanged;
                }

                // process new keep alive messages.
                var keepAliveCallback = FastKeepAliveCallback;
                if (keepAliveToProcess != null && keepAliveCallback != null)
                {
                    foreach (var message in keepAliveToProcess)
                    {
                        var keepAlive = new NotificationData
                        {
                            PublishTime = message.Timestamp,
                            SequenceNumber = message.SequenceNumber
                        };
                        keepAliveCallback(this, keepAlive);
                    }
                }

                // process new messages.
                if (messagesToProcess != null)
                {
                    int noNotificationsReceived;
                    var datachangeCallback = FastDataChangeCallback;
                    var eventCallback = FastEventCallback;

                    foreach (var message in messagesToProcess)
                    {
                        noNotificationsReceived = 0;
                        try
                        {
                            foreach (var notificationData in message.NotificationData)
                            {
                                if (notificationData.Body is DataChangeNotification datachange)
                                {
                                    datachange.PublishTime = message.PublishTime;
                                    datachange.SequenceNumber = message.SequenceNumber;

                                    noNotificationsReceived += datachange.MonitoredItems.Count;

                                    datachangeCallback?.Invoke(this, datachange, message.StringTable);
                                }

                                if (notificationData.Body is EventNotificationList events)
                                {
                                    events.PublishTime = message.PublishTime;
                                    events.SequenceNumber = message.SequenceNumber;

                                    noNotificationsReceived += events.Events.Count;

                                    eventCallback?.Invoke(this, events, message.StringTable);
                                }

                                if (notificationData.Body is StatusChangeNotification statusChanged)
                                {
                                    statusChanged.PublishTime = message.PublishTime;
                                    statusChanged.SequenceNumber = message.SequenceNumber;

                                    _logger.LogWarning("StatusChangeNotification received with Status = {Status} for SubscriptionId={Id}.",
                                        statusChanged.Status.ToString(), Id);

                                    if (statusChanged.Status == StatusCodes.GoodSubscriptionTransferred)
                                    {
                                        publishStateChangedMask |= PublishStateChangedMask.Transferred;
                                        ResetPublishTimerAndWorkerState();
                                    }
                                    else if (statusChanged.Status == StatusCodes.BadTimeout)
                                    {
                                        publishStateChangedMask |= PublishStateChangedMask.Timeout;
                                    }
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            _logger.LogError(e, "Error while processing incoming message #{SeqNumber}.",
                                message.SequenceNumber);
                        }

                        if (MaxNotificationsPerPublish != 0 && noNotificationsReceived > MaxNotificationsPerPublish)
                        {
                            _logger.LogWarning("For subscriptionId {Id}, more notifications were received={Received} " +
                                "than the max notifications per publish value={MaxNotifications}",
                                Id, noNotificationsReceived, MaxNotificationsPerPublish);
                        }
                    }
                    if ((callback != null) && (publishStateChangedMask != PublishStateChangedMask.None))
                    {
                        try
                        {
                            callback(this, new PublishStateChangedEventArgs(publishStateChangedMask));
                        }
                        catch (Exception e)
                        {
                            _logger.LogError(e, "Error while raising PublishStateChanged event.");
                        }
                    }
                }

                // do any re-publishes.
                if (messagesToRepublish != null && session != null && subscriptionId != 0)
                {
                    var count = messagesToRepublish.Count;
                    var tasks = new Task<(bool, ServiceResult)>[count];
                    for (var index = 0; index < count; index++)
                    {
                        tasks[index] = session.RepublishAsync(subscriptionId,
                            messagesToRepublish[index].SequenceNumber, ct);
                    }

                    var publishResults = await Task.WhenAll(tasks).ConfigureAwait(false);

                    lock (_cache)
                    {
                        for (var index = 0; index < count; index++)
                        {
                            var (success, serviceResult) = publishResults[index].ToTuple();

                            messagesToRepublish[index].Republished = success;
                            messagesToRepublish[index].RepublishStatus = serviceResult.StatusCode;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error while processing incoming messages.");
            }
            finally
            {
                Interlocked.Decrement(ref _outstandingMessageWorkers);
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

            if (!created && Session is null) // Occurs only on Create() and CreateAsync()
            {
                throw new ServiceResultException(StatusCodes.BadInvalidState,
                    "Subscription has not been assigned to a Session");
            }
        }

        /// <summary>
        /// Validates the sequence number of the incoming publish request.
        /// </summary>
        /// <param name="message"></param>
        private bool ValidSequentialPublishMessage(IncomingMessage message)
        {
            // If sequential publishing is enabled, only release messages in perfect sequence.
            return message.SequenceNumber <= _lastSequenceNumberProcessed + 1 ||
                // reconnect / transfer subscription case
                _resyncLastSequenceNumberProcessed ||
                // release the first message after wrapping around.
                (message.SequenceNumber == 1 && _lastSequenceNumberProcessed == uint.MaxValue);
        }

        /// <summary>
        /// Update the results to monitored items
        /// after updating the monitoring mode.
        /// </summary>
        /// <param name="monitoredItems"></param>
        /// <param name="errors"></param>
        /// <param name="results"></param>
        /// <param name="diagnosticInfos"></param>
        /// <param name="responseHeader"></param>
        /// <param name="monitoringMode"></param>
        private static bool UpdateMonitoringMode(IReadOnlyList<MonitoredItem> monitoredItems,
            List<ServiceResult> errors, StatusCodeCollection results,
            DiagnosticInfoCollection diagnosticInfos, ResponseHeader responseHeader,
            MonitoringMode monitoringMode)
        {
            // update results.
            var noErrors = true;

            for (var index = 0; index < results.Count; index++)
            {
                ServiceResult error;
                if (StatusCode.IsBad(results[index]))
                {
                    error = ClientBase.GetResult(results[index], index, diagnosticInfos,
                        responseHeader);
                    noErrors = false;
                }
                else
                {
                    monitoredItems[index].MonitoringMode = monitoringMode;
                    monitoredItems[index].Status.SetMonitoringMode(monitoringMode);
                    error = ServiceResult.Good;
                }
                errors.Add(error);
            }
            return noErrors;
        }

        /// <summary>
        /// Prepare the creation requests for all monitored items that have not yet been created.
        /// </summary>
        /// <param name="itemsToCreate"></param>
        private MonitoredItemCreateRequestCollection PrepareItemsToCreate(
            out List<MonitoredItem> itemsToCreate)
        {
            VerifySubscriptionState(true);

            var requestItems = new MonitoredItemCreateRequestCollection();
            itemsToCreate = new List<MonitoredItem>();

            lock (_cache)
            {
                foreach (var monitoredItem in _monitoredItems.Values)
                {
                    // ignore items that have been created.
                    if (monitoredItem.Status.Created)
                    {
                        continue;
                    }

                    // build item request.
                    var request = new MonitoredItemCreateRequest();

                    request.ItemToMonitor.NodeId = monitoredItem.ResolvedNodeId;
                    request.ItemToMonitor.AttributeId = monitoredItem.AttributeId;
                    request.ItemToMonitor.IndexRange = monitoredItem.IndexRange;
                    request.ItemToMonitor.DataEncoding = monitoredItem.Encoding;

                    request.MonitoringMode = monitoredItem.MonitoringMode;

                    request.RequestedParameters.ClientHandle = monitoredItem.ClientHandle;
                    request.RequestedParameters.SamplingInterval = monitoredItem.SamplingInterval;
                    request.RequestedParameters.QueueSize = monitoredItem.QueueSize;
                    request.RequestedParameters.DiscardOldest = monitoredItem.DiscardOldest;

                    if (monitoredItem.Filter != null)
                    {
                        request.RequestedParameters.Filter = new ExtensionObject(monitoredItem.Filter);
                    }

                    requestItems.Add(request);
                    itemsToCreate.Add(monitoredItem);
                }
            }
            return requestItems;
        }

        /// <summary>
        /// Prepare the modify requests for all monitored items
        /// that need modification.
        /// </summary>
        /// <param name="requestItems"></param>
        /// <param name="itemsToModify"></param>
        private void PrepareItemsToModify(
            MonitoredItemModifyRequestCollection requestItems,
            List<MonitoredItem> itemsToModify)
        {
            lock (_cache)
            {
                foreach (var monitoredItem in _monitoredItems.Values)
                {
                    // ignore items that have been created or modified.
                    if (!monitoredItem.Status.Created || !monitoredItem.AttributesModified)
                    {
                        continue;
                    }

                    // build item request.
                    var request = new MonitoredItemModifyRequest();

                    request.MonitoredItemId = monitoredItem.Status.Id;
                    request.RequestedParameters.ClientHandle = monitoredItem.ClientHandle;
                    request.RequestedParameters.SamplingInterval = monitoredItem.SamplingInterval;
                    request.RequestedParameters.QueueSize = monitoredItem.QueueSize;
                    request.RequestedParameters.DiscardOldest = monitoredItem.DiscardOldest;

                    if (monitoredItem.Filter != null)
                    {
                        request.RequestedParameters.Filter = new ExtensionObject(monitoredItem.Filter);
                    }

                    requestItems.Add(request);
                    itemsToModify.Add(monitoredItem);
                }
            }
        }

        /// <summary>
        /// Transfer all monitored items and prepares the modify
        /// requests if transfer of client handles is not possible.
        /// </summary>
        /// <param name="serverHandles"></param>
        /// <param name="clientHandles"></param>
        /// <param name="itemsToModify"></param>
        private void TransferItems(UInt32Collection serverHandles,
            UInt32Collection clientHandles, out IList<MonitoredItem> itemsToModify)
        {
            lock (_cache)
            {
                itemsToModify = new List<MonitoredItem>();
                var updatedMonitoredItems = new SortedDictionary<uint, MonitoredItem>();
                foreach (var monitoredItem in _monitoredItems.Values)
                {
                    var index = serverHandles.FindIndex(handle => handle == monitoredItem.Status.Id);
                    if (index >= 0 && index < clientHandles.Count)
                    {
                        var clientHandle = clientHandles[index];
                        updatedMonitoredItems[clientHandle] = monitoredItem;
                        monitoredItem.SetTransferResult(clientHandle);
                    }
                    else
                    {
                        // modify client handle on server
                        updatedMonitoredItems[monitoredItem.ClientHandle] = monitoredItem;
                        itemsToModify.Add(monitoredItem);
                    }
                }
                _monitoredItems = updatedMonitoredItems;
            }
        }

        /// <summary>
        /// Find or create an entry for the incoming sequence number.
        /// </summary>
        /// <param name="utcNow">The current Utc time.</param>
        /// <param name="tickCount">The current monotonic time</param>
        /// <param name="sequenceNumber">The sequence number for the new
        /// entry.</param>
        private IncomingMessage FindOrCreateEntry(DateTime utcNow, int tickCount,
            uint sequenceNumber)
        {
            IncomingMessage? entry = null;
            var node = _incomingMessages.Last;

            Debug.Assert(Monitor.IsEntered(_cache));
            while (node != null)
            {
                entry = node.Value;
                var previous = node.Previous;

                if (entry.SequenceNumber == sequenceNumber)
                {
                    entry.Timestamp = utcNow;
                    entry.TickCount = tickCount;
                    break;
                }

                if (entry.SequenceNumber < sequenceNumber)
                {
                    entry = new IncomingMessage();
                    entry.SequenceNumber = sequenceNumber;
                    entry.Timestamp = utcNow;
                    entry.TickCount = tickCount;
                    _incomingMessages.AddAfter(node, entry);
                    break;
                }

                node = previous;
                entry = null;
            }

            if (entry == null)
            {
                entry = new IncomingMessage();
                entry.SequenceNumber = sequenceNumber;
                entry.Timestamp = utcNow;
                entry.TickCount = tickCount;
                _incomingMessages.AddLast(entry);
            }

            return entry;
        }

        private event PublishStateChangedEventHandler? _publishStatusChanged;
        private event SubscriptionStateChangedEventHandler? _StateChanged;
        private List<MonitoredItem> _deletedItems;
        private SubscriptionChangeMask _changeMask;
        private byte _currentPriority;
#if NET6_0_OR_GREATER
        private PeriodicTimer? _publishTimer;
#else
        private Timer? _publishTimer;
#endif
        private long _lastNotificationTime;
        private int _lastNotificationTickCount;
        private int _keepAliveInterval;
        private int _publishLateCount;

        private readonly object _cache = new();
        private readonly LinkedList<NotificationMessage> _messageCache;
        private readonly AsyncAutoResetEvent _messageWorkerEvent;

        private IList<uint>? _availableSequenceNumbers;
        private int _maxMessageCount;
        private SortedDictionary<uint, MonitoredItem> _monitoredItems;
        private CancellationTokenSource? _messageWorkerCts;
        private Task? _messageWorkerTask;
        private int _outstandingMessageWorkers;
        private bool _sequentialPublishing;
        private uint _lastSequenceNumberProcessed;
        private bool _resyncLastSequenceNumberProcessed;

        /// <summary>
        /// A message received from the server cached until is processed or discarded.
        /// </summary>
        private class IncomingMessage
        {
            public uint SequenceNumber;
            public DateTime Timestamp;
            public int TickCount;
            public NotificationMessage? Message;
            public bool Processed;
            public bool Republished;
            public StatusCode RepublishStatus;
        }

        private readonly LinkedList<IncomingMessage> _incomingMessages = new();
        private readonly ILogger _logger;

        const int kMinKeepAliveTimerInterval = 1000;
        const int kKeepAliveTimerMargin = 1000;
        const int kRepublishMessageTimeout = 2500;
        const int kRepublishMessageExpiredTimeout = 10000;
    }
}
