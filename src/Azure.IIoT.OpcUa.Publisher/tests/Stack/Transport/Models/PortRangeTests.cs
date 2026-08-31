// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Stack.Transport.Models
{
    using Azure.IIoT.OpcUa.Publisher.Stack.Transport.Models;
    using System;
    using System.Linq;
    using System.Net;
    using Xunit;

    public class PortRangeTests
    {
        [Fact]
        public void TestSimpleRange()
        {
            var range1 = new PortRange(0, 100);
            var range2 = new PortRange(100, 0);

            Assert.Equal(101, range1.Count);
            Assert.Equal(range1, range2);
            Assert.False(range1 != range2);
            Assert.True(range1 == range2);
            Assert.True(range1.Overlaps(range2));
            Assert.True(range1.Contains(0));
            Assert.True(range1.Contains(100));
            Assert.True(range1.Contains(6));
        }

        [Fact]
        public void TestNoneEquality1()
        {
            var range1 = new PortRange(0, 4);
            var range2 = new PortRange(1, 4);

            Assert.NotEqual(range1, range2);
            Assert.True(range1 != range2);
            Assert.False(range1 == range2);
            Assert.True(range1.Overlaps(range2));
        }

        [Fact]
        public void TestNoneEquality2()
        {
            var range1 = new PortRange(0, 4);
            var range2 = new PortRange(7, 9);

            Assert.NotEqual(range1, range2);
            Assert.True(range1 != range2);
            Assert.False(range1 == range2);
            Assert.False(range1.Overlaps(range2));
        }

        [Fact]
        public void TestParsing1()
        {
            var success = PortRange.TryParse("0-100", out var range1);
            var range2 = new PortRange(0, 100);

            Assert.True(success);
            Assert.Single(range1);
            Assert.Equal(range2, range1.Single());
            Assert.Equal("0-100", PortRange.Format(range1));
        }

        [Fact]
        public void TestParsing2()
        {
            var success = PortRange.TryParse("0-100;144", out var range1);
            var range2 = new PortRange(0, 100);
            var range3 = new PortRange(144);

            Assert.True(success);
            Assert.Equal(range2, range1.First());
            Assert.Equal(range3, range1.Skip(1).First());
            Assert.Equal(2, range1.Count());
            Assert.Equal("0-100;144", PortRange.Format(range1));
        }

        [Fact]
        public void TestParsing2b()
        {
            var success = PortRange.TryParse("0-100;44", out var range1);
            var range2 = new PortRange(0, 100);

            Assert.True(success);
            Assert.Equal(range2, range1.First());
            Assert.Single(range1);
            Assert.Equal("0-100", PortRange.Format(range1));
        }

        [Fact]
        public void TestParsing3()
        {
            var success = PortRange.TryParse("0,3,6,,,,", out var range1);

            Assert.True(success);
            Assert.Equal(new PortRange(0), range1.First());
            Assert.Equal(new PortRange(3), range1.Skip(1).First());
            Assert.Equal(new PortRange(6), range1.Skip(2).First());
            Assert.Equal(3, range1.Count());
            Assert.Equal("0;3;6", PortRange.Format(range1));
        }

        [Fact]
        public void TestParsing3b()
        {
            var success = PortRange.TryParse("0-1,3-4,6-9,,,,", out var range1);

            Assert.True(success);
            Assert.Equal(new PortRange(0, 1), range1.First());
            Assert.Equal(new PortRange(3, 4), range1.Skip(1).First());
            Assert.Equal(new PortRange(6, 9), range1.Skip(2).First());
            Assert.Equal(3, range1.Count());
            Assert.Equal("0-1;3-4;6-9", PortRange.Format(range1));
        }

        [Fact]
        public void TestParsing3c()
        {
            var success = PortRange.TryParse("0-1;3-4;6-*", out var range1);

            Assert.True(success);
            Assert.Equal(new PortRange(0, 1), range1.First());
            Assert.Equal(new PortRange(3, 4), range1.Skip(1).First());
            Assert.Equal(new PortRange(6, 65536), range1.Skip(2).First());
            Assert.Equal(3, range1.Count());
            Assert.Equal("0-1;3-4;6-*", PortRange.Format(range1));
        }

        [Fact]
        public void TestParsing3d()
        {
            var success = PortRange.TryParse("0-1,1-2,2-3,,,,", out var range1);

            Assert.True(success);
            Assert.Single(range1);
            Assert.Equal(new PortRange(0, 3), range1.First());
            Assert.Equal("0-3", PortRange.Format(range1));
        }

        [Fact]
        public void TestParsing4()
        {
            var success = PortRange.TryParse(",,,,", out var range1);
            Assert.True(success);
            Assert.Empty(range1);
        }

        [Fact]
        public void TestParsing5()
        {
            var success = PortRange.TryParse("*", out var range1);
            Assert.True(success);
            Assert.Single(range1);
            Assert.Equal(new PortRange(0, 65536), range1.Single());
            Assert.Equal("*", range1.Single().ToString());
        }

        [Fact]
        public void TestParsing6()
        {
            var success = PortRange.TryParse("100-*", out var range1);
            Assert.True(success);
            Assert.Single(range1);
            Assert.Equal(new PortRange(100, 65536), range1.Single());
            Assert.Equal("100-*", range1.Single().ToString());
        }

        [Fact]
        public void TestParsing7()
        {
            var success = PortRange.TryParse("*-100", out var range1);
            Assert.True(success);
            Assert.Single(range1);
            Assert.Equal(new PortRange(0, 100), range1.Single());
            Assert.Equal("0-100", range1.Single().ToString());
        }

        [Fact]
        public void TestParseFormatExceptions()
        {
            Assert.Throws<FormatException>(() => PortRange.Parse("0.0.0.0/24;x/2;;"));
            Assert.Throws<FormatException>(() => PortRange.Parse("abf,d"));
            Assert.Throws<FormatException>(() => PortRange.Parse("0-1-2"));
            Assert.Throws<FormatException>(() => PortRange.Parse("0,1-2,f,"));
        }

        [Theory]
        [InlineData("80", "80", 1, 80, 80)]
        [InlineData(" 80 ", "80", 1, 80, 80)]
        [InlineData("80-81", "80-81", 2, 80, 81)]
        [InlineData(" 80 - 81 ", "80-81", 2, 80, 81)]
        [InlineData("0", "0", 1, 0, 0)]
        [InlineData("65535", "65535", 1, 65535, 65535)]
        [InlineData("*", "*", 65536, 0, 65535)]
        [InlineData("0-*", "*", 65536, 0, 65535)]
        [InlineData("*-65535", "*", 65536, 0, 65535)]
        [InlineData("*-0", "0", 1, 0, 0)]
        [InlineData("65535-*", "65535", 1, 65535, 65535)]
        [InlineData("*-*", "*", 65536, 0, 65535)]
        public void ParseSingleRangeWithWhitespaceAndWildcardsReturnsExpectedRange(
            string input, string expectedFormat, int expectedCount, int firstPort, int lastPort)
        {
            var ranges = PortRange.Parse(input).ToList();

            var range = Assert.Single(ranges);
            Assert.Equal(expectedFormat, range.ToString());
            Assert.Equal(expectedFormat, PortRange.Format(ranges));
            Assert.Equal(expectedCount, range.Count);
            Assert.True(range.Contains(firstPort));
            Assert.True(range.Contains(lastPort));
            Assert.False(range.Contains(firstPort - 1));
            Assert.False(range.Contains(lastPort + 1));
        }

        [Theory]
        [InlineData("5;1-2;8", "1-2;5;8")]
        [InlineData("5,1-2;8", "1-2;5;8")]
        [InlineData("5;1-2;;;;8,,", "1-2;5;8")]
        [InlineData("1-3;2-4;10", "1-4;10")]
        [InlineData("10;1-5;5-7;7-9", "1-9;10")]
        [InlineData("1-2;3-4", "1-2;3-4")]
        [InlineData("1;1;1", "1")]
        [InlineData("2-4;1-2;4-6", "1-6")]
        public void ParseMultipleRangesSortsAndMergesOnlyOverlappingRanges(
            string input, string expectedFormat)
        {
            var ranges = PortRange.Parse(input).ToList();

            Assert.Equal(expectedFormat, PortRange.Format(ranges));
        }

        [Theory]
        [InlineData("")]
        [InlineData(",")]
        [InlineData(";;;")]
        [InlineData(",;,;,")]
        public void ParseSeparatorsOnlyReturnsEmptyRangeList(string input)
        {
            var ranges = PortRange.Parse(input);

            Assert.Empty(ranges);
            Assert.Equal(string.Empty, PortRange.Format(ranges));
        }

        [Theory]
        [InlineData(" ")]
        [InlineData("abc")]
        [InlineData("1,abc")]
        [InlineData("1 2")]
        [InlineData("1/2")]
        [InlineData("1-")]
        [InlineData("-1")]
        [InlineData("1-2-3")]
        [InlineData("1--2")]
        [InlineData("-1-2")]
        public void ParseMalformedNumericFormatsThrowsFormatException(string input)
        {
            Assert.Throws<FormatException>(() => PortRange.Parse(input));
        }

        [Theory]
        [InlineData("2-1")]
        [InlineData("0-65536")]
        [InlineData("65536")]
        [InlineData("65535-65536")]
        public void ParseOutOfRangeOrReversedBoundsThrowsArgumentException(string input)
        {
            Assert.Throws<ArgumentException>(() => PortRange.Parse(input));
        }

        [Theory]
        [InlineData("")]
        [InlineData(",")]
        [InlineData("0-1")]
        [InlineData("1,0")]
        public void TryParseReturnsRangesForAcceptedInputs(string input)
        {
            var success = PortRange.TryParse(input, out var ranges);

            Assert.True(success);
            Assert.NotNull(ranges);
            Assert.Equal(PortRange.Format(PortRange.Parse(input)), PortRange.Format(ranges));
        }

        [Theory]
        [InlineData(" ")]
        [InlineData("abc")]
        [InlineData("2-1")]
        [InlineData("65536")]
        [InlineData("1-2-3")]
        public void TryParseReturnsFalseAndNullForRejectedInputs(string input)
        {
            var success = PortRange.TryParse(input, out var ranges);

            Assert.False(success);
            Assert.Null(ranges);
        }

        [Fact]
        public void TryParseReturnsFalseForNullInput()
        {
            var success = PortRange.TryParse(null!, out var ranges);

            Assert.False(success);
            Assert.Null(ranges);
        }

        [Fact]
        public void FormatUsesSemicolonBetweenRanges()
        {
            var ranges = new[]
            {
                new PortRange(10),
                new PortRange(20, 22),
                new PortRange(30, IPEndPoint.MaxPort)
            };

            var formatted = PortRange.Format(ranges);

            Assert.Equal("10;20-22;30-*", formatted);
        }

        [Fact]
        public void FormatEmptyRangeListReturnsEmptyString()
        {
            var formatted = PortRange.Format(Array.Empty<PortRange>());

            Assert.Equal(string.Empty, formatted);
        }

        [Fact]
        public void ToStringFormatsAllBoundaryShapes()
        {
            Assert.Equal("0", new PortRange(0).ToString());
            Assert.Equal("0-1", new PortRange(0, 1).ToString());
            Assert.Equal("*", new PortRange(IPEndPoint.MinPort, IPEndPoint.MaxPort).ToString());
            Assert.Equal("1-*", new PortRange(1, IPEndPoint.MaxPort).ToString());
            Assert.Equal("65535", new PortRange(IPEndPoint.MaxPort).ToString());
        }

        [Fact]
        public void ConstructorNormalizesReversedBounds()
        {
            var range = new PortRange(10, 5);

            Assert.Equal(new PortRange(5, 10), range);
            Assert.Equal(6, range.Count);
            Assert.Equal("5-10", range.ToString());
        }

        [Fact]
        public void ConstructorClampsBoundsToValidPortRange()
        {
            var range = new PortRange(-10, IPEndPoint.MaxPort + 1);

            Assert.Equal(new PortRange(IPEndPoint.MinPort, IPEndPoint.MaxPort), range);
            Assert.Equal("*", range.ToString());
        }

        [Fact]
        public void ContainsRejectsPortsOutsideRange()
        {
            var range = new PortRange(10, 20);

            Assert.False(range.Contains(9));
            Assert.True(range.Contains(10));
            Assert.True(range.Contains(15));
            Assert.True(range.Contains(20));
            Assert.False(range.Contains(21));
        }

        [Theory]
        [InlineData(10, 20, 20, 30, true)]
        [InlineData(10, 20, 11, 19, true)]
        [InlineData(10, 20, 0, 10, true)]
        [InlineData(10, 20, 0, 9, false)]
        [InlineData(10, 20, 21, 30, false)]
        public void OverlapsReturnsExpectedResult(
            int leftLow, int leftHigh, int rightLow, int rightHigh, bool expected)
        {
            var left = new PortRange(leftLow, leftHigh);
            var right = new PortRange(rightLow, rightHigh);

            Assert.Equal(expected, left.Overlaps(right));
            Assert.Equal(expected, right.Overlaps(left));
        }

        [Fact]
        public void EqualsReturnsFalseForNullAndDifferentType()
        {
            var range = new PortRange(1, 2);

            Assert.False(range.Equals(null));
            Assert.False(range.Equals("1-2"));
        }

        [Fact]
        public void EqualRangesHaveSameHashCode()
        {
            var left = new PortRange(1, 2);
            var right = new PortRange(1, 2);

            Assert.Equal(left, right);
            Assert.Equal(left.GetHashCode(), right.GetHashCode());
        }

        [Fact]
        public void GetEndpointsYieldsEndpointForEveryPortInRange()
        {
            var endpoints = new PortRange(10, 12)
                .GetEndpoints(IPAddress.Loopback)
                .ToList();

            Assert.Collection(endpoints,
                endpoint =>
                {
                    Assert.Equal(IPAddress.Loopback, endpoint.Address);
                    Assert.Equal(10, endpoint.Port);
                },
                endpoint =>
                {
                    Assert.Equal(IPAddress.Loopback, endpoint.Address);
                    Assert.Equal(11, endpoint.Port);
                },
                endpoint =>
                {
                    Assert.Equal(IPAddress.Loopback, endpoint.Address);
                    Assert.Equal(12, endpoint.Port);
                });
        }

        [Fact]
        public void OpcUaContainsDefaultDiscoveryPorts()
        {
            var ranges = PortRange.OpcUa.ToList();

            var range = Assert.Single(ranges);
            Assert.Equal(new PortRange(4840, 4841), range);
            Assert.Equal("4840-4841", PortRange.Format(ranges));
        }

        [Fact]
        public void WellKnownContainsExpectedPublisherPortRanges()
        {
            var ranges = PortRange.WellKnown.ToList();

            Assert.Equal(new[]
            {
                new PortRange(4840, 4841),
                new PortRange(48000, 48100),
                new PortRange(49320),
                new PortRange(50000),
                new PortRange(51200, 51300),
                new PortRange(62222)
            }, ranges);
            Assert.Equal("4840-4841;48000-48100;49320;50000;51200-51300;62222",
                PortRange.Format(ranges));
        }

        [Fact]
        public void AllContainsFullPortRange()
        {
            var range = Assert.Single(PortRange.All);

            Assert.Equal(new PortRange(IPEndPoint.MinPort, IPEndPoint.MaxPort), range);
            Assert.Equal("*", range.ToString());
        }

        [Fact]
        public void UnassignedEnumeratesIanaUnassignedPortRanges()
        {
            var ranges = PortRange.Unassigned.ToList();

            Assert.Equal(824, ranges.Count);
            Assert.Equal(new PortRange(4), ranges[0]);
            Assert.Equal(new PortRange(6), ranges[1]);
            Assert.Equal(new PortRange(272, 279), ranges[20]);
            Assert.Equal(new PortRange(1895), ranges[53]);
            Assert.Equal(new PortRange(1895), ranges[54]);
            Assert.Equal(new PortRange(6552, 6557), ranges[236]);
            Assert.Equal(new PortRange(7888, 7899), ranges[351]);
            Assert.Equal(new PortRange(49002, IPEndPoint.MaxPort), ranges[^1]);
        }

        [Fact]
        public void MergeCanBeAppliedToUnassignedRangesByRoundTrippingThroughFormatAndParse()
        {
            var merged = PortRange.Parse(PortRange.Format(PortRange.Unassigned)).ToList();

            Assert.Equal(816, merged.Count);
            Assert.Equal(new PortRange(4), merged[0]);
            Assert.Equal(new PortRange(904, 909), merged[47]);
            Assert.Equal(new PortRange(49002, IPEndPoint.MaxPort), merged[^1]);
        }
    }
}
