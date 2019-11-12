// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Triggering {
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Exceptions;
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.OpcUa.Publisher;
    using Microsoft.Azure.IIoT.Messaging;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Opc.Ua.PubSub;

    /// <summary>
    /// Monitored item message trigger
    /// </summary>
    public class MonitoredItemMessageTrigger : IMessageTrigger {

        /// <inheritdoc/>
        public string Id => Guid.NewGuid().ToString();

        /// <inheritdoc/>
        public long NumberOfConnectionRetries =>
            _subscriptions.Sum(sc => sc.NumberOfConnectionRetries);

        /// <inheritdoc/>
        public event EventHandler<MessageReceivedEventArgs> MessageReceived;

        /// <summary>
        /// Create trigger
        /// </summary>
        /// <param name="monitoredItemMessageTriggerConfig"></param>
        /// <param name="subscriptionManager"></param>
        public MonitoredItemMessageTrigger(IMonitoredItemSampleTriggerConfig monitoredItemMessageTriggerConfig,
            ISubscriptionManager subscriptionManager) {

            _subscriptionManager = subscriptionManager;
            _monitoredItemMessageTriggerConfig = monitoredItemMessageTriggerConfig;
            _subscriptions = new List<ISubscription>();
        }

        /// <inheritdoc/>
        public async Task RunAsync(CancellationToken ct) {

            foreach (var subscription in _monitoredItemMessageTriggerConfig.Subscriptions) {
                var sc = await _subscriptionManager.GetOrCreateSubscriptionAsync(subscription);
                sc.OnMonitoredItemSample += SubscriptionClient_MessageReceived;
                await sc.ApplyAsync(subscription.Subscription.MonitoredItems);
                _subscriptions.Add(sc);
            }

            await Task.Delay(-1, ct); // TODO - add managemnt of monitored items, etc.

            _subscriptions.ForEach(sc => {
                sc.OnSubscriptionMessage -= SubscriptionClient_MessageReceived;
                sc.Dispose(); // TODO
            });
            _subscriptions.Clear();
        }

        /// <summary>
        /// Handle received monitored item messages
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SubscriptionClient_MessageReceived(object sender, MessageReceivedEventArgs e) {
            if (!(e.Message.Value is MonitoredItemSample)) {
                throw new InvalidMessageFormatException(
                    "This trigger does only support MonitoredItemSample messages.");
            }
            MessageReceived?.Invoke(this, e);
        }

        private readonly List<ISubscription> _subscriptions;
        private readonly ISubscriptionManager _subscriptionManager;
        private readonly IMonitoredItemSampleTriggerConfig _monitoredItemMessageTriggerConfig;
    }
}