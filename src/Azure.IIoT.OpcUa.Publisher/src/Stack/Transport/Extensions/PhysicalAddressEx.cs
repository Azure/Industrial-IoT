// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Transport
{
    using System.Net.NetworkInformation;

    /// <summary>
    /// Physical address extensions
    /// </summary>
    public static class PhysicalAddressEx
    {
        /// <summary>
        /// Clone address
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static PhysicalAddress Copy(this PhysicalAddress address)
        {
            return new PhysicalAddress(address.GetAddressBytes());
        }
    }
}
