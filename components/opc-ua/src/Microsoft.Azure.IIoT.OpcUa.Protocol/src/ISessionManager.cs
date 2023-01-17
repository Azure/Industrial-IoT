// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using System.Threading.Tasks;
    using Opc.Ua.Client;
    using Opc.Ua.Client.ComplexTypes;

    /// <summary>
    /// Session manager
    /// </summary>
    public interface ISessionManager {

        /// <summary>
        /// Number of sessions
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
        public bool IsConnectionOk(ConnectionModel connection);

        /// <summary>
        /// Get or create session for subscription
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="ensureWorkingSession"></param>
        /// <returns></returns>
        ISession GetOrCreateSession(ConnectionModel connection, bool ensureWorkingSession);

        /// <summary>
        /// Get complex type system from session
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        ComplexTypeSystem GetComplexTypeSystem(ISession session);

        /// <summary>
        /// Remove session if empty
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="onlyIfEmpty"></param>
        /// <returns></returns>
        Task RemoveSessionAsync(ConnectionModel connection, bool onlyIfEmpty = true);

        /// <summary>
        /// Get or create a subscription
        /// </summary>
        /// <param name="subscription"></param>
        /// <returns></returns>
        void RegisterSubscription(ISubscription subscription);

        /// <summary>
        /// Removes a subscription
        /// </summary>
        /// <param name="subscription"></param>
        /// <returns></returns>
        void UnregisterSubscription(ISubscription subscription);

        /// <summary>
        /// stops all pending sessions
        /// </summary>
        /// <returns></returns>
        Task StopAsync();
    }
}