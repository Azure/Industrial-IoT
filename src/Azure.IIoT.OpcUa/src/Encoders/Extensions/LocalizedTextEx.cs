// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Extensions
{
    /// <summary>
    /// Localized text extensions
    /// </summary>
    public static class LocalizedTextEx
    {
        /// <summary>
        /// Convert localized text to string
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string? AsString(this LocalizedText? value)
        {
            if (value == null || value.Text == null)
            {
                return null;
            }
            var full = value.Text;
            if (!string.IsNullOrEmpty(value.Locale))
            {
                return full + "@" + value.Locale;
            }
            return full;
        }

        /// <summary>
        /// Convert string to localized text
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static LocalizedText ToLocalizedText(this string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return new LocalizedText(string.Empty);
            }
            var delim = str.LastIndexOf('@');
            if (delim == -1)
            {
                return new LocalizedText(str);
            }
            return new LocalizedText(str[(delim + 1)..], str[..delim]);
        }
    }
}
