// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Configuration
{
    using Microsoft.Extensions.Configuration;
    using System;
    using System.Collections.Generic;
    using System.Globalization;

    /// <summary>
    /// Configuration base helper class
    /// </summary>
    public abstract class ConfigureOptionBase
    {
        /// <summary>
        /// Configuration
        /// </summary>
        public IConfiguration Configuration { get; }

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        protected ConfigureOptionBase(IConfiguration configuration)
        {
            configuration ??= new ConfigurationBuilder().Build();
            Configuration = configuration;
        }

        /// <summary>
        /// Read variable and replace environment variable if needed
        /// </summary>
        /// <param name="key"></param>
        protected string? GetStringOrDefault(string key)
        {
            var value = Configuration.GetValue<string>(key);
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }
            return value.Trim();
        }

        /// <summary>
        /// Read variable and replace environment variable if needed
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        protected string GetStringOrDefault(string key, string defaultValue)
        {
            var value = Configuration.GetValue<string>(key);
            if (string.IsNullOrEmpty(value))
            {
                return defaultValue;
            }
            return value.Trim();
        }

        /// <summary>
        /// Read boolean
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        protected bool GetBoolOrDefault(string key, bool defaultValue = false)
        {
            var result = GetBoolOrNull(key);
            return result ?? defaultValue;
        }

        /// <summary>
        /// Read boolean
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        protected bool? GetBoolOrNull(string key, bool? defaultValue = null)
        {
            var value = GetStringOrDefault(key, string.Empty).ToUpperInvariant();
            var knownTrue = new HashSet<string> { "TRUE", "YES", "Y", "1" };
            var knownFalse = new HashSet<string> { "FALSE", "NO", "N", "0" };
            if (knownTrue.Contains(value))
            {
                return true;
            }
            if (knownFalse.Contains(value))
            {
                return false;
            }
            return defaultValue;
        }

        /// <summary>
        /// Get time span
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        protected TimeSpan GetDurationOrDefault(string key,
            TimeSpan defaultValue = default)
        {
            var result = GetDurationOrNull(key);
            return result ?? defaultValue;
        }

        /// <summary>
        /// Get time span
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        protected TimeSpan? GetDurationOrNull(string key,
            TimeSpan? defaultValue = null)
        {
            if (!TimeSpan.TryParse(GetStringOrDefault(key), out var result))
            {
                return defaultValue;
            }
            return result;
        }

        /// <summary>
        /// Read int
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        protected int GetIntOrDefault(string key, int defaultValue = 0)
        {
            var value = GetIntOrNull(key);
            return value ?? defaultValue;
        }

        /// <summary>
        /// Read int
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        protected int? GetIntOrNull(string key, int? defaultValue = null)
        {
            try
            {
                var value = GetStringOrDefault(key);
                if (string.IsNullOrEmpty(value))
                {
                    return defaultValue;
                }
                return Convert.ToInt32(value, CultureInfo.InvariantCulture);
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// Produces a configuration view that normalizes legacy boolean spellings
        /// before a source-generated binder consumes concrete option types.
        /// </summary>
        /// <param name="keys">Boolean-backed configuration property names.</param>
        /// <returns>A configuration view retaining the original provider precedence.</returns>
        protected IConfiguration NormalizeLegacyBooleanAliases(params string[] keys)
        {
            ArgumentNullException.ThrowIfNull(keys);
            var aliases = new HashSet<string>(keys, StringComparer.OrdinalIgnoreCase);
            var normalized = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            foreach (var (key, value) in Configuration.AsEnumerable())
            {
                if (value is null)
                {
                    continue;
                }
                var separator = key.LastIndexOf(ConfigurationPath.KeyDelimiter,
                    StringComparison.Ordinal);
                var name = separator < 0 ? key : key[(separator + 1)..];
                if (!aliases.Contains(name))
                {
                    continue;
                }
                normalized[key] = NormalizeLegacyBoolean(value);
            }
            if (normalized.Count == 0)
            {
                return Configuration;
            }
            return new ConfigurationBuilder()
                .AddConfiguration(Configuration)
                .AddInMemoryCollection(normalized)
                .Build();
        }

        private static string NormalizeLegacyBoolean(string value)
        {
            return value.Trim().ToUpperInvariant() switch
            {
                "TRUE" or "YES" or "Y" or "1" => bool.TrueString,
                "FALSE" or "NO" or "N" or "0" => bool.FalseString,
                _ => value
            };
        }
    }
}
