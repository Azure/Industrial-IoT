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

    /// <summary>
    /// Session manager
    /// </summary>
    public interface ISessionManager {

        /// <summary>
        /// Number of currently active sessions.
        /// </summary>
        int SessionCount { get; }

        /// <summary>
        /// gets the number of retiries for a specific session
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        int GetNumberOfConnectionRetries(ConnectionModel connection);

        /// <summary>
        /// Returns whether the connection is up and running or not
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        bool IsConnectionOk(ConnectionModel connection);

        /// <summary>
        /// Get a connected session
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        ValueTask<ISessionHandle> GetOrCreateSessionAsync(
            ConnectionModel connection, CancellationToken ct = default);

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