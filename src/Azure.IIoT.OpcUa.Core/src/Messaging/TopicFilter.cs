// ------------------------------------------------------------
//  Copyright (c) 2016-2019 Christian Kratky
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Messaging
{
    using System;
    using System.Buffers;
    using System.Text;

    /// <summary>
    /// Topic filter matcher utility.
    /// </summary>
    public static class TopicFilter
    {
        /// <summary>
        /// Check validity of filter string
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        public static bool IsValid(string? filter)
        {
            if (filter == null)
            {
                return false;
            }

            var sPos = 0;
            var sLen = filter.Length;

            while (sPos < sLen)
            {
                if (filter[sPos] == kSingleLevelWildcard)
                {
                    // Check for bad "+foo" or "a/+foo" subscription
                    if (sPos > 0 && filter[sPos - 1] != kLevelSeparator)
                    {
                        // Invalid filter string
                        return false;
                    }

                    // Check for bad "foo+" or "foo+/a" subscription
                    if (sPos < sLen - 1 && filter[sPos + 1] != kLevelSeparator)
                    {
                        // Invalid filter string
                        return false;
                    }
                }
                else if (filter[sPos] == kMultiLevelWildcard)
                {
                    if (sPos > 0 && filter[sPos - 1] != kLevelSeparator)
                    {
                        // Invalid filter string
                        return false;
                    }

                    if (sPos + 1 != sLen)
                    {
                        // Invalid filter string
                        return false;
                    }

                    return true;
                }
                sPos++;
            }
            return true;
        }

        /// <summary>
        /// Copied from Mqttnet library. See copyright notice.
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        public static bool Matches(string topic, string filter)
        {
            if (topic.Equals(filter, StringComparison.Ordinal))
            {
                return true;
            }

            var sPos = 0;
            var sLen = filter.Length;
            var tPos = 0;
            var tLen = topic.Length;

            while (sPos < sLen && tPos < tLen)
            {
                if (filter[sPos] == topic[tPos])
                {
                    if (tPos == tLen - 1)
                    {
                        // Check for e.g. foo matching foo/#
                        if (sPos == sLen - 3
                                && filter[sPos + 1] == kLevelSeparator
                                && filter[sPos + 2] == kMultiLevelWildcard)
                        {
                            return true;
                        }
                        // Check for e.g. foo/ matching foo/#
                        if (sPos == sLen - 2
                                && filter[sPos] == kLevelSeparator
                                && filter[sPos + 1] == kMultiLevelWildcard)
                        {
                            return true;
                        }
                    }

                    sPos++;
                    tPos++;

                    if (sPos == sLen && tPos == tLen)
                    {
                        return true;
                    }

                    if (tPos == tLen && sPos == sLen - 1 && filter[sPos] == kSingleLevelWildcard)
                    {
                        if (sPos > 0 && filter[sPos - 1] != kLevelSeparator)
                        {
                            // Invalid filter string
                            return false;
                        }

                        return true;
                    }
                }
                else
                {
                    if (filter[sPos] == kSingleLevelWildcard)
                    {
                        // Check for bad "+foo" or "a/+foo" subscription
                        if (sPos > 0 && filter[sPos - 1] != kLevelSeparator)
                        {
                            // Invalid filter string
                            return false;
                        }

                        // Check for bad "foo+" or "foo+/a" subscription
                        if (sPos < sLen - 1 && filter[sPos + 1] != kLevelSeparator)
                        {
                            // Invalid filter string
                            return false;
                        }

                        sPos++;
                        while (tPos < tLen && topic[tPos] != kLevelSeparator)
                        {
                            tPos++;
                        }

                        if (tPos == tLen && sPos == sLen)
                        {
                            return true;
                        }
                    }
                    else if (filter[sPos] == kMultiLevelWildcard)
                    {
                        if (sPos > 0 && filter[sPos - 1] != kLevelSeparator)
                        {
                            // Invalid filter string
                            return false;
                        }

                        if (sPos + 1 != sLen)
                        {
                            // Invalid filter string
                            return false;
                        }

                        return true;
                    }
                    else
                    {
                        // Check for e.g. foo/bar matching foo/+/#
                        if (sPos > 0
                                && sPos + 2 == sLen
                                && tPos == tLen
                                && filter[sPos - 1] == kSingleLevelWildcard
                                && filter[sPos] == kLevelSeparator
                                && filter[sPos + 1] == kMultiLevelWildcard)
                        {
                            return true;
                        }

                        return false;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Escape a path element
        /// </summary>
        /// <param name="pathElement"></param>
        /// <returns></returns>
        public static string Escape(string pathElement)
        {
            var span = pathElement.AsSpan();
            var index = span.IndexOfAny(kReservedTopicChars);
            if (index == -1)
            {
                return pathElement;
            }
            var sb = new StringBuilder();
            do
            {
                // ASCII escape character
                sb = sb.Append(span[..index]).Append(span[index] switch
                {
                    kSingleLevelWildcard => "\\x2b",
                    kMultiLevelWildcard => "\\x23",
                    kLevelSeparator => "\\x2f",
                    _ => "\\x5c",
                });
                if (++index == span.Length)
                {
                    return sb.ToString();
                }
                span = span[index..];
                index = span.IndexOfAny(kReservedTopicChars);
            }
            while (index != -1);
            return sb.Append(span).ToString();
        }

        private static readonly SearchValues<char> kReservedTopicChars = SearchValues.Create("\\/#+");
        private const char kLevelSeparator = '/';
        private const char kMultiLevelWildcard = '#';
        private const char kSingleLevelWildcard = '+';
    }
}
