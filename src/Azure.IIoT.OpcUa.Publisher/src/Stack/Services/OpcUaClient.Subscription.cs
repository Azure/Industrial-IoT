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

    internal sealed partial class OpcUaClient
    {
        /// <summary>
        /// Register a new subscriber for a subscription defined by the
        /// subscription template.
        /// </summary>
        /// <param name="subscription"></param>
        /// <param name="subscriber"></param>
        /// <param name="ct"></param>
        internal async ValueTask<ISubscription> RegisterAsync(
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
                    OpcUaSubscription? existingSub = null;

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
                        // arenot - we update the subscription (safely) with the new
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
            OpcUaSubscription? subscription = null)
        {
            if (subscription?.IsClosed == false)
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
        /// Called by subscription when newly created. This needs to be done
        /// here this way because the stack uses clone to clone the subscriptions
        /// just like it does with sessions and monitored items. This way we can
        /// hock the create and clone operations.
        /// </summary>
        /// <param name="subscription"></param>
        internal void OnSubscriptionCreated(OpcUaSubscription subscription)
        {
            lock (_cache)
            {
                if (subscription.IsRoot)
                {
                    _cache.AddOrUpdate(subscription.Template, subscription);
                }
            }
        }

        /// <summary>
        /// Try get subscription with subscription model
        /// </summary>
        /// <param name="template"></param>
        /// <param name="subscription"></param>
        /// <returns></returns>
        private bool TryGetSubscription(SubscriptionModel template,
            [NotNullWhen(true)] out OpcUaSubscription? subscription)
        {
            // Fast lookup
            lock (_cache)
            {
                if (_cache.TryGetValue(template, out subscription) &&
                    !subscription.IsClosed &&
                    subscription.IsRoot)
                {
                    return true;
                }
                subscription = _session?.SubscriptionHandles.Values
                    .FirstOrDefault(s => s.IsRoot && s.Template == template);
                if (subscription != null)
                {
                    _cache.AddOrUpdate(template, subscription);
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
        internal async Task SyncAsync(OpcUaSubscription subscription,
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
                var delay = await subscription.SyncAsync(caps.MaxMonitoredItemsPerSubscription,
                    caps.OperationLimits, ct).ConfigureAwait(false);
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
        /// <param name="ct"></param>
        /// <returns></returns>
        internal async Task SyncAsync(CancellationToken ct = default)
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

            var existing = session.SubscriptionHandles
                .Where(s => s.Value.IsRoot)
                .ToDictionary(k => k.Value.Template, k => k.Value);

            _logger.LogDebug(
                "{Client}: Perform synchronization of subscriptions (total: {Total})",
                this, session.SubscriptionHandles.Count);

            await EnsureSessionIsReadyForSubscriptionsAsync(session,
                ct).ConfigureAwait(false);

            // Get the max item per subscription as well as max
            var caps = await session.GetServerCapabilitiesAsync(
                NamespaceFormat.Uri, ct).ConfigureAwait(false);
            var maxMonitoredItems = caps.MaxMonitoredItemsPerSubscription;
            var limits = caps.OperationLimits;
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
                            lock (_cache)
                            {
                                _cache.Remove(close.Template);
                            }
                            if (_s2r.TryRemove(close.Template, out var r))
                            {
                                Debug.Assert(r.Count == 0,
                                    $"count of registrations {r.Count} > 0");
                            }

                            // Removes the item from the session and dispose
                            await close.DisposeAsync().ConfigureAwait(false);

                            Interlocked.Increment(ref removals);
                            Debug.Assert(close.IsClosed);
                            Debug.Assert(close.Session == null);
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
                            // Create a new subscription with the subscription
                            // configuration template that as of yet has no
                            // representation and add it to the session.
                            //
#pragma warning disable CA2000 // Dispose objects before losing scope
                            var subscription = new OpcUaSubscription(this, session,
                                add, _subscriptionOptions, _loggerFactory,
                                new OpcUaClientTagList(_connection, _metrics),
                                null, _timeProvider);
#pragma warning restore CA2000 // Dispose objects before losing scope

                            // Add the subscription to the session
                            session.Subscriptions.Add(subscription);

                            // Sync the subscription which will get it to go live.
                            var delay = await subscription.SyncAsync(maxMonitoredItems,
                                caps.OperationLimits, ct).ConfigureAwait(false);
                            Interlocked.Increment(ref additions);
                            Debug.Assert(session == subscription.Session);

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
                    .Where(u => s2r[u].Any(b => b.Dirty))
                    .Select(async update =>
                    {
                        try
                        {
                            var subscription = existing[update];
                            var delay = await subscription.SyncAsync(maxMonitoredItems,
                                caps.OperationLimits, ct).ConfigureAwait(false);
                            Interlocked.Increment(ref updates);
                            Debug.Assert(session == subscription.Session);
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
                this, removals, additions, updates, session.SubscriptionHandles.Count,
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
                var oldTable = session.NamespaceUris.ToArray();
                await session.FetchNamespaceTablesAsync(ct).ConfigureAwait(false);
                var newTable = session.NamespaceUris.ToArray();
                LogNamespaceTableChanges(oldTable, newTable);
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
        private sealed record Registration : ISubscription, ISubscriptionDiagnostics
        {
            /// <summary>
            /// The subscription configuration
            /// </summary>
            public SubscriptionModel Subscription { get; }

            /// <summary>
            /// Monitored items on the subscriber
            /// </summary>
            internal ISubscriber Owner { get; }

            /// <summary>
            /// Mark the registration as dirty
            /// </summary>
            internal bool Dirty { get; set; }

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

        private DateTimeOffset _nextSync;
#pragma warning disable CA2213 // Disposable fields should be disposed
        private readonly SemaphoreSlim _subscriptionLock = new(1, 1);
        private readonly ITimer _resyncTimer;
#pragma warning restore CA2213 // Disposable fields should be disposed
        private readonly Dictionary<ISubscriber, Registration> _registrations = new();
        private readonly ConcurrentDictionary<SubscriptionModel, List<Registration>> _s2r = new();
        private readonly Dictionary<SubscriptionModel, OpcUaSubscription> _cache = new();
        private readonly IOptions<OpcUaSubscriptionOptions> _subscriptionOptions;
    }
}
