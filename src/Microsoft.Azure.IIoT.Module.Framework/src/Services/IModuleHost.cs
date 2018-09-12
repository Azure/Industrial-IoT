// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Framework.Services {
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Encapsulates a runnable module
    /// </summary>
    public interface IModuleHost : IDisposable {

        /// <summary>
        /// Start service
        /// </summary>
        /// <param name="type"></param>
        /// <param name="siteId"></param>
        /// <param name="serviceInfo"></param>
        /// <returns></returns>
        Task StartAsync(string type, string siteId, string serviceInfo);

        /// <summary>
        /// Stop service
        /// </summary>
        /// <returns></returns>
        Task StopAsync();
    }
}
