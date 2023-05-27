// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Services
{
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Encoders;
    using Microsoft.Extensions.Logging;
    using Opc.Ua;
    using Opc.Ua.Client;
    using Opc.Ua.Client.ComplexTypes;
    using Opc.Ua.Extensions;
    using MonitoringMode = OpcUa.Publisher.Models.MonitoringMode;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using Timer = System.Timers.Timer;
    using static Azure.IIoT.OpcUa.Publisher.Stack.Services.OpcUaSubscription;

    /// <summary>
    /// Monitored item
    /// </summary>
    public sealed class OpcUaMonitoredItem : IDisposable
    {
        /// <summary>
        /// Assigned monitored item id on server
        /// </summary>
        public uint? ServerId => Item?.Status.Id;

        /// <summary>
        /// Monitored item
        /// </summary>
        public BaseMonitoredItemModel Template { get; internal set; }

        /// <summary>
        /// Monitored item as data
        /// </summary>
        public DataMonitoredItemModel? DataTemplate => Template as DataMonitoredItemModel;

        /// <summary>
        /// Monitored item as event
        /// </summary>
        public EventMonitoredItemModel? EventTemplate => Template as EventMonitoredItemModel;

        /// <summary>
        /// Monitored item created from template
        /// </summary>
        public MonitoredItem? Item { get; private set; }

        /// <summary>
        /// Status Code
        /// </summary>
        public StatusCode Status => Item?.Status?.Created == true ?
            Item.Status.Error?.StatusCode ?? StatusCodes.Good : StatusCodes.BadMonitoredItemIdInvalid;

        /// <summary>
        /// Last published time
        /// </summary>
        public DateTime NextHeartbeat { get; private set; }

        /// <summary>
        /// List of field names. Only used for event filter items
        /// </summary>
        public List<(string? Name, Guid DataSetFieldId)> Fields { get; } = new();

        /// <summary>
        /// Field identifier either configured or randomly assigned for data change items
        /// </summary>
        public Guid DataSetFieldId => DataTemplate?.DataSetClassFieldId == Guid.Empty ? _fieldId
            : DataTemplate?.DataSetClassFieldId ?? Guid.Empty;
        private readonly Guid _fieldId = Guid.NewGuid();

        /// <summary>
        /// Property setter that gets indication if item is online or not.
        /// </summary>
        /// <param name="online"></param>
        public void OnMonitoredItemStateChanged(bool online)
        {
            var conditionTimer = _conditionTimer;
            if (conditionTimer == null)
            {
                return;
            }
            lock (_lock)
            {
                if (_conditionHandlingState == null)
                {
                    return;
                }
                if (online)
                {
                    conditionTimer.Start();
                    _logger.LogDebug(
    "{Item}: Restarted pending condition handling after item went online.",
                        this);
                }
                else
                {
                    conditionTimer.Stop();
                    _logger.LogDebug(
    "{Item}: Stopped pending condition handling while item is offline.",
                        this);
                    _conditionHandlingState.Active.Clear();
                }
            }
        }

        /// <summary>
        /// Create wrapper
        /// </summary>
        /// <param name="template"></param>
        /// <param name="logger"></param>
        public OpcUaMonitoredItem(BaseMonitoredItemModel template,
            ILogger<OpcUaMonitoredItem> logger)
        {
            _logger = logger ??
                throw new ArgumentNullException(nameof(logger));
            Template = template ??
                throw new ArgumentNullException(nameof(template));
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj is not OpcUaMonitoredItem wrapper)
            {
                return false;
            }
            if (Template.GetType() != wrapper.Template.GetType())
            {
                // Event item is incompatible with a data item
                return false;
            }
            if (Template.Id != wrapper.Template.Id)
            {
                return false;
            }
            if (!Template.RelativePath.SequenceEqualsSafe(wrapper.Template.RelativePath))
            {
                return false;
            }
            if (Template.StartNodeId != wrapper.Template.StartNodeId)
            {
                return false;
            }
            if (Template.IndexRange != wrapper.Template.IndexRange)
            {
                return false;
            }
            if (Template.AttributeId != wrapper.Template.AttributeId)
            {
                return false;
            }
            return true;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hashCode = 1301977042;
            // Event item is incompatible with a data item
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<Type>.Default.GetHashCode(Template.GetType());
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<string>.Default.GetHashCode(Template.Id);
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<IReadOnlyList<string>>.Default.GetHashCode(
                    Template.RelativePath ?? Array.Empty<string>());
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<string>.Default.GetHashCode(Template.StartNodeId);
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<string>.Default.GetHashCode(Template.IndexRange ?? string.Empty);
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<NodeAttribute>.Default.GetHashCode(
                    Template.AttributeId ?? NodeAttribute.NodeId);
            return hashCode;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"Item '{Template.StartNodeId}' with server id {ServerId} - " +
                $"{(Item?.Status?.Created == true ? "" : "not ")}created";
        }

        /// <summary>
        /// Create new stack monitored item
        /// </summary>
        /// <param name="session"></param>
        /// <param name="codec"></param>
        public void Create(ISession session, IVariantEncoder codec)
        {
            Create(session.MessageContext, session.NodeCache, session.TypeTree, codec);
        }

        /// <summary>
        /// Destructor for this class
        /// </summary>
        public void Dispose()
        {
            if (Item != null)
            {
                Item.Handle = null;
            }
            var conditionTimer = _conditionTimer;
            _conditionTimer = null;
            if (conditionTimer != null)
            {
                conditionTimer.Stop();
                conditionTimer.Dispose();
            }
        }

        /// <summary>
        /// Get metadata
        /// </summary>
        /// <param name="messageContext"></param>
        /// <param name="nodeCache"></param>
        /// <param name="typeTree"></param>
        /// <param name="typeSystem"></param>
        /// <param name="fields"></param>
        /// <param name="dataTypes"></param>
        /// <returns></returns>
        public void GetMetaData(IServiceMessageContext messageContext, INodeCache nodeCache, ITypeTable typeTree,
            ComplexTypeSystem? typeSystem, FieldMetaDataCollection fields, NodeIdDictionary<DataTypeDescription> dataTypes)
        {
            Debug.Assert(Item != null);
            try
            {
                if (Item.Filter is EventFilter eventFilter)
                {
                    GetEventMetadata(eventFilter, nodeCache, typeTree, typeSystem, fields, dataTypes);
                }
                else
                {
                    GetDataMetadata(messageContext, nodeCache, typeTree, typeSystem, fields, dataTypes);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "{Item}: Failed to get metadata.", this);
            }
        }

        /// <summary>
        /// Create new stack monitored item
        /// </summary>
        /// <param name="messageContext"></param>
        /// <param name="nodeCache"></param>
        /// <param name="typeTree"></param>
        /// <param name="codec"></param>
        public void Create(IServiceMessageContext messageContext, INodeCache nodeCache, ITypeTable typeTree,
            IVariantEncoder codec)
        {
            Item = new MonitoredItem
            {
                Handle = this,
                DisplayName = Template.DisplayName ?? Template.Id,
                AttributeId = (uint)(Template.AttributeId ?? (NodeAttribute)Attributes.Value),
                IndexRange = Template.IndexRange,
                RelativePath = Template.RelativePath?.ToRelativePath(messageContext)?.Format(typeTree),
                MonitoringMode = Template.MonitoringMode.ToStackType() ?? Opc.Ua.MonitoringMode.Reporting,
                StartNodeId = Template.StartNodeId.ToNodeId(messageContext),
                QueueSize = Template.QueueSize,
                SamplingInterval = (int)Template.SamplingInterval.
                    GetValueOrDefault(TimeSpan.FromSeconds(1)).TotalMilliseconds,
                DiscardOldest = !(Template.DiscardNew ?? false)
            };

            // Set filter
            if (DataTemplate != null)
            {
                Item.Filter = DataTemplate.DataChangeFilter.ToStackModel() ??
                    (MonitoringFilter?)DataTemplate.AggregateFilter.ToStackModel(messageContext);
                if (!TrySetSkipFirst(DataTemplate.SkipFirst))
                {
                    Debug.Fail("Unexpected: Failed to set skip first setting.");
                }
            }
            else if (EventTemplate != null)
            {
                Item.Filter = GetEventFilter(messageContext, nodeCache, typeTree, codec);
            }
            else
            {
                Debug.Fail($"Unexpected: Unknown type {Template.GetType()}");
            }
        }

        /// <summary>
        /// Merge with desired state
        /// </summary>
        /// <param name="messageContext"></param>
        /// <param name="nodeCache"></param>
        /// <param name="typeTree"></param>
        /// <param name="codec"></param>
        /// <param name="model"></param>
        /// <param name="metadataChange"></param>
        /// <returns>Whether apply changes should be called on the subscription</returns>
        internal bool MergeWith(IServiceMessageContext messageContext, INodeCache nodeCache, ITypeTable typeTree,
            IVariantEncoder codec, OpcUaMonitoredItem? model, out bool metadataChange)
        {
            metadataChange = false;
            if (model == null || Item == null)
            {
                return false;
            }

            var itemChange = false;
            if ((Template.SamplingInterval ?? TimeSpan.FromSeconds(1)) !=
                (model.Template.SamplingInterval ?? TimeSpan.FromSeconds(1)))
            {
                _logger.LogDebug("{Item}: Changing sampling interval from {Old} to {New}",
                    this, Template.SamplingInterval.GetValueOrDefault(
                        TimeSpan.FromSeconds(1)).TotalMilliseconds,
                    model.Template.SamplingInterval.GetValueOrDefault(
                        TimeSpan.FromSeconds(1)).TotalMilliseconds);
                Template = Template with { SamplingInterval = model.Template.SamplingInterval };
                Item.SamplingInterval =
                    (int)Template.SamplingInterval.GetValueOrDefault(TimeSpan.FromSeconds(1)).TotalMilliseconds;
                itemChange = true;
            }
            if ((Template.DiscardNew ?? false) !=
                    model.Template.DiscardNew.GetValueOrDefault())
            {
                _logger.LogDebug("{Item}: Changing discard new mode from {Old} to {New}",
                    this, Template.DiscardNew ?? false,
                    model.Template.DiscardNew ?? false);
                Template = Template with { DiscardNew = model.Template.DiscardNew };
                Item.DiscardOldest = !(Template.DiscardNew ?? false);
                itemChange = true;
            }
            if (Template.QueueSize != model.Template.QueueSize)
            {
                _logger.LogDebug("{Item}: Changing queue size from {Old} to {New}",
                    this, Template.QueueSize,
                    model.Template.QueueSize);
                Template = Template with { QueueSize = model.Template.QueueSize };
                Item.QueueSize = Template.QueueSize;
                itemChange = true;
            }
            if ((Template.MonitoringMode ?? MonitoringMode.Reporting) !=
                (model.Template.MonitoringMode ?? MonitoringMode.Reporting))
            {
                _logger.LogDebug("{Item}: Changing monitoring mode from {Old} to {New}",
                    this, Template.MonitoringMode ?? MonitoringMode.Reporting,
                    model.Template.MonitoringMode ?? MonitoringMode.Reporting);
                Template = Template with { MonitoringMode = model.Template.MonitoringMode };
                _modeChange = Template.MonitoringMode ?? MonitoringMode.Reporting;
            }

            if (Template.DisplayName != model.Template.DisplayName)
            {
                Template = Template with { DisplayName = model.Template.DisplayName };
                Item.DisplayName = Template.DisplayName;
                metadataChange = true;
                itemChange = true;
            }

            // Should never merge items with different template types
            Debug.Assert(model.Template.GetType() == Template.GetType());

            if (model.DataTemplate != null)
            {
                Debug.Assert(DataTemplate != null);
                if (DataTemplate.DataSetClassFieldId != model.DataTemplate.DataSetClassFieldId)
                {
                    var previous = DataSetFieldId;
                    Template = DataTemplate with { DataSetClassFieldId = model.DataTemplate.DataSetClassFieldId };
                    _logger.LogDebug("{Item}: Changing dataset class field id from {Old} to {New}",
                        this, previous, DataSetFieldId);
                    metadataChange = true;
                }

                // Update change filter
                if (!model.DataTemplate.DataChangeFilter.IsSameAs(DataTemplate.DataChangeFilter))
                {
                    Template = DataTemplate with { DataChangeFilter = model.DataTemplate.DataChangeFilter };
                    _logger.LogDebug("{Item}: Changing data change filter.", this);
                    Item.Filter = DataTemplate.DataChangeFilter.ToStackModel();
                    itemChange = true;
                }

                // Update AggregateFilter
                else if (!model.DataTemplate.AggregateFilter.IsSameAs(DataTemplate.AggregateFilter))
                {
                    Template = DataTemplate with { AggregateFilter = model.DataTemplate.AggregateFilter };
                    _logger.LogDebug("{Item}: Changing aggregate change filter.", this);
                    Item.Filter = DataTemplate.AggregateFilter.ToStackModel(messageContext);
                    itemChange = true;
                }

                if (model.DataTemplate.HeartbeatInterval != DataTemplate.HeartbeatInterval)
                {
                    _logger.LogDebug("{Item}: Changing heartbeat from {Old} to {New}",
                        this, DataTemplate.HeartbeatInterval, model.DataTemplate.HeartbeatInterval);
                    Template = DataTemplate with { HeartbeatInterval = model.DataTemplate.HeartbeatInterval };

                    itemChange = true; // TODO: Not really a change in the item
                }

                if (model.DataTemplate.SkipFirst != DataTemplate.SkipFirst)
                {
                    Template = DataTemplate with { SkipFirst = model.DataTemplate.SkipFirst };

                    if (model.TrySetSkipFirst(model.DataTemplate.SkipFirst))
                    {
                        _logger.LogDebug("{Item}: Setting skip first setting to {New}", this,
                            model.DataTemplate.SkipFirst);
                    }
                    else
                    {
                        _logger.LogInformation("{Item}: Tried to set SkipFirst but it was set" +
                            "previously or first value was already processed.", this);
                    }
                    // No change, just updated internal state
                }
            }
            else if (model.EventTemplate != null)
            {
                // Update event filter
                Debug.Assert(EventTemplate != null);
                if (!model.EventTemplate.EventFilter.IsSameAs(EventTemplate.EventFilter) ||
                    !model.EventTemplate.ConditionHandling.IsSameAs(EventTemplate.ConditionHandling))
                {
                    Template = EventTemplate with
                    {
                        ConditionHandling = model.EventTemplate.ConditionHandling,
                        EventFilter = model.EventTemplate.EventFilter
                    };
                    _logger.LogDebug("{Item}: Changing event filter.", this);

                    metadataChange = true;
                    itemChange = true;
                }

                if (metadataChange)
                {
                    Item.Filter = GetEventFilter(messageContext, nodeCache, typeTree, codec);
                }
            }
            else
            {
                Debug.Fail($"Unexpected: Unknown type {model.Template.GetType()}");
            }
            return itemChange;
        }

        /// <summary>
        /// Get any changes in the monitoring mode
        /// </summary>
        internal Opc.Ua.MonitoringMode? GetMonitoringModeChange()
        {
            var change = _modeChange.ToStackType();
            _modeChange = null;
            Debug.Assert(Item != null);
            return Item.MonitoringMode == change ? null : change;
        }

        /// <summary>
        /// Get event filter
        /// </summary>
        /// <param name="messageContext"></param>
        /// <param name="nodeCache"></param>
        /// <param name="typeTree"></param>
        /// <param name="codec"></param>
        /// <returns></returns>
        private EventFilter GetEventFilter(IServiceMessageContext messageContext, INodeCache nodeCache,
            ITypeTable typeTree, IVariantEncoder codec)
        {
            Debug.Assert(EventTemplate != null);

            // Save condition handling state and disable condition processing while we update
            bool conditionHandlingWasEnabled;
            lock (_lock)
            {
                conditionHandlingWasEnabled = _conditionHandlingState != null;
                _conditionHandlingState = null;

                if (_conditionTimer == null)
                {
                    _conditionTimer = new Timer(1000);
                    _conditionTimer.AutoReset = false;
                    _conditionTimer.Elapsed += OnConditionTimerElapsed;
                }
                else
                {
                    // Always stop in case we are asked to disable condition handling
                    _conditionTimer.Stop();
                    _logger.LogInformation("{Item}: Disabled pending condition handling.", this);
                }
            }

            // set up the timer even if event is not a pending alarms event.
            var eventFilter = new EventFilter();
            if (EventTemplate.EventFilter != null)
            {
                if (!string.IsNullOrEmpty(EventTemplate.EventFilter.TypeDefinitionId))
                {
                    eventFilter = GetSimpleEventFilter(nodeCache, typeTree, messageContext);
                }
                else
                {
                    eventFilter = codec.Decode(EventTemplate.EventFilter, true);
                }
            }

            TestWhereClause(messageContext, nodeCache, eventFilter);

            // let's keep track of the internal fields we add so that they don't show up in the output
            var internalSelectClauses = new List<SimpleAttributeOperand>();
            if (!eventFilter.SelectClauses.Any(x => x.TypeDefinitionId == ObjectTypeIds.BaseEventType
                && x.BrowsePath?.FirstOrDefault() == BrowseNames.EventType))
            {
                var selectClause = new SimpleAttributeOperand(ObjectTypeIds.BaseEventType, BrowseNames.EventType);
                eventFilter.SelectClauses.Add(selectClause);
                internalSelectClauses.Add(selectClause);
            }

            ConditionHandlingState? conditionHandlingState = null;
            if (!EventTemplate.ConditionHandling.IsDisabled())
            {
                conditionHandlingState = InitializeConditionHandlingState(eventFilter,
                    internalSelectClauses);
            }

            var sb = new StringBuilder();
            // let's loop thru the final set of select clauses and setup the field names used
            Fields.Clear();
            foreach (var selectClause in eventFilter.SelectClauses)
            {
                if (!internalSelectClauses.Any(x => x == selectClause))
                {
                    sb.Clear();
                    var definedSelectClause = EventTemplate.EventFilter?.SelectClauses?
                        .ElementAtOrDefault(eventFilter.SelectClauses.IndexOf(selectClause));
                    if (!string.IsNullOrEmpty(definedSelectClause?.DisplayName))
                    {
                        sb.Append(definedSelectClause.DisplayName);
                    }
                    else
                    {
                        for (var i = 0; i < selectClause.BrowsePath?.Count; i++)
                        {
                            if (i == 0)
                            {
                                if (selectClause.BrowsePath[i].NamespaceIndex != 0)
                                {
                                    if (selectClause.BrowsePath[i].NamespaceIndex < nodeCache.NamespaceUris.Count)
                                    {
                                        sb
                                            .Append(nodeCache.NamespaceUris.GetString(selectClause.BrowsePath[i].NamespaceIndex))
                                            .Append('#');
                                    }
                                    else
                                    {
                                        sb.Append(selectClause.BrowsePath[i].NamespaceIndex).Append(':');
                                    }
                                }
                            }
                            else
                            {
                                sb.Append('/');
                            }
                            sb.Append(selectClause.BrowsePath[i].Name);
                        }
                    }

                    if (sb.Length == 0 && selectClause.TypeDefinitionId == ObjectTypeIds.ConditionType &&
                            selectClause.AttributeId == Attributes.NodeId)
                    {
                        sb.Append("ConditionId");
                    }
                    Fields.Add((sb.ToString(), Guid.NewGuid()));
                }
                else
                {
                    // if a field's nameis empty, it's not written to the output
                    Fields.Add((null, Guid.Empty));
                }
            }
            Debug.Assert(Fields.Count == eventFilter.SelectClauses.Count);

            if (conditionHandlingState != null)
            {
                lock (_lock)
                {
                    _conditionHandlingState = conditionHandlingState;
                }
                _conditionTimer.Start();
                _logger.LogInformation("{Item}: {Action} condition handling.", this,
                    !conditionHandlingWasEnabled ? "Enabled" : "Re-enabled");
            }

            return eventFilter;
        }

        /// <summary>
        /// Initialize periodic pending condition handling state
        /// </summary>
        /// <param name="eventFilter"></param>
        /// <param name="internalSelectClauses"></param>
        /// <returns></returns>
        private static ConditionHandlingState InitializeConditionHandlingState(EventFilter eventFilter,
            List<SimpleAttributeOperand> internalSelectClauses)
        {
            var conditionHandlingState = new ConditionHandlingState();

            var conditionIdClause = eventFilter.SelectClauses
                .FirstOrDefault(x => x.TypeDefinitionId == ObjectTypeIds.ConditionType && x.AttributeId == Attributes.NodeId);
            if (conditionIdClause != null)
            {
                conditionHandlingState.ConditionIdIndex = eventFilter.SelectClauses.IndexOf(conditionIdClause);
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
                conditionHandlingState.RetainIndex = eventFilter.SelectClauses.IndexOf(retainClause);
            }
            else
            {
                conditionHandlingState.RetainIndex = eventFilter.SelectClauses.Count;
                var selectClause = new SimpleAttributeOperand(ObjectTypeIds.ConditionType, BrowseNames.Retain);
                eventFilter.SelectClauses.Add(selectClause);
                internalSelectClauses.Add(selectClause);
            }
            return conditionHandlingState;
        }

        /// <summary>
        /// Builds select clause and where clause by using OPC UA reflection
        /// </summary>
        /// <param name="nodeCache"></param>
        /// <param name="typeTree"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        private EventFilter GetSimpleEventFilter(INodeCache nodeCache, ITypeTable typeTree, IServiceMessageContext context)
        {
            Debug.Assert(EventTemplate != null);
            var typeDefinitionId = EventTemplate.EventFilter.TypeDefinitionId.ToNodeId(context);
            var nodes = new List<Node>();
            ExpandedNodeId? superType = null;
            nodes.Insert(0, nodeCache.FetchNode(typeDefinitionId));
            do
            {
                superType = nodes[0].GetSuperType(typeTree);
                if (superType != null)
                {
                    nodes.Insert(0, nodeCache.FetchNode(superType));
                }
            }
            while (superType != null);

            var fieldNames = new List<QualifiedName>();

            foreach (var node in nodes)
            {
                ParseFields(nodeCache, typeTree, fieldNames, node);
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
        /// Process monitored item notification
        /// </summary>
        /// <param name="message"></param>
        /// <param name="notification"></param>
        internal void ProcessMonitoredItemNotification(OpcUaNotification message,
            MonitoredItemNotification? notification)
        {
            Debug.Assert(Item != null);
            Debug.Assert(DataTemplate != null);

            var shouldHeartbeat = ValidateHeartbeat(message.Timestamp);
            if (notification == null)
            {
                if (shouldHeartbeat)
                {
                    var heartbeatValues = Item.LastValue.ToMonitoredItemNotifications(
                        Item, () => new MonitoredItemNotificationModel
                        {
                            DataSetFieldName = string.IsNullOrEmpty(Item.DisplayName) ?
                                Template.Id : Item.DisplayName,
                            Id = Template.Id,
                            DisplayName = Item.DisplayName,
                            NodeId = Template.StartNodeId,
                            AttributeId = Item.AttributeId,
                            Value = new DataValue(Item.Status?.Error?.StatusCode
                                ?? StatusCodes.BadMonitoredItemIdInvalid)
                        });
                    var notifications = new List<MonitoredItemNotificationModel>();
                    foreach (var heartbeat in heartbeatValues)
                    {
                        var heartbeatValue = heartbeat.Clone();
                        if (heartbeatValue != null)
                        {
                            heartbeatValue.SequenceNumber = 0;
                            heartbeatValue.IsHeartbeat = true;
                            notifications.Add(heartbeatValue);
                        }
                    }
                    message.Notifications.AddRange(notifications);
                    message.MessageType = Encoders.PubSub.MessageType.KeyFrame;
                }
            }
            else
            {
                message.Notifications.AddRange(notification
                    .ToMonitoredItemNotifications(Item));
            }

            bool ValidateHeartbeat(DateTime currentPublish)
            {
                if (DataTemplate == null)
                {
                    return false;
                }
                if (NextHeartbeat == DateTime.MaxValue)
                {
                    return false;
                }
                if (NextHeartbeat > currentPublish + TimeSpan.FromMilliseconds(50))
                {
                    return false;
                }
                var interval = DataTemplate.HeartbeatInterval ?? TimeSpan.Zero;
                NextHeartbeat = TimeSpan.Zero < interval ?
                    currentPublish + interval : DateTime.MaxValue;
                return true;
            }
        }

        /// <summary>
        /// Processing the monitored item notification
        /// </summary>
        /// <param name="message"></param>
        /// <param name="notification"></param>
        internal void ProcessEventNotification(IOpcUaSubscriptionNotification message,
            EventFieldList notification)
        {
            Debug.Assert(Item != null);
            Debug.Assert(EventTemplate != null);

            var evFilter = Item.Filter as EventFilter;
            var eventTypeIndex = evFilter?.SelectClauses.IndexOf(
                evFilter?.SelectClauses
                    .FirstOrDefault(x => x.TypeDefinitionId == ObjectTypeIds.BaseEventType
                        && x.BrowsePath?.FirstOrDefault() == BrowseNames.EventType));

            ConditionHandlingState? state;
            lock (_lock)
            {
                state = _conditionHandlingState;
            }

            // now, is this a regular event or RefreshStartEventType/RefreshEndEventType?
            if (eventTypeIndex.HasValue && eventTypeIndex.Value != -1)
            {
                var eventType = notification.EventFields[eventTypeIndex.Value].Value as NodeId;
                if (eventType == ObjectTypeIds.RefreshStartEventType)
                {
                    // stop the timers during condition refresh
                    if (state != null)
                    {
                        _conditionTimer?.Stop();
                        lock (_lock)
                        {
                            state.Active.Clear();
                        }
                        _logger.LogDebug("{Item}: Stopped pending alarm handling during condition refresh.", this);
                    }
                    return;
                }
                else if (eventType == ObjectTypeIds.RefreshEndEventType)
                {
                    if (state != null)
                    {
                        // restart the timers once condition refresh is done.
                        _conditionTimer?.Start();
                        _logger.LogDebug("{Item}: Restarted pending alarm handling after condition refresh.", this);
                    }
                    return;
                }
                else if (eventType == ObjectTypeIds.RefreshRequiredEventType)
                {
                    var noErrorFound = true;

                    // issue a condition refresh to make sure we are in a correct state
                    _logger.LogInformation("{Item}: Issuing ConditionRefresh for item {Name} on subscription " +
                        "{Subscription} due to receiving a RefreshRequired event", this,
                        Item.DisplayName ?? "", Item.Subscription.DisplayName);
                    try
                    {
                        Item.Subscription.ConditionRefresh();
                    }
                    catch (ServiceResultException e)
                    {
                        _logger.LogInformation("{Item}: ConditionRefresh for item {Name} on subscription " +
                            "{Subscription} failed with a ServiceResultException '{Message}'", this,
                            Item.DisplayName ?? "", Item.Subscription.DisplayName, e.Message);
                        noErrorFound = false;
                    }
                    catch (Exception e)
                    {
                        _logger.LogInformation("{Item}: ConditionRefresh for item {Name} on subscription " +
                            "{Subscription} failed with an exception '{Message}'", this,
                            Item.DisplayName ?? "", Item.Subscription.DisplayName, e.Message);
                        noErrorFound = false;
                    }
                    if (noErrorFound)
                    {
                        _logger.LogInformation("{Item}: ConditionRefresh for item {Name} on subscription " +
                            "{Subscription} has completed", this,
                            Item.DisplayName ?? "", Item.Subscription.DisplayName);
                    }
                    return;
                }
            }

            var monitoredItemNotifications = notification.ToMonitoredItemNotifications(Item).ToList();
            if (state != null)
            {
                var conditionIdIndex = state.ConditionIdIndex;
                var retainIndex = state.RetainIndex;
                if (conditionIdIndex < monitoredItemNotifications.Count &&
                    retainIndex < monitoredItemNotifications.Count)
                {
                    // Cache conditions
                    var conditionId = monitoredItemNotifications[conditionIdIndex].Value?.Value?.ToString();
                    if (conditionId != null)
                    {
                        var retain = monitoredItemNotifications[retainIndex].Value?.GetValue(false) ?? false;
                        lock (_lock)
                        {
                            if (state.Active.ContainsKey(conditionId) && !retain)
                            {
                                state.Active.Remove(conditionId, out _);
                                state.Dirty = true;
                            }
                            else if (retain && !monitoredItemNotifications.All(m => m.Value?.Value == null))
                            {
                                state.Dirty = true;
                                monitoredItemNotifications.ForEach(notification =>
                                {
                                    notification.Value ??= new DataValue(StatusCodes.BadNoData);
                                    // Set SourceTimestamp to publish time
                                    notification.Value.SourceTimestamp = message.Timestamp;
                                });
                                state.Active.AddOrUpdate(conditionId, monitoredItemNotifications);
                            }
                        }
                    }
                }
            }
            else
            {
                // Send notifications as event
                foreach (var n in monitoredItemNotifications.Where(n => n.DataSetFieldName != null))
                {
                    message.Notifications.Add(n);
                }
            }
        }

        private void TestWhereClause(IServiceMessageContext messageContext,
            INodeCache nodeCache, EventFilter eventFilter)
        {
            foreach (var element in eventFilter.WhereClause.Elements)
            {
                if (element.FilterOperator == FilterOperator.OfType)
                {
                    foreach (var filterOperand in element.FilterOperands)
                    {
                        var nodeId = default(NodeId);
                        try
                        {
                            nodeId = (filterOperand.Body as LiteralOperand)?.Value.ToString().ToNodeId(messageContext);
                            nodeCache.FetchNode(nodeId?.ToExpandedNodeId(messageContext.NamespaceUris)); // it will throw an exception if it doesn't work
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning("{Item}: Where clause is doing OfType({NodeId}) and we got this message {Message} while looking it up",
                                this, nodeId, ex.Message);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Called when the condition timer fires
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnConditionTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            Debug.Assert(EventTemplate != null);
            var now = DateTime.UtcNow;
            ConditionHandlingState? state;
            lock (_lock)
            {
                state = _conditionHandlingState;
            }
            if (state != null)
            {
                try
                {
                    if (Item?.Created != true)
                    {
                        return;
                    }
                    var options = EventTemplate.ConditionHandling;
                    var sendPendingConditions = false;

                    // is it time to send anything?
                    if (options?.SnapshotInterval != null)
                    {
                        sendPendingConditions = now >
                            _lastSentPendingConditions + TimeSpan.FromSeconds(options.SnapshotInterval.Value);
                    }
                    if (!sendPendingConditions && state.Dirty && options?.UpdateInterval != null)
                    {
                        sendPendingConditions = now >
                            _lastSentPendingConditions + TimeSpan.FromSeconds(options.UpdateInterval.Value);
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
                    _conditionTimer?.Start();
                }
            }
        }

        /// <summary>
        /// Send pending conditions
        /// </summary>
        private void SendPendingConditions()
        {
            List<List<MonitoredItemNotificationModel>>? notifications = null;
            lock (_lock)
            {
                if (_conditionHandlingState == null)
                {
                    return;
                }
                notifications = _conditionHandlingState.Active
                    .Select(entry => entry.Value
                        .Where(n => n.DataSetFieldName != null)
                        .Select(n => n.Clone()!)
                        .Where(n => n != null)
                        .ToList())
                    .ToList();

                _conditionHandlingState.Dirty = false;
            }
            if (Item?.Subscription?.Handle is OpcUaSubscription subscription)
            {
                foreach (var conditionNotification in notifications)
                {
                    subscription.SendConditionNotification(Item.Subscription, conditionNotification);
                }
            }
        }

        /// <summary>
        /// Update data field metadata
        /// </summary>
        /// <param name="messageContext"></param>
        /// <param name="nodeCache"></param>
        /// <param name="typeTree"></param>
        /// <param name="typeSystem"></param>
        /// <param name="fields"></param>
        /// <param name="dataTypes"></param>
        private void GetDataMetadata(IServiceMessageContext messageContext, INodeCache nodeCache, ITypeTable typeTree,
            ComplexTypeSystem? typeSystem, FieldMetaDataCollection fields, NodeIdDictionary<DataTypeDescription> dataTypes)
        {
            var nodeId = Template.StartNodeId.ToExpandedNodeId(messageContext);
            try
            {
                if (nodeCache.FetchNode(nodeId) is VariableNode variable)
                {
                    AddVariableField(fields, dataTypes, nodeCache, typeTree, typeSystem, variable,
                        string.IsNullOrEmpty(Item?.DisplayName) ? Template.Id : Item.DisplayName, (Uuid)DataSetFieldId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("{Item}: Failed to get meta data for field {Field} with node {NodeId}: {Message}",
                    this, string.IsNullOrEmpty(Item?.DisplayName) ? Template.Id : Item?.DisplayName, nodeId, ex.Message);
            }
        }

        /// <summary>
        /// Add veriable field metadata
        /// </summary>
        /// <param name="fields"></param>
        /// <param name="dataTypes"></param>
        /// <param name="nodeCache"></param>
        /// <param name="typeTree"></param>
        /// <param name="typeSystem"></param>
        /// <param name="variable"></param>
        /// <param name="fieldName"></param>
        /// <param name="dataSetClassFieldId"></param>
        private void AddVariableField(FieldMetaDataCollection fields,
            NodeIdDictionary<DataTypeDescription> dataTypes, INodeCache nodeCache, ITypeTable typeTree,
            ComplexTypeSystem? typeSystem, VariableNode variable, string fieldName, Uuid dataSetClassFieldId)
        {
            var builtInType = TypeInfo.GetBuiltInType(variable.DataType, nodeCache.TypeTree);
            fields.Add(new FieldMetaData
            {
                Name = fieldName,
                DataSetFieldId = dataSetClassFieldId,
                FieldFlags = 0, // Set to 1 << 1 for PromotedField fields.
                DataType = variable.DataType,
                ArrayDimensions = variable.ArrayDimensions?.Count > 0 ? variable.ArrayDimensions : null,
                Description = variable.Description,
                ValueRank = variable.ValueRank,
                MaxStringLength = 0,
                // If the Property is EngineeringUnits, the unit of the Field Value shall match the unit of the FieldMetaData.
                Properties = null, // TODO: Add engineering units etc. to properties
                BuiltInType = (byte)builtInType
            });
            AddDataTypes(dataTypes, variable.DataType, nodeCache, typeTree, typeSystem);
        }

        /// <summary>
        /// Get event metadata.
        /// </summary>
        /// <param name="eventFilter"></param>
        /// <param name="nodeCache"></param>
        /// <param name="typeTree"></param>
        /// <param name="typeSystem"></param>
        /// <param name="fields"></param>
        /// <param name="dataTypes"></param>
        /// <returns></returns>
        private void GetEventMetadata(EventFilter eventFilter, INodeCache nodeCache, ITypeTable typeTree,
            ComplexTypeSystem? typeSystem, FieldMetaDataCollection fields, NodeIdDictionary<DataTypeDescription> dataTypes)
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
                var targetNode = FindNodeWithBrowsePath(nodeCache, typeTree, selectClause.BrowsePath, selectClause.TypeDefinitionId);
                if (targetNode is VariableNode variable)
                {
                    AddVariableField(fields, dataTypes, nodeCache, typeTree, typeSystem, variable, fieldName, dataSetClassFieldId);
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

        private static INode? FindNodeWithBrowsePath(INodeCache nodeCache, ITypeTable typeTree,
            QualifiedNameCollection browsePath, ExpandedNodeId nodeId)
        {
            INode? found = null;
            foreach (var browseName in browsePath)
            {
                found = null;
                while (found == null)
                {
                    found = nodeCache.Find(nodeId);
                    if (found is not Node node)
                    {
                        return null;
                    }

                    // Get all hierarchical references of the node and match browse name
                    foreach (var reference in node.ReferenceTable.Find(ReferenceTypeIds.HierarchicalReferences, false, true, typeTree))
                    {
                        var target = nodeCache.Find(reference.TargetId);
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
                        nodeId = typeTree.FindSuperType(nodeId);
                        if (NodeId.IsNull(nodeId))
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
        /// Add data types to the metadata
        /// </summary>
        /// <param name="dataTypes"></param>
        /// <param name="dataTypeId"></param>
        /// <param name="nodeCache"></param>
        /// <param name="typeTree"></param>
        /// <param name="typeSystem"></param>
        private void AddDataTypes(NodeIdDictionary<DataTypeDescription> dataTypes, NodeId dataTypeId,
            INodeCache nodeCache, ITypeTable typeTree, ComplexTypeSystem? typeSystem)
        {
            var baseType = dataTypeId;
            while (!NodeId.IsNull(baseType))
            {
                try
                {
                    var dataType = nodeCache.FetchNode(baseType);
                    if (dataType == null)
                    {
                        _logger.LogWarning("{Item}: Failed to find node for data type {BaseType}!", this, baseType);
                        break;
                    }

                    dataTypeId = dataType.NodeId;
                    Debug.Assert(!NodeId.IsNull(dataTypeId));
                    if (dataTypeId.NamespaceIndex == 0 && dataTypeId.IdType == IdType.Numeric)
                    {
                        var id = (BuiltInType)(int)(uint)dataTypeId.Identifier;
                        if (id >= BuiltInType.Null && id <= BuiltInType.Enumeration)
                        {
                            // Do not add builtin types
                            break;
                        }
                    }

                    var builtInType = TypeInfo.GetBuiltInType(dataTypeId, typeTree);
                    baseType = typeTree.FindSuperType(dataTypeId);

                    switch (builtInType)
                    {
                        case BuiltInType.Enumeration:
                        case BuiltInType.ExtensionObject:
                            var types = typeSystem?.GetDataTypeDefinitionsForDataType(dataType.NodeId);
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
                catch (Exception ex)
                {
                    _logger.LogWarning("{Item}: Failed to get meta data for type {DataType} (base: {BaseType}) with message: {Message}",
                        this, dataTypeId, baseType, ex.Message);
                }
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
        /// Get all the fields of a type definition node to build the select clause.
        /// </summary>
        /// <param name="nodeCache"></param>
        /// <param name="typeTree"></param>
        /// <param name="fieldNames"></param>
        /// <param name="node"></param>
        /// <param name="browsePathPrefix"></param>
        private void ParseFields(INodeCache nodeCache, ITypeTable typeTree, List<QualifiedName> fieldNames,
            Node node, string browsePathPrefix = "")
        {
            foreach (var reference in node.ReferenceTable)
            {
                if (reference.ReferenceTypeId == ReferenceTypeIds.HasComponent && !reference.IsInverse)
                {
                    var componentNode = nodeCache.FetchNode(reference.TargetId);
                    if (componentNode.NodeClass == Opc.Ua.NodeClass.Variable)
                    {
                        var fieldName = $"{browsePathPrefix}{componentNode.BrowseName.Name}";
                        fieldNames.Add(new QualifiedName(fieldName, componentNode.BrowseName.NamespaceIndex));
                        ParseFields(nodeCache, typeTree, fieldNames, componentNode, $"{fieldName}|");
                    }
                }
                else if (reference.ReferenceTypeId == ReferenceTypeIds.HasProperty)
                {
                    var propertyNode = nodeCache.FetchNode(reference.TargetId);
                    var fieldName = $"{browsePathPrefix}{propertyNode.BrowseName.Name}";
                    fieldNames.Add(new QualifiedName(fieldName, propertyNode.BrowseName.NamespaceIndex));
                }
            }
        }

        /// <summary>
        /// Whether to skip monitored item notification
        /// </summary>
        /// <returns></returns>
        public bool SkipMonitoredItemNotification()
        {
            // This will update that first value has been processed.
            var last = Interlocked.Exchange(ref _skipDataChangeNotification, (int)SkipSetting.DontSkip);
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
                    (int)SkipSetting.Skip, (int)SkipSetting.Unconfigured) == (int)SkipSetting.Unconfigured;
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

        private ConditionHandlingState? _conditionHandlingState;
        private volatile int _skipDataChangeNotification = (int)SkipSetting.Unconfigured;
        private Timer? _conditionTimer;
        private DateTime _lastSentPendingConditions = DateTime.UtcNow;
        private MonitoringMode? _modeChange;
        private readonly ILogger _logger;
        private readonly object _lock = new();
    }
}
