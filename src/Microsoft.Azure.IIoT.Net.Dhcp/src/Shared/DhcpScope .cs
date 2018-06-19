// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Net.Dhcp.Shared {
    using Microsoft.Azure.IIoT.Net.Dhcp;
    using Microsoft.Azure.IIoT.Net.Models;
    using Microsoft.Azure.IIoT.Diagnostics;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.NetworkInformation;
    using System.Threading;

    /// <summary>
    /// Dhcp server scope to manage leases of addresses
    /// in the network subnet.
    /// </summary>
    public class DhcpScope : IDhcpScope {

        /// <summary>
        /// The network interface the dhcp server context
        /// is bound to.
        /// </summary>
        public NetInterface Interface { get; set; }

        /// <summary>
        /// Assigned leases.
        /// </summary>
        internal IEnumerable<DhcpLease> Assigned {
            get {
                _lock.Wait();
                try {
                    return _assigned.Values
                        .ToList();
                }
                finally {
                    _lock.Release();
                }
            }
        }

        /// <summary>
        /// Available addresses.
        /// </summary>
        internal IEnumerable<IPAddress> Available {
            get {
                _lock.Wait();
                try {
                    return _available.Values
                        .Select(x => x.Identifier.AssignedAddress)
                        .ToList();
                }
                finally {
                    _lock.Release();
                }
            }
        }

        /// <summary>
        /// Create subnet context and network interface binding
        /// </summary>
        /// <param name="itf"></param>
        internal DhcpScope(NetInterface itf, int offsetBottom, int offsetTop,
            ILogger logger, HashSet<IPAddress> reserved = null) {
            Interface = itf ?? throw new ArgumentNullException(nameof(itf));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            var range = new AddressRange(itf);
            for (var addr = range.Low + offsetBottom + 1;
                    addr <= range.High - offsetTop - 1; addr++) {

                var assignable = (IPv4Address)addr;
                if (reserved != null && reserved.Contains(assignable)) {
                    // Exclude all reserved addresses from the range
                    continue;
                }
                if (assignable.Equals(itf.UnicastAddress) ||
                    assignable.Equals(itf.Gateway)) {
                    // Do not assign our address or the gateway address
                    continue;
                }
                _available.Add(assignable, new DhcpLease(assignable, null, false));
            }
        }

        /// <summary>
        /// Confirm a reserved lease
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="address"></param>
        /// <param name="transactionId"></param>
        /// <param name="leaseDuration"></param>
        /// <returns></returns>
        public DhcpLease Commit(PhysicalAddress clientId, IPAddress address,
            uint transactionId, TimeSpan leaseDuration) {
            _lock.Wait();
            try {
                if (_assigned.TryGetValue(new DhcpLease.Id(address, clientId),
                    out var lease)) {
                    if (lease.TransactionId == transactionId) {

                        lease.Expiry = DateTime.Now.Add(leaseDuration);
                        lease.Accepted = true;
                        return lease.Copy();
                    }
                }
                return null;
            }
            finally {
                _lock.Release();
            }
        }

        /// <summary>
        /// Release address
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="address"></param>
        public bool Release(PhysicalAddress clientId, IPAddress address) {
            _lock.Wait();
            try {
                if (_assigned.TryGetValue(new DhcpLease.Id(address, clientId),
                    out var lease)) {
                    _assigned.Remove(lease.Identifier);

                    if (!lease.External) {
                        _available.Add(lease.Identifier.AssignedAddress, lease);
                    }
                    return true;
                }
                return false;
            }
            finally {
                _lock.Release();
            }
        }

        /// <summary>
        /// Reserve a lease that some other server has assigned to the client,
        /// creates external one if it is not in our range and adds it to
        /// assigned.
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="transactionId"></param>
        /// <param name="address"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public void Reserve(PhysicalAddress clientId, uint transactionId,
            IPAddress address, TimeSpan timeout) {
            _lock.Wait();
            try {
                var id = new DhcpLease.Id(address, clientId);
                if (!_assigned.TryGetValue(id, out var lease)) {
                    if (!_available.TryGetValue(id.AssignedAddress, out lease)) {
                        // Create external lease
                        lease = new DhcpLease(id);
                    }
                    _assigned.Add(id, lease);
                }
                lease.TransactionId = transactionId;
                lease.Expiry = DateTime.Now.Add(timeout);
                lease.External = true;
                lease.Accepted = true;
            }
            finally {
                _lock.Release();
            }
        }

        /// <summary>
        /// Shelf the address because it is already in use, e.g. through
        /// static assignement and the client has declined it or the
        /// server found through icmp echo that it is already assigned
        /// after it was allocated.
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="address"></param>
        public bool Shelve(PhysicalAddress clientId, IPAddress address,
            TimeSpan shelfLife) {
            _lock.Wait();
            try {
                var id = new DhcpLease.Id(address, clientId);
                if (!_assigned.TryGetValue(id, out var lease)) {
                    return _available.Remove(address);
                }
                lease.External = true;
                lease.Accepted = true;
                lease.TransactionId = 0;
                lease.Expiry = DateTime.Now.Add(shelfLife);
                return true;
            }
            finally {
                _lock.Release();
            }
        }

        /// <summary>
        /// Trim context - returns true if context is active
        /// </summary>
        public bool Trim() {
            try {
                _lock.Wait();
                foreach (var lease in _assigned.Values.ToList()) {
                    if (DateTime.Now < lease.Expiry) {
                        // Not yet expired
                        continue;
                    }

                    _assigned.Remove(lease.Identifier);

                    lease.Identifier = lease.Identifier.TakeOwnership(null);
                    lease.Accepted = false;
                    if (!lease.External) {
                        _available.Add(lease.Identifier.AssignedAddress, lease);
                    }
                }
                return _assigned.Count == 0;
            }
            finally {
                _lock.Release();
            }
        }

        /// <summary>
        /// Allocate a lease with specified timeout
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="address"></param>
        /// <param name="transactionId"></param>
        /// <param name="offerDuration"></param>
        /// <returns></returns>
        public DhcpLease Allocate(PhysicalAddress clientId, IPAddress address,
            uint transactionId, TimeSpan offerDuration) {
            var lease = Allocate(clientId, address);
            if (lease != null) {
                lease.TransactionId = transactionId;
                lease.Expiry = DateTime.Now.Add(offerDuration);
                lease.Accepted = false;
            }
            return lease?.Copy();
        }

        /// <summary>
        /// Allocate or return an address lease
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="address"></param>
        /// <returns></returns>
        private DhcpLease Allocate(PhysicalAddress clientId, IPAddress address) {
            _lock.Wait();
            try {
                if (address != null) {
                    // First check whether address is already allocated to the client
                    var id = new DhcpLease.Id(address, clientId);
                    if (_assigned.TryGetValue(id, out var assigned)) {
                        return assigned;
                    }
                }

                // Try to get client the requested address if not allocated yet
                if (address == null || !_available.TryGetValue(address, out var lease)) {
                    // Allocate any free address
                    lease = _available.Values.FirstOrDefault();
                }

                if (lease != null) {
                    _available.Remove(lease.Identifier.AssignedAddress);
                    lease.Identifier = lease.Identifier.TakeOwnership(clientId);
                    _assigned.Add(lease.Identifier, lease);
                }

                return lease;
            }
            finally {
                _lock.Release();
            }
        }

        private readonly Dictionary<DhcpLease.Id, DhcpLease> _assigned =
            new Dictionary<DhcpLease.Id, DhcpLease>();
        private readonly SortedList<IPAddress, DhcpLease> _available =
            new SortedList<IPAddress, DhcpLease>(new IPv4Address(0));
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1);
        private readonly ILogger _logger;
    }
}
