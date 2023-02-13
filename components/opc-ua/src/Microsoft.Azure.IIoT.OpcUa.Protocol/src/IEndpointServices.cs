// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol {
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Opc.Ua.Client;
    using Opc.Ua.Client.ComplexTypes;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Endpoint services
    /// </summary>
    public interface IEndpointServices {

        /// <summary>
        /// Number of currently active sessions.
        /// </summary>
        int SessionCount { get; }

        /// <summary>
        /// Get a connected session
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="metrics"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        ValueTask<ISessionHandle> GetOrCreateSessionAsync(
            ConnectionModel connection, IMetricsContext metrics = null,
            CancellationToken ct = default);

        /// <summary>
        /// Execute the service on the provided session.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="connection"></param>
        /// <param name="service"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<T> ExecuteServiceAsync<T>(ConnectionModel connection,
            Func<ISession, Task<T>> service, CancellationToken ct);

        /// <summary>
        /// Get or create session handle
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        ISessionHandle GetSessionHandle(ConnectionModel connection);

        /// <summary>
        /// Get complex type system from session
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        ValueTask<ComplexTypeSystem> GetComplexTypeSystemAsync(
            ISession session);
    }
}
