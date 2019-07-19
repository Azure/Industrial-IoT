// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Infrastructure.ServiceBus {
    using System.Threading.Tasks;

    /// <summary>
    /// Manages service bus namespace resources
    /// </summary>
    public interface IServiceBusFactory {

        /// <summary>
        /// Create service bus namespace
        /// </summary>
        /// <param name="resourceGroup"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        Task<IServiceBusResource> CreateAsync(
            IResourceGroupResource resourceGroup, string name);

        /// <summary>
        /// Get resource
        /// </summary>
        /// <param name="resourceGroup"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        Task<IServiceBusResource> GetAsync(
            IResourceGroupResource resourceGroup, string name);
    }
}
