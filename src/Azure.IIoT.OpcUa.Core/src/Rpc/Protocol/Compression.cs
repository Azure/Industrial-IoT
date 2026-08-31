// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Rpc.Protocol
{
    using System.IO;
    using System.IO.Compression;

    /// <summary>
    /// Gzip helpers used by the direct-method chunking protocol. The chunk
    /// payloads are gzip compressed on the wire (matching the former Legacy
    /// implementation) so both ends must agree on the algorithm.
    /// </summary>
    internal static class Compression
    {
        /// <summary>
        /// Gzip compress a byte array.
        /// </summary>
        /// <param name="bytes"></param>
        public static byte[] Zip(this byte[] bytes)
        {
            using var input = new MemoryStream(bytes);
            using var result = new MemoryStream();
            using (var gs = new GZipStream(result, CompressionMode.Compress))
            {
                input.CopyTo(gs);
            }
            return result.ToArray();
        }

        /// <summary>
        /// Gzip decompress a byte array.
        /// </summary>
        /// <param name="bytes"></param>
        public static byte[] Unzip(this byte[] bytes)
        {
            using var input = new MemoryStream(bytes);
            using var output = new MemoryStream();
            using (var gs = new GZipStream(input, CompressionMode.Decompress))
            {
                gs.CopyTo(output);
            }
            return output.ToArray();
        }
    }
}
