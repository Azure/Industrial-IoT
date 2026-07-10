// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Extensions.Configuration
{
    using global::Azure.IIoT.OpcUa.Core.Configuration;

    /// <summary>
    /// Configuration builder extensions.
    /// </summary>
    public static class ConfigurationBuilderEx
    {
        /// <summary>
        /// Add environment variables from a .env file in the current directory or a parent.
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IConfigurationBuilder AddFromDotEnvFile(this IConfigurationBuilder builder)
        {
            return builder.Add(new DotEnvFileSource());
        }

        /// <summary>
        /// Add environment variables from a .env file.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static IConfigurationBuilder AddFromDotEnvFile(
            this IConfigurationBuilder builder, string filePath)
        {
            return builder.Add(new DotEnvFileSource(filePath));
        }
    }
}
