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
    using System.Runtime.Serialization;
    using System.Threading;
    using System.Threading.Tasks;

    internal abstract partial class OpcUaMonitoredItem
    {
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
            /// Create model change item
            /// </summary>
            /// <param name="owner"></param>
            /// <param name="template"></param>
            /// <param name="client"></param>
            /// <param name="logger"></param>
            /// <param name="timeProvider"></param>
            public ModelChangeEventItem(ISubscriber owner,
                MonitoredAddressSpaceModel template, OpcUaClient client,
                ILogger<ModelChangeEventItem> logger, TimeProvider timeProvider) :
                base(owner, logger, template.StartNodeId, timeProvider)
            {
                Template = template;
                _client = client;
                _fields = GetEventFields().ToArray();
            }

            /// <inheritdoc/>
            protected override void Dispose(bool disposing)
            {
                // Cleanup
                var browser = _browser;
                lock (_lock)
                {
                    _disposed = true;
                    _browser = null;
                    if (browser != null)
                    {
                        browser.OnReferenceChange -= OnReferenceChange;
                        browser.OnNodeChange -= OnNodeChange;
                        browser.CloseAsync().AsTask().GetAwaiter().GetResult();
                    }
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
                    _client.GetHashCode();
                return hashCode;
            }

            /// <inheritdoc/>
            public override string ToString()
            {
                var str = "Model Change Item";
                if (RemoteId.HasValue)
                {
                    str += $" with server id {RemoteId} ({(Created ? "" : "not ")}created)";
                }
                return str;
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
                ComplexTypeSystem? typeSystem, List<PublishedFieldMetaDataModel> fields,
                NodeIdDictionary<object> dataTypes, CancellationToken ct)
            {
                fields.AddRange(_fields);
                return ValueTask.CompletedTask;
            }

            /// <inheritdoc/>
            public override Func<CancellationToken, Task>? FinalizeCompleteChanges => async _ =>
            {
                if (!AttachedToSubscription)
                {
                    await StopBrowserAsync().ConfigureAwait(false);
                }
                else
                {
                    EnsureBrowserStarted();
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

                DisplayName = Template.DisplayName;
                AttributeId = Attributes.EventNotifier;
                MonitoringMode = Opc.Ua.MonitoringMode.Reporting;
                StartNodeId = nodeId;
                SamplingInterval = TimeSpan.Zero;
                UpdateQueueSize(subscription, Template);
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
            public override bool TryGetMonitoredItemNotifications(DateTimeOffset publishTime,
                IEncodeable evt, MonitoredItemNotifications notifications)
            {
                if (evt is not EventFieldList eventFields ||
                    !base.TryGetMonitoredItemNotifications(publishTime, evt, notifications))
                {
                    return false;
                }

                // Rebrowse and find changes or just process and send the changes
                Debug.Assert(Valid);
                Debug.Assert(Template != null);

                var evFilter = Filter as EventFilter;
                var eventTypeIndex = evFilter?.SelectClauses.IndexOf(
                    evFilter.SelectClauses
                        .Find(x => x.TypeDefinitionId == ObjectTypeIds.BaseEventType
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
                EnsureBrowserStarted();
                _browser?.Rebrowse();
                return true;
            }

            /// <inheritdoc/>
            protected override bool TryGetErrorMonitoredItemNotifications(
                StatusCode statusCode, MonitoredItemNotifications notifications)
            {
                return true;
            }

            /// <inheritdoc/>
            protected override IEnumerable<OpcUaMonitoredItem> CreateTriggeredItems(
                ILoggerFactory factory, OpcUaClient client)
            {
                if (Template.TriggeredItems != null)
                {
                    return Create(client, Template.TriggeredItems.Select(i => (Owner, i)),
                        factory, TimeProvider);
                }
                return Enumerable.Empty<OpcUaMonitoredItem>();
            }

            /// <inheritdoc/>
            protected override bool OnSamplingIntervalOrQueueSizeRevised(
                bool samplingIntervalChanged, bool queueSizeChanged)
            {
                Debug.Assert(Subscription != null);
                var applyChanges = base.OnSamplingIntervalOrQueueSizeRevised(
                    samplingIntervalChanged, queueSizeChanged);
                if (samplingIntervalChanged && CurrentSamplingInterval != TimeSpan.Zero)
                {
                    // Not necessary as sampling interval will likely always stay 0
                    applyChanges |= UpdateQueueSize(Subscription, Template);
                }
                return applyChanges;
            }

            /// <summary>
            /// Called when node changed
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
            private void OnNodeChange(object? sender, Change<Node> e)
            {
                Publish(Owner, MessageType.Event,
                    CreateEvent(kNodeChangeType, e).ToList(),
                    eventTypeName: EventTypeName);
            }

            /// <summary>
            /// Called when reference changes
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
            private void OnReferenceChange(object? sender, Change<ReferenceDescription> e)
            {
                Publish(Owner, MessageType.Event,
                    CreateEvent(kRefChangeType, e).ToList(),
                    eventTypeName: EventTypeName);
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
                            value = new Variant(changeFeedNotification.Timestamp.UtcDateTime);
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
                        PathFromRoot = changeFeedNotification.PathFromRoot,
                        NodeId = Template.StartNodeId,
                        Value = new DataValue(value.Value),
                        Flags = MonitoredItemSourceFlags.ModelChanges,
                        SequenceNumber = changeFeedNotification.SequenceNumber
                    };
                }
            }

            private static IEnumerable<PublishedFieldMetaDataModel> GetEventFields()
            {
                yield return Create(BrowseNames.EventId, builtInType: BuiltInType.ByteString);
                yield return Create(BrowseNames.EventType, builtInType: BuiltInType.NodeId);
                yield return Create(BrowseNames.SourceNode, builtInType: BuiltInType.NodeId);
                yield return Create(BrowseNames.Time, builtInType: BuiltInType.NodeId);
                yield return Create("Change", builtInType: BuiltInType.ExtensionObject);

                static PublishedFieldMetaDataModel Create(string fieldName,
                    BuiltInType builtInType = BuiltInType.ExtensionObject)
                {
                    return new PublishedFieldMetaDataModel
                    {
                        Id = (Uuid)Guid.NewGuid(),
                        DataType = "i=" + (uint)builtInType,
                        Name = fieldName,
                        ValueRank = ValueRanks.Scalar,
                        // ArrayDimensions =
                        BuiltInType = (byte)builtInType
                    };
                }
            }

            /// <summary>
            /// Start browser
            /// </summary>
            private void EnsureBrowserStarted()
            {
                lock (_lock)
                {
                    if (_disposed)
                    {
                        return;
                    }
                    // Start the browser
                    if (_browser == null &&
                        Subscription is OpcUaSubscription subscription)
                    {
                        _browser = _client.Browse(Template.RebrowsePeriod ??
                            TimeSpan.FromHours(12), subscription);

                        _browser.OnReferenceChange += OnReferenceChange;
                        _browser.OnNodeChange += OnNodeChange;
                        _logger.LogInformation("Item {Item} registered with browser.", this);
                    }
                }
            }

            /// <summary>
            /// Stop browser
            /// </summary>
            /// <returns></returns>
            private async Task StopBrowserAsync()
            {
                // Stop the browser
                IOpcUaBrowser? browser;
                lock (_lock)
                {
                    browser = _browser;
                    if (browser != null)
                    {
                        browser.OnReferenceChange -= OnReferenceChange;
                        browser.OnNodeChange -= OnNodeChange;
                    }
                    _browser = null;
                }
                if (browser != null)
                {
                    await browser.CloseAsync().ConfigureAwait(false);
                    _logger.LogInformation("Item {Item} unregistered from browser.", this);
                }
            }

            private static readonly ExpandedNodeId kRefChangeType
                = new("ReferenceChange", "http://www.microsoft.com/opc-publisher");
            private static readonly ExpandedNodeId kNodeChangeType
                = new("NodeChange", "http://www.microsoft.com/opc-publisher");
            private readonly PublishedFieldMetaDataModel[] _fields;
            private readonly OpcUaClient _client;
            private readonly object _lock = new();
            private IOpcUaBrowser? _browser;
            private bool _disposed;
        }
    }
}
