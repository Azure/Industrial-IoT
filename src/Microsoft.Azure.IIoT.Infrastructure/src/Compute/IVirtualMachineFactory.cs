// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


namespace Microsoft.Azure.IIoT.Infrastructure.Compute {
    using System.Threading.Tasks;

    /// <summary>
    /// Manages virtual machine resources
    /// </summary>
    public interface IVirtualMachineFactory {

        /// <summary>
        /// Create resource
        /// </summary>
        /// <param name="resourceGroup"></param>
        /// <param name="name"></param>
        /// <param name="customData"></param>
        /// <returns></returns>
        Task<IVirtualMachineResource> CreateAsync(
            IResourceGroupResource resourceGroup, string name,
            string customData);

        /// <summary>
        /// Get resource
        /// </summary>
        /// <param name="resourceGroup"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        Task<IVirtualMachineResource> GetAsync(
            IResourceGroupResource resourceGroup, string name);

    }
}
