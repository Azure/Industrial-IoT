// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Infrastructure.Hub {
    using System.Threading.Tasks;

    /// <summary>
    /// Manages iot hub resources
    /// </summary>
    public interface IIoTHubFactory {

        /// <summary>
        /// Create resource
        /// </summary>
        /// <param name="resourceGroup"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        Task<IIoTHubResource> CreateAsync(
            IResourceGroupResource resourceGroup, string name);

        /// <summary>
        /// Get resource
        /// </summary>
        /// <param name="resourceGroup"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        Task<IIoTHubResource> GetAsync(
            IResourceGroupResource resourceGroup, string name);
    }
}
