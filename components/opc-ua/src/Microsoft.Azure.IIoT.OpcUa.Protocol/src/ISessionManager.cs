// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using System.Threading.Tasks;
    using Opc.Ua.Client;

    /// <summary>
    /// Session manager
    /// </summary>
    public interface ISessionManager {

        /// <summary>
        /// Number of sessions
        /// </summary>
        int SessionCount { get; }

        /// <summary>
        /// Get or create session for subscription
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="createIfNotExists"></param>
        /// <param name="forceActivation"></param>
        /// <returns></returns>
        Task<Session> GetOrCreateSessionAsync(ConnectionModel connection,
            bool createIfNotExists, bool forceActivation);

        /// <summary>
        /// Remove session if empty
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="onlyIfEmpty"></param>
        /// <returns></returns>
        Task RemoveSessionAsync(ConnectionModel connection,
            bool onlyIfEmpty = true);
    }
}