// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Net.Bootp {
    using System.Net;
    using System.Net.NetworkInformation;
    using Xunit;

    public class BootpMessageTests {

        [Fact]
        public void TestPacketEquality1() {
            var message1 = new BootpMessage {
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
            Assert.Equal(s1, s2);
        }

        [Fact]
        public void TestPacketEquality2() {
            var message1 = new BootpMessage {
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
            Assert.Equal(s1, s2);
        }
    }
}
