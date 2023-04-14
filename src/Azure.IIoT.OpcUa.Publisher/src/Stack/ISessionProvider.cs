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
    /// Session provider provides access to sessions.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ISessionProvider<T>
    {
        /// <summary>
        /// Get a connected session. The session handle must be
        /// disposed when not used anymore.
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="metrics"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        ValueTask<ISessionHandle> GetOrCreateSessionAsync(
            T connection, IMetricsContext? metrics = null,
            CancellationToken ct = default);

        /// <summary>
        /// Execute the service on the provided session.
        /// The session handle must not be disposed, it is
        /// automatically disposed when this call returns.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="connection"></param>
        /// <param name="service"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<TResult> ExecuteServiceAsync<TResult>(T connection,
            Func<ISessionHandle, Task<TResult>> service,
            CancellationToken ct = default);

        /// <summary>
        /// Get a session handle for a connection or null
        /// if the session does not exist. The session might
        /// be disconnected at point it is returned. The session
        /// handle must be disposed when not used anymore.
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        ISessionHandle? GetSessionHandle(T connection);
    }
}
