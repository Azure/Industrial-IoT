// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Net.Dhcp {
    using Microsoft.Azure.IIoT.Net.Dhcp.v4;
    using Xunit;
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Text;

    public class DhcpMessageExTests {

        [Fact]
        void TestReadOption1() {
            var offset = 1;
            var data = new byte[] { 0, 2, 3, 4, 5, 6, 7, 8, 9 };

            var value = data.ReadOption(out var option, ref offset);
            Assert.Equal(new byte[] { 4, 5, 6 }, value);
            Assert.Equal(DhcpOption.TimeOffset, option);
            Assert.Equal(6, offset);
        }

        [Fact]
        void TestReadOption2() {
            var offset = 0;
            var data = new byte[] { 0, 2, 3, 4, 5, 6, 7, 8, 9 };

            var value = data.ReadOption(out var option, ref offset);
            Assert.Null(value);
            Assert.Equal(DhcpOption.Pad, option);
            Assert.Equal(3, offset);
        }

        [Fact]
        void TestReadOption3() {
            var offset = 1;
            var data = new byte[] { 0, 255, 3, 4, 5, 6, 7, 8, 9 };

            var value = data.ReadOption(out var option, ref offset);
            Assert.Null(value);
            Assert.Equal(DhcpOption.End, option);
            Assert.Equal(2, offset);
        }

        [Fact]
        void TestReadOption4() {
            var offset = 1;
            var data = new byte[] { 0, 6, 0, 4, 5, 6, 7, 8, 9 };

            var value = data.ReadOption(out var option, ref offset);
            Assert.Equal(new byte[0], value);
            Assert.Equal(DhcpOption.DomainNameServers, option);
            Assert.Equal(3, offset);
        }

        [Fact]
        void TestReadOptionThrows() {
            var offset = 2;
            var data = new byte[] { 0, 2, 3, 4, 5 };
            Assert.Throws<ArgumentException>(() => data.ReadOption(out var option, ref offset));
        }

        [Fact]
        void TestAddressToBytes1() {
            var value = new IPAddress(new byte[] { 4, 5, 6, 7 });
            var expected = new byte[] { 4, 5, 6, 7 };

            var result = value.ToBytes();
            Assert.Equal(expected, result);
        }

        [Fact]
        void TestAddressToBytesThrows1() {
            var data = new byte[] { 0, 2, 3, 4, 5 };
            Assert.Throws<FormatException>(() => data.ToIPAddress());
        }

        [Fact]
        void TestAddressToBytesThrows2() {
            var data = new byte[] { 0, 2, 3, };
            Assert.Throws<FormatException>(() => data.ToIPAddress());
        }

        [Fact]
        void TestAddressToBytes2() {
            var value = new IPAddress(0);
            var result = value.ToBytes();
            Assert.Null(result);
        }

        [Fact]
        void TestAddressesToBytes1() {
            var value = new List<IPAddress> {
                new IPAddress(new byte[] { 4, 5, 6, 7 }),
                new IPAddress(new byte[] { 4, 5, 6, 7 })
            };
            var expected = new byte[] { 4, 5, 6, 7, 4, 5, 6, 7 };
            var result = value.ToBytes();
            Assert.Equal(expected, result);
        }

        [Fact]
        void TestAddressesToBytes2() {
            var value = new List<IPAddress> { };
            var expected = new byte[] { };
            var result = value.ToBytes();
            Assert.Equal(expected, result);
        }

        [Fact]
        void TestAddressesToBytesThrows1() {
            var data = new byte[] { 0, 2, 3, 4, 5 };
            Assert.Throws<FormatException>(() => data.ToIPAddresses());
        }

        [Fact]
        void TestAddressesToBytesThrows2() {
            var data = new byte[] { 0, 2, 3, };
            Assert.Throws<FormatException>(() => data.ToIPAddresses());
        }

        [Fact]
        void TestBytesToAddress1() {
            var value = new byte[] { 4, 5, 6, 7 };
            var expected = new IPAddress(new byte[] { 4, 5, 6, 7 });

            var result = value.ToIPAddress();
            Assert.Equal(expected, result);
        }

        [Fact]
        void TestBytesToAddress2() {
            byte[] value = null;
            var result = value.ToIPAddress();
            Assert.Null(result);
        }

        [Fact]
        void TestBytesToAddresses1() {
            var value = new byte[] { 4, 5, 6, 7, 4, 5, 6, 7 };
            var expected = new List<IPAddress> {
                new IPAddress(new byte[] { 4, 5, 6, 7 }),
                new IPAddress(new byte[] { 4, 5, 6, 7 })
            };
            var result = value.ToIPAddresses();
            Assert.Equal(expected, result);
        }

        [Fact]
        void TestBytesToAddresses2() {
            var value = new byte[] { };
            var expected = new List<IPAddress> { };
            var result = value.ToIPAddresses();
            Assert.Equal(expected, result);
        }

        [Fact]
        void TestBytesToAddressesWithNull() {
            byte[] value = null;
            var expected = new List<IPAddress> { };
            var result = value.ToIPAddresses();
            Assert.Equal(expected, result);
        }


        [Fact]
        void TestTimeSpanToBytes1HostOrder() {
            var value = TimeSpan.FromSeconds(1);
            var expected = new byte[] { 1, 0, 0, 0 };
            var result = value.ToBytes();
            Assert.Equal(expected, result);
        }

        [Fact]
        void TestTimeSpanToBytes1() {
            var value = TimeSpan.FromSeconds(1);
            var expected = new byte[] { 0, 0, 0, 1 };
            var result = value.ToBytes(true);
            Assert.Equal(expected, result);
        }

        [Fact]
        void TestTimeSpanToBytes2() {
            var value = TimeSpan.FromSeconds(0);
            var expected = new byte[] { 0, 0, 0, 0 };
            var result = value.ToBytes();
            Assert.Equal(expected, result);
        }

        [Fact]
        void TestTimeSpanNullableToBytes() {
            var value = (TimeSpan?)null;
            var result = value.ToBytes(true);
            Assert.Null(result);
        }

        [Fact]
        void TestBytesToTimeSpan1HostOrder() {
            var value = new byte[] { 1, 0, 0, 0 };
            var expected = TimeSpan.FromSeconds(1);
            var result = value.ToSecondsDuration();
            Assert.Equal(expected, result);
        }

        [Fact]
        void TestBytesToTimeSpan1() {
            var value = new byte[] { 0, 0, 0, 1 };
            var expected = TimeSpan.FromSeconds(1);
            var result = value.ToSecondsDuration(true);
            Assert.Equal(expected, result);
        }

        [Fact]
        void TestBytesToTimeSpan2() {
            var value = new byte[] { 0, 0, 0, 0 };
            var expected = TimeSpan.FromSeconds(0);
            var result = value.ToSecondsDuration();
            Assert.Equal(expected, result);
        }

        [Fact]
        void TestBytesToNullableTimeSpan() {
            var value = (byte[])null;
            var buffer1 = value.ToSecondsDuration(true);
            var buffer2 = value.ToSecondsDuration();
            Assert.Null(buffer1);
            Assert.Null(buffer2);
        }

        [Fact]
        void TestBytesToTimeSpanThrows() {
            var data = new byte[] { 0, 2, };
            Assert.Throws<ArgumentException>(() => data.ToSecondsDuration());
        }


#if FALSE


        /// <summary>
        /// Convert bytes to list of addresses
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static List<IPAddress> ToIPAddresses(this byte[] data) {
            if (data != null) {
                if ((data.Length % 4) != 0) {
                    throw new FormatException("Bad length of addresses");
                }
                var buffer = new byte[4];
                var list = new List<IPAddress>();
                for (var offset = 0; offset < data.Length; offset += 4) {
                    data.CopyTo(buffer, offset);
                    list.Add(data.ToIPAddress());
                }
            }
            return null;
        }

#endif
    }
}
