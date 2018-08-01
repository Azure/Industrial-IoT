// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Infrastructure.Hub {
    using Microsoft.Azure.IIoT.Utils;
    using System.Threading.Tasks;

    public static class IoTHubFactoryEx {

        /// <summary>
        /// Create a new iot hub in a resource group.
        /// </summary>
        public static Task<IIoTHubResource> CreateAsync(this IIoTHubFactory service,
            IResourceGroupResource resourceGroup) =>
            service.CreateAsync(resourceGroup, null);

        /// <summary>
        /// Get or create new hub in a resource group.
        /// </summary>
        /// <param name="resourceGroup"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static async Task<IIoTHubResource> GetOrCreateAsync(
            this IIoTHubFactory service, IResourceGroupResource resourceGroup,
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
