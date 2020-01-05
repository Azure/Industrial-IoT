// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Extensions.Configuration {
    using System.IO;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Extension methods
    /// </summary>
    public static class ConfigurationEx {

        /// <summary>
        /// Adds .env file environment variables from an .env file that is in current
        /// folder or below up to root.
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IConfigurationBuilder AddFromDotEnvFile(this IConfigurationBuilder builder) {
            try {
                // Find .env file
                var curDir = Path.GetFullPath(Environment.CurrentDirectory);
                while (!string.IsNullOrEmpty(curDir) && !File.Exists(Path.Combine(curDir, ".env"))) {
                    curDir = Path.GetDirectoryName(curDir);
                }
                if (!string.IsNullOrEmpty(curDir)) {
                    builder.AddFromDotEnvFile(Path.Combine(curDir, ".env"));
                }
            }
            catch (IOException) { }
            return builder;
        }


        /// <summary>
        /// Adds .env file environment variables
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static IConfigurationBuilder AddFromDotEnvFile(this IConfigurationBuilder builder,
            string filePath) {
            if (!string.IsNullOrEmpty(filePath)) {
                try {
                    var lines = File.ReadAllLines(filePath);
                    var values = new Dictionary<string, string>();
                    foreach (var line in lines) {
                        var offset = line.IndexOf('=');
                        if (offset == -1) {
                            continue;
                        }
                        var key = line.Substring(0, offset).Trim();
                        if (key.StartsWith("#", StringComparison.Ordinal)) {
                            continue;
                        }
                        key = key.Replace("__", ConfigurationPath.KeyDelimiter);
                        values.AddOrUpdate(key, line.Substring(offset + 1));
                    }
                    builder.AddInMemoryCollection(values);
                }
                catch (IOException) { }
            }
            return builder;
        }

        /// <summary>
        /// Add environment variables
        /// </summary>
        /// <param name="configurationBuilder"></param>
        /// <param name="environmentVariableTarget"></param>
        /// <returns></returns>
        public static IConfigurationBuilder AddEnvironmentVariables(
            this IConfigurationBuilder configurationBuilder,
            EnvironmentVariableTarget environmentVariableTarget) {
            configurationBuilder.AddInMemoryCollection(Environment.GetEnvironmentVariables(
                environmentVariableTarget).ToKeyValuePairs<string, string>());
            return configurationBuilder;
        }
    }
}
