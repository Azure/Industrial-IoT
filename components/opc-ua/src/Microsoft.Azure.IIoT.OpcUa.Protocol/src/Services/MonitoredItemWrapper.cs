// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Services {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Models;
    using Opc.Ua;
    using Opc.Ua.Client;
    using Opc.Ua.Client.ComplexTypes;
    using Opc.Ua.Extensions;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using Timer = System.Timers.Timer;

    /// <summary>
    /// Monitored item
    /// </summary>
    public class MonitoredItemWrapper {

        /// <summary>
        /// Assigned monitored item id on server
        /// </summary>
        public uint? ServerId => Item?.Status.Id;

        /// <summary>
        /// Monitored item
        /// </summary>
        public BaseMonitoredItemModel Template { get; }

        /// <summary>
        /// Monitored item as data
        /// </summary>
        public DataMonitoredItemModel DataTemplate { get { return Template as DataMonitoredItemModel; } }

        /// <summary>
        /// Monitored item as event
        /// </summary>
        public EventMonitoredItemModel EventTemplate { get { return Template as EventMonitoredItemModel; } }

        /// <summary>
        /// Monitored item created from template
        /// </summary>
        public MonitoredItem Item { get; private set; }

        /// <summary>
        /// Last published time
        /// </summary>
        public DateTime NextHeartbeat { get; private set; }

        /// <summary>
        /// List of field names. Only used for event filter items
        /// </summary>
        public List<(string Name, Guid DataSetFieldId)> Fields { get; } = new List<(string, Guid)>();

        /// <summary>
        /// Field identifier either configured or randomly assigned for data change items
        /// </summary>
        public Guid DataSetFieldId => DataTemplate.DataSetClassFieldId == Guid.Empty ? _fieldId
            : DataTemplate.DataSetClassFieldId;
        private Guid _fieldId = Guid.NewGuid();

        /// <summary>
        /// Property setter that gets indication if item is online or not.
        /// </summary>
        public void OnMonitoredItemStateChanged(bool online) {
            if (_conditionTimer != null) {
                var enabled = _conditionTimer.Enabled;
                if (online && !enabled) {
                    lock (_lock) {
                        if (_conditionHandlingState == null) {
                            return;
                        }
                    }
                    _conditionTimer.Start();
                    _logger.Debug("{item}: Restarted pending condition handling after item went online.", this);
                }
                else if (enabled) {
                    _conditionTimer.Stop();
                    lock (_lock) {
                        _conditionHandlingState?.Active.Clear();
                    }
                    if (!online) {
                        _logger.Debug("{item}: Stopped pending condition handling while item is offline.", this);
                    }
                }
            }
        }

        /// <summary>
        /// Create wrapper
        /// </summary>
        public MonitoredItemWrapper(BaseMonitoredItemModel template, ILogger logger) {
            _logger = logger?.ForContext<MonitoredItemWrapper>() ??
                throw new ArgumentNullException(nameof(logger));
            Template = template?.Clone() ??
                throw new ArgumentNullException(nameof(template));
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) {
            if (!(obj is MonitoredItemWrapper wrapper)) {
                return false;
            }
            if (Template.GetType() != wrapper.Template.GetType()) {
                // Event item is incompatible with a data item
                return false;
            }
            if (Template.Id != wrapper.Template.Id) {
                return false;
            }
            if (!Template.RelativePath.SequenceEqualsSafe(wrapper.Template.RelativePath)) {
                return false;
            }
            if (Template.StartNodeId != wrapper.Template.StartNodeId) {
                return false;
            }
            if (Template.IndexRange != wrapper.Template.IndexRange) {
                return false;
            }
            if (Template.AttributeId != wrapper.Template.AttributeId) {
                return false;
            }
            return true;
        }

        /// <inheritdoc/>
        public override int GetHashCode() {
            var hashCode = 1301977042;
            // Event item is incompatible with a data item
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<Type>.Default.GetHashCode(Template.GetType());
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<string>.Default.GetHashCode(Template.Id);
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<string[]>.Default.GetHashCode(Template.RelativePath);
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<string>.Default.GetHashCode(Template.StartNodeId);
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<string>.Default.GetHashCode(Template.IndexRange);
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<NodeAttribute?>.Default.GetHashCode(Template.AttributeId);
            return hashCode;
        }

        /// <inheritdoc/>
        public override string ToString() {
            return $"Item '{Template.StartNodeId}' with server id {ServerId} - " +
                $"{(Item?.Status?.Created == true ? "" : "not ")}created";
        }

        /// <summary>
        /// Create new stack monitored item
        /// </summary>
        public void Create(ISession session, IVariantEncoder codec, bool activate) {
            Create(session.MessageContext as ServiceMessageContext, session.NodeCache, session.TypeTree, codec, activate);
        }

        /// <summary>
        /// Destructor for this class
        /// </summary>
        public void Destroy() {
            Item.Handle = null;
            if (_conditionTimer != null) {
                _conditionTimer.Stop();
                _conditionTimer.Dispose();
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
            ComplexTypeSystem typeSystem, FieldMetaDataCollection fields, NodeIdDictionary<DataTypeDescription> dataTypes) {
            Debug.Assert(Item != null);
            try {
                if (Item.Filter is EventFilter eventFilter) {
                    GetEventMetadata(eventFilter, nodeCache, typeTree, typeSystem, fields, dataTypes);
                }
                else {
                    GetDataMetadata(messageContext, nodeCache, typeTree, typeSystem, fields, dataTypes);
                }
            }
            catch (Exception e) {
                _logger.Error(e, "{item}: Failed to get metadata.", this);
            }
        }

        /// <summary>
        /// Create new stack monitored item
        /// </summary>
        public void Create(ServiceMessageContext messageContext, INodeCache nodeCache, ITypeTable typeTree,
            IVariantEncoder codec, bool activate) {

            Item = new MonitoredItem {
                Handle = this,
                DisplayName = Template.DisplayName ?? Template.Id,
                AttributeId = (uint)Template.AttributeId.GetValueOrDefault((NodeAttribute)Attributes.Value),
                IndexRange = Template.IndexRange,
                RelativePath = Template.RelativePath?
                            .ToRelativePath(messageContext)?
                            .Format(typeTree),
                MonitoringMode = activate
                    ? Template.MonitoringMode.ToStackType().
                        GetValueOrDefault(Opc.Ua.MonitoringMode.Reporting)
                    : Opc.Ua.MonitoringMode.Disabled,
                StartNodeId = Template.StartNodeId.ToNodeId(messageContext),
                QueueSize = Template.QueueSize,
                SamplingInterval = (int)Template.SamplingInterval.
                    GetValueOrDefault(TimeSpan.FromSeconds(1)).TotalMilliseconds,
                DiscardOldest = !Template.DiscardNew.GetValueOrDefault(false),
            };

            // Set filter
            if (DataTemplate != null) {
                Item.Filter = DataTemplate.DataChangeFilter.ToStackModel() ??
                    ((MonitoringFilter)DataTemplate.AggregateFilter.ToStackModel(messageContext));
                if (!TrySetSkipFirst(DataTemplate.SkipFirst)) {
                    Debug.Fail($"Unexpected: Failed to set skip first setting.");
                }
            }
            else if (EventTemplate != null) {
                var eventFilter = GetEventFilter(messageContext, nodeCache, typeTree, codec);
                Item.Filter = eventFilter;
            }
            else {
                Debug.Fail($"Unexpected: Unknown type {Template.GetType()}");
            }
        }

        /// <summary>
        /// Add the monitored item identifier of the triggering item.
        /// </summary>
        internal void AddTriggerLink(uint? id) {
            if (id != null) {
                _newTriggers.Add(id.Value);
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
            IVariantEncoder codec, MonitoredItemWrapper model, out bool metadataChange) {

            metadataChange = false;
            if (model == null || Item == null) {
                return false;
            }

            var itemChange = false;
            if (Template.SamplingInterval.GetValueOrDefault(TimeSpan.FromSeconds(1)) !=
                model.Template.SamplingInterval.GetValueOrDefault(TimeSpan.FromSeconds(1))) {
                _logger.Debug("{item}: Changing sampling interval from {old} to {new}",
                    this, Template.SamplingInterval.GetValueOrDefault(
                        TimeSpan.FromSeconds(1)).TotalMilliseconds,
                    model.Template.SamplingInterval.GetValueOrDefault(
                        TimeSpan.FromSeconds(1)).TotalMilliseconds);
                Template.SamplingInterval = model.Template.SamplingInterval;
                Item.SamplingInterval =
                    (int)Template.SamplingInterval.GetValueOrDefault(TimeSpan.FromSeconds(1)).TotalMilliseconds;
                itemChange = true;
            }
            if (Template.DiscardNew.GetValueOrDefault(false) !=
                    model.Template.DiscardNew.GetValueOrDefault()) {
                _logger.Debug("{item}: Changing discard new mode from {old} to {new}",
                    this, Template.DiscardNew.GetValueOrDefault(false),
                    model.Template.DiscardNew.GetValueOrDefault(false));
                Template.DiscardNew = model.Template.DiscardNew;
                Item.DiscardOldest = !Template.DiscardNew.GetValueOrDefault(false);
                itemChange = true;
            }
            if (Template.QueueSize != model.Template.QueueSize) {
                _logger.Debug("{item}: Changing queue size from {old} to {new}",
                    this, Template.QueueSize,
                    model.Template.QueueSize);
                Template.QueueSize = model.Template.QueueSize;
                Item.QueueSize = Template.QueueSize;
                itemChange = true;
            }
            if (Template.MonitoringMode.GetValueOrDefault(Publisher.Models.MonitoringMode.Reporting) !=
                model.Template.MonitoringMode.GetValueOrDefault(Publisher.Models.MonitoringMode.Reporting)) {
                _logger.Debug("{item}: Changing monitoring mode from {old} to {new}",
                    this, Template.MonitoringMode.GetValueOrDefault(Publisher.Models.MonitoringMode.Reporting),
                    model.Template.MonitoringMode.GetValueOrDefault(Publisher.Models.MonitoringMode.Reporting));
                Template.MonitoringMode = model.Template.MonitoringMode;
                _modeChange = Template.MonitoringMode.GetValueOrDefault(Publisher.Models.MonitoringMode.Reporting);
            }

            if (Template.DisplayName != model.Template.DisplayName) {
                Template.DisplayName = model.Template.DisplayName;
                Item.DisplayName = Template.DisplayName;
                metadataChange = true;
                itemChange = true;
            }

            // Should never merge items with different template types
            Debug.Assert(model.Template.GetType() == Template.GetType());

            if (model.DataTemplate != null) {

                if (DataTemplate.DataSetClassFieldId != model.DataTemplate.DataSetClassFieldId) {
                    var previous = DataSetFieldId;
                    DataTemplate.DataSetClassFieldId = model.DataTemplate.DataSetClassFieldId;
                    _logger.Debug("{item}: Changing dataset class field id from {old} to {new}",
                        this, previous, DataSetFieldId);
                    metadataChange = true;
                }

                // Update change filter
                if (!model.DataTemplate.DataChangeFilter.IsSameAs(DataTemplate.DataChangeFilter)) {
                    DataTemplate.DataChangeFilter = model.DataTemplate.DataChangeFilter;
                    _logger.Debug("{item}: Changing data change filter.");
                    Item.Filter = DataTemplate.DataChangeFilter.ToStackModel();
                    itemChange = true;
                }

                // Update AggregateFilter
                else if (!model.DataTemplate.AggregateFilter.IsSameAs(DataTemplate.AggregateFilter)) {
                    DataTemplate.AggregateFilter = model.DataTemplate.AggregateFilter;
                    _logger.Debug("{item}: Changing aggregate change filter.");
                    Item.Filter = DataTemplate.AggregateFilter.ToStackModel(messageContext);
                    itemChange = true;
                }

                if (model.DataTemplate.HeartbeatInterval != DataTemplate.HeartbeatInterval) {
                    _logger.Debug("{item}: Changing heartbeat from {old} to {new}",
                        this, DataTemplate.HeartbeatInterval, model.DataTemplate.HeartbeatInterval);
                    DataTemplate.HeartbeatInterval = model.DataTemplate.HeartbeatInterval;

                    itemChange = true; // TODO: Not really a change in the item
                }

                if (model.DataTemplate.SkipFirst != DataTemplate.SkipFirst) {
                    DataTemplate.SkipFirst = model.DataTemplate.SkipFirst;

                    if (model.TrySetSkipFirst(model.DataTemplate.SkipFirst)) {
                        _logger.Debug("{item}: Setting skip first setting to {new}", this,
                            model.DataTemplate.SkipFirst);
                    }
                    else {
                        _logger.Information("{item}: Tried to set SkipFirst but it was set" +
                            "previously or first value was already processed.", this,
                            model.DataTemplate.SkipFirst);
                    }
                    // No change, just updated internal state
                }
            }
            else if (model.EventTemplate != null) {

                // Update event filter
                if (!model.EventTemplate.EventFilter.IsSameAs(EventTemplate.EventFilter) ||
                    !model.EventTemplate.ConditionHandling.IsSameAs(EventTemplate.ConditionHandling)) {

                    EventTemplate.ConditionHandling = model.EventTemplate.ConditionHandling;
                    EventTemplate.EventFilter = model.EventTemplate.EventFilter;
                    _logger.Debug("{item}: Changing event filter.");

                    metadataChange = true;
                    itemChange = true;
                }

                if (metadataChange) {
                    Item.Filter = GetEventFilter(messageContext, nodeCache, typeTree, codec);
                }
            }
            else {
                Debug.Fail($"Unexpected: Unknown type {model.Template.GetType()}");
            }
            return itemChange;
        }

        /// <summary>
        /// Get triggering configuration changes for this item
        /// </summary>
        internal bool GetTriggeringLinks(out IEnumerable<uint> addLinks,
            out IEnumerable<uint> removeLinks) {
            var remove = _triggers.Except(_newTriggers).ToList();
            var add = _newTriggers.Except(_triggers).ToList();
            _triggers = _newTriggers;
            _newTriggers = new HashSet<uint>();
            addLinks = add;
            removeLinks = remove;
            if (add.Count > 0 || remove.Count > 0) {
                _logger.Debug("{item}: Adding {add} triggering links and removing {remove} triggering links",
                    this,
                    add.Count,
                    remove.Count);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Get any changes in the monitoring mode
        /// </summary>
        internal MonitoringMode? GetMonitoringModeChange() {
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
            ITypeTable typeTree, IVariantEncoder codec) {

            // set up the timer even if event is not a pending alarms event.
            var created = false;
            if (_conditionTimer == null) {
                _conditionTimer = new Timer(1000);
                _conditionTimer.AutoReset = false;
                _conditionTimer.Elapsed += OnConditionTimerElapsed;
                created = true;
            }

            if (!created && EventTemplate.ConditionHandling.IsDisabled()) {
                // Always stop in case we are asked to disable condition handling
                _conditionTimer.Stop();
                lock (_lock) {
                    _conditionHandlingState = null;
                }
                _logger.Information("{item}: Disabled pending alarm handling.", this);
            }

            var eventFilter = new EventFilter();
            if (EventTemplate.EventFilter != null) {
                if (!string.IsNullOrEmpty(EventTemplate.EventFilter.TypeDefinitionId)) {
                    eventFilter = GetSimpleEventFilter(nodeCache, typeTree, messageContext);
                }
                else {
                    eventFilter = codec.Decode(EventTemplate.EventFilter, true);
                }
            }

            TestWhereClause(messageContext, nodeCache, typeTree, eventFilter);

            // let's keep track of the internal fields we add so that they don't show up in the output
            var internalSelectClauses = new List<SimpleAttributeOperand>();
            if (!eventFilter.SelectClauses.Any(x => x.TypeDefinitionId == ObjectTypeIds.BaseEventType
                && x.BrowsePath?.FirstOrDefault() == BrowseNames.EventType)) {
                var selectClause = new SimpleAttributeOperand(ObjectTypeIds.BaseEventType, BrowseNames.EventType);
                eventFilter.SelectClauses.Add(selectClause);
                internalSelectClauses.Add(selectClause);
            }

            if (!EventTemplate.ConditionHandling.IsDisabled()) {
                var conditionHandlingState = InitializeConditionHandlingState(eventFilter, internalSelectClauses);
                lock (_lock) {
                    _conditionHandlingState = conditionHandlingState;
                }
                _conditionTimer.Start();
                _logger.Information("{item}: {Action} pending alarm handling.", this, created ? "Enabled" : "Re-enabled");
            }

            var sb = new StringBuilder();

            // let's loop thru the final set of select clauses and setup the field names used
            foreach (var selectClause in eventFilter.SelectClauses) {
                if (!internalSelectClauses.Any(x => x == selectClause)) {
                    sb.Clear();
                    for (var i = 0; i < selectClause.BrowsePath?.Count; i++) {
                        if (i == 0) {
                            if (selectClause.BrowsePath[i].NamespaceIndex != 0) {
                                if (selectClause.BrowsePath[i].NamespaceIndex < nodeCache.NamespaceUris.Count) {
                                    sb.Append(nodeCache.NamespaceUris.GetString(selectClause.BrowsePath[i].NamespaceIndex));
                                    sb.Append('#');
                                }
                                else {
                                    sb.Append($"{selectClause.BrowsePath[i].NamespaceIndex}:");
                                }
                            }
                        }
                        else {
                            sb.Append('/');
                        }
                        sb.Append(selectClause.BrowsePath[i].Name);
                    }

                    if (sb.Length == 0) {
                        if (selectClause.TypeDefinitionId == ObjectTypeIds.ConditionType &&
                            selectClause.AttributeId == Attributes.NodeId) {
                            sb.Append("ConditionId");
                        }
                    }
                    Fields.Add((sb.ToString(), Guid.NewGuid()));
                }
                else {
                    // if a field's nameis empty, it's not written to the output
                    Fields.Add((null, Guid.Empty));
                }
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
            List<SimpleAttributeOperand> internalSelectClauses) {
            var conditionHandlingState = new ConditionHandlingState();

            var conditionIdClause = eventFilter.SelectClauses
                .FirstOrDefault(x => x.TypeDefinitionId == ObjectTypeIds.ConditionType && x.AttributeId == Attributes.NodeId);
            if (conditionIdClause != null) {
                conditionHandlingState.ConditionIdIndex = eventFilter.SelectClauses.IndexOf(conditionIdClause);
            }
            else {
                conditionHandlingState.ConditionIdIndex = eventFilter.SelectClauses.Count;
                var selectClause = new SimpleAttributeOperand() {
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
            if (retainClause != null) {
                conditionHandlingState.RetainIndex = eventFilter.SelectClauses.IndexOf(retainClause);
            }
            else {
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
        private EventFilter GetSimpleEventFilter(INodeCache nodeCache, ITypeTable typeTree, IServiceMessageContext context) {
            var typeDefinitionId = EventTemplate.EventFilter.TypeDefinitionId.ToNodeId(context);
            var nodes = new List<Node>();
            ExpandedNodeId superType = null;
            nodes.Insert(0, nodeCache.FetchNode(typeDefinitionId));
            do {
                superType = nodes[0].GetSuperType(typeTree);
                if (superType != null) {
                    nodes.Insert(0, nodeCache.FetchNode(superType));
                }
            }
            while (superType != null);

            var fieldNames = new List<QualifiedName>();

            foreach (var node in nodes) {
                ParseFields(nodeCache, typeTree, fieldNames, node);
            }
            fieldNames = fieldNames
                .Distinct()
                .OrderBy(x => x.Name).ToList();

            var eventFilter = new EventFilter();
            // Let's add ConditionId manually first if event is derived from ConditionType
            if (nodes.Any(x => x.NodeId == ObjectTypeIds.ConditionType)) {
                eventFilter.SelectClauses.Add(new SimpleAttributeOperand() {
                    BrowsePath = new QualifiedNameCollection(),
                    TypeDefinitionId = ObjectTypeIds.ConditionType,
                    AttributeId = Attributes.NodeId
                });
            }

            foreach (var fieldName in fieldNames) {
                var selectClause = new SimpleAttributeOperand() {
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
        public void ProcessMonitoredItemNotification(SubscriptionNotificationModel message,
            MonitoredItemNotification notification) {

            Debug.Assert(Item != null);
            Debug.Assert(Template != null);
            var shouldHeartbeat = ValidateHeartbeat(message.Timestamp);
            if (notification == null && shouldHeartbeat) {
                var heartbeatValues = Item.LastValue.ToMonitoredItemNotifications(Item,
                    () => new MonitoredItemNotificationModel {
                        DataSetFieldName = Template?.DataSetFieldName,
                        Id = Template.Id,
                        DisplayName = Item.DisplayName,
                        NodeId = Template.StartNodeId,
                        AttributeId = Item.AttributeId,
                        Value = new DataValue(Item.Status?.Error?.StatusCode ?? StatusCodes.BadMonitoredItemIdInvalid),
                    });
                foreach (var heartbeat in heartbeatValues) {
                    var heartbeatValue = heartbeat.Clone();
                    heartbeatValue.SequenceNumber = 0;
                    heartbeatValue.IsHeartbeat = true;
                    Debug.Assert(message.Notifications != null);
                    message.Notifications.Add(heartbeatValue);
                    message.MessageType = Opc.Ua.PubSub.MessageType.KeyFrame;
                }
            }
            else {
                foreach (var n in notification.ToMonitoredItemNotifications(Item)) {
                    message.Notifications.Add(n);
                }
            }

            bool ValidateHeartbeat(DateTime currentPublish) {
                if (DataTemplate == null) {
                    return false;
                }
                if (NextHeartbeat == DateTime.MaxValue) {
                    return false;
                }
                if (NextHeartbeat > currentPublish + TimeSpan.FromMilliseconds(50)) {
                    return false;
                }
                NextHeartbeat = TimeSpan.Zero < DataTemplate.HeartbeatInterval.GetValueOrDefault(TimeSpan.Zero) ?
                    currentPublish + DataTemplate.HeartbeatInterval.Value : DateTime.MaxValue;
                return true;
            }
        }

        /// <summary>
        /// Processing the monitored item notification
        /// </summary>
        /// <param name="message"></param>
        /// <param name="notification"></param>
        public void ProcessEventNotification(SubscriptionNotificationModel message, EventFieldList notification) {
            Debug.Assert(Item != null);
            var evFilter = Item.Filter as EventFilter;
            var eventTypeIndex = evFilter?.SelectClauses.IndexOf(
                evFilter?.SelectClauses
                    .FirstOrDefault(x => x.TypeDefinitionId == ObjectTypeIds.BaseEventType
                        && x.BrowsePath?.FirstOrDefault() == BrowseNames.EventType));


            ConditionHandlingState state;
            lock (_lock) {
                state = _conditionHandlingState;
            }

            // now, is this a regular event or RefreshStartEventType/RefreshEndEventType?
            if (eventTypeIndex.HasValue && eventTypeIndex.Value != -1) {
                var eventType = notification.EventFields[eventTypeIndex.Value].Value as NodeId;
                if (eventType == ObjectTypeIds.RefreshStartEventType) {
                    // stop the timers during condition refresh
                    if (state != null) {
                        _conditionTimer.Stop();
                        lock (_lock) {
                            state.Active.Clear();
                        }
                        _logger.Debug("{item}: Stopped pending alarm handling during condition refresh.", this);
                    }
                    return;
                }
                else if (eventType == ObjectTypeIds.RefreshEndEventType) {
                    if (state != null) {
                        // restart the timers once condition refresh is done.
                        _conditionTimer.Start();
                        _logger.Debug("{item}: Restarted pending alarm handling after condition refresh.", this);
                    }
                    return;
                }
                else if (eventType == ObjectTypeIds.RefreshRequiredEventType) {
                    var noErrorFound = true;

                    // issue a condition refresh to make sure we are in a correct state
                    _logger.Information("{item}: Issuing ConditionRefresh for item {item} on subscription " +
                        "{subscription} due to receiving a RefreshRequired event", this,
                        Item.DisplayName ?? "", Item.Subscription.DisplayName);
                    try {
                        Item.Subscription.ConditionRefresh();
                    }
                    catch (ServiceResultException e) {
                        _logger.Information("{item}: ConditionRefresh for item {item} on subscription " +
                            "{subscription} failed with a ServiceResultException '{message}'", this,
                            Item.DisplayName ?? "", Item.Subscription.DisplayName, e.Message);
                        noErrorFound = false;
                    }
                    catch (Exception e) {
                        _logger.Information("{item}: ConditionRefresh for item {item} on subscription " +
                            "{subscription} failed with an exception '{message}'", this,
                            Item.DisplayName ?? "", Item.Subscription.DisplayName, e.Message);
                        noErrorFound = false;
                    }
                    if (noErrorFound) {
                        _logger.Information("{item}: ConditionRefresh for item {item} on subscription " +
                            "{subscription} has completed", this,
                            Item.DisplayName ?? "", Item.Subscription.DisplayName);
                    }
                    return;
                }
            }

            var monitoredItemNotifications = notification.ToMonitoredItemNotifications(Item).ToList();
            if (state != null) {
                // Cache conditions
                var conditionId = monitoredItemNotifications[state.ConditionIdIndex].Value.Value?.ToString();
                if (conditionId != null) {
                    var retain = monitoredItemNotifications[state.RetainIndex].Value.GetValue(false);
                    lock (_lock) {
                        if (state.Active.ContainsKey(conditionId) && !retain) {
                            state.Active.Remove(conditionId, out _);
                            state.Dirty = true;
                        }
                        else if (retain && !monitoredItemNotifications.All(m => m.Value?.Value == null)) {
                            state.Dirty = true;
                            monitoredItemNotifications.ForEach(notification => {
                                if (notification.Value == null) {
                                    notification.Value = new DataValue(StatusCodes.BadNoData);
                                }
                                // Set SourceTimestamp to publish time
                                notification.Value.SourceTimestamp = message.Timestamp;
                            });
                            state.Active.AddOrUpdate(conditionId, monitoredItemNotifications);
                        }
                    }
                }
            }
            else {
                // Send notifications as event
                foreach (var n in monitoredItemNotifications.Where(n => n.DataSetFieldName != null)) {
                    message.Notifications.Add(n);
                }
            }
        }

        private void TestWhereClause(IServiceMessageContext messageContext,
            INodeCache nodeCache, ITypeTable typeTree, EventFilter eventFilter) {
            foreach (var element in eventFilter.WhereClause.Elements) {
                if (element.FilterOperator == FilterOperator.OfType) {
                    foreach (var filterOperand in element.FilterOperands) {
                        var nodeId = default(NodeId);
                        try {
                            nodeId = (filterOperand.Body as LiteralOperand).Value.ToString().ToNodeId(messageContext);
                            nodeCache.FetchNode(nodeId.ToExpandedNodeId(messageContext.NamespaceUris)); // it will throw an exception if it doesn't work
                        }
                        catch (Exception ex) {
                            _logger.Warning("{item}: Where clause is doing OfType({nodeId}) and we got this message {message} while looking it up",
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
        private void OnConditionTimerElapsed(object sender, System.Timers.ElapsedEventArgs e) {
            var now = DateTime.UtcNow;
            ConditionHandlingState state;
            lock (_lock) {
                state = _conditionHandlingState;
            }
            if (state != null) {
                try {
                    if (!Item.Created) {
                        return;
                    }
                    var options = EventTemplate.ConditionHandling;
                    var sendPendingConditions = false;

                    // is it time to send anything?
                    if (options?.SnapshotInterval != null) {
                        sendPendingConditions = now >
                            _lastSentPendingConditions + TimeSpan.FromSeconds(options.SnapshotInterval.Value);
                    }
                    if (!sendPendingConditions && state.Dirty && options?.UpdateInterval != null) {
                        sendPendingConditions = now >
                            _lastSentPendingConditions + TimeSpan.FromSeconds(options.UpdateInterval.Value);
                    }
                    if (sendPendingConditions) {
                        SendPendingConditions();
                        _lastSentPendingConditions = now;
                    }
                }
                catch (Exception ex) {
                    _logger.Error(ex, "{item}: SendPendingConditions failed.", this);
                }
                finally {
                    _conditionTimer.Start();
                }
            }
        }

        /// <summary>
        /// Send pending conditions
        /// </summary>
        private void SendPendingConditions() {
            List<List<MonitoredItemNotificationModel>> notifications = null;
            lock (_lock) {
                if (_conditionHandlingState == null) {
                    return;
                }
                notifications = _conditionHandlingState.Active
                    .Select(entry => entry.Value
                        .Where(n => n.DataSetFieldName != null)
                        .Select(n => n.Clone())
                        .ToList())
                    .ToList();
                _conditionHandlingState.Dirty = false;
            }
            if (Item.Subscription?.Handle is SubscriptionServices.SubscriptionWrapper subscription) {
                foreach (var conditionNotification in notifications) {
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
            ComplexTypeSystem typeSystem, FieldMetaDataCollection fields, NodeIdDictionary<DataTypeDescription> dataTypes) {
            var nodeId = Template.StartNodeId.ToExpandedNodeId(messageContext);
            try {
                var variable = nodeCache.FetchNode(nodeId) as VariableNode;
                if (variable != null) {
                    AddVariableField(fields, dataTypes, nodeCache, typeTree, typeSystem, variable,
                        Template.DataSetFieldName, (Uuid)DataSetFieldId);
                }
            }
            catch (Exception ex) {
                _logger.Warning("{item}: Failed to get meta data for field {field} with node {nodeId}: {message}",
                    this, Template.DataSetFieldName, nodeId, ex.Message);
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
            ComplexTypeSystem typeSystem, VariableNode variable, string fieldName, Uuid dataSetClassFieldId) {
            var builtInType = TypeInfo.GetBuiltInType(variable.DataType, nodeCache.TypeTree);
            fields.Add(new FieldMetaData {
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
            ComplexTypeSystem typeSystem, FieldMetaDataCollection fields, NodeIdDictionary<DataTypeDescription> dataTypes) {
            Debug.Assert(Fields.Count == eventFilter.SelectClauses.Count);
            for (var i = 0; i < eventFilter.SelectClauses.Count; i++) {
                var selectClause = eventFilter.SelectClauses[i];
                var fieldName = Fields[i].Name;
                if (fieldName == null) {
                    continue;
                }
                var dataSetClassFieldId = (Uuid)Fields[i].DataSetFieldId;
                var targetNode = FindNodeWithBrowsePath(nodeCache, typeTree, selectClause.BrowsePath, selectClause.TypeDefinitionId);
                if (targetNode is VariableNode variable) {
                    AddVariableField(fields, dataTypes, nodeCache, typeTree, typeSystem, variable, fieldName, dataSetClassFieldId);
                }
                else {
                    fields.Add(new FieldMetaData {
                        Name = fieldName,
                        DataSetFieldId = dataSetClassFieldId,
                    });
                }
            }
        }

        private static INode FindNodeWithBrowsePath(INodeCache nodeCache, ITypeTable typeTree,
            QualifiedNameCollection browsePath, ExpandedNodeId nodeId) {

            INode found = null;
            foreach (var browseName in browsePath) {
                found = null;
                while (found == null) {
                    found = nodeCache.Find(nodeId);
                    if (!(found is Node node)) {
                        return null;
                    }

                    // Get all hierarchical references of the node and match browse name
                    foreach (var reference in node.ReferenceTable.Find(ReferenceTypeIds.HierarchicalReferences, false, true, typeTree)) {
                        var target = nodeCache.Find(reference.TargetId);
                        if (target?.BrowseName == browseName) {
                            nodeId = target.NodeId;
                            found = target;
                            break;
                        }
                    }

                    if (found == null) {
                        // Try super type
                        nodeId = typeTree.FindSuperType(nodeId);
                        if (NodeId.IsNull(nodeId)) {
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
            INodeCache nodeCache, ITypeTable typeTree, ComplexTypeSystem typeSystem) {
            var baseType = dataTypeId;
            while (!NodeId.IsNull(baseType)) {
                try {
                    var dataType = nodeCache.FetchNode(baseType);
                    if (dataType == null) {
                        _logger.Warning("{item}: Failed to find node for data type {baseType}!", this, baseType);
                        break;
                    }

                    dataTypeId = dataType.NodeId;
                    Debug.Assert(!NodeId.IsNull(dataTypeId));
                    if (dataTypeId.NamespaceIndex == 0 && dataTypeId.IdType == IdType.Numeric) {
                        var id = (BuiltInType)(int)(uint)dataTypeId.Identifier;
                        if (id >= BuiltInType.Null && id <= BuiltInType.Enumeration) {
                            // Do not add builtin types
                            break;
                        }
                    }

                    var builtInType = TypeInfo.GetBuiltInType(dataTypeId, typeTree);
                    baseType = typeTree.FindSuperType(dataTypeId);

                    switch (builtInType) {
                        case BuiltInType.Enumeration:
                        case BuiltInType.ExtensionObject:
                            NodeIdDictionary<DataTypeDefinition> types = null;
#if FULLMETADATA // Enable when supported in stack
                            types = typeSystem?.GetDataTypeDefinitionsForDataType(dataType.NodeId);
#endif
                            if (types == null || types.Count == 0) {
                                dataTypes.AddOrUpdate(dataType.NodeId, GetDefault(dataType, builtInType));
                                break;
                            }
                            foreach (var type in types) {
                                if (!dataTypes.ContainsKey(type.Key)) {
                                    DataTypeDescription description = type.Value switch {
                                        StructureDefinition s =>
                                            new StructureDescription {
                                                DataTypeId = type.Key,
                                                Name = dataType.BrowseName,
                                                StructureDefinition = s
                                            },
                                        EnumDefinition e =>
                                            new EnumDescription {
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
                            dataTypes.AddOrUpdate(dataTypeId, new SimpleTypeDescription {
                                DataTypeId = dataTypeId,
                                Name = dataType.BrowseName,
                                BaseDataType = baseType,
                                BuiltInType = (byte)builtInType
                            });
                            break;
                    }
                }
                catch (Exception ex) {
                    _logger.Warning("{item}: Failed to get meta data for type {dataType} (base: {baseType}) with message: {message}",
                        this, dataTypeId, baseType, ex.Message);
                }
            }

            static DataTypeDescription GetDefault(Node dataType, BuiltInType builtInType) {
                return builtInType == BuiltInType.Enumeration
                    ? new EnumDescription {
                        DataTypeId = dataType.NodeId,
                        Name = dataType.BrowseName
                    } : new StructureDescription {
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
            Node node, string browsePathPrefix = "") {
            foreach (var reference in node.ReferenceTable) {
                if (reference.ReferenceTypeId == ReferenceTypeIds.HasComponent && !reference.IsInverse) {
                    var componentNode = nodeCache.FetchNode(reference.TargetId);
                    if (componentNode.NodeClass == Opc.Ua.NodeClass.Variable) {
                        var fieldName = $"{browsePathPrefix}{componentNode.BrowseName.Name}";
                        fieldNames.Add(new QualifiedName(fieldName, componentNode.BrowseName.NamespaceIndex));
                        ParseFields(nodeCache, typeTree, fieldNames, componentNode, $"{fieldName}|");
                    }
                }
                else if (reference.ReferenceTypeId == ReferenceTypeIds.HasProperty) {
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
        public bool SkipMonitoredItemNotification() {
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
        public bool TrySetSkipFirst(bool skipFirst) {
            if (skipFirst) {
                // We only allow updating first skip setting while unconfigured
                return Interlocked.CompareExchange(ref _skipDataChangeNotification,
                    (int)SkipSetting.Skip, (int)SkipSetting.Unconfigured) == (int)SkipSetting.Unconfigured;
            }
            else {
                // Unset skip setting if it was configured but first message was not yet processed
                Interlocked.CompareExchange(ref _skipDataChangeNotification,
                    (int)SkipSetting.Unconfigured, (int)SkipSetting.Skip);
                return true;
            }
        }

        enum SkipSetting : int {
            DontSkip, // Default
            Skip, // Skip first value
            Unconfigured, // Configuration not applied yet
        }

        private sealed class ConditionHandlingState {

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

        private ConditionHandlingState _conditionHandlingState;
        private volatile int _skipDataChangeNotification = (int)SkipSetting.Unconfigured;
        private Timer _conditionTimer;
        private DateTime _lastSentPendingConditions = DateTime.UtcNow;
        private HashSet<uint> _newTriggers = new HashSet<uint>();
        private HashSet<uint> _triggers = new HashSet<uint>();
        private Publisher.Models.MonitoringMode? _modeChange;
        private readonly ILogger _logger;
        private readonly object _lock = new object();
    }
}

