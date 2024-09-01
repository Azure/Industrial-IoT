// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System
{
    using System.Security.Cryptography;
    using System.Text;

    /// <summary>
    /// String helper extensions
    /// </summary>
    public static class StringEx
    {
        /// <summary>
        /// Hashes the string
        /// </summary>
        /// <param name="str">string to hash</param>
        /// <returns></returns>
        public static string ToSha1Hash(this string str)
        {
            return Encoding.UTF8.GetBytes(str).ToSha1Hash();
        }

        /// <summary>
        /// Hashes the string
        /// </summary>
        /// <param name="str">string to hash</param>
        /// <returns></returns>
        public static string ToSha2Hash(this string str)
        {
            return Encoding.UTF8.GetBytes(str).ToSha2Hash();
        }

        /// <summary>
        /// Create guid from string using sha2
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static Guid ToGuid(this string? str)
        {
            if (string.IsNullOrWhiteSpace(str))
            {
                return Guid.Empty;
            }
            return new Guid(SHA256.HashData(Encoding.UTF8.GetBytes(str)).AsSpan().Slice(0, 16));
        }

        /// <summary>
        /// Create Uuid from string using sha2
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static Opc.Ua.Uuid ToUuid(this string? str)
        {
            return (Opc.Ua.Uuid)ToGuid(str);
        }

        /// <summary>
        /// Hashes the string
        /// </summary>
        /// <param name="bytestr">string to hash</param>
        /// <returns></returns>
        [Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA5350:Do Not Use Weak Cryptographic Algorithms",
        Justification = "SHA1 not used for crypto operation.")]
        public static string ToSha1Hash(this byte[] bytestr)
        {
            var hash = SHA1.HashData(bytestr);
            return hash.ToBase16String(false);
        }

        /// <summary>
        /// Hashes the string
        /// </summary>
        /// <param name="bytestr">string to hash</param>
        /// <returns></returns>
        public static string ToSha2Hash(this byte[] bytestr)
        {
            var hash = SHA256.HashData(bytestr);
            return hash.ToBase16String(false);
        }
    }
}
