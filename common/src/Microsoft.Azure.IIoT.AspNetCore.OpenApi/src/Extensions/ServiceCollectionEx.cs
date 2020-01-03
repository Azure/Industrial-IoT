// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.OpenApi.Models {
    using Microsoft.Azure.IIoT.AspNetCore.OpenApi;
    using Microsoft.Azure.IIoT.Auth.Clients;
    using Microsoft.Azure.IIoT.Auth.Server;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc.Controllers;
    using Microsoft.AspNetCore.Mvc.Infrastructure;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// Configure OpenApi
    /// </summary>
    public static class ServiceCollectionEx {

        /// <summary>
        /// Collect configured scopes from all controllers registered as services
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IEnumerable<string> GetRequiredScopes(
            this IServiceCollection services) {
            var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<IOptions<AuthorizationOptions>>();
            return provider.GetRequiredService<IActionDescriptorCollectionProvider>()
                .ActionDescriptors.Items
                .OfType<ControllerActionDescriptor>()
                .Cast<ControllerActionDescriptor>()
                .SelectMany(d => d.GetRequiredPolicyGlaims(options.Value))
                .Distinct();
        }

        /// <summary>
        /// Configure OpenApi
        /// </summary>
        /// <param name="services"></param>
        /// <param name="config"></param>
        /// <param name="infos"></param>
        public static void AddSwagger(this IServiceCollection services,
            IOpenApiConfig config, params OpenApiInfo[] infos) {

            if (infos == null) {
                throw new ArgumentNullException(nameof(infos));
            }
            if (config == null) {
                throw new ArgumentNullException(nameof(config));
            }

            // Generate swagger documentation
            services.AddSwaggerGen(options => {

                // Add annotations
                options.EnableAnnotations();

                // Add autorest extensions
                options.SchemaFilter<AutoRestSchemaExtensions>();

                foreach (var info in infos) {

                    // Generate doc for version
                    options.SwaggerDoc(info.Version, info);

                    // Add help
                    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory,
                        config.GetType().Assembly.GetName().Name + ".xml"), true);
                }

                // If auth enabled, need to have bearer token to access any api
                if (config.WithAuth) {
                    if (string.IsNullOrEmpty(config.OpenApiAppId) ||
                        !(config is IClientConfig resource)) {
                        options.AddSecurityDefinition("bearer", new OpenApiSecurityScheme {
                            Description =
                                "Authorization token in the form of 'bearer <token>'",
                            Name = "Authorization",
                            In = ParameterLocation.Header
                        });
                        options.OperationFilter<AutoRestOperationExtensions>();
                    }
                    else {
                        options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme {
                            Type = SecuritySchemeType.OAuth2,
                            Description = "Implicit oauth2 token flow.",
                            Flows = new OpenApiOAuthFlows {
                                Implicit = new OpenApiOAuthFlow {
                                    AuthorizationUrl = new Uri(resource.GetAuthorityUrl() + "/oauth2/authorize"),
                                    Scopes = services.GetRequiredScopes()
                                    .ToDictionary(k => k, k => $"Access {k} operations")
                                }
                            }
                        });
                        options.OperationFilter<SecurityRequirementsOperationFilter>();
                    }
                }
                else {
                    options.OperationFilter<AutoRestOperationExtensions>();
                }
            });
            services.AddSwaggerGenNewtonsoftSupport();
        }
    }
}

