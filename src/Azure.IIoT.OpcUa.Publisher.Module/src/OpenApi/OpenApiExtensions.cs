// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.OpenApi
{
    using System;
    using System.Text;

    /// <summary>
    /// String helper extensions
    /// </summary>
    internal static class OpenApiExtensions
    {
        /// <summary>
        /// Yet another case insensitve equals
        /// </summary>
        /// <param name="str"></param>
        /// <param name="to"></param>
        public static bool EqualsIgnoreCase(this string str, string to)
        {
            return StringComparer.OrdinalIgnoreCase.Equals(str, to);
        }

        /// <summary>
        /// Removes all whitespace and replaces it with single space.
        /// </summary>
        /// <param name="value"></param>
        public static string? SingleSpacesNoLineBreak(this string? value)
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
