// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders
{
    using System;
    using System.Buffers;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;

    internal static class Extensions
    {
        /// <summary>
        /// Sanitize file name
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static string SanitizeFileName(this string fileName)
        {
            if (fileName.Length > 250)
            {
                fileName = new string(fileName.AsSpan()[..230]);
                fileName += fileName.ToSha1Hash();
            }
            return fileName;
        }

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
