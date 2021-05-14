// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Opc.Ua.Client;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents a client session managed by the client host
    /// </summary>
    public interface IClientSession : IDisposable {

        /// <summary>
        /// Whether the session is inactive and can be collected
        /// </summary>
        bool Inactive { get; }

        /// <summary>
        /// How many operations are currently pending for the session.
        /// </summary>
        int Pending { get; }

        /// <summary>
        /// Try schedule operation on the session
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="elevation"></param>
        /// <param name="priority"></param>
        /// <param name="serviceCall"></param>
        /// <param name="handler"></param>
        /// <param name="timeout"></param>
        /// <param name="ct"></param>
        /// <param name="completion"></param>
        /// <returns></returns>
        bool TryScheduleServiceCall<T>(CredentialModel elevation, int priority,
            Func<Session, Task<T>> serviceCall, Func<Exception, bool> handler,
            TimeSpan? timeout, CancellationToken? ct, out Task<T> completion);

        /// <summary>
        /// Get a new safe handle to the session
        /// </summary>
        /// <returns></returns>
        ISessionHandle GetSafeHandle();

        /// <summary>
        /// Closes the session and cancels any outstanding operations.
        /// </summary>
        /// <returns></returns>
        Task CloseAsync();
    }
}
