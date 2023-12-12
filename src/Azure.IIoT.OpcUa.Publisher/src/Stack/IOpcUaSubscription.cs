// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack
{
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Subscription abstraction
    /// </summary>
    public interface IOpcUaSubscription : IDisposable
    {
        /// <summary>
        /// Subscription keep alive events
        /// </summary>
        event EventHandler<IOpcUaSubscriptionNotification>? OnSubscriptionKeepAlive;

        /// <summary>
        /// Subscription data change events
        /// </summary>
        event EventHandler<IOpcUaSubscriptionNotification>? OnSubscriptionDataChange;

        /// <summary>
        /// Subscription event change events
        /// </summary>
        event EventHandler<IOpcUaSubscriptionNotification>? OnSubscriptionEventChange;

        /// <summary>
        /// Subscription data change diagnostics events
        /// </summary>
        event EventHandler<(bool, int, int, int)>? OnSubscriptionDataDiagnosticsChange;

        /// <summary>
        /// Subscription event change diagnostics events
        /// </summary>
        event EventHandler<(bool, int)>? OnSubscriptionEventDiagnosticsChange;

        /// <summary>
        /// Identifier of the subscription
        /// </summary>
        string? Name { get; }

        /// <summary>
        /// Assigned index
        /// </summary>
        ushort Id { get; }

        /// <summary>
        /// Connection
        /// </summary>
        ConnectionModel? Connection { get; }

        /// <summary>
        /// Create a keep alive notification
        /// </summary>
        /// <returns></returns>
        IOpcUaSubscriptionNotification? CreateKeepAlive();

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
        /// Close and delete subscription
        /// </summary>
        /// <returns></returns>
        ValueTask CloseAsync();
    }
}
