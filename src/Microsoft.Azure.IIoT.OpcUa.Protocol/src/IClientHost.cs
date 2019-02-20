// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using System;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;

    /// <summary>
    /// Client stack services
    /// </summary>
    public interface IClientHost {

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
        /// Register endpoint state callback
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="callback"></param>
        Task Register(EndpointModel endpoint,
            Func<EndpointConnectivityState, Task> callback);

        /// <summary>
        /// Unregister endpoint status callback
        /// </summary>
        /// <param name="endpoint"></param>
        Task Unregister(EndpointModel endpoint);
    }
}
