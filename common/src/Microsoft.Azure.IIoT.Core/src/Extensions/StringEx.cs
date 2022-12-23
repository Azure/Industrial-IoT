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
        /// Helper to create a unique name
        /// </summary>
        /// <param name="len"></param>
        /// <param name="prefix"></param>
        /// <returns></returns>
        public static string CreateUnique(int len, string prefix = "") {
            return (prefix + Guid.NewGuid().ToString("N"))
                .Substring(0, Math.Min(len, 32 + prefix.Length));
        }

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
        /// Equal to any in the list
        /// </summary>
        /// <param name="str"></param>
        /// <param name="to"></param>
        /// <param name="ignoreCase"></param>
        /// <returns></returns>
        public static bool AnyOf(this string str, IEnumerable<string> to,
            bool ignoreCase = false) {
            return to.Any(s => s.Equals(str, ignoreCase ?
                StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Equal to any in the list
        /// </summary>
        /// <param name="str"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public static bool AnyOf(this string str, params string[] to) {
            return AnyOf(str, to, false);
        }

        /// <summary>
        /// Equal to any in the list but case ignoring
        /// </summary>
        /// <param name="str"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public static bool AnyOfIgnoreCase(this string str, params string[] to) {
            return AnyOf(str, to, true);
        }

        /// <summary>
        /// Check whether this is base 64
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool IsBase64(this string str) {
            try {
                Convert.FromBase64String(str);
                return true;
            }
            catch {
                return false;
            }
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
        /// Convert to camel case
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ToCamelCase(this string value) {
            if (value == null || value.Length <= 1) {
                return value;
            }
            var words = value.Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries);
            var result = words[0].Substring(0, 1).ToLower() + words[0].Substring(1);
            for (var i = 1; i < words.Length; i++) {
                result += words[i].Substring(0, 1).ToUpper() + words[i].Substring(1);
            }
            return result;
        }

        /// <summary>
        /// Extract data between the end of the first occurence of findStart
        /// and the start of findEnd.
        /// </summary>
        /// <param name="value">The string to search</param>
        /// <param name="findStart">After which to extract</param>
        /// <param name="findEnd">until string is extracted</param>
        /// <returns></returns>
        public static string Extract(this string value, string findStart,
            string findEnd) {
            Contract.Assert(value != null);
            var start = 0;
            if (!string.IsNullOrEmpty(findStart)) {
                start = value.IndexOf(findStart, 0, StringComparison.Ordinal);
                if (start == -1) {
                    return string.Empty;
                }
                start += findStart.Length;
            }
            if (string.IsNullOrEmpty(findEnd)) {
                return value.Substring(start);
            }
            var end = value.IndexOf(findEnd, start, StringComparison.Ordinal);
            if (end == -1) {
                return string.Empty;
            }
            return value.Substring(start, end - start);
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
