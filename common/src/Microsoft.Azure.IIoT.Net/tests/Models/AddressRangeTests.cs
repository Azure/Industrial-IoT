// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Net.Models {
    using Xunit;
    using System.Net;
    using System;
    using System.Linq;

    public class AddressRangeTests {

        [Fact]
        public void TestSubnetRange1() {
            var range = new AddressRange(
                IPAddress.Parse("10.93.232.185"), 20);

            Assert.Equal(IPAddress.Parse("10.93.224.0"), (IPv4Address)range.Low);
            Assert.Equal(IPAddress.Parse("10.93.239.255"), (IPv4Address)range.High);
            Assert.Equal(4096, range.Count);
        }

        [Fact]
        public void TestSubnetRange2() {
            var range = new AddressRange(
                IPAddress.Parse("10.93.232.185"), IPAddress.Parse("255.255.240.0"));

            Assert.Equal(IPAddress.Parse("10.93.224.0"), (IPv4Address)range.Low);
            Assert.Equal(IPAddress.Parse("10.93.239.255"), (IPv4Address)range.High);
            Assert.Equal(4096, range.Count);
        }

        [Fact]
        public void TestSubnetNicLocal() {
            var nics = NetworkInformationEx.GetAllNetInterfaces(NetworkClass.Wired);
            if (!nics.Any()) {
                return;
            }
            var nic = nics.First();
            var range = new AddressRange(nic, true);

            Assert.Equal(nic.UnicastAddress, (IPv4Address)range.Low);
            Assert.Equal(nic.UnicastAddress, (IPv4Address)range.High);
            Assert.Equal(1, range.Count);
        }

        [Fact]
        public void TestSubnetNic() {
            var nics = NetworkInformationEx.GetAllNetInterfaces(NetworkClass.Wired);
            if (!nics.Any()) {
                return;
            }
            var nic = nics.First();
            var expected = new AddressRange(nic.UnicastAddress, nic.SubnetMask);
            var range = new AddressRange(nic);

            Assert.Equal((IPv4Address)expected.Low, (IPv4Address)range.Low);
            Assert.Equal((IPv4Address)expected.High, (IPv4Address)range.High);
            Assert.Equal(expected.Count, range.Count);
        }

        [Fact]
        public void TestSimpleRange() {
            var range = new AddressRange(0, 255);

            Assert.Equal(0u, range.Low);
            Assert.Equal(255u, range.High);
            Assert.Equal(256, range.Count);
            Assert.Equal(IPAddress.Any, (IPv4Address)range.Low);
        }

        [Fact]
        public void TestEquality1() {
            var range1 = new AddressRange(0, 255);
            var range2 = new AddressRange(0, 255);

            Assert.Equal(range1, range2);
            Assert.True(range1 == range2);
            Assert.False(range1 != range2);
        }

        [Fact]
        public void TestNoneEquality1() {
            var range1 = new AddressRange(0, 255);
            var range2 = new AddressRange(1, 255);

            Assert.NotEqual(range1, range2);
            Assert.True(range1 != range2);
            Assert.False(range1 == range2);
        }

        [Fact]
        public void TestEquality2() {
            var range1 = new AddressRange((IPv4Address)0u, 24);
            var range2 = new AddressRange(0, 255);

            Assert.Equal(range1, range2);
            Assert.True(range1 == range2);
            Assert.False(range1 != range2);
        }

        [Fact]
        public void TestParsing1() {
            AddressRange.TryParse("0.0.0.0/24", out var range1);
            var range2 = new AddressRange(0, 255);

            Assert.Single(range1);
            Assert.Equal(range2, range1.Single());
        }

        [Fact]
        public void TestParsing2() {
            AddressRange.TryParse("0.0.0.0/24,0.0.0.0/24", out var range1);
            var range2 = new AddressRange(0, 255);

            Assert.Single(range1);
            Assert.Equal(range2, range1.First());
        }

        [Fact]
        public void TestParsing3() {
            AddressRange.TryParse("0.0.0.0/24;0.0.0.0/24;;;", out var range1);
            var range2 = new AddressRange(0, 255);

            Assert.Single(range1);
            Assert.Equal(range2, range1.First());
        }

        [Fact]
        public void TestParseFormatExceptions() {

            Assert.Throws<FormatException>(() => AddressRange.Parse("0.0.0.0/24;x/2;;"));
            Assert.Throws<FormatException>(() => AddressRange.Parse("0.0.=0/24"));
            Assert.Throws<FormatException>(() => AddressRange.Parse("0.0.0.0"));
            Assert.Throws<FormatException>(() => AddressRange.Parse("0.0.0.0.0/2"));
            Assert.Throws<FormatException>(() => AddressRange.Parse("0.0.0.0/88"));
            Assert.Throws<FormatException>(() => AddressRange.Parse("0..0/88"));
        }

    }
}
