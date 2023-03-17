// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Utils
{
    using Microsoft.Extensions.Configuration;
    using System;
    using System.Collections.Generic;
    using System.Globalization;

    /// <summary>
    /// Configuration base helper class
    /// </summary>
    public abstract class ConfigBase
    {
        /// <summary>
        /// Configuration
        /// </summary>
        public IConfiguration Configuration { get; }

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        protected ConfigBase(IConfiguration configuration)
        {
            configuration ??= new ConfigurationBuilder()
                    .Build();
            Configuration = configuration;
        }

        /// <summary>
        /// Read variable and replace environment variable if needed
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        protected string GetStringOrDefault(string key, Func<string> defaultValue = null)
        {
            var value = Configuration.GetValue<string>(key);
            if (string.IsNullOrEmpty(value))
            {
                if (defaultValue == null)
                {
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
        protected bool GetBoolOrDefault(string key, Func<bool> defaultValue = null)
        {
            var result = GetBoolOrNull(key);
            return result ?? defaultValue?.Invoke() ?? false;
        }

        /// <summary>
        /// Read boolean
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        protected bool? GetBoolOrNull(string key, Func<bool?> defaultValue = null)
        {
            var value = GetStringOrDefault(key, () => "").ToLowerInvariant();
            var knownTrue = new HashSet<string> { "true", "yes", "y", "1" };
            var knownFalse = new HashSet<string> { "false", "no", "n", "0" };
            if (knownTrue.Contains(value))
            {
                return true;
            }
            if (knownFalse.Contains(value))
            {
                return false;
            }
            return defaultValue?.Invoke();
        }
    }
}
