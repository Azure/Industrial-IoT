// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol {
    using Microsoft.Azure.IIoT.OpcUa.Models;
    using Opc.Ua;
    using Opc.Ua.Client;
    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Internal stack client services
    /// </summary>
    public interface IOpcUaClient {

        /// <summary>
        /// Returns the client certificate
        /// </summary>
        X509Certificate2 Certificate { get; }

        /// <summary>
        /// Update client certificate
        /// </summary>
        /// <param name="certificate"></param>
        /// <returns></returns>
        Task UpdateClientCertificate(X509Certificate2 certificate);

        /// <summary>
        /// Try to get all offered endpoints from all servers discoverable
        /// using the provided endpoint url.
        /// </summary>
        /// <param name="discoveryUrl"></param>
        /// <returns></returns>
        Task<IEnumerable<OpcUaDiscoveryResult>> DiscoverAsync(
            Uri discoveryUrl, CancellationToken ct);

        /// <summary>
        /// Execute the service
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="endpoint"></param>
        /// <param name="service">callback providing a session to use</param>
        /// <param name="exception">exception handler</param>
        /// <returns></returns>
        Task<T> ExecuteServiceAsync<T>(EndpointModel endpoint,
            Func<Session, Task<T>> service, Func<Exception, bool> exception);
    }
}
