// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System.Net {
    using System.Net.Sockets;
    using System.Threading.Tasks;

    /// <summary>
    /// Endpoint extensions
    /// </summary>
    public static class EndPointEx {

        /// <summary>
        /// Get ip address from endpoint if the endpoint is an
        /// IPEndPoint.  Otherwise return null.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="preferv4"></param>
        /// <returns></returns>
        public static IPAddress GetIPAddress(this EndPoint endpoint,
            bool preferv4 = false) {
            if (endpoint is IPEndPoint ipe) {
                var address = ipe.Address;
                if (preferv4 &&
                    address.AddressFamily == AddressFamily.InterNetworkV6 &&
                    address.IsIPv4MappedToIPv6) {
                    return address.MapToIPv4();
                }
                return address;
            }
            return null;
        }

        /// <summary>
        /// Get port from endpoint if the endpoint is an
        /// IPEndPoint.  Otherwise return -1.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        public static int GetPort(this EndPoint endpoint) {
            if (endpoint is IPEndPoint ipe) {
                return ipe.Port;
            }
            return -1;
        }

        /// <summary>
        /// Resolve endpoint to host:port or throw.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        public static string Resolve(this EndPoint endpoint) {
            var entry = endpoint.GetIPAddress().GetHostEntry();
            return $"{entry.HostName}:{endpoint.GetPort()}";
        }

        /// <summary>
        /// Resolve endpoint to host:port or throw.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        public static async Task<string> ResolveAsync(this EndPoint endpoint) {
            var entry = await endpoint.GetIPAddress().GetHostEntryAsync();
            return $"{entry.HostName}:{endpoint.GetPort()}";
        }

        /// <summary>
        /// Resolve endpoint to host:port or return address:port as
        /// string if resolve fails
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        public static string TryResolve(this EndPoint endpoint) {
            if (endpoint == null) {
                return null;
            }
            try {
                return endpoint.Resolve();
            }
            catch {
                return $"{endpoint.GetIPAddress(true)}:{endpoint.GetPort()}";
            }
        }

        /// <summary>
        /// Resolve endpoint to host:port or return address:port as
        /// string if resolve fails
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        public static async Task<string> TryResolveAsync(this EndPoint endpoint) {
            if (endpoint == null) {
                return null;
            }
            try {
                return await endpoint.ResolveAsync();
            }
            catch {
                return $"{endpoint.GetIPAddress(true)}:{endpoint.GetPort()}";
            }
        }
    }
}
