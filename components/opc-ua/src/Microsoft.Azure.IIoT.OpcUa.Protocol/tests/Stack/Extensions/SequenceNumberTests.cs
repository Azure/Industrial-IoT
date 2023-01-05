// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Extensions {
    using System.Linq;
    using Xunit;

    public class SequenceNumberTests {

        [Fact]
        public void TestIncrementSequenceNumber0() {
            uint v = 3234232;
            uint v2 = SequenceNumber.Increment32(ref v);
            Assert.Equal(v, v2);
            Assert.Equal(3234232u + 1u, v);
        }

        [Fact]
        public void TestIncrementSequenceNumber1() {
            uint v = 0xffffffff;
            uint v2 = SequenceNumber.Increment32(ref v);
            Assert.Equal(v, v2);
            Assert.Equal(1u, v);
        }

        [Fact]
        public void TestIncrementSequenceNumber2() {
            uint v = 0;
            uint v2 = SequenceNumber.Increment32(ref v);
            Assert.Equal(v, v2);
            Assert.Equal(1u, v);
        }

        [Fact]
        public void TestIncrementSequenceNumber3() {
            uint v = 0xfffffffe;
            uint v2 = SequenceNumber.Increment32(ref v);
            Assert.Equal(v, v2);
            Assert.Equal(0xffffffff, v);
        }

        [Fact]
        public void TestGetMissingSequenceNumbers0() {
            var missing = SequenceNumber.Missing(4u, 4u).ToList();
            Assert.Empty(missing);
            missing = SequenceNumber.Missing(0u, 0u).ToList();
            Assert.Empty(missing);
            missing = SequenceNumber.Missing(0xffffffffu, 0xffffffffu).ToList();
            Assert.Empty(missing);
        }

        [Fact]
        public void TestGetMissingSequenceNumbers1() {
            var missing = SequenceNumber.Missing(0xffffffffu, 1u).ToList();
            Assert.Empty(missing);
        }

        [Fact]
        public void TestGetMissingSequenceNumbers2() {
            var missing = SequenceNumber.Missing(1u, 0xffffffffu).ToList();
            Assert.Empty(missing);
        }

        [Fact]
        public void TestGetMissingSequenceNumbers3() {
            var missing = SequenceNumber.Missing(0xfffffffeu, 2u).ToList();
            Assert.Equal(new uint[] { 0xffffffff, 1u }, missing);
            missing = SequenceNumber.Missing(2u, 0xfffffffeu).ToList();
            Assert.Equal(new uint[] { 0xffffffff, 1u }, missing);
        }

        [Fact]
        public void TestGetMissingSequenceNumbers4() {
            var missing = SequenceNumber.Missing(0xfffffffeu, 3u).ToList();
            Assert.Equal(new uint[] { 0xffffffff, 1u, 2u }, missing);
            missing = SequenceNumber.Missing(3u, 0xfffffffeu).ToList();
            Assert.Equal(new uint[] { 0xffffffff, 1u, 2u }, missing);
        }

        [Fact]
        public void TestGetMissingSequenceNumbers5() {
            var maxInt = unchecked((uint)int.MaxValue);
            var minInt = unchecked((uint)int.MinValue);
            var missing = SequenceNumber.Missing(maxInt - 1, minInt + 1).ToList();
            Assert.Equal(new uint[] { maxInt, minInt }, missing);
            missing = SequenceNumber.Missing(minInt + 1, maxInt - 1).ToList();
            Assert.Equal(new uint[] { maxInt, minInt }, missing);
            missing = SequenceNumber.Missing(minInt, maxInt).ToList();
            Assert.Empty(missing);
            missing = SequenceNumber.Missing(maxInt, minInt).ToList();
            Assert.Empty(missing);
            missing = SequenceNumber.Missing(maxInt, maxInt).ToList();
            Assert.Empty(missing);
            missing = SequenceNumber.Missing(minInt, minInt).ToList();
            Assert.Empty(missing);
        }
    }
}
