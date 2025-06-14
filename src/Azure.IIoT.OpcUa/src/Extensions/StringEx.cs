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
        /// <param name="bytestr">string to hash</param>
        /// <returns></returns>
        [Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA5350:Do Not Use Weak Cryptographic Algorithms",
        Justification = "SHA1 not used for crypto operation.")]
        public static string ToSha1Hash(this byte[] bytestr)
        {
            var hash = SHA1.HashData(bytestr);
            return hash.ToBase16String(false);
        }
    }
}
