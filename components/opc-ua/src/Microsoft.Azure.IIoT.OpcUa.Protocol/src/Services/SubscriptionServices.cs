// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Services {
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Models;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.Azure.IIoT.Messaging;
    using Microsoft.Azure.IIoT.Utils;
    using Opc.Ua;
    using Opc.Ua.Client;
    using Opc.Ua.Extensions;
    using Opc.Ua.PubSub;
    using Serilog;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Subscription services implementation
    /// </summary>
    public class SubscriptionServices : ISubscriptionManager, IDisposable {

        /// <inheritdoc/>
        public int TotalSubscriptionCount => _subscriptions.Count;

        /// <summary>
        /// Create subscription manager
        /// </summary>
        /// <param name="sessionManager"></param>
        /// <param name="logger"></param>
        public SubscriptionServices(ISessionManager sessionManager, ILogger logger) {
            _sessionManager = sessionManager;
            _logger = logger;
        }

        /// <inheritdoc/>
        public Task<ISubscription> GetOrCreateSubscriptionAsync(SubscriptionInfoModel subscriptionModel) {
            var sub = _subscriptions.GetOrAdd(
                SubscriptionWrapper.GetId(subscriptionModel.Subscription),
                key => new SubscriptionWrapper(key, this, subscriptionModel, _logger));
            return Task.FromResult<ISubscription>(sub);
        }

        /// <inheritdoc/>
        public void Dispose() {
            // Cleanup remaining subscriptions
            var subscriptions = _subscriptions.Values.ToList();
            _subscriptions.Clear();
            subscriptions.ForEach(s => Try.Op(() => s.Dispose()));
        }


        // TODO : Timer to lazily invalidate subscriptions after a while


        /// <summary>
        /// Subscription implementation
        /// </summary>
        internal sealed class SubscriptionWrapper : ISubscription {

            /// <inheritdoc/>
            public string Id { get; }

            /// <inheritdoc/>
            public ConnectionModel Connection => _subscription.Connection;

            /// <inheritdoc/>
            public event EventHandler<MessageReceivedEventArgs> OnSubscriptionMessage;

            /// <inheritdoc/>
            public event EventHandler<MessageReceivedEventArgs> OnMonitoredItemSample;

            /// <inheritdoc/>
            public long NumberOfConnectionRetries { get; private set; }

            /// <inheritdoc/>
            public Dictionary<string, DataValue> LastValues { get; }

            /// <summary>
            /// Subscription wrapper
            /// </summary>
            /// <param name="id"></param>
            /// <param name="outer"></param>
            /// <param name="model"></param>
            /// <param name="logger"></param>
            public SubscriptionWrapper(string id, SubscriptionServices outer,
                SubscriptionInfoModel model, ILogger logger) {
                _subscription = model.Clone();
                _outer = outer;
                _logger = logger.ForContext<SubscriptionWrapper>();
                Id = id;
                LastValues = new Dictionary<string, DataValue>();
                _timer = new Timer(_ => OnCheckAsync().Wait());
                _lock = new SemaphoreSlim(1, 1);
            }

            /// <inheritdoc/>
            public async Task CloseAsync() {

                _outer._subscriptions.TryRemove(Id, out _);

                await _lock.WaitAsync();
                try {
                    var session = await _outer._sessionManager.GetOrCreateSessionAsync(Connection, false);
                    if (session != null) {
                        var subscription = session.Subscriptions
                            .SingleOrDefault(s => s.DisplayName == Id);
                        if (subscription != null) {
                            session.RemoveSubscription(subscription);
                        }
                        // Cleanup session if empty
                        await _outer._sessionManager.RemoveSessionAsync(Connection);
                    }
                }
                finally {
                    _lock.Release();
                }
            }

            /// <inheritdoc/>
            public void Dispose() {
                Try.Async(CloseAsync).Wait();
                _timer.Dispose();
                _lock.Dispose();
            }


            /// <inheritdoc/>
            public async Task<ServiceMessageContext> GetServiceMessageContextAsync() {
                await _lock.WaitAsync();
                try {
                    var session = await _outer._sessionManager.GetOrCreateSessionAsync(Connection, false);
                    return session?.MessageContext;
                }
                finally {
                    _lock.Release();
                }
            }

            /// <inheritdoc/>
            public async Task ApplyAsync(IEnumerable<MonitoredItemModel> monitoredItems) {
                await _lock.WaitAsync();
                try {
                    _monitoredItems = monitoredItems.ToList();

                    var rawSubscription = await GetSubscriptionAsync();
                    var updateRequired = false;
                    // Synchronize the  desired state with the state of the raw subscription
                    var orphanedNodes = rawSubscription.MonitoredItems.Where(m =>
                        !_monitoredItems
                            .Select(s => s.NodeId.ToNodeId(rawSubscription.Session.MessageContext))
                            .Contains(m.StartNodeId));
                    foreach (var orphanedNode in orphanedNodes) {
                        rawSubscription.RemoveItem(orphanedNode);
                        updateRequired = true;
                    }

                    var newMonitoredItems = new List<MonitoredItem>();
                    foreach (var monitoredItemInfo in monitoredItems) {
                        var monitoredItem =
                            rawSubscription.MonitoredItems.SingleOrDefault(m =>
                                m.DisplayName == monitoredItemInfo.NodeId);
                        if (monitoredItem != null) {
                            continue;
                        }

                        _logger.Debug("Adding new monitored item with NodeId='{id}'...",
                                monitoredItemInfo.NodeId);
                        try {

                            monitoredItem = new MonitoredItem {
                                DisplayName = monitoredItemInfo.NodeId,
                                StartNodeId = monitoredItemInfo.NodeId.ToNodeId(
                                    rawSubscription.Session.MessageContext),
                                QueueSize = monitoredItemInfo.QueueSize ?? 0,
                                SamplingInterval = monitoredItemInfo.SamplingInterval ?? 0,
                                DiscardOldest = !(monitoredItemInfo.DiscardNew ?? false)
                            };

                            LastValues[monitoredItemInfo.NodeId] =
                                rawSubscription.Session.ReadValue(monitoredItem.StartNodeId);

                            monitoredItem.Notification += OnMonitoredItemChanged;
                            rawSubscription.AddItem(monitoredItem);
                            newMonitoredItems.Add(monitoredItem);

                            updateRequired = true;
                        }
                        catch (ServiceResultException e) {
                            switch ((uint)e.Result.StatusCode) {
                                case StatusCodes.BadNodeIdInvalid:
                                case StatusCodes.BadNodeIdUnknown:
                                    _logger.Error(e, "Failed to monitor node due to '{message}'.",
                                        e.Message);

                                    // Remove node and continue
                                    _monitoredItems.Remove(monitoredItemInfo);
                                    break;
                                default:
                                    throw e;
                            }
                        }
                    }

                    if (updateRequired) {
                        rawSubscription.ApplyChanges();

                        ValidateRevisedProperties(rawSubscription.DisplayName,
                            nameof(rawSubscription.PublishingInterval),
                                () => rawSubscription.PublishingInterval,
                                () => rawSubscription.CurrentPublishingInterval);
                        foreach (var monitoredItem in newMonitoredItems) {
                            ValidateRevisedProperties(monitoredItem.StartNodeId.ToString(),
                                nameof(monitoredItem.SamplingInterval),
                                    () => monitoredItem.SamplingInterval,
                                    () => monitoredItem.Status.SamplingInterval);
                        }

                        // Set timer to check connection periodically
                        if (_monitoredItems.Count > 0) {
                            _timer.Change(TimeSpan.FromSeconds(10), Timeout.InfiniteTimeSpan);
                        }
                    }
                }
                catch (ServiceResultException sre) {
                    // TODO: Convert to better exception
                    _logger.Error(sre, "Failed apply monitored items.");
                    await _outer._sessionManager.RemoveSessionAsync(Connection, false);
                    throw sre;
                }
                finally {
                    _lock.Release();
                }
            }

            /// <summary>
            /// Get identifier for subscription
            /// </summary>
            /// <param name="subscription"></param>
            /// <returns></returns>
            internal static string GetId(SubscriptionModel subscription) {
                var displayName =
                   ((subscription.PublishingInterval ?? 0).ToString() +
                    (subscription.MaxKeepAliveCount ?? 0).ToString() +
                    (subscription.MaxNotificationsPerPublish ?? 0).ToString() +
                    (subscription.PublishingDisabled ?? false).ToString() +
                    (subscription.Priority ?? 0).ToString() +
                    (subscription.LifeTimeCount ?? 0).ToString()).ToSha1Hash();
                if (!string.IsNullOrEmpty(subscription.Id)) {
                    displayName += "_" + subscription.Id;
                }
                return displayName;
            }

            /// <summary>
            /// Check connectivity
            /// </summary>
            private async Task OnCheckAsync() {
                try {
                    await ApplyAsync(_monitoredItems);
                    // Changes the timer to check connection if items is not empty
                }
                catch (Exception e) { // TODO Catch exceptions related to connection
                    NumberOfConnectionRetries++;

                    // Retry in 3 seconds
                    _timer?.Change(TimeSpan.FromSeconds(3), Timeout.InfiniteTimeSpan);
                    _logger.Error(e, "Failed ensure connection for monitored items.");
                }
            }

            /// <summary>
            /// Validate revised properties of monitored item
            /// </summary>
            /// <typeparam name="TPropertyType"></typeparam>
            /// <param name="itemDescriptor"></param>
            /// <param name="propertyName"></param>
            /// <param name="configuredValue"></param>
            /// <param name="revisedValue"></param>
            /// <returns></returns>
            private void ValidateRevisedProperties<TPropertyType>(string itemDescriptor, string propertyName,
                Func<TPropertyType> configuredValue, Func<TPropertyType> revisedValue)
                where TPropertyType : IComparable {
                if (configuredValue().CompareTo(revisedValue()) != 0) {
                    _logger.Warning(
                        "The property '{propertyName}' of monitored item with node id '{itemDescriptor}' " +
                        "has been revised from '{configuredValue}' to '{revisedValue}'",
                        propertyName, itemDescriptor, configuredValue(), revisedValue());
                }
            }

            /// <summary>
            /// Create subscription
            /// </summary>
            /// <returns></returns>
            private async Task<Subscription> GetSubscriptionAsync() {
                var session = await _outer._sessionManager.GetOrCreateSessionAsync(Connection, true);
                var subscription = session.Subscriptions
                    .SingleOrDefault(s => s.DisplayName == Id);
                if (subscription == null) {

                    subscription = new Subscription(session.DefaultSubscription) {
                        PublishingInterval = _subscription.Subscription.PublishingInterval ?? 0,
                        DisplayName = Id,
                        KeepAliveCount = _subscription.Subscription.MaxKeepAliveCount ?? 10,
                        MaxNotificationsPerPublish = _subscription.Subscription.MaxNotificationsPerPublish ?? 0,
                        PublishingEnabled = !(_subscription.Subscription.PublishingDisabled ?? false),
                        Priority = _subscription.Subscription.Priority ?? 0,
                        LifetimeCount = _subscription.Subscription.LifeTimeCount ?? 2400,
                        TimestampsToReturn = TimestampsToReturn.Both,
                        FastDataChangeCallback = OnSubscriptionDataChanged
                        // MaxMessageCount = 10,
                    };

                    session.AddSubscription(subscription);
                    subscription.Create();
                    _logger.Debug("Added subscription '{name}' to session '{session}'.",
                         Id, session.SessionName);
                }
                return subscription;
            }

            /// <summary>
            /// Subscription data changed
            /// </summary>
            /// <param name="subscription"></param>
            /// <param name="notification"></param>
            /// <param name="stringTable"></param>
            private void OnSubscriptionDataChanged(Subscription subscription,
                DataChangeNotification notification, IList<string> stringTable) {
                try {
                    if (OnSubscriptionMessage == null) {
                        return;
                    }
                    var values = new Dictionary<string, DataValue>();
                    foreach (var monitoredItemNotification in notification.MonitoredItems) {
                        var monitoredItem = subscription.MonitoredItems.SingleOrDefault(
                            m => m.ClientHandle == monitoredItemNotification.ClientHandle);
                        if (monitoredItem == null) {
                            continue;
                        }
                        values[monitoredItem.DisplayName] = monitoredItemNotification.Value;
                    }
                    var message = new SubscriptionMessage {
                        ServiceMessageContext = subscription.Session.MessageContext,
                        Values = values
                    };
                    var md = new MessageData<SubscriptionMessage>(Guid.NewGuid().ToString(), message);
                    OnSubscriptionMessage(this, new MessageReceivedEventArgs(md));
                }
                catch (Exception ex) {
                    _logger.Debug(ex, "Exception processing subscription notification");
                }
            }

            /// <summary>
            /// Monitored item notification handler
            /// </summary>
            /// <param name="monitoredItem"></param>
            /// <param name="e"></param>
            private void OnMonitoredItemChanged(MonitoredItem monitoredItem,
                MonitoredItemNotificationEventArgs e) {
                try {
                    if (e?.NotificationValue == null || monitoredItem?.Subscription?.Session == null) {
                        return;
                    }
                    if (!(e.NotificationValue is MonitoredItemNotification notification)) {
                        return;
                    }
                    if (!(notification.Value is DataValue value)) {
                        return;
                    }
                    // TODO Check against _monitoredItems content
                    LastValues[monitoredItem.DisplayName] = value;
                    if (OnMonitoredItemSample == null) {
                        return;
                    }
                    var message = new MonitoredItemSample {
                        ServiceMessageContext = monitoredItem.Subscription.Session.MessageContext,
                        Value = value,
                        EndpointUrl = monitoredItem.Subscription.Session.Endpoint.EndpointUrl,
                        NodeId = monitoredItem.StartNodeId,
                        ApplicationUri = monitoredItem.Subscription.Session.Endpoint.Server.ApplicationUri,
                        DisplayName = monitoredItem.DisplayName,
                        SubscriptionId = Id,
                        ExtraFields = _subscription.ExtraFields
                    };
                    var md = new MessageData<MonitoredItemSample>(Guid.NewGuid().ToString(), message);
                    OnMonitoredItemSample(this, new MessageReceivedEventArgs(md));
                }
                catch (Exception ex) {
                    _logger.Debug(ex, "Exception processing monitored item notification");
                }
            }

            private readonly SubscriptionInfoModel _subscription;
            private readonly SubscriptionServices _outer;
            private readonly ILogger _logger;
            private readonly SemaphoreSlim _lock;
            private readonly Timer _timer;
            private List<MonitoredItemModel> _monitoredItems;
        }

        private readonly ILogger _logger;
        private readonly ConcurrentDictionary<string, SubscriptionWrapper> _subscriptions =
            new ConcurrentDictionary<string, SubscriptionWrapper>();
        private readonly ISessionManager _sessionManager;
    }
}