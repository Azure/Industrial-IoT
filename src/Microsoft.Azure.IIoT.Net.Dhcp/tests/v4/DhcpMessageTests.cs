// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Net.Dhcp.v4 {
    using Microsoft.Azure.IIoT.Net.Bootp;
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.NetworkInformation;
    using Xunit;

    public class DhcpMessageTests {

        [Fact]
        public void TestPacketEquals1() {
            var message1 = new DhcpMessage {
                Chaddr = new PhysicalAddress(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 }),
                Yiaddr = new IPAddress(new byte[] { 1, 1, 1, 1 }),
                Siaddr = new IPAddress(new byte[] { 2, 2, 2, 2 }),
                Hops = 4,
                Op = BootpOpCode.BootRequest,
                Xid = 44,
                IPAddressLeaseTime = TimeSpan.FromDays(1),
                MessageType = DhcpMessageType.Offer,
                ServerIdentifier = IPAddress.Parse("127.0.0.1"),
                Message = "some long error message"
            };

            var packet = message1.AsPacket();
            var message2 = DhcpMessage.Parse(packet, 0, packet.Length);

            var s1 = message1.ToString();
            var s2 = message2.ToString();

            Assert.Equal(message1, message2);
            Assert.Equal(packet.Length, message2.AsPacket().Length);
            Assert.Equal(s1, s2);
        }

        [Fact]
        public void TestPacketEquals2() {
            var message1 = new DhcpMessage {
                Yiaddr = new IPAddress(new byte[] { 1, 1, 1, 1 }),
                Siaddr = new IPAddress(new byte[] { 2, 2, 2, 2 }),
                Op = BootpOpCode.BootReply,
                File = "Filename",
                Xid = 44,
                DomainName = "Testcasedomain",
                DomainNameServers = new List<IPAddress> {
                    IPAddress.Parse("192.22.254.23"),
                    IPAddress.Parse("192.22.254.24"),
                    IPAddress.Parse("192.22.254.32")
                },
                Router = IPAddress.Parse("192.22.254.1"),
                IPAddressLeaseTime = TimeSpan.FromDays(1),
                MessageType = DhcpMessageType.Ack,
                ServerIdentifier = IPAddress.Parse("127.0.0.1")
            };

            var packet = message1.AsPacket();
            var message2 = DhcpMessage.Parse(packet, 0, packet.Length);
            var s1 = message1.ToString();
            var s2 = message2.ToString();

            Assert.Equal(message1, message2);
            Assert.Equal(packet.Length, message2.AsPacket().Length);
            Assert.Equal(s1, s2);
        }

        [Fact]
        public void TestPacketEqualsBootp1() {
            var message1 = new DhcpMessage {
                Chaddr = new PhysicalAddress(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 }),
                Yiaddr = new IPAddress(new byte[] { 1, 1, 1, 1 }),
                Siaddr = new IPAddress(new byte[] { 2, 2, 2, 2 }),
                Hops = 4,
                Op = BootpOpCode.BootRequest,
                Xid = 44
            };

            var packet = message1.AsPacket();
            var message2 = BootpMessage.Parse(packet, 0, packet.Length);

            var s1 = message1.ToString();
            var s2 = message2.ToString();

            Assert.Equal(message1, message2);
            Assert.Equal(packet.Length, message2.AsPacket().Length);
            Assert.NotEqual(s1, s2);
        }

        [Fact]
        public void TestPacketEqualsBootp2() {
            var message1 = new DhcpMessage {
                Yiaddr = new IPAddress(new byte[] { 1, 1, 1, 1 }),
                Siaddr = new IPAddress(new byte[] { 2, 2, 2, 2 }),
                Op = BootpOpCode.BootReply,
                File = "Filename",
                Xid = 44
            };

            var packet = message1.AsPacket();
            var message2 = BootpMessage.Parse(packet, 0, packet.Length);
            var s1 = message1.ToString();
            var s2 = message2.ToString();

            Assert.Equal(message1, message2);
            Assert.Equal(packet.Length, message2.AsPacket().Length);
            Assert.NotEqual(s1, s2);
        }

        [Fact]
        public void TestPacketNotEqualToBootp3() {
            var message1 = new DhcpMessage {
                Chaddr = new PhysicalAddress(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 }),
                Yiaddr = new IPAddress(new byte[] { 1, 1, 1, 1 }),
                Siaddr = new IPAddress(new byte[] { 2, 2, 2, 2 }),
                Hops = 4,
                Op = BootpOpCode.BootRequest,
                Xid = 44,
                
                MessageType = DhcpMessageType.Decline,
                Message = "test"
            };

            var packet = message1.AsPacket();
            var message2 = BootpMessage.Parse(packet, 0, packet.Length);

            var s1 = message1.ToString();
            var s2 = message2.ToString();

            Assert.NotEqual(message1, message2);
            Assert.NotEqual(packet.Length, message2.AsPacket().Length);
            Assert.NotEqual(s1, s2);
        }
    }
}
