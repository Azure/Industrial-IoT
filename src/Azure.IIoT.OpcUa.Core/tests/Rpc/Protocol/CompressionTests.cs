// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Rpc.Protocol
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Xunit;

    public sealed class CompressionTests
    {
        [Theory]
        [InlineData("")]
        [InlineData("short")]
        [InlineData("The quick brown fox jumps over the lazy dog.")]
        public void ZipRoundTripsPayload(string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);

            var zipped = bytes.Zip();
            var unzipped = zipped.Unzip();

            Assert.Equal(bytes, unzipped);
        }

        [Fact]
        public void ZipProducesGzipHeader()
        {
            var zipped = Encoding.UTF8.GetBytes("payload").Zip();

            Assert.InRange(zipped.Length, 3, int.MaxValue);
            Assert.Equal(0x1f, zipped[0]);
            Assert.Equal(0x8b, zipped[1]);
        }

        [Fact]
        public void UnzipRejectsInvalidGzipPayload()
        {
            Assert.Throws<InvalidDataException>(() =>
                Encoding.UTF8.GetBytes("not-gzip").Unzip());
        }

        [Fact]
        public void ZipRoundTripsBinaryPayload()
        {
            var bytes = Enumerable.Range(0, 256).Select(i => (byte)i).ToArray();

            var unzipped = bytes.Zip().Unzip();

            Assert.Equal(bytes, unzipped);
        }
    }
}
