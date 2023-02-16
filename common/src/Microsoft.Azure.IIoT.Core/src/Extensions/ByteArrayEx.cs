// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System {
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;

    /// <summary>
    /// Byte buffer extensions
    /// </summary>
    public static class ByteArrayEx {

        /// <summary>
        /// Convert to base 16
        /// </summary>
        /// <param name="value"></param>
        /// <param name="upperCase"></param>
        /// <returns></returns>
        public static string ToBase16String(this byte[] value,
            bool upperCase = true) {
            if (value == null) {
                return null;
            }
            var charLookup = upperCase ?
                "0123456789ABCDEF" : "0123456789abcdef";
            var chars = new char[value.Length * 2];
            // no checking needed here
            var j = 0;
            for (var i = 0; i < value.Length; i++) {
                chars[j++] = charLookup[value[i] >> 4];
                chars[j++] = charLookup[value[i] & 0xf];
            }
            return new string(chars);
        }

        /// <summary>
        /// Convert to base 64
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ToBase64String(this byte[] value) {
            if (value == null) {
                return null;
            }
            return Convert.ToBase64String(value);
        }


        /// <summary>
        /// Hashes the string
        /// </summary>
        /// <param name="bytestr">string to hash</param>
        /// <returns></returns>
        [Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA5350:Do Not Use Weak Cryptographic Algorithms",
            Justification = "SHA1 not used for crypto operation.")]
        public static string ToSha1Hash(this byte[] bytestr) {
            if (bytestr == null) {
                return null;
            }
#pragma warning disable SYSLIB0021 // Type or member is obsolete
            using (var sha1 = new SHA1Managed()) {
                var hash = sha1.ComputeHash(bytestr);
                return hash.ToBase16String(false);
            }
#pragma warning restore SYSLIB0021 // Type or member is obsolete
        }

        /// <summary>
        /// Zip string to byte array
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static byte[] Zip(this byte[] str) {
            using (var input = new MemoryStream(str)) {
                return input.Zip();
            }
        }

        /// <summary>
        /// Unzip from byte array to string
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static byte[] Unzip(this byte[] bytes) {
            using (var input = new MemoryStream(bytes)) {
                return input.Unzip();
            }
        }
    }
}
