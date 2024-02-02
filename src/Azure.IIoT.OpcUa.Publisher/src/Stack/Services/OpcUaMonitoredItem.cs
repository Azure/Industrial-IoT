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
    using Opc.Ua.Extensions;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Threading;
    using System.Threading.Tasks;
    using Timer = System.Timers.Timer;

    /// <summary>
    /// Update display name
    /// </summary>
    /// <param name="displayName"></param>
    public delegate void UpdateString(string displayName);

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
    /// <param name="dataSetName"></param>
    /// <param name="diagnosticsOnly"></param>
    public delegate void Callback(MessageType messageType,
        IEnumerable<MonitoredItemNotificationModel> notifications,
        ISession? session = null, string? dataSetName = null,
        bool diagnosticsOnly = false);

    /// <summary>
    /// Monitored item
    /// </summary>
    internal abstract class OpcUaMonitoredItem : MonitoredItem, IDisposable
    {
        /// <summary>
        /// Assigned monitored item id on server
        /// </summary>
        public uint? RemoteId => Created ? Status.Id : null;

        /// <summary>
        /// The item is valid once added to the subscription. Contract:
        /// The item will be invalid until the subscription calls
        /// <see cref="AddTo(Subscription, IOpcUaSession, out bool)"/>
        /// to add it to the subscription. After removal the item
        /// is still Valid, but not Created. The item is
        /// again invalid after <see cref="IDisposable.Dispose"/> is
        /// called.
        /// </summary>
        public bool Valid { get; protected internal set; }

        /// <summary>
        /// Data set name
        /// </summary>
        public virtual string? DataSetName { get; }

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
        /// Get the display name for the node. This is called after
        /// the node is resolved and registered as applicable.
        /// </summary>
        public virtual (string NodeId, UpdateString Update)? GetDisplayName
            => null;

        /// <summary>
        /// Resolve relative path first. If this returns null
        /// the relative path either does not exist or we let
        /// subscription take care of resolving the path.
        /// </summary>
        public virtual (string NodeId, string[] Path, UpdateNodeId Update)? Resolve
            => null;

        /// <summary>
        /// Effective node id
        /// </summary>
        protected string NodeId { get; set; }

        /// <summary>
        /// Last saved value
        /// </summary>
        public IEncodeable? LastReceivedValue { get; private set; }

        /// <summary>
        /// Create item
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="nodeId"></param>
        protected OpcUaMonitoredItem(ILogger logger, string nodeId)
        {
            NodeId = nodeId;
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
            NodeId = item.NodeId;
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

        /// <summary>
        /// Create items
        /// </summary>
        /// <param name="items"></param>
        /// <param name="factory"></param>
        /// <param name="client"></param>
        /// <returns></returns>
        public static IEnumerable<OpcUaMonitoredItem> Create(
            IEnumerable<BaseMonitoredItemModel> items, ILoggerFactory factory,
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
                            yield return new DataItemWithCyclicRead(client,
                                dmi, factory.CreateLogger<DataItemWithCyclicRead>());
                        }
                        else if (dmi.HeartbeatInterval != null)
                        {
                            yield return new DataItemWithHeartbeat(dmi,
                                factory.CreateLogger<DataItemWithHeartbeat>());
                        }
                        else
                        {
                            yield return new DataItem(dmi,
                                factory.CreateLogger<DataItem>());
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
                            yield return new EventItem(emi,
                                factory.CreateLogger<EventItem>());
                        }
                        break;
                    case MonitoredAddressSpaceModel mam:
                        if (client != null)
                        {
                            yield return new ModelChangeEventItem(mam, client,
                                factory.CreateLogger<ModelChangeEventItem>());
                        }
                        break;
                    case ExtensionFieldModel efm:
                        yield return new FieldItem(efm,
                            factory.CreateLogger<FieldItem>());
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
        /// Try and get metadata for the item
        /// </summary>
        /// <param name="session"></param>
        /// <param name="typeSystem"></param>
        /// <param name="fields"></param>
        /// <param name="dataTypes"></param>
        /// <param name="ct"></param>
        public abstract ValueTask GetMetaDataAsync(IOpcUaSession session,
            ComplexTypeSystem? typeSystem, FieldMetaDataCollection fields,
            NodeIdDictionary<DataTypeDescription> dataTypes, CancellationToken ct);

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
        /// <param name="metadataChanged"></param>
        /// <returns></returns>
        public virtual bool AddTo(Subscription subscription, IOpcUaSession session,
            out bool metadataChanged)
        {
            if (Valid)
            {
                subscription.AddItem(this);
                _logger.LogDebug(
                    "Added monitored item {Item} to subscription #{SubscriptionId}.",
                    this, subscription.Id);
                metadataChanged = true;
                return true;
            }
            metadataChanged = false;
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
        /// <param name="metadataChanged"></param>
        /// <returns></returns>
        public abstract bool MergeWith(OpcUaMonitoredItem item,
            IOpcUaSession session, out bool metadataChanged);

        /// <summary>
        /// Finalize merge
        /// </summary>
        public virtual Func<IOpcUaSession, CancellationToken, Task>? FinalizeMergeWith { get; }

        /// <summary>
        /// Remove from subscription
        /// </summary>
        /// <param name="subscription"></param>
        /// <param name="metadataChanged"></param>
        /// <returns></returns>
        public virtual bool RemoveFrom(Subscription subscription,
            out bool metadataChanged)
        {
            if (AttachedToSubscription)
            {
                subscription.RemoveItem(this);
                _logger.LogDebug(
                    "Removed monitored item {Item} from subscription #{SubscriptionId}.",
                    this, subscription.Id);
                metadataChanged = true;
                return true;
            }
            metadataChanged = false;
            return false;
        }

        /// <summary>
        /// Finalize remove from
        /// </summary>
        public virtual Func<CancellationToken, Task>? FinalizeRemoveFrom { get; }

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
        /// <param name="metadataChanged"></param>
        /// <returns></returns>
        protected bool MergeWith<T>(T template, T desired, out T updated,
            out bool metadataChanged) where T : BaseMonitoredItemModel
        {
            metadataChanged = false;
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

            if (updated.FetchDataSetFieldName != desired.FetchDataSetFieldName)
            {
                updated = updated with
                {
                    FetchDataSetFieldName = desired.FetchDataSetFieldName,
                    DataSetFieldName = desired.FetchDataSetFieldName == true ?
                        null : updated.DataSetFieldName
                };
                // Not a change yet, will be done as display name fetching or below
                // itemChange = true;
            }

            if (updated.FetchDataSetFieldName != true &&
                updated.DisplayName != desired.DisplayName)
            {
                updated = updated with { DataSetFieldName = desired.DataSetFieldName };
                DisplayName = updated.DisplayName;
                metadataChanged = true;
                itemChange = true;
            }
            return itemChange;
        }

        /// <summary>
        /// Add veriable field metadata
        /// </summary>
        /// <param name="fields"></param>
        /// <param name="dataTypes"></param>
        /// <param name="session"></param>
        /// <param name="typeSystem"></param>
        /// <param name="variable"></param>
        /// <param name="fieldName"></param>
        /// <param name="dataSetClassFieldId"></param>
        /// <param name="ct"></param>
        protected async ValueTask AddVariableFieldAsync(FieldMetaDataCollection fields,
            NodeIdDictionary<DataTypeDescription> dataTypes, IOpcUaSession session,
            ComplexTypeSystem? typeSystem, VariableNode variable,
            string fieldName, Uuid dataSetClassFieldId, CancellationToken ct)
        {
            byte builtInType = 0;
            try
            {
                builtInType = (byte)await TypeInfo.GetBuiltInTypeAsync(variable.DataType,
                    session.TypeTree, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogInformation("{Item}: Failed to get built in type for type {DataType}" +
                    " with message: {Message}", this, variable.DataType, ex.Message);
            }
            fields.Add(new FieldMetaData
            {
                Name = fieldName,
                DataSetFieldId = dataSetClassFieldId,
                FieldFlags = 0, // Set to 1 << 1 for PromotedField fields.
                DataType = variable.DataType,
                ArrayDimensions = variable.ArrayDimensions?.Count > 0
                    ? variable.ArrayDimensions : null,
                Description = variable.Description,
                ValueRank = variable.ValueRank,
                MaxStringLength = 0,
                // If the Property is EngineeringUnits, the unit of the Field Value
                // shall match the unit of the FieldMetaData.
                Properties = null, // TODO: Add engineering units etc. to properties
                BuiltInType = builtInType
            });
            await AddDataTypesAsync(dataTypes, variable.DataType, session, typeSystem,
                ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Add data types to the metadata
        /// </summary>
        /// <param name="dataTypes"></param>
        /// <param name="dataTypeId"></param>
        /// <param name="session"></param>
        /// <param name="typeSystem"></param>
        /// <param name="ct"></param>
        protected async ValueTask AddDataTypesAsync(NodeIdDictionary<DataTypeDescription> dataTypes,
            NodeId dataTypeId, IOpcUaSession session, ComplexTypeSystem? typeSystem,
            CancellationToken ct)
        {
            if (IsBuiltInType(dataTypeId))
            {
                return;
            }

            var baseType = dataTypeId;
            while (!Opc.Ua.NodeId.IsNull(baseType))
            {
                try
                {
                    var dataType = await session.NodeCache.FetchNodeAsync(baseType, ct).ConfigureAwait(false);
                    if (dataType == null)
                    {
                        _logger.LogWarning("{Item}: Failed to find node for data type {BaseType}!",
                            this, baseType);
                        break;
                    }

                    dataTypeId = dataType.NodeId;
                    Debug.Assert(!Opc.Ua.NodeId.IsNull(dataTypeId));
                    if (IsBuiltInType(dataTypeId))
                    {
                        // Do not add builtin types
                        break;
                    }

                    var builtInType = await TypeInfo.GetBuiltInTypeAsync(dataTypeId, session.TypeTree,
                        ct).ConfigureAwait(false);
                    baseType = await session.TypeTree.FindSuperTypeAsync(dataTypeId, ct).ConfigureAwait(false);

                    switch (builtInType)
                    {
                        case BuiltInType.Enumeration:
                        case BuiltInType.ExtensionObject:
                            var types = typeSystem?.GetDataTypeDefinitionsForDataType(
                                dataType.NodeId);
                            if (types == null || types.Count == 0)
                            {
                                dataTypes.AddOrUpdate(dataType.NodeId, GetDefault(dataType, builtInType));
                                break;
                            }
                            foreach (var type in types)
                            {
                                if (!dataTypes.ContainsKey(type.Key))
                                {
                                    var description = type.Value switch
                                    {
                                        StructureDefinition s =>
                                            new StructureDescription
                                            {
                                                DataTypeId = type.Key,
                                                Name = dataType.BrowseName,
                                                StructureDefinition = s
                                            },
                                        EnumDefinition e =>
                                            new EnumDescription
                                            {
                                                DataTypeId = type.Key,
                                                Name = dataType.BrowseName,
                                                EnumDefinition = e
                                            },
                                        _ => GetDefault(dataType, builtInType),
                                    };
                                    dataTypes.AddOrUpdate(type.Key, description);
                                }
                            }
                            break;
                        default:
                            dataTypes.AddOrUpdate(dataTypeId, new SimpleTypeDescription
                            {
                                DataTypeId = dataTypeId,
                                Name = dataType.BrowseName,
                                BaseDataType = baseType,
                                BuiltInType = (byte)builtInType
                            });
                            break;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogInformation("{Item}: Failed to get meta data for type {DataType}" +
                        " (base: {BaseType}) with message: {Message}", this, dataTypeId,
                        baseType, ex.Message);
                    break;
                }
            }

            static bool IsBuiltInType(NodeId dataTypeId)
            {
                if (dataTypeId.NamespaceIndex == 0 && dataTypeId.IdType == IdType.Numeric)
                {
                    var id = (BuiltInType)(int)(uint)dataTypeId.Identifier;
                    if (id >= BuiltInType.Null && id <= BuiltInType.Enumeration)
                    {
                        return true;
                    }
                }
                return false;
            }
            static DataTypeDescription GetDefault(Node dataType, BuiltInType builtInType)
            {
                return builtInType == BuiltInType.Enumeration
                    ? new EnumDescription
                    {
                        DataTypeId = dataType.NodeId,
                        Name = dataType.BrowseName
                    } : new StructureDescription
                    {
                        DataTypeId = dataType.NodeId,
                        Name = dataType.BrowseName
                    };
            }
        }

        /// <summary>
        /// Extension Field item
        /// </summary>
        [DataContract(Namespace = Namespaces.OpcUaXsd)]
        [KnownType(typeof(DataChangeFilter))]
        [KnownType(typeof(EventFilter))]
        [KnownType(typeof(AggregateFilter))]
        internal class FieldItem : OpcUaMonitoredItem
        {
            /// <summary>
            /// Item as extension field
            /// </summary>
            public ExtensionFieldModel Template { get; protected internal set; }

            /// <summary>
            /// Create wrapper
            /// </summary>
            /// <param name="template"></param>
            /// <param name="logger"></param>
            public FieldItem(ExtensionFieldModel template,
                ILogger<FieldItem> logger) : base(logger, template.StartNodeId)
            {
                Template = template;
            }

            /// <summary>
            /// Copy constructor
            /// </summary>
            /// <param name="item"></param>
            /// <param name="copyEventHandlers"></param>
            /// <param name="copyClientHandle"></param>
            protected FieldItem(FieldItem item, bool copyEventHandlers,
                bool copyClientHandle)
                : base(item, copyEventHandlers, copyClientHandle)
            {
                Template = item.Template;
                _fieldId = item._fieldId;
                _value = item._value;
            }

            /// <inheritdoc/>
            public override MonitoredItem CloneMonitoredItem(
                bool copyEventHandlers, bool copyClientHandle)
            {
                return new FieldItem(this, copyEventHandlers, copyClientHandle);
            }

            /// <inheritdoc/>
            public override bool Equals(object? obj)
            {
                if (obj is not FieldItem fieldItem)
                {
                    return false;
                }
                if ((Template.DataSetFieldName ?? string.Empty) !=
                    (fieldItem.Template.DataSetFieldName ?? string.Empty))
                {
                    return false;
                }
                if (Template.Value != fieldItem.Template.Value)
                {
                    return false;
                }
                return true;
            }

            /// <inheritdoc/>
            public override int GetHashCode()
            {
                var hashCode = 81523234;
                hashCode = (hashCode * -1521134295) +
                    EqualityComparer<string>.Default.GetHashCode(
                        Template.DataSetFieldName ?? string.Empty);
                hashCode = (hashCode * -1521134295) +
                    Template.Value.GetHashCode();
                return hashCode;
            }

            /// <inheritdoc/>
            public override string ToString()
            {
                return $"Field '{Template.DataSetFieldName}' with value {Template.Value}.";
            }

            /// <inheritdoc/>
            public override ValueTask GetMetaDataAsync(IOpcUaSession session,
                ComplexTypeSystem? typeSystem, FieldMetaDataCollection fields,
                NodeIdDictionary<DataTypeDescription> dataTypes, CancellationToken ct)
            {
                return AddVariableFieldAsync(fields, dataTypes, session, typeSystem, new VariableNode
                {
                    DataType = (int)BuiltInType.Variant
                }, Template.DisplayName, (Uuid)_fieldId, ct);
            }

            /// <inheritdoc/>
            public override bool AddTo(Subscription subscription,
                IOpcUaSession session, out bool metadataChanged)
            {
                metadataChanged = true;
                _value = new DataValue(session.Codec.Decode(Template.Value, BuiltInType.Variant));
                Valid = true;
                return true;
            }

            /// <inheritdoc/>
            public override bool MergeWith(OpcUaMonitoredItem item, IOpcUaSession session,
                out bool metadataChanged)
            {
                metadataChanged = false;
                return false;
            }

            /// <inheritdoc/>
            public override bool RemoveFrom(Subscription subscription, out bool metadataChanged)
            {
                metadataChanged = true;
                _value = new DataValue();
                Valid = false;
                return true;
            }

            /// <inheritdoc/>
            public override bool TryCompleteChanges(Subscription subscription,
                ref bool applyChanges,
                Callback cb)
            {
                return true;
            }

            /// <inheritdoc/>
            public override bool TryGetLastMonitoredItemNotifications(uint sequenceNumber,
                IList<MonitoredItemNotificationModel> notifications)
            {
                if (!Valid)
                {
                    return false;
                }
                notifications.Add(ToMonitoredItemNotification(sequenceNumber));
                return true;
            }

            /// <inheritdoc/>
            public override bool TryGetMonitoredItemNotifications(uint sequenceNumber,
                DateTime timestamp, IEncodeable evt, IList<MonitoredItemNotificationModel> notifications)
            {
                Debug.Fail("Unexpected notification on extension field");
                return false;
            }

            /// <inheritdoc/>
            protected override bool TryGetErrorMonitoredItemNotifications(
                uint sequenceNumber, StatusCode statusCode,
                IList<MonitoredItemNotificationModel> notifications)
            {
                Debug.Fail("Unexpected notification on extension field");
                return false;
            }

            /// <summary>
            /// Convert to monitored item notifications
            /// </summary>
            /// <param name="sequenceNumber"></param>
            /// <returns></returns>
            protected MonitoredItemNotificationModel ToMonitoredItemNotification(uint sequenceNumber)
            {
                Debug.Assert(Valid);
                Debug.Assert(Template != null);

                return new MonitoredItemNotificationModel
                {
                    Id = Template.Id,
                    DataSetFieldName = Template.DisplayName,
                    DataSetName = Template.DisplayName,
                    NodeId = NodeId,
                    Value = _value,
                    SequenceNumber = sequenceNumber
                };
            }

            private DataValue _value = new();
            private readonly Guid _fieldId = Guid.NewGuid();
        }

        /// <summary>
        /// Data item
        /// </summary>
        [DataContract(Namespace = Namespaces.OpcUaXsd)]
        [KnownType(typeof(DataChangeFilter))]
        [KnownType(typeof(EventFilter))]
        [KnownType(typeof(AggregateFilter))]
        internal class DataItem : OpcUaMonitoredItem
        {
            /// <inheritdoc/>
            public override (string NodeId, string[] Path, UpdateNodeId Update)? Resolve
                => Template.RelativePath != null &&
                    (TheResolvedNodeId == Template.StartNodeId || string.IsNullOrEmpty(TheResolvedNodeId)) ?
                    (Template.StartNodeId, Template.RelativePath.ToArray(),
                        (v, context) => TheResolvedNodeId = NodeId
                            = v.AsString(context, Template.NamespaceFormat) ?? string.Empty) : null;

            /// <inheritdoc/>
            public override (string NodeId, UpdateNodeId Update)? Register
                => Template.RegisterRead && !string.IsNullOrEmpty(TheResolvedNodeId) ?
                    (TheResolvedNodeId, (v, context) => NodeId
                            = v.AsString(context, Template.NamespaceFormat) ?? string.Empty) : null;

            /// <inheritdoc/>
            public override (string NodeId, UpdateString Update)? GetDisplayName
                => Template.FetchDataSetFieldName == true && Template.DataSetFieldName != null &&
                    !string.IsNullOrEmpty(NodeId) ?
                    (NodeId, v => Template = Template with { DataSetFieldName = v }) : null;

            /// <summary>
            /// Monitored item as data
            /// </summary>
            public DataMonitoredItemModel Template { get; protected internal set; }

            /// <summary>
            /// Resolved node id
            /// </summary>
            protected string TheResolvedNodeId { get; private set; }

            /// <summary>
            /// Field identifier either configured or randomly assigned
            /// for data change items.
            /// </summary>
            public Guid DataSetClassFieldId
                => Template?.DataSetClassFieldId == Guid.Empty ?
                    _fieldId : Template?.DataSetClassFieldId ?? Guid.Empty;

            /// <summary>
            /// Create wrapper
            /// </summary>
            /// <param name="template"></param>
            /// <param name="logger"></param>
            public DataItem(DataMonitoredItemModel template,
                ILogger<DataItem> logger) : base(logger, template.StartNodeId)
            {
                Template = template;

                //
                // We also track the resolved node id so we distinguish it
                // from the registered and thus effective node id
                //
                TheResolvedNodeId = template.StartNodeId;
            }

            /// <summary>
            /// Copy constructor
            /// </summary>
            /// <param name="item"></param>
            /// <param name="copyEventHandlers"></param>
            /// <param name="copyClientHandle"></param>
            protected DataItem(DataItem item, bool copyEventHandlers,
                bool copyClientHandle)
                : base(item, copyEventHandlers, copyClientHandle)
            {
                TheResolvedNodeId = item.TheResolvedNodeId;
                Template = item.Template;
                _fieldId = item._fieldId;
                _skipDataChangeNotification = item._skipDataChangeNotification;
            }

            /// <inheritdoc/>
            public override MonitoredItem CloneMonitoredItem(
                bool copyEventHandlers, bool copyClientHandle)
            {
                return new DataItem(this, copyEventHandlers, copyClientHandle);
            }

            /// <inheritdoc/>
            public override bool Equals(object? obj)
            {
                if (obj is not DataItem dataItem)
                {
                    return false;
                }
                if ((Template.DataSetFieldId ?? string.Empty) !=
                    (dataItem.Template.DataSetFieldId ?? string.Empty))
                {
                    return false;
                }
                if (!Template.RelativePath.SequenceEqualsSafe(
                    dataItem.Template.RelativePath))
                {
                    return false;
                }
                if (Template.StartNodeId != dataItem.Template.StartNodeId)
                {
                    return false;
                }
                if (Template.RegisterRead != dataItem.Template.RegisterRead)
                {
                    return false;
                }
                if (Template.IndexRange != dataItem.Template.IndexRange)
                {
                    return false;
                }
                if (Template.AttributeId != dataItem.Template.AttributeId)
                {
                    return false;
                }
                return true;
            }

            /// <inheritdoc/>
            public override int GetHashCode()
            {
                var hashCode = 81523234;
                hashCode = (hashCode * -1521134295) +
                    EqualityComparer<string>.Default.GetHashCode(
                        Template.DataSetFieldId ?? string.Empty);
                hashCode = (hashCode * -1521134295) +
                    EqualityComparer<IReadOnlyList<string>>.Default.GetHashCode(
                        Template.RelativePath ?? Array.Empty<string>());
                hashCode = (hashCode * -1521134295) +
                    EqualityComparer<string>.Default.GetHashCode(Template.StartNodeId);
                hashCode = (hashCode * -1521134295) +
                    EqualityComparer<string>.Default.GetHashCode(Template.IndexRange ?? string.Empty);
                hashCode = (hashCode * -1521134295) +
                    EqualityComparer<bool>.Default.GetHashCode(Template.RegisterRead);
                hashCode = (hashCode * -1521134295) +
                    EqualityComparer<NodeAttribute>.Default.GetHashCode(
                        Template.AttributeId ?? NodeAttribute.NodeId);
                return hashCode;
            }

            /// <inheritdoc/>
            public override string ToString()
            {
                return $"Data Item '{Template.StartNodeId}' with server id {RemoteId} - " +
                    $"{(Status?.Created == true ? "" : "not ")}created";
            }

            /// <inheritdoc/>
            public override async ValueTask GetMetaDataAsync(IOpcUaSession session,
                ComplexTypeSystem? typeSystem, FieldMetaDataCollection fields,
                NodeIdDictionary<DataTypeDescription> dataTypes, CancellationToken ct)
            {
                var nodeId = NodeId.ToExpandedNodeId(session.MessageContext);
                if (Opc.Ua.NodeId.IsNull(nodeId))
                {
                    // Failed.
                    return;
                }
                try
                {
                    var node = await session.NodeCache.FetchNodeAsync(nodeId, ct).ConfigureAwait(false);
                    if (node is VariableNode variable)
                    {
                        await AddVariableFieldAsync(fields, dataTypes, session, typeSystem, variable,
                            Template.DisplayName, (Uuid)DataSetClassFieldId, ct).ConfigureAwait(false);
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogInformation("{Item}: Failed to get meta data for field {Field} " +
                        "with node {NodeId} with message {Message}.", this, Template.DisplayName,
                        nodeId, ex.Message);
                }
            }

            /// <inheritdoc/>
            public override bool AddTo(Subscription subscription, IOpcUaSession session,
                out bool metadataChanged)
            {
                var nodeId = NodeId.ToNodeId(session.MessageContext);
                if (Opc.Ua.NodeId.IsNull(nodeId))
                {
                    metadataChanged = false;
                    return false;
                }

                DisplayName = Template.DisplayName;
                AttributeId = (uint)(Template.AttributeId ??
                    (NodeAttribute)Attributes.Value);
                IndexRange = Template.IndexRange;
                StartNodeId = nodeId;
                MonitoringMode = Template.MonitoringMode.ToStackType()
                    ?? Opc.Ua.MonitoringMode.Reporting;
                QueueSize = Template.QueueSize;
                SamplingInterval = (int)Template.SamplingInterval.
                    GetValueOrDefault(TimeSpan.FromSeconds(1)).TotalMilliseconds;
                Filter = Template.DataChangeFilter.ToStackModel() ??
                    (MonitoringFilter?)Template.AggregateFilter.ToStackModel(
                        session.MessageContext);
                DiscardOldest = !(Template.DiscardNew ?? false);
                Valid = true;

                if (!TrySetSkipFirst(Template.SkipFirst))
                {
                    Debug.Fail("Unexpected: Failed to set skip first setting.");
                }
                return base.AddTo(subscription, session, out metadataChanged);
            }

            /// <inheritdoc/>
            public override bool TryGetMonitoredItemNotifications(uint sequenceNumber,
                DateTime timestamp, IEncodeable evt, IList<MonitoredItemNotificationModel> notifications)
            {
                if (evt is MonitoredItemNotification min &&
                    base.TryGetMonitoredItemNotifications(sequenceNumber, timestamp, evt, notifications))
                {
                    return ProcessMonitoredItemNotification(sequenceNumber, timestamp, min, notifications);
                }
                return false;
            }

            /// <inheritdoc/>
            public override bool MergeWith(OpcUaMonitoredItem item, IOpcUaSession session,
                 out bool metadataChanged)
            {
                metadataChanged = false;
                if (item is not DataItem model || !Valid)
                {
                    return false;
                }

                var itemChange = MergeWith(Template, model.Template, out var updated,
                    out metadataChanged);
                if (itemChange)
                {
                    Template = updated;
                }

                if ((Template.SamplingInterval ?? TimeSpan.FromSeconds(1)) !=
                    (Template.SamplingInterval ?? TimeSpan.FromSeconds(1)))
                {
                    _logger.LogDebug("{Item}: Changing sampling interval from {Old} to {New}",
                        this, Template.SamplingInterval.GetValueOrDefault(
                            TimeSpan.FromSeconds(1)).TotalMilliseconds,
                        model.Template.SamplingInterval.GetValueOrDefault(
                            TimeSpan.FromSeconds(1)).TotalMilliseconds);
                    Template = Template with { SamplingInterval = model.Template.SamplingInterval };
                    SamplingInterval =
                        (int)Template.SamplingInterval.GetValueOrDefault(
                            TimeSpan.FromSeconds(1)).TotalMilliseconds;
                    itemChange = true;
                }

                if (Template.DataSetClassFieldId != model.Template.DataSetClassFieldId)
                {
                    var previous = DataSetClassFieldId;
                    Template = Template with { DataSetClassFieldId = model.Template.DataSetClassFieldId };
                    _logger.LogDebug("{Item}: Changing dataset class field id from {Old} to {New}",
                        this, previous, DataSetClassFieldId);
                    metadataChanged = true;
                }

                // Update change filter
                if (!model.Template.DataChangeFilter.IsSameAs(Template.DataChangeFilter))
                {
                    Template = Template with { DataChangeFilter = model.Template.DataChangeFilter };
                    _logger.LogDebug("{Item}: Changing data change filter.", this);
                    Filter = Template.DataChangeFilter.ToStackModel();
                    itemChange = true;
                }

                // Update AggregateFilter
                else if (!model.Template.AggregateFilter.IsSameAs(Template.AggregateFilter))
                {
                    Template = Template with { AggregateFilter = model.Template.AggregateFilter };
                    _logger.LogDebug("{Item}: Changing aggregate change filter.", this);
                    Filter = Template.AggregateFilter.ToStackModel(session.MessageContext);
                    itemChange = true;
                }
                if (model.Template.SkipFirst != Template.SkipFirst)
                {
                    Template = Template with { SkipFirst = model.Template.SkipFirst };

                    if (model.TrySetSkipFirst(model.Template.SkipFirst))
                    {
                        _logger.LogDebug("{Item}: Setting skip first setting to {New}", this,
                            model.Template.SkipFirst);
                    }
                    else
                    {
                        _logger.LogInformation("{Item}: Tried to set SkipFirst but it was set" +
                            "previously or first value was already processed.", this);
                    }
                    // No change, just updated internal state
                }
                return itemChange;
            }

            /// <inheritdoc/>
            public override bool TryGetLastMonitoredItemNotifications(uint sequenceNumber,
                IList<MonitoredItemNotificationModel> notifications)
            {
                SkipMonitoredItemNotification(); // Key frames should always be sent
                return base.TryGetLastMonitoredItemNotifications(sequenceNumber,
                    notifications);
            }

            /// <inheritdoc/>
            protected override bool TryGetErrorMonitoredItemNotifications(
                uint sequenceNumber, StatusCode statusCode,
                IList<MonitoredItemNotificationModel> notifications)
            {
                notifications.Add(ToMonitoredItemNotification(sequenceNumber,
                    new DataValue(statusCode)));
                return true;
            }

            /// <summary>
            /// Process monitored item notification
            /// </summary>
            /// <param name="sequenceNumber"></param>
            /// <param name="timestamp"></param>
            /// <param name="monitoredItemNotification"></param>
            /// <param name="notifications"></param>
            /// <returns></returns>
            protected virtual bool ProcessMonitoredItemNotification(uint sequenceNumber,
                DateTime timestamp, MonitoredItemNotification monitoredItemNotification,
                IList<MonitoredItemNotificationModel> notifications)
            {
                if (!SkipMonitoredItemNotification())
                {
                    notifications.Add(ToMonitoredItemNotification(sequenceNumber,
                        monitoredItemNotification.Value));
                    return true;
                }
                return false;
            }

            /// <summary>
            /// Convert to monitored item notifications
            /// </summary>
            /// <param name="sequenceNumber"></param>
            /// <param name="dataValue"></param>
            /// <returns></returns>
            protected MonitoredItemNotificationModel ToMonitoredItemNotification(
                uint sequenceNumber, DataValue dataValue)
            {
                Debug.Assert(Valid);
                Debug.Assert(Template != null);

                return new MonitoredItemNotificationModel
                {
                    Id = Template.DataSetFieldId ?? string.Empty,
                    DataSetFieldName = Template.DisplayName,
                    DataSetName = Template.DisplayName,
                    NodeId = NodeId,
                    Value = dataValue,
                    Overflow = dataValue.StatusCode.Overflow ? 1 : 0,
                    SequenceNumber = sequenceNumber
                };
            }

            /// <summary>
            /// Whether to skip monitored item notification
            /// </summary>
            /// <returns></returns>
            public bool SkipMonitoredItemNotification()
            {
                // This will update that first value has been processed.
                var last = Interlocked.Exchange(ref _skipDataChangeNotification,
                    (int)SkipSetting.DontSkip);
                return last == (int)SkipSetting.Skip;
            }

            /// <summary>
            /// Try set skip first setting. We allow updating while first value
            /// is not yet processed, which is the case if skip setting is unconfigured.
            /// </summary>
            /// <param name="skipFirst"></param>
            /// <returns></returns>
            public bool TrySetSkipFirst(bool skipFirst)
            {
                if (skipFirst)
                {
                    // We only allow updating first skip setting while unconfigured
                    return Interlocked.CompareExchange(ref _skipDataChangeNotification,
                        (int)SkipSetting.Skip,
                        (int)SkipSetting.Unconfigured) == (int)SkipSetting.Unconfigured;
                }
                // Unset skip setting if it was configured but first message was not yet processed
                Interlocked.CompareExchange(ref _skipDataChangeNotification,
                    (int)SkipSetting.Unconfigured, (int)SkipSetting.Skip);

                return true;
            }

            enum SkipSetting
            {
                /// <summary> Default </summary>
                DontSkip,
                /// <summary> Skip first value </summary>
                Skip,
                /// <summary> Configuration not applied yet </summary>
                Unconfigured,
            }

            private volatile int _skipDataChangeNotification = (int)SkipSetting.Unconfigured;
            private readonly Guid _fieldId = Guid.NewGuid();
        }

        /// <summary>
        /// <para>
        /// Data monitored item with heartbeat handling. If no data is sent
        /// within the heartbeat interval the previously received data is
        /// sent instead. Heartbeat is not a cyclic read.
        /// </para>
        /// <para>
        /// Heartbeat is now implemented as a watchdog of the notification
        /// which is reset every time a new value arrives. This is different
        /// from past implementation where the keep alive was driving heartbeats.
        /// This caused issues and was ineffective due to the use of the value
        /// cache on the subscription object caching _all_ values received.
        /// </para>
        /// </summary>
        [DataContract(Namespace = Namespaces.OpcUaXsd)]
        [KnownType(typeof(DataChangeFilter))]
        [KnownType(typeof(EventFilter))]
        [KnownType(typeof(AggregateFilter))]
        internal sealed class DataItemWithHeartbeat : DataItem
        {
            /// <summary>
            /// Create data item with heartbeat
            /// </summary>
            /// <param name="dataTemplate"></param>
            /// <param name="logger"></param>
            public DataItemWithHeartbeat(DataMonitoredItemModel dataTemplate,
                ILogger<DataItem> logger) : base(dataTemplate, logger)
            {
                _heartbeatInterval = dataTemplate.HeartbeatInterval
                    ?? dataTemplate.SamplingInterval ?? TimeSpan.FromSeconds(1);
                _timerInterval = Timeout.InfiniteTimeSpan;
                _heartbeatBehavior = dataTemplate.HeartbeatBehavior
                    ?? HeartbeatBehavior.WatchdogLKV;
                _heartbeatTimer = new Timer();
                _heartbeatTimer.Elapsed += SendHeartbeatNotifications;
                _heartbeatTimer.AutoReset = true;
                _heartbeatTimer.Enabled = true;
            }

            /// <summary>
            /// Copy constructor
            /// </summary>
            /// <param name="item"></param>
            /// <param name="copyEventHandlers"></param>
            /// <param name="copyClientHandle"></param>
            private DataItemWithHeartbeat(DataItemWithHeartbeat item, bool copyEventHandlers,
                bool copyClientHandle)
                : base(item, copyEventHandlers, copyClientHandle)
            {
                _heartbeatInterval = item._heartbeatInterval;
                _timerInterval = item._timerInterval;
                _heartbeatBehavior = item._heartbeatBehavior;
                _lastValueReceived = item._lastValueReceived;
                _callback = item._callback;
                _heartbeatTimer = item.CloneTimer();
                if (_heartbeatTimer != null)
                {
                    _heartbeatTimer.Elapsed += SendHeartbeatNotifications;
                }
            }

            /// <inheritdoc/>
            public override MonitoredItem CloneMonitoredItem(
                bool copyEventHandlers, bool copyClientHandle)
            {
                return new DataItemWithHeartbeat(this, copyEventHandlers, copyClientHandle);
            }

            /// <inheritdoc/>
            public override bool Equals(object? obj)
            {
                if (obj is not DataItemWithHeartbeat)
                {
                    return false;
                }
                return base.Equals(obj);
            }

            /// <inheritdoc/>
            public override int GetHashCode()
            {
                return 334455667 + base.GetHashCode();
            }

            /// <inheritdoc/>
            public override string ToString()
            {
                return $"Data Item '{Template.StartNodeId}' " +
                    $"(with {Template.HeartbeatBehavior ?? HeartbeatBehavior.WatchdogLKV} Heartbeat) " +
                    $"with server id {RemoteId} - {(Status?.Created == true ? "" :
                        "not ")}created";
            }

            /// <inheritdoc/>
            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    var timer = CloneTimer();
                    timer?.Dispose();
                }
                base.Dispose(disposing);
            }

            /// <inheritdoc/>
            protected override bool ProcessMonitoredItemNotification(uint sequenceNumber,
                DateTime timestamp, MonitoredItemNotification monitoredItemNotification,
                IList<MonitoredItemNotificationModel> notifications)
            {
                Debug.Assert(Valid);
                var result = base.ProcessMonitoredItemNotification(sequenceNumber, timestamp,
                    monitoredItemNotification, notifications);

                if (_heartbeatTimer != null && (_heartbeatBehavior & HeartbeatBehavior.PeriodicLKV) == 0)
                {
                    _heartbeatTimer.Interval = _timerInterval.TotalMilliseconds;
                    _heartbeatTimer.Enabled = true;
                }
                return result;
            }

            /// <inheritdoc/>
            public override bool MergeWith(OpcUaMonitoredItem item, IOpcUaSession session,
                 out bool metadataChanged)
            {
                metadataChanged = false;
                if (item is not DataItemWithHeartbeat model || !Valid)
                {
                    return false;
                }

                var itemChange = false;

                if (_heartbeatInterval != model._heartbeatInterval)
                {
                    _logger.LogDebug("{Item}: Changing heartbeat from {Old} to {New}",
                        this, _heartbeatInterval, model._heartbeatInterval);

                    _heartbeatInterval = model._heartbeatInterval;
                    itemChange = true; // TODO: Not really a change in the item
                }

                if (_heartbeatBehavior != model._heartbeatBehavior)
                {
                    _logger.LogDebug("{Item}: Changing heartbeat behavior from {Old} to {New}",
                        this, _heartbeatBehavior, model._heartbeatBehavior);

                    _heartbeatBehavior = model._heartbeatBehavior;
                    itemChange = true; // TODO: Not really a change in the item
                }

                itemChange |= base.MergeWith(model, session, out metadataChanged);
                return itemChange;
            }

            /// <inheritdoc/>
            public override bool TryCompleteChanges(Subscription subscription,
                ref bool applyChanges,
                Callback cb)
            {
                if (_heartbeatTimer == null)
                {
                    return false;
                }
                var result = base.TryCompleteChanges(subscription, ref applyChanges, cb);
                {
                    var lkg = (_heartbeatBehavior & HeartbeatBehavior.WatchdogLKG)
                            == HeartbeatBehavior.WatchdogLKG;
                    if (!AttachedToSubscription || (!result && lkg))
                    {
                        _callback = null;
                        // Stop heartbeat
                        _heartbeatTimer.Enabled = false;
                        _timerInterval = Timeout.InfiniteTimeSpan;
                    }
                    else
                    {
                        Debug.Assert(AttachedToSubscription);
                        _callback = cb;
                        if (_timerInterval != _heartbeatInterval)
                        {
                            // Start heartbeat after completion
                            _heartbeatTimer.Interval = _heartbeatInterval.TotalMilliseconds;
                            _timerInterval = _heartbeatInterval;
                        }
                        _heartbeatTimer.Enabled = true;
                    }
                }
                return result;
            }

            /// <inheritdoc/>
            public override bool TryGetMonitoredItemNotifications(uint sequenceNumber, DateTime timestamp,
                IEncodeable evt, IList<MonitoredItemNotificationModel> notifications)
            {
                _lastValueReceived = DateTime.UtcNow;
                if (_heartbeatTimer != null && (_heartbeatBehavior & HeartbeatBehavior.PeriodicLKV) == 0)
                {
                    _heartbeatTimer.Enabled = false;
                }
                return base.TryGetMonitoredItemNotifications(sequenceNumber, timestamp, evt, notifications);
            }

            /// <summary>
            /// TODO: What is a Good value? Right now we say that it must either be full good or
            /// have a value and not a bad status code (to cover Good_, and Uncertain_ as well)
            /// </summary>
            /// <param name="value"></param>
            /// <returns></returns>
            private static bool IsGoodDataValue(DataValue? value)
            {
                if (value == null)
                {
                    return false;
                }
                return value.StatusCode == StatusCodes.Good ||
                    (value.WrappedValue != Variant.Null && !StatusCode.IsBad(value.StatusCode));
            }

            /// <summary>
            /// Send heartbeat
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
            private void SendHeartbeatNotifications(object? sender, System.Timers.ElapsedEventArgs e)
            {
                var callback = _callback;
                if (callback == null || !Valid)
                {
                    return;
                }

                var lastNotification = LastReceivedValue as MonitoredItemNotification;
                if ((_heartbeatBehavior & HeartbeatBehavior.WatchdogLKG)
                        == HeartbeatBehavior.WatchdogLKG &&
                        !IsGoodDataValue(lastNotification?.Value))
                {
                    // Currently no last known good value (LKG) to send
                    return;
                }

                var lastValue = lastNotification?.Value;
                if (lastValue == null && Status?.Error?.StatusCode != null)
                {
                    lastValue = new DataValue(Status.Error.StatusCode);
                }

                if (lastValue == null)
                {
                    // Currently no last known value (LKV) to send
                    return;
                }
                if ((_heartbeatBehavior & HeartbeatBehavior.WatchdogLKVWithUpdatedTimestamps)
                        == HeartbeatBehavior.WatchdogLKVWithUpdatedTimestamps)
                {
                    // Adjust to the diff between now and received if desired
                    // Should not be possible that last value received is null, nevertheless.
                    var diffTime = _lastValueReceived.HasValue ?
                        DateTime.UtcNow - _lastValueReceived.Value : TimeSpan.Zero;

                    lastValue = new DataValue(lastValue)
                    {
                        SourceTimestamp = lastValue.SourceTimestamp == DateTime.MinValue ?
                            DateTime.MinValue : lastValue.SourceTimestamp.Add(diffTime),
                        ServerTimestamp = lastValue.ServerTimestamp == DateTime.MinValue ?
                            DateTime.MinValue : lastValue.ServerTimestamp.Add(diffTime)
                    };
                }

                // If last value is null create a error value.
                var heartbeat = new MonitoredItemNotificationModel
                {
                    Id = Template.Id,
                    DataSetFieldName = Template.DisplayName,
                    DataSetName = Template.DisplayName,
                    NodeId = NodeId,
                    Value = lastValue,
                    Flags = MonitoredItemSourceFlags.Heartbeat,
                    SequenceNumber = 0
                };
                callback(MessageType.DeltaFrame, heartbeat.YieldReturn(),
                    diagnosticsOnly: (_heartbeatBehavior & HeartbeatBehavior.WatchdogLKVDiagnosticsOnly)
                        == HeartbeatBehavior.WatchdogLKVDiagnosticsOnly);
            }

            /// <summary>
            /// Clone the timer
            /// </summary>
            /// <returns></returns>
            private Timer? CloneTimer()
            {
                var timer = _heartbeatTimer;
                _heartbeatTimer = null;
                if (timer != null)
                {
                    timer.Elapsed -= SendHeartbeatNotifications;
                }
                return timer;
            }

            private Timer? _heartbeatTimer;
            private TimeSpan _timerInterval;
            private HeartbeatBehavior _heartbeatBehavior;
            private TimeSpan _heartbeatInterval;
            private Callback? _callback;
            private DateTime? _lastValueReceived;
        }

        /// <summary>
        /// Cyclic read items are part of a subscription but disabled. They
        /// execute through the client sampler periodically (at the configured
        /// sampling rate).
        /// </summary>
        [DataContract(Namespace = Namespaces.OpcUaXsd)]
        [KnownType(typeof(DataChangeFilter))]
        [KnownType(typeof(EventFilter))]
        [KnownType(typeof(AggregateFilter))]
        internal sealed class DataItemWithCyclicRead : DataItem
        {
            /// <summary>
            /// Create cyclic read item
            /// </summary>
            /// <param name="client"></param>
            /// <param name="template"></param>
            /// <param name="logger"></param>
            public DataItemWithCyclicRead(IOpcUaClient client,
                DataMonitoredItemModel template, ILogger<DataItemWithCyclicRead> logger)
                : base(template with
                {
                    // Always ensure item is disabled
                    MonitoringMode = Publisher.Models.MonitoringMode.Disabled
                }, logger)
            {
                _client = client;

                LastReceivedValue = new MonitoredItemNotification
                {
                    Value = new DataValue(StatusCodes.GoodNoData)
                };
            }

            /// <summary>
            /// Copy constructor
            /// </summary>
            /// <param name="item"></param>
            /// <param name="copyEventHandlers"></param>
            /// <param name="copyClientHandle"></param>
            private DataItemWithCyclicRead(DataItemWithCyclicRead item, bool copyEventHandlers,
                bool copyClientHandle)
                : base(item, copyEventHandlers, copyClientHandle)
            {
                _client = item._client;
                _sampler = item.CloneSampler();
                _callback = item._callback;
                if (_sampler != null)
                {
                    _sampler.OnValueChange += OnSampledDataValueReceived;
                }
            }

            /// <inheritdoc/>
            public override MonitoredItem CloneMonitoredItem(
                bool copyEventHandlers, bool copyClientHandle)
            {
                return new DataItemWithCyclicRead(this, copyEventHandlers, copyClientHandle);
            }

            /// <inheritdoc/>
            protected override void Dispose(bool disposing)
            {
                // Cleanup
                var sampler = CloneSampler();
                if (sampler != null)
                {
                    sampler.CloseAsync().AsTask().GetAwaiter().GetResult();
                }
                base.Dispose(disposing);
            }

            /// <inheritdoc/>
            public override bool Equals(object? obj)
            {
                if (obj is not DataItemWithCyclicRead cyclicRead)
                {
                    return false;
                }
                if (_client != cyclicRead._client)
                {
                    return false;
                }
                if ((Template.SamplingInterval ?? TimeSpan.FromSeconds(1)) !=
                    (cyclicRead.Template.SamplingInterval ?? TimeSpan.FromSeconds(1)))
                {
                    return false;
                }
                return base.Equals(obj);
            }

            /// <inheritdoc/>
            public override int GetHashCode()
            {
                var hashCode = base.GetHashCode();
                hashCode = (hashCode * -1521134295) +
                    _client.GetHashCode();
                hashCode = (hashCode * -1521134295) +
                    EqualityComparer<TimeSpan>.Default.GetHashCode(
                        Template.SamplingInterval ?? TimeSpan.FromSeconds(1));
                return hashCode;
            }

            /// <inheritdoc/>
            public override string ToString()
            {
                return $"Cyclic read '{Template.StartNodeId}' every {Template.SamplingInterval
                    ?? TimeSpan.FromSeconds(1)}";
            }

            /// <inheritdoc/>
            public override bool MergeWith(OpcUaMonitoredItem item, IOpcUaSession session,
                out bool metadataChanged)
            {
                if (item is not DataItemWithCyclicRead)
                {
                    metadataChanged = false;
                    return false;
                }
                return base.MergeWith(item, session, out metadataChanged);
            }

            /// <inheritdoc/>
            public override Func<CancellationToken, Task>? FinalizeMonitoringModeChange => async ct =>
            {
                if (!AttachedToSubscription)
                {
                    // Disabling sampling
                    if (_sampler != null)
                    {
                        _sampler.OnValueChange -= OnSampledDataValueReceived;
                        await _sampler.CloseAsync().ConfigureAwait(false);
                        _sampler = null;

                        _logger.LogDebug("Item {Item} unregistered from sampler.", this);
                    }
                }
                else if (_sampler == null)
                {
                    Debug.Assert(MonitoringMode == Opc.Ua.MonitoringMode.Disabled);
                    _sampler = _client.Sample(TimeSpan.FromMilliseconds(SamplingInterval),
                        new ReadValueId
                        {
                            AttributeId = AttributeId,
                            IndexRange = IndexRange,
                            NodeId = ResolvedNodeId
                        },
                        Subscription.DisplayName);
                    _sampler.OnValueChange += OnSampledDataValueReceived;
                    _logger.LogDebug("Item {Item} successfully registered with sampler.",
                        this);
                }
            };

            /// <inheritdoc/>
            public override bool TryCompleteChanges(Subscription subscription,
                ref bool applyChanges, Callback cb)
            {
                // Dont call base implementation as it is not what we want.
                if (!Valid)
                {
                    return false;
                }
                _callback = !AttachedToSubscription ? null : cb;
                return true;
            }

            /// <inheritdoc/>
            public override bool TryGetLastMonitoredItemNotifications(uint sequenceNumber,
                IList<MonitoredItemNotificationModel> notifications)
            {
                // Dont call base implementation as it is not what we want.
                notifications.Add(new MonitoredItemNotificationModel
                {
                    Id = Template.Id,
                    DataSetName = Template.DisplayName,
                    DataSetFieldName = Template.DisplayName,
                    NodeId = Template.StartNodeId,
                    SequenceNumber = sequenceNumber,
                    Value = LastSampledValue,
                    Flags = MonitoredItemSourceFlags.CyclicRead
                });
                return true;
            }

            /// <inheritdoc/>
            public override bool TryGetMonitoredItemNotifications(uint sequenceNumber,
                DateTime timestamp, IEncodeable evt, IList<MonitoredItemNotificationModel> notifications)
            {
                Debug.Fail("Should never be called since item is disabled.");
                return false;
            }

            /// <summary>
            /// Called when data is received from the sampler
            /// </summary>
            /// <param name="o"></param>
            /// <param name="e"></param>
            private void OnSampledDataValueReceived(object? o, DataValueChange e)
            {
                var callback = _callback;
                if (callback == null)
                {
                    return;
                }

                LastSampledValue = e.Value;

                var notification = new MonitoredItemNotificationModel
                {
                    Id = Template.Id,
                    DataSetName = Template.DisplayName,
                    DataSetFieldName = Template.DisplayName,
                    NodeId = Template.StartNodeId,
                    SequenceNumber = e.SequenceNumber,
                    Flags = MonitoredItemSourceFlags.CyclicRead,
                    Overflow = e.Overflow,
                    Value = e.Value
                };
                callback(MessageType.DeltaFrame, notification.YieldReturn(),
                    o as ISession);
            }

            /// <summary>
            /// Last sampled value
            /// </summary>
            internal DataValue LastSampledValue
            {
                get
                {
                    Debug.Assert(LastReceivedValue is MonitoredItemNotification);
                    return ((MonitoredItemNotification)LastReceivedValue).Value
                        ?? new DataValue(StatusCodes.GoodNoData);
                }
                set
                {
                    Debug.Assert(LastReceivedValue is MonitoredItemNotification);
                    ((MonitoredItemNotification)LastReceivedValue).Value = value;
                }
            }

            /// <summary>
            /// Clone the sampler
            /// </summary>
            /// <returns></returns>
            private IOpcUaSampler? CloneSampler()
            {
                var sampler = _sampler;
                _sampler = null;
                if (sampler != null)
                {
                    sampler.OnValueChange -= OnSampledDataValueReceived;
                }
                return sampler;
            }

            private readonly IOpcUaClient _client;
            private Callback? _callback;
            private IOpcUaSampler? _sampler;
        }

        /// <summary>
        /// Model Change item
        /// </summary>
        [DataContract(Namespace = Namespaces.OpcUaXsd)]
        internal class ModelChangeEventItem : OpcUaMonitoredItem
        {
            /// <summary>
            /// Monitored item as event
            /// </summary>
            public MonitoredAddressSpaceModel Template { get; protected internal set; }

            /// <summary>
            /// Root id
            /// </summary>
            public NodeId? RootNodeId { get; private set; }

            /// <summary>
            /// Create model change item
            /// </summary>
            /// <param name="template"></param>
            /// <param name="client"></param>
            /// <param name="logger"></param>
            public ModelChangeEventItem(MonitoredAddressSpaceModel template, IOpcUaClient client,
                ILogger<ModelChangeEventItem> logger) : base(logger, template.StartNodeId)
            {
                Template = template;
                _client = client;
                _fields = GetEventFields().ToArray();
            }

            /// <summary>
            /// Copy constructor
            /// </summary>
            /// <param name="item"></param>
            /// <param name="copyEventHandlers"></param>
            /// <param name="copyClientHandle"></param>
            private ModelChangeEventItem(ModelChangeEventItem item, bool copyEventHandlers,
                bool copyClientHandle)
                : base(item, copyEventHandlers, copyClientHandle)
            {
                Template = item.Template;
                _client = item._client;
                _callback = item._callback;
                _fields = item._fields;
                RootNodeId = item.RootNodeId;

                _browser = item.CloneBrowser();
                if (_browser != null)
                {
                    _browser.OnReferenceChange += OnReferenceChange;
                    _browser.OnNodeChange += OnNodeChange;
                }
            }

            /// <inheritdoc/>
            public override MonitoredItem CloneMonitoredItem(
                bool copyEventHandlers, bool copyClientHandle)
            {
                return new ModelChangeEventItem(this, copyEventHandlers, copyClientHandle);
            }

            /// <inheritdoc/>
            protected override void Dispose(bool disposing)
            {
                // Cleanup
                var browser = CloneBrowser();
                if (browser != null)
                {
                    browser.CloseAsync().AsTask().GetAwaiter().GetResult();
                }
                base.Dispose(disposing);
            }

            /// <inheritdoc/>
            public override bool Equals(object? obj)
            {
                if (obj is not ModelChangeEventItem modelChange)
                {
                    return false;
                }
                if ((Template.DataSetFieldId ?? string.Empty) !=
                    (modelChange.Template.DataSetFieldId ?? string.Empty))
                {
                    return false;
                }
                if ((Template.DataSetFieldName ?? string.Empty) !=
                    (modelChange.Template.DataSetFieldName ?? string.Empty))
                {
                    return false;
                }
                if (Template.StartNodeId != modelChange.Template.StartNodeId)
                {
                    return false;
                }
                if (Template.RootNodeId != modelChange.Template.RootNodeId)
                {
                    return false;
                }
                if (_client != modelChange._client)
                {
                    return false;
                }
                return true;
            }

            /// <inheritdoc/>
            public override int GetHashCode()
            {
                var hashCode = 435243663;
                hashCode = (hashCode * -1521134295) +
                    EqualityComparer<string>.Default.GetHashCode(
                        Template.DataSetFieldName ?? string.Empty);
                hashCode = (hashCode * -1521134295) +
                    EqualityComparer<string>.Default.GetHashCode(
                        Template.DataSetFieldId ?? string.Empty);
                hashCode = (hashCode * -1521134295) +
                    EqualityComparer<string>.Default.GetHashCode(
                        Template.StartNodeId);
                hashCode = (hashCode * -1521134295) +
                    EqualityComparer<string>.Default.GetHashCode(
                        Template.RootNodeId ?? string.Empty);
                hashCode = (hashCode * -1521134295) +
                    _client.GetHashCode();
                return hashCode;
            }

            /// <inheritdoc/>
            public override string ToString()
            {
                return
                    $"Model Change Item with server id {RemoteId}" +
                    $" - {(Status?.Created == true ? "" : "not ")}created";
            }

            /// <inheritdoc/>
            public override bool MergeWith(OpcUaMonitoredItem item, IOpcUaSession session,
                 out bool metadataChanged)
            {
                metadataChanged = false;
                if (item is not ModelChangeEventItem || !Valid)
                {
                    return false;
                }
                return true;
            }

            /// <inheritdoc/>
            public override ValueTask GetMetaDataAsync(IOpcUaSession session,
                ComplexTypeSystem? typeSystem, FieldMetaDataCollection fields,
                NodeIdDictionary<DataTypeDescription> dataTypes, CancellationToken ct)
            {
                fields.AddRange(_fields);
                return ValueTask.CompletedTask;
            }

            /// <inheritdoc/>
            public override bool TryCompleteChanges(Subscription subscription,
                ref bool applyChanges, Callback cb)
            {
                var result = base.TryCompleteChanges(subscription, ref applyChanges, cb);
                if (!AttachedToSubscription)
                {
                    _callback = null;
                }
                else
                {
                    _callback = cb;
                }
                return result;
            }

            /// <inheritdoc/>
            public override Func<CancellationToken, Task>? FinalizeCompleteChanges => async _ =>
            {
                if (!AttachedToSubscription)
                {
                    // Stop the browser
                    if (_browser != null)
                    {
                        _browser.OnReferenceChange -= OnReferenceChange;
                        _browser.OnNodeChange -= OnNodeChange;

                        await _browser.CloseAsync().ConfigureAwait(false);
                        _logger.LogInformation("Item {Item} unregistered from browser.", this);
                        _browser = null;
                    }
                }
                else
                {
                    // Start the browser
                    if (_browser == null)
                    {
                        _browser = _client.Browse(Template.RebrowsePeriod ??
                            TimeSpan.FromHours(12), RootNodeId ?? ObjectIds.RootFolder);

                        _browser.OnReferenceChange += OnReferenceChange;
                        _browser.OnNodeChange += OnNodeChange;
                        _logger.LogInformation("Item {Item} registered with browser.", this);
                    }
                }
            };

            /// <inheritdoc/>
            public override bool AddTo(Subscription subscription,
                IOpcUaSession session, out bool metadataChanged)
            {
                var nodeId = NodeId.ToNodeId(session.MessageContext);
                if (Opc.Ua.NodeId.IsNull(nodeId))
                {
                    metadataChanged = false;
                    return false;
                }

                RootNodeId = Template.RootNodeId.ToNodeId(session.MessageContext);
                if (Opc.Ua.NodeId.IsNull(RootNodeId))
                {
                    RootNodeId = ObjectIds.RootFolder;
                }

                DisplayName = Template.DisplayName;
                AttributeId = Attributes.EventNotifier;
                MonitoringMode = Opc.Ua.MonitoringMode.Reporting;
                StartNodeId = nodeId;
                QueueSize = Template.QueueSize;
                SamplingInterval = 0;
                Filter = GetEventFilter();
                DiscardOldest = !(Template.DiscardNew ?? false);
                Valid = true;

                return base.AddTo(subscription, session, out metadataChanged);

                static MonitoringFilter GetEventFilter()
                {
                    var eventFilter = new EventFilter();
                    eventFilter.SelectClauses.Add(new SimpleAttributeOperand()
                    {
                        BrowsePath = new QualifiedNameCollection { BrowseNames.EventType },
                        TypeDefinitionId = ObjectTypeIds.BaseModelChangeEventType,
                        AttributeId = Attributes.NodeId
                    });
                    eventFilter.SelectClauses.Add(new SimpleAttributeOperand()
                    {
                        BrowsePath = new QualifiedNameCollection { BrowseNames.Changes },
                        TypeDefinitionId = ObjectTypeIds.GeneralModelChangeEventType,
                        AttributeId = Attributes.Value
                    });
                    eventFilter.WhereClause = new ContentFilter();
                    eventFilter.WhereClause.Push(FilterOperator.OfType,
                        ObjectTypeIds.BaseModelChangeEventType);
                    return eventFilter;
                }
            }

            /// <inheritdoc/>
            public override bool TryGetMonitoredItemNotifications(uint sequenceNumber, DateTime timestamp,
                IEncodeable evt, IList<MonitoredItemNotificationModel> notifications)
            {
                if (evt is not EventFieldList eventFields ||
                    !base.TryGetMonitoredItemNotifications(sequenceNumber, timestamp, evt, notifications))
                {
                    return false;
                }

                // Rebrowse and find changes or just process and send the changes
                Debug.Assert(Valid);
                Debug.Assert(Template != null);

                var evFilter = Filter as EventFilter;
                var eventTypeIndex = evFilter?.SelectClauses.IndexOf(
                    evFilter.SelectClauses
                        .FirstOrDefault(x => x.TypeDefinitionId == ObjectTypeIds.BaseEventType
                            && x.BrowsePath?.FirstOrDefault() == BrowseNames.EventType));

                if (eventTypeIndex.HasValue && eventTypeIndex.Value != -1)
                {
                    var eventType = eventFields.EventFields[eventTypeIndex.Value].Value as NodeId;
                    if (eventType == ObjectTypeIds.GeneralModelChangeEventType)
                    {
                        // Find what changed and refresh only that
                        // return true;
                    }
                    else
                    {
                        Debug.Assert(eventType == ObjectTypeIds.BaseModelChangeEventType);
                    }
                }

                // The model changed, trigger Rebrowse
                _browser?.Rebrowse();
                return true;
            }

            /// <inheritdoc/>
            protected override bool TryGetErrorMonitoredItemNotifications(
                uint sequenceNumber, StatusCode statusCode,
                IList<MonitoredItemNotificationModel> notifications)
            {
                return true;
            }

            /// <summary>
            /// Called when node changed
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
            private void OnNodeChange(object? sender, Change<Node> e)
            {
                _callback?.Invoke(MessageType.Event, CreateEvent(_nodeChangeType, e),
                    sender as ISession, DataSetName);
            }

            /// <summary>
            /// Called when reference changes
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
            private void OnReferenceChange(object? sender, Change<ReferenceDescription> e)
            {
                _callback?.Invoke(MessageType.Event, CreateEvent(_refChangeType, e),
                    sender as ISession, DataSetName);
            }

            /// <summary>
            /// Clone the browser
            /// </summary>
            /// <returns></returns>
            private IOpcUaBrowser? CloneBrowser()
            {
                var browser = _browser;
                _browser = null;
                if (browser != null)
                {
                    browser.OnReferenceChange -= OnReferenceChange;
                    browser.OnNodeChange -= OnNodeChange;
                }
                return browser;
            }

            /// <summary>
            /// Create the event
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="eventType"></param>
            /// <param name="changeFeedNotification"></param>
            /// <returns></returns>
            private IEnumerable<MonitoredItemNotificationModel> CreateEvent<T>(ExpandedNodeId eventType,
                Change<T> changeFeedNotification) where T : class
            {
                for (var i = 0; i < _fields.Length; i++)
                {
                    Variant? value = null;
                    var field = _fields[i];
                    switch (i)
                    {
                        case 0:
                            value = new Variant((Uuid)Guid.NewGuid());
                            break;
                        case 1:
                            value = eventType;
                            break;
                        case 2:
                            value = new Variant(changeFeedNotification.Source);
                            break;
                        case 3:
                            value = new Variant(changeFeedNotification.Timestamp);
                            break;
                        case 4:
                            value = changeFeedNotification.ChangedItem == null ?
                                Variant.Null : new Variant(changeFeedNotification.ChangedItem);
                            break;
                    }
                    if (value == null)
                    {
                        continue;
                    }
                    yield return new MonitoredItemNotificationModel
                    {
                        Id = Template.Id ?? string.Empty,
                        DataSetName = Template.DisplayName,
                        DataSetFieldName = field.Name,
                        NodeId = Template.StartNodeId,
                        Value = new DataValue(value.Value),
                        Flags = MonitoredItemSourceFlags.ModelChanges,
                        SequenceNumber = changeFeedNotification.SequenceNumber
                    };
                }
            }

            private static IEnumerable<FieldMetaData> GetEventFields()
            {
                yield return Create(BrowseNames.EventId, builtInType: BuiltInType.ByteString);
                yield return Create(BrowseNames.EventType, builtInType: BuiltInType.NodeId);
                yield return Create(BrowseNames.SourceNode, builtInType: BuiltInType.NodeId);
                yield return Create(BrowseNames.Time, builtInType: BuiltInType.NodeId);
                yield return Create("Change", builtInType: BuiltInType.ExtensionObject);

                static FieldMetaData Create(string fieldName, NodeId? dataType = null,
                    BuiltInType builtInType = BuiltInType.ExtensionObject)
                {
                    return new FieldMetaData
                    {
                        DataSetFieldId = (Uuid)Guid.NewGuid(),
                        DataType = dataType ?? new NodeId((uint)builtInType),
                        Name = fieldName,
                        ValueRank = ValueRanks.Scalar,
                        // ArrayDimensions =
                        BuiltInType = (byte)builtInType
                    };
                }
            }

            private static readonly ExpandedNodeId _refChangeType
                = new("ReferenceChange", "http://www.microsoft.com/opc-publisher");
            private static readonly ExpandedNodeId _nodeChangeType
                = new("NodeChange", "http://www.microsoft.com/opc-publisher");
            private readonly FieldMetaData[] _fields;
            private readonly IOpcUaClient _client;
            private IOpcUaBrowser? _browser;
            private Callback? _callback;
        }

        /// <summary>
        /// Event monitored item
        /// </summary>
        [DataContract(Namespace = Namespaces.OpcUaXsd)]
        [KnownType(typeof(DataChangeFilter))]
        [KnownType(typeof(EventFilter))]
        [KnownType(typeof(AggregateFilter))]
        internal class EventItem : OpcUaMonitoredItem
        {
            /// <inheritdoc/>
            public override (string NodeId, UpdateString Update)? GetDisplayName
                => Template.FetchDataSetFieldName == true &&
                    !string.IsNullOrEmpty(Template.EventFilter.TypeDefinitionId) &&
                    Template.DataSetFieldName == null ?
                    (Template.EventFilter.TypeDefinitionId!, v => Template = Template with
                    {
                        DataSetFieldName = v
                    }) : null;

            /// <inheritdoc/>
            public override (string NodeId, string[] Path, UpdateNodeId Update)? Resolve
                => Template.RelativePath != null &&
                    (NodeId == Template.StartNodeId || string.IsNullOrEmpty(NodeId)) ?
                    (Template.StartNodeId, Template.RelativePath.ToArray(),
                        (v, context) => NodeId
                            = v.AsString(context, Template.NamespaceFormat) ?? string.Empty) : null;

            /// <inheritdoc/>
            public override string? DataSetName => Template.DisplayName;

            /// <summary>
            /// Monitored item as event
            /// </summary>
            public EventMonitoredItemModel Template { get; protected internal set; }

            /// <summary>
            /// List of field names.
            /// </summary>
            public List<(string? Name, Guid DataSetFieldId)> Fields { get; } = new();

            /// <summary>
            /// Create wrapper
            /// </summary>
            /// <param name="template"></param>
            /// <param name="logger"></param>
            public EventItem(EventMonitoredItemModel template,
                ILogger<EventItem> logger) : base(logger, template.StartNodeId)
            {
                Template = template;
            }

            /// <summary>
            /// Copy constructor
            /// </summary>
            /// <param name="item"></param>
            /// <param name="copyEventHandlers"></param>
            /// <param name="copyClientHandle"></param>
            protected EventItem(EventItem item, bool copyEventHandlers,
                bool copyClientHandle)
                : base(item, copyEventHandlers, copyClientHandle)
            {
                Fields = item.Fields;
                Template = item.Template;
            }

            /// <inheritdoc/>
            public override MonitoredItem CloneMonitoredItem(
                bool copyEventHandlers, bool copyClientHandle)
            {
                return new EventItem(this, copyEventHandlers, copyClientHandle);
            }

            /// <inheritdoc/>
            public override bool Equals(object? obj)
            {
                if (obj is not EventItem eventItem)
                {
                    return false;
                }
                if ((Template.DataSetFieldId ?? string.Empty) !=
                    (eventItem.Template.DataSetFieldId ?? string.Empty))
                {
                    return false;
                }
                if ((Template.DataSetFieldName ?? string.Empty) !=
                    (eventItem.Template.DataSetFieldName ?? string.Empty))
                {
                    return false;
                }
                if (!Template.RelativePath.SequenceEqualsSafe(eventItem.Template.RelativePath))
                {
                    return false;
                }
                if (Template.StartNodeId != eventItem.Template.StartNodeId)
                {
                    return false;
                }
                if (Template.AttributeId != eventItem.Template.AttributeId)
                {
                    return false;
                }
                return true;
            }

            /// <inheritdoc/>
            public override int GetHashCode()
            {
                var hashCode = 423444443;
                hashCode = (hashCode * -1521134295) +
                    EqualityComparer<string>.Default.GetHashCode(
                        Template.DataSetFieldName ?? string.Empty);
                hashCode = (hashCode * -1521134295) +
                    EqualityComparer<string>.Default.GetHashCode(
                        Template.DataSetFieldId ?? string.Empty);
                hashCode = (hashCode * -1521134295) +
                    EqualityComparer<IReadOnlyList<string>>.Default.GetHashCode(
                        Template.RelativePath ?? Array.Empty<string>());
                hashCode = (hashCode * -1521134295) +
                    EqualityComparer<string>.Default.GetHashCode(
                        Template.StartNodeId);
                hashCode = (hashCode * -1521134295) +
                    EqualityComparer<NodeAttribute>.Default.GetHashCode(
                        Template.AttributeId ?? NodeAttribute.NodeId);
                return hashCode;
            }

            /// <inheritdoc/>
            public override string ToString()
            {
                return $"Event Item '{Template.StartNodeId}' with server id {RemoteId} - " +
                    $"{(Status?.Created == true ? "" : "not ")}created";
            }

            /// <inheritdoc/>
            public override async ValueTask GetMetaDataAsync(IOpcUaSession session,
                ComplexTypeSystem? typeSystem, FieldMetaDataCollection fields,
                NodeIdDictionary<DataTypeDescription> dataTypes, CancellationToken ct)
            {
                if (Filter is not EventFilter eventFilter)
                {
                    return;
                }
                try
                {
                    Debug.Assert(Fields.Count == eventFilter.SelectClauses.Count);
                    for (var i = 0; i < eventFilter.SelectClauses.Count; i++)
                    {
                        var selectClause = eventFilter.SelectClauses[i];
                        var fieldName = Fields[i].Name;
                        if (fieldName == null)
                        {
                            continue;
                        }
                        var dataSetClassFieldId = (Uuid)Fields[i].DataSetFieldId;
                        var targetNode = await FindNodeWithBrowsePathAsync(session, selectClause.BrowsePath,
                            selectClause.TypeDefinitionId, ct).ConfigureAwait(false);
                        if (targetNode is VariableNode variable)
                        {
                            await AddVariableFieldAsync(fields, dataTypes, session, typeSystem, variable,
                                fieldName, dataSetClassFieldId, ct).ConfigureAwait(false);
                        }
                        else
                        {
                            fields.Add(new FieldMetaData
                            {
                                Name = fieldName,
                                DataSetFieldId = dataSetClassFieldId
                            });
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.LogDebug(e, "{Item}: Failed to get metadata for event.", this);
                    throw;
                }
            }

            public override Func<IOpcUaSession, CancellationToken, Task>? FinalizeAddTo
                => async (session, ct)
                => Filter = await GetEventFilterAsync(session, ct).ConfigureAwait(false);

            /// <inheritdoc/>
            public override bool AddTo(Subscription subscription,
                IOpcUaSession session, out bool metadataChanged)
            {
                var nodeId = NodeId.ToNodeId(session.MessageContext);
                if (Opc.Ua.NodeId.IsNull(nodeId))
                {
                    metadataChanged = false;
                    return false;
                }
                DisplayName = Template.DisplayName;
                AttributeId = (uint)(Template.AttributeId
                    ?? (NodeAttribute)Attributes.EventNotifier);
                MonitoringMode = Template.MonitoringMode.ToStackType()
                    ?? Opc.Ua.MonitoringMode.Reporting;
                StartNodeId = nodeId;
                QueueSize = Template.QueueSize;
                SamplingInterval = 0;
                DiscardOldest = !(Template.DiscardNew ?? false);
                Valid = true;

                return base.AddTo(subscription, session, out metadataChanged);
            }

            /// <inheritdoc/>
            public override bool MergeWith(OpcUaMonitoredItem item, IOpcUaSession session,
                 out bool metadataChanged)
            {
                metadataChanged = false;
                if (item is not EventItem model || !Valid)
                {
                    return false;
                }

                var itemChange = MergeWith(Template, model.Template, out var updated,
                    out metadataChanged);
                if (itemChange)
                {
                    Template = updated;
                }

                // Update event filter
                if (!model.Template.EventFilter.IsSameAs(Template.EventFilter))
                {
                    Template = Template with
                    {
                        EventFilter = model.Template.EventFilter
                    };
                    _logger.LogDebug("{Item}: Changing event filter.", this);
                    metadataChanged = true;
                    itemChange = true;
                }
                return itemChange;
            }

            public override Func<IOpcUaSession, CancellationToken, Task>? FinalizeMergeWith
                => async (session, ct)
                => Filter = await GetEventFilterAsync(session, ct).ConfigureAwait(false);

            /// <inheritdoc/>
            public override bool TryGetMonitoredItemNotifications(uint sequenceNumber, DateTime timestamp,
                IEncodeable evt, IList<MonitoredItemNotificationModel> notifications)
            {
                if (evt is EventFieldList eventFields &&
                    base.TryGetMonitoredItemNotifications(sequenceNumber, timestamp, evt, notifications))
                {
                    return ProcessEventNotification(sequenceNumber, timestamp, eventFields, notifications);
                }
                return false;
            }

            /// <inheritdoc/>
            protected override bool TryGetErrorMonitoredItemNotifications(
                uint sequenceNumber, StatusCode statusCode,
                IList<MonitoredItemNotificationModel> notifications)
            {
                foreach (var field in Fields)
                {
                    if (field.Name == null)
                    {
                        continue;
                    }
                    notifications.Add(new MonitoredItemNotificationModel
                    {
                        Id = Template.Id ?? string.Empty,
                        DataSetName = Template.DisplayName,
                        DataSetFieldName = field.Name,
                        NodeId = Template.StartNodeId,
                        Value = new DataValue(statusCode),
                        Flags = MonitoredItemSourceFlags.Error,
                        SequenceNumber = sequenceNumber
                    });
                }
                return true;
            }

            /// <summary>
            /// Process event notifications
            /// </summary>
            /// <param name="sequenceNumber"></param>
            /// <param name="timestamp"></param>
            /// <param name="eventFields"></param>
            /// <param name="notifications"></param>
            /// <returns></returns>
            protected virtual bool ProcessEventNotification(uint sequenceNumber, DateTime timestamp,
                EventFieldList eventFields, IList<MonitoredItemNotificationModel> notifications)
            {
                // Send notifications as event
                foreach (var n in ToMonitoredItemNotifications(sequenceNumber, eventFields)
                    .Where(n => n.DataSetFieldName != null))
                {
                    notifications.Add(n);
                }
                return true;
            }

            /// <summary>
            /// Convert to monitored item notifications
            /// </summary>
            /// <param name="sequenceNumber"></param>
            /// <param name="eventFields"></param>
            /// <returns></returns>
            protected IEnumerable<MonitoredItemNotificationModel> ToMonitoredItemNotifications(
                uint sequenceNumber, EventFieldList eventFields)
            {
                Debug.Assert(Valid);
                Debug.Assert(Template != null);

                if (Fields.Count >= eventFields.EventFields.Count)
                {
                    for (var i = 0; i < eventFields.EventFields.Count; i++)
                    {
                        yield return new MonitoredItemNotificationModel
                        {
                            Id = Template.Id ?? string.Empty,
                            DataSetName = Template.DisplayName,
                            DataSetFieldName = Fields[i].Name,
                            NodeId = Template.StartNodeId,
                            Value = new DataValue(eventFields.EventFields[i]),
                            SequenceNumber = sequenceNumber
                        };
                    }
                }
            }

            /// <summary>
            /// Get event filter
            /// </summary>
            /// <param name="session"></param>
            /// <param name="ct"></param>
            /// <returns></returns>
            protected virtual async ValueTask<EventFilter> GetEventFilterAsync(IOpcUaSession session,
                CancellationToken ct)
            {
                var (eventFilter, internalSelectClauses) =
                    await BuildEventFilterAsync(session, ct).ConfigureAwait(false);
                UpdateFieldNames(session, eventFilter, internalSelectClauses);
                return eventFilter;
            }

            /// <summary>
            /// Update field names
            /// </summary>
            /// <param name="session"></param>
            /// <param name="eventFilter"></param>
            /// <param name="internalSelectClauses"></param>
            protected void UpdateFieldNames(IOpcUaSession session, EventFilter eventFilter,
                List<SimpleAttributeOperand> internalSelectClauses)
            {
                // let's loop thru the final set of select clauses and setup the field names used
                Fields.Clear();
                foreach (var selectClause in eventFilter.SelectClauses)
                {
                    if (!internalSelectClauses.Any(x => x == selectClause))
                    {
                        var fieldName = string.Empty;
                        var definedSelectClause = Template.EventFilter.SelectClauses?
                            .ElementAtOrDefault(eventFilter.SelectClauses.IndexOf(selectClause));
                        if (!string.IsNullOrEmpty(definedSelectClause?.DisplayName))
                        {
                            fieldName = definedSelectClause.DisplayName;
                        }
                        else if (selectClause.BrowsePath != null && selectClause.BrowsePath.Count != 0)
                        {
                            // Format as relative path string
                            fieldName = selectClause.BrowsePath
                                .Select(q => q.AsString(session.MessageContext, Template.NamespaceFormat))
                                .Aggregate((a, b) => $"{a}/{b}");
                        }

                        if (fieldName.Length == 0 &&
                            selectClause.TypeDefinitionId == ObjectTypeIds.ConditionType &&
                            selectClause.AttributeId == Attributes.NodeId)
                        {
                            fieldName = "ConditionId";
                        }
                        Fields.Add((fieldName, Guid.NewGuid()));
                    }
                    else
                    {
                        // if a field's nameis empty, it's not written to the output
                        Fields.Add((null, Guid.Empty));
                    }
                }
                Debug.Assert(Fields.Count == eventFilter.SelectClauses.Count);
            }

            /// <summary>
            /// Build event filter
            /// </summary>
            /// <param name="session"></param>
            /// <param name="ct"></param>
            /// <returns></returns>
            protected async ValueTask<(EventFilter, List<SimpleAttributeOperand>)> BuildEventFilterAsync(
                IOpcUaSession session, CancellationToken ct)
            {
                EventFilter? eventFilter;
                if (!string.IsNullOrEmpty(Template.EventFilter.TypeDefinitionId))
                {
                    eventFilter = await GetSimpleEventFilterAsync(session, ct).ConfigureAwait(false);
                }
                else
                {
                    eventFilter = session.Codec.Decode(Template.EventFilter);
                }

                // let's keep track of the internal fields we add so that they don't show up in the output
                var selectClauses = new List<SimpleAttributeOperand>();
                if (!eventFilter.SelectClauses.Any(x => x.TypeDefinitionId == ObjectTypeIds.BaseEventType
                    && x.BrowsePath?.FirstOrDefault() == BrowseNames.EventType))
                {
                    var selectClause = new SimpleAttributeOperand(ObjectTypeIds.BaseEventType, BrowseNames.EventType);
                    eventFilter.SelectClauses.Add(selectClause);
                    selectClauses.Add(selectClause);
                }
                return (eventFilter, selectClauses);
            }

            /// <summary>
            /// Builds select clause and where clause by using OPC UA reflection
            /// </summary>
            /// <param name="session"></param>
            /// <param name="ct"></param>
            /// <returns></returns>
            private async ValueTask<EventFilter> GetSimpleEventFilterAsync(IOpcUaSession session,
                CancellationToken ct)
            {
                Debug.Assert(Template != null);
                var typeDefinitionId = Template.EventFilter.TypeDefinitionId.ToNodeId(
                    session.MessageContext);
                var nodes = new List<Node>();
                ExpandedNodeId? superType = null;
                var typeDefinitionNode = await session.NodeCache.FetchNodeAsync(typeDefinitionId,
                    ct).ConfigureAwait(false);
                nodes.Insert(0, typeDefinitionNode);
                do
                {
                    superType = nodes[0].GetSuperType(session.TypeTree);
                    if (superType != null)
                    {
                        typeDefinitionNode = await session.NodeCache.FetchNodeAsync(superType,
                            ct).ConfigureAwait(false);
                        nodes.Insert(0, typeDefinitionNode);
                    }
                }
                while (superType != null);

                var fieldNames = new List<QualifiedName>();

                foreach (var node in nodes)
                {
                    await ParseFieldsAsync(session, fieldNames, node, string.Empty, ct).ConfigureAwait(false);
                }
                fieldNames = fieldNames
                    .Distinct()
                    .OrderBy(x => x.Name).ToList();

                var eventFilter = new EventFilter();
                // Let's add ConditionId manually first if event is derived from ConditionType
                if (nodes.Any(x => x.NodeId == ObjectTypeIds.ConditionType))
                {
                    eventFilter.SelectClauses.Add(new SimpleAttributeOperand()
                    {
                        BrowsePath = new QualifiedNameCollection(),
                        TypeDefinitionId = ObjectTypeIds.ConditionType,
                        AttributeId = Attributes.NodeId
                    });
                }

                foreach (var fieldName in fieldNames)
                {
                    var selectClause = new SimpleAttributeOperand()
                    {
                        TypeDefinitionId = ObjectTypeIds.BaseEventType,
                        AttributeId = Attributes.Value,
                        BrowsePath = fieldName.Name
                            .Split('|')
                            .Select(x => new QualifiedName(x, fieldName.NamespaceIndex))
                            .ToArray()
                    };
                    eventFilter.SelectClauses.Add(selectClause);
                }
                eventFilter.WhereClause = new ContentFilter();
                eventFilter.WhereClause.Push(FilterOperator.OfType, typeDefinitionId);

                return eventFilter;
            }

            /// <summary>
            /// Find node by browse path
            /// </summary>
            /// <param name="session"></param>
            /// <param name="browsePath"></param>
            /// <param name="nodeId"></param>
            /// <param name="ct"></param>
            /// <returns></returns>
            private static async ValueTask<INode?> FindNodeWithBrowsePathAsync(IOpcUaSession session,
                QualifiedNameCollection browsePath, ExpandedNodeId nodeId, CancellationToken ct)
            {
                INode? found = null;
                foreach (var browseName in browsePath)
                {
                    found = null;
                    while (found == null)
                    {
                        found = await session.NodeCache.FindAsync(nodeId, ct).ConfigureAwait(false);
                        if (found is not Node node)
                        {
                            return null;
                        }

                        //
                        // Get all hierarchical references of the node and
                        // match browse name
                        //
                        foreach (var reference in node.ReferenceTable.Find(
                            ReferenceTypeIds.HierarchicalReferences, false,
                                true, session.TypeTree))
                        {
                            var target = await session.NodeCache.FindAsync(reference.TargetId,
                                ct).ConfigureAwait(false);
                            if (target?.BrowseName == browseName)
                            {
                                nodeId = target.NodeId;
                                found = target;
                                break;
                            }
                        }

                        if (found == null)
                        {
                            // Try super type
                            nodeId = await session.TypeTree.FindSuperTypeAsync(nodeId,
                                ct).ConfigureAwait(false);
                            if (Opc.Ua.NodeId.IsNull(nodeId))
                            {
                                // Nothing can be found since there is no more super type
                                return null;
                            }
                        }
                    }
                    nodeId = found.NodeId;
                }
                return found;
            }

            /// <summary>
            /// Get all the fields of a type definition node to build the
            /// select clause.
            /// </summary>
            /// <param name="session"></param>
            /// <param name="fieldNames"></param>
            /// <param name="node"></param>
            /// <param name="browsePathPrefix"></param>
            /// <param name="ct"></param>
            protected static async ValueTask ParseFieldsAsync(IOpcUaSession session, List<QualifiedName> fieldNames,
                Node node, string browsePathPrefix, CancellationToken ct)
            {
                foreach (var reference in node.ReferenceTable)
                {
                    if (reference.ReferenceTypeId == ReferenceTypeIds.HasComponent &&
                        !reference.IsInverse)
                    {
                        var componentNode = await session.NodeCache.FetchNodeAsync(reference.TargetId,
                            ct).ConfigureAwait(false);
                        if (componentNode.NodeClass == Opc.Ua.NodeClass.Variable)
                        {
                            var fieldName = browsePathPrefix + componentNode.BrowseName.Name;
                            fieldNames.Add(new QualifiedName(
                                fieldName, componentNode.BrowseName.NamespaceIndex));
                            await ParseFieldsAsync(session, fieldNames, componentNode,
                                $"{fieldName}|", ct).ConfigureAwait(false);
                        }
                    }
                    else if (reference.ReferenceTypeId == ReferenceTypeIds.HasProperty)
                    {
                        var propertyNode = await session.NodeCache.FetchNodeAsync(reference.TargetId,
                            ct).ConfigureAwait(false);
                        var fieldName = browsePathPrefix + propertyNode.BrowseName.Name;
                        fieldNames.Add(new QualifiedName(
                            fieldName, propertyNode.BrowseName.NamespaceIndex));
                    }
                }
            }
        }

        /// <summary>
        /// Condition item
        /// </summary>
        [DataContract(Namespace = Namespaces.OpcUaXsd)]
        [KnownType(typeof(DataChangeFilter))]
        [KnownType(typeof(EventFilter))]
        [KnownType(typeof(AggregateFilter))]
        internal class Condition : EventItem
        {
            /// <summary>
            /// Create condition item
            /// </summary>
            /// <param name="template"></param>
            /// <param name="logger"></param>
            public Condition(EventMonitoredItemModel template,
                ILogger<EventItem> logger) : base(template, logger)
            {
                _snapshotInterval = template.ConditionHandling?.SnapshotInterval
                    ?? throw new ArgumentException("Invalid snapshot interval");
                _updateInterval = template.ConditionHandling?.UpdateInterval
                    ?? _snapshotInterval;

                _conditionHandlingState = new ConditionHandlingState();
                _conditionTimer = new Timer();
                _conditionTimer.Elapsed += OnConditionTimerElapsed;
                _conditionTimer.AutoReset = false;
                _conditionTimer.Enabled = true;
            }

            /// <summary>
            /// Copy constructor
            /// </summary>
            /// <param name="item"></param>
            /// <param name="copyEventHandlers"></param>
            /// <param name="copyClientHandle"></param>
            private Condition(Condition item, bool copyEventHandlers,
                bool copyClientHandle)
                : base(item, copyEventHandlers, copyClientHandle)
            {
                _snapshotInterval = item._snapshotInterval;
                _updateInterval = item._updateInterval;
                _conditionHandlingState = item._conditionHandlingState;
                _lastSentPendingConditions = item._lastSentPendingConditions;
                _callback = item._callback;
                _conditionTimer = item.CloneTimer();
                if (_conditionTimer != null)
                {
                    _conditionTimer.Elapsed += OnConditionTimerElapsed;
                }
            }

            /// <inheritdoc/>
            public override MonitoredItem CloneMonitoredItem(
                bool copyEventHandlers, bool copyClientHandle)
            {
                return new Condition(this, copyEventHandlers, copyClientHandle);
            }

            /// <inheritdoc/>
            public override bool Equals(object? obj)
            {
                if (obj is not Condition item)
                {
                    return false;
                }
                return base.Equals(item);
            }

            /// <inheritdoc/>
            public override int GetHashCode()
            {
                return 38138123 + base.GetHashCode();
            }

            /// <inheritdoc/>
            public override string ToString()
            {
                return
                    $"Condition Item '{Template.StartNodeId}' with server id {RemoteId}" +
                    $" - {(Status?.Created == true ? "" : "not ")}created";
            }

            /// <inheritdoc/>
            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    var timer = CloneTimer();
                    timer?.Dispose();
                }
                base.Dispose(disposing);
            }

            /// <inheritdoc/>
            protected override bool ProcessEventNotification(uint sequenceNumber, DateTime timestamp,
                EventFieldList eventFields, IList<MonitoredItemNotificationModel> notifications)
            {
                Debug.Assert(Valid);
                Debug.Assert(Template != null);

                if (_conditionTimer == null)
                {
                    return false;
                }

                var evFilter = Filter as EventFilter;
                var eventTypeIndex = evFilter?.SelectClauses.IndexOf(
                    evFilter.SelectClauses
                        .FirstOrDefault(x => x.TypeDefinitionId == ObjectTypeIds.BaseEventType
                            && x.BrowsePath?.FirstOrDefault() == BrowseNames.EventType));

                var state = _conditionHandlingState;

                // now, is this a regular event or RefreshStartEventType/RefreshEndEventType?
                if (eventTypeIndex.HasValue && eventTypeIndex.Value != -1)
                {
                    var eventType = eventFields.EventFields[eventTypeIndex.Value].Value as NodeId;
                    if (eventType == ObjectTypeIds.RefreshStartEventType)
                    {
                        // stop the timers during condition refresh
                        _conditionTimer.Enabled = false;
                        state.Active.Clear();
                        _logger.LogDebug("{Item}: Stopped pending alarm handling " +
                            "during condition refresh.", this);
                        return true;
                    }
                    else if (eventType == ObjectTypeIds.RefreshEndEventType)
                    {
                        // restart the timers once condition refresh is done.
                        _conditionTimer.Interval = 1000;
                        _conditionTimer.Enabled = true;
                        _logger.LogDebug("{Item}: Restarted pending alarm handling " +
                            "after condition refresh.", this);
                        return true;
                    }
                    else if (eventType == ObjectTypeIds.RefreshRequiredEventType)
                    {
                        var noErrorFound = true;

                        // issue a condition refresh to make sure we are in a correct state
                        _logger.LogInformation("{Item}: Issuing ConditionRefresh for " +
                            "item {Name} on subscription {Subscription} due to receiving " +
                            "a RefreshRequired event", this, Template.DisplayName,
                            Subscription.DisplayName);
                        try
                        {
                            Subscription.ConditionRefresh();
                        }
                        catch (Exception e)
                        {
                            _logger.LogInformation("{Item}: ConditionRefresh for item {Name} " +
                                "on subscription {Subscription} failed with error '{Message}'",
                                this, Template.DisplayName, Subscription.DisplayName, e.Message);
                            noErrorFound = false;
                        }
                        if (noErrorFound)
                        {
                            _logger.LogInformation("{Item}: ConditionRefresh for item {Name} " +
                                "on subscription {Subscription} has completed", this,
                                Template.DisplayName, Subscription.DisplayName);
                        }
                        return true;
                    }
                }

                var monitoredItemNotifications = ToMonitoredItemNotifications(
                    sequenceNumber, eventFields).ToList();
                var conditionIdIndex = state.ConditionIdIndex;
                var retainIndex = state.RetainIndex;
                if (conditionIdIndex < monitoredItemNotifications.Count &&
                    retainIndex < monitoredItemNotifications.Count)
                {
                    // Cache conditions
                    var conditionId = monitoredItemNotifications[conditionIdIndex].Value?
                        .Value?.ToString();
                    if (conditionId != null)
                    {
                        var retain = monitoredItemNotifications[retainIndex].Value?
                            .GetValue(false) ?? false;

                        if (state.Active.ContainsKey(conditionId) && !retain)
                        {
                            state.Active.Remove(conditionId, out _);
                            state.Dirty = true;
                        }
                        else if (retain && !monitoredItemNotifications
                            .All(m => m.Value?.Value == null))
                        {
                            state.Dirty = true;
                            monitoredItemNotifications.ForEach(n =>
                            {
                                n.Value ??= new DataValue(StatusCodes.GoodNoData);
                                // Set SourceTimestamp to publish time
                                n.Value.SourceTimestamp = timestamp;
                            });
                            state.Active.AddOrUpdate(conditionId, monitoredItemNotifications);
                        }
                    }
                }
                return true;
            }

            /// <inheritdoc/>
            public override bool MergeWith(OpcUaMonitoredItem item, IOpcUaSession session,
                 out bool metadataChanged)
            {
                metadataChanged = false;
                if (item is not Condition model || !Valid)
                {
                    return false;
                }

                var itemChange = false;
                if (_snapshotInterval != model._snapshotInterval)
                {
                    _logger.LogDebug("{Item}: Changing shptshot interval from {Old} to {New}",
                        this, TimeSpan.FromSeconds(_snapshotInterval).TotalMilliseconds,
                        TimeSpan.FromSeconds(model._snapshotInterval).TotalMilliseconds);

                    _snapshotInterval = model._snapshotInterval;
                    itemChange = true;
                }

                if (_updateInterval != model._updateInterval)
                {
                    _logger.LogDebug("{Item}: Changing update interval from {Old} to {New}",
                        this, TimeSpan.FromSeconds(_updateInterval).TotalMilliseconds,
                        TimeSpan.FromSeconds(model._updateInterval).TotalMilliseconds);

                    _updateInterval = model._updateInterval;
                    itemChange = true;
                }

                itemChange |= base.MergeWith(model, session, out metadataChanged);
                return itemChange;
            }

            /// <inheritdoc/>
            public override bool TryCompleteChanges(Subscription subscription,
                ref bool applyChanges, Callback cb)
            {
                var result = base.TryCompleteChanges(subscription, ref applyChanges, cb);
                if (_conditionTimer == null)
                {
                    return false;
                }
                if (!AttachedToSubscription || !result)
                {
                    _callback = null;
                    _conditionTimer.Enabled = false;
                }
                else
                {
                    _callback = cb;
                    _conditionTimer.Interval = 1000;
                    _conditionTimer.Enabled = true;
                }
                return result;
            }

            /// <summary>
            /// Get event filter
            /// </summary>
            /// <param name="session"></param>
            /// <param name="ct"></param>
            /// <returns></returns>
            protected override async ValueTask<EventFilter> GetEventFilterAsync(IOpcUaSession session,
                CancellationToken ct)
            {
                var (eventFilter, internalSelectClauses) =
                    await BuildEventFilterAsync(session, ct).ConfigureAwait(false);

                var conditionHandlingState = InitializeConditionHandlingState(
                    eventFilter, internalSelectClauses);

                UpdateFieldNames(session, eventFilter, internalSelectClauses);

                _conditionHandlingState = conditionHandlingState;
                if (_conditionTimer != null)
                {
                    _conditionTimer.Interval = 1000;
                    _conditionTimer.Enabled = true;
                }

                return eventFilter;
            }

            /// <summary>
            /// Initialize periodic pending condition handling state
            /// </summary>
            /// <param name="eventFilter"></param>
            /// <param name="internalSelectClauses"></param>
            /// <returns></returns>
            private static ConditionHandlingState InitializeConditionHandlingState(
                EventFilter eventFilter, List<SimpleAttributeOperand> internalSelectClauses)
            {
                var conditionHandlingState = new ConditionHandlingState();

                var conditionIdClause = eventFilter.SelectClauses
                    .FirstOrDefault(x => x.TypeDefinitionId == ObjectTypeIds.ConditionType
                        && x.AttributeId == Attributes.NodeId);
                if (conditionIdClause != null)
                {
                    conditionHandlingState.ConditionIdIndex =
                        eventFilter.SelectClauses.IndexOf(conditionIdClause);
                }
                else
                {
                    conditionHandlingState.ConditionIdIndex = eventFilter.SelectClauses.Count;
                    var selectClause = new SimpleAttributeOperand()
                    {
                        BrowsePath = new QualifiedNameCollection(),
                        TypeDefinitionId = ObjectTypeIds.ConditionType,
                        AttributeId = Attributes.NodeId
                    };
                    eventFilter.SelectClauses.Add(selectClause);
                    internalSelectClauses.Add(selectClause);
                }

                var retainClause = eventFilter.SelectClauses
                    .FirstOrDefault(x => x.TypeDefinitionId == ObjectTypeIds.ConditionType &&
                        x.BrowsePath?.FirstOrDefault() == BrowseNames.Retain);
                if (retainClause != null)
                {
                    conditionHandlingState.RetainIndex =
                        eventFilter.SelectClauses.IndexOf(retainClause);
                }
                else
                {
                    conditionHandlingState.RetainIndex = eventFilter.SelectClauses.Count;
                    var selectClause = new SimpleAttributeOperand(
                        ObjectTypeIds.ConditionType, BrowseNames.Retain);
                    eventFilter.SelectClauses.Add(selectClause);
                    internalSelectClauses.Add(selectClause);
                }
                return conditionHandlingState;
            }

            /// <summary>
            /// Called when the condition timer fires
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
            private void OnConditionTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
            {
                Debug.Assert(Template != null);
                var now = DateTime.UtcNow;
                var state = _conditionHandlingState;
                try
                {
                    if (!Created)
                    {
                        return;
                    }

                    // is it time to send anything?
                    var sendPendingConditions = now >
                        _lastSentPendingConditions + TimeSpan.FromSeconds(_snapshotInterval);
                    if (!sendPendingConditions && state.Dirty)
                    {
                        sendPendingConditions = now >
                            _lastSentPendingConditions + TimeSpan.FromSeconds(_updateInterval);
                    }
                    if (sendPendingConditions)
                    {
                        SendPendingConditions();
                        _lastSentPendingConditions = now;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "{Item}: SendPendingConditions failed.", this);
                }
                finally
                {
                    if (_conditionTimer != null)
                    {
                        _conditionTimer.Interval = 1000;
                        _conditionTimer.Enabled = true;
                    }
                }
            }

            /// <summary>
            /// Send pending conditions
            /// </summary>
            private void SendPendingConditions()
            {
                var state = _conditionHandlingState;
                var callback = _callback;
                if (callback == null)
                {
                    return;
                }

                var notifications = state.Active
                    .Select(entry => entry.Value
                        .Where(n => n.DataSetFieldName != null)
                        .Select(n => n with { })
                        .ToList())
                    .ToList();
                state.Dirty = false;

                foreach (var conditionNotification in notifications)
                {
                    callback(MessageType.Condition, conditionNotification,
                        dataSetName: DataSetName);
                }
            }

            /// <summary>
            /// Clone the timer
            /// </summary>
            /// <returns></returns>
            private Timer? CloneTimer()
            {
                var timer = _conditionTimer;
                _conditionTimer = null;
                if (timer != null)
                {
                    timer.Elapsed -= OnConditionTimerElapsed;
                }
                return timer;
            }

            private sealed class ConditionHandlingState
            {
                /// <summary>
                /// Index in the SelectClause array for Condition id field
                /// </summary>
                public int ConditionIdIndex { get; set; }

                /// <summary>
                /// Index in the SelectClause array for Retain field
                /// </summary>
                public int RetainIndex { get; set; }

                /// <summary>
                /// Has the pending alarms events been updated since las update message?
                /// </summary>
                public bool Dirty { get; set; }

                /// <summary>
                /// Cache of the latest events for the pending alarms optionally monitored
                /// </summary>
                public Dictionary<string, List<MonitoredItemNotificationModel>> Active { get; }
                    = new Dictionary<string, List<MonitoredItemNotificationModel>>();
            }

            private Callback? _callback;
            private ConditionHandlingState _conditionHandlingState;
            private DateTime _lastSentPendingConditions = DateTime.UtcNow;
            private int _snapshotInterval;
            private int _updateInterval;
            private Timer? _conditionTimer;
        }

        /// <summary>
        /// Logger
        /// </summary>
        protected readonly ILogger _logger;
    }
}
