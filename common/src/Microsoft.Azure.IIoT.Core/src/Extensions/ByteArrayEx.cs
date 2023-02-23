// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System
{
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;

    /// <summary>
    /// Byte buffer extensions
    /// </summary>
    public static class ByteArrayEx
    {
        /// <summary>
        /// Hashes the string
        /// </summary>
        /// <param name="bytestr">string to hash</param>
        /// <returns></returns>
        [Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA5350:Do Not Use Weak Cryptographic Algorithms",
            Justification = "SHA1 not used for crypto operation.")]
        public static string ToSha1Hash(this byte[] bytestr)
        {
            if (bytestr == null)
            {
                return null;
            }
            var hash = SHA1.HashData(bytestr);
            return hash.ToBase16String(false);
        }

        /// <summary>
        /// Zip string to byte array
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static byte[] Zip(this byte[] str)
        {
            using (var input = new MemoryStream(str))
            {
                return input.Zip();
            }
        }

        /// <summary>
        /// Unzip from byte array to string
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static byte[] Unzip(this byte[] bytes)
        {
            using (var input = new MemoryStream(bytes))
            {
                return input.Unzip();
            }
        }
    }
}
