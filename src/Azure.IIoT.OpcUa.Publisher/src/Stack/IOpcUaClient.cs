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
        /// Trigger the client to manage the subscription. This is a
        /// no op if the subscription is not registered or the client
        /// is not connected.
        /// </summary>
        /// <param name="subscription"></param>
        /// <param name="closeSubscription"></param>
        void ManageSubscription(IOpcUaSubscription subscription,
            bool closeSubscription = false);
    }
}
