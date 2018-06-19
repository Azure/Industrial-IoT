// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Net.Dhcp.Shared {
    using System.Collections.Generic;
    using System.Net.NetworkInformation;
    using System;
    using System.Net;

    /// <summary>
    /// Represents a lease
    /// </summary>
    public class DhcpLease {

        ///
        /// The combination of 'client identifier' or 'chaddr' and assigned
        /// network address constitute a unique identifier for the client's
        /// lease and are used by both the client and server to identify a
        /// lease referred to in any DHCP messages.
        ///
        public class Id {

            /// <summary>
            /// Create lease id
            /// </summary>
            /// <param name="address"></param>
            /// <param name="clientId"></param>
            public Id(IPAddress address, PhysicalAddress clientId = null) {
                ClientId = clientId;
                AssignedAddress = address;
            }

            /// <summary>
            /// Owner of the lease
            /// </summary>
            public PhysicalAddress ClientId { get; }

            /// <summary>
            /// Assigned address
            /// </summary>
            public IPAddress AssignedAddress { get; }

            /// <inheritdoc/>
            public override bool Equals(object obj) {
                return obj is Id identifier &&
                    EqualityComparer<PhysicalAddress>.Default.Equals(
                        ClientId, identifier.ClientId) &&
                    EqualityComparer<IPAddress>.Default.Equals(
                        AssignedAddress, identifier.AssignedAddress);
            }

            /// <inheritdoc/>
            public override int GetHashCode() {
                var hashCode = -680090833;
                hashCode = hashCode * -1521134295 +
                    EqualityComparer<PhysicalAddress>.Default.GetHashCode(ClientId);
                hashCode = hashCode * -1521134295 +
                    EqualityComparer<IPAddress>.Default.GetHashCode(AssignedAddress);
                return hashCode;
            }

            internal Id TakeOwnership(PhysicalAddress clientId) =>
                new Id(AssignedAddress, clientId.Copy());

            internal Id Copy() =>
                new Id(AssignedAddress.Copy(), ClientId.Copy());
        }

        /// <summary>
        /// Create lease
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="external"></param>
        public DhcpLease(Id identifier, bool external = true) {
            Identifier = identifier ??
                throw new ArgumentNullException(nameof(identifier));
            External = external;
        }

        /// <summary>
        /// Create lease
        /// </summary>
        /// <param name="address"></param>
        /// <param name="clientId"></param>
        /// <param name="external"></param>
        public DhcpLease(IPAddress address,
            PhysicalAddress clientId = null, bool external = true) :
            this(new Id(address, clientId), external) {
        }

        /// <summary>
        /// Lease identifier
        /// </summary>
        public Id Identifier { get; set; }

        /// <summary>
        /// Whether this is an unmanaged lease.
        /// </summary>
        public bool External { get; set; }

        /// <summary>
        /// Lease expiry
        /// </summary>
        public DateTime Expiry { get; set; }

        /// <summary>
        /// Transaction id of the original offer
        /// </summary>
        public uint TransactionId { get; set; }

        /// <summary>
        /// Whether lease was acked
        /// </summary>
        public bool Accepted { get; set; }

        /// <summary>
        /// Clone
        /// </summary>
        /// <returns></returns>
        internal DhcpLease Copy() =>
            new DhcpLease(Identifier.Copy(), External) {
                Expiry = Expiry,
                TransactionId = TransactionId,
                Accepted = Accepted
            };

        /// <inheritdoc/>
        public override bool Equals(object obj) {
            return obj is DhcpLease lease &&
                EqualityComparer<Id>.Default.Equals(
                    Identifier, lease.Identifier) &&
                External == lease.External &&
                Expiry == lease.Expiry &&
                TransactionId == lease.TransactionId &&
                Accepted == lease.Accepted;
        }

        /// <inheritdoc/>
        public override int GetHashCode() {
            var hashCode = -15211660;
            hashCode = hashCode * -1521134295 +
                EqualityComparer<Id>.Default.GetHashCode(Identifier);
            hashCode = hashCode * -1521134295 +
                External.GetHashCode();
            hashCode = hashCode * -1521134295 +
                Expiry.GetHashCode();
            hashCode = hashCode * -1521134295 +
                TransactionId.GetHashCode();
            hashCode = hashCode * -1521134295 +
                Accepted.GetHashCode();
            return hashCode;
        }
    }
}
