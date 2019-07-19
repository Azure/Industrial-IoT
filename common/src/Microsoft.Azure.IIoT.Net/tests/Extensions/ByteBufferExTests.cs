// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Net {
    using System;
    using System.Text;
    using Xunit;

    public class ByteBufferExTests {

        [Fact]
        public void TestReadUint8() {
            var offset = 3;
            var data = new byte[] { 0, 2, 3, 4, 5 };
            var val = data.ReadUInt8(ref offset);

            Assert.Equal(4, val);
            Assert.Equal(4, offset);
        }

        [Fact]
        public void TestReadUint8Throws() {
            var offset = 8;
            var data = new byte[] { 0, 2, 3, 4, 5 };

            Assert.Throws<IndexOutOfRangeException>(() => data.ReadUInt8(ref offset));
        }

        [Fact]
        public void TestReadUint16HostOrder() {
            var offset = 1;
            var data = new byte[] { 0, 2, 3, 4, 5 };
            var val = data.ReadUInt16(ref offset);

            Assert.Equal(0x0302, val);
            Assert.Equal(3, offset);
        }

        [Fact]
        public void TestReadUint16() {
            var offset = 1;
            var data = new byte[] { 0, 2, 3, 4, 5 };
            var val = data.ReadUInt16(ref offset, true);

            Assert.Equal(0x0203, val);
            Assert.Equal(3, offset);
        }

        [Fact]
        public void TestReadUint16Throws() {
            var offset = 4;
            var data = new byte[] { 0, 2, 3, 4, 5 };

            Assert.Throws<ArgumentException>(() => data.ReadUInt16(ref offset));
            Assert.Throws<ArgumentException>(() => data.ReadUInt16(ref offset, true));
        }

        [Fact]
        public void TestReadUint32HostOrder() {
            var offset = 1;
            var data = new byte[] { 0, 2, 3, 4, 5 };
            var val = data.ReadUInt32(ref offset);

            Assert.Equal(0x05040302u, val);
            Assert.Equal(5, offset);
        }

        [Fact]
        public void TestReadUint32() {
            var offset = 1;
            var data = new byte[] { 0, 2, 3, 4, 5 };
            var val = data.ReadUInt32(ref offset, true);

            Assert.Equal(0x02030405u, val);
            Assert.Equal(5, offset);
        }

        [Fact]
        public void TestReadUint32Throws() {
            var offset = 4;
            var data = new byte[] { 0, 2, 3, 4, 5 };

            Assert.Throws<ArgumentException>(() => data.ReadUInt32(ref offset));
            Assert.Throws<ArgumentException>(() => data.ReadUInt32(ref offset, true));
        }

        [Fact]
        public void TestReadBuffer1() {
            var offset = 4;
            var data = new byte[] { 0, 2, 3, 4, 5, 6, 7, 8, 9 };
            var expected = new byte[] { 5, 6, 7 };

            var value = data.ReadByteArray(ref offset, 3, 10);
            Assert.Equal(3, value.Length);
            Assert.Equal(expected, value);
            Assert.Equal(14, offset);
        }

        [Fact]
        public void TestReadBuffer2() {
            var offset = 4;
            var data = new byte[] { 0, 2, 3, 4, 5, 6, 7, 8, 9 };
            var expected = new byte[] { 5, 6, 7 };

            var value = data.ReadByteArray(ref offset, 3);
            Assert.Equal(3, value.Length);
            Assert.Equal(expected, value);
            Assert.Equal(7, offset);
        }

        [Fact]
        public void TestReadBuffer3() {
            var offset = 4;
            var data = new byte[] { 0, 2, 3, 4, 5, 6, 7, 8, 9 };
            var expected = new byte[] { 5, 6, 7 };

            var value = data.ReadByteArray(ref offset, 3, 3);
            Assert.Equal(3, value.Length);
            Assert.Equal(expected, value);
            Assert.Equal(7, offset);
        }

        [Fact]
        public void TestReadBufferThrows() {
            var offset = 2;
            var data = new byte[] { 0, 2, 3, 4, 5 };

            Assert.Throws<ArgumentException>(() => data.ReadByteArray(ref offset, 5));
            Assert.Throws<ArgumentException>(() => data.ReadByteArray(ref offset, 5));
        }

        [Fact]
        public void TestUInt16ToBytes1HostOrder() {
            var value = (ushort)1u;
            var expected = new byte[] { 1, 0 };
            var result = value.ToBytes();
            Assert.Equal(expected, result);
        }

        [Fact]
        public void TestUInt16ToBytes1() {
            var value = (ushort)1u;
            var expected = new byte[] { 0, 1 };
            var result = value.ToBytes(true);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void TestUInt16ToBytes2() {
            var value = (ushort)0u;
            var expected = new byte[] { 0, 0 };
            var result = value.ToBytes();
            Assert.Equal(expected, result);
        }

        [Fact]
        public void TestUInt32ToBytes1HostOrder() {
            var value = 1u;
            var expected = new byte[] { 1, 0, 0, 0 };
            var result = value.ToBytes();
            Assert.Equal(expected, result);
        }

        [Fact]
        public void TestUInt32ToBytes1() {
            var value = 1u;
            var expected = new byte[] { 0, 0, 0, 1 };
            var result = value.ToBytes(true);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void TestUInt32ToBytes2() {
            var value = 0u;
            var expected = new byte[] { 0, 0, 0, 0 };
            var result = value.ToBytes();
            Assert.Equal(expected, result);
        }

        [Fact]
        public void TestStringToBytes() {
            var value = "aaaa";
            var expected = new byte[] { 97, 97, 97, 97 };
            var result = value.ToBytes();
            Assert.Equal(expected, result);
        }

        [Fact]
        public void TestStringToBytesUtf8() {
            var value = "aaaa";
            var expected = new byte[] { 97, 97, 97, 97 };
            var result = value.ToBytes(Encoding.UTF8);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void TestEmptyStringToBytes() {
            var value = string.Empty;
            var result = value.ToBytes();
            Assert.Null(result);
        }

        [Fact]
        public void TestBytesToString() {
            var value = new byte[] { 97, 97, 97, 97 };
            var expected = "aaaa";
            var result = value.ToEncodedString();
            Assert.Equal(expected, result);
        }

        [Fact]
        public void TestBytesToStringUtf8() {
            var value = new byte[] { 97, 97, 97, 97 };
            var expected = "aaaa";
            var result = value.ToEncodedString(Encoding.UTF8);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void TestBytesToStringUtf8WithOffset() {
            var value = new byte[] { 97, 97, 97, 97 };
            var expected = "aa";
            var result = value.ToEncodedString(Encoding.UTF8, 2);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void TestNullBytesToEmptyString() {
            byte[] value = null;
            var result = value.ToEncodedString();
            var expected = string.Empty;
            Assert.Equal(expected, result);
        }
    }
}
