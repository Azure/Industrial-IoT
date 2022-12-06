// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Framework.Client.MqttClient {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;

    internal delegate bool TryParse<in TInput, TOutput>(TInput input, bool ignoreCase, out TOutput output);

    internal static class CommonExtensionMethods {
        private const char ValuePairDelimiter = ';';
        private const char ValuePairSeparator = '=';

        public static IDictionary<string, string> ToDictionary(this string valuePairString, char kvpDelimiter, char kvpSeparator) {
            if (string.IsNullOrWhiteSpace(valuePairString)) {
                throw new ArgumentException("Malformed Token");
            }

            // This regex allows semi-colons to be part of the allowed characters for device names. Although new devices are not
            // allowed to have semi-colons in the name, some legacy devices still have them and so this name validation cannot be changed.
            var parts = new Regex($"(?:^|{kvpDelimiter})([^{kvpDelimiter}{kvpSeparator}]*){kvpSeparator}")
                .Matches(valuePairString)
                .Cast<Match>()
                .Select(m => new string[] {
                    m.Result("$1"),
                    valuePairString.Substring(
                        m.Index + m.Value.Length,
                        (m.NextMatch().Success ? m.NextMatch().Index : valuePairString.Length) - (m.Index + m.Value.Length))
                });

            if (!parts.Any() || parts.Any(p => p.Length != 2)) {
                throw new FormatException("Malformed Token");
            }
            return parts.ToDictionary(kvp => kvp[0], (kvp) => kvp[1], StringComparer.OrdinalIgnoreCase);
        }

        public static void AppendKeyValuePairIfNotEmpty(this StringBuilder builder, string name, object value) {
            if (value != null) {
                builder.Append(name);
                builder.Append(ValuePairSeparator);
                builder.Append(value);
                builder.Append(ValuePairDelimiter);
            }
        }

        /// <summary>
        /// Get the required value of a property.
        /// </summary>
        /// <param name="properties">The properties dictionary.</param>
        /// <param name="propertyName">The property to get.</param>
        /// <returns>A converted value for the property.</returns>
        public static T GetRequired<T>(this IDictionary<string, string> properties, string propertyName)
            where T : IConvertible {
            return (T)Convert.ChangeType(properties[propertyName], typeof(T));
        }

        /// <summary>
        /// Get the optional value of a property.
        /// </summary>
        /// <param name="properties">The properties dictionary.</param>
        /// <param name="propertyName">The property to get.</param>
        /// <param name="defaultValue">The default value for the property.</param>
        /// <returns>A converted or default value for the property.</returns>
        public static T GetOptional<T>(this IDictionary<string, string> properties, string propertyName, T defaultValue = default(T))
            where T : IConvertible {
            properties.TryGetValue(propertyName, out string value);
            try {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch {
                return defaultValue;
            }
        }
    }
}
