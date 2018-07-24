// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System {
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public static class StringEx {

        /// <summary>
        /// Helper to create a unique name
        /// </summary>
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
        /// <returns></returns>
        public static bool AnyOf(this string str, IEnumerable<string> to,
            bool ignoreCase = false) => to.Any(s => s.Equals(str, ignoreCase ?
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
        /// Trims only matching chars from front and back
        /// </summary>
        /// <param name="input"></param>
        /// <param name="match"></param>
        /// <returns></returns>
        public static string TrimMatching(this string input, char match) {
            if (input.Length >= 2 && input[0] == match &&
                input[input.Length - 1] == match) {
                return input.Substring(1, input.Length - 2);
            }
            return input;
        }

        /// <summary>
        /// Trims quotes
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string TrimQuotes(this string input) {
            var value = input.TrimMatching('"');
            if (value == input) {
                return input.TrimMatching('\'');
            }
            return value;
        }

        /// <summary>
        /// Replace whitespace
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string SanitizePropertyName(this string value) {
            var chars = new char[checked(value.Length)];
            unchecked {
                for (var i = 0; i < value.Length; i++) {
                    chars[i] = !char.IsLetterOrDigit(value[i]) ? '_' : value[i];
                }
            }
            return new string(chars);
        }

        /// <summary>
        /// Extract data between start and end
        /// </summary>
        /// <param name="str"></param>
        /// <param name="findStart"></param>
        /// <param name="findEnd"></param>
        /// <returns></returns>
        public static string Extract(this string str, string findStart, string findEnd) {
            var start = str.IndexOf(findStart, 0, StringComparison.Ordinal);
            if (start == -1) {
                return string.Empty;
            }
            start += findStart.Length;
            var end = str.IndexOf(findEnd, start, StringComparison.Ordinal);
            if (end == -1) {
                return string.Empty;
            }
            return str.Substring(start, end - start);
        }

        /// <summary>
        /// Split using predicate
        /// </summary>
        /// <param name="str"></param>
        /// <param name="predicate"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static IEnumerable<string> Split(this string str, Func<char, bool> predicate,
            StringSplitOptions options = StringSplitOptions.None) {
            var next = 0;
            for (var c = 0; c < str.Length; c++) {
                if (predicate(str[c])) {
                    var v = str.Substring(next, c - next);
                    if (options != StringSplitOptions.RemoveEmptyEntries ||
                        !string.IsNullOrEmpty(v)) {
                        yield return v;
                    }
                    next = c + 1;
                }
            }
            if (options == StringSplitOptions.RemoveEmptyEntries && next == str.Length) {
                yield break;
            }
            yield return str.Substring(next);
        }

        /// <summary>
        /// Split command line based on https://stackoverflow.com/questions/298830
        /// </summary>
        /// <param name="commandLine"></param>
        /// <returns></returns>
        public static string[] ParseAsCommandLine(this string commandLine) {
            char? quote = null;
            var isEscaping = false;
            return commandLine
                .Split(c => {
                    if (c == '\\' && !isEscaping) {
                        isEscaping = true;
                        return false;
                    }
                    if ((c == '"' || c == '\'') && !isEscaping) {
                        quote = c;
                    }
                    isEscaping = false;
                    return quote == null && char.IsWhiteSpace(c);
                }, StringSplitOptions.RemoveEmptyEntries)
                .Select(arg => arg.Trim().TrimMatching(quote ?? ' ').Replace("\\\"", "\""))
                .Where(arg => !string.IsNullOrEmpty(arg))
                .ToArray();
        }
    }
}
