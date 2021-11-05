// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


namespace Microsoft.Azure.IIoT.OpcUa.Registry.Utils {
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Pattern matcher
    /// </summary>
    public static class QueryPattern {

        /// <summary>
        /// Returns true if the target string matches the UA pattern string.
        /// The pattern string may include UA wildcards %_\[]!
        /// </summary>
        /// <param name="target">String to check for a pattern match.</param>
        /// <param name="pattern">Pattern to match with the target string.</param>
        /// <returns>true if the target string matches the pattern, otherwise false.</returns>
        public static bool Match(string target, string pattern) {
            if (string.IsNullOrEmpty(target)) {
                return false;
            }
            if (string.IsNullOrEmpty(pattern)) {
                return true;
            }

            var tokens = Parse(pattern);
            var targetIndex = 0;
            for (var index = 0; index < tokens.Count; index++) {
                targetIndex = Match(target, targetIndex, tokens, ref index);

                if (targetIndex < 0) {
                    return false;
                }
            }
            if (targetIndex < target.Length) {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Returns true if the pattern string contains a UA pattern.
        /// The pattern string may include UA wildcards %_\[]!
        /// </summary>

        public static bool IsMatchPattern(string pattern) {
            var patternChars = new char[] { '%', '_', '\\', '[', ']', '!' };
            if (string.IsNullOrEmpty(pattern)) {
                return false;
            }

            foreach (var patternChar in patternChars) {
                if (pattern.Contains(patternChar)) {
                    return true;
                }
            }
            return false;
        }


        /// <summary>
        /// Parse
        /// </summary>
        /// <param name="pattern"></param>
        /// <returns></returns>
        private static List<string> Parse(string pattern) {
            var tokens = new List<string>();

            var index = 0;
            var buffer = new StringBuilder();

            while (index < pattern.Length) {
                var ch = pattern[index];

                if (ch == '\\') {
                    index++;

                    if (index >= pattern.Length) {
                        break;
                    }

                    buffer.Append(pattern[index]);
                    index++;
                    continue;
                }

                if (ch == '_') {
                    if (buffer.Length > 0) {
                        tokens.Add(buffer.ToString());
                        buffer.Length = 0;
                    }

                    tokens.Add("_");
                    index++;
                    continue;
                }

                if (ch == '%') {
                    if (buffer.Length > 0) {
                        tokens.Add(buffer.ToString());
                        buffer.Length = 0;
                    }

                    tokens.Add("%");
                    index++;

                    while (index < pattern.Length && pattern[index] == '%') {
                        index++;
                    }

                    continue;
                }

                if (ch == '[') {
                    if (buffer.Length > 0) {
                        tokens.Add(buffer.ToString());
                        buffer.Length = 0;
                    }

                    buffer.Append(ch);
                    index++;

                    var start = 0;
                    var end = 0;
                    while (index < pattern.Length && pattern[index] != ']') {
                        if (pattern[index] == '-' && index > 0 && index < pattern.Length - 1) {
                            start = Convert.ToInt32(pattern[index - 1]) + 1;
                            end = Convert.ToInt32(pattern[index + 1]);

                            while (start < end) {
                                buffer.Append(Convert.ToChar(start));
                                start++;
                            }

                            buffer.Append(Convert.ToChar(end));
                            index += 2;
                            continue;
                        }

                        buffer.Append(pattern[index]);
                        index++;
                    }

                    buffer.Append("]");
                    tokens.Add(buffer.ToString());
                    buffer.Length = 0;

                    index++;
                    continue;
                }

                buffer.Append(ch);
                index++;
            }

            if (buffer.Length > 0) {
                tokens.Add(buffer.ToString());
                buffer.Length = 0;
            }

            return tokens;
        }

        /// <summary>
        /// Skip
        /// </summary>
        /// <param name="target"></param>
        /// <param name="targetIndex"></param>
        /// <param name="tokens"></param>
        /// <param name="tokenIndex"></param>
        /// <returns></returns>
        private static int SkipToNext(string target, int targetIndex,
            IList<string> tokens, ref int tokenIndex) {
            if (targetIndex >= target.Length - 1) {
                return targetIndex + 1;
            }

            if (tokenIndex >= tokens.Count - 1) {
                return target.Length + 1;
            }


            if (!tokens[tokenIndex + 1].StartsWith("[^", StringComparison.Ordinal)) {
                var nextTokenIndex = tokenIndex + 1;

                // skip over unmatched chars.
                while (targetIndex < target.Length &&
                    Match(target, targetIndex, tokens, ref nextTokenIndex) < 0) {
                    targetIndex++;
                    nextTokenIndex = tokenIndex + 1;
                }

                nextTokenIndex = tokenIndex + 1;

                // skip over duplicate matches.
                while (targetIndex < target.Length &&
                    Match(target, targetIndex, tokens, ref nextTokenIndex) >= 0) {
                    targetIndex++;
                    nextTokenIndex = tokenIndex + 1;
                }

                // return last match.
                if (targetIndex <= target.Length) {
                    return targetIndex - 1;
                }
            }
            else {
                var start = targetIndex;
                var nextTokenIndex = tokenIndex + 1;

                // skip over matches.
                while (targetIndex < target.Length &&
                    Match(target, targetIndex, tokens, ref nextTokenIndex) >= 0) {
                    targetIndex++;
                    nextTokenIndex = tokenIndex + 1;
                }

                // no match in string.
                if (targetIndex < target.Length) {
                    return -1;
                }

                // try the next token.
                if (tokenIndex >= tokens.Count - 2) {
                    return target.Length + 1;
                }

                tokenIndex++;

                return SkipToNext(target, start, tokens, ref tokenIndex);
            }

            return -1;
        }

        /// <summary>
        /// Match
        /// </summary>
        /// <param name="target"></param>
        /// <param name="targetIndex"></param>
        /// <param name="tokens"></param>
        /// <param name="tokenIndex"></param>
        /// <returns></returns>
        private static int Match(string target, int targetIndex,
            IList<string> tokens, ref int tokenIndex) {
            if (tokens == null || tokenIndex < 0 || tokenIndex >= tokens.Count) {
                return -1;
            }

            if (target == null || targetIndex < 0 || targetIndex >= target.Length) {
                if (tokens[tokenIndex] == "%" && tokenIndex == tokens.Count - 1) {
                    return targetIndex;
                }

                return -1;
            }

            var token = tokens[tokenIndex];

            if (token == "_") {
                if (targetIndex >= target.Length) {
                    return -1;
                }

                return targetIndex + 1;
            }

            if (token == "%") {
                return SkipToNext(target, targetIndex, tokens, ref tokenIndex);
            }

            if (token.StartsWith("[", StringComparison.Ordinal)) {
                var inverse = false;
                var match = false;

                for (var index = 1; index < token.Length - 1; index++) {
                    if (token[index] == '^') {
                        inverse = true;
                        continue;
                    }

                    if (!inverse && target[targetIndex] == token[index]) {
                        return targetIndex + 1;
                    }

                    match |= inverse && target[targetIndex] == token[index];
                }

                if (inverse && !match) {
                    return targetIndex + 1;
                }

                return -1;
            }

            if (target.Substring(targetIndex).StartsWith(token, StringComparison.Ordinal)) {
                return targetIndex + token.Length;
            }
            return -1;
        }
    }
}
