// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.OpenApi.Models {
    using Microsoft.Azure.IIoT.AspNetCore.OpenApi;
    using Microsoft.Azure.IIoT.Auth.Clients;
    using Microsoft.Azure.IIoT.Auth.Server;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc.Infrastructure;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Controllers;

    /// <summary>
    /// Configure OpenApi
    /// </summary>
    public static class ServiceCollectionEx {

        /// <summary>
        /// Configure OpenApi
        /// </summary>
        /// <param name="services"></param>
        /// <param name="config"></param>
        /// <param name="title"></param>
        /// <param name="description"></param>
        public static void AddSwagger(this IServiceCollection services,
            IOpenApiConfig config, string title, string description) {

            if (config == null) {
                throw new ArgumentNullException(nameof(config));
            }

            services.AddApiVersioning(o => {
                o.AssumeDefaultVersionWhenUnspecified = true;
                o.DefaultApiVersion = new ApiVersion(1, 0);
            });

            // Controllers must be added first
            var provider = services.BuildServiceProvider();
            var api = provider.GetRequiredService<IActionDescriptorCollectionProvider>();
            var infos = api.GetOpenApiInfos(title, description);

            // Generate swagger documentation
            services.AddSwaggerGen(options => {

                // Add annotations
                options.EnableAnnotations();

                // Add autorest extensions
                options.SchemaFilter<AutoRestSchemaExtensions>();
                options.ParameterFilter<AutoRestSchemaExtensions>();
                options.RequestBodyFilter<AutoRestSchemaExtensions>();
                options.DocumentFilter<ApiVersionExtensions>();

                // Ensure the routes are added to the right Swagger doc
                options.DocInclusionPredicate((version, descriptor) => {
                    if (descriptor.ActionDescriptor is ControllerActionDescriptor desc) {
                        return desc.MatchesVersion(version);
                    }
                    return true;
                });

                foreach (var info in infos) {
                    // Generate doc for version
                    options.SwaggerDoc(info.Version, info);
                }

                // Add help
                foreach (var file in Directory.GetFiles(AppContext.BaseDirectory, "*.xml")) {
                    options.IncludeXmlComments(file, true);
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
                        var authOptions = provider.GetRequiredService<IOptions<AuthorizationOptions>>();
                        options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme {
                            Type = SecuritySchemeType.OAuth2,
                            Description = "Implicit oauth2 token flow.",
                            Flows = new OpenApiOAuthFlows {
                                Implicit = new OpenApiOAuthFlow {
                                    AuthorizationUrl =
                                        new Uri(resource.GetAuthorityUrl() + "/oauth2/authorize"),
                                    Scopes = api.GetRequiredScopes(authOptions)
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

