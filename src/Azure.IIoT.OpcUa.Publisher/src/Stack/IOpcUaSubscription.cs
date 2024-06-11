// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack
{
    using Opc.Ua.Client;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// The opc ua subscription is an internal interface
    /// between opc ua client and the subscription owned
    /// by the client.
    /// </summary>
    internal interface IOpcUaSubscription
    {
        /// <summary>
        /// Create or update the subscription now using the
        /// currently configured subscription configuration.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        ValueTask SyncWithSessionAsync(ISession session,
            CancellationToken ct = default);

        /// <summary>
        /// Try get the current position in the out stream.
        /// </summary>
        /// <param name="subscriptionId"></param>
        /// <param name="sequenceNumber"></param>
        /// <returns></returns>
        bool TryGetCurrentPosition(out uint subscriptionId,
            out uint sequenceNumber);

        /// <summary>
        /// Notifiy session disconnected/reconnecting
        /// </summary>
        /// <param name="disconnected"></param>
        /// <returns></returns>
        void NotifySessionConnectionState(bool disconnected);

        /// <summary>
        /// Notifies the subscription that should remove
        /// itself from the session. If the session is null
        /// then there is no session and the subscription
        /// should clean up.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        ValueTask CloseInSessionAsync(ISession? session,
            CancellationToken ct = default);
    }
}
