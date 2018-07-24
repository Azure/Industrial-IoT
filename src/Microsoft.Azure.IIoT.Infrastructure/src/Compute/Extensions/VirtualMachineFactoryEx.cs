// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Infrastructure.Compute {
    using System.Threading.Tasks;

    public static class VirtualMachineFactoryEx {
        /// <summary>
        /// Create a new virtual machine
        /// </summary>
        public static Task<IVirtualMachineResource> CreateAsync(
            this IVirtualMachineFactory service, IResourceGroupResource resourceGroup,
            string name = null) => service.CreateAsync(resourceGroup, name, null);
    }
}
