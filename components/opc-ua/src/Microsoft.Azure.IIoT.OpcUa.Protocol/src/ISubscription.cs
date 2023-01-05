// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol {
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Models;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Opc.Ua.Client;

    /// <summary>
    /// Subscription abstraction
    /// </summary>
    public interface ISubscription : IDisposable {

        /// <summary>
        /// Subscription data change events
        /// </summary>
        event EventHandler<SubscriptionNotificationModel> OnSubscriptionDataChange;

        /// <summary>
        /// Subscription event change events
        /// </summary>
        event EventHandler<SubscriptionNotificationModel> OnSubscriptionEventChange;

        /// <summary>
        /// Subscription data change diagnostics events
        /// </summary>
        event EventHandler<int> OnSubscriptionDataDiagnosticsChange;

        /// <summary>
        /// Subscription event change diagnostics events
        /// </summary>
        event EventHandler<int> OnSubscriptionEventDiagnosticsChange;

        /// <summary>
        /// Identifier of the subscription
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Index inside the publisher
        /// </summary>
        ushort Id { get; }

        /// <summary>
        /// Enabled - successfully created on server
        /// </summary>
        bool Enabled { get; }

        /// <summary>
        /// Publishing is active
        /// </summary>
        bool Active { get; }

        /// <summary>
        /// Connection
        /// </summary>
        ConnectionModel Connection { get; }

        /// <summary>
        /// Number of retries on the session
        /// </summary>
        int NumberOfConnectionRetries { get; }

        /// <summary>
        /// IsConnectionOk
        /// </summary>
        bool IsConnectionOk { get; }

        /// <summary>
        /// Number of nodes connected and receiving data
        /// </summary>
        int NumberOfGoodNodes { get; }

        /// <summary>
        /// Number of nodes disconnected
        /// </summary>
        int NumberOfBadNodes { get; }

        /// <summary>
        /// Create a keep alive notification
        /// </summary>
        /// <returns></returns>
        SubscriptionNotificationModel CreateKeepAlive();

        /// <summary>
        /// Adds a snapshot of all values to the notification
        /// </summary>
        /// <returns></returns>
        bool TryUpgradeToKeyFrame(SubscriptionNotificationModel notification);

        /// <summary>
        /// Apply desired state
        /// </summary>
        /// <param name="monitoredItems"></param>
        /// <param name="configuration"></param>
        /// <returns>enabled</returns>
        Task ApplyAsync(IEnumerable<BaseMonitoredItemModel> monitoredItems,
            SubscriptionConfigurationModel configuration);

        /// <summary>
        /// Creates the subscription and it's associated monitored items
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        Task EnableAsync(Session session);

        /// <summary>
        /// Sets the subscription and it's monitored items in publishing respective
        /// reporting state
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        Task ActivateAsync(Session session);

        /// <summary>
        /// disables publishing for the subscription
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        Task DeactivateAsync(Session session);

        /// <summary>
        /// Close and delete subscription
        /// </summary>
        /// <returns></returns>
        Task CloseAsync();

        /// <summary>
        /// Function that gets called when subscription state changes between online/offline
        /// </summary>
        void OnSubscriptionStateChanged(bool online);
    }
}
