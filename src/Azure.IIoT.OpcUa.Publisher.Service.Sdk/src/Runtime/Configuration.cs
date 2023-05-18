// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.Sdk.Runtime
{
    using Autofac;
    using Furly.Extensions.Configuration;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using System;

    /// <summary>
    /// Sdk configuration
    /// </summary>
    public static class Configuration
    {
        /// <summary>
        /// Configure service sdk
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="configure"></param>
        /// <returns></returns>
        public static ContainerBuilder ConfigureServiceSdk(this ContainerBuilder builder,
            Action<ServiceSdkOptions>? configure = null)
        {
            builder.AddOptions();
            if (configure != null)
            {
                builder.Configure(configure);
            }

            builder.RegisterType<SdkConfig>()
                .AsImplementedInterfaces().SingleInstance();
            return builder;
        }

        /// <summary>
        /// Sdk configurator
        /// </summary>
        internal sealed class SdkConfig : PostConfigureOptionBase<ServiceSdkOptions>
        {
            /// <summary>
            /// Configuration keys
            /// </summary>
            private const string kServiceUrlKey = "ServiceUrl";

            /// <inheritdoc/>
            public override void PostConfigure(string? name, ServiceSdkOptions options)
            {
                if (options.ServiceUrl == null)
                {
                    options.ServiceUrl = GetStringOrDefault(kServiceUrlKey,
                        GetStringOrDefault(EnvVars.PCS_SERVICE_URL,
                            "http://localhost:9080"));
                }
            }

            /// <inheritdoc/>
            public SdkConfig(IConfiguration configuration) :
                base(configuration)
            {
            }
        }
    }
}
