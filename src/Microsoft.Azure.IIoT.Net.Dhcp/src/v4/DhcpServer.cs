// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Net.Dhcp.v4 {
    using Microsoft.Azure.IIoT.Net.Dhcp.Shared;
    using Microsoft.Azure.IIoT.Net.Dhcp;
    using Microsoft.Azure.IIoT.Net.Bootp;
    using Microsoft.Azure.IIoT.Diagnostics;
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Net;

    /// <summary>
    /// Implementation of dhcp server protocol. Implements several operational
    /// modes using configuration:
    ///
    /// - Active: Full server - cooperatively service offers and requests
    /// - Manual: Only support static configuration and record network leases
    /// - Passive: Only listen and record conversations in the subnet context
    ///
    /// The host is responsiblé to provide subnet, message and responder context
    /// which allows for effective testing and composability.
    /// </summary>
    public class DhcpServer : IDhcpServer {

        /// <summary>
        /// Create server
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="configuration"></param>
        public DhcpServer(ILogger logger, IDhcpServerConfig configuration = null) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration;
        }


        /// <summary>
        /// Process message packet
        /// </summary>
        /// <param name="subnet"></param>
        /// <param name="packet"></param>
        /// <param name="length"></param>
        /// <param name="responder"></param>
        /// <returns></returns>
        public Task ProcessMessageAsync(IDhcpScope subnet, byte[] packet,
            int length, IDhcpResponder responder) {
            if (responder == null) {
                throw new ArgumentNullException(nameof(responder));
            }
            if (subnet == null) {
                throw new ArgumentNullException(nameof(subnet));
            }
            if (packet == null) {
                throw new ArgumentNullException(nameof(packet));
            }
            try {
                var message = DhcpMessage.Parse(packet, 0, length);
#if !LOG_VERBOSE
                _logger.Debug("RECEIVED DHCP REQUEST", () => message);
#endif
                return ProcessMessageAsync(subnet, message, responder);
            }
            catch (Exception ex) {
                _logger.Error("Error Parsing Dhcp Message", () => ex);
                return Task.CompletedTask;
            }
        }

        /// <summary>
        /// Process message
        /// </summary>
        /// <param name="message"></param>
        public Task ProcessMessageAsync(IDhcpScope subnet, DhcpMessage message,
            IDhcpResponder responder) {
            if (message.Op != BootpOpCode.BootRequest) {
                throw new FormatException($"Bad operation {message.Op}.");
            }
            var messageType = message.MessageType;
            if (!messageType.HasValue) {
                throw new FormatException("Bad data received, ignoring.");
            }
            switch (messageType.Value) {
                case DhcpMessageType.Discover:
                    return HandleDiscoverRequestAsync(subnet, message, responder);
                case DhcpMessageType.Request:
                    // Confirm and commit configuration
                    return HandleRequestMessageAsync(subnet, message, responder);
                case DhcpMessageType.Decline:
                    // Client found conflict, invalidate
                    return HandleDeclineMessageAsync(subnet, message);
                case DhcpMessageType.Release:
                    // Client is done - release address
                    return HandleReleaseMessageAsync(subnet, message);
                case DhcpMessageType.Inform:
                    // No op
                    break;
                case DhcpMessageType.Ack:
                case DhcpMessageType.Nak:
                case DhcpMessageType.Offer:
                    // Other server messages
                    break;
                default:
                    _logger.Warn($"Unknown Dhcp Message {messageType} " +
                        $"received. ignoring...", () => { });
                    break;
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Send response back to client.
        /// </summary>
        /// <param name="response"></param>
        /// <param name="address"></param>
        /// <param name="responder"></param>
        /// <returns></returns>
        private Task SendResponseAsync(DhcpMessage response,
            IPAddress address, IDhcpResponder responder) {

            if (address == null) {
                return Task.CompletedTask;
            }
#if !LOG_VERBOSE
            _logger.Debug("SEND DHCP RESPONSE", () => response);
#endif
            var packet = response.AsPacket();
            return responder.SendResponseAsync(packet, packet.Length, address);
        }

        /// <summary>
        /// If the client receives a DHCPNAK message, the client restarts the
        /// configuration process.
        /// The client times out and retransmits the DHCPREQUEST message if the
        /// client receives neither a DHCPACK or a DHCPNAK message.  The client
        /// retransmits the DHCPREQUEST.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="error"></param>
        private Task SendNakResponseAsync(DhcpMessage message, string error,
            IDhcpResponder responder) {

            var response = new DhcpMessage {
                MessageType = DhcpMessageType.Nak,

                Op = BootpOpCode.BootReply,
                Secs = message.Secs,
                Xid = message.Xid,
                Yiaddr = message.Yiaddr,
                Chaddr = message.Chaddr,

                Message = error ?? "ERR"
            };

            return SendResponseAsync(response, message.Yiaddr, responder);
        }

        /// <summary>
        /// The client may choose to relinquish its lease on a network address
        /// by sending a DHCPRELEASE message to the server.
        /// The client identifies the lease to be released with its 'client
        /// identifier', or 'chaddr' and network address.
        /// </summary>
        /// <param name="subnet"></param>
        /// <param name="message"></param>
        private Task HandleReleaseMessageAsync(IDhcpScope subnet,
            DhcpMessage message) {
            if (subnet.Release(message.ClientIdentifier,
                message.RequestedIPAddress)) {
                _logger.Debug("Lease released", () => { });
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// If the client detects that the address is already in use (e.g.
        /// through the use of ARP), the client MUST send a DHCPDECLINE message
        /// to the server and restarts the configuration process.  The client
        /// SHOULD wait a minimum of ten seconds before restarting the
        /// configuration process to avoid excessive network traffic in
        /// case of looping.
        /// </summary>
        /// <param name="subnet"></param>
        /// <param name="message"></param>
        private Task HandleDeclineMessageAsync(IDhcpScope subnet,
            DhcpMessage message) {
            if (subnet.Shelve(message.ClientIdentifier,
                message.RequestedIPAddress, _configuration.LeaseDuration)) {
                _logger.Debug("Lease declined", () => { });
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// The client broadcasts a DHCPDISCOVER message on its local physical
        /// subnet.The DHCPDISCOVER message MAY include options that suggest
        /// values for the network address and lease duration.BOOTP relay
        /// agents may pass the message on to DHCP servers not on the same
        /// physical subnet.
        /// </summary>
        /// <param name="subnet"></param>
        /// <param name="message"></param>
        private Task HandleDiscoverRequestAsync(IDhcpScope subnet,
            DhcpMessage message, IDhcpResponder responder) {
            if (_configuration?.ListenOnly ?? true) {
                // In passive mode, discard discover requests
                return Task.CompletedTask;
            }
            var clientId = message.ClientIdentifier;
            if (_configuration.StaticAssignment.TryGetValue(clientId,
                out var reserved)) {
                if (reserved != null) {
                    return SendOfferResponseAsync(subnet, message,
                        new DhcpLease(reserved, clientId) {
                            TransactionId = message.Xid,
                            Expiry = DateTime.Now.Add(_configuration.LeaseDuration)
                        }, responder);
                }
            }
            else if (_configuration.DisableAutoAssignment) {
                // Client blocked
                return SendNakResponseAsync(message, "Blocked", responder);
            }
            return HandleDiscoverRequest2Async(subnet, message, responder);
        }

        /// <summary>
        /// Allocate address on the subnet. When allocating a new address,
        /// servers SHOULD check that the offered network address is not
        /// already in use; e.g., the server may probe the offered address
        /// with an ICMP Echo Request. Servers SHOULD be implemented so that
        /// network administrators MAY choose to disable probes of newly
        /// allocated addresses.  The server transmits the DHCPOFFER message
        /// to the client, using the BOOTP relay agent if necessary.
        /// </summary>
        /// <param name="subnet"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        private async Task HandleDiscoverRequest2Async(IDhcpScope subnet,
            DhcpMessage message, IDhcpResponder responder) {
            var clientId = message.ClientIdentifier;
            while (true) {
                // Reserve a lease we will offer.
                var offer = subnet.Allocate(clientId, message.RequestedIPAddress,
                    message.Xid, _configuration.OfferTimeout);
                if (offer == null) {
                    break;
                }
                var isAssigned = await responder.IsAddressAssignedAsync(
                    offer.Identifier.AssignedAddress);
                if (isAssigned) {
                    // Allocated address is already assigned, shelf and retry.
                    subnet.Shelve(clientId, offer.Identifier.AssignedAddress,
                        _configuration.LeaseDuration);
                    continue;
                }
                await SendOfferResponseAsync(subnet, message, offer, responder);
                return;
            }
            await SendNakResponseAsync(message, "Exhausted", responder);
        }

        /// <summary>
        /// Each server may respond with a DHCPOFFER message that includes an
        /// available network address in the 'yiaddr' field (and other
        /// configuration parameters in DHCP options).  Servers need not
        /// reserve the offered network address, although the protocol will
        /// work more efficiently if the server avoids allocating the offered
        /// network address to another client.
        ///
        /// The client receives one or more DHCPOFFER messages from one or more
        /// servers.  The client may choose to wait for multiple responses.
        /// The client chooses one server from which to request configuration
        /// parameters, based on the configuration parameters offered in the
        /// DHCPOFFER messages.  The client broadcasts a DHCPREQUEST message
        /// that MUST include the 'server identifier' option to indicate which
        /// server it has selected, and that MAY include other options
        /// specifying desired configuration values.  The 'requested IP
        /// address' option MUST be set to the value of 'yiaddr' in the
        /// DHCPOFFER message from the server.  This DHCPREQUEST message is
        /// broadcast and relayed through DHCP/BOOTP relay agents.  To help
        /// ensure that any BOOTP relay agents forward the DHCPREQUEST message
        /// to the same set of DHCP servers that received the original
        /// DHCPDISCOVER message, the DHCPREQUEST message MUST use the same
        /// value in the DHCP message header's 'secs' field and be sent to the
        /// same IP broadcast address as the original DHCPDISCOVER message.
        /// The client times out and retransmits the DHCPDISCOVER message if
        /// the client receives no DHCPOFFER messages.
        /// </summary>
        /// <param name="subnet"></param>
        /// <param name="message"></param>
        /// <param name="offer"></param>
        private Task SendOfferResponseAsync(IDhcpScope subnet,
            DhcpMessage message, DhcpLease offer, IDhcpResponder responder) {

            // Reuse message
            message.Op = BootpOpCode.BootReply;
            message.Yiaddr = offer.Identifier.AssignedAddress;

            // Offer new address
            message.MessageType = DhcpMessageType.Offer;
            message.RequestedIPAddress = offer.Identifier.AssignedAddress;
            message.IPAddressLeaseTime = _configuration.LeaseDuration;
            message.Router = subnet.Interface.Gateway;
            message.SubnetMask = subnet.Interface.SubnetMask;
            message.ServerIdentifier = subnet.Interface.UnicastAddress;
            message.DomainName = _configuration.DnsSuffix ??
                subnet.Interface.DnsSuffix;
            message.DomainNameServers = _configuration.DnsServers ??
                subnet.Interface.DnsServers.ToList();

            //
            // BROADCAST (B) flag the client can use to indicate in which way
            // (broadcast or unicast) it can receive the DHCPOFFER: 0x8000
            // for broadcast, 0x0000 for unicast.[5] Usually, the DHCPOFFER
            // is sent through unicast.
            //
            var address = (message.Flags & 1) == 1 ? IPAddress.Broadcast :
                message.Yiaddr;
            return SendResponseAsync(message, address, responder);
        }

        /// <summary>
        /// In the initial offer case the servers receive the DHCPREQUEST
        /// broadcast from the client as result of the DHCPOFFER.
        ///
        /// Those servers not selected by the DHCPREQUEST message use the
        /// message as notification that the client has declined that server's
        /// offer.  The server selected in the DHCPREQUEST message commits the
        /// binding for the client to persistent storage and responds with a
        /// DHCPACK message containing the configuration parameters for the
        /// requesting client.
        ///
        /// The combination of 'client identifier' or 'chaddr' and assigned
        /// network address constitute a unique identifier for the client's
        /// lease and are used by both the client and server to identify a
        /// lease referred to in any DHCP messages.
        ///
        /// If the selected server is unable to satisfy the DHCPREQUEST message
        /// (e.g., the requested network address has been allocated), the
        /// server SHOULD respond with a DHCPNAK message.
        ///
        /// A server MAY choose to mark addresses offered to clients in
        /// DHCPOFFER messages as unavailable.  The server SHOULD mark an
        /// address offered to a client in a DHCPOFFER message as available if
        /// the server receives no DHCPREQUEST message from that client.
        ///
        /// If a client remembers and wishes to reuse a previously allocated
        /// network address, a client may choose to omit some of the steps
        /// described in the above section. In this case the client broadcasts
        /// a DHCPREQUEST message on its local subnet.
        ///
        /// The message includes the client's network address in the
        /// 'requested IP address' option. As the client has not received its
        /// network address, it MUST NOT fill in the 'ciaddr' field. BOOTP
        /// relay agents pass the message on to DHCP servers not on the same
        /// subnet. If the client used a 'client identifier' to obtain its
        /// address, the client MUST use the same 'client identifier' in the
        /// DHCPREQUEST message.
        ///
        /// Servers with knowledge of the client's configuration parameters
        /// respond with a DHCPACK message to the client. Servers SHOULD NOT
        /// check that the client's network address is already in use; the
        /// client may respond to ICMP Echo Request messages at this point.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="subnet"></param>
        /// <returns></returns>
        private Task HandleRequestMessageAsync(IDhcpScope subnet,
            DhcpMessage message, IDhcpResponder responder) {

            var clientId = message.ClientIdentifier;
            var address = message.RequestedIPAddress;
            if (address == null) {
                // Bad request
                return SendNakResponseAsync(message, null, responder);
            }

            if (_configuration?.ListenOnly ?? true ||
                message.ServerIdentifier != subnet.Interface.UnicastAddress) {
                //
                // If in passive mode or the server identifier in the request
                // is not ours thus we were not selected.  So only log the
                // lease in our network interface binding context.
                //
                subnet.Reserve(clientId, message.Xid, address,
                    _configuration.LeaseDuration);
                return Task.CompletedTask;
            }

            // Check for manual assigned addresses
            if (_configuration.StaticAssignment.TryGetValue(clientId,
                out var reserved)) {
                if (reserved != null) {
                    if (!address.Equals(reserved)) {
                        // Client blocked from having this address or any address
                        return SendNakResponseAsync(message, reserved == null ?
                            "Blocked" : "Bad address", responder);
                    }

                    // Ack
                    return SendAckResponseAsync(subnet, message,
                        new DhcpLease(reserved, clientId) {
                            Accepted = true,
                            Expiry = DateTime.Now.Add(_configuration.LeaseDuration)
                        }, responder);
                }
            }
            else if (_configuration.DisableAutoAssignment) {
                // Client blocked
                return SendNakResponseAsync(message, "Blocked", responder);
            }

            // Commit the communicated lease
            var lease = subnet.Commit(clientId, address, message.Xid,
                _configuration.LeaseDuration);
            if (lease != null) {
                return SendAckResponseAsync(subnet, message, lease, responder);
            }
            // No activated lease found
            return SendNakResponseAsync(message, "Unknown lease", responder);
        }

        /// <summary>
        /// Any configuration parameters in the DHCPACK message SHOULD NOT
        /// conflict with those in the earlier DHCPOFFER message to which the
        /// client is responding.  The server SHOULD NOT check the offered
        /// network address at this point. The 'yiaddr' field in the DHCPACK
        /// messages is filled in with the selected network address.
        ///
        /// The client receives the DHCPACK message with configuration
        /// parameters.  The client SHOULD perform a final check on the
        /// parameters (e.g., ARP for allocated network address), and note the
        /// duration of the lease specified in the DHCPACK message.  At this
        /// point, the client is configured.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="lease"></param>
        private Task SendAckResponseAsync(IDhcpScope subnet,
            DhcpMessage message, DhcpLease lease, IDhcpResponder responder) {

            // Reuse message
            message.Op = BootpOpCode.BootReply;
            message.Yiaddr = lease.Identifier.AssignedAddress;

            // Offer new address
            message.MessageType = DhcpMessageType.Ack;
            message.RequestedIPAddress = lease.Identifier.AssignedAddress;

            message.IPAddressLeaseTime = _configuration.LeaseDuration;
            message.Router = subnet.Interface.Gateway;
            message.SubnetMask = subnet.Interface.SubnetMask;
            message.ServerIdentifier = subnet.Interface.UnicastAddress;
            message.DomainName = _configuration.DnsSuffix ??
                subnet.Interface.DnsSuffix;
            message.DomainNameServers = _configuration.DnsServers ??
                subnet.Interface.DnsServers.ToList();

            return SendResponseAsync(message, message.Yiaddr, responder);
        }

        private readonly IDhcpServerConfig _configuration;
        private readonly ILogger _logger;
    }
}
