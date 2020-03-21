// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.AspNetCore.ForwardedHeaders {

    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.HttpOverrides;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Extension to configure processing of forwarded headers
    /// </summary>
    public static class ServiceCollectionEx {

        /// <summary>
        /// Configure processing of forwarded headers
        /// </summary>
        /// <param name="services"></param>
        /// <param name="fhConfig"></param>
        public static void ConfigureForwardedHeaders(
            this IServiceCollection services,
            IForwardedHeadersConfig fhConfig
        ) {
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

        }
    }
}
