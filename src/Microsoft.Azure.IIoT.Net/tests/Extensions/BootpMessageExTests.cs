// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Net.Bootp {
    using Xunit;
    using System;
    using System.Net;
    using System.Net.NetworkInformation;
    using System.IO;

    public class BootpMessageExTests {


        [Fact]
        void TestReadAddress() {
            var offset = 3;
            var data = new byte[] { 0, 2, 3, 4, 5, 6, 7, 8, 9 };
            var expected = new IPAddress(new byte[] { 4, 5, 6, 7 });

            var value = data.ReadAddress(ref offset);
            Assert.Equal(expected, value);
            Assert.Equal(7, offset);
        }

        [Fact]
        void TestReadAddressThrows() {
            var offset = 2;
            var data = new byte[] { 0, 2, 3, 4, 5 };
            Assert.Throws<ArgumentException>(() => data.ReadAddress(ref offset));
        }

        [Fact]
        void TestReadMacAddress() {
            var offset = 4;
            var data = new byte[] { 0, 2, 3, 4, 5, 6, 7, 8, 9, 0 };
            var expected = new PhysicalAddress(new byte[] { 5, 6, 7, 8, 9, 0});

            var value = data.ReadPhysical(ref offset, 6);
            Assert.Equal(expected, value);
            Assert.Equal(10, offset);
        }

        [Fact]
        void TestReadMacAddressThrows() {
            var offset1 = 2;
            var offset2 = 8;
            var data = new byte[] { 0, 2, 3, 4, 5 };
            Assert.Throws<ArgumentException>(() => data.ReadPhysical(
                ref offset1, 8));
            Assert.Throws<ArgumentException>(() => data.ReadPhysical(
                ref offset2, 2));
        }

        [Fact]
        void TestTimeSpanToBytes1() {
            var value = TimeSpan.FromSeconds(1);
            var expected = new byte[] { 0, 1 };
            using (var stream = new MemoryStream()) {
                stream.WriteUInt16Seconds(value);
                Assert.Equal(expected, stream.ToArray());
            }
        }

        [Fact]
        void TestTimeSpanToBytes2() {
            var value = TimeSpan.FromSeconds(0);
            var expected = new byte[] { 0, 0, 0, 0 };
            using (var stream = new MemoryStream()) {
                stream.WriteUInt16Seconds(value, 4);
                Assert.Equal(expected, stream.ToArray());
            }
        }

        [Fact]
        void TestBytesToUint16TimeSpan1() {
            var offset = 0;
            var value = new byte[] { 0, 1 };
            var expected = TimeSpan.FromSeconds(1);
            var result = value.ReadUInt16Seconds(ref offset);
            Assert.Equal(expected, result);
            Assert.Equal(2, offset);
        }

        [Fact]
        void TestBytesToTimeSpan2() {
            var offset = 0;
            var value = new byte[] { 0, 0, };
            var expected = TimeSpan.FromSeconds(0);
            var result = value.ReadUInt16Seconds(ref offset);
            Assert.Equal(expected, result);
            Assert.Equal(2, offset);
        }

        [Fact]
        void TestBytesToTimeSpanThrows() {
            var offset1 = 0;
            var offset2 = 0;
            var data1 = new byte[] { 0 };
            var data2 = new byte[] { };
            Assert.Throws<ArgumentException>(() => data1.ReadUInt16Seconds(ref offset1));
            Assert.Throws<ArgumentOutOfRangeException>(() => data2.ReadUInt16Seconds(ref offset2));
        }
    }
}
