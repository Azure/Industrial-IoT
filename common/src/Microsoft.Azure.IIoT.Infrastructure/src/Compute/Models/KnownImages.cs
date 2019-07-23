// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Infrastructure.Compute {

    /// <summary>
    /// Constants for known images
    /// </summary>
    public static class KnownImages {

        /// <summary>
        /// Ubuntu 14.04
        /// </summary>
        public static readonly VirtualMachineImage Ubuntu_14_04_lts =
            new VirtualMachineImage {
                Offer = "UbuntuServer",
                Publisher = "Canonical",
                Sku = "14.04-LTS",
                IsLinux = true
            };

        /// <summary>
        /// Ubuntu 16.04
        /// </summary>
        public static readonly VirtualMachineImage Ubuntu_16_04_lts =
            new VirtualMachineImage {
                Offer = "UbuntuServer",
                Publisher = "Canonical",
                Sku = "16.04-LTS",
                IsLinux = true
            };

        /// <summary>
        /// Ubuntu 18.04
        /// </summary>
        public static readonly VirtualMachineImage Ubuntu_18_04_lts =
            new VirtualMachineImage {
                Offer = "UbuntuServer",
                Publisher = "Canonical",
                Sku = "18.04-LTS",
                IsLinux = true
            };
    }
}
