// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Extensions.DependencyInjection {
    using Microsoft.Azure.IIoT.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Builder;
    using System;
    using Microsoft.Extensions.Options;
    using Microsoft.AspNetCore.HttpsPolicy;

    /// <summary>
    /// Configure http redirection and hsts
    /// </summary>
    public static class HostingConfigurationEx {

        /// <summary>
        /// Use https redirection
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public static IApplicationBuilder UsePathBase(this IApplicationBuilder app) {
            var config = app.ApplicationServices.GetService<IWebHostConfig>();
            if (config == null) {
                return app;
            }
            if (!string.IsNullOrEmpty(config.ServicePathBase)) {
                app.UsePathBase(config.ServicePathBase);
            }
            return app;
        }


        /// <summary>
        /// Use https redirection
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseHttpsRedirect(this IApplicationBuilder app) {
            var config = app.ApplicationServices.GetService<IWebHostConfig>();
            if (config == null) {
                return app;
            }
            if (config.HttpsRedirectPort > 0) {
                app.UseHsts();
                app.UseHttpsRedirection();
            }
            return app;
        }

        /// <summary>
        /// Add https redirection
        /// </summary>
        /// <param name="services"></param>
        public static void AddHttpsRedirect(this IServiceCollection services) {
            services.AddHsts(options => {
                options.Preload = true;
                options.IncludeSubDomains = true;
                options.MaxAge = TimeSpan.FromDays(60);
            });
            services.AddHttpsRedirection(options => options.HttpsPort = 0);
            services.AddTransient<IConfigureOptions<HttpsRedirectionOptions>>(services => {
                var config = services.GetService<IWebHostConfig>();
                if (config == null) {
                    throw new InvalidOperationException("Must have configured web host context");
                }
                return new ConfigureNamedOptions<HttpsRedirectionOptions>(Options.DefaultName, options => {
                    options.RedirectStatusCode = StatusCodes.Status307TemporaryRedirect;
                    options.HttpsPort = config.HttpsRedirectPort;
                });
            });
        }
    }
}
