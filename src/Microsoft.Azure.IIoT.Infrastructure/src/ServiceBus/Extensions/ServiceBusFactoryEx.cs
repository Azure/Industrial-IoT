// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Infrastructure.ServiceBus {
    using Microsoft.Azure.IIoT.Utils;
    using System.Threading.Tasks;

    public static class ServiceBusFactoryEx {

        /// <summary>
        /// Create a new randomly named service bus namespace
        /// </summary>
        public static Task<IServiceBusResource> CreateAsync(
            this IServiceBusFactory service, IResourceGroupResource resourceGroup) =>
            service.CreateAsync(resourceGroup, null);

        /// <summary>
        /// Get or create new service bus namespace in a resource group.
        /// </summary>
        /// <param name="resourceGroup"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static async Task<IServiceBusResource> GetOrCreateAsync(
            this IServiceBusFactory service, IResourceGroupResource resourceGroup,
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
