// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Infrastructure.Compute {
    using Microsoft.Azure.IIoT.Infrastructure.Network;
    using Microsoft.Azure.IIoT.Utils;
    using System.Threading.Tasks;

    /// <summary>
    /// Virtual machine factory extensions
    /// </summary>
    public static class VirtualMachineFactoryEx {

        /// <summary>
        /// Create a new linux virtual machine
        /// </summary>
        public static Task<IVirtualMachineResource> CreateAsync(
            this IVirtualMachineFactory service, IResourceGroupResource resourceGroup,
            string name = null, INetworkResource network = null, string customData = null) {
            return service.CreateAsync(resourceGroup, name, network, null, customData);
        }

        /// <summary>
        /// Create a new linux virtual machine
        /// </summary>
        public static Task<IVirtualMachineResource> CreateAsync(
            this IVirtualMachineFactory service, IResourceGroupResource resourceGroup,
            string name, VirtualMachineImage image) {
            return service.CreateAsync(resourceGroup, name, null, image, null);
        }

        /// <summary>
        /// Create ubuntu server 18.04 LTS vm
        /// </summary>
        public static Task<IVirtualMachineResource> CreateBionicAsync(
            this IVirtualMachineFactory service, IResourceGroupResource resourceGroup,
            string name = null, INetworkResource network = null, string customData = null) {
            return service.CreateAsync(resourceGroup, name, network, KnownImages.Ubuntu_18_04_lts,
                customData);
        }

        /// <summary>
        /// Create ubuntu server 16.04 LTS vm
        /// </summary>
        public static Task<IVirtualMachineResource> CreateXenialAsync(
            this IVirtualMachineFactory service, IResourceGroupResource resourceGroup,
            string name = null, INetworkResource network = null, string customData = null) {
            return service.CreateAsync(resourceGroup, name, network, KnownImages.Ubuntu_16_04_lts,
                customData);
        }

        /// <summary>
        /// Get or create new vm in a resource group.
        /// </summary>
        /// <param name="service"></param>
        /// <param name="resourceGroup"></param>
        /// <param name="name"></param>
        /// <param name="network"></param>
        /// <param name="image"></param>
        /// <returns></returns>
        public static async Task<IVirtualMachineResource> GetOrCreateAsync(
            this IVirtualMachineFactory service, IResourceGroupResource resourceGroup,
            string name, INetworkResource network = null, VirtualMachineImage image = null) {
            var resource = await Try.Async(() => service.GetAsync(resourceGroup, name));
            if (resource == null) {
                resource = await service.CreateAsync(resourceGroup, name, network, image, null);
            }
            return resource;
        }

        /// <summary>
        /// Get or create new ubuntu server 16.04 LTS vm in a resource group.
        /// </summary>
        /// <param name="service"></param>
        /// <param name="resourceGroup"></param>
        /// <param name="name"></param>
        /// <param name="network"></param>
        /// <returns></returns>
        public static Task<IVirtualMachineResource> GetOrCreateXenialAsync(
            this IVirtualMachineFactory service, IResourceGroupResource resourceGroup,
            string name, INetworkResource network = null) {
            return service.GetOrCreateAsync(resourceGroup, name, network, KnownImages.Ubuntu_16_04_lts);
        }

        /// <summary>
        /// Get or create new ubuntu server 16.04 LTS vm in a resource group.
        /// </summary>
        /// <param name="service"></param>
        /// <param name="resourceGroup"></param>
        /// <param name="name"></param>
        /// <param name="network"></param>
        /// <returns></returns>
        public static Task<IVirtualMachineResource> GetOrCreateBionicAsync(
            this IVirtualMachineFactory service, IResourceGroupResource resourceGroup,
            string name, INetworkResource network = null) {
            return service.GetOrCreateAsync(resourceGroup, name, network, KnownImages.Ubuntu_18_04_lts);
        }
    }
}
