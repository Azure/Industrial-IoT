// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders
{
    using System.Buffers;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;

    internal static class Extensions
    {
        /// <summary>
        /// Decompress
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static ReadOnlySequence<byte> GzipDecompress(this ReadOnlySequence<byte> data)
        {
            using var compressedStream = new MemoryStream(data.ToArray());
            using var gzip = new GZipStream(compressedStream, CompressionMode.Decompress);
            return new ReadOnlySequence<byte>(gzip.ReadAsBuffer());
        }

        /// <summary>
        /// Compress
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static ReadOnlySequence<byte> GzipCompress(this ReadOnlySequence<byte> data)
        {
            using var compressedStream = new MemoryStream();
            using (var gzip = new GZipStream(compressedStream, CompressionMode.Compress))
            {
                foreach (var buffer in data)
                {
                    gzip.Write(buffer.Span);
                }
            }
            return new ReadOnlySequence<byte>(compressedStream.ToArray());
        }
    }
}
