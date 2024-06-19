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
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

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
    /// Update relative path
    /// </summary>
    /// <param name="path"></param>
    /// <param name="messageContext"></param>
    public delegate void UpdateRelativePath(RelativePath path,
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
    internal abstract partial class OpcUaMonitoredItem : MonitoredItem, IDisposable
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
        /// Get the relative path from root for the node. This is called
        /// after the node is resolved but not yet registered
        /// </summary>
        public virtual (string NodeId, UpdateRelativePath Update)? GetPath
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
        /// Last value received
        /// </summary>
        public DateTime? LastReceivedTime { get; private set; }

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

            LastReceivedTime = item.LastReceivedTime;
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
            ComplexTypeSystem? typeSystem, List<PublishedFieldMetaDataModel> fields,
            NodeIdDictionary<object> dataTypes, CancellationToken ct);

        /// <summary>
        /// Called when the underlying session is disconnected
        /// </summary>
        /// <param name="disconnected"></param>
        public virtual void NotifySessionConnectionState(bool disconnected)
        {
        }

        /// <summary>
        /// Check whether the monitored item is late
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public virtual bool WasLastValueReceivedBefore(DateTime dateTime)
        {
            if (!Valid || !AttachedToSubscription)
            {
                return false;
            }
            return !LastReceivedTime.HasValue || LastReceivedTime.Value < dateTime;
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
            try
            {
                LastReceivedValue = (IEncodeable)encodeablePayload.Clone();
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "{Item}: Could not clone last value.", this);
                LastReceivedValue = encodeablePayload;
            }
            LastReceivedTime = DateTime.UtcNow;
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
        protected async ValueTask AddVariableFieldAsync(List<PublishedFieldMetaDataModel> fields,
            NodeIdDictionary<object> dataTypes, IOpcUaSession session,
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
            fields.Add(new PublishedFieldMetaDataModel
            {
                Flags = 0, // Set to 1 << 1 for PromotedField fields.
                Name = fieldName,
                Id = dataSetClassFieldId,
                DataType = variable.DataType.AsString(session.MessageContext,
                    NamespaceFormat.Expanded),
                ArrayDimensions = variable.ArrayDimensions?.Count > 0
                    ? variable.ArrayDimensions : null,
                Description = variable.Description.AsString(),
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
        /// <exception cref="ServiceResultException"></exception>
        private async ValueTask AddDataTypesAsync(NodeIdDictionary<object> dataTypes,
            NodeId dataTypeId, IOpcUaSession session, ComplexTypeSystem? typeSystem,
            CancellationToken ct)
        {
            if (IsBuiltInType(dataTypeId))
            {
                return;
            }

            var typesToResolve = new Queue<NodeId>();
            typesToResolve.Enqueue(dataTypeId);
            while (typesToResolve.Count > 0)
            {
                var baseType = typesToResolve.Dequeue();
                while (!Opc.Ua.NodeId.IsNull(baseType))
                {
                    try
                    {
                        var dataType = await session.NodeCache.FetchNodeAsync(baseType,
                            ct).ConfigureAwait(false);
                        if (dataType == null)
                        {
                            _logger.LogError(
                                "{Item}: Failed to find node for data type {BaseType}!",
                                this, baseType);
                            break;
                        }

                        dataTypeId = dataType.NodeId;
                        Debug.Assert(!Opc.Ua.NodeId.IsNull(dataTypeId));
                        if (IsBuiltInType(dataTypeId))
                        {
                            // Do not add builtin types - we are done here now
                            break;
                        }

                        var builtInType = await TypeInfo.GetBuiltInTypeAsync(dataTypeId,
                            session.TypeTree, ct).ConfigureAwait(false);
                        baseType = await session.TypeTree.FindSuperTypeAsync(dataTypeId,
                            ct).ConfigureAwait(false);

                        var browseName = dataType.BrowseName
                            .AsString(session.MessageContext, NamespaceFormat.Expanded);
                        var typeName = dataType.NodeId
                            .AsString(session.MessageContext, NamespaceFormat.Expanded);
                        if (typeName == null)
                        {
                            // No type name - that should not happen
                            throw new ServiceResultException(StatusCodes.BadDataTypeIdUnknown,
                                $"Failed to get metadata type name for {dataType.NodeId}.");
                        }
                        switch (builtInType)
                        {
                            case BuiltInType.Enumeration:
                            case BuiltInType.ExtensionObject:
                                var types = typeSystem?.GetDataTypeDefinitionsForDataType(
                                    dataType.NodeId);
                                if (types == null || types.Count == 0)
                                {
                                    var dtNode = await session.NodeCache.FetchNodeAsync(dataTypeId,
                                            ct).ConfigureAwait(false);
                                    if (dtNode is DataTypeNode v &&
                                        v.DataTypeDefinition.Body is DataTypeDefinition t)
                                    {
                                        types ??= new NodeIdDictionary<DataTypeDefinition>();
                                        types.Add(dataTypeId, t);
                                    }
                                    else
                                    {
                                        dataTypes.AddOrUpdate(dataType.NodeId, GetDefault(
                                            dataType, builtInType, session.MessageContext));
                                        break;
                                    }
                                }
                                foreach (var type in types)
                                {
                                    if (!dataTypes.ContainsKey(type.Key))
                                    {
                                        var description = type.Value switch
                                        {
                                            StructureDefinition s =>
                                                new StructureDescriptionModel
                                                {
                                                    DataTypeId = typeName,
                                                    Name = browseName,
                                                    BaseDataType = s.BaseDataType.AsString(
                                                        session.MessageContext, NamespaceFormat.Expanded),
                                                    DefaultEncodingId = s.DefaultEncodingId.AsString(
                                                        session.MessageContext, NamespaceFormat.Expanded),
                                                    StructureType = s.StructureType.ToServiceType(),
                                                    Fields = GetFields(s.Fields, typesToResolve,
                                                        session.MessageContext, NamespaceFormat.Expanded)
                                                        .ToList()
                                                },
                                            EnumDefinition e =>
                                                new EnumDescriptionModel
                                                {
                                                    DataTypeId = typeName,
                                                    Name = browseName,
                                                    BuiltInType = null,
                                                    IsOptionSet = e.IsOptionSet,
                                                    Fields = e.Fields
                                                        .Select(f => new EnumFieldDescriptionModel
                                                        {
                                                            Value = f.Value,
                                                            DisplayName = f.DisplayName.AsString(),
                                                            Name = f.Name,
                                                            Description = f.Description.AsString()
                                                        })
                                                        .ToList()
                                                },
                                            _ => GetDefault(dataType, builtInType, session.MessageContext),
                                        };
                                        dataTypes.AddOrUpdate(type.Key, description);
                                    }
                                }
                                break;
                            default:
                                var baseName = baseType
                                    .AsString(session.MessageContext, NamespaceFormat.Expanded);
                                dataTypes.AddOrUpdate(dataTypeId, new SimpleTypeDescriptionModel
                                {
                                    DataTypeId = typeName,
                                    Name = browseName,
                                    BaseDataType = baseName,
                                    BuiltInType = (byte)builtInType
                                });
                                break;
                        }
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        _logger.LogInformation("{Item}: Failed to get meta data for type " +
                            "{DataType} (base: {BaseType}) with message: {Message}", this,
                            dataTypeId, baseType, ex.Message);
                        break;
                    }
                }

                object GetDefault(Node dataType, BuiltInType builtInType, IServiceMessageContext context)
                {
                    _logger.LogError("{Item}: Could not find a valid type definition for {Type} " +
                        "({BuiltInType}). Adding a default placeholder with no fields instead.",
                        this, dataType, builtInType);
                    var name = dataType.BrowseName.AsString(context, NamespaceFormat.Expanded);
                    var dataTypeId = dataType.NodeId.AsString(context, NamespaceFormat.Expanded);
                    return dataTypeId == null
                        ? throw new ServiceResultException(StatusCodes.BadConfigurationError)
                        : builtInType == BuiltInType.Enumeration
                        ? new EnumDescriptionModel
                        {
                            Fields = new List<EnumFieldDescriptionModel>(),
                            DataTypeId = dataTypeId,
                            Name = name
                        }
                        : new StructureDescriptionModel
                        {
                            Fields = new List<StructureFieldDescriptionModel>(),
                            DataTypeId = dataTypeId,
                            Name = name
                        };
                }

                static IEnumerable<StructureFieldDescriptionModel> GetFields(
                    StructureFieldCollection? fields, Queue<NodeId> typesToResolve,
                    IServiceMessageContext context, NamespaceFormat namespaceFormat)
                {
                    if (fields == null)
                    {
                        yield break;
                    }
                    foreach (var f in fields)
                    {
                        if (!IsBuiltInType(f.DataType))
                        {
                            typesToResolve.Enqueue(f.DataType);
                        }
                        yield return new StructureFieldDescriptionModel
                        {
                            IsOptional = f.IsOptional,
                            MaxStringLength = f.MaxStringLength,
                            ValueRank = f.ValueRank,
                            ArrayDimensions = f.ArrayDimensions,
                            DataType = f.DataType.AsString(context, namespaceFormat)
                                ?? string.Empty,
                            Name = f.Name,
                            Description = f.Description.AsString()
                        };
                    }
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
        }

        /// <summary>
        /// Logger
        /// </summary>
        protected readonly ILogger _logger;
    }
}
