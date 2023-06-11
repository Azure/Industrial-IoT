// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack
{
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// The subscription handle is an internal interface
    /// between opc ua client and the subscription owned
    /// by the client.
    /// </summary>
    internal interface ISubscriptionHandle
    {
        /// <summary>
        /// Apply the current subscription configuration to
        /// the session.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        ValueTask SyncWithSessionAsync(IOpcUaSession session,
            CancellationToken ct = default);

        /// <summary>
        /// Called to signal the underlying session is
        /// disconnected and the subscription is offline, or
        /// when it is reconnected and the session is back online.
        /// This is the case during reconnect handler execution
        /// or when the subscription was disconnected.
        /// </summary>
        /// <param name="online"></param>
        /// <param name="connectionAttempts"></param>
        void OnSubscriptionStateChanged(bool online,
            int connectionAttempts);

        /// <summary>
        /// Try get the current position in the out stream.
        /// </summary>
        /// <param name="subscriptionId"></param>
        /// <param name="sequenceNumber"></param>
        /// <returns></returns>
        bool TryGetCurrentPosition(out uint subscriptionId,
            out uint sequenceNumber);
    }
}
