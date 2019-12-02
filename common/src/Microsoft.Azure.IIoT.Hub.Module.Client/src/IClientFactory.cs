// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Framework.Client {
    using Microsoft.Azure.Devices.Client;
    using System.Threading.Tasks;

    /// <summary>
    /// Client factory interface
    /// </summary>
    public interface IClientFactory {

        /// <summary>
        /// Set retry policy
        /// </summary>
        IRetryPolicy RetryPolicy { get; set; }

        /// <summary>
        /// Create client
        /// </summary>
        /// <param name="config"></param>
        /// <param name="product"></param>
        /// <param name="onError"></param>
        /// <returns></returns>
        Task<IClient> CreateAsync(IModuleConfig config, string product,
            IProcessControl onError = null);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        Task DisposeClient(IClient client);
    }
}
