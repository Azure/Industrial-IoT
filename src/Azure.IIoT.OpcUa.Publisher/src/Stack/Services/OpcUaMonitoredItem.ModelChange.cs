// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Services
{
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Azure.IIoT.OpcUa.Encoders.PubSub;
    using Microsoft.Extensions.Logging;
    using Opc.Ua;
    using Opc.Ua.Client;
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
            /// <param name="template"></param>
            /// <param name="client"></param>
            /// <param name="logger"></param>
            public ModelChangeEventItem(MonitoredAddressSpaceModel template,
                IOpcUaClient client, ILogger<ModelChangeEventItem> logger)
                : base(logger, template.Order)
            {
                Template = template;
                _client = client;
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
                browser?.CloseAsync().AsTask().GetAwaiter().GetResult();
                base.Dispose(disposing);
            }

            /// <inheritdoc/>
            public override bool Equals(object? obj)
            {
                if (obj is not ModelChangeEventItem modelChange)
                {
                    return false;
                }
                if ((Template.Id ?? string.Empty) !=
                    (modelChange.Template.Id ?? string.Empty))
                {
                    return false;
                }
                if ((Template.Name ?? string.Empty) !=
                    (modelChange.Template.Name ?? string.Empty))
                {
                    return false;
                }
                if (_client != modelChange._client)
                {
                    return false;
                }
                return base.Equals(obj);
            }

            /// <inheritdoc/>
            public override int GetHashCode()
            {
                return HashCode.Combine(base.GetHashCode(),
                    nameof(ModelChangeEventItem),
                    Template.Name ?? string.Empty,
                    Template.Id ?? string.Empty,
                    _client);
            }

            /// <inheritdoc/>
            public override string ToString()
            {
                return
                    $"Model Change Item with server id {RemoteId}" +
                    $" - {(Status?.Created == true ? "" : "not ")}created";
            }

            /// <inheritdoc/>
            public override bool MergeWith(OpcUaMonitoredItem item, IOpcUaSession session)
            {
                if (item is not ModelChangeEventItem || !Valid)
                {
                    return false;
                }
                return true;
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
                            TimeSpan.FromHours(12), Subscription.DisplayName);

                        _browser.OnReferenceChange += OnReferenceChange;
                        _browser.OnNodeChange += OnNodeChange;
                        _logger.LogInformation("Item {Item} registered with browser.", this);
                    }
                }
            };

            /// <inheritdoc/>
            public override bool AddTo(Subscription subscription,
                IOpcUaSession session)
            {
                DisplayName = Template.GetFieldId();
                AttributeId = Attributes.EventNotifier;
                MonitoringMode = MonitoringMode.Reporting;
                StartNodeId = ObjectIds.RootFolder;
                QueueSize = Template.QueueSize;
                SamplingInterval = 0;
                Filter = GetEventFilter();
                DiscardOldest = !(Template.DiscardNew ?? false);
                Valid = true;

                return base.AddTo(subscription, session);

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

            /// <inheritdoc/>
            protected override IEnumerable<OpcUaMonitoredItem> CreateTriggeredItems(
                ILoggerFactory factory, IOpcUaClient? client = null)
            {
                if (Template.TriggeredItems != null)
                {
                    return Create(Template.TriggeredItems, factory, client);
                }
                return Enumerable.Empty<OpcUaMonitoredItem>();
            }

            /// <summary>
            /// Called when node changed
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
            private void OnNodeChange(object? sender, Change<Node> e)
            {
                _callback?.Invoke(MessageType.Event, CreateEvent(_nodeChangeType, e),
                    sender as ISession);
            }

            /// <summary>
            /// Called when reference changes
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
            private void OnReferenceChange(object? sender, Change<ReferenceDescription> e)
            {
                _callback?.Invoke(MessageType.Event, CreateEvent(_refChangeType, e),
                    sender as ISession);
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
                    var fieldName = _fields[i];
                    switch (i)
                    {
                        case 0:
                            value = new Variant(Guid.NewGuid().ToByteArray());
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
                        Order = Order,
                        MonitoredItemId = Template.GetMonitoredItemId(),
                        FieldId = fieldName,
                        Context = Template.Context,
                        PathFromRoot = changeFeedNotification.PathFromRoot,
                        NodeId = Template.StartNodeId,
                        Value = new DataValue(value.Value),
                        Flags = MonitoredItemSourceFlags.ModelChanges,
                        SequenceNumber = changeFeedNotification.SequenceNumber
                    };
                }
            }

            // TODO: Put somewhere central to align with resolver
            private static readonly string[] _fields = new[]
            {
                BrowseNames.EventId,
                BrowseNames.EventType,
                BrowseNames.SourceNode,
                BrowseNames.Time,
                "Change"
            };
            private static readonly ExpandedNodeId _refChangeType
                = new("ReferenceChange", "http://www.microsoft.com/opc-publisher");
            private static readonly ExpandedNodeId _nodeChangeType
                = new("NodeChange", "http://www.microsoft.com/opc-publisher");

            private readonly IOpcUaClient _client;
            private IOpcUaBrowser? _browser;
            private Callback? _callback;
        }
    }
}
