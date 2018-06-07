// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Swashbuckle.AspNetCore.Swagger {
    using Microsoft.Azure.IIoT.Auth.Azure;
    using Microsoft.Azure.IIoT.Web.Swagger;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.DependencyInjection;
    using System;
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// Configure swagger
    /// </summary>
    public static class SwaggerEx {

        /// <summary>
        /// Configure swagger
        /// </summary>
        /// <param name="services"></param>
        /// <param name="config"></param>
        public static void AddSwagger(this IServiceCollection services,
            ISwaggerConfig config, Info info) {

            if (info == null) {
                throw new ArgumentNullException(nameof(info));
            }
            if (config == null) {
                throw new ArgumentNullException(nameof(config));
            }
            // Generate swagger documentation
            services.AddSwaggerGen(options => {
                // Add info
                options.SwaggerDoc(info.Version, info);

                // Add help
                options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory,
                    config.GetType().Assembly.GetName().Name + ".xml"), true);

                // If auth enabled, need to have bearer token to access any api
                if (config.WithAuth) {
                    var resource = config as IClientConfig;
                    if (string.IsNullOrEmpty(config.SwaggerClientId) || resource == null) {
                        options.AddSecurityDefinition("bearer", new ApiKeyScheme {
                            Description =
                                "Authorization token in the form of 'bearer <token>'",
                            Name = "Authorization",
                            In = "header"
                        });
                    }
                    else {
                        options.AddSecurityDefinition("oauth2", new OAuth2Scheme {
                            Type = "oauth2",
                            Flow = "implicit",
                            AuthorizationUrl = GetAuthorityUrl(resource) +
                                "/oauth2/authorize",
                            Scopes = new Dictionary<string, string> {
                                { "read", "Access read operations" },
                                { "write", "Access write operations" }
                            },
                            TokenUrl = GetAuthorityUrl(resource) +
                                "/oauth2/token"
                        });
                    }
                }
            });
        }

        /// <summary>
        /// Use swagger in application
        /// </summary>
        /// <param name="app"></param>
        public static void UseSwagger(this IApplicationBuilder app,
            ISwaggerConfig config, Info info) {

            if (info == null) {
                throw new ArgumentNullException(nameof(info));
            }
            if (config == null) {
                throw new ArgumentNullException(nameof(config));
            }

            // Enable swagger and swagger ui
            app.UseSwagger(options => {
                options.PreSerializeFilters.Add((doc, request) =>
                    doc.Host = request.Host.Value);
            });
            if (!config.UIEnabled) {
                return;
            }
            app.UseSwaggerUI(options => {
                var resource = config as IClientConfig;
                if (config.WithAuth && resource != null) {
                    options.OAuthAppName(info.Title);
                    options.OAuthClientId(config.SwaggerClientId);
                    options.OAuthClientSecret(config.SwaggerClientSecret);
                    options.OAuthRealm(resource.ClientId);
                    options.OAuthAdditionalQueryStringParams(
                        new Dictionary<string, string> {
                            ["resource"] = resource.ClientId
                        });
                }
                options.RoutePrefix = "";
                options.SwaggerEndpoint("/swagger/v1/swagger.json", info.Version);
            });
        }

        /// <summary>
        /// Helper to create the autority url
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        private static string GetAuthorityUrl(IClientConfig config) {
            return config.Authority ?? "https://login.microsoftonline.com/" +
                (config.TenantId ?? "common");
        }
    }
}
