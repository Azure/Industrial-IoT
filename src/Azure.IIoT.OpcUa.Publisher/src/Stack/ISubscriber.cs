// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack
{
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack.Services;
    using System.Collections.Generic;

    /// <summary>
    /// Lightweight subscription a client can create on
    /// a connection providing monitored items.
    /// </summary>
    public interface ISubscriber
    {
        /// <summary>
        /// The monitored items that shall be monitored in this
        /// subscription. If the list is updated the registration
        /// object must be updated and the list is read again.
        /// </summary>
        IEnumerable<BaseMonitoredItemModel> MonitoredItems { get; }

        /// <summary>
        /// The semantics of the desired monitored items
        /// changed, therefore the subscriber should update
        /// its information
        /// </summary>
        void OnMonitoredItemSemanticsChanged();

        /// <summary>
        /// Called when a keep alive notification is received
        /// in the subscription.
        /// </summary>
        /// <param name="notification"></param>
        void OnSubscriptionKeepAlive(
            OpcUaSubscriptionNotification notification);

        /// <summary>
        /// Called when subscription data changes
        /// </summary>
        /// <param name="notification"></param>
        void OnSubscriptionDataChangeReceived(
            OpcUaSubscriptionNotification notification);

        /// <summary>
        /// Called when sampled values were received
        /// </summary>
        /// <param name="notification"></param>
        void OnSubscriptionCyclicReadCompleted(
            OpcUaSubscriptionNotification notification);

        /// <summary>
        /// Called when event changes
        /// </summary>
        /// <param name="notification"></param>
        void OnSubscriptionEventReceived(
            OpcUaSubscriptionNotification notification);

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
