// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System
{
    using System.Collections.Generic;
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

        /// <summary>
        /// Convert to base 16.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="upperCase"></param>
        /// <returns></returns>
        public static string ToBase16String(this byte[] value,
            bool upperCase = true)
        {
            var charLookup = upperCase ?
                "0123456789ABCDEF" : "0123456789abcdef";
            var chars = new char[value.Length * 2];
            var j = 0;
            for (var i = 0; i < value.Length; i++)
            {
                chars[j++] = charLookup[value[i] >> 4];
                chars[j++] = charLookup[value[i] & 0xf];
            }
            return new string(chars);
        }

        /// <summary>
        /// Convert to base 64.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ToBase64String(this byte[] value)
        {
            return Convert.ToBase64String(value);
        }

        /// <summary>
        /// Encode for use in a URL.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string UrlEncode(this string value)
        {
            return Uri.EscapeDataString(value);
        }

        /// <summary>
        /// Decode from URL encoding.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string UrlDecode(this string value)
        {
            return Uri.UnescapeDataString(value.Replace("+", " ", StringComparison.Ordinal));
        }

        /// <summary>
        /// Trims matching quotes.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string TrimQuotes(this string value)
        {
            var trimmed = value.TrimMatchingChar('"');
            return trimmed == value ? value.TrimMatchingChar('\'') : trimmed;
        }

        /// <summary>
        /// Trims a char from front and back if both match.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="match"></param>
        /// <returns></returns>
        public static string TrimMatchingChar(this string value, char match)
        {
            if (value.Length >= 2 && value[0] == match && value[^1] == match)
            {
                return value[1..^1];
            }
            return value;
        }

        /// <summary>
        /// Returns a query and fragmentless URI.
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public static Uri NoQueryAndFragment(this Uri uri)
        {
            return new UriBuilder(uri) { Fragment = null, Query = null }.Uri;
        }

        /// <summary>
        /// Cast object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static T As<T>(this object value)
        {
            return (T)value;
        }
    }
}
