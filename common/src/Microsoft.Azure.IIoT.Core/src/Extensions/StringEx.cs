// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System {
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// String helper extensions
    /// </summary>
    public static class StringEx {

        /// <summary>
        /// Yet another case insensitve equals
        /// </summary>
        /// <param name="str"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public static bool EqualsIgnoreCase(this string str, string to) {
            return StringComparer.OrdinalIgnoreCase.Equals(str, to);
        }

        /// <summary>
        /// Convert from base 64
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static byte[] DecodeAsBase64(this string str) {
            if (str == null) {
                return null;
            }
            return Convert.FromBase64String(str);
        }

        /// <summary>
        /// Check if base 16
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool IsBase16(this string str) {
            try {
                DecodeAsBase16(str);
                return true;
            }
            catch {
                return false;
            }
        }

        /// <summary>
        /// Convert from base 16
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static byte[] DecodeAsBase16(this string str) {
            if (str == null) {
                return null;
            }
            if (str.Length % 2 != 0) {
                throw new ArgumentException("Invalid length", nameof(str));
            }
            var bytes = new byte[str.Length / 2];
            for (var i = 0; i < str.Length; i += 2) {
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
        public static string ToSha1Hash(this string str) {
            return Encoding.UTF8.GetBytes(str).ToSha1Hash();
        }

        /// <summary>
        /// Trims quotes
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string TrimQuotes(this string value) {
            Contract.Assert(value != null);
            var trimmed = value.TrimMatchingChar('"');
            if (trimmed == value) {
                return value.TrimMatchingChar('\'');
            }
            return trimmed;
        }

        /// <summary>
        /// Split string using a predicate for each character that
        /// determines whether the position is a split point.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="predicate"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static IEnumerable<string> Split(this string value,
            Func<char, bool> predicate,
            StringSplitOptions options = StringSplitOptions.None) {
            Contract.Assert(value != null);
            if (predicate == null) {
                yield return value;
            }
            else {
                var next = 0;
                for (var c = 0; c < value.Length; c++) {
                    if (predicate(value[c])) {
                        var v = value.Substring(next, c - next);
                        if (options != StringSplitOptions.RemoveEmptyEntries ||
                            !string.IsNullOrEmpty(v)) {
                            yield return v;
                        }
                        next = c + 1;
                    }
                }
                if (options == StringSplitOptions.RemoveEmptyEntries && next == value.Length) {
                    yield break;
                }
                yield return value.Substring(next);
            }
        }

        /// <summary>
        /// Trims a char from front and back if both match
        /// </summary>
        /// <param name="value"></param>
        /// <param name="match"></param>
        /// <returns></returns>
        public static string TrimMatchingChar(this string value, char match) {
            Contract.Assert(value != null);
            if (value.Length >= 2 && value[0] == match &&
                value[value.Length - 1] == match) {
                return value.Substring(1, value.Length - 2);
            }
            return value;
        }

        /// <summary>
        /// Removes all whitespace and replaces it with single space.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string SingleSpacesNoLineBreak(this string value) {
            if (string.IsNullOrEmpty(value)) {
                return value;
            }
            var builder = new StringBuilder();
            var lastCharWasWs = false;
            foreach (var c in value) {
                if (char.IsWhiteSpace(c)) {
                    lastCharWasWs = true;
                    continue;
                }
                if (lastCharWasWs) {
                    builder.Append(' ');
                    lastCharWasWs = false;
                }
                builder.Append(c);
            }
            return builder.ToString();
        }
    }
}
