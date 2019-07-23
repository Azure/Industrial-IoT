// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Infrastructure.Network {
    using System.Threading.Tasks;

    /// <summary>
    /// Manages network resources
    /// </summary>
    public interface INetworkFactory {

        /// <summary>
        /// Create new network
        /// </summary>
        /// <param name="resourceGroup"></param>
        /// <param name="name"></param>
        /// <param name="addressSpace"></param>
        /// <param name="withSecurity"></param>
        /// <returns></returns>
        Task<INetworkResource> CreateAsync(
            IResourceGroupResource resourceGroup, string name,
            string addressSpace, bool withSecurity);

        /// <summary>
        /// Get network
        /// </summary>
        /// <param name="resourceGroup"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        Task<INetworkResource> GetAsync(
            IResourceGroupResource resourceGroup, string name);
    }
}
