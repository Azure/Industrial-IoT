// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Net.Dhcp {
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Server interface
    /// </summary>
    public interface IDhcpServerHost : IDisposable {

        /// <summary>
        /// Start server
        /// </summary>
        /// <returns></returns>
        Task StartAsync();

        /// <summary>
        /// Stop server
        /// </summary>
        /// <returns></returns>
        Task StopAsync();
    }
}