// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack
{
    using Microsoft.Azure.IIoT.Diagnostics;
    using Opc.Ua.Client;
    using Opc.Ua.Client.ComplexTypes;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Session services
    /// </summary>
    public interface ISessionProvider<T>
    {
        /// <summary>
        /// Get a connected session
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="metrics"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        ValueTask<ISessionHandle> GetOrCreateSessionAsync(
            T connection, IMetricsContext metrics = null,
            CancellationToken ct = default);

        /// <summary>
        /// Execute the service on the provided session.
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="connection"></param>
        /// <param name="service"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<R> ExecuteServiceAsync<R>(T connection,
            Func<ISessionHandle, Task<R>> service, CancellationToken ct);

        /// <summary>
        /// Get or create session handle
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        ISessionHandle GetSessionHandle(T connection);

        /// <summary>
        /// Get a session handle from a session
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        ISessionHandle GetSessionHandle(ISession session);
    }
}
