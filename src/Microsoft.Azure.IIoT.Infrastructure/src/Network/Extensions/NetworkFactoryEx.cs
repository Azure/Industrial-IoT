// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Infrastructure.Network {
    using Microsoft.Azure.IIoT.Utils;
    using System.Threading.Tasks;

    public static class StorageFactoryEx {

        /// <summary>
        /// Create a new randomly named storage
        /// </summary>
        public static Task<INetworkResource> CreateAsync(
            this INetworkFactory service, IResourceGroupResource resourceGroup) =>
            service.CreateAsync(resourceGroup, null, null, false);

        /// <summary>
        /// Get or create new storage in a resource group.
        /// </summary>
        /// <param name="resourceGroup"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static async Task<INetworkResource> GetOrCreateAsync(
            this INetworkFactory service, IResourceGroupResource resourceGroup,
            string name, string addressSpace = null, bool secure = false) {
            var resource = await Try.Async(() => service.GetAsync(resourceGroup,
                name));
            if (resource == null) {
                resource = await service.CreateAsync(resourceGroup, name,
                    addressSpace, secure);
            }
            return resource;
        }
    }
}
