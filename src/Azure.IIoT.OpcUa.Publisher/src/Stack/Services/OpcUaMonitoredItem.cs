// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Services
{
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Encoders.PubSub;
    using Microsoft.Extensions.Logging;
    using Opc.Ua;
    using Opc.Ua.Client;
    using Opc.Ua.Client.ComplexTypes;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Update node id
    /// </summary>
    /// <param name="nodeId"></param>
    /// <param name="messageContext"></param>
    public delegate void UpdateNodeId(NodeId nodeId,
        IServiceMessageContext messageContext);

    /// <summary>
    /// Callback
    /// </summary>
    /// <param name="messageType"></param>
    /// <param name="notifications"></param>
    /// <param name="session"></param>
    /// <param name="diagnosticsOnly"></param>
    public delegate void Callback(MessageType messageType,
        IEnumerable<MonitoredItemNotificationModel> notifications,
        ISession? session = null, bool diagnosticsOnly = false);

    /// <summary>
    /// Monitored item
    /// </summary>
    internal abstract partial class OpcUaMonitoredItem : MonitoredItem,
        IDisposable
    {
        /// <summary>
        /// Assigned monitored item id on server
        /// </summary>
        public uint? RemoteId => Created ? Status.Id : null;

        /// <summary>
        /// The item is valid once added to the subscription. Contract:
        /// The item will be invalid until the subscription calls
        /// <see cref="AddTo(Subscription, IOpcUaSession)"/>
        /// to add it to the subscription. After removal the item
        /// is still Valid, but not Created. The item is
        /// again invalid after <see cref="IDisposable.Dispose"/> is
        /// called.
        /// </summary>
        public bool Valid { get; protected internal set; }

        /// <summary>
        /// Order of the item
        /// </summary>
        public int Order { get; }

        /// <summary>
        /// Whether the item is part of a subscription or not
        /// </summary>
        public bool AttachedToSubscription => Subscription != null;

        /// <summary>
        /// Registered read node updater. If this property is null then
        /// the node does not need to be registered.
        /// </summary>
        public virtual (string NodeId, UpdateNodeId Update)? Register
            => null;

        /// <summary>
        /// Resolve relative path first. If this returns null
        /// the relative path either does not exist or we let
        /// subscription take care of resolving the path.
        /// </summary>
        public virtual (string NodeId, string[] Path, UpdateNodeId Update)? Resolve
            => null;

        /// <summary>
        /// Last saved value
        /// </summary>
        public IEncodeable? LastReceivedValue { get; private set; }

        /// <summary>
        /// Create item
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="order"></param>
        protected OpcUaMonitoredItem(ILogger logger, int order)
        {
            Order = order;
            _logger = logger;
        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="item"></param>
        /// <param name="copyEventHandlers"></param>
        /// <param name="copyClientHandle"></param>
        protected OpcUaMonitoredItem(OpcUaMonitoredItem item,
            bool copyEventHandlers, bool copyClientHandle)
            : base(item, copyEventHandlers, copyClientHandle)
        {
            Order = item.Order;
            _logger = item._logger;

            LastReceivedValue = item.LastReceivedValue;
            Valid = item.Valid;
        }

        /// <inheritdoc/>
        public override abstract MonitoredItem CloneMonitoredItem(
            bool copyEventHandlers, bool copyClientHandle);

        /// <inheritdoc/>
        public override object Clone()
        {
            return CloneMonitoredItem(true, true);
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj is OpcUaMonitoredItem)
            {
                return true;
            }
            return false;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return 252343123;
        }

        /// <summary>
        /// Create items
        /// </summary>
        /// <param name="items"></param>
        /// <param name="factory"></param>
        /// <param name="client"></param>
        /// <returns></returns>
        public static IEnumerable<OpcUaMonitoredItem> Create(
            IEnumerable<BaseItemModel> items, ILoggerFactory factory,
            IOpcUaClient? client = null)
        {
            foreach (var item in items)
            {
                switch (item)
                {
                    case DataMonitoredItemModel dmi:
                        if (dmi.SamplingUsingCyclicRead &&
                            client != null)
                        {
                            yield return new CyclicRead(client,
                                dmi, factory.CreateLogger<CyclicRead>());
                        }
                        else if (dmi.HeartbeatInterval != null)
                        {
                            yield return new Heartbeat(dmi,
                                factory.CreateLogger<Heartbeat>());
                        }
                        else
                        {
                            yield return new DataChange(dmi,
                                factory.CreateLogger<DataChange>());
                        }
                        break;
                    case EventMonitoredItemModel emi:
                        if (emi.ConditionHandling?.SnapshotInterval != null)
                        {
                            yield return new Condition(emi,
                                factory.CreateLogger<Condition>());
                        }
                        else
                        {
                            yield return new Event(emi,
                                factory.CreateLogger<Event>());
                        }
                        break;
                    case MonitoredAddressSpaceModel mam:
                        if (client != null)
                        {
                            yield return new ModelChangeEventItem(mam, client,
                                factory.CreateLogger<ModelChangeEventItem>());
                        }
                        break;
                    case ExtensionFieldItemModel efm:
                        yield return new Field(efm,
                            factory.CreateLogger<Field>());
                        break;
                    case ConfigurationErrorItemModel epi:
                        yield return new Error(epi,
                            factory.CreateLogger<Error>());
                        break;
                    default:
                        Debug.Fail($"Unexpected type of item {item}");
                        break;
                }
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing && Valid)
            {
                if (AttachedToSubscription)
                {
                    // The item should have been removed from the subscription
                    _logger.LogError("Unexpected state: Item {Item} must " +
                        "already be removed from subscription, but wasn't.", this);
                }
                Valid = false;
            }
        }

        /// <summary>
        /// Add the item to the subscription
        /// </summary>
        /// <param name="subscription"></param>
        /// <param name="session"></param>
        /// <returns></returns>
        public virtual bool AddTo(Subscription subscription, IOpcUaSession session)
        {
            if (Valid)
            {
                subscription.AddItem(this);
                _logger.LogDebug(
                    "Added monitored item {Item} to subscription #{SubscriptionId}.",
                    this, subscription.Id);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Finalize add
        /// </summary>
        public virtual Func<IOpcUaSession, CancellationToken, Task>? FinalizeAddTo { get; }

        /// <summary>
        /// Merge item in the subscription with this item
        /// </summary>
        /// <param name="item"></param>
        /// <param name="session"></param>
        /// <returns></returns>
        public abstract bool MergeWith(OpcUaMonitoredItem item, IOpcUaSession session);

        /// <summary>
        /// Finalize merge
        /// </summary>
        public virtual Func<IOpcUaSession, CancellationToken, Task>? FinalizeMergeWith { get; }

        /// <summary>
        /// Remove from subscription
        /// </summary>
        /// <param name="subscription"></param>
        /// <returns></returns>
        public virtual bool RemoveFrom(Subscription subscription)
        {
            if (AttachedToSubscription)
            {
                subscription.RemoveItem(this);
                _logger.LogDebug(
                    "Removed monitored item {Item} from subscription #{SubscriptionId}.",
                    this, subscription.Id);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Complete changes previously made and provide callback
        /// </summary>
        /// <param name="subscription"></param>
        /// <param name="applyChanges"></param>
        /// <param name="cb"></param>
        /// <returns></returns>
        public virtual bool TryCompleteChanges(Subscription subscription,
            ref bool applyChanges, Callback cb)
        {
            if (!Valid)
            {
                return false;
            }

            if (!AttachedToSubscription)
            {
                _logger.LogDebug(
                    "Item {Item} removed from subscription #{SubscriptionId} with {Status}.",
                    this, subscription.Id, Status.Error);
                // Complete removal
                return true;
            }

            if (Status.MonitoringMode == Opc.Ua.MonitoringMode.Disabled)
            {
                return false;
            }

            if (Status.Error == null || !StatusCode.IsNotGood(Status.Error.StatusCode))
            {
                if (SamplingInterval != Status.SamplingInterval ||
                    QueueSize != Status.QueueSize)
                {
                    _logger.LogInformation(
                        @"Server has revised {Item} ('{Name}') in subscription #{SubscriptionId}
The item's actual/desired states:
SamplingInterval {CurrentSamplingInterval}/{SamplingInterval},
QueueSize {CurrentQueueSize}/{QueueSize}",
                        StartNodeId, DisplayName, subscription.Id,
                        Status.SamplingInterval, SamplingInterval,
                        Status.QueueSize, QueueSize);
                }
                else
                {
                    _logger.LogDebug(
                        "Item {Item} added to subscription #{SubscriptionId} successfully.",
                        this, subscription.Id);
                }
                return true;
            }

            _logger.LogWarning(
                "Error adding monitored item {Item} to subscription #{SubscriptionId} due to {Status}.",
                this, subscription.Id, Status.Error);

            // Not needed, mode changes applied after
            // applyChanges = true;
            return false;
        }

        /// <summary>
        /// Called on all items after monitoring mode was changed
        /// successfully.
        /// </summary>
        /// <returns></returns>
        public virtual Func<CancellationToken, Task>? FinalizeCompleteChanges { get; }

        /// <summary>
        /// Get any changes in the monitoring mode to apply if any.
        /// Otherwise the returned value is null.
        /// </summary>
        public virtual Opc.Ua.MonitoringMode? GetMonitoringModeChange()
        {
            if (!AttachedToSubscription || !Valid)
            {
                return null;
            }
            var currentMode = Status?.MonitoringMode
                ?? Opc.Ua.MonitoringMode.Disabled;
            var desiredMode = MonitoringMode;
            return currentMode != desiredMode ? desiredMode : null;
        }

        /// <summary>
        /// Called on all items after monitoring mode was changed
        /// successfully.
        /// </summary>
        /// <returns></returns>
        public virtual Func<CancellationToken, Task>? FinalizeMonitoringModeChange { get; }

        /// <summary>
        /// Try get monitored item notifications from
        /// the subscription's monitored item event payload.
        /// </summary>
        /// <param name="sequenceNumber"></param>
        /// <param name="timestamp"></param>
        /// <param name="encodeablePayload"></param>
        /// <param name="notifications"></param>
        /// <returns></returns>
        public virtual bool TryGetMonitoredItemNotifications(uint sequenceNumber, DateTime timestamp,
            IEncodeable encodeablePayload, IList<MonitoredItemNotificationModel> notifications)
        {
            if (!Valid)
            {
                return false;
            }
            LastReceivedValue = encodeablePayload;
            return true;
        }

        /// <summary>
        /// Get last monitored item notification saved
        /// </summary>
        /// <param name="sequenceNumber"></param>
        /// <param name="notifications"></param>
        /// <returns></returns>
        public virtual bool TryGetLastMonitoredItemNotifications(uint sequenceNumber,
            IList<MonitoredItemNotificationModel> notifications)
        {
            var lastValue = LastReceivedValue;
            if (lastValue == null || Status?.Error != null)
            {
                return TryGetErrorMonitoredItemNotifications(sequenceNumber,
                    Status?.Error.StatusCode ?? StatusCodes.GoodNoData,
                    notifications);
            }
            return TryGetMonitoredItemNotifications(sequenceNumber, DateTime.UtcNow,
                lastValue, notifications);
        }

        /// <summary>
        /// Create triggered items
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="client"></param>
        /// <returns></returns>
        protected abstract IEnumerable<OpcUaMonitoredItem> CreateTriggeredItems(
            ILoggerFactory factory, IOpcUaClient? client = null);

        /// <summary>
        /// Add error to notification list
        /// </summary>
        /// <param name="sequenceNumber"></param>
        /// <param name="statusCode"></param>
        /// <param name="notifications"></param>
        /// <returns></returns>
        protected abstract bool TryGetErrorMonitoredItemNotifications(
            uint sequenceNumber, StatusCode statusCode,
            IList<MonitoredItemNotificationModel> notifications);

        /// <summary>
        /// Merge item
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="template"></param>
        /// <param name="desired"></param>
        /// <param name="updated"></param>
        /// <returns></returns>
        protected bool MergeWith<T>(T template, T desired, out T updated)
            where T : BaseMonitoredItemModel
        {
            updated = template;

            if (!Valid)
            {
                return false;
            }

            var itemChange = false;
            if ((updated.DiscardNew ?? false) !=
                    desired.DiscardNew.GetValueOrDefault())
            {
                _logger.LogDebug("{Item}: Changing discard new mode from {Old} to {New}",
                    this, updated.DiscardNew ?? false,
                    desired.DiscardNew ?? false);
                updated = updated with { DiscardNew = desired.DiscardNew };
                DiscardOldest = !(updated.DiscardNew ?? false);
                itemChange = true;
            }
            if (updated.QueueSize != desired.QueueSize)
            {
                _logger.LogDebug("{Item}: Changing queue size from {Old} to {New}",
                    this, updated.QueueSize,
                    desired.QueueSize);
                updated = updated with { QueueSize = desired.QueueSize };
                QueueSize = updated.QueueSize;
                itemChange = true;
            }
            if ((updated.MonitoringMode ?? Publisher.Models.MonitoringMode.Reporting) !=
                (desired.MonitoringMode ?? Publisher.Models.MonitoringMode.Reporting))
            {
                _logger.LogDebug("{Item}: Changing monitoring mode from {Old} to {New}",
                    this, updated.MonitoringMode ?? Publisher.Models.MonitoringMode.Reporting,
                    desired.MonitoringMode ?? Publisher.Models.MonitoringMode.Reporting);
                updated = updated with { MonitoringMode = desired.MonitoringMode };
                MonitoringMode = updated.MonitoringMode.ToStackType()
                    ?? Opc.Ua.MonitoringMode.Reporting;

                // Not a change yet, will be done as bulk update
                // itemChange = true;
            }
            return itemChange;
        }

        /// <summary>
        /// Logger
        /// </summary>
        protected readonly ILogger _logger;
    }
}
