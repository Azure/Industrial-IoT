// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Infrastructure.Compute {

    public static class KnownImages {

        public static readonly VirtualMachineImage Ubuntu_14_04_lts =
            new VirtualMachineImage {
                Offer = "UbuntuServer",
                Publisher = "Canonical",
                Sku = "14.04-LTS",
                IsLinux = true
            };

        public static readonly VirtualMachineImage Ubuntu_16_04_lts =
            new VirtualMachineImage {
                Offer = "UbuntuServer",
                Publisher = "Canonical",
                Sku = "16.04-LTS",
                IsLinux = true
            };

        public static readonly VirtualMachineImage Ubuntu_18_04_lts =
            new VirtualMachineImage {
                Offer = "UbuntuServer",
                Publisher = "Canonical",
                Sku = "18.04-LTS",
                IsLinux = true
            };
    }
}
