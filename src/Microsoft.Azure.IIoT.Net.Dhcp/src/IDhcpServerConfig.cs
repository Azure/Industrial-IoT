// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Net.Dhcp {
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.NetworkInformation;

    /// <summary>
    /// Dhcp server configuration options.
    /// </summary>
    public interface IDhcpServerConfig {

        /// <summary>
        /// If true then only monitor dhcp and do not
        /// serve any addresses.
        /// </summary>
        bool ListenOnly { get; }

        /// <summary>
        /// Timeout of any offers
        /// </summary>
        TimeSpan OfferTimeout { get; }

        /// <summary>
        /// Duration of leases
        /// </summary>
        TimeSpan LeaseDuration { get; }

        /// <summary>
        /// Offset into the network interface address
        /// range from where to assign addresses
        /// </summary>
        int AddressRangeOffsetBottom { get; }

        /// <summary>
        /// Offset from the top of the network interface
        /// address range from where to assign addresses.
        /// </summary>
        int AddressRangeOffsetTop { get; }

        /// <summary>
        /// Dns servers
        /// </summary>
        List<IPAddress> DnsServers { get; }

        /// <summary>
        /// Dns suffix
        /// </summary>
        string DnsSuffix { get; }

        /// <summary>
        /// Allow only clients with entries in the reservations table
        /// to be served.
        /// </summary>
        bool DisableAutoAssignment { get; }

        /// <summary>
        /// Reservations and optionally whitelist of clients that are
        /// allowed to connect.  If the ip address is null, an address
        /// from the range is assigned.
        /// </summary>
        Dictionary<PhysicalAddress, IPAddress> StaticAssignment { get; }
    }
}
