// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Infrastructure.Storage {
    using System.Threading.Tasks;

    /// <summary>
    /// Manages storage resources
    /// </summary>
    public interface IStorageFactory {

        /// <summary>
        /// Create storage
        /// </summary>
        /// <param name="resourceGroup"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        Task<IStorageResource> CreateAsync(
            IResourceGroupResource resourceGroup, string name);

        /// <summary>
        /// Get resource
        /// </summary>
        /// <param name="resourceGroup"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        Task<IStorageResource> GetAsync(
            IResourceGroupResource resourceGroup, string name);
    }
}
