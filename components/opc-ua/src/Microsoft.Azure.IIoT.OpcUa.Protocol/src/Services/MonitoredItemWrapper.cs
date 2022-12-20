// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Services {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Models;
    using Opc.Ua;
    using Opc.Ua.Client;
    using Opc.Ua.Encoders;
    using Opc.Ua.Extensions;
    using Opc.Ua.Types;
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
        public Guid DataSetFieldId => Template.DataSetClassFieldId == Guid.Empty ? _fieldId
            : Template.DataSetClassFieldId;
        private Guid _fieldId = Guid.NewGuid();

        /// <summary>
        /// Cache of the latest events for the pending alarms optionally monitored
        /// </summary>
        public Dictionary<string, MonitoredItemNotificationModel> PendingAlarmEvents { get; }
            = new Dictionary<string, MonitoredItemNotificationModel>();

        /// <summary>
        /// Property setter that gets indication if item is online or not.
        /// </summary>
        public void OnMonitoredItemStateChanged(bool online) {
            if (_pendingAlarmsTimer != null) {
                var enabled = _pendingAlarmsTimer.Enabled;
                if (EventTemplate.PendingAlarms?.IsEnabled == true && online && !enabled) {
                    _pendingAlarmsTimer.Start();
                    _logger.Debug("{item}: Restarted pending alarm handling after item went online.", this);
                }
                else if (enabled) {
                    _pendingAlarmsTimer.Stop();
                    lock (_lock) {
                        PendingAlarmEvents.Clear();
                    }
                    if (!online) {
                        _logger.Debug("{item}: Stopped pending alarm handling while item is offline.", this);
                    }
                }
            }
        }

        /// <summary>
        /// validates if a heartbeat is required.
        /// A heartbeat will be forced for the very first time
        /// </summary>
        public bool ValidateHeartbeat(DateTime currentPublish) {
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
            if (!(obj is MonitoredItemWrapper item)) {
                return false;
            }
            if (Template.GetType() != item.Template.GetType()) {
                // Event item is incompatible with a data item
                return false;
            }
            if (Template.Id != item.Template.Id) {
                return false;
            }
            if (Template.DataSetClassFieldId != item.Template.DataSetClassFieldId) {
                return false;
            }
            if (!Template.RelativePath.SequenceEqualsSafe(item.Template.RelativePath)) {
                return false;
            }
            if (Template.StartNodeId != item.Template.StartNodeId) {
                return false;
            }
            if (Template.IndexRange != item.Template.IndexRange) {
                return false;
            }
            if (Template.AttributeId != item.Template.AttributeId) {
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
                EqualityComparer<Guid>.Default.GetHashCode(Template.DataSetClassFieldId);
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

        public void Create(Session session, IVariantEncoder codec, bool activate) {
            Create(session.MessageContext as ServiceMessageContext, session.NodeCache, codec, activate);
        }

        /// <summary>
        /// Destructor for this class
        /// </summary>
        public void Destroy() {
            Item.Handle = null;
            if (_pendingAlarmsTimer != null) {
                _pendingAlarmsTimer.Stop();
                _pendingAlarmsTimer.Dispose();
            }
        }

        /// <summary>
        /// Get metadata
        /// </summary>
        /// <param name="messageContext"></param>
        /// <param name="nodeCache"></param>
        /// <param name="fields"></param>
        /// <param name="dataTypes"></param>
        /// <returns></returns>
        public void GetMetaData(IServiceMessageContext messageContext, INodeCache nodeCache,
            FieldMetaDataCollection fields, NodeIdDictionary<DataTypeDescription> dataTypes) {
            try {
                if (Item.Filter is EventFilter eventFilter) {
                    if (EventTemplate.PendingAlarms?.IsEnabled == true) {
                        GetPendingAlarmsMetadata(nodeCache, fields, dataTypes);
                    }
                    else {
                        GetEventMetadata(eventFilter, nodeCache, fields, dataTypes);
                    }
                }
                else {
                    GetDataMetadata(messageContext, nodeCache, fields, dataTypes);
                }
            }
            catch (Exception e) {
                _logger.Error(e, "{item}: Failed to get metadata.", this);
            }
        }

        /// <summary>
        /// Create new stack monitored item
        /// </summary>
        public void Create(ServiceMessageContext messageContext, INodeCache nodeCache,
            IVariantEncoder codec, bool activate) {

            Item = new MonitoredItem {
                Handle = this,
                DisplayName = Template.DisplayName ?? Template.Id,
                AttributeId = (uint)Template.AttributeId.GetValueOrDefault((NodeAttribute)Attributes.Value),
                IndexRange = Template.IndexRange,
                RelativePath = Template.RelativePath?
                            .ToRelativePath(messageContext)?
                            .Format(nodeCache.TypeTree),
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
                var eventFilter = GetEventFilter(messageContext, nodeCache, codec);
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
        /// <param name="codec"></param>
        /// <param name="model"></param>
        /// <param name="metadataChange"></param>
        /// <returns>Whether apply changes should be called on the subscription</returns>
        internal bool MergeWith(IServiceMessageContext messageContext, INodeCache nodeCache,
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

            if (Template.DataSetClassFieldId != model.Template.DataSetClassFieldId) {
                var previous = DataSetFieldId;
                Template.DataSetClassFieldId = model.Template.DataSetClassFieldId;
                _logger.Debug("{item}: Changing dataset class field id from {old} to {new}",
                    this, previous, DataSetFieldId);
                metadataChange = true;
            }

            // Should never merge items with different template types
            Debug.Assert(model.Template.GetType() == Template.GetType());

            if (model.DataTemplate != null) {

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
                    !model.EventTemplate.PendingAlarms.IsSameAs(EventTemplate.PendingAlarms)) {

                    EventTemplate.PendingAlarms = model.EventTemplate.PendingAlarms;
                    EventTemplate.EventFilter = model.EventTemplate.EventFilter;
                    _logger.Debug("{item}: Changing event filter.");

                    metadataChange = true;
                    itemChange = true;
                }

                if (metadataChange) {
                    Item.Filter = GetEventFilter(messageContext, nodeCache, codec);
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
            return Item.MonitoringMode == change ? null : change;
        }

        /// <summary>
        /// Get event filter
        /// </summary>
        /// <param name="messageContext"></param>
        /// <param name="nodeCache"></param>
        /// <param name="codec"></param>
        /// <returns></returns>
        private EventFilter GetEventFilter(IServiceMessageContext messageContext, INodeCache nodeCache,
            IVariantEncoder codec) {

            // set up the timer even if event is not a pending alarms event.
            var created = false;
            if (_pendingAlarmsTimer == null) {
                _pendingAlarmsTimer = new Timer(1000);
                _pendingAlarmsTimer.AutoReset = false;
                _pendingAlarmsTimer.Elapsed += OnPendingAlarmsTimerElapsed;
                created = true;
            }

            if (!created && EventTemplate.PendingAlarms?.IsEnabled != true) {
                // Always stop in case we are asked to disable pending alarms
                _pendingAlarmsTimer.Stop();
                lock (_lock) {
                    PendingAlarmEvents.Clear();
                }
                _logger.Information("{item}: Disabled pending alarm handling.", this);
            }

            var eventFilter = new EventFilter();
            if (EventTemplate.EventFilter != null) {
                if (!string.IsNullOrEmpty(EventTemplate.EventFilter.TypeDefinitionId)) {
                    eventFilter = GetSimpleEventFilter(nodeCache, messageContext);
                }
                else {
                    eventFilter = codec.Decode(EventTemplate.EventFilter, true);
                }
            }

            TestWhereClause(messageContext, nodeCache, eventFilter);

            // let's keep track of the internal fields we add so that they don't show up in the output
            var internalSelectClauses = new List<SimpleAttributeOperand>();
            if (!eventFilter.SelectClauses.Any(x => x.TypeDefinitionId == ObjectTypeIds.BaseEventType
                && x.BrowsePath?.FirstOrDefault() == BrowseNames.EventType)) {
                var selectClause = new SimpleAttributeOperand(ObjectTypeIds.BaseEventType, BrowseNames.EventType);
                eventFilter.SelectClauses.Add(selectClause);
                internalSelectClauses.Add(selectClause);
            }

            if (EventTemplate.PendingAlarms?.IsEnabled == true) {
                var conditionIdClause = eventFilter.SelectClauses
                    .FirstOrDefault(x => x.TypeDefinitionId == ObjectTypeIds.ConditionType && x.AttributeId == Attributes.NodeId);
                if (conditionIdClause != null) {
                    EventTemplate.PendingAlarms.ConditionIdIndex = eventFilter.SelectClauses.IndexOf(conditionIdClause);
                }
                else {
                    EventTemplate.PendingAlarms.ConditionIdIndex = eventFilter.SelectClauses.Count;
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
                    EventTemplate.PendingAlarms.RetainIndex = eventFilter.SelectClauses.IndexOf(retainClause);
                }
                else {
                    EventTemplate.PendingAlarms.RetainIndex = eventFilter.SelectClauses.Count;
                    var selectClause = new SimpleAttributeOperand(ObjectTypeIds.ConditionType, BrowseNames.Retain);
                    eventFilter.SelectClauses.Add(selectClause);
                    internalSelectClauses.Add(selectClause);
                }

                _pendingAlarmsTimer.Start();
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
        /// Builds select clause and where clause by using OPC UA reflection
        /// </summary>
        /// <param name="nodeCache"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        private EventFilter GetSimpleEventFilter(INodeCache nodeCache, IServiceMessageContext context) {
            var typeDefinitionId = EventTemplate.EventFilter.TypeDefinitionId.ToNodeId(context);
            var nodes = new List<Node>();
            ExpandedNodeId superType = null;
            nodes.Insert(0, nodeCache.FetchNode(typeDefinitionId));
            do {
                superType = nodes[0].GetSuperType(nodeCache.TypeTree);
                if (superType != null) {
                    nodes.Insert(0, nodeCache.FetchNode(superType));
                }
            }
            while (superType != null);

            var fieldNames = new List<QualifiedName>();

            foreach (var node in nodes) {
                ParseFields(nodeCache, fieldNames, node);
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
        /// Processing the monitored item notification
        /// </summary>
        /// <param name="message"></param>
        /// <param name="notification"></param>
        public void ProcessMonitoredItemNotification(SubscriptionNotificationModel message, EventFieldList notification) {
            var pendingAlarmsOptions = EventTemplate?.PendingAlarms;
            var evFilter = Item.Filter as EventFilter;
            var eventTypeIndex = evFilter?.SelectClauses.IndexOf(
                evFilter?.SelectClauses
                    .FirstOrDefault(x => x.TypeDefinitionId == ObjectTypeIds.BaseEventType
                        && x.BrowsePath?.FirstOrDefault() == BrowseNames.EventType));

            // now, is this a regular event or RefreshStartEventType/RefreshEndEventType?
            if (eventTypeIndex.HasValue && eventTypeIndex.Value != -1) {
                var eventType = notification.EventFields[eventTypeIndex.Value].Value as NodeId;
                if (eventType == ObjectTypeIds.RefreshStartEventType) {
                    // stop the timers during condition refresh
                    if (pendingAlarmsOptions?.IsEnabled == true) {
                        _pendingAlarmsTimer.Stop();
                        lock (_lock) {
                            PendingAlarmEvents.Clear();
                        }
                        _logger.Debug("{item}: Stopped pending alarm handling during condition refresh.", this);
                    }
                    return;
                }
                else if (eventType == ObjectTypeIds.RefreshEndEventType) {
                    if (pendingAlarmsOptions?.IsEnabled == true) {
                        // restart the timers once condition refresh is done.
                        _pendingAlarmsTimer.Start();
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

            var monitoredItemNotification = notification.ToMonitoredItemNotification(Item);
            if (message == null) {
                return;
            }
            var values = monitoredItemNotification.Value.GetValue(typeof(EncodeableDictionary)) as EncodeableDictionary;
            if (pendingAlarmsOptions?.IsEnabled == true && values != null) {
                if (pendingAlarmsOptions.ConditionIdIndex.HasValue && pendingAlarmsOptions.RetainIndex.HasValue) {
                    var conditionId = values[pendingAlarmsOptions.ConditionIdIndex.Value].Value.ToString();
                    var retain = values[pendingAlarmsOptions.RetainIndex.Value].Value.GetValue(false);
                    lock (_lock) {
                        if (PendingAlarmEvents.ContainsKey(conditionId) && !retain) {
                            PendingAlarmEvents.Remove(conditionId, out var monitoredItemNotificationModel);
                            pendingAlarmsOptions.Dirty = true;
                        }
                        else if (retain) {
                            pendingAlarmsOptions.Dirty = true;
                            PendingAlarmEvents.AddOrUpdate(conditionId, monitoredItemNotification);
                        }
                    }
                }
            }
            else {
                message.Notifications?.Add(monitoredItemNotification);
            }
        }

        private void TestWhereClause(IServiceMessageContext messageContext,
            INodeCache nodeCache, EventFilter eventFilter) {
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

        private void OnPendingAlarmsTimerElapsed(object sender, System.Timers.ElapsedEventArgs e) {
            var now = DateTime.UtcNow;
            var pendingAlarmsOptions = EventTemplate.PendingAlarms;
            if (pendingAlarmsOptions?.IsEnabled == true) {
                try {
                    // is it time to send anything?
                    if (Item.Created &&
                        (now > (_lastSentPendingAlarms + (pendingAlarmsOptions.SnapshotIntervalTimespan ?? TimeSpan.MaxValue))) ||
                            ((now > (_lastSentPendingAlarms + (pendingAlarmsOptions.UpdateIntervalTimespan ?? TimeSpan.MaxValue))) && pendingAlarmsOptions.Dirty)) {
                        SendPendingAlarms();
                        _lastSentPendingAlarms = now;
                    }
                }
                catch (Exception ex) {
                    _logger.Error(ex, "{item}: SendPendingAlarms failed.", this);
                }
                finally {
                    _pendingAlarmsTimer.Start();
                }
            }
        }

        /// <summary>
        /// Send pending alarms
        /// </summary>
        private void SendPendingAlarms() {
            ExtensionObject[] notifications = null;
            uint sequenceNumber;
            lock (_lock) {
                notifications = PendingAlarmEvents.Values
                    .Select(x => x.Value.Value)
                    .OfType<ExtensionObject>()
                    .ToArray();
                sequenceNumber = ++_pendingAlarmsSequenceNumber;
                EventTemplate.PendingAlarms.Dirty = false;
            }

            if (Item.Subscription?.Handle is SubscriptionServices.SubscriptionWrapper subscription) {
                var pendingAlarmsNotification = new MonitoredItemNotificationModel {
                    DataSetFieldName = Template?.DataSetFieldName,
                    Id = Template?.Id,
                    AttributeId = Item.AttributeId,
                    DisplayName = Item.DisplayName,
                    IsHeartbeat = false,
                    SequenceNumber = sequenceNumber,
                    NodeId = Item.StartNodeId.ToString(),
                    Value = new DataValue(notifications)
                };
                subscription.SendEventNotification(Item.Subscription, pendingAlarmsNotification);
            }
        }

        /// <summary>
        /// Update data field metadata
        /// </summary>
        /// <param name="messageContext"></param>
        /// <param name="nodeCache"></param>
        /// <param name="fields"></param>
        /// <param name="dataTypes"></param>
        private void GetDataMetadata(IServiceMessageContext messageContext, INodeCache nodeCache,
            FieldMetaDataCollection fields, NodeIdDictionary<DataTypeDescription> dataTypes) {
            var nodeId = Template.StartNodeId.ToExpandedNodeId(messageContext);
            try {
                var variable = nodeCache.FetchNode(nodeId) as VariableNode;
                if (variable != null) {
                    AddVariableField(fields, dataTypes, nodeCache, variable,
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
        /// <param name="variable"></param>
        /// <param name="fieldName"></param>
        /// <param name="dataSetClassFieldId"></param>
        private void AddVariableField(FieldMetaDataCollection fields,
            NodeIdDictionary<DataTypeDescription> dataTypes, INodeCache nodeCache,
            VariableNode variable, string fieldName, Uuid dataSetClassFieldId) {
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
            AddDataTypes(dataTypes, variable.DataType, nodeCache);
        }

        /// <summary>
        /// Get event metadata.
        /// </summary>
        /// <param name="eventFilter"></param>
        /// <param name="nodeCache"></param>
        /// <param name="fields"></param>
        /// <param name="dataTypes"></param>
        /// <returns></returns>
        private void GetEventMetadata(EventFilter eventFilter, INodeCache nodeCache,
            FieldMetaDataCollection fields, NodeIdDictionary<DataTypeDescription> dataTypes) {
            Debug.Assert(Fields.Count == eventFilter.SelectClauses.Count);
            for (var i = 0; i < eventFilter.SelectClauses.Count; i++) {
                var selectClause = eventFilter.SelectClauses[i];
                var fieldName = Fields[i].Name;
                if (fieldName == null) {
                    continue;
                }
                var dataSetClassFieldId = (Uuid)Fields[i].DataSetFieldId;
                var typeDefinition = nodeCache.FetchNode(selectClause.TypeDefinitionId);
                if (typeDefinition != null) {
                    var targetNode = nodeCache.BuildBrowsePath(typeDefinition, selectClause.BrowsePath);
                    if (!NodeId.IsNull(targetNode)) {
                        var variable = nodeCache.FetchNode(selectClause.TypeDefinitionId) as VariableNode;
                        if (variable != null) {
                            AddVariableField(fields, dataTypes, nodeCache, variable, fieldName, dataSetClassFieldId);
                            continue;
                        }
                    }
                }
                fields.Add(new FieldMetaData {
                    Name = fieldName,
                    DataSetFieldId = dataSetClassFieldId,
                });
            }
        }

        /// <summary>
        /// Fake meta data for pending alarms
        /// </summary>
        /// <param name="nodeCache"></param>
        /// <param name="fields"></param>
        /// <param name="dataTypes"></param>
        /// <returns></returns>
        private void GetPendingAlarmsMetadata(INodeCache nodeCache,
            FieldMetaDataCollection fields, NodeIdDictionary<DataTypeDescription> dataTypes) {
            var dataType = TypeInfo.GetDataTypeId(typeof(EncodeableDictionary));
            fields.Add(new FieldMetaData {
                Name = Template.DataSetFieldName,
                DataSetFieldId = (Uuid)DataSetFieldId,
                DataType = dataType,
                Description = "Pending Alarms",
                ValueRank = ValueRanks.Scalar,
                BuiltInType = (byte)BuiltInType.ExtensionObject
            });
            // AddDataTypes(dataTypes, dataType, nodeCache);
        }

        /// <summary>
        /// Add data types to the metadata
        /// </summary>
        /// <param name="dataTypes"></param>
        /// <param name="dataTypeId"></param>
        /// <param name="nodeCache"></param>
        private void AddDataTypes(NodeIdDictionary<DataTypeDescription> dataTypes, NodeId dataTypeId,
            INodeCache nodeCache) {
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

                    var builtInType = TypeInfo.GetBuiltInType(dataTypeId, nodeCache.TypeTree);
                    baseType = nodeCache.TypeTree.FindSuperType(dataTypeId);

                    if (!dataTypes.ContainsKey(dataType.NodeId)) {
                        switch (builtInType) {
                            case BuiltInType.ExtensionObject:
                                StructureDefinition structureDefinition = null;  // TODO: Use complex type system
                                dataTypes.Add(dataTypeId, new StructureDescription {
                                    DataTypeId = dataTypeId,
                                    Name = dataType.BrowseName,
                                    StructureDefinition = structureDefinition
                                });
                                break;
                            case BuiltInType.Enumeration:
                                EnumDefinition enumDefinition = null;  // TODO: Use complex type system
                                dataTypes.Add(dataTypeId, new EnumDescription {
                                    DataTypeId = dataTypeId,
                                    Name = dataType.BrowseName,
                                    EnumDefinition = enumDefinition,
                                    BuiltInType = (byte)builtInType
                                });
                                break;
                            default:
                                dataTypes.Add(dataTypeId, new SimpleTypeDescription {
                                    DataTypeId = dataTypeId,
                                    Name = dataType.BrowseName,
                                    BaseDataType = baseType,
                                    BuiltInType = (byte)builtInType
                                });
                                break;
                        }
                    }
                }
                catch (Exception ex) {
                    _logger.Warning("{item}: Failed to get meta data for type {dataType} (base: {baseType}) with message: {message}",
                        this, dataTypeId, baseType, ex.Message);
                }
            }
        }

        /// <summary>
        /// Get all the fields of a type definition node to build the select clause.
        /// </summary>
        /// <param name="nodeCache"></param>
        /// <param name="fieldNames"></param>
        /// <param name="node"></param>
        /// <param name="browsePathPrefix"></param>
        private void ParseFields(INodeCache nodeCache, List<QualifiedName> fieldNames, Node node, string browsePathPrefix = "") {
            foreach (var reference in node.ReferenceTable) {
                if (reference.ReferenceTypeId == ReferenceTypeIds.HasComponent && !reference.IsInverse) {
                    var componentNode = nodeCache.FetchNode(reference.TargetId);
                    if (componentNode.NodeClass == Opc.Ua.NodeClass.Variable) {
                        var fieldName = $"{browsePathPrefix}{componentNode.BrowseName.Name}";
                        fieldNames.Add(new QualifiedName(fieldName, componentNode.BrowseName.NamespaceIndex));
                        ParseFields(nodeCache, fieldNames, componentNode, $"{fieldName}|");
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

        private volatile int _skipDataChangeNotification = (int)SkipSetting.Unconfigured;
        private Timer _pendingAlarmsTimer;
        private DateTime _lastSentPendingAlarms = DateTime.UtcNow;
        private uint _pendingAlarmsSequenceNumber;
        private HashSet<uint> _newTriggers = new HashSet<uint>();
        private HashSet<uint> _triggers = new HashSet<uint>();
        private Publisher.Models.MonitoringMode? _modeChange;
        private readonly ILogger _logger;
        private readonly object _lock = new object();
    }
}

