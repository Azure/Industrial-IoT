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
        public static string CreateUnique(int len, string prefix = "") =>
            (prefix + Guid.NewGuid().ToString("N"))
                .Substring(0, Math.Min(len, 32 + prefix.Length));

        /// <summary>
        /// Yet another case insensitve equals
        /// </summary>
        /// <param name="str"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public static bool EqualsIgnoreCase(this string str, string to) =>
            StringComparer.OrdinalIgnoreCase.Equals(str, to);

        /// <summary>
        /// Equal to any in the list
        /// </summary>
        /// <param name="str"></param>
        /// <param name="to"></param>
        /// <param name="ignoreCase"></param>
        /// <returns></returns>
        public static bool AnyOf(this string str, IEnumerable<string> to,
            bool ignoreCase = false) =>
                to.Any(s => s.Equals(str, ignoreCase ?
                    StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Equal to any in the list
        /// </summary>
        /// <param name="str"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public static bool AnyOf(this string str, params string[] to) =>
            AnyOf(str, to, false);

        /// <summary>
        /// Equal to any in the list but case ignoring
        /// </summary>
        /// <param name="str"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public static bool AnyOfIgnoreCase(this string str, params string[] to) =>
            AnyOf(str, to, true);

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
        /// Hashes the string
        /// </summary>
        /// <param name="str">string to hash</param>
        /// <returns></returns>
        public static string ToSha1Hash(this string str) =>
            Encoding.UTF8.GetBytes(str).ToSha1Hash();

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
    }
}
