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
                try
                {
                    //
                    // If callback is registered with a different subscription
                    // dispose first and release the reference count to the client.
                    //
                    // TODO: Here we want to check if there is only one subscriber
                    // for the subscription. If there is - we want to update the
                    // subscription (safely) with the new template configuration.
                    // Essentially original behavior before 2.9.12.
                    //
                    if (_registrations.TryGetValue(subscriber, out var existing) &&
                        existing.Subscription != subscription)
                    {
                        await existing.DisposeAsync().ConfigureAwait(false);
                        Debug.Assert(!_registrations.ContainsKey(subscriber));
                    }

                    var registration = new Registration(this, subscription, subscriber);
                    _registrations.Add(subscriber, registration);
                    TriggerSubscriptionSynchronization(null);
                    return registration;
                }
                finally
                {
                    _subscriptionLock.Release();
                }
            }
            finally
            {
                Release();
            }
        }

        /// <summary>
        /// Called by subscription to obtain the monitored items that
        /// should be part of itself. This is called under the subscription
        /// lock from the management thread so no need to lock here.
        /// </summary>
        /// <param name="template"></param>
        /// <returns></returns>
        internal IEnumerable<(ISubscriber, BaseMonitoredItemModel)> GetItems(
            SubscriptionModel template)
        {
            Debug.Assert(_subscriptionLock.CurrentCount == 0, "Must be locked");

            // Consider having an index for template to subscribers
            // This will be needed anyway as we must support partitioning

            return _registrations
                .Where(s => s.Value.Subscription == template)
                .SelectMany(s => s.Key.MonitoredItems.Select(i => (s.Key, i)));
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
            if (subscription == null)
            {
                TriggerConnectionEvent(ConnectionEvent.SubscriptionSyncAll);
            }
            else
            {
                TriggerConnectionEvent(ConnectionEvent.SubscriptionSyncOne,
                    subscription);
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
            _cache.AddOrUpdate(subscription.Template, subscription);
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
            if (_cache.TryGetValue(template, out subscription))
            {
                return true;
            }
            subscription = _session?.SubscriptionHandles
                .Find(s => s.Template == template);
            return subscription != null;
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
            await _subscriptionLock.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                await subscription.SyncAsync(ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "{Client}: Error trying to sync subscription {Subscription}",
                    this, subscription);
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
            var numberOfOperations = 0;
            var existing = session.SubscriptionHandles.ToDictionary(k => k.Template);

            await EnsureSessionIsReadyForSubscriptionsAsync(session,
                ct).ConfigureAwait(false);

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
                var registered = _registrations
                    .GroupBy(v => v.Value.Subscription)
                    .ToDictionary(kv => kv.Key, kv => kv.ToList());

                // Close and remove items that have no subscribers
                await Task.WhenAll(existing.Keys
                    .Except(registered.Keys)
                    .Select(k => existing[k])
                    .Select(async close =>
                    {
                        try
                        {
                            _cache.TryRemove(close.Template, out _);

                            // Removes the item from the session
                            await close.CloseAsync(session,
                                ct).ConfigureAwait(false);
                            Interlocked.Increment(ref numberOfOperations);
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
                await Task.WhenAll(registered.Keys
                    .Except(existing.Keys)
                    .Select(async add =>
                    {
                        try
                        {
                            //
                            // Create a new subscription with the subscription
                            // configuration template that as yet has no
                            // representation and add it to the session.
                            //
#pragma warning disable CA2000 // Dispose objects before losing scope
                            var subscription = new OpcUaSubscription(this,
                                add, _subscriptionOptions, CreateSessionTimeout,
                                _loggerFactory,
                                new OpcUaClientTagList(_connection, _metrics),
                                _timeProvider);
#pragma warning restore CA2000 // Dispose objects before losing scope

                            // Add the subscription to the session
                            session.AddSubscription(subscription);

                            // Sync the subscription which will get it to go live.
                            await subscription.SyncAsync(ct).ConfigureAwait(false);
                            Interlocked.Increment(ref numberOfOperations);
                            Debug.Assert(session == subscription.Session);

                            registered[add].ForEach(r => r.Value.Dirty = false);
                        }
                        catch (OperationCanceledException) { }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "{Client}: Failed to add " +
                                "subscription {Subscription} in session.",
                                this, add);
                        }
                    })).ConfigureAwait(false);

                // Update any items where subscriber signalled the item was updated
                await Task.WhenAll(registered.Keys.Intersect(existing.Keys)
                    .Where(u => registered[u].Any(b => b.Value.Dirty))
                    .Select(async update =>
                    {
                        try
                        {
                            var subscription = existing[update];
                            await subscription.SyncAsync(ct).ConfigureAwait(false);
                            Interlocked.Increment(ref numberOfOperations);
                            Debug.Assert(session == subscription.Session);
                            registered[update].ForEach(r => r.Value.Dirty = false);
                        }
                        catch (OperationCanceledException) { }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "{Client}: Failed to update " +
                                "subscription {Subscription} in session.",
                                this, update);
                        }
                    })).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Client}: Error trying to sync subscriptions.", this);
            }
            finally
            {
                _subscriptionLock.Release();
            }

            // Finish
            session.UpdateOperationTimeout(false);
            UpdatePublishRequestCounts();

            if (numberOfOperations < 2)
            {
                // Limit logging
                return;
            }

            // Clear the node cache - TODO: we should have a real node cache here
            session?.NodeCache.Clear();

            _logger.LogInformation("{Client}: Applied {Count} changes to subscriptions " +
                "in session took {Duration}.", this, numberOfOperations, sw.Elapsed);
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
        /// Subscription registration
        /// </summary>
        private sealed record Registration : ISubscription, ISubscriptionDiagnostics
        {
            /// <summary>
            /// The subscription configuration
            /// </summary>
            public SubscriptionModel Subscription { get; }

            /// <summary>
            /// Mark the registration as dirty
            /// </summary>
            public bool Dirty { get; internal set; }

            /// <inheritdoc/>
            public IOpcUaClientDiagnostics ClientDiagnostics => _outer;

            /// <inheritdoc/>
            public ISubscriptionDiagnostics Diagnostics => this;

            /// <inheritdoc/>
            public int GoodMonitoredItems
                => _outer.TryGetSubscription(Subscription, out var subscription)
                    ? subscription.GetGoodMonitoredItems(_owner) : 0;
            /// <inheritdoc/>
            public int BadMonitoredItems
                => _outer.TryGetSubscription(Subscription, out var subscription)
                    ? subscription.GetBadMonitoredItems(_owner) : 0;
            /// <inheritdoc/>
            public int LateMonitoredItems
                => _outer.TryGetSubscription(Subscription, out var subscription)
                    ? subscription.GetLateMonitoredItems(_owner) : 0;
            /// <inheritdoc/>
            public int HeartbeatsEnabled
                => _outer.TryGetSubscription(Subscription, out var subscription)
                    ? subscription.GetHeartbeatsEnabled(_owner) : 0;
            /// <inheritdoc/>
            public int ConditionsEnabled
                => _outer.TryGetSubscription(Subscription, out var subscription)
                    ? subscription.GetConditionsEnabled(_owner) : 0;

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
                _owner = owner;
                _outer = outer;

                _outer.AddRef();
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
                    _outer._registrations.Remove(_owner);
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
                ISubscriber owner, DataSetFieldContentFlags? fieldMask,
                DataSetMetaDataModel dataSetMetaData, uint minorVersion,
                CancellationToken ct = default)
            {
                if (!_outer.TryGetSubscription(Subscription, out var subscription))
                {
                    throw new ServiceResultException(StatusCodes.BadNoSubscription,
                        "Subscription not found");
                }
                return await subscription.CollectMetaDataAsync(owner, fieldMask,
                    dataSetMetaData, minorVersion, ct).ConfigureAwait(false);
            }

            private readonly OpcUaClient _outer;
            private readonly ISubscriber _owner;
        }

#pragma warning disable CA2213 // Disposable fields should be disposed
        private readonly SemaphoreSlim _subscriptionLock = new(1, 1);
#pragma warning restore CA2213 // Disposable fields should be disposed
        private readonly Dictionary<ISubscriber, Registration> _registrations = new();
        private readonly IOptions<OpcUaSubscriptionOptions> _subscriptionOptions;
        private readonly ConcurrentDictionary<SubscriptionModel, OpcUaSubscription> _cache = new();
    }
}
