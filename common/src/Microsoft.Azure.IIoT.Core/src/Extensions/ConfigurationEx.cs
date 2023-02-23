// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Extensions.Configuration {
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Extension methods
    /// </summary>
    public static class ConfigurationEx {
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
