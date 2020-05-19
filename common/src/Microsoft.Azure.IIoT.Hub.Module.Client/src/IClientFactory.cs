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
        /// Device id
        /// </summary>
        string DeviceId { get; }

        /// <summary>
        /// Module id
        /// </summary>
        string ModuleId { get; }

        /// <summary>
        /// Gateway Host Name
        /// </summary>
        string Gateway { get; }

        /// <summary>
        /// Set retry policy
        /// </summary>
        IRetryPolicy RetryPolicy { get; set; }

        /// <summary>
        /// Create client
        /// </summary>
        /// <param name="product"></param>
        /// <param name="onError"></param>
        /// <returns></returns>
        Task<IClient> CreateAsync(string product,
            IProcessControl onError = null);
    }
}
