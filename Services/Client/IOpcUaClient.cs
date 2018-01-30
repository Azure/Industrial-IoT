// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Client {
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Models;
    using Opc.Ua;
    using Opc.Ua.Client;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Internal stack client services
    /// </summary>
    public interface IOpcUaClient {

        bool UsesProxy { get; }

        /// <summary>
        /// Try to get concretely offered endpoints from server specified by the 
        /// passed in endpoint.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        Task TryConnectAsync(ServerEndpointModel endpoint,
            Action<ITransportChannel, IEnumerable<EndpointDescription>> callback);

        /// <summary>
        /// Execute the service
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="endpoint"></param>
        /// <param name="service">callback providing a session to use</param>
        /// <returns></returns>
        Task<T> ExecuteServiceAsync<T>(ServerEndpointModel endpoint,
            Func<Session, Task<T>> service);
    }
}