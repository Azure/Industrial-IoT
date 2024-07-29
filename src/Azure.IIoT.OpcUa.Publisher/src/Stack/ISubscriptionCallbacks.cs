// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using System.Collections.Generic;

    /// <summary>
    /// Subscription callbacks
    /// </summary>
    public interface ISubscriptionCallbacks
    {
        /// <summary>
        /// Gets all monitored items in the subscription
        /// </summary>
        public IReadOnlyList<BaseMonitoredItemModel> MonitoredItems { get; }

        /// <summary>
        /// Called when the subscription is updated
        /// </summary>
        /// <param name="subscriptionHandle"></param>
        public void OnSubscriptionUpdated(
            ISubscriptionHandle? subscriptionHandle);

        /// <summary>
        /// Called when a keep alive notification is received
        /// </summary>
        /// <param name="notification"></param>
        public void OnSubscriptionKeepAlive(
            IOpcUaSubscriptionNotification notification);

        /// <summary>
        /// Called when subscription data changes
        /// </summary>
        /// <param name="notification"></param>
        public void OnSubscriptionDataChangeReceived(
            IOpcUaSubscriptionNotification notification);

        /// <summary>
        /// Called when sampled values were received
        /// </summary>
        /// <param name="notification"></param>
        public void OnSubscriptionCyclicReadCompleted(
            IOpcUaSubscriptionNotification notification);

        /// <summary>
        /// Called when event changes
        /// </summary>
        /// <param name="notification"></param>
        public void OnSubscriptionEventReceived(
            IOpcUaSubscriptionNotification notification);

        /// <summary>
        /// ChannelDiagnostics for data change notifications
        /// </summary>
        /// <param name="liveData"></param>
        /// <param name="valueChanges"></param>
        /// <param name="overflow"></param>
        /// <param name="heartbeats"></param>
        void OnSubscriptionDataDiagnosticsChange(bool liveData,
            int valueChanges, int overflow, int heartbeats);

        /// <summary>
        /// ChannelDiagnostics for data change notifications
        /// </summary>
        /// <param name="valuesSampled"></param>
        /// <param name="overflow"></param>
        void OnSubscriptionCyclicReadDiagnosticsChange(
            int valuesSampled, int overflow);

        /// <summary>
        /// Event diagnostics
        /// </summary>
        /// <param name="liveData"></param>
        /// <param name="events"></param>
        /// <param name="overflow"></param>
        /// <param name="modelChanges"></param>
        void OnSubscriptionEventDiagnosticsChange(bool liveData,
            int events, int overflow, int modelChanges);
    }
}
