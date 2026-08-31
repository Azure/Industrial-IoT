// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Stack.Transport.Models
{
    using Azure.IIoT.OpcUa.Publisher.Stack.Transport.Models;
    using System.Net;
    using System.Net.NetworkInformation;
    using Xunit;

    /// <summary>
    /// Tests for <see cref="NetInterface"/> equality, hash, and constructors.
    /// </summary>
    public sealed class NetInterfaceTests
    {
        [Fact]
        public void SimpleConstructor_SetsNameAndAddresses()
        {
            var addr = IPAddress.Parse("10.0.0.1");
            var mask = IPAddress.Parse("255.255.255.0");
            var ni = new NetInterface("eth0", addr, mask);

            Assert.Equal("eth0", ni.Name);
            Assert.Equal(addr, ni.UnicastAddress);
            Assert.Equal(mask, ni.SubnetMask);
            Assert.Null(ni.MacAddress);
            Assert.Null(ni.Gateway);
            Assert.Null(ni.DnsSuffix);
        }

        [Fact]
        public void FullConstructor_SetsAllProperties()
        {
            var addr = IPAddress.Parse("10.0.0.1");
            var mask = IPAddress.Parse("255.255.255.0");
            var gw = IPAddress.Parse("10.0.0.254");
            var mac = new PhysicalAddress([0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF]);
            var dns = new[] { IPAddress.Parse("8.8.8.8") };

            var ni = new NetInterface("eth0", mac, addr, mask, gw, "example.com", dns);

            Assert.Equal("eth0", ni.Name);
            Assert.Equal(addr, ni.UnicastAddress);
            Assert.Equal(mask, ni.SubnetMask);
            Assert.Equal(gw, ni.Gateway);
            Assert.Equal(mac, ni.MacAddress);
            Assert.Equal("example.com", ni.DnsSuffix);
        }

        [Fact]
        public void Equals_SameValues_ReturnsTrue()
        {
            var addr = IPAddress.Parse("10.0.0.1");
            var mask = IPAddress.Parse("255.255.255.0");
            var gw = IPAddress.Parse("10.0.0.254");
            var mac = new PhysicalAddress([0x01, 0x02, 0x03, 0x04, 0x05, 0x06]);

            var ni1 = new NetInterface("eth0", mac, addr, mask, gw, "local", []);
            var ni2 = new NetInterface("eth0", mac, addr, mask, gw, "local", []);

            Assert.Equal(ni1, ni2);
            Assert.Equal(ni1.GetHashCode(), ni2.GetHashCode());
        }

        [Fact]
        public void Equals_DifferentAddress_ReturnsFalse()
        {
            var mask = IPAddress.Parse("255.255.255.0");
            var ni1 = new NetInterface("eth0", IPAddress.Parse("10.0.0.1"), mask);
            var ni2 = new NetInterface("eth0", IPAddress.Parse("10.0.0.2"), mask);

            Assert.NotEqual(ni1, ni2);
        }

        [Fact]
        public void Equals_NullAndDifferentType_ReturnsFalse()
        {
            var ni = new NetInterface("eth0", IPAddress.Loopback, IPAddress.Parse("255.255.255.0"));

            Assert.False(ni.Equals(null));
            Assert.False(ni.Equals("eth0"));
        }

        [Fact]
        public void Equals_SimpleConstructorNullGatewayAndDns_ComparesCorrectly()
        {
            var ni1 = new NetInterface("eth0", IPAddress.Loopback, IPAddress.Parse("255.0.0.0"));
            var ni2 = new NetInterface("eth0", IPAddress.Loopback, IPAddress.Parse("255.0.0.0"));

            Assert.Equal(ni1, ni2);
        }
    }
}
