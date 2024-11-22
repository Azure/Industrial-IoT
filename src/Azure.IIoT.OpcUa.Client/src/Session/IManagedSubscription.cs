// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation, The OPC Foundation, Inc.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Non sdk interface that allows subscription manager to manage
    /// subcriptions. Must be implemted by subscriptions to be
    /// manageable by the <see cref="SubscriptionManager"/>.
    /// </summary>
    public interface IManagedSubscription : ISubscription, IMessageProcessor
    {
        /// <summary>
        /// Called after the subscription was transferred.
        /// </summary>
        /// <param name="subscriptionId">Id of the transferred
        /// subscription.</param>
        /// <param name="availableSequenceNumbers">A list of
        /// sequence number ranges
        /// that identify NotificationMessages that are in the
        /// Subscription’s retransmission queue. This parameter
        /// is null if the transfer of the Subscription failed.
        /// </param>
        /// <param name="ct">The cancellation token.</param>
        ValueTask<bool> TransferAsync(uint? subscriptionId,
            IReadOnlyList<uint>? availableSequenceNumbers,
            CancellationToken ct = default);

        /// <summary>
        /// Recreate the subscription on a new session
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        ValueTask RecreateAsync(CancellationToken ct = default);

        /// <summary>
        /// Removes an item from the subscription.
        /// </summary>
        /// <param name="monitoredItem"></param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="monitoredItem"/> is <c>null</c>.</exception>
        void RemoveItem(MonitoredItem monitoredItem);
    }
}
