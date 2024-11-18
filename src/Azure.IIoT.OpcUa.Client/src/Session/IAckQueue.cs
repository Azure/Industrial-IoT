// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation, The OPC Foundation, Inc.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client
{
    using Opc.Ua;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Message acknoledgement queue
    /// </summary>
    public interface IAckQueue
    {
        /// <summary>
        /// Subscriptionss queue acknoledgements for completed
        /// notifications as soon as they are dispatched / handled.
        /// </summary>
        /// <param name="ack"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        ValueTask QueueAsync(SubscriptionAcknowledgement ack,
            CancellationToken ct = default);

        /// <summary>
        /// Complete subscription
        /// </summary>
        /// <param name="subscription"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        ValueTask CompleteAsync(Subscription subscription,
            CancellationToken ct = default);
    }
}
