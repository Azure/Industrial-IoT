// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Services
{
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Opc.Ua;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Opc.Ua.Client;

    internal sealed partial class OpcUaClient
    {
        /// <summary>
        /// Register a new subscriber for a subscription defined by the
        /// subscription template.
        /// </summary>
        /// <param name="subscription"></param>
        /// <param name="subscriber"></param>
        /// <param name="ct"></param>
        internal async ValueTask<ISubscriptionRegistration> RegisterAsync(
            SubscriptionModel subscription, ISubscriber subscriber,
            CancellationToken ct = default)
        {
            // Take a reference to the client for the lifetime of the subscription
            AddRef();
            try
            {
                await _subscriptionLock.WaitAsync(ct).ConfigureAwait(false);
                AddRef();
                try
                {
                    VirtualSubscription? existingSub = null;

                    //
                    // If subscriber is registered with a different subscription we either
                    // update the subscription or dispose the old one and create a new one.
                    //
                    if (_registrations.TryGetValue(subscriber, out var existing) &&
                        existing.Subscription != subscription)
                    {
                        existing.RemoveAndReleaseNoLockInternal();
                        Debug.Assert(!_registrations.ContainsKey(subscriber));

                        //
                        // We check if there are any other subscribers registered with the
                        // same subscription configuration that we want to apply. If there
                        // are not - we update the subscription (safely) with the new
                        // desired template configuration. Essentially original behavior
                        // before 2.9.12.
                        //
                        if ((!_s2r.TryGetValue(subscription, out var c) || c.Count == 0) &&
                            TryGetSubscription(existing.Subscription, out existingSub))
                        {
                            existingSub.Update(subscription);
                        }
                    }

                    var registration = new Registration(this, subscription, subscriber);
                    TriggerSubscriptionSynchronization(existingSub);
                    return registration;
                }
                finally
                {
                    Release();
                    _subscriptionLock.Release();
                }
            }
            finally
            {
                Release();
            }
        }

        /// <summary>
        /// Get subscribers for a subscription template to get at the monitored
        /// items that should be created in the subscription or subscriptions.
        /// Called under the subscription lock as a result of synchronization.
        /// </summary>
        /// <param name="template"></param>
        /// <returns></returns>
        internal IEnumerable<ISubscriber> GetSubscribers(SubscriptionModel template)
        {
            Debug.Assert(_subscriptionLock.CurrentCount == 0, "Must be locked");

            if (_s2r.TryGetValue(template, out var registrations))
            {
                return registrations.Select(r => r.Owner);
            }

            return Enumerable.Empty<ISubscriber>();
        }

        /// <summary>
        /// Trigger the client to manage the subscription. This is a
        /// no op if the subscription is not registered or the client
        /// is not connected.
        /// </summary>
        /// <param name="subscription"></param>
        internal void TriggerSubscriptionSynchronization(
            VirtualSubscription? subscription = null)
        {
            if (subscription != null)
            {
                TriggerConnectionEvent(ConnectionEvent.SubscriptionSyncOne,
                    subscription);
            }
            else
            {
                TriggerConnectionEvent(ConnectionEvent.SubscriptionSyncAll);
            }
        }

        /// <summary>
        /// Try get subscription with subscription model
        /// </summary>
        /// <param name="template"></param>
        /// <param name="subscription"></param>
        /// <returns></returns>
        private bool TryGetSubscription(SubscriptionModel template,
            [NotNullWhen(true)] out VirtualSubscription? subscription)
        {
            // Fast lookup
            lock (_subscriptions)
            {
                if (_subscriptions.TryGetValue(template, out subscription))
                {
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Access to the subscription to sync state must go through the
        /// subscription lock. This just wraps the sync call on the
        /// subscription.
        /// </summary>
        /// <param name="subscription"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        internal async Task SyncAsync(VirtualSubscription subscription,
            CancellationToken ct = default)
        {
            var session = _session;
            if (session == null)
            {
                return;
            }

            await _subscriptionLock.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                // Get the max item per subscription as well as max
                var caps = await session.GetServerCapabilitiesAsync(
                    NamespaceFormat.Uri, ct).ConfigureAwait(false);
                var delay = await subscription.SyncAsync(session.OperationLimits,
                    ct).ConfigureAwait(false);
                RescheduleSynchronization(delay);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "{Client}: Error trying to sync subscription {Subscription}",
                    this, subscription);
                RescheduleSynchronization(TimeSpan.FromMinutes(1));
            }
            finally
            {
                _subscriptionLock.Release();
            }
        }

        /// <summary>
        /// Called by the management thread to synchronize the subscriptions
        /// within a session as a result of the trigger call or when a session
        /// is reconnected/recreated.
        /// </summary>
        /// <param name="connected"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        internal async Task SyncAsync(bool connected, CancellationToken ct = default)
        {
            var session = _session;
            if (session == null)
            {
                return;
            }
            var sw = Stopwatch.StartNew();
            var removals = 0;
            var additions = 0;
            var updates = 0;
            Dictionary<SubscriptionModel, VirtualSubscription> existing;
            lock (_subscriptions)
            {
                existing = _subscriptions.ToDictionary();
            }

            _logger.LogDebug(
                "{Client}: Perform synchronization of subscriptions (total: {Total})",
                this, session.Subscriptions.Count);

            await EnsureSessionIsReadyForSubscriptionsAsync(session,
                ct).ConfigureAwait(false);

            // Get the max item per subscription as well as max
            var delay = Timeout.InfiniteTimeSpan;

            //
            // Take the subscription lock here! - we hold it all the way until we
            // have updated all subscription states. The subscriptions will access
            // the client again to obtain the monitored items from the subscribers
            // and we do not want any subscribers to be touched or removed while
            // we process the current registrations. Since the call to get the items
            // is frequent, we do not want to generate a copy every time but let
            // the subscriptions access the items directly.
            //
            await _subscriptionLock.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                var s2r = _s2r.ToDictionary(kv => kv.Key, kv => kv.Value.ToList());

                // Close and remove items that have no subscribers
                await Task.WhenAll(existing.Keys
                    .Except(s2r.Keys)
                    .Select(k => existing[k])
                    .Select(async close =>
                    {
                        try
                        {
                            lock (_subscriptions)
                            {
                                _subscriptions.Remove(close.Template);
                            }
                            if (_s2r.TryRemove(close.Template, out var r))
                            {
                                Debug.Assert(r.Count == 0,
                                    $"count of registrations {r.Count} > 0");
                            }

                            // Removes the item from the session and dispose
                            await close.DisposeAsync().ConfigureAwait(false);
                            Interlocked.Increment(ref removals);
                        }
                        catch (OperationCanceledException) { }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "{Client}: Failed to close " +
                                "subscription {Subscription} in session.",
                                this, close);
                        }
                    })).ConfigureAwait(false);

                // Add new subscription for items with subscribers
                var delays = await Task.WhenAll(s2r.Keys
                    .Except(existing.Keys)
                    .Select(async add =>
                    {
                        try
                        {
                            //
                            // Create a new virtual subscription with the subscription
                            // configuration template that as of yet has no representation
                            //
                            var subscription = new VirtualSubscription(this, add,
                                _subscriptionOptions);
                            lock (_subscriptions)
                            {
                                _subscriptions.Add(add, subscription);
                            }

                            // Sync the subscription which will get it to go live.
                            var delay = await subscription.SyncAsync(session.OperationLimits,
                                ct).ConfigureAwait(false);
                            Interlocked.Increment(ref additions);

                            s2r[add].ForEach(r => r.Dirty = false);
                            return delay;
                        }
                        catch (OperationCanceledException)
                        {
                            return Timeout.InfiniteTimeSpan;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "{Client}: Failed to add " +
                                "subscription {Subscription} in session.",
                                this, add);
                            return TimeSpan.FromMinutes(1);
                        }
                    })).ConfigureAwait(false);

                delay = delays.DefaultIfEmpty(Timeout.InfiniteTimeSpan).Min();
                // Update any items where subscriber signalled the item was updated
                delays = await Task.WhenAll(s2r.Keys.Intersect(existing.Keys)
                    .Where(u => s2r[u].Any(b => b.Dirty || connected))
                    .Select(async update =>
                    {
                        try
                        {
                            var subscription = existing[update];
                            var delay = await subscription.SyncAsync(session.OperationLimits,
                                ct).ConfigureAwait(false);
                            Interlocked.Increment(ref updates);
                            s2r[update].ForEach(r => r.Dirty = false);
                            return delay;
                        }
                        catch (OperationCanceledException)
                        {
                            return Timeout.InfiniteTimeSpan;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "{Client}: Failed to update " +
                                "subscription {Subscription} in session.",
                                this, update);
                            return TimeSpan.FromMinutes(1);
                        }
                    })).ConfigureAwait(false);

                var delay2 = delays.DefaultIfEmpty(Timeout.InfiniteTimeSpan).Min();
                RescheduleSynchronization(delay < delay2 ? delay : delay2);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Client}: Error trying to sync subscriptions.", this);
                var delay2 = TimeSpan.FromMinutes(1);
                RescheduleSynchronization(delay < delay2 ? delay : delay2);
            }
            finally
            {
                _subscriptionLock.Release();
            }

            // Finish
            UpdatePublishRequestCounts();

            if (updates + removals + additions == 0)
            {
                return;
            }
            _logger.LogInformation("{Client}: Removed {Removals}, added {Additions}, " +
                "and updated {Updates} subscriptions (total: {Total}) took {Duration} ms.",
                this, removals, additions, updates, session.Subscriptions.Count,
                sw.ElapsedMilliseconds);
        }

        /// <summary>
        /// Check session is ready for subscriptions, which means we fetch the
        /// namespace table and type system needed for the encoders and metadata.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task EnsureSessionIsReadyForSubscriptionsAsync(OpcUaSession session,
            CancellationToken ct)
        {
            try
            {
                // Reload namespace tables should they have changed...
                await session.FetchNamespaceTablesAsync(ct).ConfigureAwait(false);
            }
            catch (ServiceResultException sre) // anything else is not expected
            {
                _logger.LogWarning(sre, "{Client}: Failed to fetch namespace table...", this);
            }

            if (!DisableComplexTypeLoading && !session.IsTypeSystemLoaded)
            {
                // Ensure type system is loaded
                await session.GetComplexTypeSystemAsync(ct).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Called under lock, schedule resynchronization of all subscriptions
        /// after the specified delay
        /// </summary>
        /// <param name="delay"></param>
        private void RescheduleSynchronization(TimeSpan delay)
        {
            Debug.Assert(_subscriptionLock.CurrentCount == 0, "Must be locked");

            if (delay == Timeout.InfiniteTimeSpan)
            {
                return;
            }

            var nextSync = _timeProvider.GetUtcNow() + delay;
            if (nextSync <= _nextSync)
            {
                _nextSync = nextSync;
                _resyncTimer.Change(delay, Timeout.InfiniteTimeSpan);
            }
        }

        /// <summary>
        /// Subscription registration
        /// </summary>
        private sealed record Registration : ISubscriptionRegistration, ISubscriptionDiagnostics
        {
            /// <summary>
            /// Monitored items on the subscriber
            /// </summary>
            internal ISubscriber Owner { get; }

            /// <summary>
            /// Mark the registration as dirty
            /// </summary>
            internal bool Dirty { get; set; }

            /// <summary>
            /// The subscription configuration
            /// </summary>
            public SubscriptionModel Subscription { get; }

            /// <inheritdoc/>
            public IOpcUaClientDiagnostics ClientDiagnostics => _outer;

            /// <inheritdoc/>
            public ISubscriptionDiagnostics Diagnostics => this;

            /// <inheritdoc/>
            public int GoodMonitoredItems
                => _outer.TryGetSubscription(Subscription, out var subscription)
                    ? subscription.GetGoodMonitoredItems(Owner) : 0;
            /// <inheritdoc/>
            public int BadMonitoredItems
                => _outer.TryGetSubscription(Subscription, out var subscription)
                    ? subscription.GetBadMonitoredItems(Owner) : 0;
            /// <inheritdoc/>
            public int LateMonitoredItems
                => _outer.TryGetSubscription(Subscription, out var subscription)
                    ? subscription.GetLateMonitoredItems(Owner) : 0;
            /// <inheritdoc/>
            public int HeartbeatsEnabled
                => _outer.TryGetSubscription(Subscription, out var subscription)
                    ? subscription.GetHeartbeatsEnabled(Owner) : 0;
            /// <inheritdoc/>
            public int ConditionsEnabled
                => _outer.TryGetSubscription(Subscription, out var subscription)
                    ? subscription.GetConditionsEnabled(Owner) : 0;

            /// <summary>
            /// Create subscription
            /// </summary>
            /// <param name="outer"></param>
            /// <param name="subscription"></param>
            /// <param name="owner"></param>
            public Registration(OpcUaClient outer,
                SubscriptionModel subscription, ISubscriber owner)
            {
                Subscription = subscription;
                Owner = owner;
                _outer = outer;

                _outer.AddRef();
                AddNoLockInternal();
            }

            /// <inheritdoc/>
            public async ValueTask DisposeAsync()
            {
                if (_outer._disposed)
                {
                    //
                    // Possibly the client has shut down before the owners of
                    // the registration have disposed it. This is not an error.
                    // It might however be better to order the clients to get
                    // disposed before clients.
                    //
                    return;
                }

                // Remove registration
                await _outer._subscriptionLock.WaitAsync().ConfigureAwait(false);
                try
                {
                    RemoveNoLockInternal();
                    _outer.TriggerSubscriptionSynchronization(null);
                }
                finally
                {
                    _outer._subscriptionLock.Release();
                    _outer.Release();
                }
            }

            /// <inheritdoc/>
            public OpcUaSubscriptionNotification? CreateKeepAlive()
            {
                if (!_outer.TryGetSubscription(Subscription, out var subscription))
                {
                    return null;
                }
                return subscription.CreateKeepAlive();
            }

            /// <inheritdoc/>
            public void NotifyMonitoredItemsChanged()
            {
                Dirty = true;
                _outer.TryGetSubscription(Subscription, out var subscription);
                _outer.TriggerSubscriptionSynchronization(subscription);
            }

            /// <inheritdoc/>
            public async ValueTask<PublishedDataSetMetaDataModel> CollectMetaDataAsync(
                ISubscriber owner, DataSetMetaDataModel dataSetMetaData,
                uint minorVersion, CancellationToken ct = default)
            {
                if (!_outer.TryGetSubscription(Subscription, out var subscription))
                {
                    throw new ServiceResultException(StatusCodes.BadNoSubscription,
                        "Subscription not found");
                }
                return await subscription.CollectMetaDataAsync(owner, dataSetMetaData,
                    minorVersion, ct).ConfigureAwait(false);
            }

            /// <summary>
            /// Remove registration and release reference but not under
            /// lock (like user of the registration handle) and without
            /// triggering an update.
            /// </summary>
            internal void RemoveAndReleaseNoLockInternal()
            {
                RemoveNoLockInternal();
                _outer.Release();
            }

            private void AddNoLockInternal()
            {
                _outer._registrations.Add(Owner, this);
                _outer._s2r.AddOrUpdate(Subscription, _
                    => new List<Registration> { this },
                (_, c) =>
                {
                    c.Add(this);
                    return c;
                });
            }

            private void RemoveNoLockInternal()
            {
                _outer._s2r.AddOrUpdate(Subscription, _ =>
                {
                    Debug.Fail("Unexpected");
                    return new List<Registration>();
                }, (_, c) =>
                {
                    c.Remove(this);
                    return c;
                });
                _outer._registrations.Remove(Owner);
            }

            private readonly OpcUaClient _outer;
        }
        /// <summary>
        /// Subscription manager ensures that batches of monitored items are
        /// efficiently partitioned across subscriptions.
        /// </summary>
        internal sealed class VirtualSubscription : SubscriptionOptions,
            IAsyncDisposable, IEquatable<VirtualSubscription>
        {
            /// <summary>
            /// Template for subscription
            /// </summary>
            public SubscriptionModel Template { get; private set; }

            /// <summary>
            /// Unique subscription identifier in the process
            /// </summary>
            public uint Id { get; }

            /// <summary>
            /// Subscription id
            /// </summary>
            public string Name { get; private set; }

            public byte DesiredPriority
                => Template.Priority
                ?? 0;

            public uint DesiredMaxNotificationsPerPublish
                => Template.MaxNotificationsPerPublish
                ?? 0;

            public uint DesiredLifetimeCount
                => Template.LifetimeCount
                ?? _options.Value.DefaultLifeTimeCount
                ?? 0;

            public uint DesiredKeepAliveCount
                => Template.KeepAliveCount
                ?? _options.Value.DefaultKeepAliveCount
                ?? 0;

            public TimeSpan DesiredPublishingInterval
                => Template.PublishingInterval
                ?? _options.Value.DefaultPublishingInterval
                ?? TimeSpan.FromSeconds(1);

            public bool UseDeferredAcknoledgements
                => Template.UseDeferredAcknoledgements
                ?? _options.Value.UseDeferredAcknoledgements
                ?? false;

            public bool EnableImmediatePublishing
                => Template.EnableImmediatePublishing
                ?? _options.Value.EnableImmediatePublishing
                ?? false;
            public bool DesiredRepublishAfterTransfer
                => Template.RepublishAfterTransfer
                ?? _options.Value.DefaultRepublishAfterTransfer
                ?? false;

            public TimeSpan MonitoredItemWatchdogTimeout
                => Template.MonitoredItemWatchdogTimeout
                ?? _options.Value.DefaultMonitoredItemWatchdogTimeout
                ?? TimeSpan.Zero;

            public MonitoredItemWatchdogCondition WatchdogCondition
                => Template.WatchdogCondition
                ?? _options.Value.DefaultMonitoredItemWatchdogCondition
                ?? MonitoredItemWatchdogCondition.WhenAnyIsLate;

            public SubscriptionWatchdogBehavior? WatchdogBehavior
                => Template.WatchdogBehavior
                ?? _options.Value.DefaultWatchdogBehavior;

            public bool ResolveBrowsePathFromRoot
                => Template.ResolveBrowsePathFromRoot
                ?? _options.Value.FetchOpcBrowsePathFromRoot
                ?? false;

            /// <summary>
            /// Create a virtual subscription that contains one or more subscriptions
            /// partitioned by max supported monitored items.
            /// </summary>
            /// <param name="client"></param>
            /// <param name="template"></param>
            /// <param name="options"></param>
            internal VirtualSubscription(OpcUaClient client, SubscriptionModel template,
                IOptions<OpcUaSubscriptionOptions> options)
            {
                _client = client;
                _options = options;

                Id = Opc.Ua.SequenceNumber.Increment32(ref _lastIndex);
                Template = template;
                Name = Template.CreateSubscriptionId();
            }

            /// <inheritdoc/>
            public override string? ToString()
            {
                return Name;
            }

            /// <inheritdoc/>
            public override bool Equals(object? obj)
            {
                if (obj is VirtualSubscription subscription)
                {
                    return subscription.Id == Id;
                }
                return false;
            }

            /// <inheritdoc/>
            public bool Equals(VirtualSubscription? other)
            {
                if (other is null)
                {
                    return false;
                }
                return other.Id == Id;
            }

            /// <inheritdoc/>
            public override int GetHashCode()
            {
                return Template.GetHashCode();
            }

            /// <inheritdoc/>
            public async ValueTask DisposeAsync()
            {
                try
                {
                    var subscriptions = _subscriptions.ToList();
                    _subscriptions = Array.Empty<OpcUaSubscription>();
                    foreach (var subscription in subscriptions)
                    {
                        await subscription.DeleteAsync(true, default).ConfigureAwait(false);
                        await subscription.DisposeAsync().ConfigureAwait(false);
                    }
                }
                finally
                {
                    _lock.Dispose();
                }
            }

            /// <summary>
            /// Notify session disconnected/reconnecting. This is called
            /// on all subscriptions in the session and takes child subscriptions
            /// into account
            /// </summary>
            /// <param name="disconnected"></param>
            /// <returns></returns>
            internal void NotifySessionConnectionState(bool disconnected)
            {
                foreach (var subscription in _subscriptions)
                {
                    subscription.NotifySessionConnectionState(disconnected);
                }
            }

            /// <summary>
            /// Get number of good monitored item for the subscriber across
            /// this and all child subscriptions
            /// </summary>
            /// <param name="owner"></param>
            /// <returns></returns>
            internal int GetGoodMonitoredItems(ISubscriber owner)
            {
                return GetAllMonitoredItems().Count(r => r is OpcUaMonitoredItem h
                    && h.Owner == owner && h.IsGood);
            }

            /// <summary>
            /// Get number of bad monitored item for the subscriber across
            /// this and all child subscriptions
            /// </summary>
            /// <param name="owner"></param>
            /// <returns></returns>
            internal int GetBadMonitoredItems(ISubscriber owner)
            {
                return GetAllMonitoredItems().Count(r => r is OpcUaMonitoredItem h
                    && h.Owner == owner && h.IsBad);
            }

            /// <summary>
            /// Get number of late monitored item for the subscriber across
            /// this and all child subscriptions
            /// </summary>
            /// <param name="owner"></param>
            /// <returns></returns>
            internal int GetLateMonitoredItems(ISubscriber owner)
            {
                return GetAllMonitoredItems().Count(r => r is OpcUaMonitoredItem h
                    && h.Owner == owner && h.IsLate);
            }

            /// <summary>
            /// Get number of enabled heartbeats for the subscriber across
            /// this and all child subscriptions
            /// </summary>
            /// <param name="owner"></param>
            /// <returns></returns>
            internal int GetHeartbeatsEnabled(ISubscriber owner)
            {
                return GetAllMonitoredItems().Count(r => r is OpcUaMonitoredItem.Heartbeat h
                    && h.Owner == owner && h.TimerEnabled);
            }

            /// <summary>
            /// Get number of conditions enabled for the subscriber across
            /// this and all child subscriptions
            /// </summary>
            /// <param name="owner"></param>
            /// <returns></returns>
            internal int GetConditionsEnabled(ISubscriber owner)
            {
                return GetAllMonitoredItems().Count(r => r is OpcUaMonitoredItem.Condition h
                    && h.Owner == owner && h.TimerEnabled);
            }

            /// <summary>
            /// Collect metadata for the subscriber across this and all child
            /// subscriptions
            /// </summary>
            /// <param name="owner"></param>
            /// <param name="dataSetMetaData"></param>
            /// <param name="minorVersion"></param>
            /// <param name="ct"></param>
            /// <returns></returns>
            /// <exception cref="ServiceResultException"></exception>
            internal async ValueTask<PublishedDataSetMetaDataModel> CollectMetaDataAsync(
                ISubscriber owner, DataSetMetaDataModel dataSetMetaData, uint minorVersion,
                CancellationToken ct)
            {
                var session = _client._session;
                if (session == null)
                {
                    throw ServiceResultException.Create(StatusCodes.BadSessionIdInvalid,
                        "Session not connected.");
                }

                var typeSystem = await session.GetComplexTypeSystemAsync(ct).ConfigureAwait(false);
                var dataTypes = new NodeIdDictionary<object>();
                var fields = new List<PublishedFieldMetaDataModel>();

                await _lock.WaitAsync(ct).ConfigureAwait(false);
                try
                {
                    foreach (var monitoredItem in GetAllMonitoredItems().Where(m => m.Owner == owner))
                    {
                        await monitoredItem.GetMetaDataAsync(session, typeSystem,
                            fields, dataTypes, ct).ConfigureAwait(false);
                    }
                    return new PublishedDataSetMetaDataModel
                    {
                        DataSetMetaData =
                            dataSetMetaData,
                        EnumDataTypes =
                            dataTypes.Values.OfType<EnumDescriptionModel>().ToList(),
                        StructureDataTypes =
                            dataTypes.Values.OfType<StructureDescriptionModel>().ToList(),
                        SimpleDataTypes =
                            dataTypes.Values.OfType<SimpleTypeDescriptionModel>().ToList(),
                        Fields =
                            fields,
                        MinorVersion =
                            minorVersion
                    };
                }
                finally
                {
                    _lock.Release();
                }
            }

            /// <summary>
            /// Update subscription configuration and apply changes later during
            /// synchronization. This is used when the subscription is owned by a
            /// single subscriber and the configuration is updated.
            /// </summary>
            /// <param name="template"></param>
            internal void Update(SubscriptionModel template)
            {
                Template = template;
                Name = Template.CreateSubscriptionId();
            }

            /// <summary>
            /// Create keep alive notification
            /// </summary>
            /// <returns></returns>
            internal OpcUaSubscriptionNotification? CreateKeepAlive()
            {
                var subscriptions = _subscriptions;
                return subscriptions.Count > 0 ? subscriptions[0].CreateKeepAlive() : null;
            }

            /// <summary>
            /// Create or update the subscription now using the currently configured
            /// subscription configuration template and session inside the client.
            /// </summary>
            /// <param name="limits"></param>
            /// <param name="ct"></param>
            /// <returns></returns>
            /// <exception cref="ServiceResultException"></exception>
            internal async ValueTask<TimeSpan> SyncAsync(Limits limits, CancellationToken ct)
            {
                var maxMonitoredItems = limits.MaxMonitoredItemsPerSubscription;
                if (maxMonitoredItems <= 0)
                {
                    maxMonitoredItems = _options.Value.MaxMonitoredItemPerSubscription
                        ?? kMaxMonitoredItemPerSubscriptionDefault;
                }

                var session = _client._session;
                if (session?.Connected != true)
                {
                    return TimeSpan.FromSeconds(5);
                }

                var retryDelay = Timeout.InfiniteTimeSpan;

                // Parition the monitored items across subscriptions
                var partitions = Partition.Create(_client.GetSubscribers(Template),
                    maxMonitoredItems, _options.Value);

                await _lock.WaitAsync(ct).ConfigureAwait(false);
                try
                {
                    var subscriptions = _subscriptions.Where(s => s.Session == session).ToList();
                    //
                    // Force recreate subscriptions that are marked as such. Only need to
                    // do this for subscriptions that are below or equal the partition count.
                    //
                    var clamp = Math.Min(partitions.Count, subscriptions.Count);
                    for (var i = 0; i < clamp; i++)
                    {
                        var subscription = subscriptions[i];
                        if (!subscription.ForceRecreate)
                        {
                            continue;
                        }
                        _client._logger.LogInformation(
                            "{Client}: Force recreate {Subscription} ...", _client, subscription);
                        await subscription.DeleteAsync(true, ct).ConfigureAwait(false);
                        await subscription.DisposeAsync().ConfigureAwait(false);
                        subscriptions[i] = (OpcUaSubscription)session.Subscriptions.Add(this);
                    }
                    if (subscriptions.Count < partitions.Count)
                    {
                        // Grow
                        for (var idx = subscriptions.Count; idx < partitions.Count; idx++)
                        {
                            var subscription = (OpcUaSubscription)session.Subscriptions.Add(this);
                            subscriptions.Add(subscription);
                        }
                    }
                    else if (subscriptions.Count > partitions.Count)
                    {
                        // Shrink
                        foreach (var subscription in subscriptions.Skip(partitions.Count))
                        {
                            await subscription.DeleteAsync(true, ct).ConfigureAwait(false);
                            await subscription.DisposeAsync().ConfigureAwait(false);
                        }
                        subscriptions.RemoveRange(partitions.Count, subscriptions.Count - partitions.Count);
                    }

                    _subscriptions = subscriptions;
                    for (var partitionIdx = 0; partitionIdx < partitions.Count; partitionIdx++)
                    {
                        // Synchronize the subscription of this partition
                        await subscriptions[partitionIdx].SynchronizeSubscriptionAsync(
                            ct).ConfigureAwait(false);

                        // Add partitioned items
                        var partition = partitions[partitionIdx];
                        var delay = await subscriptions[partitionIdx].SynchronizeMonitoredItemsAsync(
                            partition, limits, ct).ConfigureAwait(false);
                        if (retryDelay > delay)
                        {
                            retryDelay = delay;
                        }
                    }

                    // Finalize all subscriptions
                    foreach (var subscription in subscriptions)
                    {
                        await subscription.FinalizeSyncAsync(ct).ConfigureAwait(false);
                    }
                }
                finally
                {
                    _lock.Release();
                }
                return retryDelay;
            }

            /// <summary>
            /// Get all monitored items încluding all child subscriptions.
            /// This call is used to collect all items recursively.
            /// </summary>
            /// <returns></returns>
            private IEnumerable<OpcUaMonitoredItem> GetAllMonitoredItems()
            {
                var items = Enumerable.Empty<OpcUaMonitoredItem>();
                foreach (var subscription in _subscriptions)
                {
                    items = items.Concat(subscription.CurrentlyMonitored);
                }
                return items;
            }

            private const int kMaxMonitoredItemPerSubscriptionDefault = 64 * 1024;
            private IReadOnlyList<OpcUaSubscription> _subscriptions = Array.Empty<OpcUaSubscription>();
            private readonly SemaphoreSlim _lock = new(1, 1);
            private readonly OpcUaClient _client;
            private readonly IOptions<OpcUaSubscriptionOptions> _options;
            private static uint _lastIndex;
        }

        /// <summary>
        /// Helper to partition subscribers across subscriptions. Uses a bag packing
        /// algorithm.
        /// </summary>
        internal sealed class Partition
        {
            /// <summary>
            /// Monitored items that should be in the subscription partition
            /// </summary>
            public List<(ISubscriber, BaseMonitoredItemModel)> Items { get; } = new();

            /// <summary>
            /// Create
            /// </summary>
            /// <param name="subscribers"></param>
            /// <param name="maxMonitoredItemsInPartition"></param>
            /// <param name="options"></param>
            /// <returns></returns>
            public static List<Partition> Create(IEnumerable<ISubscriber> subscribers,
                uint maxMonitoredItemsInPartition, OpcUaSubscriptionOptions options)
            {
                var partitions = new List<Partition>();
                foreach (var subscriberItems in subscribers
                    .Select(s => s.MonitoredItems
                        .Select(m => (s, m.SetDefaults(options)))
                        .ToList())
                    .OrderByDescending(tl => tl.Count))
                {
                    var placed = false;
                    foreach (var partition in partitions)
                    {
                        if (partition.Items.Count +
                            subscriberItems.Count <= maxMonitoredItemsInPartition)
                        {
                            partition.Items.AddRange(subscriberItems);
                            placed = true;
                            break;
                        }
                    }
                    if (!placed)
                    {
                        // Break items into batches of max here and add partition each
                        foreach (var batch in subscriberItems.Batch(
                            (int)maxMonitoredItemsInPartition))
                        {
                            var newPartition = new Partition();
                            newPartition.Items.AddRange(batch);
                            partitions.Add(newPartition);
                        }
                    }
                }
                return partitions;
            }
        }

        private DateTimeOffset _nextSync;
#pragma warning disable CA2213 // Disposable fields should be disposed
        private readonly SemaphoreSlim _subscriptionLock = new(1, 1);
        private readonly ITimer _resyncTimer;
#pragma warning restore CA2213 // Disposable fields should be disposed
        private readonly Dictionary<ISubscriber, Registration> _registrations = new();
        private readonly ConcurrentDictionary<SubscriptionModel, List<Registration>> _s2r = new();
        private readonly Dictionary<SubscriptionModel, VirtualSubscription> _subscriptions = new();
        private readonly IOptions<OpcUaSubscriptionOptions> _subscriptionOptions;
    }
}
