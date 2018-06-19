// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Edge.Services {
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Encapsulates a runnable module
    /// </summary>
    public interface IEdgeHost : IDisposable {

        /// <summary>
        /// Start service
        /// </summary>
        /// <param name="type"></param>
        /// <param name="siteId"></param>
        /// <returns></returns>
        Task StartAsync(string type, string siteId);

        /// <summary>
        /// Stop service
        /// </summary>
        /// <returns></returns>
        Task StopAsync();
    }
}