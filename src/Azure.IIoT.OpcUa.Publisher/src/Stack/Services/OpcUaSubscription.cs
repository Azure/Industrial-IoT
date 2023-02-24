// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Services
{
    using Azure.IIoT.OpcUa.Encoders;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Azure.IIoT.OpcUa.Shared.Models;
    using Furly.Extensions.Serializers;
    using Furly.Extensions.Utils;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Extensions.Logging;
    using Opc.Ua;
    using Opc.Ua.Client;
    using Opc.Ua.Extensions;
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.Diagnostics.Metrics;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Subscription implementation
    /// </summary>
    internal sealed class OpcUaSubscription : ISubscription
    {
        /// <inheritdoc/>
        public string Name => _subscription.Id.Id;

        /// <inheritdoc/>
        public ushort Id { get; }

        /// <inheritdoc/>
        public ConnectionModel Connection => _subscription.Id.Connection;

        /// <inheritdoc/>
        public event EventHandler<SubscriptionNotificationModel> OnSubscriptionDataChange;

        /// <inheritdoc/>
        public event EventHandler<SubscriptionNotificationModel> OnSubscriptionEventChange;

        /// <inheritdoc/>
        public event EventHandler<int> OnSubscriptionDataDiagnosticsChange;

        ///  <inheritdoc/>
        public event EventHandler<int> OnSubscriptionEventDiagnosticsChange;

        /// <summary>
        /// Get subscription from session (no lock)
        /// </summary>
        /// <returns></returns>
        private Subscription Subscription
        {
            get
            {
                // Called under lock of session manager
                var session = _sessions.GetSessionHandle(
                    _subscription.Id.Connection)?.Session;
                if (session == null)
                {
                    return null;
                }
                return session.Subscriptions.SingleOrDefault(s => s.Handle == this);
            }
        }

        /// <summary>
        /// Subscription
        /// </summary>
        private OpcUaSubscription(ISessionProvider<ConnectionModel> session,
            IClientServicesConfig config, ILogger logger, IMetricsContext metrics)
            : this(metrics ?? throw new ArgumentNullException(nameof(metrics)))
        {
            _sessions = session ??
                throw new ArgumentNullException(nameof(session));
            _config = config ??
                throw new ArgumentNullException(nameof(config));
            _logger = logger /*?.ForContext<OpcUaSubscription>() TODO: USE loggerFactory here*/ ??
                throw new ArgumentNullException(nameof(logger));
            _lock = new SemaphoreSlim(1, 1);
            _currentlyMonitored = ImmutableDictionary<uint, OpcUaMonitoredItem>.Empty;
            Id = SequenceNumber.Increment16(ref _lastIndex);
        }

        /// <summary>
        /// Create subscription
        /// </summary>
        /// <param name="outer"></param>
        /// <param name="config"></param>
        /// <param name="subscription"></param>
        /// <param name="logger"></param>
        /// <param name="metrics"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        internal static async ValueTask<ISubscription> CreateAsync(
            ISessionProvider<ConnectionModel> outer, IClientServicesConfig config,
            SubscriptionModel subscription, ILogger logger, IMetricsContext metrics,
            CancellationToken ct = default)
        {
            // Create object
            var newSubscription = new OpcUaSubscription(outer, config, logger, metrics);

            // Initialize
            await newSubscription.UpdateAsync(subscription, ct).ConfigureAwait(false);

            return newSubscription;
        }

        /// <inheritdoc/>
        public void OnSubscriptionStateChanged(bool online)
        {
            foreach (var monitoredItem in _currentlyMonitored.Values)
            {
                monitoredItem.OnMonitoredItemStateChanged(online);
            }
        }

        /// <inheritdoc/>
        public bool TryUpgradeToKeyFrame(SubscriptionNotificationModel notification)
        {
            _lock.Wait();
            try
            {
                var subscription = Subscription;
                if (subscription == null)
                {
                    return false;
                }
                var allNotifications = subscription.MonitoredItems
                        .SelectMany(m => m.LastValue.ToMonitoredItemNotifications(m))
                        .ToList();
                notification.MetaData = _currentMetaData;
                notification.MessageType = Encoders.PubSub.MessageType.KeyFrame;
                notification.Notifications = allNotifications;
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to get a keyframe from monitored item cache in subscription {Name}.",
                    Name);
                return false;
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public SubscriptionNotificationModel CreateKeepAlive()
        {
            _lock.Wait();
            try
            {
                var subscription = Subscription;
                if (subscription == null)
                {
                    return null;
                }
                return new SubscriptionNotificationModel
                {
                    ServiceMessageContext = subscription.Session.MessageContext,
                    ApplicationUri = subscription.Session.Endpoint.Server.ApplicationUri,
                    EndpointUrl = subscription.Session.Endpoint.EndpointUrl,
                    MetaData = _currentMetaData,
                    SubscriptionName = Name,
                    SequenceNumber = SequenceNumber.Increment32(ref _sequenceNumber),
                    SubscriptionId = Id,
                    MessageType = Encoders.PubSub.MessageType.KeepAlive,
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to create a subscription notification for subscription {Name}.",
                    Name);
                return null;
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async ValueTask UpdateAsync(SubscriptionModel subscription,
            CancellationToken ct)
        {
            if (subscription == null)
            {
                throw new ArgumentNullException(nameof(subscription));
            }
            if (subscription.Configuration == null)
            {
                throw new ArgumentException("Missing configuration", nameof(subscription));
            }
            if (subscription.Id?.Connection == null)
            {
                throw new ArgumentException("Missing connection information", nameof(subscription));
            }
            await _lock.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                var previousSubscription = _subscription?.Id;

                // Update subscription configuration
                _subscription = subscription.Clone();

                if (previousSubscription != null && previousSubscription != _subscription.Id)
                {
                    //
                    // TODO: Do we need to remove any session  from session manager
                    // if the subscription was transfered to new connection model
                    // has changed?
                    //
                    _logger.LogError("Upgrading existing subscription to different session.");
                }

                // try to get a session using the provided configuration
                var session = await _sessions.GetOrCreateSessionAsync(
                    _subscription.Id.Connection, _metrics, default).ConfigureAwait(false);
                Debug.Assert(session != null);

                try
                {
                    //
                    // Try and apply configuration. If we fail session will retry
                    // periodically through the session manager
                    //
                    await ApplyInternalAsync(session).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to apply changes to subscription.");
                }

                //
                // Now register with the session to ensure the state is
                // re-applied when session changes or connects if unconnected.
                //
                session.RegisterSubscription(this);
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async ValueTask ReapplyToSessionAsync(ISessionHandle session)
        {
            // This is
            await _lock.WaitAsync().ConfigureAwait(false);
            try
            {
                await ApplyInternalAsync(session).ConfigureAwait(false);
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async ValueTask CloseAsync()
        {
            await _lock.WaitAsync().ConfigureAwait(false);
            ISessionHandle session;
            try
            {
                if (_closed)
                {
                    return;
                }
                _closed = true;

                session = _sessions.GetSessionHandle(_subscription.Id.Connection);
                if (session == null)
                {
                    _logger.LogWarning(
                        "Failed to close subscription '{Subscription}'. " +
                        "The attached session '{SessionId}' could not be found.",
                        Name, Connection.CreateConnectionId());
                    return;
                }

                // Unregister subscription from session
                session.UnregisterSubscription(this);
            }
            finally
            {
                _lock.Release();
            }

            // Get raw subscription from underlying session and close that one too
            var subscription = session.Session.Subscriptions.SingleOrDefault(s => s.Handle == this);
            if (subscription != null)
            {
                // This does not throw
                await CloseSubscriptionAsync(subscription).ConfigureAwait(false);
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (!_closed)
            {
                Try.Async(() => CloseAsync().AsTask()).Wait();
                Debug.Assert(_closed);
            }
            _lock.Dispose();
        }

        /// <summary>
        /// Send condition notification
        /// </summary>
        /// <param name="subscription"></param>
        /// <param name="notifications"></param>
        internal void SendConditionNotification(Subscription subscription,
            IEnumerable<MonitoredItemNotificationModel> notifications)
        {
            var onSubscriptionEventChange = OnSubscriptionEventChange;
            if (onSubscriptionEventChange == null || !(subscription?.Session?.Connected ?? false))
            {
                return;
            }
            var message = new SubscriptionNotificationModel
            {
                ServiceMessageContext = subscription?.Session?.MessageContext,
                ApplicationUri = subscription?.Session?.Endpoint?.Server?.ApplicationUri,
                EndpointUrl = subscription?.Session?.Endpoint?.EndpointUrl,
                SubscriptionName = Name,
                SubscriptionId = Id,
                SequenceNumber = SequenceNumber.Increment32(ref _sequenceNumber),
                MessageType = Encoders.PubSub.MessageType.Condition,
                MetaData = _currentMetaData,
                Timestamp = DateTime.UtcNow,
                Notifications = notifications.ToList()
            };
            onSubscriptionEventChange.Invoke(this, message);
            var onSubscriptionEventDiagnosticsChange = OnSubscriptionEventDiagnosticsChange;
            onSubscriptionEventDiagnosticsChange?.Invoke(this, 1);
        }

        /// <summary>
        /// Apply changes to the subscription in session (no lock)
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        private async Task ApplyInternalAsync(ISessionHandle session)
        {
            try
            {
                var rawSubscription = GetInnerSubscription(session.Session);

                // set the new set of monitored items
                _subscription.MonitoredItems = _subscription.MonitoredItems?.Select(n => n.Clone()).ToList();

                if (session != null && rawSubscription != null)
                {
                    if (!rawSubscription.PublishingEnabled)
                    {
                        // Initialized subscription, resolve display names first
                        ResolveDisplayNames(session.Session);
                    }

                    await SetMonitoredItemsAsync(rawSubscription, _subscription.MonitoredItems)
                        .ConfigureAwait(false);

                    if (!rawSubscription.PublishingEnabled || rawSubscription.ChangesPending)
                    {
                        _logger.LogDebug("Enabling subscription {Name} in session '{SessionId}'...",
                            Name, Connection.CreateConnectionId());
                        rawSubscription.SetPublishingMode(true);
                        await rawSubscription.ApplyChangesAsync().ConfigureAwait(false);
                        _logger.LogInformation("Subscription {Name} in session '{SessionId}' enabled.",
                            Name, Connection.CreateConnectionId());
                    }
                }
                else
                {
                    _logger.LogInformation(
                        "Failed to apply subscription changes to Subscription {Name} - no session found.", Name);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to apply monitored items to Subscription {Name}.", Name);
            }
        }

        /// <summary>
        /// Destroy subscription
        /// </summary>
        /// <param name="subscription"></param>
        private async Task CloseSubscriptionAsync(Subscription subscription)
        {
            try
            {
                _logger.LogInformation(
                    "Closing subscription '{Name}' in session '{SessionId}'...",
                    Name, Connection.CreateConnectionId());
                Try.Op(() => subscription.SetPublishingMode(false));
                await Try.Async(() => subscription.ApplyChangesAsync()).ConfigureAwait(false);
                await Try.Async(() => subscription.DeleteItemsAsync(default)).ConfigureAwait(false);
                await Try.Async(() => subscription.ApplyChangesAsync()).ConfigureAwait(false);
                _logger.LogDebug("Deleted monitored items for '{Name}'/'{SessionId}'",
                    Name, Connection.CreateConnectionId());
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to close subscription '{Name}'/'{SessionId}'",
                    Name, Connection.CreateConnectionId());
            }
        }

        /// <summary>
        /// Reads the display name of the nodes to be monitored
        /// </summary>
        private void ResolveDisplayNames(ISession session)
        {
            if (!(_subscription.Configuration?.ResolveDisplayName ?? false))
            {
                return;
            }

            if (session == null)
            {
                return;
            }

            var unresolvedMonitoredItems = _subscription.MonitoredItems
                .OfType<DataMonitoredItemModel>()
                .Where(mi => string.IsNullOrEmpty(mi.DisplayName));
            if (!unresolvedMonitoredItems.Any())
            {
                return;
            }

            try
            {
                var nodeIds = unresolvedMonitoredItems.
                    Select(n =>
                    {
                        try
                        {
                            return n.StartNodeId.ToNodeId(session.MessageContext);
                        }
                        catch (ServiceResultException sre)
                        {
                            _logger.LogWarning("Failed to resolve display name for '{MonitoredItem}' due to '{Message}'",
                                n.StartNodeId, sre.Message);
                        }
                        catch (Exception e)
                        {
                            _logger.LogError(e, "Failed to resolve display name for '{MonitoredItem}'",
                                n.StartNodeId);
                            throw;
                        }
                        return null;
                    });
                if (nodeIds.Any())
                {
                    session.ReadDisplayName(nodeIds.ToList(), out var displayNames, out var errors);
                    var index = 0;
                    foreach (var monitoredItem in unresolvedMonitoredItems)
                    {
                        if (StatusCode.IsGood(errors[index].StatusCode))
                        {
                            monitoredItem.DisplayName = displayNames[index];
                        }
                        else
                        {
                            monitoredItem.DisplayName = null;
                            _logger.LogWarning("Failed to read display name for '{MonitoredItem}' due to '{StatusCode}'",
                                monitoredItem.StartNodeId, errors[index].StatusCode);
                        }
                        index++;
                    }
                }
            }
            catch (ServiceResultException sre)
            {
                _logger.LogWarning("Failed to resolve display names for monitored items due to '{Message}'",
                    sre.Message);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to resolve display names for monitored items");
                throw;
            }
        }

        /// <summary>
        /// Synchronize monitored items and triggering configuration in subscription
        /// </summary>
        private async Task<bool> SetMonitoredItemsAsync(Subscription rawSubscription,
            IEnumerable<BaseMonitoredItemModel> monitoredItems)
        {
            var currentState = rawSubscription.MonitoredItems
                .Select(m => m.Handle)
                .OfType<OpcUaMonitoredItem>()
                .ToHashSetSafe();

            var noErrorFound = true;
            var count = 0;
            var sessionHandle = _sessions.GetSessionHandle(rawSubscription.Session);
            if (monitoredItems?.Any() != true)
            {
                // cleanup entire subscription
                var toCleanupList = currentState.Select(t => t.Item);
                if (toCleanupList.Any())
                {
                    // Remove monitored items not in desired state
                    _logger.LogTrace("Remove monitored items in subscription "
                        + "'{Name}'/'{SessionId}'...",
                        Name, Connection.CreateConnectionId());
                    foreach (var toRemove in toCleanupList)
                    {
                        _logger.LogTrace("Removing monitored item '{Item}'...",
                            toRemove.StartNodeId);
                        ((OpcUaMonitoredItem)toRemove.Handle).Destroy();
                        count++;
                    }
                    rawSubscription.RemoveItems(toCleanupList);
                    _logger.LogInformation("Removed {Count} monitored items in subscription "
                        + "'{Name}'/'{SessionId}'",
                        count, Name, Connection.CreateConnectionId());
                }
                _currentlyMonitored = ImmutableDictionary<uint, OpcUaMonitoredItem>.Empty;

                // TODO: Set metadata to empty here
                await rawSubscription.ApplyChangesAsync().ConfigureAwait(false);
                rawSubscription.SetPublishingMode(false);
                if (rawSubscription.MonitoredItemCount != 0)
                {
                    _logger.LogWarning("Failed to remove {Count} monitored items from subscription "
                        + "'{Name}'/'{SessionId}'", rawSubscription.MonitoredItemCount,
                        Name, Connection.CreateConnectionId());
                }
                return noErrorFound;
            }

            // Synchronize the desired items with the state of the raw subscription
            var desiredState = monitoredItems
                .Select(m => new OpcUaMonitoredItem(m, _logger))
                .ToHashSetSafe();

            var metadataChanged = false;
            var applyChanges = false;

            var toRemoveList = currentState.Except(desiredState).Select(t => t.Item);
            if (toRemoveList.Any())
            {
                count = 0;
                // Remove monitored items not in desired state
                foreach (var toRemove in toRemoveList)
                {
                    _logger.LogTrace("Removing monitored item '{Item}'...", toRemove.StartNodeId);
                    ((OpcUaMonitoredItem)toRemove.Handle).Destroy();
                    count++;
                }
                rawSubscription.RemoveItems(toRemoveList);
                applyChanges = true;
                metadataChanged = true;

                _logger.LogInformation("Removed {Count} monitored items from subscription "
                    + "'{Name}'/'{SessionId}'", count, Name, Connection.CreateConnectionId());
            }

            // todo re-associate detached handles!?
            var toRemoveDetached = rawSubscription.MonitoredItems.Where(m => m.Status == null);
            if (toRemoveDetached.Any())
            {
                rawSubscription.RemoveItems(toRemoveDetached);
                _logger.LogInformation("Removed {Count} detached monitored items from subscription "
                    + "'{Name}'/'{SessionId}'",
                    toRemoveDetached.Count(),
                    Name,
                    Connection.CreateConnectionId());
            }

            var nowMonitored = new List<OpcUaMonitoredItem>();
            var toAddList = desiredState.Except(currentState);
            if (toAddList.Any())
            {
                count = 0;
                // Add new monitored items not in current state
                _logger.LogTrace("Add monitored items to subscription '{Name}'/'{SessionId}'...",
                    Name, Connection.CreateConnectionId());
                foreach (var toAdd in toAddList)
                {
                    // Create monitored item
                    try
                    {
                        toAdd.Create(rawSubscription.Session, sessionHandle.Codec);
                        if (toAdd.EventTemplate != null)
                        {
                            toAdd.Item.AttributeId = Attributes.EventNotifier;
                        }
                        nowMonitored.Add(toAdd);
                        count++;
                        _logger.LogTrace("Adding new monitored item '{Item}'...",
                            toAdd.Item.StartNodeId);
                    }
                    catch (ServiceResultException sre)
                    {
                        _logger.LogWarning("Failed to add new monitored item '{Item}' due to '{Message}'",
                            toAdd.Template.StartNodeId, sre.Message);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Failed to add new monitored item '{Item}'",
                            toAdd.Template.StartNodeId);
                        throw;
                    }
                }
                rawSubscription.AddItems(
                    toAddList.Where(t => t?.Item != null).Select(t => t.Item).ToList());
                applyChanges = true;
                metadataChanged = true;
                _logger.LogInformation("Added {Count} monitored items to subscription "
                    + "'{Name}'/'{SessionId}'", count, Name, Connection.CreateConnectionId());
            }

            // Update monitored items that have changed
            var desiredUpdates = desiredState.Intersect(currentState)
                .ToDictionary(k => k, v => v);
            count = 0;
            foreach (var toUpdate in currentState.Intersect(desiredState))
            {
                if (toUpdate.MergeWith(rawSubscription.Session?.MessageContext,
                        rawSubscription.Session?.NodeCache, rawSubscription.Session?.TypeTree,
                        sessionHandle.Codec, desiredUpdates[toUpdate], out var metadata))
                {
                    _logger.LogTrace("Updating monitored item '{Item}'...", toUpdate);
                    count++;
                }
                if (metadata)
                {
                    metadataChanged = true;
                }
                nowMonitored.Add(toUpdate);
            }
            if (count > 0)
            {
                applyChanges = true;
                _logger.LogInformation("Updated {Count} monitored items in subscription "
                    + "'{Name}'/'{SessionId}'",
                    count,
                    Name,
                    Connection.CreateConnectionId());
            }

            var currentlyMonitored = _currentlyMonitored;
            if (applyChanges)
            {
                await rawSubscription.ApplyChangesAsync().ConfigureAwait(false);
                _currentlyMonitored = currentlyMonitored
                    = nowMonitored.ToImmutableDictionary(m => m.Item.ClientHandle, m => m);

                // sanity check
                foreach (var monitoredItem in currentlyMonitored.Values)
                {
                    if (monitoredItem.Item.Status.Error != null &&
                        StatusCode.IsNotGood(monitoredItem.Item.Status.Error.StatusCode))
                    {
                        _logger.LogWarning("Error monitoring node {Id} due to {Code} in subscription "
                            + "'{Name}'/'{SessionId}'", monitoredItem.Item.StartNodeId,
                            monitoredItem.Item.Status.Error.StatusCode, Name, Connection.CreateConnectionId());
                        monitoredItem.Template.MonitoringMode = Shared.Models.MonitoringMode.Disabled;
                        noErrorFound = false;
                    }
                }

                count = currentlyMonitored.Values.Count(m => m.Item.Status.Error == null);
                _logger.LogInformation("Now monitoring {Count} nodes in subscription "
                    + "'{Name}'/'{SessionId}'", count, Name, Connection.CreateConnectionId());
                if (currentlyMonitored.Count != rawSubscription.MonitoredItemCount)
                {
                    _logger.LogError("Monitored items mismatch: Monitoring: {Existing} != In Subscription: {Items} ",
                        currentlyMonitored.Count, rawSubscription.MonitoredItemCount);
                }
            }
            else
            {
                // do a sanity check
                foreach (var monitoredItem in currentlyMonitored.Values)
                {
                    if (monitoredItem.Item.Status.MonitoringMode == Opc.Ua.MonitoringMode.Disabled ||
                        (monitoredItem.Item.Status.Error != null &&
                        StatusCode.IsNotGood(monitoredItem.Item.Status.Error.StatusCode)))
                    {
                        monitoredItem.Template.MonitoringMode = Shared.Models.MonitoringMode.Disabled;
                        noErrorFound = false;
                        applyChanges = true;
                    }
                }
                if (applyChanges)
                {
                    await rawSubscription.ApplyChangesAsync().ConfigureAwait(false);
                }
            }

            if (metadataChanged && _subscription.Configuration?.MetaData != null)
            {
                //
                // Use the date time to version across reboots. This could be done more elegantly by
                // saving the last version to persistent storage such as twin, but this is ok for
                // the sake of being able to have an incremental version number defining metadata changes.
                //
                var metaDataVersion = DateTime.UtcNow.ToBinary();
                var major = (uint)(metaDataVersion >> 32);
                var minor = (uint)metaDataVersion;

                _logger.LogInformation("Metadata changed to {Major}.{Minor} for subscription"
                    + "'{Name}'/'{SessionId}'",
                    major, minor, Name, Connection.CreateConnectionId());

                var typeSystem = await sessionHandle.GetComplexTypeSystemAsync().ConfigureAwait(false);
                var dataTypes = new NodeIdDictionary<DataTypeDescription>();
                var fields = new FieldMetaDataCollection();
                foreach (var monitoredItem in currentlyMonitored.Values)
                {
                    monitoredItem.GetMetaData(rawSubscription.Session?.MessageContext,
                        rawSubscription.Session?.NodeCache, rawSubscription.Session?.TypeTree,
                        typeSystem, fields, dataTypes);
                }

                _currentMetaData = new DataSetMetaDataType
                {
                    Name = _subscription.Configuration.MetaData.Name,
                    DataSetClassId = (Uuid)_subscription.Configuration.MetaData.DataSetClassId,
                    Namespaces = rawSubscription.Session?.NamespaceUris.ToArray(),
                    EnumDataTypes = dataTypes.Values.OfType<EnumDescription>().ToArray(),
                    StructureDataTypes = dataTypes.Values.OfType<StructureDescription>().ToArray(),
                    SimpleDataTypes = dataTypes.Values.OfType<SimpleTypeDescription>().ToArray(),
                    Fields = fields,
                    Description = _subscription.Configuration.MetaData.Description,
                    ConfigurationVersion = new ConfigurationVersionDataType
                    {
                        MajorVersion = major,
                        MinorVersion = minor
                    }
                };
            }

            if (currentlyMonitored.Count > 0)
            {
                // Change monitoring mode of all valid items if needed
                var validItems = currentlyMonitored.Values.Where(v => v.Item.Created);
                foreach (var change in validItems.GroupBy(i => i.GetMonitoringModeChange()))
                {
                    if (change.Key == null)
                    {
                        continue;
                    }
                    var changeList = change.ToList();
                    _logger.LogInformation("Set monitoring to {Value} for {Count} items in subscription "
                        + "'{Name}'/'{SessionId}'.",
                        change.Key.Value,
                        change.Count(),
                        Name,
                        Connection.CreateConnectionId());

                    var itemsToChange = changeList.ConvertAll(t => t.Item);
                    var results = rawSubscription.SetMonitoringMode(change.Key.Value, itemsToChange);
                    if (results != null)
                    {
                        var erroneousResultsCount = results
                            .Count(r => r != null && StatusCode.IsNotGood(r.StatusCode));

                        // Check the number of erroneous results and log.
                        if (erroneousResultsCount > 0)
                        {
                            _logger.LogWarning("Failed to set monitoring for {Count} items in subscription "
                                + "'{Name}'/'{SessionId}'.",
                                erroneousResultsCount,
                                Name,
                                Connection.CreateConnectionId());

                            for (var i = 0; i < results.Count && i < itemsToChange.Count; ++i)
                            {
                                if (StatusCode.IsNotGood(results[i].StatusCode))
                                {
                                    _logger.LogWarning("Set monitoring for item '{Item}' in subscription "
                                        + "'{Name}'/'{SessionId}' failed with '{Status}'.",
                                        itemsToChange[i].StartNodeId,
                                        Name,
                                        Connection.CreateConnectionId(),
                                        results[i].StatusCode);
                                    changeList[i].Template.MonitoringMode = Shared.Models.MonitoringMode.Disabled;
                                    changeList[i].Item.MonitoringMode = Opc.Ua.MonitoringMode.Disabled;
                                }
                            }
                            noErrorFound = false;
                        }
                    }
                    if (change.Any(x => x.EventTemplate != null))
                    {
                        _logger.LogInformation("Now issuing ConditionRefresh for item {Item} on subscription " +
                            "{Name}", change.FirstOrDefault()?.Item?.DisplayName ?? "", rawSubscription.DisplayName);
                        try
                        {
                            rawSubscription.ConditionRefresh();
                        }
                        catch (ServiceResultException e)
                        {
                            _logger.LogInformation("ConditionRefresh for item {Item} on subscription " +
                                "{Name} failed with a ServiceResultException '{Message}'",
                                change.FirstOrDefault()?.Item?.DisplayName ?? "", rawSubscription.DisplayName, e.Message);
                            noErrorFound = false;
                        }
                        catch (Exception e)
                        {
                            _logger.LogInformation("ConditionRefresh for item {Item} on subscription " +
                                "{Name} failed with an exception '{Message}'",
                                change.FirstOrDefault()?.Item?.DisplayName ?? "", rawSubscription.DisplayName, e.Message);
                            noErrorFound = false;
                        }
                        if (noErrorFound)
                        {
                            _logger.LogInformation("ConditionRefresh for item {Item} on subscription " +
                                "{Name} has completed",
                                change.FirstOrDefault()?.Item?.DisplayName ?? "", rawSubscription.DisplayName);
                        }
                    }
                }
                foreach (var item in validItems)
                {
                    if (item.Item.SamplingInterval != item.Item.Status.SamplingInterval ||
                        item.Item.QueueSize != item.Item.Status.QueueSize)
                    {
                        _logger.LogInformation(@"Server has revised '{Item}' in '{Name}'/'{SessionId}' 
The item's actual/desired states:
SamplingInterval {CurrentSamplingInterval}/{SamplingInterval},
QueueSize {CurrentQueueSize}/{QueueSize}", item.Item.StartNodeId, _subscription.Id,
                            _subscription.Id.Connection.CreateConnectionId(), item.Item.Status.SamplingInterval,
                            item.Item.SamplingInterval, item.Item.Status.QueueSize, item.Item.QueueSize);
                    }
                }
            }
            return noErrorFound;
        }

        /// <summary>
        /// Helper to calculate greatest common divisor for the parameter of keep alive
        /// count used to allow the trigger of heart beats in a given interval.
        /// </summary>
        private static uint GreatCommonDivisor(uint a, uint b)
        {
            return b == 0 ? a : GreatCommonDivisor(b, a % b);
        }

        /// <summary>
        /// Resets the operation timeout on the session accrding to the publishing intervals on all subscriptions
        /// </summary>
        private void ReapplySessionOperationTimeout(ISession session, Subscription newSubscription)
        {
            if (session == null)
            {
                return;
            }

            var currentOperationTimeout = _config.OperationTimeout;
            var localMaxOperationTimeout =
                newSubscription.PublishingInterval * (int)newSubscription.KeepAliveCount;
            if (currentOperationTimeout < localMaxOperationTimeout)
            {
                currentOperationTimeout = localMaxOperationTimeout;
            }

            foreach (var subscription in session.Subscriptions)
            {
                localMaxOperationTimeout =
                    (int)subscription.CurrentPublishingInterval * (int)subscription.CurrentKeepAliveCount;
                if (currentOperationTimeout < localMaxOperationTimeout)
                {
                    currentOperationTimeout = localMaxOperationTimeout;
                }
            }
            if (session.OperationTimeout != currentOperationTimeout)
            {
                session.OperationTimeout = currentOperationTimeout;
            }
        }

        /// <summary>
        /// Get a subscription with the supplied configuration (no lock)
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        private Subscription GetInnerSubscription(ISession session)
        {
            // TODO propagate the default PublishingInterval currently only avaliable for standalone mode
            var configuredPublishingInterval = (int)(_subscription.Configuration?.PublishingInterval)
                .GetValueOrDefault(TimeSpan.FromSeconds(1)).TotalMilliseconds;
            var normedPublishingInterval = (uint)(configuredPublishingInterval > 0 ? configuredPublishingInterval : 1);

            // calculate the KeepAliveCount no matter what, perhaps monitored items were changed
            var revisedKeepAliveCount = (_subscription.Configuration?.KeepAliveCount)
?? _config.MaxKeepAliveCount;

            _subscription.MonitoredItems?.ForEach(m =>
            {
                if (m is DataMonitoredItemModel dataItem)
                {
                    var heartbeat = (uint)(dataItem?.HeartbeatInterval).GetValueOrDefault(TimeSpan.Zero).TotalMilliseconds;
                    if (heartbeat != 0)
                    {
                        var itemKeepAliveCount = heartbeat / normedPublishingInterval;
                        revisedKeepAliveCount = GreatCommonDivisor(revisedKeepAliveCount, itemKeepAliveCount);
                    }
                }
            });

            var configuredMaxNotificationsPerPublish = session.DefaultSubscription.MaxNotificationsPerPublish;
            var configuredLifetimeCount = (_subscription.Configuration?.LifetimeCount)
?? session.DefaultSubscription.LifetimeCount;

            var configuredPriority = (_subscription.Configuration?.Priority)
?? session.DefaultSubscription.Priority;

            var subscription = session.Subscriptions.SingleOrDefault(s => s.Handle == this);
            if (subscription == null)
            {
                subscription = new Subscription(session.DefaultSubscription)
                {
                    Handle = this,
                    DisplayName = Name,
                    PublishingEnabled = false, // false on initialization
                    KeepAliveCount = revisedKeepAliveCount,
                    PublishingInterval = configuredPublishingInterval,
                    MaxNotificationsPerPublish = configuredMaxNotificationsPerPublish,
                    LifetimeCount = configuredLifetimeCount,
                    Priority = configuredPriority,
                    SequentialPublishing = true, // TODO: Make configurable
                    DisableMonitoredItemCache = false,
                    FastDataChangeCallback = OnSubscriptionDataChangeNotification,
                    FastEventCallback = OnSubscriptionEventNotificationList,
                };

                ReapplySessionOperationTimeout(session, subscription);

                var result = session.AddSubscription(subscription);
                if (!result)
                {
                    _logger.LogError("Failed to add subscription '{Name}' to session:'{SessionId}'",
                        _subscription.Id,
                        _subscription.Id.Connection.CreateConnectionId());
                    return null;
                }

                subscription.Create();

                if (!subscription.Created)
                {
                    _logger.LogError("Failed to create subscription '{Name}'/'{SessionId}'",
                        _subscription.Id, _subscription.Id.Connection.CreateConnectionId());
                    return null;
                }
                LogRevisedValues(subscription, true);
            }
            else
            {
                // Apply new configuration on configuration on original subscription
                var modifySubscription = false;

                if (revisedKeepAliveCount != subscription.KeepAliveCount)
                {
                    _logger.LogDebug(
                        "Subscription '{Name}'/'{SessionId}' change KeepAliveCount to {New}",
                        _subscription.Id,
                        _subscription.Id.Connection.CreateConnectionId(),
                        revisedKeepAliveCount);

                    subscription.KeepAliveCount = revisedKeepAliveCount;
                    modifySubscription = true;
                }
                if (subscription.PublishingInterval != configuredPublishingInterval)
                {
                    _logger.LogDebug(
                        "Subscription '{Name}'/'{SessionId}' change publishing interval to {New}",
                        _subscription.Id,
                        _subscription.Id.Connection.CreateConnectionId(),
                        configuredPublishingInterval);
                    subscription.PublishingInterval = configuredPublishingInterval;
                    modifySubscription = true;
                }

                if (subscription.MaxNotificationsPerPublish != configuredMaxNotificationsPerPublish)
                {
                    _logger.LogDebug(
                        "Subscription '{Name}'/'{SessionId}' change MaxNotificationsPerPublish to {New}",
                        _subscription.Id,
                        _subscription.Id.Connection.CreateConnectionId(),
                        configuredMaxNotificationsPerPublish);
                    subscription.MaxNotificationsPerPublish = configuredMaxNotificationsPerPublish;
                    modifySubscription = true;
                }

                if (subscription.LifetimeCount != configuredLifetimeCount)
                {
                    _logger.LogDebug(
                        "Subscription '{Name}'/'{SessionId}' change LifetimeCount to {New}",
                        _subscription.Id,
                        _subscription.Id.Connection.CreateConnectionId(),
                        configuredLifetimeCount);
                    subscription.LifetimeCount = configuredLifetimeCount;
                    modifySubscription = true;
                }
                if (subscription.Priority != configuredPriority)
                {
                    _logger.LogDebug("Subscription '{Name}'/'{SessionId}' change Priority to {New}",
                        _subscription.Id,
                        _subscription.Id.Connection.CreateConnectionId(),
                        configuredPriority);
                    subscription.Priority = configuredPriority;
                    modifySubscription = true;
                }
                if (modifySubscription)
                {
                    subscription.Modify();
                    LogRevisedValues(subscription, false);
                }
            }
            return subscription;
        }

        /// <summary>
        /// Log revised values of the subscription
        /// </summary>
        /// <param name="subscription"></param>
        /// <param name="created"></param>
        private void LogRevisedValues(Subscription subscription, bool created)
        {
            _logger.LogInformation(@"Successfully {Action} subscription '{Subscription}'/'{SessionId}'.
Actual (revised) state/desired state:
# PublishingEnabled {CurrentPublishingEnabled}/{PublishingEnabled}
# PublishingInterval {CurrentPublishingInterval}/{PublishingInterval}
# KeepAliveCount {CurrentKeepAliveCount}/{KeepAliveCount}
# LifetimeCount {CurrentLifetimeCount}/{LifetimeCount}", created ? "created" : "modified",
                _subscription.Id, _subscription.Id.Connection.CreateConnectionId(),
                subscription.CurrentPublishingEnabled, subscription.PublishingEnabled,
                subscription.CurrentPublishingInterval, subscription.PublishingInterval,
                subscription.CurrentKeepAliveCount, subscription.KeepAliveCount,
                subscription.CurrentLifetimeCount, subscription.LifetimeCount);
        }

        /// <summary>
        /// Handle event notification. Depending on the sequential publishing setting
        /// this will be called in order and thread safe or from different threads.
        /// </summary>
        /// <param name="subscription"></param>
        /// <param name="notification"></param>
        /// <param name="stringTable"></param>
        private void OnSubscriptionEventNotificationList(Subscription subscription,
            EventNotificationList notification, IList<string> stringTable)
        {
            var onSubscriptionEventChange = OnSubscriptionEventChange;
            if (onSubscriptionEventChange == null)
            {
                return;
            }

            if (subscription == null)
            {
                _logger.LogWarning(
                    "EventChange for subscription: Subscription is null");
                return;
            }

            if (notification?.Events == null)
            {
                _logger.LogWarning(
                    "EventChange for subscription: {Subscription} having empty notification",
                    subscription.DisplayName);
                return;
            }

            if (notification.Events.Count == 0)
            {
                _logger.LogWarning(
                    "EventChange for subscription: {Subscription} having no events",
                    subscription.DisplayName);
                return;
            }
            try
            {
                // check if notification is a keep alive
                var isKeepAlive = notification.Events[0].ClientHandle == 0
                    && notification.Events[0].Message?.NotificationData?.Count == 0;
                if (isKeepAlive)
                {
                    var sequenceNumber = notification.Events[0].Message.SequenceNumber;
                    var publishTime = notification.Events[0].Message.PublishTime;

                    // in case of a keepalive,the sequence number is not incremented by the servers
                    _logger.LogInformation("Keep alive for subscription '{Name}'/'{SessionId}' " +
                        "with sequenceNumber {SequenceNumber}, publishTime {PublishTime} on Event callback.",
                        Name, Connection.CreateConnectionId(), sequenceNumber, publishTime);

                    var message = new SubscriptionNotificationModel
                    {
                        ServiceMessageContext = subscription?.Session?.MessageContext,
                        ApplicationUri = subscription?.Session?.Endpoint?.Server?.ApplicationUri,
                        EndpointUrl = subscription?.Session?.Endpoint?.EndpointUrl,
                        SequenceNumber = SequenceNumber.Increment32(ref _sequenceNumber),
                        SubscriptionName = Name,
                        Timestamp = publishTime,
                        SubscriptionId = Id,
                        MessageType = Encoders.PubSub.MessageType.KeepAlive,
                        MetaData = _currentMetaData
                    };
                    onSubscriptionEventChange.Invoke(this, message);
                }
                else
                {
                    var numOfEvents = 0;
                    var missingSequenceNumbers = Array.Empty<uint>();
                    for (var i = 0; i < notification.Events.Count; i++)
                    {
                        var eventNotification = notification.Events[i];
                        Debug.Assert(eventNotification != null);

                        var monitoredItem = subscription.FindItemByClientHandle(eventNotification.ClientHandle);
                        var sequenceNumber = eventNotification.Message.SequenceNumber;

                        if (i == 0 && !SequenceNumber.Validate(sequenceNumber, ref _lastSequenceNumber,
                            out missingSequenceNumbers, out var dropped))
                        {
                            _logger.LogWarning("Event for monitored item {ClientHandle} subscription " +
                                "'{Name}'/'{SessionId}' has unexpected sequenceNumber {SequenceNumber} " +
                                "missing {ExpectedSequenceNumber} which were {Dropped}, publishTime {PublishTime}",
                                eventNotification.ClientHandle, Name, Connection.CreateConnectionId(), sequenceNumber,
                                SequenceNumber.ToString(missingSequenceNumbers), dropped ? "dropped" : "already received",
                                eventNotification.Message.PublishTime);
                        }

                        _logger.LogTrace("Event for subscription: {Subscription}, sequence#: " +
                            "{Sequence} isKeepAlive: {KeepAlive}, publishTime: {PublishTime}",
                            subscription.DisplayName, sequenceNumber, isKeepAlive, eventNotification.Message.PublishTime);

                        if (monitoredItem?.Handle is OpcUaMonitoredItem wrapper)
                        {
                            var message = new SubscriptionNotificationModel
                            {
                                ServiceMessageContext = subscription?.Session?.MessageContext,
                                ApplicationUri = subscription?.Session?.Endpoint?.Server?.ApplicationUri,
                                EndpointUrl = subscription?.Session?.Endpoint?.EndpointUrl,
                                SubscriptionName = Name,
                                SubscriptionId = Id,
                                SequenceNumber = SequenceNumber.Increment32(ref _sequenceNumber),
                                MessageType = Encoders.PubSub.MessageType.Event,
                                MetaData = _currentMetaData,
                                Timestamp = eventNotification.Message.PublishTime,
                                Notifications = new List<MonitoredItemNotificationModel>(),
                            };

                            wrapper.ProcessEventNotification(message, eventNotification);

                            if (message.Notifications.Count > 0)
                            {
                                onSubscriptionEventChange.Invoke(this, message);
                                numOfEvents++;
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Monitored item not found with client handle {ClientHandle} " +
                                "for Event received for subscription '{Name}'/'{SessionId}' + " +
                                "{SequenceNumber}, publishTime {PublishTime}",
                                eventNotification.ClientHandle, Name, Connection.CreateConnectionId(),
                                sequenceNumber, eventNotification.Message.PublishTime);
                        }
                    }
                    var onSubscriptionEventDiagnosticsChange = OnSubscriptionEventDiagnosticsChange;
                    onSubscriptionEventDiagnosticsChange?.Invoke(this, numOfEvents);
                }
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Exception processing subscription notification");
            }
        }

        /// <summary>
        /// Handle data change notification. Depending on the sequential publishing setting
        /// this will be called in order and thread safe or from different threads.
        /// </summary>
        /// <param name="subscription"></param>
        /// <param name="notification"></param>
        /// <param name="stringTable"></param>
        private void OnSubscriptionDataChangeNotification(Subscription subscription,
            DataChangeNotification notification, IList<string> stringTable)
        {
            var onSubscriptionDataChange = OnSubscriptionDataChange;
            if (onSubscriptionDataChange == null)
            {
                return;
            }
            if (notification?.MonitoredItems == null || notification.MonitoredItems.Count == 0)
            {
                _logger.LogWarning(
                    "DataChange for subscription '{Name}'/'{SessionId}' has empty notification",
                    Name, Connection.CreateConnectionId());
                return;
            }
            try
            {
                // check if notification is a keep alive
                var isKeepAlive = notification.MonitoredItems.Count == 1
                    && notification.MonitoredItems[0].ClientHandle == 0
                    && notification.MonitoredItems[0].Message?.NotificationData?.Count == 0;

                SubscriptionNotificationModel message;
                if (isKeepAlive)
                {
                    var sequenceNumber = notification.MonitoredItems[0].Message.SequenceNumber;
                    var publishTime = notification.MonitoredItems[0].Message.PublishTime;

                    // in case of a keepalive,the sequence number is not incremented by the servers
                    _logger.LogInformation("Keep alive for subscription '{Name}'/'{SessionId}' " +
                        "with sequenceNumber {SequenceNumber}, publishTime {PublishTime} on data change.",
                        Name, Connection.CreateConnectionId(), sequenceNumber, publishTime);

                    message = new SubscriptionNotificationModel
                    {
                        ServiceMessageContext = subscription?.Session?.MessageContext,
                        ApplicationUri = subscription?.Session?.Endpoint?.Server?.ApplicationUri,
                        EndpointUrl = subscription?.Session?.Endpoint?.EndpointUrl,
                        SubscriptionName = Name,
                        Timestamp = publishTime,
                        SubscriptionId = Id,
                        SequenceNumber = SequenceNumber.Increment32(ref _sequenceNumber),
                        MessageType = Encoders.PubSub.MessageType.KeepAlive,
                        MetaData = _currentMetaData,
                        Notifications = new List<MonitoredItemNotificationModel>(),
                    };
                }
                else
                {
                    message = new SubscriptionNotificationModel
                    {
                        ServiceMessageContext = subscription?.Session?.MessageContext,
                        ApplicationUri = subscription?.Session?.Endpoint?.Server?.ApplicationUri,
                        EndpointUrl = subscription?.Session?.Endpoint?.EndpointUrl,
                        SubscriptionName = Name,
                        SubscriptionId = Id,
                        MessageType = Encoders.PubSub.MessageType.DeltaFrame,
                        MetaData = _currentMetaData,
                        SequenceNumber = SequenceNumber.Increment32(ref _sequenceNumber),
                        Notifications = new List<MonitoredItemNotificationModel>(),
                    };
                    var missingSequenceNumbers = Array.Empty<uint>();
                    for (var i = 0; i < notification.MonitoredItems.Count; i++)
                    {
                        Debug.Assert(notification?.MonitoredItems != null);
                        var item = notification.MonitoredItems[i];
                        Debug.Assert(item != null);

                        var monitoredItem = subscription.FindItemByClientHandle(item.ClientHandle);
                        var sequenceNumber = item.Message.SequenceNumber;
                        message.Timestamp = item.Message.PublishTime;

                        // All notifications have the same message and thus sequence number
                        if (i == 0 && !SequenceNumber.Validate(sequenceNumber, ref _lastSequenceNumber,
                            out missingSequenceNumbers, out var dropped))
                        {
                            _logger.LogWarning("DataChange for monitored item {ClientHandle} subscription " +
                                "'{Name}'/'{SessionId}' has unexpected sequenceNumber {SequenceNumber} " +
                                "missing {ExpectedSequenceNumber} which were {Dropped}, publishTime {PublishTime}",
                                item.ClientHandle, Name, Connection.CreateConnectionId(), sequenceNumber,
                                SequenceNumber.ToString(missingSequenceNumbers),
                                dropped ? "dropped" : "already received", item.Message.PublishTime);
                        }

                        _logger.LogTrace("Data change for subscription: {Subscription}, sequence#: " +
                            "{Sequence} isKeepAlive: {KeepAlive}, publishTime: {PublishTime}",
                            subscription.DisplayName, sequenceNumber, isKeepAlive, message.Timestamp);

                        if (monitoredItem?.Handle is OpcUaMonitoredItem wrapper)
                        {
                            wrapper.ProcessMonitoredItemNotification(message, item);
                        }
                        else
                        {
                            _logger.LogWarning("Monitored item not found with client handle {ClientHandle} " +
                                "for DataChange received for subscription '{Name}'/'{SessionId}' + " +
                                "{SequenceNumber}, publishTime {PublishTime}",
                                item.ClientHandle, Name, Connection.CreateConnectionId(),
                                sequenceNumber, message.Timestamp);
                        }
                    }
                }

                // add a heartbeat for monitored items that did not receive a datachange notification
                var currentlyMonitored = _currentlyMonitored.ToBuilder();
                currentlyMonitored.RemoveRange(notification.MonitoredItems.Select(m => m.ClientHandle));
                foreach (var wrapper in currentlyMonitored.Values)
                {
                    wrapper.ProcessMonitoredItemNotification(message, null);
                }

                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    var erroneousNotifications = message.Notifications?
                        .Where(n => n.Value.Value == null
                            || StatusCode.IsNotGood(n.Value.StatusCode))
                        .ToList();

                    if (erroneousNotifications.Count > 0)
                    {
                        _logger.LogDebug("Found {Count} notifications with null value or not good status "
                            + "code for '{Subscription}'/'{SessionId}' subscription.",
                            erroneousNotifications.Count, Name, Connection.CreateConnectionId());
                    }
                }

                onSubscriptionDataChange.Invoke(this, message);
                Debug.Assert(message.Notifications != null);
                var onSubscriptionDataDiagnosticsChange = OnSubscriptionDataDiagnosticsChange;
                if (message.Notifications.Count > 0 && onSubscriptionDataDiagnosticsChange != null)
                {
                    onSubscriptionDataDiagnosticsChange.Invoke(this, message.Notifications.Count);
                }
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Exception processing subscription notification");
            }
        }

        private int NumberOfGoodNodes
            => _currentlyMonitored.Values.Count(x => StatusCode.IsGood(x.Status));
        private int NumberOfBadNodes
            => _currentlyMonitored.Values.Count(x => StatusCode.IsBad(x.Status));

        /// <summary>
        /// Create observable metrics
        /// </summary>
        /// <param name="metrics"></param>
        public OpcUaSubscription(IMetricsContext metrics)
        {
            Diagnostics.Meter_CreateObservableUpDownCounter("iiot_edge_publisher_good_nodes",
                () => new Measurement<long>(NumberOfGoodNodes, metrics.TagList), "Monitored items",
                "Monitored items successfully created..");
            Diagnostics.Meter_CreateObservableUpDownCounter("iiot_edge_publisher_bad_nodes",
                () => new Measurement<long>(NumberOfBadNodes, metrics.TagList), "Monitored items",
                "Monitored items with errors.");
            Diagnostics.Meter_CreateObservableUpDownCounter("iiot_edge_publisher_monitored_items",
                () => new Measurement<long>(_currentlyMonitored.Count, metrics.TagList), "Monitored items",
                "Monitored item count.");
            _metrics = metrics;
        }

        private ImmutableDictionary<uint, OpcUaMonitoredItem> _currentlyMonitored;
        private SubscriptionModel _subscription;
        private readonly ISessionProvider<ConnectionModel> _sessions;
        private readonly IClientServicesConfig _config;
        private readonly ILogger _logger;
        private readonly IMetricsContext _metrics;
        private readonly SemaphoreSlim _lock;
        private DataSetMetaDataType _currentMetaData;
        private uint _lastSequenceNumber;
        private uint _sequenceNumber;
        private bool _closed;
        private static uint _lastIndex;
    }
}
