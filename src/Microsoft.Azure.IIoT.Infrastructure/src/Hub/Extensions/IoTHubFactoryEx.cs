// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Infrastructure.Hub {
    using System.Threading.Tasks;

    public static class IoTHubFactoryEx {

        /// <summary>
        /// Create a new iot hub in a resource group.
        /// </summary>
        public static Task<IIoTHubResource> CreateAsync(this IIoTHubFactory service,
            IResourceGroupResource resourceGroup) =>
            service.CreateAsync(resourceGroup, null);
    }
}
