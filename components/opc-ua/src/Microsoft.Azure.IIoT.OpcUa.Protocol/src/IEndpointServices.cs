// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Opc.Ua.Client;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Endpoint services
    /// </summary>
    public interface IEndpointServices {

        /// <summary>
        /// Execute the service on the provided session.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="connection"></param>
        /// <param name="elevation"></param>
        /// <param name="priority"></param>
        /// <param name="service"></param>
        /// <param name="timeout"></param>
        /// <param name="ct"></param>
        /// <param name="exceptionHandler"></param>
        /// <returns></returns>
        Task<T> ExecuteServiceAsync<T>(ConnectionModel connection,
            CredentialModel elevation, int priority, Func<Session,
            Task<T>> service, TimeSpan? timeout, CancellationToken ct,
            Func<Exception, bool> exceptionHandler);

        /// <summary>
        /// Get or create session handle
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        ISessionHandle GetSessionHandle(ConnectionModel connection);

        /// <summary>
        /// Register endpoint state callback
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="callback"></param>
        IDisposable RegisterCallback(ConnectionModel endpoint,
            Func<EndpointConnectivityState, Task> callback);
    }
}
