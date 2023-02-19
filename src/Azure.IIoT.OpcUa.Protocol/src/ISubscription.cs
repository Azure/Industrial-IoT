// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Protocol {
    using Azure.IIoT.OpcUa.Protocol.Models;
    using Azure.IIoT.OpcUa.Shared.Models;
    using System;
    using System.Threading;
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
        /// Assigned index
        /// </summary>
        ushort Id { get; }

        /// <summary>
        /// Connection
        /// </summary>
        ConnectionModel Connection { get; }

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
        /// <param name="ct"></param>
        ValueTask UpdateAsync(SubscriptionModel configuration,
            CancellationToken ct = default);

        /// <summary>
        /// Reapply current configuration
        /// </summary>
        /// <returns></returns>
        ValueTask ReapplyToSessionAsync(ISessionHandle session);

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
        ValueTask CloseAsync();
    }
}
