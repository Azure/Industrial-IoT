// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Stack.Transport.Models
{
    using Azure.IIoT.OpcUa.Publisher.Stack.Transport.Models;
    using System;
    using System.Net;
    using Xunit;

    public class IPv4AddressTests
    {
        [Fact]
        public void TestEmptyAddress()
        {
            var address = new IPv4Address([0, 0, 0, 0]);
            uint val = address;
            long val2 = address;
            Assert.Equal(0u, val);
            Assert.Equal(0L, val2);
            Assert.Equal(address, IPAddress.Any);
        }

        [Fact]
        public void TestIncrementAddress()
        {
            var address = new IPv4Address([255, 255, 255, 255]);
            var any = new IPv4Address([0, 0, 0, 0]);
            var incremented = address + 1;
            address++;
            Assert.Equal(0u, (uint)incremented);
            Assert.Equal(IPAddress.Any, incremented);
            Assert.Equal(any, incremented);
            Assert.Equal(0u, (uint)address);
            Assert.Equal(IPAddress.Any, address);
            Assert.Equal(any, address);
        }

        [Fact]
        public void TestDecrementAddress()
        {
            var address = new IPv4Address([0, 0, 0, 0]);
            var bcast = new IPv4Address([255, 255, 255, 255]);
            var decremented = address - 1;
            address--;
            Assert.Equal((uint)bcast, (uint)decremented);
            Assert.Equal(IPAddress.Broadcast, decremented);
            Assert.Equal(bcast, decremented);
            Assert.Equal(-1, (int)decremented);
            Assert.Equal((uint)bcast, (uint)address);
            Assert.Equal(bcast, address);
            Assert.Equal(IPAddress.Broadcast, address);
            Assert.Equal(-1, (int)address);
        }

        [Fact]
        public void TestSubtractAddress()
        {
            var bcast = new IPv4Address([255, 255, 255, 255]);
            var any = new IPv4Address([0, 0, 0, 0]);
            var subtracted = bcast - bcast;
            Assert.Equal(0u, subtracted);
            Assert.Equal(IPAddress.Any, (IPv4Address)subtracted);
            Assert.Equal(any, (IPv4Address)subtracted);
        }

        [Fact]
        public void ThrowForSize()
        {
            Assert.Throws<ArgumentException>(() => new IPv4Address([]));
            Assert.Throws<ArgumentException>(() => new IPv4Address([0]));
            Assert.Throws<ArgumentException>(() => new IPv4Address([0, 1, 1, 2, 1]));
            Assert.Throws<ArgumentException>(() => new IPv4Address([0, 2, 4]));
        }
    }
}
