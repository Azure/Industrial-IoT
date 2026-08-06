// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Stack.Transport.Extensions
{
    using System;
    using System.Net;
    using System.Net.Sockets;
    using Xunit;

    /// <summary>
    /// Tests for EndPointEx extension methods.
    /// </summary>
    public class EndPointExTests
    {
        // ─────────────────────── GetIPAddress ───────────────────────

        [Fact]
        public void GetIPAddress_IPEndPoint_ReturnsAddress()
        {
            var endpoint = new IPEndPoint(IPAddress.Loopback, 4840);
            var result = endpoint.GetIPAddress();
            Assert.Equal(IPAddress.Loopback, result);
        }

        [Fact]
        public void GetIPAddress_IPv4MappedToIPv6_PreferV4_ReturnsMappedV4()
        {
            var v4 = IPAddress.Parse("192.168.1.1");
            var mapped = v4.MapToIPv6();
            var endpoint = new IPEndPoint(mapped, 4840);

            var result = endpoint.GetIPAddress(preferv4: true);

            // Should be converted back to v4
            Assert.Equal(AddressFamily.InterNetwork, result.AddressFamily);
            Assert.Equal(v4, result);
        }

        [Fact]
        public void GetIPAddress_IPv6Loopback_PreferV4_ReturnsSameAddress()
        {
            // ::1 is not a mapped v4 address, preferv4 has no effect
            var endpoint = new IPEndPoint(IPAddress.IPv6Loopback, 4840);
            var result = endpoint.GetIPAddress(preferv4: true);
            Assert.Equal(IPAddress.IPv6Loopback, result);
        }

        [Fact]
        public void GetIPAddress_NonIPEndPoint_Throws()
        {
            // DnsEndPoint is not an IPEndPoint
            EndPoint ep = new DnsEndPoint("localhost", 4840);
            Assert.Throws<ArgumentException>(() => ep.GetIPAddress());
        }

        // ─────────────────────── GetPort ───────────────────────

        [Fact]
        public void GetPort_IPEndPoint_ReturnsPort()
        {
            var endpoint = new IPEndPoint(IPAddress.Loopback, 4840);
            var result = endpoint.GetPort();
            Assert.Equal(4840, result);
        }

        [Fact]
        public void GetPort_NonIPEndPoint_ReturnsMinusOne()
        {
            EndPoint ep = new DnsEndPoint("localhost", 4840);
            var result = ep.GetPort();
            Assert.Equal(-1, result);
        }

        [Fact]
        public void GetPort_ZeroPort_ReturnsZero()
        {
            var endpoint = new IPEndPoint(IPAddress.Loopback, 0);
            var result = endpoint.GetPort();
            Assert.Equal(0, result);
        }
    }
}
