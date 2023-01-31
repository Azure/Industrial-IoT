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
        /// Apply desired state of the subscription and its monitored items.
        /// This will attempt a differential update of the subscription
        /// and monitored items state. It is called periodically, when the
        /// configuration is updated or when a session is reconnected and
        /// the subscription needs to be recreated.
        /// </summary>
        /// <param name="configuration"></param>
        /// <returns>enabled</returns>
        Task UpdateAsync(SubscriptionModel configuration);

        /// <summary>
        /// Reapply current configuration
        /// </summary>
        /// <returns></returns>
        Task ReapplyToSessionAsync(ISessionHandle session);

        /// <summary>
        /// Called to signal the underlying session is disconnected and the
        /// subscription is offline, or when it is reconnected and the
        /// session is back online. This is the case during reconnect handler
        /// execution or when the subscription was disconnected.
        /// </summary>
        /// <param name="online"></param>
        void OnSubscriptionStateChanged(bool online);

        /// <summary>
        /// Close and delete subscription
        /// </summary>
        /// <returns></returns>
        Task CloseAsync();
    }
}
