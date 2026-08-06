// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Stack.Transport.Extensions
{
    using Azure.IIoT.OpcUa.Publisher.Stack.Transport.Models;
    using System.Net;
    using Xunit;

    /// <summary>
    /// Tests for <see cref="IPAddressEx.AsV4"/>.
    /// </summary>
    public sealed class IPAddressExTests
    {
        [Fact]
        public void AsV4_IPv4Address_ReturnsSameValueAsIPv4Address()
        {
            var addr = IPAddress.Parse("192.168.1.1");
            var v4 = addr.AsV4();
            Assert.IsType<IPv4Address>(v4);
            Assert.Equal(addr, v4);
        }

        [Fact]
        public void AsV4_Loopback_ReturnsLoopback()
        {
            var v4 = IPAddress.Loopback.AsV4();
            Assert.Equal(IPAddress.Loopback, v4);
        }

        [Fact]
        public void AsV4_AnyAddress_ReturnsAny()
        {
            var v4 = IPAddress.Any.AsV4();
            Assert.Equal(IPAddress.Any, v4);
        }

        [Fact]
        public void AsV4_Broadcast_ReturnsBroadcast()
        {
            var v4 = IPAddress.Broadcast.AsV4();
            Assert.Equal(IPAddress.Broadcast, v4);
        }

        [Fact]
        public void AsV4_PreservesUintRoundTrip()
        {
            var addr = IPAddress.Parse("10.20.30.40");
            var v4 = addr.AsV4();
            uint asUint = v4;
            IPv4Address back = asUint;
            Assert.Equal(addr, back);
        }
    }
}
