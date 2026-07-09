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
    }
}
