﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client
{
    using Opc.Ua;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Manages connecting sessions
    /// </summary>
    public interface IConnectionManager : IDisposable
    {
        /// <summary>
        /// Connect a session or get a session from the session pool. The session
        /// in the pool is retrieved using the same options are passed. If the
        /// session needs to be created it is created using the configured
        /// resiliency options. The returned object can be used to release the
        /// ownership of the session back to the pool.
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        ValueTask<PooledSession> GetOrConnectAsync(PooledSessionOptions connection,
            CancellationToken ct = default);

        /// <summary>
        /// Connect a session directly to the endpoint. The session is not pooled.
        /// Max settings are bypassed. Uses the resiliency strategy configured to
        /// connect or none if none was specified.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="options"></param>
        /// <param name="useReverseConnect"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        ValueTask<ISession> ConnectAsync(EndpointDescription endpoint,
            SessionCreateOptions? options = null, bool useReverseConnect = false,
            CancellationToken ct = default);

        /// <summary>
        /// Test connecting to an endpoint using the specified options.
        /// The session is created and closed and result returned. No
        /// resiliency policy is applied.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="useReverseConnect"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        ValueTask<ServiceResult> TestAsync(EndpointDescription endpoint,
            bool useReverseConnect = false, CancellationToken ct = default);
    }
}
