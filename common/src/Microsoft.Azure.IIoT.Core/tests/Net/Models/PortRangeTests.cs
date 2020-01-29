// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Net.Models {
    using Xunit;
    using System;
    using System.Linq;

    public class PortRangeTests {

        [Fact]
        public void TestSimpleRange() {
            var range1 = new PortRange(0, 100);
            var range2 = new PortRange(100, 0);

            Assert.Equal(101, range1.Count);
            Assert.Equal(range1, range2);
            Assert.False(range1 != range2);
            Assert.True(range1 == range2);
        }

        [Fact]
        public void TestNoneEquality() {
            var range1 = new PortRange(0, 4);
            var range2 = new PortRange(1, 4);

            Assert.NotEqual(range1, range2);
            Assert.True(range1 != range2);
            Assert.False(range1 == range2);
        }

        [Fact]
        public void TestParsing1() {
            PortRange.TryParse("0-100", out var range1);
            var range2 = new PortRange(0, 100);

            Assert.Single(range1);
            Assert.Equal(range2, range1.Single());
        }

        [Fact]
        public void TestParsing2() {
            PortRange.TryParse("0-100;44", out var range1);
            var range2 = new PortRange(0, 100);
            var range3 = new PortRange(44);

            Assert.Equal(range2, range1.First());
            Assert.Equal(range3, range1.Skip(1).First());
            Assert.Equal(2, range1.Count());
        }

        [Fact]
        public void TestParsing3() {
            PortRange.TryParse("0,1,2,,,,", out var range1);

            Assert.Equal(new PortRange(0), range1.First());
            Assert.Equal(new PortRange(1), range1.Skip(1).First());
            Assert.Equal(new PortRange(2), range1.Skip(2).First());
            Assert.Equal(3, range1.Count());
        }

        [Fact]
        public void TestParsing4() {
            PortRange.TryParse(",,,,", out var range1);
            Assert.Empty(range1);
        }

        [Fact]
        public void TestParsing5() {
            PortRange.TryParse("*", out var range1);
            Assert.Single(range1);
            Assert.Equal(new PortRange(0, 65536), range1.Single());
        }

        [Fact]
        public void TestParsing6() {
            PortRange.TryParse("100-*", out var range1);
            Assert.Single(range1);
            Assert.Equal(new PortRange(100, 65536), range1.Single());
        }

        [Fact]
        public void TestParsing7() {
            PortRange.TryParse("*-100", out var range1);
            Assert.Single(range1);
            Assert.Equal(new PortRange(0, 100), range1.Single());
        }

        [Fact]
        public void TestParseFormatExceptions() {

            Assert.Throws<FormatException>(() => PortRange.Parse("0.0.0.0/24;x/2;;"));
            Assert.Throws<FormatException>(() => PortRange.Parse("abf,d"));
            Assert.Throws<FormatException>(() => PortRange.Parse("0-1-2"));
            Assert.Throws<FormatException>(() => PortRange.Parse("0,1-2,f,"));
        }
    }
}
