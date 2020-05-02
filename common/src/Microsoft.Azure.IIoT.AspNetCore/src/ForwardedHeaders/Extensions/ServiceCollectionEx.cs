// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Extensions.DependencyInjection {
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.HttpOverrides;
    using Microsoft.Azure.IIoT.AspNetCore.ForwardedHeaders;

    /// <summary>
    /// Extension to configure processing of forwarded headers
    /// </summary>
    public static class ServiceCollectionEx {

        /// <summary>
        /// Use header forwarding
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseHeaderForwarding(this IApplicationBuilder builder) {
            var fhConfig = builder.ApplicationServices.GetService<IForwardedHeadersConfig>();
            if (fhConfig == null || !fhConfig.AspNetCoreForwardedHeadersEnabled) {
                return builder;
            }
            return builder.UseForwardedHeaders();
        }

        /// <summary>
        /// Configure processing of forwarded headers
        /// </summary>
        /// <param name="services"></param>
        public static IServiceCollection AddHeaderForwarding(this IServiceCollection services) {
            var fhConfig = services.BuildServiceProvider().GetService<IForwardedHeadersConfig>();
            if (fhConfig == null || !fhConfig.AspNetCoreForwardedHeadersEnabled) {
                return services;
            }
            services.Configure<ForwardedHeadersOptions>(options => {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor |
                    ForwardedHeaders.XForwardedProto;

                if (fhConfig.AspNetCoreForwardedHeadersForwardLimit > 0) {
                    options.ForwardLimit = fhConfig.AspNetCoreForwardedHeadersForwardLimit;
                }

                // Only loopback proxies are allowed by default.
                // Clear that restriction because forwarders are enabled by explicit
                // configuration.
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            });
            return services;
        }
    }
}
