// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using System.Threading.Tasks;
    using Opc.Ua.Client;
    using Opc.Ua.Client.ComplexTypes;
    using System.Threading;
    using Microsoft.Azure.IIoT.Diagnostics;

    /// <summary>
    /// Session manager
    /// </summary>
    public interface ISessionManager {

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
        /// Get session for connection
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        ISessionHandle FindSession(ConnectionModel connection);

        /// <summary>
        /// Get complex type system from session
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        ValueTask<ComplexTypeSystem> GetComplexTypeSystemAsync(ISession session);
    }
}