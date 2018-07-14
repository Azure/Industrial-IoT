// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System.Net {
    using Microsoft.Azure.IIoT.Net.Models;
    using System.Collections.Generic;

    public static class IPAddressEx {

        /// <summary>
        /// Is empty
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static bool IsEmpty(this IPAddress address) =>
            address == null ||
            address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any) ||
            address.Equals(IPAddress.None) || address.Equals(IPAddress.IPv6None);

        /// <summary>
        /// Clone address as v4 address
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static IPv4Address AsV4(this IPAddress address) =>
            address == null ? null : new IPv4Address(address.GetAddressBytes());

        /// <summary>
        /// Clone address
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static IPAddress Copy(this IPAddress address) =>
            address == null ? null : new IPAddress(address.GetAddressBytes());


        /// <summary>
        /// Returns descending comparer for addresses
        /// </summary>
        public static IComparer<IPAddress> Descending =>
            new DescendingComparer();

        /// <summary>
        /// Returns ascending comparer for addresses
        /// </summary>
        public static IComparer<IPAddress> Ascending =>
            new AscendingComparer();

        /// <summary>
        /// Ascending comparer implementation
        /// </summary>
        private class DescendingComparer : IComparer<IPAddress> {
            public int Compare(IPAddress x, IPAddress y) =>
                y.AsV4().CompareTo(x);
        }

        /// <summary>
        /// Ascending comparer implementation
        /// </summary>
        private class AscendingComparer : IComparer<IPAddress> {
            public int Compare(IPAddress x, IPAddress y) =>
                x.AsV4().CompareTo(y);
        }
    }
}
