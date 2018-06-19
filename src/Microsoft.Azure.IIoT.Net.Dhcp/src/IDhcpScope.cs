// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Net.Dhcp {
    using Microsoft.Azure.IIoT.Net.Dhcp.Shared;
    using Microsoft.Azure.IIoT.Net.Models;
    using System;
    using System.Net;
    using System.Net.NetworkInformation;

    /// <summary>
    /// Manage subnet addresses
    /// </summary>
    public interface IDhcpScope {

        /// <summary>
        /// Interface information for the subnet
        /// </summary>
        NetInterface Interface { get; set; }

        /// <summary>
        /// Allocate lease
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="address"></param>
        /// <param name="transactionId"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        DhcpLease Allocate(PhysicalAddress clientId, IPAddress address,
            uint transactionId, TimeSpan timeout);

        /// <summary>
        /// Commit allocation
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="address"></param>
        /// <param name="transactionId"></param>
        /// <param name="leaseDuration"></param>
        /// <returns></returns>
        DhcpLease Commit(PhysicalAddress clientId, IPAddress address,
            uint transactionId, TimeSpan leaseDuration);

        /// <summary>
        /// Release allocation when no more in use.
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="address"></param>
        /// <returns></returns>
        bool Release(PhysicalAddress clientId, IPAddress address);

        /// <summary>
        /// Reserve an address that was externally assigned e.g.
        /// statically or by another server.
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="transactionId"></param>
        /// <param name="address"></param>
        /// <param name="timeout"></param>
        void Reserve(PhysicalAddress clientId, uint transactionId,
            IPAddress address, TimeSpan timeout);

        /// <summary>
        /// Shelve an address for a particular amount of time.
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="address"></param>
        /// <param name="shelfLife"></param>
        /// <returns></returns>
        bool Shelve(PhysicalAddress clientId, IPAddress address,
            TimeSpan shelfLife);
    }
}