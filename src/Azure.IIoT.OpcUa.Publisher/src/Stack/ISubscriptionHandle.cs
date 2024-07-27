// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack
{
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;

    /// <summary>
    /// Subscription handle is a safe abstraction that allows the owner of the
    /// subscription to update and control without requiring access to the
    /// underlying state in the opc ua client session.
    /// </summary>
    public interface ISubscriptionHandle
    {
        /// <summary>
        /// Identifier of the subscription
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Assigned index
        /// </summary>
        ushort LocalIndex { get; }

        /// <summary>
        /// State of the underlying client
        /// </summary>
        IOpcUaClientDiagnostics State { get; }

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
        void Update(SubscriptionModel configuration);

        /// <summary>
        /// Close subscription handle
        /// </summary>
        /// <returns></returns>
        void Close();
    }
}
