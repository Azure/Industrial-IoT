// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Opc Ua client provides access to sessions services. It must be disposed
    /// when not used as the inner session state is ref counted.
    /// </summary>
    internal interface IOpcUaClient : IDisposable
    {
        /// <summary>
        /// Safe access underlying session or null if session not available.
        /// The object return must be disposed to release the reader lock
        /// guarding the session. While holding the reader lock the session is
        /// not disposed or replaced.
        /// </summary>
        ISessionHandle GetSessionHandle();

        /// <summary>
        /// Get access to a session handle. Waits on the reader lock to
        /// ensure the handle is connected. While holding the reader lock
        /// the session is not disposed or replaced.
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        ValueTask<ISessionHandle> GetSessionHandleAsync(
            CancellationToken ct = default);

        /// <summary>
        /// Register a subscription which takes a reference on the session
        /// handle. Must be unregistered to release the reference count.
        /// Reference count going to 1 means that the connect thread is
        /// started to unblock the writer lock on the session once connected.
        /// Once the session is connected the subcription state is applied.
        /// If the session is already connected it is applied inline.
        /// </summary>
        /// <param name="subscription"></param>
        /// <returns></returns>
        void RegisterSubscription(ISubscriptionHandle subscription);

        /// <summary>
        /// Trigger the client to manage the subscription. This is a
        /// no op if the subscription is not registered or the client
        /// is not connected.
        /// </summary>
        /// <param name="subscription"></param>
        void ManageSubscription(ISubscriptionHandle subscription);

        /// <summary>
        /// Removes a subscription and releases the reference count. If the
        /// refernce count goes to 0 the session is disconnected and the
        /// writer lock is aquired until it is going back to 1 or higher.
        /// </summary>
        /// <param name="subscription"></param>
        /// <returns></returns>
        void UnregisterSubscription(ISubscriptionHandle subscription);
    }
}
