// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Utils {
    using System.Collections.Generic;
    using System;
    using System.Linq;

    /// <summary>
    /// Simple Command line options helper
    /// </summary>
    public sealed class CliOptions {

        /// <summary>
        /// Helper to collect options
        /// </summary>
        /// <param name="args">Command line arguments</param>
        /// <param name="offset">Offset into the array</param>
        /// <returns></returns>
        public CliOptions(string[] args, int offset = 1) {
            options = new Dictionary<string, string>();
            for (var i = offset; i < args.Length;) {
                var key = args[i];
                if (key[0] != '-') {
                    throw new ArgumentException($"{key} is not an option.");
                }
                i++;
                if (i == args.Length) {
                    options.Add(key, "");
                    break;
                }
                var val = args[i];
                if (val[0] == '-') {
                    // An option, so previous one is a boolean option
                    options.Add(key, "");
                    continue;
                }
                options.Add(key, val);
                i++;
            }
        }

        /// <summary>
        /// Split command line
        /// </summary>
        /// <param name="commandLine"></param>
        /// <returns></returns>
        public static string[] ParseAsCommandLine(string commandLine) {
            char? quote = null;
            var isEscaping = false;
            if (commandLine == null) {
                return new string[0];
            }
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
                .Select(arg => arg
                    .Trim()
                    .TrimMatchingChar(quote ?? ' ')
                    .Replace("\\\"", "\""))
                .Where(arg => !string.IsNullOrEmpty(arg))
                .ToArray();
        }

        /// <summary>
        /// Get option value
        /// </summary>
        /// <param name="key1"></param>
        /// <param name="key2"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public T GetValueOrDefault<T>(string key1, string key2, T defaultValue) {
            if (!options.TryGetValue(key1, out var value) &&
                !options.TryGetValue(key2, out value)) {
                return defaultValue;
            }
            try {
                return value.As<T>();
            }
            catch {
                throw new ArgumentException(
                    $"Invalid value '{value}' provided for parameter {key1}, {key2}.");
            }
        }

        /// <summary>
        /// Get mandatory option value
        /// </summary>
        /// <param name="key1"></param>
        /// <param name="key2"></param>
        /// <returns></returns>
        public T GetValue<T>(string key1, string key2) {
            if (!options.TryGetValue(key1, out var value) &&
                !options.TryGetValue(key2, out value)) {
                throw new ArgumentException($"Missing {key1}/{key2} option.");
            }
            try {
                return value.As<T>();
            }
            catch {
                throw new ArgumentException(
                    $"Invalid value '{value}' provided for parameter {key1}, {key2}.");
            }
        }

        /// <summary>
        /// Get boolean option value
        /// </summary>
        /// <param name="key1"></param>
        /// <param name="key2"></param>
        /// <returns></returns>
        public bool IsSet(string key1, string key2) {
            if (!options.TryGetValue(key1, out var value) &&
                !options.TryGetValue(key2, out value)) {
                return false;
            }
            if (string.IsNullOrEmpty(value)) {
                return true;
            }
            try {
                return value.As<bool>();
            }
            catch {
                throw new ArgumentException(
                    $"'{value}' cannot be evaluted as a boolean for parameter {key1}, {key2}.");
            }
        }

        /// <summary>
        /// Get mandatory option value
        /// </summary>
        /// <param name="key1"></param>
        /// <param name="key2"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public T? GetValueOrDefault<T>(string key1, string key2,
            T? defaultValue) where T : struct {
            if (!options.TryGetValue(key1, out var value) &&
                !options.TryGetValue(key2, out value)) {
                return defaultValue;
            }
            if (typeof(T).IsEnum) {
                try {
                    return (T)Enum.Parse(typeof(T), value, true);
                }
                catch {
                    throw new ArgumentException("Value must be one of [" +
                        Enum.GetNames(typeof(T)).Aggregate((a, b) => a + ", " + b) + "]");
                }
            }
            try {
                return value.As<T>();
            }
            catch {
                throw new ArgumentException(
                    $"Invalid value '{value}' provided for parameter {key1}, {key2}.");
            }
        }

        /// <summary>
        /// Get boolean option value or nullable
        /// </summary>
        /// <param name="key1"></param>
        /// <param name="key2"></param>
        /// <returns></returns>
        public bool? IsProvidedOrNull(string key1, string key2) {
            if (!options.TryGetValue(key1, out var value) &&
                !options.TryGetValue(key2, out value)) {
                return null;
            }
            if (string.IsNullOrEmpty(value)) {
                return true;
            }
            try {
                return value.As<bool>();
            }
            catch {
                throw new ArgumentException(
                    $"'{value}' cannot be evaluted as a boolean for parameter {key1}, {key2}.");
            }
        }

        private readonly Dictionary<string, string> options;
    }
}
