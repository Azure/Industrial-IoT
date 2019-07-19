// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Client host services
    /// </summary>
    public interface IClientHost {

        /// <summary>
        /// Add certificate to trust list
        /// </summary>
        /// <param name="certificate"></param>
        /// <returns></returns>
        Task AddTrustedPeerAsync(byte[] certificate);

        /// <summary>
        /// Remove certificate from trust list
        /// </summary>
        /// <param name="certificate"></param>
        /// <returns></returns>
        Task RemoveTrustedPeerAsync(byte[] certificate);

        /// <summary>
        /// Register endpoint state callback
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="callback"></param>
        Task RegisterAsync(EndpointModel endpoint,
            Func<EndpointConnectivityState, Task> callback);

        /// <summary>
        /// Unregister endpoint status callback
        /// </summary>
        /// <param name="endpoint"></param>
        Task UnregisterAsync(EndpointModel endpoint);
    }
}
