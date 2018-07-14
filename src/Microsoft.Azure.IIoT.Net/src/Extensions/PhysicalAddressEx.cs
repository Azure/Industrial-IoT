// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Net {
    using System.Net.NetworkInformation;

    public static class PhysicalAddressEx {

        /// <summary>
        /// Clone address
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static PhysicalAddress Copy(this PhysicalAddress address) =>
            address == null ? null : new PhysicalAddress(address.GetAddressBytes());

        /// <summary>
        /// Is empty
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static bool IsEmpty(this PhysicalAddress address) =>
            address == null || address.Equals(PhysicalAddress.None);

    }
}
