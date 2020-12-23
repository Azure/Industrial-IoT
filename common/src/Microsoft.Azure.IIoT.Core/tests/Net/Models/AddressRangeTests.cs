// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Net.Models {
    using Xunit;
    using System.Net;
    using System;
    using System.Linq;
    using System.Collections.Generic;

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
        public void TestSingleAddress() {
            var range = new AddressRange(IPAddress.Loopback, 32, "local");

            var list = new List<uint>();
            range.FillNextBatch(list, 1000);

            Assert.Single(list);
            Assert.Equal(IPAddress.Loopback, (IPv4Address)list.Single());
            Assert.Equal(IPAddress.Loopback, (IPv4Address)range.High);
            Assert.Equal(IPAddress.Loopback, (IPv4Address)range.Low);
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
            Assert.Equal("custom", range1.First().Nic);
            Assert.Equal("0.0.0.0/24", range1.First().ToString());
        }

        [Fact]
        public void TestParsing1WithRange() {
            AddressRange.TryParse("0.0.0.0-0.0.0.255", out var range1);
            var range2 = new AddressRange(0, 255);

            Assert.Single(range1);
            Assert.Equal(range2, range1.Single());
            Assert.Equal("custom", range1.First().Nic);
            Assert.Equal("0.0.0.0/24", range1.First().ToString());
        }

        [Fact]
        public void TestParsing1WithNic() {
            AddressRange.TryParse("0.0.0.0/24[abc]", out var range1);
            var range2 = new AddressRange(0, 255);

            Assert.Single(range1);
            Assert.Equal(range2, range1.Single());
            Assert.Equal("abc", range1.First().Nic);
            Assert.Equal("0.0.0.0/24 [abc]", range1.First().ToString());
        }

        [Fact]
        public void TestParsing2() {
            AddressRange.TryParse("0.0.0.0/24,0.0.0.0/24", out var range1);
            var range2 = new AddressRange(0, 255);

            Assert.Single(range1);
            Assert.Equal(range2, range1.First());
            Assert.Equal("custom", range1.First().Nic);
            Assert.Equal("0.0.0.0/24", range1.First().ToString());
        }

        [Fact]
        public void TestParsing2WithNic() {
            AddressRange.TryParse("0.0.0.0/24[xyz],0.0.0.0/24[abc def ]", out var range1);
            var range2 = new AddressRange(0, 255);

            Assert.Single(range1);
            Assert.Equal(range2, range1.First());
            Assert.Equal("xyz", range1.First().Nic);
        }

        [Fact]
        public void TestParsing3() {
            AddressRange.TryParse("0.0.0.0/24;0.0.0.0/24;;;", out var range1);
            var range2 = new AddressRange(0, 255);

            Assert.Single(range1);
            Assert.Equal(range2, range1.First());
            Assert.Equal("custom", range1.First().Nic);
        }

        [Fact]
        public void TestParsing3b() {
            AddressRange.TryParse("0.0.0.0/24;0.0.0.0-0.0.0.255;;;", out var range1);
            var range2 = new AddressRange(0, 255);

            Assert.Single(range1);
            Assert.Equal(range2, range1.First());
            Assert.Equal("custom", range1.First().Nic);
            Assert.Equal("0.0.0.0/24", range1.First().ToString());
        }

        [Fact]
        public void TestParsing4() {
            AddressRange.TryParse("0.0.0.0/24[abc];0.0.0.0/24[abc];;;", out var range1);
            var range2 = new AddressRange(0, 255);

            Assert.Single(range1);
            Assert.Equal(range2, range1.First());
            Assert.Equal("abc", range1.First().Nic);
        }

        [Fact]
        public void TestParsing5() {
            AddressRange.TryParse("0.0.0.0/24 [abc];0.0.0.0-0.0.0.255[abc];;;", out var range1);
            var range2 = new AddressRange(0, 255);

            Assert.Single(range1);
            Assert.Equal(range2, range1.First());
            Assert.Equal("abc", range1.First().Nic);
        }

        [Fact]
        public void TestParsing6() {
            var str = "1.1.1.1/24[abc];2.2.2.2/24[cde];3.3.3.3/24 [efg]";
            AddressRange.TryParse(str, out var ranges);

            var range1 = new AddressRange(16843008, 16843263);
            var range2 = new AddressRange(33686016, 33686271);
            var range3 = new AddressRange(50529024, 50529279);

            Assert.Collection(ranges,
                a => {
                    Assert.Equal(range1, a);
                    Assert.Equal("abc", a.Nic);
                },
                b => {
                    Assert.Equal(range2, b);
                    Assert.Equal("cde", b.Nic);
                },
                c => {
                    Assert.Equal(range3, c);
                    Assert.Equal("efg", c.Nic);
                });
            Assert.Equal("1.1.1.0/24 [abc];2.2.2.0/24 [cde];3.3.3.0/24 [efg]",
                AddressRange.Format(ranges));
        }

        [Fact]
        public void TestParsing7() {
            var str = "192.168.1.0-192.168.2.9[abc]";
            AddressRange.TryParse(str, out var ranges);

            var range1 = new AddressRange(3232235776, 3232236031);
            var range2 = new AddressRange(3232236032, 3232236039);
            var range3 = new AddressRange(3232236040, 3232236041);

            Assert.Collection(ranges,
                a => {
                    Assert.Equal(range1, a);
                    Assert.Equal("abc", a.Nic);
                },
                b => {
                    Assert.Equal(range2, b);
                    Assert.Equal("abc", b.Nic);
                },
                c => {
                    Assert.Equal(range3, c);
                    Assert.Equal("abc", c.Nic);
                });

            Assert.Equal("192.168.1.0/24 [abc];192.168.2.0/29 [abc];192.168.2.8/31 [abc]",
                AddressRange.Format(ranges));
        }

        [Fact]
        public void TestParseFormatExceptions() {

            Assert.Throws<FormatException>(() => AddressRange.Parse("0.0.0.0/24;x/2;;"));
            Assert.Throws<FormatException>(() => AddressRange.Parse("0.0.=0/24"));
            Assert.Throws<FormatException>(() => AddressRange.Parse("0.0.0.0-0..0"));
            Assert.Throws<FormatException>(() => AddressRange.Parse("0.0.0.0-0.0."));
            Assert.Throws<FormatException>(() => AddressRange.Parse("0.0.0.0/16-0.0.0.0/22"));
            Assert.Throws<FormatException>(() => AddressRange.Parse("0.0.0.0-0.0.0.222/3"));
            Assert.Throws<FormatException>(() => AddressRange.Parse("0.0.0.0[]"));
            Assert.Throws<FormatException>(() => AddressRange.Parse("0.0.0.0"));
            Assert.Throws<FormatException>(() => AddressRange.Parse("0.0.0.0.0/2"));
            Assert.Throws<FormatException>(() => AddressRange.Parse("0.0.0.0/88"));
            Assert.Throws<FormatException>(() => AddressRange.Parse("0.0.0.0/88[333]"));
            Assert.Throws<FormatException>(() => AddressRange.Parse("0..0/88"));
        }

    }
}
