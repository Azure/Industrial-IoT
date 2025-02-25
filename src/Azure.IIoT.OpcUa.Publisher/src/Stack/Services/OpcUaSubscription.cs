// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Services
{
    using Azure.IIoT.OpcUa.Publisher.Stack.Extensions;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Encoders.PubSub;
    using Furly.Extensions.Utils;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Opc.Ua;
    using Opc.Ua.Client;
    using Opc.Ua.Client.ComplexTypes;
    using Opc.Ua.Extensions;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Metrics;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Text;

    /// <summary>
    /// Subscription implementation
    /// </summary>
    [DataContract(Namespace = OpcUaClient.Namespace)]
    [KnownType(typeof(OpcUaMonitoredItem))]
    [KnownType(typeof(OpcUaMonitoredItem.DataChange))]
    [KnownType(typeof(OpcUaMonitoredItem.CyclicRead))]
    [KnownType(typeof(OpcUaMonitoredItem.Heartbeat))]
    [KnownType(typeof(OpcUaMonitoredItem.ModelChangeEventItem))]
    [KnownType(typeof(OpcUaMonitoredItem.Event))]
    [KnownType(typeof(OpcUaMonitoredItem.Condition))]
    internal sealed class OpcUaSubscription : Subscription, IAsyncDisposable,
        IEquatable<OpcUaSubscription>
    {
        /// <summary>
        /// Template for subscription
        /// </summary>
        public SubscriptionModel Template { get; private set; }

        /// <summary>
        /// Is root subscription
        /// </summary>
        public bool IsRoot => _parentId == null;

        /// <summary>
        /// Unique subscription identifier in the process
        /// </summary>
        public uint SubscriptionId { get; }

        /// <summary>
        /// The name of the subscription
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Whether the subscription is online
        /// </summary>
        internal bool IsOnline
            => Session?.Connected == true && !IsClosed;

        /// <summary>
        /// Whether subscription is closed
        /// </summary>
        internal bool IsClosed
            => _disposed || Session == null;

        /// <summary>
        /// Currently monitored but unordered
        /// </summary>
        private IEnumerable<OpcUaMonitoredItem> CurrentlyMonitored
            => MonitoredItems.OfType<OpcUaMonitoredItem>();

        public byte DesiredPriority
            => Template.Priority
            ?? Session?.DefaultSubscription?.Priority
            ?? 0;

        public uint DesiredMaxNotificationsPerPublish
            => Template.MaxNotificationsPerPublish
            ?? Session?.DefaultSubscription?.MaxNotificationsPerPublish
            ?? 0;

        public uint DesiredLifetimeCount
            => Template.LifetimeCount
            ?? _options.Value.DefaultLifeTimeCount
            ?? Session?.DefaultSubscription?.LifetimeCount
            ?? 0;

        public uint DesiredKeepAliveCount
            => Template.KeepAliveCount
            ?? _options.Value.DefaultKeepAliveCount
            ?? Session?.DefaultSubscription?.KeepAliveCount
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

        public bool EnableSequentialPublishing
            => Template.EnableSequentialPublishing
            ?? _options.Value.EnableSequentialPublishing
            ?? true;

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
        /// Subscription
        /// </summary>
        /// <param name="client"></param>
        /// <param name="template"></param>
        /// <param name="options"></param>
        /// <param name="loggerFactory"></param>
        /// <param name="metrics"></param>
        /// <param name="parentId"></param>
        /// <param name="timeProvider"></param>
        internal OpcUaSubscription(OpcUaClient client, SubscriptionModel template,
            IOptions<OpcUaSubscriptionOptions> options, ILoggerFactory loggerFactory,
            IMetricsContext metrics, uint? parentId = null, TimeProvider? timeProvider = null)
        {
            _client = client;
            _options = options;
            _loggerFactory = loggerFactory;
            _metrics = metrics;
            _parentId = parentId;
            _timeProvider = timeProvider ?? TimeProvider.System;

            Template = template;
            Name = Template.CreateSubscriptionId();
            SubscriptionId = Opc.Ua.SequenceNumber.Increment32(ref _lastIndex);

            _logger = _loggerFactory.CreateLogger<OpcUaSubscription>();

            Initialize();
            _keepAliveWatcher = _timeProvider.CreateTimer(OnKeepAliveMissing, null,
                Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            _monitoredItemWatcher = _timeProvider.CreateTimer(OnMonitoredItemWatchdog, null,
                Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

            InitializeMetrics();
            _client.OnSubscriptionCreated(this);

            ResetMonitoredItemWatchdogTimer(PublishingEnabled);
        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="subscription"></param>
        /// <param name="copyEventHandlers"></param>
        private OpcUaSubscription(OpcUaSubscription subscription, bool copyEventHandlers)
            : base(subscription, copyEventHandlers)
        {
            _options = subscription._options;
            _loggerFactory = subscription._loggerFactory;
            _timeProvider = subscription._timeProvider;
            _metrics = subscription._metrics;
            _firstDataChangeReceived = subscription._firstDataChangeReceived;

            Template = subscription.Template;
            Name = subscription.Name;

            SubscriptionId = subscription.SubscriptionId;
            _parentId = subscription._parentId;

            _client = subscription._client;
            _logger = subscription._logger;
            _sequenceNumber = subscription._sequenceNumber;

            _goodMonitoredItems = subscription._goodMonitoredItems;
            _badMonitoredItems = subscription._badMonitoredItems;
            _lateMonitoredItems = subscription._lateMonitoredItems;
            _reportingItems = subscription._reportingItems;
            _disabledItems = subscription._disabledItems;
            _samplingItems = subscription._samplingItems;
            _notAppliedItems = subscription._notAppliedItems;

            _missingKeepAlives = subscription._missingKeepAlives;
            _unassignedNotifications = subscription._unassignedNotifications;
            _continuouslyMissingKeepAlives = subscription._continuouslyMissingKeepAlives;

            Initialize();
            _keepAliveWatcher = _timeProvider.CreateTimer(OnKeepAliveMissing, null,
                Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            _monitoredItemWatcher = _timeProvider.CreateTimer(OnMonitoredItemWatchdog, null,
                Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

            InitializeMetrics();
            _client.OnSubscriptionCreated(this);
        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="subscription"></param>
        /// <param name="parentId"></param>
        private OpcUaSubscription(OpcUaSubscription subscription, uint parentId)
        {
            _options = subscription._options;
            _loggerFactory = subscription._loggerFactory;
            _timeProvider = subscription._timeProvider;
            _client = subscription._client;
            _metrics = subscription._metrics;
            _parentId = parentId;

            Template = subscription.Template;
            Name = subscription.Name;

            SubscriptionId = Opc.Ua.SequenceNumber.Increment32(ref _lastIndex);
            _logger = _loggerFactory.CreateLogger<OpcUaSubscription>();

            Initialize();

            _keepAliveWatcher = _timeProvider.CreateTimer(OnKeepAliveMissing, null,
                Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            _monitoredItemWatcher = _timeProvider.CreateTimer(OnMonitoredItemWatchdog, null,
                Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

            InitializeMetrics();
            ResetMonitoredItemWatchdogTimer(PublishingEnabled);
        }

        /// <inheritdoc/>
        public override object Clone()
        {
            return new OpcUaSubscription(this, true);
        }

        /// <inheritdoc/>
        public override Subscription CloneSubscription(bool copyEventHandlers)
        {
            return new OpcUaSubscription(this, copyEventHandlers);
        }

        /// <inheritdoc/>
        public override string? ToString()
        {
            var sb = new StringBuilder()
                .Append(Id)
                .Append(':')
                .Append(SubscriptionId)
                .Append(':');
            if (_parentId != null)
            {
                sb = sb
                    .Append(_parentId.Value)
                    .Append("->");
            }
            sb = sb.Append(Name);
            if (_childId != null)
            {
                sb = sb
                    .Append("->")
                    .Append(_childId.Value);
            }
            return sb.ToString();
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj is OpcUaSubscription subscription)
            {
                return subscription.Template.Equals(Template) &&
                    subscription.SubscriptionId == SubscriptionId;
            }
            return false;
        }

        /// <inheritdoc/>
        public bool Equals(OpcUaSubscription? other)
        {
            if (other is null)
            {
                return false;
            }
            return
                other.Template.Equals(Template) &&
                other.SubscriptionId == SubscriptionId;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return Template.GetHashCode();
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                try
                {
                    ResetMonitoredItemWatchdogTimer(false);
                    _keepAliveWatcher.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

                    FastDataChangeCallback = null;
                    FastEventCallback = null;
                    FastKeepAliveCallback = null;

                    PublishStatusChanged -= OnPublishStatusChange;
                    StateChanged -= OnStateChange;

                    var items = CurrentlyMonitored.ToList();
                    if (items.Count != 0)
                    {
                        //
                        // When the entire session is disposed and recreated we must
                        // still dispose all monitored items that are remaining
                        //
                        items.ForEach(item => item.Dispose());
                        RemoveItems(MonitoredItems);
                        Debug.Assert(!CurrentlyMonitored.Any());

                        _logger.LogInformation(
                            "Disposed Subscription {Subscription} with {Count)} items.",
                            this, items.Count);
                    }
                    else
                    {
                        _logger.LogInformation("Disposed Subscription {Subscription}.",
                            this);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Disposing Subscription {Subscription} encountered error.", this);

                    // Eat the error
                }
                finally
                {
                    _disposed = true;
                    _keepAliveWatcher.Dispose();
                    _monitoredItemWatcher.Dispose();
                    _meter.Dispose();

                    Handle = null;
                }
            }
            base.Dispose(disposing);
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            //
            // Called by the management thread to "close" the subscription and dispose it.
            // Note that the session calls dispose again or when it is closed or
            // reconnected. This here is called when the management thread determines
            // to gracefully close the subscription.
            //
            try
            {
                // first close the children
                var child = GetChildSubscription();
                if (child != null)
                {
                    await child.DisposeAsync().ConfigureAwait(false);
                }

                if (IsClosed)
                {
                    return;
                }

                Debug.Assert(Session != null);

                ResetKeepAliveTimer();
                ResetMonitoredItemWatchdogTimer(false);

                // Does not throw
                await CloseCurrentSubscriptionAsync().ConfigureAwait(false);

                _logger.LogInformation("Closed Subscription {Subscription}.", this);
                Debug.Assert(Session == null);
            }
            finally
            {
                Dispose();
            }
        }

        /// <summary>
        /// Try get the current position in the out stream. This is called
        /// on all subscriptions in the session and takes child subscriptions
        /// into account
        /// </summary>
        /// <param name="subscriptionId"></param>
        /// <param name="sequenceNumber"></param>
        /// <returns></returns>
        internal bool TryGetCurrentPosition(out uint subscriptionId, out uint sequenceNumber)
        {
            subscriptionId = Id;
            sequenceNumber = _currentSequenceNumber;
            return UseDeferredAcknoledgements;
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
            foreach (var item in CurrentlyMonitored)
            {
                item.NotifySessionConnectionState(disconnected);
            }
        }

        /// <summary>
        /// Create a keep alive message
        /// </summary>
        /// <returns></returns>
        internal OpcUaSubscriptionNotification? CreateKeepAlive()
        {
            Debug.Assert(IsRoot);
            if (IsClosed)
            {
                _logger.LogError("Subscription {Subscription} closed!", this);
                return null;
            }
            try
            {
                var session = Session;
                if (session == null)
                {
                    return null;
                }
                return new OpcUaSubscriptionNotification(this, session.MessageContext,
                    Array.Empty<MonitoredItemNotificationModel>(), _timeProvider)
                {
                    ApplicationUri = session.Endpoint.Server.ApplicationUri,
                    EndpointUrl = session.Endpoint.EndpointUrl,
                    SequenceNumber = Opc.Ua.SequenceNumber.Increment32(ref _sequenceNumber),
                    MessageType = MessageType.KeepAlive
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to create keep alive for subscription {Subscription}.", this);
                return null;
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
            Debug.Assert(IsRoot);
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
            Debug.Assert(IsRoot);
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
            Debug.Assert(IsRoot);
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
            Debug.Assert(IsRoot);
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
            Debug.Assert(IsRoot);
            return GetAllMonitoredItems().Count(r => r is OpcUaMonitoredItem.Condition h
                && h.Owner == owner && h.TimerEnabled);
        }

        /// <summary>
        /// Collect metadata for the subscriber across this and all child
        /// subscriptions
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="dataSetFieldContentMask"></param>
        /// <param name="dataSetMetaData"></param>
        /// <param name="minorVersion"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="ServiceResultException"></exception>
        internal async ValueTask<PublishedDataSetMetaDataModel> CollectMetaDataAsync(
            ISubscriber owner, DataSetFieldContentFlags? dataSetFieldContentMask,
            DataSetMetaDataModel dataSetMetaData, uint minorVersion, CancellationToken ct)
        {
            Debug.Assert(IsRoot);
            if (Session is not OpcUaSession session)
            {
                throw ServiceResultException.Create(StatusCodes.BadSessionIdInvalid,
                    "Session not connected.");
            }

            var typeSystem = await session.GetComplexTypeSystemAsync(ct).ConfigureAwait(false);
            var dataTypes = new NodeIdDictionary<object>();
            var fields = new List<PublishedFieldMetaDataModel>();

            await CollectMetaDataAsync(owner, session, typeSystem, dataTypes, fields,
                ct).ConfigureAwait(false);

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

        /// <summary>
        /// Update subscription configuration and apply changes later during
        /// synchronization. This is used when the subscription is owned by a
        /// single subscriber and the configuration is updated.
        /// </summary>
        /// <param name="template"></param>
        internal void Update(SubscriptionModel template)
        {
            // Debug.Assert(IsRoot); -- called recursively down to all children.

            Template = template;
            Name = Template.CreateSubscriptionId();

            GetChildSubscription()?.Update(template);
        }

        /// <summary>
        /// Create or update the subscription now using the currently configured
        /// subscription configuration template.
        /// </summary>
        /// <param name="maxMonitoredItemsPerSubscription"></param>
        /// <param name="limits"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="ServiceResultException"></exception>
        internal async ValueTask<TimeSpan> SyncAsync(uint? maxMonitoredItemsPerSubscription,
            OperationLimitsModel limits, CancellationToken ct)
        {
            Debug.Assert(IsRoot);
            if (_disposed)
            {
                throw ServiceResultException.Create(StatusCodes.BadUnexpectedError,
                    "Subscription was disposed.");
            }

            var maxMonitoredItems = maxMonitoredItemsPerSubscription ?? 0u;
            if (maxMonitoredItems <= 0)
            {
                maxMonitoredItems = _options.Value.MaxMonitoredItemPerSubscription
                    ?? kMaxMonitoredItemPerSubscriptionDefault;
            }

            Debug.Assert(Session != null);
            if (Session is not OpcUaSession session)
            {
                throw ServiceResultException.Create(StatusCodes.BadUnexpectedError,
                    "Session not of expected type.");
            }

            var retryDelay = Timeout.InfiniteTimeSpan;

            // Force recreate all subscriptions in the chain if needed
            await ForceRecreateIfNeededAsync(session).ConfigureAwait(false);

            // Parition the monitored items across subscriptions
            var partitions = Partition.Create(_client.GetSubscribers(Template),
                maxMonitoredItems, _options.Value);

            var subscriptionPartition = this; // The root is the default
            for (var partitionIdx = 0; partitionIdx < partitions.Count; partitionIdx++)
            {
                // Synchronize the subscription of this partition
                await subscriptionPartition.SynchronizeSubscriptionAsync(
                    ct).ConfigureAwait(false);

                // Add partitioned items
                var partition = partitions[partitionIdx];
                var delay = await subscriptionPartition.SynchronizeMonitoredItemsAsync(
                    partition, limits, ct).ConfigureAwait(false);
                if (retryDelay > delay)
                {
                    retryDelay = delay;
                }

                if (partitionIdx == partitions.Count - 1)
                {
                    break;
                }

                // Get or create a child subscription
                subscriptionPartition = subscriptionPartition.GetChildSubscription(true);
                if (subscriptionPartition == null)
                {
                    throw ServiceResultException.Create(StatusCodes.BadUnexpectedError,
                        "Failed to create child subscription.");
                }
            }

            //
            // subscription now is the tail or head subscription. We remove
            // all child subscriptions below it as they are not needed anymore.
            //
            var tail = subscriptionPartition;
            while (tail != null)
            {
                tail = tail.GetChildSubscription();
                if (tail != null)
                {
                    await tail.DisposeAsync().ConfigureAwait(false);
                }
            }
            // Snip off here
            subscriptionPartition._childId = null;

            // Force finalize all subscriptions in the (new) chain if needed
            await FinalizeSyncAsync(ct).ConfigureAwait(false);

            return retryDelay;
        }

        /// <summary>
        /// Force recreate all subscriptions in the chain if needed
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        private async Task ForceRecreateIfNeededAsync(OpcUaSession session)
        {
            var child = GetChildSubscription();
            if (child != null)
            {
                await child.ForceRecreateIfNeededAsync(session).ConfigureAwait(false);
            }

            if (!_forceRecreate)
            {
                return;
            }

            _forceRecreate = false;
            _logger.LogInformation(
                "========  Closing subscription {Subscription} and re-creating =========",
                this);

            // Does not throw
            await CloseCurrentSubscriptionAsync().ConfigureAwait(false);

            Debug.Assert(Session == null);
            session.AddSubscription(this); // Re-add the subscription now
            Debug.Assert(Session == session);
        }

        /// <summary>
        /// Finalize sync of all subscriptions in the chain if needed
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task FinalizeSyncAsync(CancellationToken ct)
        {
            var child = GetChildSubscription();
            if (child != null)
            {
                await child.FinalizeSyncAsync(ct).ConfigureAwait(false);
            }

            if (ChangesPending)
            {
                await ApplyChangesAsync(ct).ConfigureAwait(false);
            }

            var shouldEnable = MonitoredItems
                .OfType<OpcUaMonitoredItem>()
                .Any(m => m.AttachedToSubscription
                    && m.MonitoringMode != Opc.Ua.MonitoringMode.Disabled);
            if (PublishingEnabled ^ shouldEnable)
            {
                await SetPublishingModeAsync(shouldEnable, ct).ConfigureAwait(false);

                _logger.LogInformation(
                    "{State} Subscription {Subscription} in session {Session}.",
                    shouldEnable ? "Enabled" : "Disabled", this, Session);

                ResetMonitoredItemWatchdogTimer(shouldEnable);
            }
        }

        /// <summary>
        /// Get a subscription with the supplied configuration (no lock)
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="ServiceResultException"></exception>
        private async ValueTask SynchronizeSubscriptionAsync(CancellationToken ct)
        {
            if (Handle == null)
            {
                Handle = SubscriptionId; // Initialized for the first time
                DisplayName = Name + SubscriptionId;
                PublishingEnabled = EnableImmediatePublishing;
                KeepAliveCount = DesiredKeepAliveCount;
                PublishingInterval = (int)DesiredPublishingInterval.TotalMilliseconds;
                MaxNotificationsPerPublish = DesiredMaxNotificationsPerPublish;
                LifetimeCount = DesiredLifetimeCount;
                Priority = DesiredPriority;

                // TODO: use a channel and reorder task before calling OnMessage
                // to order or else republish is called too often
                RepublishAfterTransfer = DesiredRepublishAfterTransfer;
                SequentialPublishing = EnableSequentialPublishing;

                _logger.LogInformation(
                    "Creating new {State} subscription {Subscription} in session {Session}.",
                    PublishingEnabled ? "enabled" : "disabled", this, Session);

                Debug.Assert(Session != null);
                await CreateAsync(ct).ConfigureAwait(false);
                if (!Created)
                {
                    Handle = null;
                    var session = Session;
                    await session.RemoveSubscriptionAsync(this, ct).ConfigureAwait(false);
                    Debug.Assert(Session == null);
                    throw new ServiceResultException(StatusCodes.BadSubscriptionIdInvalid,
                        $"Failed to create subscription {this} in session {session}");
                }

                ResetMonitoredItemWatchdogTimer(PublishingEnabled);
                LogRevisedValues(true);
                Debug.Assert(Id != 0);
                Debug.Assert(Created);

                _firstDataChangeReceived = false;
            }
            else
            {
                //
                // Only needed when we reconfiguring a subscription with a single subscriber
                // This is not yet implemented.
                // TODO: Consider removing...
                //

                // Apply new configuration on configuration on original subscription
                var modifySubscription = false;

                if (DesiredKeepAliveCount != KeepAliveCount)
                {
                    _logger.LogInformation(
                        "Change KeepAliveCount to {New} in Subscription {Subscription}...",
                        DesiredKeepAliveCount, this);

                    KeepAliveCount = DesiredKeepAliveCount;
                    modifySubscription = true;
                }

                if (PublishingInterval != (int)DesiredPublishingInterval.TotalMilliseconds)
                {
                    _logger.LogInformation(
                        "Change publishing interval to {New} in Subscription {Subscription}...",
                        DesiredPublishingInterval, this);
                    PublishingInterval = (int)DesiredPublishingInterval.TotalMilliseconds;
                    modifySubscription = true;
                }

                if (MaxNotificationsPerPublish != DesiredMaxNotificationsPerPublish)
                {
                    _logger.LogInformation(
                        "Change MaxNotificationsPerPublish to {New} in Subscription {Subscription}",
                        DesiredMaxNotificationsPerPublish, this);
                    MaxNotificationsPerPublish = DesiredMaxNotificationsPerPublish;
                    modifySubscription = true;
                }

                if (LifetimeCount != DesiredLifetimeCount)
                {
                    _logger.LogInformation(
                        "Change LifetimeCount to {New} in Subscription {Subscription}...",
                        DesiredLifetimeCount, this);
                    LifetimeCount = DesiredLifetimeCount;
                    modifySubscription = true;
                }
                if (Priority != DesiredPriority)
                {
                    _logger.LogInformation(
                        "Change Priority to {New} in Subscription {Subscription}...",
                        DesiredPriority, this);
                    Priority = DesiredPriority;
                    modifySubscription = true;
                }
                if (modifySubscription)
                {
                    await ModifyAsync(ct).ConfigureAwait(false);
                    _logger.LogInformation(
                        "Subscription {Subscription} in session {Session} successfully modified.",
                        this, Session);
                    LogRevisedValues(false);
                    ResetMonitoredItemWatchdogTimer(PublishingEnabled);
                }
            }
            ResetKeepAliveTimer();
        }

        /// <summary>
        /// Synchronize partition of monitored items into this subscription
        /// </summary>
        /// <param name="partition"></param>
        /// <param name="operationLimits"></param>
        /// <param name="ct"></param>
        /// <exception cref="ServiceResultException"></exception>
        private async ValueTask<TimeSpan> SynchronizeMonitoredItemsAsync(
            Partition partition, OperationLimitsModel operationLimits, CancellationToken ct)
        {
            if (Session is not OpcUaSession session)
            {
                throw ServiceResultException.Create(StatusCodes.BadSessionIdInvalid,
                    "Session not connected.");
            }

            // Get the items assigned to this subscription.
#pragma warning disable CA2000 // Dispose objects before losing scope
            var desired = OpcUaMonitoredItem
                .Create(_client, partition.Items, _loggerFactory, _timeProvider)
                .ToHashSet();
#pragma warning restore CA2000 // Dispose objects before losing scope

            var previouslyMonitored = CurrentlyMonitored.ToHashSet();
            var remove = previouslyMonitored.Except(desired).ToHashSet();
            var add = desired.Except(previouslyMonitored).ToHashSet();
            var same = previouslyMonitored.ToHashSet();
            var errorsDuringSync = 0;
            same.IntersectWith(desired);

            //
            // Resolve the browse paths for all nodes first.
            //
            // We shortcut this only through the added items since the identity (hash)
            // of the monitored item is dependent on the node and browse path, so any
            // update of either results in a newly added monitored item and the old
            // one removed.
            //
            var allResolvers = add
                .Select(a => a.Resolve)
                .Where(a => a != null);
            foreach (var resolvers in allResolvers.Batch(
                operationLimits.GetMaxNodesPerTranslatePathsToNodeIds()))
            {
                var response = await session.Services.TranslateBrowsePathsToNodeIdsAsync(
                    new RequestHeader(), new BrowsePathCollection(resolvers
                        .Select(a => new BrowsePath
                        {
                            StartingNode = a!.Value.NodeId.ToNodeId(
                                session.MessageContext),
                            RelativePath = a.Value.Path.ToRelativePath(
                                session.MessageContext)
                        })), ct).ConfigureAwait(false);

                var results = response.Validate(response.Results, s => s.StatusCode,
                    response.DiagnosticInfos, resolvers);
                if (results.ErrorInfo != null)
                {
                    // Could not do anything...
                    _logger.LogWarning(
                        "Failed to resolve browse path in {Subscription} due to {ErrorInfo}...",
                        this, results.ErrorInfo);
                    throw ServiceResultException.Create(results.ErrorInfo.StatusCode,
                        results.ErrorInfo.ErrorMessage ?? "Failed to resolve browse paths");
                }

                foreach (var result in results)
                {
                    var resolvedId = NodeId.Null;
                    if (result.ErrorInfo == null && result.Result.Targets.Count == 1)
                    {
                        resolvedId = result.Result.Targets[0].TargetId.ToNodeId(
                            session.MessageContext.NamespaceUris);
                        result.Request!.Value.Update(resolvedId, session.MessageContext);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to resolve browse path for {NodeId} " +
                            "in {Subscription} due to '{ServiceResult}'",
                            result.Request!.Value.NodeId, this, result.ErrorInfo);
                        errorsDuringSync++;
                    }
                }
            }

            //
            // If retrieving paths for all the items from the root folder was configured do so
            // now. All items that fail here should be retried later.
            //
            if (ResolveBrowsePathFromRoot)
            {
                var allGetPaths = add
                    .Select(a => a.GetPath)
                    .Where(a => a != null);
                var pathsRetrieved = 0;
                foreach (var getPathsBatch in allGetPaths.Batch(10000))
                {
                    var getPath = getPathsBatch.ToList();
                    var paths = await session.GetBrowsePathsFromRootAsync(new RequestHeader(),
                        getPath.Select(n => n!.Value.NodeId.ToNodeId(session.MessageContext)),
                        ct).ConfigureAwait(false);
                    for (var index = 0; index < paths.Count; index++)
                    {
                        getPath[index]!.Value.Update(paths[index].Path, session.MessageContext);
                        if (paths[index].ErrorInfo != null)
                        {
                            _logger.LogWarning("Failed to get root path for {NodeId} " +
                                "in {Subscription} due to '{ServiceResult}'",
                                getPath[index]!.Value.NodeId, this, paths[index].ErrorInfo);
                        }
                        else
                        {
                            pathsRetrieved++;
                        }
                    }
                }
                if (pathsRetrieved > 0)
                {
                    _logger.LogInformation(
                        "Retrieved {Count} paths for items in subscription {Subscription}.",
                        pathsRetrieved, this);
                }
            }

            //
            // Register nodes for reading if needed. This is needed anytime the session
            // changes as the registration is only valid in the context of the session
            //
            var allRegistrations = add.Concat(same)
                .Select(a => a.Register)
                .Where(a => a != null);
            foreach (var registrations in allRegistrations.Batch(
                operationLimits.GetMaxNodesPerRegisterNodes()))
            {
                var response = await session.Services.RegisterNodesAsync(
                    new RequestHeader(), new NodeIdCollection(registrations
                        .Select(a => a!.Value.NodeId.ToNodeId(session.MessageContext))),
                    ct).ConfigureAwait(false);
                foreach (var (First, Second) in response.RegisteredNodeIds.Zip(registrations))
                {
                    Debug.Assert(Second != null);
                    if (!NodeId.IsNull(First))
                    {
                        Second.Value.Update(First, session.MessageContext);
                    }
                }
            }

            var metadataChanged = new HashSet<ISubscriber>();
            var applyChanges = false;
            var updated = 0;

            foreach (var toUpdate in same)
            {
                if (!desired.TryGetValue(toUpdate, out var theDesiredUpdate))
                {
                    errorsDuringSync++;
                    continue;
                }
                desired.Remove(theDesiredUpdate);
                Debug.Assert(toUpdate.GetType() == theDesiredUpdate.GetType());
                Debug.Assert(toUpdate.Subscription == this);
                try
                {
                    if (toUpdate.MergeWith(theDesiredUpdate, session, out var metadata))
                    {
                        _logger.LogDebug(
                            "Trying to update monitored item '{Item}' in {Subscription}...",
                            toUpdate, this);
                        if (toUpdate.FinalizeMergeWith != null && metadata)
                        {
                            await toUpdate.FinalizeMergeWith(session, ct).ConfigureAwait(false);
                        }
                        updated++;
                        applyChanges = true;
                    }
                    if (metadata)
                    {
                        metadataChanged.Add(toUpdate.Owner);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Failed to update monitored item '{Item}' in {Subscription}...",
                        toUpdate, this);
                    errorsDuringSync++;
                }
                finally
                {
                    theDesiredUpdate.Dispose();
                }
            }

            var removed = 0;
            foreach (var toRemove in remove)
            {
                Debug.Assert(toRemove.Subscription == this);
                try
                {
                    if (toRemove.RemoveFrom(this, out var metadata))
                    {
                        _logger.LogDebug(
                            "Trying to remove monitored item '{Item}' from {Subscription}...",
                            toRemove, this);
                        removed++;
                        applyChanges = true;
                    }
                    if (metadata)
                    {
                        metadataChanged.Add(toRemove.Owner);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Failed to remove monitored item '{Item}' from {Subscription}...",
                        toRemove, this);
                    errorsDuringSync++;
                }
            }

            var added = 0;
            foreach (var toAdd in add)
            {
                desired.Remove(toAdd);
                Debug.Assert(toAdd.Subscription == null);
                try
                {
                    if (toAdd.AddTo(this, session, out var metadata))
                    {
                        _logger.LogDebug(
                            "Adding monitored item '{Item}' to {Subscription}...",
                            toAdd, this);

                        if (toAdd.FinalizeAddTo != null && metadata)
                        {
                            await toAdd.FinalizeAddTo(session, ct).ConfigureAwait(false);
                        }
                        added++;
                        applyChanges = true;
                    }
                    if (metadata)
                    {
                        metadataChanged.Add(toAdd.Owner);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Failed to add monitored item '{Item}' to {Subscription}...",
                        toAdd, this);
                    errorsDuringSync++;
                }
            }

            Debug.Assert(desired.Count == 0, "We should have processed all desired updates.");

            if (applyChanges)
            {
                await ApplyChangesAsync(ct).ConfigureAwait(false);
                if (MonitoredItemCount == 0 && !EnableImmediatePublishing)
                {
                    await SetPublishingModeAsync(false, ct).ConfigureAwait(false);

                    _logger.LogInformation(
                        "Disabled empty Subscription {Subscription} in session {Session}.",
                        this, session);

                    ResetMonitoredItemWatchdogTimer(false);
                }
            }

            // Perform second pass over all monitored items and complete.
            applyChanges = false;
            var badMonitoredItems = 0;

            var desiredMonitoredItems = same;
            desiredMonitoredItems.UnionWith(add);

            //
            // Resolve display names for all nodes that still require a name
            // other than the node id string.
            //
            // Note that we use the desired set here and update the display
            // name after AddTo/MergeWith as it only effects the messages
            // and metadata emitted and not the item as it is set up in the
            // subscription (like what we do when resolving nodes). This
            // supports the scenario where the user sets a desired display
            // name of null to force reading the display name from the node
            // and updating the existing display name (previously set) and
            // at the same time is quite effective to only update what is
            // necessary.
            //
            var allDisplayNameUpdates = desiredMonitoredItems
                .Select(a => (a.Owner, a.GetDisplayName))
                .Where(a => a.GetDisplayName.HasValue)
                .ToList();
            if (allDisplayNameUpdates.Count > 0)
            {
                foreach (var displayNameUpdates in allDisplayNameUpdates.Batch(
                    operationLimits.GetMaxNodesPerRead()))
                {
                    var response = await session.Services.ReadAsync(new RequestHeader(),
                        0, Opc.Ua.TimestampsToReturn.Neither, new ReadValueIdCollection(
                        displayNameUpdates.Select(a => new ReadValueId
                        {
                            NodeId = a.GetDisplayName!.Value.NodeId.ToNodeId(session.MessageContext),
                            AttributeId = (uint)NodeAttribute.DisplayName
                        })), ct).ConfigureAwait(false);
                    var results = response.Validate(response.Results,
                        s => s.StatusCode, response.DiagnosticInfos, displayNameUpdates);

                    if (results.ErrorInfo != null)
                    {
                        _logger.LogWarning(
                            "Failed to resolve display name in {Subscription} due to {ErrorInfo}...",
                            this, results.ErrorInfo);

                        // We will retry later.
                        errorsDuringSync++;
                    }
                    else
                    {
                        foreach (var result in results)
                        {
                            string? displayName = null;
                            if (result.Result.Value is not null)
                            {
                                displayName =
                                    (result.Result.Value as LocalizedText)?.ToString();
                                metadataChanged.Add(result.Request.Owner);
                            }
                            else
                            {
                                _logger.LogWarning("Failed to read display name for {NodeId} " +
                                    "in {Subscription} due to '{ServiceResult}'",
                                    result.Request.GetDisplayName!.Value.NodeId, this,
                                    result.ErrorInfo);
                            }
                            result.Request.GetDisplayName!.Value.Update(
                                displayName ?? string.Empty);
                        }
                    }
                }
            }

            _logger.LogDebug(
                "Completing {Count} same/added and {Removed} removed items in subscription {Subscription}...",
                desiredMonitoredItems.Count, remove.Count, this);
            foreach (var monitoredItem in desiredMonitoredItems.Concat(remove))
            {
                if (!monitoredItem.TryCompleteChanges(this, ref applyChanges))
                {
                    // Apply more changes in future passes
                    badMonitoredItems++;
                }
            }

            Debug.Assert(remove.All(m => !m.AttachedToSubscription),
                "All removed items should be detached now");
            var set = desiredMonitoredItems.Where(m => m.Valid).ToList();
            _logger.LogDebug(
                "Completed {Count} valid and {Invalid} invalid items in subscription {Subscription}...",
                set.Count, desiredMonitoredItems.Count - set.Count, this);

            var finalize = set
                .Where(i => i.FinalizeCompleteChanges != null)
                .Select(i => i.FinalizeCompleteChanges!(ct))
                .ToArray();
            if (finalize.Length > 0)
            {
                await Task.WhenAll(finalize).ConfigureAwait(false);
            }

            if (applyChanges)
            {
                // Apply any additional changes
                await ApplyChangesAsync(ct).ConfigureAwait(false);
            }

            Debug.Assert(set.Select(m => m.ClientHandle).Distinct().Count() == set.Count,
                "Client handles are not distinct or one of the items is null");

            _logger.LogDebug(
                "Setting monitoring mode on {Count} items in subscription {Subscription}...",
                set.Count, this);

            //
            // Finally change the monitoring mode as required. Batch the requests
            // on the update of monitored item state from monitored items. On AddTo
            // the monitoring mode was already configured. This is for updates as
            // they are not applied through ApplyChanges
            //
            foreach (var change in set.GroupBy(i => i.GetMonitoringModeChange()))
            {
                if (change.Key == null)
                {
                    // Not a valid item
                    continue;
                }

                foreach (var itemsBatch in change.Batch(
                    operationLimits.GetMaxMonitoredItemsPerCall()))
                {
                    var itemsToChange = itemsBatch.Cast<MonitoredItem>().ToList();
                    _logger.LogInformation(
                        "Set monitoring to {Value} for {Count} items in subscription {Subscription}.",
                        change.Key.Value, itemsToChange.Count, this);

                    var results = await SetMonitoringModeAsync(change.Key.Value,
                        itemsToChange, ct).ConfigureAwait(false);
                    if (results != null)
                    {
                        var erroneousResultsCount = results
                            .Count(r => r != null && StatusCode.IsNotGood(r.StatusCode));

                        // Check the number of erroneous results and log.
                        if (erroneousResultsCount > 0)
                        {
                            _logger.LogWarning(
                                "Failed to set monitoring for {Count} items in subscription {Subscription}.",
                                erroneousResultsCount, this);
                            for (var i = 0; i < results.Count && i < itemsToChange.Count; ++i)
                            {
                                if (StatusCode.IsNotGood(results[i].StatusCode))
                                {
                                    _logger.LogWarning("Set monitoring for item '{Item}' in "
                                        + "subscription {Subscription} failed with '{Status}'.",
                                        itemsToChange[i].StartNodeId, this, results[i].StatusCode);
                                }
                            }
                            // Retry later
                            errorsDuringSync++;
                        }
                    }
                }
            }

            finalize = set
                .Where(i => i.FinalizeMonitoringModeChange != null)
                .Select(i => i.FinalizeMonitoringModeChange!(ct))
                .ToArray();
            if (finalize.Length > 0)
            {
                await Task.WhenAll(finalize).ConfigureAwait(false);
            }

            // Cleanup all items that are not in the currently monitoring list
            var dispose = previouslyMonitored
                .Except(set)
                .ToList();
            dispose.ForEach(m => m.Dispose());

            // Notify semantic change now that we have update the monitored items
            foreach (var owner in metadataChanged)
            {
                owner.OnMonitoredItemSemanticsChanged();
            }

            // Refresh condition
            if (set.OfType<OpcUaMonitoredItem.Condition>().Any())
            {
                _logger.LogInformation(
                    "Issuing ConditionRefresh on subscription {Subscription}", this);
                try
                {
                    await ConditionRefreshAsync(ct).ConfigureAwait(false);
                    _logger.LogInformation("ConditionRefresh on subscription " +
                        "{Subscription} has completed.", this);
                }
                catch (Exception e)
                {
                    _logger.LogInformation("ConditionRefresh on subscription " +
                        "{Subscription} failed with an exception '{Message}'",
                        this, e.Message);
                    errorsDuringSync++;
                }
            }

            set.ForEach(item => item.LogRevisedSamplingRateAndQueueSize());

            var goodMonitoredItems =
                Math.Max(set.Count - badMonitoredItems, 0);
            var reportingItems = set
                .Count(r => r.Status?.MonitoringMode == Opc.Ua.MonitoringMode.Reporting);
            var disabledItems = set
                .Count(r => r.Status?.MonitoringMode == Opc.Ua.MonitoringMode.Disabled);
            var samplingItems = set
                .Count(r => r.Status?.MonitoringMode == Opc.Ua.MonitoringMode.Sampling);
            var notAppliedItems = set
                .Count(r => r.Status?.MonitoringMode != r.MonitoringMode);
            var heartbeatItems = set
                .Count(r => r is OpcUaMonitoredItem.Heartbeat);
            var conditionItems = set
                .Count(r => r is OpcUaMonitoredItem.Condition);
            var heartbeatsEnabled = set
                .Count(r => r is OpcUaMonitoredItem.Heartbeat h && h.TimerEnabled);
            var conditionsEnabled = set
                .Count(r => r is OpcUaMonitoredItem.Condition h && h.TimerEnabled);

            ReportMonitoredItemChanges(set.Count, goodMonitoredItems, badMonitoredItems,
                errorsDuringSync, notAppliedItems, reportingItems, disabledItems, heartbeatItems,
                heartbeatsEnabled, conditionItems, conditionsEnabled, samplingItems,
                dispose.Count);

            // Set up subscription management trigger
            if (badMonitoredItems != 0 || errorsDuringSync != 0)
            {
                // There were items that could not be added to subscription
                return Delay(_options.Value.InvalidMonitoredItemRetryDelayDuration,
                    TimeSpan.FromMinutes(5));
            }
            else if (desiredMonitoredItems.Count != set.Count)
            {
                // There were items !Valid but desired.
                return Delay(_options.Value.BadMonitoredItemRetryDelayDuration,
                    TimeSpan.FromMinutes(30));
            }
            else
            {
                // Nothing to do
                return Delay(_options.Value.SubscriptionManagementIntervalDuration,
                    Timeout.InfiniteTimeSpan);
            }
        }

        /// <summary>
        /// Initialize state
        /// </summary>
        private void Initialize()
        {
            FastKeepAliveCallback = OnSubscriptionKeepAliveNotification;
            FastDataChangeCallback = OnSubscriptionDataChangeNotification;
            FastEventCallback = OnSubscriptionEventNotificationList;
            PublishStatusChanged += OnPublishStatusChange;
            StateChanged += OnStateChange;

            TimestampsToReturn = Opc.Ua.TimestampsToReturn.Both;
            DisableMonitoredItemCache = true;
        }

        /// <summary>
        /// Get child subscription
        /// </summary>
        /// <param name="createIfNotExist"></param>
        /// <returns></returns>
        private OpcUaSubscription? GetChildSubscription(bool createIfNotExist = false)
        {
            if (Session is OpcUaSession session)
            {
                if (_childId.HasValue &&
                    session.SubscriptionHandles.TryGetValue(_childId.Value,
                    out var subscription))
                {
                    // Found entry
                    return subscription;
                }

                if (createIfNotExist)
                {
                    subscription = new OpcUaSubscription(this, parentId: SubscriptionId);
                    _childId = subscription.SubscriptionId;
                    session.AddSubscription(subscription);
                    return subscription;
                }

                if (_childId != null)
                {
                    _logger.LogError(
                        "Child subscription {ChildId} not found in session {Session}.",
                        _childId, session);
                }
                _childId = null;
            }
            return null;
        }

        /// <summary>
        /// Get all monitored items încluding all child subscriptions.
        /// This call is used to collect all items recursively.
        /// </summary>
        /// <param name="parent"></param>
        /// <returns></returns>
        private IEnumerable<OpcUaMonitoredItem> GetAllMonitoredItems(
            IEnumerable<OpcUaMonitoredItem>? parent = null)
        {
            parent ??= [];

            parent = parent.Concat(CurrentlyMonitored);

            var child = GetChildSubscription();
            if (child != null)
            {
                // Recursively concat the items of all children
                parent = child.GetAllMonitoredItems(parent);
            }
            return parent;
        }

        /// <summary>
        /// Collect metadata across this and all child subscriptions
        /// recursively from parent to child to child and so on.
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="session"></param>
        /// <param name="typeSystem"></param>
        /// <param name="dataTypes"></param>
        /// <param name="fields"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task CollectMetaDataAsync(ISubscriber owner, OpcUaSession session,
            ComplexTypeSystem? typeSystem, NodeIdDictionary<object> dataTypes,
            List<PublishedFieldMetaDataModel> fields, CancellationToken ct)
        {
            foreach (var monitoredItem in CurrentlyMonitored.Where(m => m.Owner == owner))
            {
                await monitoredItem.GetMetaDataAsync(session, typeSystem,
                    fields, dataTypes, ct).ConfigureAwait(false);
            }

            var child = GetChildSubscription();
            if (child != null)
            {
                await child.CollectMetaDataAsync(owner,
                    session, typeSystem, dataTypes, fields, ct).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Close subscription
        /// </summary>
        /// <returns></returns>
        private async Task CloseCurrentSubscriptionAsync()
        {
            ResetKeepAliveTimer();
            try
            {
                Handle = null; // Mark as closed

                _logger.LogDebug("Closing subscription '{Subscription}'...", this);

                // Dispose all monitored items
                var items = CurrentlyMonitored.ToList();

                RemoveItems(MonitoredItems);
                _currentSequenceNumber = 0;
                _goodMonitoredItems = 0;
                _badMonitoredItems = 0;

                _reportingItems = 0;
                _disabledItems = 0;
                _samplingItems = 0;
                _notAppliedItems = 0;

                ResetMonitoredItemWatchdogTimer(false);

                await Try.Async(() => SetPublishingModeAsync(false, default)).ConfigureAwait(false);
                await Try.Async(() => DeleteItemsAsync(default)).ConfigureAwait(false);
                await Try.Async(() => ApplyChangesAsync(default)).ConfigureAwait(false);

                items.ForEach(item => item.Dispose());
                _logger.LogDebug("Deleted {Count} monitored items for '{Subscription}'.",
                    items.Count, this);

                await Try.Async(() => DeleteAsync(true, default)).ConfigureAwait(false);

                if (Session != null)
                {
                    await Session.RemoveSubscriptionAsync(this).ConfigureAwait(false);
                }
                Debug.Assert(Session == null, "Subscription should not be part of session");
                Debug.Assert(!CurrentlyMonitored.Any(), "Not all items removed.");
                _logger.LogInformation("Subscription '{Subscription}' closed.", this);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to close subscription {Subscription}", this);
            }
        }

        /// <summary>
        /// Log revised values of the subscription
        /// </summary>
        /// <param name="created"></param>
        private void LogRevisedValues(bool created)
        {
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
        /// Report monitored item changes
        /// </summary>
        /// <param name="count"></param>
        /// <param name="goodMonitoredItems"></param>
        /// <param name="badMonitoredItems"></param>
        /// <param name="errorsDuringSync"></param>
        /// <param name="notAppliedItems"></param>
        /// <param name="reportingItems"></param>
        /// <param name="disabledItems"></param>
        /// <param name="heartbeatItems"></param>
        /// <param name="heartbeatsEnabled"></param>
        /// <param name="conditionItems"></param>
        /// <param name="conditionsEnabled"></param>
        /// <param name="samplingItems"></param>
        /// <param name="disposed"></param>
        private void ReportMonitoredItemChanges(int count,
            int goodMonitoredItems, int badMonitoredItems,
            int errorsDuringSync, int notAppliedItems,
            int reportingItems, int disabledItems,
            int heartbeatItems, int heartbeatsEnabled,
            int conditionItems, int conditionsEnabled,
            int samplingItems, int disposed)
        {
            if (_badMonitoredItems != badMonitoredItems ||
                _errorsDuringSync != errorsDuringSync ||
                _goodMonitoredItems != goodMonitoredItems ||
                _reportingItems != reportingItems ||
                _disabledItems != disabledItems ||
                _samplingItems != samplingItems ||
                _notAppliedItems != notAppliedItems ||
                _heartbeatItems != heartbeatItems ||
                _conditionItems != conditionItems)
            {
                if (samplingItems == 0 && heartbeatItems == 0 && conditionItems == 0 &&
                    notAppliedItems == 0)
                {
                    if (errorsDuringSync == 0 && disabledItems == 0)
                    {
                        _logger.LogInformation(
@"{Subscription} - Removed {Removed} - now monitoring {Count} nodes:
# Good/Bad/Reporting:   {Good}/{Bad}/{Reporting}",
                            this, disposed, count,
                            goodMonitoredItems, badMonitoredItems, reportingItems);
                    }
                    else
                    {
                        _logger.LogWarning(
@"{Subscription} - Removed {Removed} - now monitoring {Count} nodes:
# Good/Bad/Reporting:   {Good}/{Bad}/{Reporting}
# Disabled/Errors:      {Disabled}/{Errors}",
                            this, disposed, count,
                            goodMonitoredItems, badMonitoredItems, reportingItems,
                            disabledItems, errorsDuringSync);
                    }
                }
                else
                {
                    _logger.LogInformation(
@"{Subscription} - Removed {Removed} - now monitoring {Count} nodes:
# Good/Bad/Reporting:   {Good}/{Bad}/{Reporting}
# Disabled/Errors:      {Disabled}/{Errors} (Not applied: {NotApplied})
# Sampling:             {Sampling}
# Heartbeat/ing:        {Heartbeat}/{EnabledHeartbeats}
# Condition/ing:        {Conditions}/{EnabledConditions}",
                            this, disposed, count,
                            goodMonitoredItems, badMonitoredItems, reportingItems,
                            disabledItems, errorsDuringSync, notAppliedItems,
                            samplingItems,
                            heartbeatItems, heartbeatsEnabled,
                            conditionItems, conditionsEnabled);
                }
            }
            else
            {
                _logger.LogDebug(
                    "{ Subscription} Applied changes to monitored items, but nothing changed.",
                    this);
            }

            _badMonitoredItems = badMonitoredItems;
            _errorsDuringSync = errorsDuringSync;
            _goodMonitoredItems = goodMonitoredItems;
            _reportingItems = reportingItems;
            _disabledItems = disabledItems;
            _samplingItems = samplingItems;
            _notAppliedItems = notAppliedItems;
            _heartbeatItems = heartbeatItems;
            _conditionItems = conditionItems;
        }

        /// <summary>
        /// Calculate delay
        /// </summary>
        /// <param name="delay"></param>
        /// <param name="defaultDelay"></param>
        /// <returns></returns>
        private static TimeSpan Delay(TimeSpan? delay, TimeSpan defaultDelay)
        {
            if (delay == null)
            {
                delay = defaultDelay;
            }
            else if (delay == TimeSpan.Zero)
            {
                delay = Timeout.InfiniteTimeSpan;
            }
            return delay.Value;
        }

        /// <summary>
        /// Send notification
        /// </summary>
        /// <param name="callback"></param>
        /// <param name="messageType"></param>
        /// <param name="notifications"></param>
        /// <param name="eventTypeName"></param>
        /// <param name="diagnosticsOnly"></param>
        /// <param name="timestamp"></param>
        internal void SendNotification(ISubscriber callback, MessageType messageType,
            IList<MonitoredItemNotificationModel> notifications,
            string? eventTypeName, bool diagnosticsOnly, DateTimeOffset? timestamp)
        {
            var curSession = Session;
            var messageContext = curSession?.MessageContext;

            if (messageContext == null)
            {
                _logger.LogDebug("Sending notification while session is disconnected.");
                messageContext = ServiceMessageContext.GlobalContext;
            }

#pragma warning disable CA2000 // Dispose objects before losing scope
            var message = new OpcUaSubscriptionNotification(this, messageContext, notifications,
                _timeProvider, createdTimestamp: timestamp)
            {
                ApplicationUri = curSession?.Endpoint?.Server?.ApplicationUri,
                EndpointUrl = curSession?.Endpoint?.EndpointUrl,
                EventTypeName = eventTypeName,
                SequenceNumber = Opc.Ua.SequenceNumber.Increment32(ref _sequenceNumber),
                MessageType = messageType
            };
#pragma warning restore CA2000 // Dispose objects before losing scope

            var count = message.GetDiagnosticCounters(out var modelChanges,
                out var heartbeats, out var overflows);
            if (messageType == MessageType.Event || messageType == MessageType.Condition)
            {
                if (!diagnosticsOnly)
                {
                    callback.OnSubscriptionEventReceived(message);
                }
                if (count > 0)
                {
                    callback.OnSubscriptionEventDiagnosticsChange(false,
                        count, overflows, modelChanges == 0 ? 0 : 1);
                }
            }
            else
            {
                if (!diagnosticsOnly)
                {
                    callback.OnSubscriptionDataChangeReceived(message);
                }
                if (count > 0)
                {
                    callback.OnSubscriptionDataDiagnosticsChange(false,
                        count, overflows, heartbeats);
                }
            }
        }

        /// <summary>
        /// Handle event notification. Depending on the sequential publishing setting
        /// this will be called in order and thread safe or from different threads.
        /// </summary>
        /// <param name="subscription"></param>
        /// <param name="notification"></param>
        /// <param name="stringTable"></param>
        private void OnSubscriptionEventNotificationList(Subscription subscription,
            EventNotificationList notification, IList<string>? stringTable)
        {
            Debug.Assert(ReferenceEquals(subscription, this));
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (notification?.Events == null)
            {
                _logger.LogWarning(
                    "EventChange for subscription {Subscription} has empty notification.", this);
                return;
            }

            if (notification.Events.Count == 0)
            {
                _logger.LogWarning(
                    "EventChange for subscription {Subscription} has no events.", this);
                return;
            }

            var session = Session;
            if (session is not IOpcUaSession sessionContext)
            {
                _logger.LogWarning(
                    "EventChange for subscription {Subscription} received without a session {Session}.",
                    this, session);
                return;
            }

            ResetKeepAliveTimer();

            var sw = Stopwatch.StartNew();
            try
            {
                var sequenceNumber = notification.SequenceNumber;
                var publishTime = notification.PublishTime;

                Debug.Assert(notification.Events != null);

                if (sequenceNumber == 1)
                {
                    // Do not log when the sequence number is 1 after reconnect
                    _previousSequenceNumber = 1;
                }
                else if (!Opc.Ua.SequenceNumber.Validate(sequenceNumber, ref _previousSequenceNumber,
                    out var missingSequenceNumbers, out var dropped))
                {
                    _logger.LogWarning("Event subscription notification for subscription " +
                        "{Subscription} has unexpected sequenceNumber {SequenceNumber} missing " +
                        "{ExpectedSequenceNumber} which were {Dropped}, publishTime {PublishTime}",
                        this, sequenceNumber,
                        Opc.Ua.SequenceNumber.ToString(missingSequenceNumbers), dropped ?
                            "dropped" : "already received", publishTime);
                }

                var overflows = 0;
                var events = new List<(string?, OpcUaMonitoredItem.MonitoredItemNotifications)>();
                foreach (var eventFieldList in notification.Events)
                {
                    Debug.Assert(eventFieldList != null);
                    if (TryGetMonitoredItemForNotification(eventFieldList.ClientHandle, out var monitoredItem))
                    {
                        var collector = new OpcUaMonitoredItem.MonitoredItemNotifications();
                        if (!monitoredItem.TryGetMonitoredItemNotifications(publishTime, eventFieldList, collector))
                        {
                            _logger.LogDebug("Skipping the monitored item notification for Event " +
                                "received for subscription {Subscription}", this);
                        }
                        events.Add((monitoredItem.EventTypeName, collector));
                    }
                }

                var total = events.Sum(e => e.Item2.Notifications.Count);
#pragma warning disable CA2000 // Dispose objects before losing scope
                var advance = new Advance(this, sequenceNumber, total);
#pragma warning restore CA2000 // Dispose objects before losing scope
                foreach (var (name, evt) in events)
                {
                    foreach (var (callback, notifications) in evt.Notifications)
                    {
#pragma warning disable CA2000 // Dispose objects before losing scope
                        var message = new OpcUaSubscriptionNotification(this, session.MessageContext,
                            notifications, _timeProvider, advance, sequenceNumber)
                        {
                            ApplicationUri = session.Endpoint?.Server?.ApplicationUri,
                            EndpointUrl = session.Endpoint?.EndpointUrl,
                            EventTypeName = name,
                            SequenceNumber = Opc.Ua.SequenceNumber.Increment32(ref _sequenceNumber),
                            MessageType = MessageType.Event,
                            PublishTimestamp = publishTime
                        };
#pragma warning restore CA2000 // Dispose objects before losing scope

                        if (message.Notifications.Count > 0)
                        {
                            callback.OnSubscriptionEventReceived(message);
                            overflows += message.Notifications.Sum(n => n.Overflow);
                            callback.OnSubscriptionEventDiagnosticsChange(true, overflows, 1, 0);
                        }
                        else
                        {
                            _logger.LogDebug("No notifications added to the message.");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Exception processing subscription notification");
            }
            finally
            {
                _logger.LogDebug("Event callback took {Elapsed}", sw.Elapsed);
                if (sw.ElapsedMilliseconds > 1000)
                {
                    _logger.LogWarning("Spent more than 1 second in fast event callback.");
                }
            }
        }

        /// <summary>
        /// Handle keep alive messages
        /// </summary>
        /// <param name="subscription"></param>
        /// <param name="notification"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void OnSubscriptionKeepAliveNotification(Subscription subscription,
            NotificationData notification)
        {
            Debug.Assert(ReferenceEquals(subscription, this));
            ObjectDisposedException.ThrowIf(_disposed, this);

            ResetKeepAliveTimer();

            if (!PublishingEnabled)
            {
                _logger.LogDebug(
                    "Keep alive event received while publishing is not enabled - skip.");
                return;
            }

            var session = Session;
            if (session is not IOpcUaSession)
            {
                _logger.LogWarning(
                    "Keep alive event for subscription {Subscription} received without session {Session}.",
                    this, session);
                return;
            }

            var sw = Stopwatch.StartNew();
            try
            {
                var sequenceNumber = notification.SequenceNumber;
                var publishTime = notification.PublishTime;

                // in case of a keepalive,the sequence number is not incremented by the servers
                _logger.LogDebug("Keep alive for subscription {Subscription} " +
                    "with sequenceNumber {SequenceNumber}, publishTime {PublishTime}.",
                    this, sequenceNumber, publishTime);

#pragma warning disable CA2000 // Dispose objects before losing scope
                var message = new OpcUaSubscriptionNotification(this, session.MessageContext,
                    Array.Empty<MonitoredItemNotificationModel>(), _timeProvider)
                {
                    ApplicationUri = session.Endpoint?.Server?.ApplicationUri,
                    EndpointUrl = session.Endpoint?.EndpointUrl,
                    PublishTimestamp = publishTime,
                    SequenceNumber = Opc.Ua.SequenceNumber.Increment32(ref _sequenceNumber),
                    MessageType = MessageType.KeepAlive
                };
#pragma warning restore CA2000 // Dispose objects before losing scope
                foreach (var callback in CurrentlyMonitored
                    .Select(c => c.Owner)
                    .Distinct())
                {
                    callback.OnSubscriptionKeepAlive(message);
                }

                Debug.Assert(message.Notifications != null);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Exception processing keep alive notification");
            }
            finally
            {
                _logger.LogDebug("Keep alive callback took {Elapsed}", sw.Elapsed);
                if (sw.ElapsedMilliseconds > 1000)
                {
                    _logger.LogWarning("Spent more than 1 second in fast keep alive callback.");
                }
            }
        }

        /// <summary>
        /// Handle cyclic read notifications created by the client
        /// </summary>
        /// <param name="subscription"></param>
        /// <param name="values"></param>
        /// <param name="sequenceNumber"></param>
        /// <param name="publishTime"></param>
        public void OnSubscriptionCylicReadNotification(Subscription subscription,
            List<SampledDataValueModel> values, uint sequenceNumber, DateTime publishTime)
        {
            Debug.Assert(ReferenceEquals(subscription, this));
            ObjectDisposedException.ThrowIf(_disposed, this);
            var session = Session;
            if (session is not IOpcUaSession sessionContext)
            {
                _logger.LogWarning(
                    "DataChange for subscription {Subscription} received without session {Session}.",
                    this, session);
                return;
            }

            var sw = Stopwatch.StartNew();
            try
            {
                var collector = new OpcUaMonitoredItem.MonitoredItemNotifications();
                foreach (var cyclicDataChange in values.OrderBy(m => m.Value?.SourceTimestamp))
                {
                    if (TryGetMonitoredItemForNotification(cyclicDataChange.ClientHandle, out var monitoredItem) &&
                        !monitoredItem.TryGetMonitoredItemNotifications(publishTime, cyclicDataChange, collector))
                    {
                        _logger.LogDebug(
                            "Skipping the cyclic read data change received for subscription {Subscription}", this);
                    }
                }

                foreach (var (callback, notifications) in collector.Notifications)
                {
#pragma warning disable CA2000 // Dispose objects before losing scope
                    var message = new OpcUaSubscriptionNotification(this, session.MessageContext, notifications,
                        _timeProvider, null, sequenceNumber)
                    {
                        ApplicationUri = session.Endpoint?.Server?.ApplicationUri,
                        EndpointUrl = session.Endpoint?.EndpointUrl,
                        PublishTimestamp = publishTime,
                        SequenceNumber = Opc.Ua.SequenceNumber.Increment32(ref _sequenceNumber),
                        MessageType = MessageType.DeltaFrame
                    };
#pragma warning restore CA2000 // Dispose objects before losing scope

                    callback.OnSubscriptionCyclicReadCompleted(message);
                    Debug.Assert(message.Notifications != null);
                    var count = message.GetDiagnosticCounters(out var _, out _, out var overflows);
                    if (count > 0)
                    {
                        callback.OnSubscriptionCyclicReadDiagnosticsChange(count, overflows);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Exception processing cyclic read notification");
            }
            finally
            {
                _logger.LogDebug("Cyclic read callback took {Elapsed}", sw.Elapsed);
                if (sw.ElapsedMilliseconds > 1000)
                {
                    _logger.LogWarning("Spent more than 1 second in cyclic read callback.");
                }
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
            DataChangeNotification notification, IList<string>? stringTable)
        {
            Debug.Assert(ReferenceEquals(subscription, this));
            ObjectDisposedException.ThrowIf(_disposed, this);

            var firstDataChangeReceived = _firstDataChangeReceived;
            _firstDataChangeReceived = true;
            var session = Session;
            if (session is not IOpcUaSession sessionContext)
            {
                _logger.LogWarning(
                    "DataChange for subscription {Subscription} received without session {Session}.",
                    this, session);
                return;
            }

            ResetKeepAliveTimer();

            var sw = Stopwatch.StartNew();
            try
            {
                var sequenceNumber = notification.SequenceNumber;
                var publishTime = notification.PublishTime;

                // All notifications have the same message and thus sequence number
                if (sequenceNumber == 1)
                {
                    // Do not log when the sequence number is 1 after reconnect
                    _previousSequenceNumber = 1;
                }
                else if (!Opc.Ua.SequenceNumber.Validate(sequenceNumber, ref _previousSequenceNumber,
                    out var missingSequenceNumbers, out var dropped))
                {
                    _logger.LogWarning("DataChange notification for subscription " +
                        "{Subscription} has unexpected sequenceNumber {SequenceNumber} " +
                        "missing {ExpectedSequenceNumber} which were {Dropped}, publishTime {PublishTime}",
                        this, sequenceNumber,
                        Opc.Ua.SequenceNumber.ToString(missingSequenceNumbers),
                        dropped ? "dropped" : "already received", publishTime);
                }

                // Collect notifications
                var collector = new OpcUaMonitoredItem.MonitoredItemNotifications();
                foreach (var item in notification.MonitoredItems)
                {
                    Debug.Assert(item != null);
                    if (TryGetMonitoredItemForNotification(item.ClientHandle, out var monitoredItem) &&
                        !monitoredItem.TryGetMonitoredItemNotifications(publishTime, item, collector))
                    {
                        _logger.LogDebug(
                            "Skipping the monitored item notification for DataChange " +
                            "received for subscription {Subscription}", this);
                    }
                }

                // Send to listeners
#pragma warning disable CA2000 // Dispose objects before losing scope
                var advance = new Advance(this, sequenceNumber, collector.Notifications.Count);
#pragma warning restore CA2000 // Dispose objects before losing scope
                foreach (var (callback, notifications) in collector.Notifications)
                {
#pragma warning disable CA2000 // Dispose objects before losing scope
                    var message = new OpcUaSubscriptionNotification(this, session.MessageContext,
                        notifications, _timeProvider, advance, sequenceNumber)
                    {
                        ApplicationUri = session.Endpoint?.Server?.ApplicationUri,
                        EndpointUrl = session.Endpoint?.EndpointUrl,
                        PublishTimestamp = publishTime,
                        SequenceNumber = Opc.Ua.SequenceNumber.Increment32(ref _sequenceNumber),
                        MessageType =
                            firstDataChangeReceived ? MessageType.DeltaFrame : MessageType.KeyFrame
                    };
#pragma warning restore CA2000 // Dispose objects before losing scope
                    Debug.Assert(notification.MonitoredItems != null);

                    callback.OnSubscriptionDataChangeReceived(message);
                    Debug.Assert(message.Notifications != null);
                    var count = message.GetDiagnosticCounters(out var _, out var heartbeats, out var overflows);
                    if (count > 0)
                    {
                        callback.OnSubscriptionDataDiagnosticsChange(true, count, overflows, heartbeats);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Exception processing subscription notification");
            }
            finally
            {
                _logger.LogDebug("Data change callback took {Elapsed}", sw.Elapsed);
                if (sw.ElapsedMilliseconds > 1000)
                {
                    _logger.LogWarning("Spent more than 1 second in fast data change callback.");
                }
            }
        }

        /// <summary>
        /// Get monitored item using client handle
        /// </summary>
        /// <param name="clientHandle"></param>
        /// <param name="monitoredItem"></param>
        /// <returns></returns>
        private bool TryGetMonitoredItemForNotification(uint clientHandle,
            [NotNullWhen(true)] out OpcUaMonitoredItem? monitoredItem)
        {
            monitoredItem = FindItemByClientHandle(clientHandle) as OpcUaMonitoredItem;
            if (monitoredItem != null)
            {
                return true;
            }

            _unassignedNotifications++;
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug(
                    "Monitored item not found with client handle {ClientHandle} in subscription {Subscription}.",
                    clientHandle, this);
            }
            return false;
        }

        /// <summary>
        /// Get notifications
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="notifications"></param>
        /// <returns></returns>
        internal bool TryGetNotifications(ISubscriber owner,
            [NotNullWhen(true)] out IList<MonitoredItemNotificationModel>? notifications)
        {
            try
            {
                if (IsClosed)
                {
                    notifications = null;
                    return false;
                }

                var collector = new OpcUaMonitoredItem.MonitoredItemNotifications();
                // Ensure we order by client handle exactly like the meta data is ordered
                foreach (var item in CurrentlyMonitored
                    .Where(m => m.Owner == owner).OrderBy(m => m.ClientHandle))
                {
                    item.TryGetLastMonitoredItemNotifications(collector);
                }

                if (!collector.Notifications.TryGetValue(owner, out var actualNotifications))
                {
                    notifications = null;
                    return false;
                }
                notifications = actualNotifications;
                return true;
            }
            catch (Exception ex)
            {
                notifications = null;
                _logger.LogError(ex, "Failed to get a notifications from monitored " +
                    "items in subscription {Subscription}.", this);
                return false;
            }
        }

        /// <summary>
        /// Reset keep alive timer
        /// </summary>
        private void ResetKeepAliveTimer()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _continuouslyMissingKeepAlives = 0;

            if (!IsOnline)
            {
                _keepAliveWatcher.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
                return;
            }

            var keepAliveTimeout = TimeSpan.FromMilliseconds(
                (CurrentPublishingInterval * (CurrentKeepAliveCount + 1)) + 1000);
            try
            {
                _keepAliveWatcher.Change(keepAliveTimeout, keepAliveTimeout);
            }
            catch (ArgumentOutOfRangeException)
            {
                _keepAliveWatcher.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            }
        }

        /// <summary>
        /// Reset the monitored item watchdog
        /// </summary>
        /// <param name="publishingEnabled"></param>
        private void ResetMonitoredItemWatchdogTimer(bool publishingEnabled)
        {
            var timeout = MonitoredItemWatchdogTimeout;
            if (timeout == TimeSpan.Zero)
            {
                if (_lastMonitoredItemCheck == null)
                {
                    return;
                }
                publishingEnabled = false;
            }
            if (!publishingEnabled)
            {
                if (_lastMonitoredItemCheck != null)
                {
                    _logger.LogInformation(
                        "{Subscription}: Stopping monitored item watchdog ({Timeout}).",
                        this, timeout);
                }
                _lastMonitoredItemCheck = null;
                _monitoredItemWatcher.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            }
            else
            {
                if (_lastMonitoredItemCheck == null)
                {
                    _logger.LogInformation(
                        "{Subscription}: Restarting monitored item watchdog ({Timeout}).",
                        this, timeout);
                }

                _lastMonitoredItemCheck = _timeProvider.GetUtcNow();
                Debug.Assert(timeout != TimeSpan.Zero);
                _monitoredItemWatcher.Change(timeout, timeout);
            }
        }

        /// <summary>
        /// Checks status of monitored items
        /// </summary>
        /// <param name="state"></param>
        private void OnMonitoredItemWatchdog(object? state)
        {
            var action = WatchdogBehavior ?? SubscriptionWatchdogBehavior.Diagnostic;
            lock (_timers)
            {
                if (_disposed || _monitoredItemWatcher == null)
                {
                    Debug.Fail("Should not be called after dispose");
                    return;
                }

                if (!IsOnline || _lastMonitoredItemCheck == null)
                {
                    // Stop watchdog
                    _monitoredItemWatcher.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
                    return;
                }

                if (_goodMonitoredItems == 0)
                {
                    _lastMonitoredItemCheck = _timeProvider.GetUtcNow();
                    return;
                }

                var lastCount = _lateMonitoredItems;
                var itemsChecked = 0;
                foreach (var item in CurrentlyMonitored)
                {
                    itemsChecked++;
                    if (item.WasLastValueReceivedBefore(_lastMonitoredItemCheck.Value))
                    {
                        _logger.LogDebug(
                            "Monitored item {Item} in subscription {Subscription} is late.",
                            item, this);
                        _lateMonitoredItems++;
                    }
                }
                _lastMonitoredItemCheck = _timeProvider.GetUtcNow();
                var missing = _lateMonitoredItems - lastCount;
                if (missing == 0)
                {
                    _logger.LogDebug("All monitored items in {Subscription} are reporting.",
                        this);
                    return;
                }
                if (action == SubscriptionWatchdogBehavior.Diagnostic)
                {
                    return;
                }
                if (itemsChecked != missing && WatchdogCondition
                    != MonitoredItemWatchdogCondition.WhenAllAreLate)
                {
                    _logger.LogDebug("Some monitored items in {Subscription} are late.",
                        this);
                    return;
                }
                _logger.LogInformation("{Count} of the {Total} monitored items in " +
                    "{Subscription} are now late - running {Action} behavior action.",
                    missing, itemsChecked, this, action);
            }

            var msg = $"Performed watchdog action {action} for subscription {this} " +
                $"because it has {_lateMonitoredItems} late monitored items.";
            RunWatchdogAction(action, msg);
        }

        /// <summary>
        /// Run watchdog action
        /// </summary>
        /// <param name="action"></param>
        /// <param name="msg"></param>
        private void RunWatchdogAction(SubscriptionWatchdogBehavior action, string msg)
        {
            switch (action)
            {
                case SubscriptionWatchdogBehavior.Diagnostic:
                    _logger.LogCritical("{Message}", msg);
                    break;
                case SubscriptionWatchdogBehavior.Reset:
                    ResetMonitoredItemWatchdogTimer(false);
                    _forceRecreate = true;
                    break;
                case SubscriptionWatchdogBehavior.FailFast:
                    Publisher.Runtime.FailFast(msg, null);
                    break;
                case SubscriptionWatchdogBehavior.ExitProcess:
                    Console.WriteLine(msg);
                    Publisher.Runtime.Exit(-10);
                    break;
            }
        }

        /// <summary>
        /// Called when keep alive callback was missing
        /// </summary>
        /// <param name="state"></param>
        private void OnKeepAliveMissing(object? state)
        {
            lock (_timers)
            {
                if (_disposed)
                {
                    Debug.Fail("Should not be called after dispose");
                    return;
                }

                if (!IsOnline)
                {
                    // Stop watchdog
                    _keepAliveWatcher.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
                    return;
                }

                _missingKeepAlives++;
                _continuouslyMissingKeepAlives++;

                if (_continuouslyMissingKeepAlives == CurrentLifetimeCount + 1)
                {
                    var action = WatchdogBehavior ?? SubscriptionWatchdogBehavior.Reset;
                    _logger.LogCritical(
                        "#{Count}/{Lifetimecount}: Keep alive count exceeded. Perform {Action} for {Subscription}...",
                        _continuouslyMissingKeepAlives, CurrentLifetimeCount, action, this);

                    RunWatchdogAction(action, $"Subscription {this}: Keep alives exceeded " +
                        $"({_continuouslyMissingKeepAlives}/{CurrentLifetimeCount}).");
                }
                else
                {
                    _logger.LogInformation(
                        "#{Count}/{Lifetimecount}: Subscription {Subscription} is missing keep alive.",
                        _continuouslyMissingKeepAlives, CurrentLifetimeCount, this);
                }
            }
        }

        /// <summary>
        /// Publish status changed
        /// </summary>
        /// <param name="subscription"></param>
        /// <param name="e"></param>
        private void OnPublishStatusChange(Subscription subscription, PublishStateChangedEventArgs e)
        {
            if (_disposed)
            {
                return;
            }
            if (e.Status.HasFlag(PublishStateChangedMask.Stopped) && !_publishingStopped)
            {
                _logger.LogInformation("Subscription {Subscription} STOPPED!", this);
                _keepAliveWatcher.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
                ResetMonitoredItemWatchdogTimer(false);
                _publishingStopped = true;
            }
            if (e.Status.HasFlag(PublishStateChangedMask.Recovered) && _publishingStopped)
            {
                _logger.LogInformation("Subscription {Subscription} RECOVERED!", this);
                ResetKeepAliveTimer();
                ResetMonitoredItemWatchdogTimer(true);
                _publishingStopped = false;
            }
            if (e.Status.HasFlag(PublishStateChangedMask.Transferred))
            {
                _logger.LogInformation("Subscription {Subscription} transferred.", this);
            }
            if (e.Status.HasFlag(PublishStateChangedMask.Republish))
            {
                _logger.LogInformation("Subscription {Subscription} republishing...", this);
            }
            if (e.Status.HasFlag(PublishStateChangedMask.KeepAlive))
            {
                _logger.LogTrace("Subscription {Subscription} keep alive.", this);
                ResetKeepAliveTimer();
            }
            if (e.Status.HasFlag(PublishStateChangedMask.Timeout))
            {
                var action = WatchdogBehavior ?? SubscriptionWatchdogBehavior.Reset;
                _logger.LogInformation("Subscription {Subscription} TIMEOUT! ---- " +
                    "Server closed subscription - performing recovery action {Action}...",
                    this, action);

                //
                // Timed out on server - this means that the subscription is gone and
                // needs to be recreated. This is the default watchdog behavior.
                //
                RunWatchdogAction(action, $"Subscription {this} timed out!");
            }
        }

        /// <summary>
        /// Subscription status changed
        /// </summary>
        /// <param name="subscription"></param>
        /// <param name="e"></param>
        private void OnStateChange(Subscription subscription, SubscriptionStateChangedEventArgs e)
        {
            if (e.Status.HasFlag(SubscriptionChangeMask.Created))
            {
                _logger.LogDebug("Subscription {Subscription} created.", this);
                _publishingStopped = false;
            }
            if (e.Status.HasFlag(SubscriptionChangeMask.Deleted))
            {
                _logger.LogDebug("Subscription {Subscription} deleted.", this);
            }
            if (e.Status.HasFlag(SubscriptionChangeMask.Modified))
            {
                _logger.LogDebug("Subscription {Subscription} modified", this);
            }
            if (e.Status.HasFlag(SubscriptionChangeMask.ItemsAdded))
            {
                _logger.LogDebug("Subscription {Subscription} items added.", this);
            }
            if (e.Status.HasFlag(SubscriptionChangeMask.ItemsRemoved))
            {
                _logger.LogDebug("Subscription {Subscription} items removed.", this);
            }
            if (e.Status.HasFlag(SubscriptionChangeMask.ItemsCreated))
            {
                _logger.LogDebug("Subscription {Subscription} items created.", this);
            }
            if (e.Status.HasFlag(SubscriptionChangeMask.ItemsDeleted))
            {
                _logger.LogDebug("Subscription {Subscription} items deleted.", this);
            }
            if (e.Status.HasFlag(SubscriptionChangeMask.ItemsModified))
            {
                _logger.LogDebug("Subscription {Subscription} items modified.", this);
            }
            if (e.Status.HasFlag(SubscriptionChangeMask.Transferred))
            {
                _logger.LogDebug("Subscription {Subscription} transferred.", this);
            }
        }

        /// <summary>
        /// Helper to partition subscribers across subscriptions. Uses a bag packing
        /// algorithm.
        /// </summary>
        private sealed class Partition
        {
            /// <summary>
            /// Monitored items that should be in the subscription partition
            /// </summary>
            public List<(ISubscriber, BaseMonitoredItemModel)> Items { get; } = [];

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

        /// <summary>
        /// Helper to advance the sequence number when all notifications are
        /// completed.
        /// </summary>
        private sealed class Advance : IDisposable
        {
            /// <summary>
            /// Create helper
            /// </summary>
            /// <param name="opcUaSubscription"></param>
            /// <param name="sequenceNumber"></param>
            /// <param name="count"></param>
            public Advance(OpcUaSubscription opcUaSubscription,
                uint sequenceNumber, int count)
            {
                _count = count;
                _opcUaSubscription = opcUaSubscription;
                _subscriptionId = opcUaSubscription.Id;
                _sequenceNumber = sequenceNumber;
            }

            /// <inheritdoc/>
            public void Dispose()
            {
                var done = Interlocked.Decrement(ref _count);
                Debug.Assert(done >= 0);
                if (done == 0 && _opcUaSubscription.Id == _subscriptionId)
                {
                    _opcUaSubscription._logger.LogTrace(
                        "Advancing stream #{SubscriptionId} to #{Position}",
                        _subscriptionId, _sequenceNumber);
                    _opcUaSubscription._currentSequenceNumber = _sequenceNumber;
                }
            }

            private readonly OpcUaSubscription _opcUaSubscription;
            private readonly uint _subscriptionId;
            private readonly uint _sequenceNumber;
            private int _count;
        }

        private int HeartbeatsEnabled
            => MonitoredItems.Count(r => r is OpcUaMonitoredItem.Heartbeat h && h.TimerEnabled);
        private int ConditionsEnabled
            => MonitoredItems.Count(r => r is OpcUaMonitoredItem.Condition h && h.TimerEnabled);
        private IOpcUaClientDiagnostics State
            => (_client as IOpcUaClientDiagnostics) ?? OpcUaClient.Disconnected;

        /// <summary>
        /// Create observable metrics
        /// </summary>
        public void InitializeMetrics()
        {
            _meter.CreateObservableCounter("iiot_edge_publisher_missing_keep_alives",
                () => new Measurement<long>(_missingKeepAlives, _metrics.TagList),
                description: "Number of missing keep alives in subscription.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_monitored_items",
                () => new Measurement<long>(MonitoredItemCount, _metrics.TagList),
                description: "Total monitored item count.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_disabled_nodes",
                () => new Measurement<long>(_disabledItems, _metrics.TagList),
                description: "Monitored items with monitoring mode disabled.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_nodes_monitoring_mode_inconsistent",
                () => new Measurement<long>(_notAppliedItems, _metrics.TagList),
                description: "Monitored items with monitoring mode not applied.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_reporting_nodes",
                () => new Measurement<long>(_reportingItems, _metrics.TagList),
                description: "Monitored items reporting.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_sampling_nodes",
                () => new Measurement<long>(_samplingItems, _metrics.TagList),
                description: "Monitored items with sampling enabled.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_heartbeat_nodes",
                () => new Measurement<long>(_heartbeatItems, _metrics.TagList),
                description: "Monitored items with heartbeats configured.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_heartbeat_enabled_nodes",
                () => new Measurement<long>(HeartbeatsEnabled, _metrics.TagList),
                description: "Monitored items with heartbeats enabled.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_condition_nodes",
                () => new Measurement<long>(_conditionItems, _metrics.TagList),
                description: "Monitored items with condition monitoring configured.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_condition_enabled_nodes",
                () => new Measurement<long>(ConditionsEnabled, _metrics.TagList),
                description: "Monitored items with condition monitoring enabled.");
            _meter.CreateObservableCounter("iiot_edge_publisher_unassigned_notification_count",
                () => new Measurement<long>(_unassignedNotifications, _metrics.TagList),
                description: "Number of notifications that could not be assigned.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_good_nodes",
                () => new Measurement<long>(_goodMonitoredItems, _metrics.TagList),
                description: "Monitored items successfully created.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_bad_nodes",
                () => new Measurement<long>(_badMonitoredItems, _metrics.TagList),
                description: "Monitored items that were not successfully created.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_late_nodes",
                () => new Measurement<long>(_lateMonitoredItems, _metrics.TagList),
                description: "Monitored items that are late reporting.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_subscription_stopped_count",
                () => new Measurement<int>(_publishingStopped ? 1 : 0, _metrics.TagList),
                description: "Number of subscriptions that stopped publishing.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_deferred_acks_enabled_count",
                () => new Measurement<int>(UseDeferredAcknoledgements ? 1 : 0, _metrics.TagList),
                description: "Number of subscriptions with deferred acknoledgements enabled.");
            _meter.CreateObservableCounter("iiot_edge_publisher_deferred_acks_last_sequencenumber",
                () => new Measurement<long>(_sequenceNumber, _metrics.TagList),
                description: "Sequence number of the last notification received in subscription.");
            _meter.CreateObservableCounter("iiot_edge_publisher_deferred_acks_completed_sequencenumber",
                () => new Measurement<long>(_currentSequenceNumber, _metrics.TagList),
                description: "Sequence number of the next notification to acknoledge in subscription.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_publish_requests_per_subscription",
                () => new Measurement<double>(Ratio(State.OutstandingRequestCount, State.SubscriptionCount),
                _metrics.TagList), description: "Good publish requests per subsciption.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_good_publish_requests_per_subscription",
                () => new Measurement<double>(Ratio(State.GoodPublishRequestCount, State.SubscriptionCount),
                _metrics.TagList), description: "Good publish requests per subsciption.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_bad_publish_requests_per_subscription",
                () => new Measurement<double>(Ratio(State.BadPublishRequestCount, State.SubscriptionCount),
                _metrics.TagList), description: "Bad publish requests per subsciption.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_min_publish_requests_per_subscription",
                () => new Measurement<double>(Ratio(State.MinPublishRequestCount, State.SubscriptionCount),
                _metrics.TagList), description: "Min publish requests queued per subsciption.");

            static double Ratio(int value, int count) => count == 0 ? 0.0 : (double)value / count;
        }

        private const int kMaxMonitoredItemPerSubscriptionDefault = 64 * 1024;
        private uint _previousSequenceNumber;
        private uint _sequenceNumber;
        private uint _currentSequenceNumber;
        private bool _firstDataChangeReceived;
        private bool _forceRecreate;
        private uint? _childId;
        private readonly uint? _parentId;
        private readonly OpcUaClient _client;
        private readonly IOptions<OpcUaSubscriptionOptions> _options;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;
        private readonly IMetricsContext _metrics;
        private readonly ITimer _keepAliveWatcher;
        private readonly ITimer _monitoredItemWatcher;
        private readonly TimeProvider _timeProvider;
        private readonly Meter _meter = Diagnostics.NewMeter();
        private static uint _lastIndex;
        private DateTimeOffset? _lastMonitoredItemCheck;
        private int _goodMonitoredItems;
        private int _reportingItems;
        private int _disabledItems;
        private int _samplingItems;
        private int _notAppliedItems;
        private int _heartbeatItems;
        private int _conditionItems;
        private int _lateMonitoredItems;
        private int _badMonitoredItems;
        private int _errorsDuringSync;
        private int _missingKeepAlives;
        private int _continuouslyMissingKeepAlives;
        private long _unassignedNotifications;
        private bool _publishingStopped;
        private bool _disposed;
        private readonly Lock _timers = new();
    }
}
