// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Net.Models {
    using System.Collections.Generic;
    using System.Net;
    using System.Net.NetworkInformation;

    /// <summary>
    /// Network interface model
    /// </summary>
    public sealed class NetInterface {

        /// <summary>
        /// Network interface name
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Address info
        /// </summary>
        public IPAddress UnicastAddress { get; }

        /// <summary>
        /// Subnet mask
        /// </summary>
        public IPAddress SubnetMask { get; }

        /// <summary>
        /// Mac address of interface
        /// </summary>
        public PhysicalAddress MacAddress { get; }

        /// <summary>
        /// Gateway address
        /// </summary>
        public IPAddress Gateway { get; }

        /// <summary>
        /// Domain name
        /// </summary>
        public string DnsSuffix { get; }

        /// <summary>
        /// Name servers
        /// </summary>
        public IEnumerable<IPAddress> DnsServers { get; }

        /// <summary>
        /// Create interface address
        /// </summary>
        /// <param name="name"></param>
        /// <param name="unicastAddress"></param>
        /// <param name="subnetMask"></param>
        public NetInterface(string name,
            IPAddress unicastAddress, IPAddress subnetMask) {
            Name = name;
            UnicastAddress = unicastAddress;
            SubnetMask = subnetMask;
        }

        /// <summary>
        /// Create context
        /// </summary>
        public NetInterface(string name, PhysicalAddress macAddress,
            IPAddress unicastAddress, IPAddress subnetMask, IPAddress gateway,
            string dnsSuffix, IEnumerable<IPAddress> dnsServers) :
                this(name, unicastAddress, subnetMask) {

            Gateway = gateway;
            MacAddress = macAddress;
            DnsServers = dnsServers;
            DnsSuffix = dnsSuffix;
        }

        /// <summary>
        /// Equality
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj) {
            return obj is NetInterface context &&
                EqualityComparer<PhysicalAddress>.Default.Equals(
                    MacAddress, context.MacAddress) &&
                EqualityComparer<IPAddress>.Default.Equals(
                    UnicastAddress, context.UnicastAddress) &&
                EqualityComparer<IPAddress>.Default.Equals(
                    SubnetMask, context.SubnetMask) &&
                EqualityComparer<IPAddress>.Default.Equals(
                    Gateway, context.Gateway) &&
                // EqualityComparer<IPAddress>.Default.Equals(
                //     DnsServers, context.DnsServers) &&
                EqualityComparer<string>.Default.Equals(
                    DnsSuffix, context.DnsSuffix);
        }

        /// <summary>
        /// Get unique hash code
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode() {
            var hashCode = 1761702729;
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<PhysicalAddress>.Default.GetHashCode(MacAddress);
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<IPAddress>.Default.GetHashCode(UnicastAddress);
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<IPAddress>.Default.GetHashCode(SubnetMask);
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<IPAddress>.Default.GetHashCode(Gateway);
            // hashCode = hashCode * -1521134295 +
            //     EqualityComparer<DnsServers>.Default.GetHashCode(DnsServers);
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<string>.Default.GetHashCode(DnsSuffix);
            return hashCode;
        }
    }
}
