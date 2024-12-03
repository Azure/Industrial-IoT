// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Extensions
{
    using System.Linq;
    using Xunit;

    public class SequenceNumberTests
    {
        [Fact]
        public void TestIncrementSequenceNumber0()
        {
            uint v = 3234232;
            var v2 = SequenceNumber.Increment32(ref v);
            Assert.Equal(v, v2);
            Assert.Equal(3234232u + 1u, v);
        }

        [Fact]
        public void TestIncrementSequenceNumber1()
        {
            var v = 0xffffffff;
            var v2 = SequenceNumber.Increment32(ref v);
            Assert.Equal(v, v2);
            Assert.Equal(1u, v);
        }

        [Fact]
        public void TestIncrementSequenceNumber2()
        {
            uint v = 0;
            var v2 = SequenceNumber.Increment32(ref v);
            Assert.Equal(v, v2);
            Assert.Equal(1u, v);
        }

        [Fact]
        public void TestIncrementSequenceNumber3()
        {
            var v = 0xfffffffe;
            var v2 = SequenceNumber.Increment32(ref v);
            Assert.Equal(v, v2);
            Assert.Equal(0xffffffff, v);
        }

        [Fact]
        public void TestGetMissingSequenceNumbers0()
        {
            var missing = SequenceNumber.Missing(4u, 4u, out var dropped).ToList();
            Assert.Empty(missing);
            Assert.False(dropped);
            missing = [.. SequenceNumber.Missing(0u, 0u, out dropped)];
            Assert.Empty(missing);
            Assert.False(dropped);
            missing = [.. SequenceNumber.Missing(0xffffffffu, 0xffffffffu, out dropped)];
            Assert.Empty(missing);
            Assert.False(dropped);
        }

        [Fact]
        public void TestGetMissingSequenceNumbers1()
        {
            var missing = SequenceNumber.Missing(0xffffffffu, 1u, out var dropped).ToList();
            Assert.Empty(missing);
            Assert.False(dropped);
        }

        [Fact]
        public void TestGetMissingSequenceNumbers2()
        {
            var missing = SequenceNumber.Missing(1u, 0xffffffffu, out var dropped).ToList();
            Assert.Empty(missing);
            Assert.False(dropped);
        }

        [Fact]
        public void TestGetMissingSequenceNumbers3()
        {
            var missing = SequenceNumber.Missing(0xfffffffeu, 2u, out var dropped).ToList();
            Assert.Equal(new uint[] { 0xffffffff, 1u }, missing);
            Assert.True(dropped);
            missing = [.. SequenceNumber.Missing(2u, 0xfffffffeu, out dropped)];
            Assert.Equal(new uint[] { 0xffffffff, 1u }, missing);
            Assert.False(dropped);
        }

        [Fact]
        public void TestGetMissingSequenceNumbers4()
        {
            var missing = SequenceNumber.Missing(0xfffffffeu, 3u, out var dropped).ToList();
            Assert.Equal(new uint[] { 0xffffffff, 1u, 2u }, missing);
            Assert.True(dropped);
            missing = [.. SequenceNumber.Missing(3u, 0xfffffffeu, out dropped)];
            Assert.Equal(new uint[] { 0xffffffff, 1u, 2u }, missing);
            Assert.False(dropped);
        }

        [Fact]
        public void TestGetMissingSequenceNumbers5()
        {
            const uint maxInt = unchecked(int.MaxValue);
            const uint minInt = unchecked((uint)int.MinValue);
            var missing = SequenceNumber.Missing(maxInt - 1, minInt + 1, out var dropped).ToList();
            Assert.Equal(new uint[] { maxInt, minInt }, missing);
            Assert.True(dropped);
            missing = [.. SequenceNumber.Missing(minInt + 1, maxInt - 1, out dropped)];
            Assert.Equal(new uint[] { maxInt, minInt }, missing);
            Assert.False(dropped);
            missing = [.. SequenceNumber.Missing(minInt, maxInt, out dropped)];
            Assert.Empty(missing);
            Assert.False(dropped);
            missing = [.. SequenceNumber.Missing(maxInt, minInt, out dropped)];
            Assert.Empty(missing);
            Assert.False(dropped);
            missing = [.. SequenceNumber.Missing(maxInt, maxInt, out dropped)];
            Assert.Empty(missing);
            Assert.False(dropped);
            missing = [.. SequenceNumber.Missing(minInt, minInt, out dropped)];
            Assert.Empty(missing);
            Assert.False(dropped);
        }
    }
}
