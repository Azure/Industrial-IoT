// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System
{
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Text;

    /// <summary>
    /// String helper extensions
    /// </summary>
    public static class StringEx
    {
        /// <summary>
        /// Yet another case insensitve equals
        /// </summary>
        /// <param name="str"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public static bool EqualsIgnoreCase(this string str, string to)
        {
            return StringComparer.OrdinalIgnoreCase.Equals(str, to);
        }

        /// <summary>
        /// Convert from base 64
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static byte[] DecodeAsBase64(this string str)
        {
            if (str == null)
            {
                return null;
            }
            return Convert.FromBase64String(str);
        }

        /// <summary>
        /// Check if base 16
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool IsBase16(this string str)
        {
            try
            {
                DecodeAsBase16(str);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Convert from base 16
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static byte[] DecodeAsBase16(this string str)
        {
            if (str == null)
            {
                return null;
            }
            if (str.Length % 2 != 0)
            {
                throw new ArgumentException("Invalid length", nameof(str));
            }
            var bytes = new byte[str.Length / 2];
            for (var i = 0; i < str.Length; i += 2)
            {
                var s = str.Substring(i, 2);
                bytes[i / 2] = byte.Parse(s, Globalization.NumberStyles.HexNumber, null);
            }
            return bytes;
        }

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
        /// Removes all whitespace and replaces it with single space.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string SingleSpacesNoLineBreak(this string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }
            var builder = new StringBuilder();
            var lastCharWasWs = false;
            foreach (var c in value)
            {
                if (char.IsWhiteSpace(c))
                {
                    lastCharWasWs = true;
                    continue;
                }
                if (lastCharWasWs)
                {
                    builder.Append(' ');
                    lastCharWasWs = false;
                }
                builder.Append(c);
            }
            return builder.ToString();
        }
    }
}
