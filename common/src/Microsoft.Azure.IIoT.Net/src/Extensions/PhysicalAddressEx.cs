// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Net {
    using System.Net.NetworkInformation;

    /// <summary>
    /// Physical address extensions
    /// </summary>
    public static class PhysicalAddressEx {

        /// <summary>
        /// Clone address
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static PhysicalAddress Copy(this PhysicalAddress address) {
            return address == null ? null : new PhysicalAddress(address.GetAddressBytes());
        }

        /// <summary>
        /// Is empty
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static bool IsEmpty(this PhysicalAddress address) {
            return address == null || address.Equals(PhysicalAddress.None);
        }
    }
}
