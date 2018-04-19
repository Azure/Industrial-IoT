// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.Devices.Edge.Services {
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Encapsulates a runnable module
    /// </summary>
    public interface IEdgeHost : IDisposable {

        /// <summary>
        /// Operation timeout
        /// </summary>
        TimeSpan Timeout { get; set; }

        /// <summary>
        /// Start service
        /// </summary>
        /// <returns></returns>
        Task StartAsync();

        /// <summary>
        /// Stop service
        /// </summary>
        /// <returns></returns>
        Task StopAsync();
    }
}