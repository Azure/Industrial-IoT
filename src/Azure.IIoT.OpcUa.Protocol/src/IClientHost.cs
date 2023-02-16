// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Protocol {
    using Microsoft.Azure.IIoT;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Client host services
    /// </summary>
    public interface IClientHost : IHostProcess, IAsyncDisposable {

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
    }
}
