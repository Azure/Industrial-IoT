// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System {
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;

    /// <summary>
    /// Byte buffer extensions
    /// </summary>
    public static class ByteArrayEx {

        /// <summary>
        /// Clone byte buffer
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static byte[] Copy(this byte[] bytes) {
            var copy = new byte[bytes.Length];
            bytes.CopyTo(copy, 0);
            return copy;
        }

        /// <summary>
        /// Convert to base 16
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ToBase16String(this byte[] value) {
            if (value == null) {
                return null;
            }
            const string hexAlphabet = @"0123456789abcdef";
            var chars = new char[checked(value.Length * 2)];
            unchecked {
                for (var i = 0; i < value.Length; i++) {
                    chars[i * 2] = hexAlphabet[value[i] >> 4];
                    chars[i * 2 + 1] = hexAlphabet[value[i] & 0xF];
                }
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
        public static string ToSha1Hash(this byte[] bytestr) {
            if (bytestr == null) {
                return null;
            }
            using (var sha1 = new SHA1Managed()) {
                var hash = sha1.ComputeHash(bytestr);
                return hash.ToBase16String();
            }
        }

        /// <summary>
        /// Append byte array to string builder
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
        /// Check whether bytes are ascii
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static bool IsAscii(this byte[] bytes, int length) =>
            bytes.Take(length).All(x => x > 32 || x <= 127);
    }
}
