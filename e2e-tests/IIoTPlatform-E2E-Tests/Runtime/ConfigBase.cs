// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatform_E2E_Tests.Runtime {

    using Microsoft.Extensions.Configuration;
    using System;

    /// <summary>
    /// Configuration base helper class
    /// </summary>
    abstract class ConfigBase {

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
        /// Read configuration variable
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
    }
}
