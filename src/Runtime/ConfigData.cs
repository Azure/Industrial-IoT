// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


using Microsoft.Azure.IIoT.Diagnostics;
using Microsoft.Azure.IIoT.Exceptions;
using Microsoft.Azure.IIoT.Services.Runtime;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Microsoft.Azure.IIoT.OpcUa.Services.GdsVault.Runtime
{

    public class ConfigData : ServiceConfig
    {

        public ConfigData(string processId, string id, IConfigurationRoot configuration)
             : base(processId, id, configuration)
        {
        }

        /// <summary>
        /// Read string key and replace environment variable if needed.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public string GetString(string key, string defaultValue = "")
        {
            var value = this.Configuration.GetValue(key, defaultValue);
            ReplaceEnvironmentVariables(ref value, defaultValue);
            return value;
        }

        /// <summary>
        /// Read boolean key and replace environment variable if needed.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public bool GetBool(string key, bool defaultValue = false)
        {
            var value = GetString(key, defaultValue.ToString()).ToLowerInvariant();
            var knownTrue = new HashSet<string> { "true", "t", "yes", "y", "1", "-1" };
            var knownFalse = new HashSet<string> { "false", "f", "no", "n", "0" };
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
        /// Read int key and replace environment variable if needed.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public int GetInt(string key, int defaultValue = 0)
        {
            try
            {
                return Convert.ToInt32(GetString(key, defaultValue.ToString()));
            }
            catch (Exception e)
            {
                throw new InvalidConfigurationException(
                    $"Unable to load configuration value for '{key}'", e);
            }
        }

        /// <summary>
        /// Read log level from configuration.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public LogLevel GetLogLevel(string key, LogLevel defaultValue)
        {
            var level = GetString(key);
            if (!string.IsNullOrEmpty(level))
            {
                switch (level.ToLowerInvariant())
                {
                    case "Warning":
                        return LogLevel.Warn;
                    case "Trace":
                    case "Debug":
                        return LogLevel.Debug;
                    case "Information":
                        return LogLevel.Info;
                    case "Error":
                    case "Critical":
                        return LogLevel.Error;
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// Replace all placeholders
        /// </summary>
        /// <param name="value"></param>
        /// <param name="defaultValue"></param>
        private void ReplaceEnvironmentVariables(ref string value, string defaultValue)
        {
            if (string.IsNullOrEmpty(value))
            {
                value = defaultValue;
                return;
            }
            // Search for optional replacements: ${?VAR_NAME}
            var keys = Regex.Matches(value, @"\${\?([a-zA-Z_][a-zA-Z0-9_]*)}").Cast<Match>()
                .Select(m => m.Groups[1].Value).Distinct().ToArray();
            // Replace
            foreach (var key in keys)
            {
                value = value.Replace("${?" + key + "}", GetString(key, string.Empty));
            }

            // Pattern for mandatory replacements: ${VAR_NAME}
            const string PATTERN = @"\${([a-zA-Z_][a-zA-Z0-9_]*)}";
            // Search
            keys = Regex.Matches(value, PATTERN).Cast<Match>()
                .Select(m => m.Groups[1].Value).Distinct().ToArray();
            // Replace
            foreach (var key in keys)
            {
                var replacement = GetString(key, null);
                if (replacement != null)
                {
                    value = value.Replace("${" + key + "}", replacement);
                }
            }
            // Non replaced placeholders cause an exception
            keys = Regex.Matches(value, PATTERN).Cast<Match>()
                .Select(m => m.Groups[1].Value).ToArray();
            if (keys.Length > 0)
            {
                var varsNotFound = keys.Aggregate(", ", (current, k) => current + k);
                throw new InvalidConfigurationException(
                    "Environment variables not found: " + varsNotFound);
            }
            value.Trim();
            if (string.IsNullOrEmpty(value))
            {
                value = defaultValue;
            }
        }

    }
}
