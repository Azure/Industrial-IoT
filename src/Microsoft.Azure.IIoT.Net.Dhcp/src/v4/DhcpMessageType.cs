// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Net.Dhcp.v4 {

    /// <summary>
    /// Dhcp message type
    /// </summary>
    public enum DhcpMessageType : byte {

        Undefined,

        /// <summary>
        /// Client broadcast to locate available servers.
        /// </summary>
        Discover,

        /// <summary>
        /// Server to client in response to DHCPDISCOVER with
        /// offer of configuration parameters.
        /// </summary>
        Offer,

        /// <summary>
        /// Client message to servers either (a) requesting
        /// offered parameters from one server and implicitly
        /// declining offers from all others, (b) confirming
        /// correctness of previously allocated address after,
        /// e.g., system reboot, or (c) extending the lease on a
        /// particular network address.
        /// </summary>
        Request,

        /// <summary>
        /// Client to server indicating network address is already
        /// in use.
        /// </summary>
        Decline,

        /// <summary>
        /// Server to client with configuration parameters,
        /// including committed network address.
        /// </summary>
        Ack,

        /// <summary>
        /// Server to client indicating client's notion of network
        /// address is incorrect (e.g., client has moved to new
        /// subnet) or client's lease as expired
        /// </summary>
        Nak,

        /// <summary>
        /// Client to server relinquishing network address and
        /// cancelling remaining lease.
        /// </summary>
        Release,

        /// <summary>
        /// Client to server, asking only for local configuration
        /// parameters; client already has externally configured
        /// network address.
        /// </summary>
        Inform,

        ForceRenew,
        LeaseQuery,
        LeaseUnassigned,
        LeaseUnknown,
        LeaseActive
    }
}
