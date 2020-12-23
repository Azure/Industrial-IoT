// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System.Net {
    using Microsoft.Azure.IIoT.Net.Models;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Ip address extensions
    /// </summary>
    public static class IPAddressEx {

        /// <summary>
        /// Is empty
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static bool IsEmpty(this IPAddress address) {
            return address == null ||
address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any) ||
address.Equals(IPAddress.None) || address.Equals(IPAddress.IPv6None);
        }

        /// <summary>
        /// Clone address as v4 address
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static IPv4Address AsV4(this IPAddress address) {
            return address == null ? null : new IPv4Address(address.GetAddressBytes());
        }

        /// <summary>
        /// Clone address
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static IPAddress Copy(this IPAddress address) {
            return address == null ? null : new IPAddress(address.GetAddressBytes());
        }

        /// <summary>
        /// Resolve address to host entry
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static IPHostEntry GetHostEntry(this IPAddress address) {
            return Dns.GetHostEntry(address);
        }

        /// <summary>
        /// Resolve address to host
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static Task<IPHostEntry> GetHostEntryAsync(this IPAddress address) {
            return Dns.GetHostEntryAsync(address);
        }

        /// <summary>
        /// Resolve address to host
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static string Resolve(this IPAddress address) {
            return address.GetHostEntry().HostName;
        }

        /// <summary>
        /// Resolve address to host
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static async Task<string> ResolveAsync(this IPAddress address) {
            var entry = await address.GetHostEntryAsync();
            return entry.HostName;
        }

        /// <summary>
        /// Resolve address to host or return address as
        /// string of resolve fails
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static string TryResolve(this IPAddress address) {
            if (address == null) {
                return null;
            }
            try {
                return address.Resolve();
            }
            catch {
                return address.ToString();
            }
        }

        /// <summary>
        /// Resolve address to host or return address as
        /// string of resolve fails
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static async Task<string> TryResolveAsync(this IPAddress address) {
            if (address == null) {
                return null;
            }
            try {
                return await address.ResolveAsync();
            }
            catch {
                return address.ToString();
            }
        }

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
            public int Compare(IPAddress x, IPAddress y) {
                return y.AsV4().CompareTo(x);
            }
        }

        /// <summary>
        /// Ascending comparer implementation
        /// </summary>
        private class AscendingComparer : IComparer<IPAddress> {
            public int Compare(IPAddress x, IPAddress y) {
                return x.AsV4().CompareTo(y);
            }
        }
    }
}
