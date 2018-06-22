// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Net.Dhcp.Shared {
    using Microsoft.Azure.IIoT.Net.Dhcp;
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.NetworkInformation;

    /// <summary>
    /// Dhcp server configuration defaults.
    /// </summary>
    public class DhcpServerDefaults : IDhcpServerConfig {

        /// <inheritdoc/>
        public bool ListenOnly { get; set; }

        /// <inheritdoc/>
        public TimeSpan OfferTimeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <inheritdoc/>
        public TimeSpan LeaseDuration { get; set; } = TimeSpan.FromDays(1);

        /// <inheritdoc/>
        public int AddressRangeOffsetBottom { get; set; }

        /// <inheritdoc/>
        public int AddressRangeOffsetTop { get; set; }

        /// <inheritdoc/>
        public bool AssignDescending { get; set; }

        /// <inheritdoc/>
        public List<IPAddress> DnsServers { get; set; }

        /// <inheritdoc/>
        public string DnsSuffix { get; set; }

        /// <inheritdoc/>
        public bool DisableAutoAssignment { get; set; }

        /// <inheritdoc/>
        public Dictionary<PhysicalAddress, IPAddress> StaticAssignment { get; set; } =
            new Dictionary<PhysicalAddress, IPAddress>();
    }
}
