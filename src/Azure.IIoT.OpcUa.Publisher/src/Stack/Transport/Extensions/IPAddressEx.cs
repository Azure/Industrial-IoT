// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System.Net
{
    using Azure.IIoT.OpcUa.Publisher.Stack.Transport.Models;
    using System.Threading.Tasks;

    /// <summary>
    /// Ip address extensions
    /// </summary>
    public static class IPAddressEx
    {
        /// <summary>
        /// Clone address as v4 address
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static IPv4Address AsV4(this IPAddress address)
        {
            return new IPv4Address(address.GetAddressBytes());
        }

        /// <summary>
        /// Resolve address to host entry
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static IPHostEntry GetHostEntry(this IPAddress address)
        {
            return Dns.GetHostEntry(address);
        }

        /// <summary>
        /// Resolve address to host
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static Task<IPHostEntry> GetHostEntryAsync(this IPAddress address)
        {
            return Dns.GetHostEntryAsync(address);
        }
    }
}
