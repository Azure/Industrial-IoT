// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Framework.Services {
    using Microsoft.Azure.IIoT.Module.Framework.Client;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Encapsulates a runnable module
    /// </summary>
    public interface IModuleHost : IDisposable {

        /// <summary>
        /// Start module host
        /// </summary>
        /// <param name="type"></param>
        /// <param name="siteId"></param>
        /// <param name="productInfo"></param>
        /// <param name="version"></param>
        /// <param name="control"></param>
        /// <returns></returns>
        Task StartAsync(string type, string siteId,
            string productInfo, string version,
            IProcessControl control = null);

        /// <summary>
        /// Stop module host
        /// </summary>
        /// <returns></returns>
        Task StopAsync();
    }
}
