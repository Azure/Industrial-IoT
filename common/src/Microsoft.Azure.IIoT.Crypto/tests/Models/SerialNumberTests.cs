// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto.Models {
    using System.Numerics;
    using Xunit;
    using System;

    /// <summary>
    /// Serial number tests
    /// </summary>
    public class SerialNumberTests {

        [Theory]
        [InlineData(1)]
        [InlineData(346234)]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(-346234)]
        [InlineData(long.MinValue)]
        [InlineData(long.MaxValue)]
        public void ConvertToStringAndParse(long value) {

            var serial1 = new SerialNumber(new BigInteger(value));
            var serial2 = SerialNumber.Parse(serial1.ToString());
            Assert.Equal(serial1, serial2);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(1000)]
        [InlineData(4)]
        public void ConvertToStringAndParseSize(long value) {

            var serial1 = new SerialNumber(value);
            var serial2 = SerialNumber.Parse(serial1.ToString());
            Assert.Equal(serial1, serial2);
        }

        [Fact]
        public void TestArgumentThrows() {
            Assert.Throws<ArgumentOutOfRangeException>(() => new SerialNumber(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SerialNumber(0));
            Assert.Throws<ArgumentNullException>(() => new SerialNumber(null));
            Assert.Throws<ArgumentException>(() => new SerialNumber(new byte[0]));
        }
    }
}
