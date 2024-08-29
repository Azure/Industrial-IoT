// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Services
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Microsoft.Extensions.Logging;
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
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
                TriggerConnectionEvent(ConnectionEvent.SubscriptionManageAll);
            }
            else
            {
                TriggerConnectionEvent(ConnectionEvent.SubscriptionManageOne,
                    subscription);
            }
        }

        /// <summary>
        /// Called by the management thread to synchronize the subscriptions
        /// within a session as a result of the trigger call or when a session
        /// is reconnected/recreated.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        internal async Task SyncAsync(OpcUaSession session,
            CancellationToken ct = default)
        {
            Debug.Assert(session != null, "Session is null");
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
                            var subscription = new OpcUaSubscription(
                                this, add, _options, _loggerFactory,
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
        private sealed record Registration : ISubscription
        {
            /// <summary>
            /// The subscription configuration
            /// </summary>
            public SubscriptionModel Subscription { get; }

            /// <inheritdoc/>
            public IOpcUaClientDiagnostics State => _outer;

            /// <summary>
            /// Mark the registration as dirty
            /// </summary>
            public bool Dirty { get; internal set; }

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
                // Remove registration
                await _outer._subscriptionLock.WaitAsync().ConfigureAwait(false);
                try
                {
                    _outer._registrations.Remove(_owner);
                }
                finally
                {
                    _outer._subscriptionLock.Release();
                    _outer.Release();
                }
            }

            public OpcUaSubscriptionNotification? CreateKeepAlive()
            {
                // Find the subscription
                var subscription = _outer._session?.SubscriptionHandles
                    .Find(s => s.Template == Subscription);
                if (subscription == null)
                {
                    return null;
                }
                return subscription.CreateKeepAlive();
            }

            public void NotifyMonitoredItemsChanged()
            {
                var subscription = _outer._session?.SubscriptionHandles
                   .Find(s => s.Template == Subscription);

                Dirty = true;
                _outer.TriggerSubscriptionSynchronization(subscription);
            }

            public async ValueTask<PublishedDataSetMetaDataModel> CollectMetaDataAsync(
                ISubscriber owner, DataSetMetaDataModel dataSetMetaData,
                uint minorVersion, CancellationToken ct = default)
            {
                var subscription = _outer._session?.SubscriptionHandles
                   .Find(s => s.Template == Subscription);

                if (subscription == null)
                {
                    throw ServiceResultException.Create(StatusCodes.BadNoSubscription,
                        "Subscription not found");
                }

                return await subscription.CollectMetaDataAsync(owner,
                    dataSetMetaData, minorVersion, ct).ConfigureAwait(false);
            }

            private readonly OpcUaClient _outer;
            private readonly ISubscriber _owner;
        }

#pragma warning disable CA2213 // Disposable fields should be disposed
        private readonly SemaphoreSlim _subscriptionLock = new(1, 1);
#pragma warning restore CA2213 // Disposable fields should be disposed
        private readonly Dictionary<ISubscriber, Registration> _registrations = new();
    }
}
