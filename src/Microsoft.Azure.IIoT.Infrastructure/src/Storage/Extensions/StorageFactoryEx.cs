// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Infrastructure.Storage {
    using Microsoft.Azure.IIoT.Utils;
    using System.Threading.Tasks;

    /// <summary>
    /// Storage factory extensions
    /// </summary>
    public static class StorageFactoryEx {

        /// <summary>
        /// Create a new randomly named storage
        /// </summary>
        public static Task<IStorageResource> CreateAsync(
            this IStorageFactory service, IResourceGroupResource resourceGroup) {
            return service.CreateAsync(resourceGroup, null);
        }

        /// <summary>
        /// Get or create new storage in a resource group.
        /// </summary>
        /// <param name="service"></param>
        /// <param name="resourceGroup"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static async Task<IStorageResource> GetOrCreateAsync(
            this IStorageFactory service, IResourceGroupResource resourceGroup,
            string name) {
            var resource = await Try.Async(() => service.GetAsync(resourceGroup,
                name));
            if (resource == null) {
                resource = await service.CreateAsync(resourceGroup, name);
            }
            return resource;
        }
    }
}
