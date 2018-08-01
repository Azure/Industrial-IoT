// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Net.Models {
    using Xunit;
    using System.Collections.Generic;
    using System.Text;
    using System.Net;
    using System;
    using System.Linq;

    public class AddressRangeTests {

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
            var range1 = new AddressRange(new IPv4Address(0u), 24);
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
