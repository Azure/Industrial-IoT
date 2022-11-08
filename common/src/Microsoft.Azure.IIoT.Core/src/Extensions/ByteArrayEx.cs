// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System {
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;

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
        /// Test base 16
        /// </summary>
        /// <param name="base16"></param>
        /// <param name="upperCase"></param>
        /// <returns></returns>
        public static bool IsBase16(this string base16,
            bool upperCase = true) {
            if (string.IsNullOrWhiteSpace(base16)) {
                return false;
            }
            var charLookup = upperCase ?
                "0123456789ABCDEF" : "0123456789abcdef";
            foreach (var c in base16) {
                if (!charLookup.Contains(c))
                    return false;
            }
            return true;
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
        /// Convert a byte array to string and limit to certain size.
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="size"></param>
        public static string ToString(this byte[] bytes, int size) {
            var truncate = bytes.Length > size;
            var length = truncate ? size : bytes.Length;
            var ascii = IsAscii(bytes, length);
            var content = ascii ? Encoding.ASCII.GetString(bytes, 0, length) :
                BitConverter.ToString(bytes, 0, length);
            length = content.IndexOf('\n');
            if (length > 0) {
                return content.Substring(0, length);
            }
            return content;
        }

        /// <summary>
        /// Check whether bytes are all ascii
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static bool IsAscii(this byte[] bytes, int length) {
            return bytes.Take(length).All(x => x > 32 || x <= 127);
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
