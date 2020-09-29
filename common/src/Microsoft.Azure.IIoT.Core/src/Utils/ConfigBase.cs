// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Utils {
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Extensions.Configuration;
    using Serilog;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Configuration base helper class
    /// </summary>
    public abstract class ConfigBase {
        /// <summary>
        /// Logger
        /// </summary>
        protected static ILogger _logger;

        /// <summary>
        /// Configuration
        /// </summary>
        public IConfiguration Configuration { get; }

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        protected ConfigBase(IConfiguration configuration) {
            configuration ??= new ConfigurationBuilder().Build();

            Configuration = configuration;
        }

        /// <summary>
        /// Read variable and replace environment variable if needed
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        protected string GetStringOrDefault(string key, Func<string> defaultValue = null) {
            var value = Configuration.GetValue<string>(key);
            if (string.IsNullOrEmpty(value)) {
                if (defaultValue == null) {
                    return string.Empty;
                }
                return defaultValue.Invoke();
            }
            return value.Trim();
        }

        /// <summary>
        /// Read boolean
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        protected bool GetBoolOrDefault(string key, Func<bool> defaultValue = null) {
            var result = GetBoolOrNull(key);
            if (result != null) {
                return result.Value;
            }
            return defaultValue?.Invoke() ?? false;
        }

        /// <summary>
        /// Read boolean
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        protected bool? GetBoolOrNull(string key, Func<bool?> defaultValue = null) {
            var value = GetStringOrDefault(key, () => "").ToLowerInvariant();
            var knownTrue = new HashSet<string> { "true", "yes", "y", "1" };
            var knownFalse = new HashSet<string> { "false", "no", "n", "0" };
            if (knownTrue.Contains(value)) {
                return true;
            }
            if (knownFalse.Contains(value)) {
                return false;
            }
            return defaultValue?.Invoke();
        }

        /// <summary>
        /// Get time span
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        protected TimeSpan GetDurationOrDefault(string key,
            Func<TimeSpan> defaultValue = null) {
            var result = GetDurationOrNull(key);
            if (result == null) {
                if (defaultValue != null) {
                    return defaultValue.Invoke();
                }
                throw new InvalidConfigurationException(
                    $"Unable to load timespan value for '{key}' from configuration.");
            }
            return result.Value;
        }

        /// <summary>
        /// Get time span
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        protected TimeSpan? GetDurationOrNull(string key,
            Func<TimeSpan?> defaultValue = null) {
            if (!TimeSpan.TryParse(GetStringOrDefault(key), out var result)) {
                return defaultValue?.Invoke();
            }
            return result;
        }

        /// <summary>
        /// Read int
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        protected int GetIntOrDefault(string key, Func<int> defaultValue = null) {
            var value = GetIntOrNull(key);
            if (value.HasValue) {
                return value.Value;
            }
            return defaultValue?.Invoke() ?? 0;
        }

        /// <summary>
        /// Read int
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        protected int? GetIntOrNull(string key, Func<int?> defaultValue = null) {
            try {
                var value = GetStringOrDefault(key, null);
                if (string.IsNullOrEmpty(value)) {
                    return defaultValue?.Invoke();
                }
                return Convert.ToInt32(value);
            }
            catch {
                return defaultValue?.Invoke();
            }
        }

        /// <summary>
        /// Read variable and get connection string token from it
        /// </summary>
        /// <param name="key"></param>
        /// <param name="getter"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        protected string GetConnectonStringTokenOrDefault(string key,
            Func<ConnectionString, string> getter, Func<string> defaultValue = null) {
            var value = Configuration.GetValue<string>(key);
            if (string.IsNullOrEmpty(value)
                || !ConnectionString.TryParse(value.Trim(), out var cs)
                || string.IsNullOrEmpty(value = getter(cs))) {
                if (defaultValue == null) {
                    return string.Empty;
                }
                return defaultValue.Invoke();
            }
            return value;
        }

        /// <summary>
        /// Checks and warns about deprecated environment variables.
        /// </summary>
        /// <returns></returns>
        public void CheckDeprecatedVariables(ILogger logger) {
            _logger = logger;

            // List with pairs of deprecated and new/replacement options.
            // If newOption is null, warning will not suggest using it instead.
            var deprecatedOptions = new List<(string deprecatedOption, string newOption)> {
                // Deprecated on 2020-09-17.
                (PcsVariable.PCS_DEFAULT_PUBLISH_MAX_OUTGRESS_MESSAGES,
                 PcsVariable.PCS_DEFAULT_PUBLISH_MAX_EGRESS_MESSAGE_QUEUE),

                // TODO: Add rest of deprecated PCS variables,
            };

            // Warn about deprecated option and optionally suggest using new one.
            foreach (var option in deprecatedOptions) {
                if (!string.IsNullOrEmpty(Configuration.GetValue<string>(option.deprecatedOption))) {
                    string warning = @$"The parameter or environment variable '{option.deprecatedOption}' has been deprecated and will be removed in a future version. ";
                    warning += !string.IsNullOrEmpty(option.newOption)
                        ? @$"Please use '{option.newOption}' instead."
                        : "";

                    _logger?.Warning(warning);
                }
            }
        }
    }
}
